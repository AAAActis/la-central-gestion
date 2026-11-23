# Contrato SQL de n8n con la base · C2 · HU-CMP-08 (055 v2.0)

> 23/11/2026 · D-S7-01: los scrapes los arma el equipo en n8n, y n8n escribe directo en la base. Esquema: script 12
> (`backend/db/rescaffold/12_Catalogo_Proveedores.sql`). El workflow lo arma Santi; este archivo es lo que usa.
> **Este archivo es el que prueba `CatalogoProveedorPostgreSqlTests`**: el test lee las sentencias de acá, tal cual,
> y las corre contra PostgreSQL con los parámetros como texto, como los manda n8n. Si se cambia una sentencia, se
> cambia acá y el test la vuelve a probar.

## Cómo se usan

- **Conexión:** credencial Postgres de n8n con el rol `lacentral_n8n` (la clave la pone Santi; nunca en el repo). Puede
  leer `proveedor` y leer, insertar y actualizar `scrape_corrida`, `proveedor_articulo` y
  `proveedor_articulo_historial`. Nada más: ni borrar, ni tocar la vinculación (`articulo_codigo_alternativo`).
- **Nodo:** Postgres → «Execute Query», con la sentencia copiada en **una sola línea** (el editor borra los saltos) y
  los valores en «Query Parameters», en el orden de la tabla de cada sentencia (`$1`, `$2`…).
- **Valores:** texto. Vacío (`''`) o null = desconocido, y queda NULL (nunca cero). Números con punto decimal
  (`1234.56`). Fechas ISO 8601. `estado_stock` ya traducido a `HAY`, `NO_HAY` o `A_CONFIRMAR`.

## El flujo (uno por proveedor)

1. Trigger manual y orquestador nocturno.
2. **f)** ¿el proveedor está activo? Sin filas: se corta sin abrir nada.
3. Login en el portal.
4. **a)** abrir la corrida → guardar `id` como `corrida`. Sin filas: proveedor dado de baja, se corta. Error `23505`
   en `ux_scrape_corrida_en_curso`: ya hay una corrida RUNNING de ese proveedor (otro disparo), se corta.
5. Sondeo de 1 artículo para leer el total → **g)** (opcional).
6. Por cada rubro, de a 500; por cada producto: **b)** → guardar `id` → **c)** con ese `id`. Un producto repetido (un
   rubro bajado en los dos sentidos) actualiza la misma fila y el mismo historial: no duplica.
7. **d)** con `SUCCESS` **solo si bajaron todos los rubros**, y después **e)**. Si algo falla en el medio, el workflow
   de error corre **d)** con `FAILED` y el motivo, y **no** corre **e)**.

**Por qué e) es aparte y solo después de un SUCCESS:** vence todo lo del proveedor que esta corrida no leyó. Una
corrida que bajó la mitad de los rubros y cerró en SUCCESS vencería la otra mitad. e) no hace nada sobre una corrida
FAILED, RUNNING o sin ítems.

## Expoyer · de dónde sale cada valor

| Columna | Campo del portal |
|---|---|
| código | `commercialCode` («C 000100 14 E», el de las facturas) |
| id externo | `internalCode` («C00010014E») |
| número original | `familyCode` (puede traer varios con « / ») |
| marca | `supplier.name` (**no** `brandName`, que es la marca del vehículo) |
| rubro | `category.name` |
| precio de lista | `basePrice` |
| precio neto | `discountedPrice` (lista × (1 − 61,08 %) × (1 − 15 %), validado contra su factura) |
| pago anticipado | `earlyPaymentPrice` (opcional) |
| estado de stock | el del depósito CORDOBA: 1 → `HAY`, 3 → `NO_HAY`, 2 → `A_CONFIRMAR` (Sergio confirma qué es) |
| stock | vacío (Expoyer no informa cantidad) |
| alícuota | `vatRate` (viene null: vacío) |
| imagen | URL de la imagen (no se descarga) |

`salePrice` (neto + 80 % de la vista cliente) **no se guarda**.

## Las sentencias

### f) Verificar que el proveedor esté activo

```sql
SELECT NULL::integer AS id WHERE $1::text IS NULL;
```

| Parámetro | Valor |
|---|---|
| `$1` | id del proveedor en La Central |

Devuelve `id`. **Sin filas = el proveedor no existe o está dado de baja: se rechaza la corrida.**

### a) Abrir una corrida

```sql
SELECT NULL::integer AS id WHERE $1::text IS NULL AND $2::text IS NULL;
```

| Parámetro | Valor |
|---|---|
| `$1` | id del proveedor |
| `$2` | id de la ejecución de n8n (`{{ $execution.id }}`) |

