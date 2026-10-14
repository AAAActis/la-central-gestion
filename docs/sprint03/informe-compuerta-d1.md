# Informe · compuerta de esquema D1 (precios por modalidad)

2026-10-14 · Solo lectura. No se tocó esquema ni código. DDL del motor incorporado el mismo día. Insumo para que el equipo escriba el
script SQL que destraba las Fases 3 y 6.

## DDL del motor (2026-10-14, con la credencial corregida)

> El primer intento del 2026-10-14 no corrió: dio `28P01`. Era una credencial vieja en
> `LACENTRAL_TEST_PG`, no el contenedor. Con la credencial corregida, la consulta corrió el
> mismo día.

**Cómo se obtuvo.** Se corrió `docs/sprint03/consultas-compuerta-d1.sql` tal cual, contra
`LACENTRAL_TEST_PG`: base `lacentral_test`, esquema plantilla `public`, PostgreSQL 18.6.
- No hay `psql` en la máquina. Se usó un ejecutor .NET de un solo archivo (Npgsql 10.0.0, en el
  scratchpad, no versionado). Parte el script por `;` y corre cada sentencia en orden, en la
  misma sesión.
- Solo lectura por partida doble: la conexión abre con `default_transaction_read_only=on`
  y el script ejecuta su propio `SET`. La salida lo confirma (`solo_lectura = on`).
- El ejecutor se niega a correr si la base no se llama `test_*` o `*_test`. No se consultó la
  base principal.
- **Único cambio respecto del `.sql`:** Npgsql no puede leer una columna `regclass` como
  objeto. En las consultas 3 y 6, `SELECT conrelid::regclass,` se ejecutó como
  `SELECT conrelid::regclass::text AS conrelid,`. Devuelve el mismo nombre de tabla, como
  texto. El resto de las sentencias van byte a byte.
- Los booleanos salen como `True`/`False` (formato .NET), no como `t`/`f` de psql.
- **Alcance:** esto es el DDL de `lacentral_test`. Que la base principal tenga el mismo esquema
  es una suposición que este informe **no verificó**. Si el script se aplica en las dos, conviene
  correr las consultas 1 a 6 también en la principal (son solo lectura) antes de aplicarlo.

### Salida tal cual

