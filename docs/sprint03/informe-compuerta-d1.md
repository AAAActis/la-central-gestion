# Informe · compuerta de esquema D1 (precios por modalidad)

2026-10-14 · Solo lectura. No se tocó esquema ni código. Insumo para que el equipo escriba el
script SQL que destraba las Fases 3 y 6.

## ⚠️ Lo que NO se pudo ejecutar: el DDL del motor

La consulta al catálogo **no corrió**. `LACENTRAL_TEST_PG` apunta a
`Host=100.125.169.91;Database=lacentral_test;Username=lacentral`. Tailscale está activo y
el host responde (`arch`, conexión directa), pero el servidor **rechaza la contraseña**:

```
Npgsql.PostgresException (0x80004005): 28P01: password authentication failed for user "lacentral"
```

El valor de la variable es el mismo en el proceso y en el registro de usuario. Se reintentó
una vez. No se buscaron otras credenciales y no se consultó la base principal.

**Por eso este informe NO contiene el DDL que devuelve el motor.** Lo de abajo sale del
script fuente y del scaffold, y está marcado como tal. Con la credencial reparada, correr:

```
psql "<LACENTRAL_TEST_PG>" -f docs/sprint03/consultas-compuerta-d1.sql
```

y pegar la salida en la sección "DDL del motor" de este archivo. Las consultas son de solo
lectura: la sesión se pone en `default_transaction_read_only = on`. Devuelven columnas
(`format_type`), PK, UNIQUE, FK y CHECK (`pg_get_constraintdef`), índices
(`pg_get_indexdef`), FKs entrantes, triggers y comentario de `articulo_margen`. También
toda columna `numeric`, `money`, `real` o `double` del esquema, ENUMs, tablas o columnas
con nombre de modalidad, y el conteo de `articulo_margen` por `numero`.

### DDL del motor
_(pendiente: ver arriba)_

## articulo_margen según el script fuente y el scaffold (NO es el motor)

`backend/db/base_completa.sql:212-220`. `rescaffold/01_Ajustes_Sprint3.sql` no la modifica.

```sql
CREATE TABLE articulo_margen (
    articulo_id INTEGER      NOT NULL REFERENCES articulo(id) ON DELETE CASCADE,
    numero      SMALLINT     NOT NULL,
    porcentaje  NUMERIC(6,2) NOT NULL,
    PRIMARY KEY (articulo_id, numero),
    CONSTRAINT margen_porcentaje_valido CHECK (porcentaje >= 0)
);
COMMENT ON TABLE articulo_margen IS
  'En La Central se usan tres márgenes: 50 %, 40 % y 0,01 %. El último representa precio de venta igual al costo, porque el sistema anterior no admite cargar cero.';
```

Scaffold (`backend/LaCentral.Data/Models/ArticuloMargen.cs` y `LaCentralDbContext.cs`, bloque `Entity<ArticuloMargen>`, `ToTable` en L232):
PK `articulo_margen_pkey (articulo_id, numero)`, FK `articulo_margen_articulo_id_fkey`,
`porcentaje` con `HasPrecision(6, 2)`. `numero` es `short`. El scaffold **no registra** el CHECK
`margen_porcentaje_valido`, porque EF no scaffoldea CHECKs. Tampoco escribe el `ON DELETE CASCADE`,
pero en una FK requerida ese es el comportamiento por defecto, así que no lo pierde. Sin índice
propio fuera de la PK. Sin columna de identidad.

Hay dos cosas para el equipo:
- La clave es `numero` (listas 1/2/3 del sistema anterior). No dice qué modalidad es cada número.
- El comentario de la tabla todavía describe el 0,01 % como "precio igual al costo". D1 lo
  reemplazó por Costo = × 1,00 (0 %). El CHECK `>= 0` ya admite el 0.

## Columnas de precio del esquema (script fuente + scaffold; tipo y escala)

| Tabla | Columna | Tipo | Null | CHECK |
|---|---|---|---|---|
| articulo | precio_costo | NUMERIC(14,2) | sí | `articulo_precios_no_negativos` (≥ 0 o null) |
| articulo | precio_venta_estimado | NUMERIC(14,2) | sí | ídem |
| articulo_margen | porcentaje | NUMERIC(6,2) | no | `margen_porcentaje_valido` (≥ 0) |
| articulo_historial_compra | precio_costo | NUMERIC(14,2) | no | `historial_precio_no_negativo` (≥ 0) |
| precio_proveedor | precio_lista | NUMERIC(14,2) | sí | — |
| precio_proveedor | precio_bonificado | NUMERIC(14,2) | sí | — |
| factura_compra_detalle | precio_unitario | NUMERIC(14,2) | no | `fcd_precio_no_negativo` (≥ 0) |
| factura_compra_detalle | descuento_porcentaje | NUMERIC(6,2) | no, default 0 | `fcd_descuento_valido` (0–100) |
| factura_venta_detalle | precio_unitario | NUMERIC(14,2) | no | `fvd_precio_no_negativo` (≥ 0) |
| factura_compra | percepcion_{iva, iibb, municipal, ganancias} | NUMERIC(14,2) | sí, default 0 | — (no son precios; D8) |

