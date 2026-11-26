# C3 · `lacentral_demo`

Base para mostrarle el sistema a Sergio. **Datos inventados**: 5 proveedores, 30 artículos con
2 a 4 códigos de proveedor cada uno, y 15 facturas de compra entre agosto y octubre de 2026, con
el mismo artículo comprado a proveedores distintos, a precios distintos y en fechas distintas.

Hoy la demo tiene **8 proveedores**: los 5 de `sembrar_demo.cs` (`DEMO-01` a `DEMO-05`), Expoyer
(`00035`, cargado a mano por el equipo, sin sufijo «(demo)») y ETMAN y Fazio, del complemento de
la sección 4 (`DEMO-06` y `DEMO-07`).

No se demuestra sobre `lacentral_test` (los tests crean y borran esquemas ahí) ni sobre
`lacentral` (datos reales de la empresa).

## 1 · Crear la base (lo hace el equipo, una vez)

El rol de desarrollo no tiene `CREATEDB` ni `CONNECT` sobre `lacentral`. Con un rol que sí:

```sql
CREATE DATABASE lacentral_demo;
-- dentro de lacentral_demo:
--   la PARTE A de ../base_completa.sql (sin la PARTE B ni la C: no se importa Multisoft),
--   ../rescaffold/01 a 04, en orden,
--   y el 07 SOLO cuando el código que se demuestra tenga el re-scaffold del 07.
GRANT CONNECT ON DATABASE lacentral_demo TO <rol de desarrollo>;
```

El esquema de la base tiene que ser el mismo que el código que corre contra ella.

## 2 · Levantar la API contra la base de demo

```fish
set -x ConnectionStrings__Postgres "<cadena de lacentral_demo>"
set -x Jwt__Key (head -c 48 /dev/urandom | base64)
dotnet run --project backend/LaCentral.Api --urls http://localhost:5181
```

## 3 · Sembrar

```fish
set -x LACENTRAL_DEMO_PG "<cadena de lacentral_demo>"
set -x LACENTRAL_DEMO_CLAVE "<clave de los usuarios de demo>"   # no va al repo
dotnet run backend/db/demo/sembrar_demo.cs
```

- Corre una sola vez y sobre una base vacía: si encuentra proveedores, artículos o usuarios, aborta.
- Escribe solo en `lacentral_demo` (la otra excepción es un ensayo: un esquema `demo_ensayo_*`
  dentro de `lacentral_test`).
- Usuarios: `admin.demo` (ADMINISTRADOR, Fragueiro) y `operador.demo` (OPERADOR, San Vicente),
  los dos con `LACENTRAL_DEMO_CLAVE`.
- Proveedores, artículos y compras entran **por la API**: el stock, el costo vigente, el historial
  y los precios por modalidad salen de los casos de uso reales. Por SQL van solo los usuarios y la
  fecha de registro de cada compra (la API estampa «ahora»).

## 4 · Complemento: proveedores para el reconocimiento por razón social (Fix E, #196)

Se corre **después** de `sembrar_demo.cs`, con SQL contra `lacentral_demo`. No hay `psql` en todas las
máquinas: se aplicó con un `.cs` de una sola vez que ejecuta el archivo con Npgsql.

`sembrar_proveedores_pdf.sql` agrega `DEMO-06` «ETMAN Repuestos (demo)» y `DEMO-07` «Fazio (demo)»:
activos y **sin CUIT**, para que ningún CUIT del PDF los ubique y se pruebe el camino por razón
social, que los deja marcados «Revisar».

- **SQL directo**, excepción a «por la API» de la sección 3: son dos filas sin CUIT, hechas
  a propósito para probar el reconocimiento por razón social del Fix E.
- **Solo `lacentral_demo`**: tiene una guarda que corta con una excepción si `current_database()`
  es otra. No tocar `lacentral` ni `lacentral_test`.
- **Idempotente por `codigo`** (`ON CONFLICT (codigo) DO NOTHING`) y una sola transacción, con
  verificación final: exactamente un ETMAN y un Fazio activos.
- Expoyer ya está en la demo; no se siembra.

### 4 bis · Marcarlos en negro (Fix E2, D-S7-12)

Después del anterior, `marcar_en_negro_demo.sql` pone `cuit_simulado = true` en `DEMO-06`, `DEMO-07` y Expoyer
(`00035`). Sin eso, un PDF sin CUIT conocido no los reconoce; una factura A no se asigna nunca por razón social.
Solo `lacentral_demo` (tiene guarda), idempotente; lo aplica Javi o Santi.

## Ensayo del 04/11/2026

Sobre el esquema de `develop` (scripts 01 a 04), en `lacentral_test.demo_ensayo_c3`:
2 usuarios, 5 proveedores, 30 artículos, 90 códigos, 15 facturas, 180 líneas, 15 fechas
distintas. Cada artículo tiene de 2 a 4 compras a proveedores distintos y a precios distintos.