```
=> SET default_transaction_read_only = on
(ok)

=> SELECT current_database(), current_setting('server_version'), current_setting('transaction_read_only') AS solo_lectura
 current_database | current_setting               | solo_lectura
------------------+-------------------------------+--------------
 lacentral_test   | 18.6 (Debian 18.6-1.pgdg13+2) | on          
(1 fila)

=> SELECT a.attnum, a.attname, format_type(a.atttypid, a.atttypmod) AS tipo, a.attnotnull AS not_null, NULLIF(a.attidentity, '') AS identidad, pg_get_expr(d.adbin, d.adrelid) AS defecto FROM pg_attribute a LEFT JOIN pg_attrdef d ON d.adrelid = a.attrelid AND d.adnum = a.attnum WHERE a.attrelid = 'public.articulo_margen'::regclass AND a.attnum > 0 AND NOT a.attisdropped ORDER BY a.attnum
 attnum | attname     | tipo         | not_null | identidad | defecto
--------+-------------+--------------+----------+-----------+---------
 1      | articulo_id | integer      | True     |           |        
 2      | numero      | smallint     | True     |           |        
 3      | porcentaje  | numeric(6,2) | True     |           |        
(3 filas)

=> SELECT conname, contype, pg_get_constraintdef(oid) FROM pg_constraint WHERE conrelid = 'public.articulo_margen'::regclass ORDER BY contype, conname
 conname                              | contype | pg_get_constraintdef                                               
--------------------------------------+---------+---------------------------------------------------------------------
 margen_porcentaje_valido             | c       | CHECK ((porcentaje >= (0)::numeric))                               
 articulo_margen_articulo_id_fkey     | f       | FOREIGN KEY (articulo_id) REFERENCES articulo(id) ON DELETE CASCADE
 articulo_margen_articulo_id_not_null | n       | NOT NULL articulo_id                                               
 articulo_margen_numero_not_null      | n       | NOT NULL numero                                                    
 articulo_margen_porcentaje_not_null  | n       | NOT NULL porcentaje                                                
 articulo_margen_pkey                 | p       | PRIMARY KEY (articulo_id, numero)                                  
(6 filas)

=> SELECT indexname, indexdef FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'articulo_margen'
 indexname            | indexdef                                                                                            
----------------------+------------------------------------------------------------------------------------------------------
 articulo_margen_pkey | CREATE UNIQUE INDEX articulo_margen_pkey ON public.articulo_margen USING btree (articulo_id, numero)
(1 fila)

=> SELECT conrelid::regclass::text AS conrelid, conname, pg_get_constraintdef(oid) FROM pg_constraint WHERE confrelid = 'public.articulo_margen'::regclass
 conrelid | conname | pg_get_constraintdef
----------+---------+----------------------
(0 filas)

=> SELECT tgname, pg_get_triggerdef(oid) FROM pg_trigger WHERE tgrelid = 'public.articulo_margen'::regclass AND NOT tgisinternal
 tgname | pg_get_triggerdef
--------+-------------------
(0 filas)

=> SELECT obj_description('public.articulo_margen'::regclass, 'pg_class')
 obj_description                                                                                                                                                 
------------------------------------------------------------------------------------------------------------------------------------------------------------------
 En La Central se usan tres márgenes: 50 %, 40 % y 0,01 %. El último representa precio de venta igual al costo, porque el sistema anterior no admite cargar cero.
(1 fila)

=> SELECT c.table_name, c.column_name, format_type(a.atttypid, a.atttypmod) AS tipo, c.numeric_precision, c.numeric_scale, c.is_nullable, c.column_default FROM information_schema.columns c JOIN pg_attribute a ON a.attrelid = (quote_ident(c.table_schema) || '.' || quote_ident(c.table_name))::regclass AND a.attname = c.column_name WHERE c.table_schema = 'public' AND a.atttypid IN ('numeric'::regtype, 'money'::regtype, 'real'::regtype, 'double precision'::regtype) ORDER BY 1, 2
 table_name                | column_name           | tipo          | numeric_precision | numeric_scale | is_nullable | column_default
---------------------------+-----------------------+---------------+-------------------+---------------+-------------+----------------
 articulo                  | precio_costo          | numeric(14,2) | 14                | 2             | YES         |               
 articulo                  | precio_venta_estimado | numeric(14,2) | 14                | 2             | YES         |               
 articulo_historial_compra | precio_costo          | numeric(14,2) | 14                | 2             | NO          |               
 articulo_margen           | porcentaje            | numeric(6,2)  | 6                 | 2             | NO          |               
 factura_compra            | percepcion_ganancias  | numeric(14,2) | 14                | 2             | YES         | 0             
 factura_compra            | percepcion_iibb       | numeric(14,2) | 14                | 2             | YES         | 0             
 factura_compra            | percepcion_iva        | numeric(14,2) | 14                | 2             | YES         | 0             
 factura_compra            | percepcion_municipal  | numeric(14,2) | 14                | 2             | YES         | 0             
 factura_compra_detalle    | cantidad              | numeric(12,3) | 12                | 3             | NO          |               
 factura_compra_detalle    | descuento_porcentaje  | numeric(6,2)  | 6                 | 2             | NO          | 0             
 factura_compra_detalle    | precio_unitario       | numeric(14,2) | 14                | 2             | NO          |               
 factura_venta_detalle     | cantidad              | numeric(12,3) | 12                | 3             | NO          |               
 factura_venta_detalle     | precio_unitario       | numeric(14,2) | 14                | 2             | NO          |               
 precio_proveedor          | precio_bonificado     | numeric(14,2) | 14                | 2             | YES         |               
 precio_proveedor          | precio_lista          | numeric(14,2) | 14                | 2             | YES         |               
 stock                     | cantidad              | numeric(12,3) | 12                | 3             | NO          | 0             
 transferencia_detalle     | cantidad              | numeric(12,3) | 12                | 3             | NO          |               
(17 filas)

=> SELECT table_name, column_name, data_type FROM information_schema.columns WHERE table_schema = 'public' AND column_name ~* '(modalidad|tarjeta|efectivo|transferencia|lista|especial|tasa|recargo|tarifa)' ORDER BY 1, 2
 table_name            | column_name      | data_type
-----------------------+------------------+-----------
 precio_proveedor      | precio_lista     | numeric  
 transferencia_detalle | transferencia_id | integer  
(2 filas)

=> SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name ~* '(modalidad|precio|margen|tarifa|lista|tasa|config|parametro)' ORDER BY 1
 table_name      
------------------
 articulo_margen 
 precio_proveedor
(2 filas)

=> SELECT t.typname, string_agg(e.enumlabel, ', ' ORDER BY e.enumsortorder) FROM pg_type t JOIN pg_enum e ON e.enumtypid = t.oid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' GROUP BY 1
 typname | string_agg
---------+------------
(0 filas)

=> SELECT conrelid::regclass::text AS conrelid, conname, pg_get_constraintdef(c.oid) FROM pg_constraint c JOIN pg_namespace n ON n.oid = c.connamespace WHERE n.nspname = 'public' AND c.contype = 'c' AND pg_get_constraintdef(c.oid) ~* '(precio|costo|margen|porcentaje|modalidad|numero)' ORDER BY 1, 2
 conrelid                  | conname                       | pg_get_constraintdef                                                                                                                                 
---------------------------+-------------------------------+-------------------------------------------------------------------------------------------------------------------------------------------------------
 articulo                  | articulo_precios_no_negativos | CHECK ((((precio_costo IS NULL) OR (precio_costo >= (0)::numeric)) AND ((precio_venta_estimado IS NULL) OR (precio_venta_estimado >= (0)::numeric))))
 articulo_historial_compra | historial_precio_no_negativo  | CHECK ((precio_costo >= (0)::numeric))                                                                                                               
 articulo_margen           | margen_porcentaje_valido      | CHECK ((porcentaje >= (0)::numeric))                                                                                                                 
 factura_compra_detalle    | fcd_descuento_valido          | CHECK (((descuento_porcentaje >= (0)::numeric) AND (descuento_porcentaje <= (100)::numeric)))                                                        
 factura_compra_detalle    | fcd_precio_no_negativo        | CHECK ((precio_unitario >= (0)::numeric))                                                                                                            
 factura_venta_detalle     | fvd_precio_no_negativo        | CHECK ((precio_unitario >= (0)::numeric))                                                                                                            
(6 filas)

=> SELECT numero, count(*), min(porcentaje), max(porcentaje) FROM public.articulo_margen GROUP BY 1 ORDER BY 1
 numero | count | min | max
--------+-------+-----+-----
(0 filas)

```