El scaffold coincide en todos los tipos y escalas (`HasPrecision` en `LaCentralDbContext.cs`).
Las únicas otras columnas decimales son cantidades `NUMERIC(12,3)`: stock,
transferencia_detalle y los dos detalles de factura. No hay `money`, `real` ni `double`.
**Sin confirmar contra el motor.**

`precio_proveedor` es el precio **del proveedor** (lista y bonificado, con `origen` en
`CONSULTA_WEB | FACTURA_COMPRA | CARGA_MANUAL`). No es un precio de venta por modalidad.

## Qué lee y qué escribe hoy el código

| Pieza | Lee | Escribe |
|---|---|---|
| `Articulo.CalcularPrecioEstimado(Modalidad)` (`backend/LaCentral.UseCases/Entidades/Articulo.cs:65`) | Solo memoria: `CostoVigente` y `Margenes` (lista de `MargenModalidad`). Fórmula `costo × (1 + %/100)`, redondeo a 2 decimales AwayFromZero. null si no hay costo o no hay margen para la modalidad | nada |
| `MargenModalidad` (`Articulo.cs:33`) | — | — · `Modalidad` es enum con nombre (1.6) y `Porcentaje` es decimal. **Ningún código lo mapea desde o hacia `articulo_margen`** |
| `ArticuloRepositorio` (`backend/LaCentral.Data/Repositorios/ArticuloRepositorio.cs`) | `ObtenerDetallePorIdAsync` (L88-106): `articulo.precio_costo` → `CostoVigente` del DTO. `PreciosPorModalidad: null` fijo (L103-105: "el esquema vigente no identifica modalidades") | **nada**: solo implementa `IConsultaArticulosRepositorio`; no hay alta ni modificación de artículo |
| `ArticuloPrecioEstimadoTests` (`backend/LaCentral.Tests/Articulos/`) | Unitarios puros sobre la entidad (10 casos: 7 Fact + 1 Theory ×3), sin base | — |
| `ArticuloRepositorioTests:119` | afirma `PreciosPorModalidad == null` | — |

Ninguna línea de código lee `articulo.precio_venta_estimado`, `articulo_margen` ni
`precio_proveedor`. Ninguna escribe en ninguna columna de precio. El contrato de salida ya
existe: `PrecioModalidadConsultaDto(string Modalidad, decimal? Valor)`
(`backend/LaCentral.UseCases/Articulos/Dtos/ConsultaArticulos.cs:33`). No tiene campo para
la razón de la ausencia que pide la Fase 4.

## ¿Hay dónde guardar un precio por modalidad CON NOMBRE?

**No, según el script fuente y el scaffold.** Pendiente de confirmar contra el motor
(consulta 5 del `.sql`).

- `articulo.precio_venta_estimado`: un solo valor sin modalidad.
- `articulo_margen`: guarda **porcentajes**, no precios. La clave es `numero SMALLINT`, sin
  nombre. Ni el script ni el código definen tipos ENUM (`CREATE TYPE`, `HasPostgresEnum` y
  `MapEnum` no aparecen).
- `precio_proveedor`: es el precio de compra por proveedor.
- Ninguna tabla tiene nombre de modalidad, tarifa, tasa o configuración de precios.

## Datos para el diseño del script (no son decisiones)

- El modelo actual es "porcentaje sobre costo". D1 tiene modalidades que no entran en ese
  modelo. Efectivo se calcula sobre **Lista** (lista × 0,90), no sobre el costo.
  CasoEspecial es un precio fijo que el recálculo no debe pisar (Fase 6). Tarjeta3 y
  Tarjeta6 dependen de tasas configurables que todavía no existen. Para esas tasas no hay
  tabla de parámetros.
- La Fase 4 exige mostrar la **razón** de un precio ausente: sin costo, sin configuración o
  caso especial no fijado. Hoy no hay dónde persistirla ni el DTO la transporta.
- `articulo_margen` tiene datos importados de Multisoft (`numero` 1/2/3). El script tiene
  que decidir qué hacer con ellos. El conteo por `numero` está en la consulta 7.