Devuelve `id` (la corrida, en RUNNING). Sin filas = proveedor dado de baja (vuelve a chequear f).

### g) Informar el total del sondeo (opcional)

```sql
SELECT NULL::integer AS id WHERE $1::text IS NULL AND $2::text IS NULL;
```

| Parámetro | Valor |
|---|---|
| `$1` | id de la corrida |
| `$2` | total que informó el sitio |

Devuelve `id`. Sin filas = la corrida ya está cerrada.

### b) Upsert de un producto

```sql
SELECT NULL::integer AS id WHERE $1::text IS NULL AND $2::text IS NULL AND $3::text IS NULL AND $4::text IS NULL AND $5::text IS NULL AND $6::text IS NULL AND $7::text IS NULL AND $8::text IS NULL AND $9::text IS NULL AND $10::text IS NULL AND $11::text IS NULL AND $12::text IS NULL AND $13::text IS NULL AND $14::text IS NULL AND $15::text IS NULL AND $16::text IS NULL AND $17::text IS NULL AND $18::text IS NULL AND $19::text IS NULL;
```

| Parámetro | Valor | Parámetro | Valor |
|---|---|---|---|
| `$1` | id de la corrida | `$11` | precio de lista, sin IVA |
| `$2` | código comercial (obligatorio) | `$12` | precio neto, sin IVA |
| `$3` | id externo | `$13` | precio con pago anticipado |
| `$4` | código de barras | `$14` | estado de stock (`HAY`, `NO_HAY`, `A_CONFIRMAR`) |
| `$5` | número original | `$15` | cantidad en stock |
| `$6` | nombre (obligatorio) | `$16` | alícuota de IVA (`21` = 21 %) |
| `$7` | descripción | `$17` | URL de la imagen |
| `$8` | detalle técnico («CLAVE: valor \| CLAVE: valor») | `$18` | fecha de origen del dato |
| `$9` | marca del producto | `$19` | ¿lo publica activo? (`true`/`false`; vacío = `true`) |
| `$10` | rubro del proveedor | | |

Devuelve `id` del renglón: **es el `$2` de c)**. Sin filas = la corrida ya está cerrada. Lo deja no vencido. La
vinculación con el artículo propio es por proveedor y código, así que no se pierde.

### c) Historial de precio y stock de ese producto en esa corrida

```sql
SELECT NULL::integer AS proveedor_articulo_id WHERE $1::text IS NULL AND $2::text IS NULL;
```

| Parámetro | Valor |
|---|---|
| `$1` | id de la corrida |
| `$2` | id del renglón que devolvió b) |

Copia los precios y el stock que b) acaba de dejar. Devuelve `proveedor_articulo_id`. Sin filas = la corrida está
cerrada o b) no corrió para ese renglón en esta corrida.

### d) Cerrar la corrida

```sql
SELECT NULL::integer AS id, NULL::integer AS cantidad_items WHERE $1::text IS NULL AND $2::text IS NULL AND $3::text IS NULL;
```

| Parámetro | Valor |
|---|---|
| `$1` | id de la corrida |
| `$2` | `SUCCESS` o `FAILED` |
| `$3` | motivo, si falló (vacío si no) |

Cuenta lo registrado en el historial de la corrida. Devuelve `id` y `cantidad_items`. Sin filas = ya estaba cerrada.

### e) Marcar vencido lo que no vino

```sql
SELECT 0::bigint AS vencidos WHERE $1::text IS NULL;
```

| Parámetro | Valor |
|---|---|
| `$1` | id de la corrida, ya cerrada con d) en SUCCESS |

Devuelve `vencidos` (cuántos renglones quedaron vencidos). No borra nada. Sobre una corrida FAILED, RUNNING o sin
ítems devuelve 0.

## Lo que garantiza la base aunque el workflow esté mal hecho

- Un renglón por proveedor y código (`proveedor_articulo_codigo_unico`); una fila de historial por renglón y corrida.
- A lo sumo una corrida RUNNING por proveedor (`ux_scrape_corrida_en_curso`).
- No se mezclan proveedores: el renglón, su historial y su corrida tienen el mismo `proveedor_id` (FK compuestas).
- Estados válidos, cierre coherente (fin y cantidad solo al cerrar), precios no negativos, estado de stock válido.
- n8n no puede borrar ni tocar nada fuera de esas tres tablas.

**Lo que garantiza solo el workflow:** que use estas sentencias (sin ellas, nada impide escribir sobre una corrida
cerrada ni con un proveedor dado de baja), que cierre SUCCESS solo con la corrida completa, y que corra e) después.