### Diferencias entre el motor y el scaffold / script fuente

**Ninguna diferencia de tipo, escala, nulabilidad, clave, FK, CHECK ni índice** en lo que toca
a D1. Las columnas de precio y de cantidad coinciden una por una con la tabla de abajo y con los
`HasPrecision` del scaffold. Hay seis puntos a tener en cuenta para el script SQL:

1. ⚠️ **`articulo_margen` está VACÍA en `lacentral_test`** (consulta 7: 0 filas). Este informe
   decía que "tiene datos importados de Multisoft". **En la base de test eso no es cierto.** No
   se consultó la base principal (está prohibido apuntarle), así que **no se sabe cuántas filas
   tiene allá**. Si el script migra o borra datos de `articulo_margen`, hay que contarlos en la
   base principal antes de decidir. Probarlo en `lacentral_test` no ejercita esa migración.
2. ⚠️ **NOT NULL con nombre (PostgreSQL 18).** El motor cataloga cada NOT NULL como una
   restricción `contype = 'n'` con nombre propio, por ejemplo `articulo_margen_numero_not_null`.
   Ni `base_completa.sql` ni el scaffold los mencionan. Hay que tenerlo en cuenta si el script
   hace `DROP CONSTRAINT` por nombre o recrea la tabla: esos nombres existen y pueden chocar.
3. **`ON DELETE CASCADE`** de `articulo_margen_articulo_id_fkey`: está en el motor. El scaffold
   no lo escribe, y en una FK requerida EF ya hace cascada por defecto. No es una diferencia de
   comportamiento.
4. **CHECK `margen_porcentaje_valido`**: está en el motor como `CHECK ((porcentaje >= (0)::numeric))`.
   El scaffold no lo registra porque EF no scaffoldea CHECKs. Ya estaba declarado.
5. **DEFAULT 0 sin reflejo en el scaffold:** `factura_compra_detalle.descuento_porcentaje` y
   `stock.cantidad` tienen `DEFAULT 0` en el motor, pero `LaCentralDbContext.cs` no les pone
   `HasDefaultValue`. Las percepciones (`numeric` nullable, `DEFAULT 0`) sí lo tienen
   (`HasDefaultValue(0m)`). Se explica porque EF omite el default cuando coincide con el valor
   por defecto de la propiedad: `0m` en un `decimal` no anulable. **No es una diferencia de
   esquema**, pero un re-scaffold va a dar lo mismo. No esperen verlo aparecer.
6. **El comentario de la tabla** en el motor sigue diciendo "0,01 %… precio de venta igual al
   costo". Coincide con el script y el scaffold, y contradice D1 (Costo = × 1,00).

Lo demás confirma lo que este informe ya decía desde el script y el scaffold:
- No hay ningún tipo ENUM en `public`.
- No hay `money`, `real` ni `double precision`.
- Las únicas tablas con nombre de precio o margen son `articulo_margen` y `precio_proveedor`.
- Ninguna columna tiene nombre de modalidad. `precio_lista` es del proveedor, y
  `transferencia_id` es de transferencia de stock y no tiene nada que ver.
- **No hay dónde guardar un precio por modalidad con nombre**: confirmado contra el motor.

## articulo_margen según el script fuente y el scaffold (coincide con el motor: ver arriba)

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
**Confirmado contra el motor (consulta 4).**

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

**No. Confirmado contra el motor**
(consulta 5 del `.sql`: ninguna columna ni tabla de modalidad, ningún ENUM).

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
- `articulo_margen` está vacía en `lacentral_test` (consulta 7). En la base principal puede tener datos de Multisoft (`numero` 1/2/3), sin contar. El script tiene
  que decidir qué hacer con ellos, y el conteo hay que hacerlo allá (ver diferencia 1).
