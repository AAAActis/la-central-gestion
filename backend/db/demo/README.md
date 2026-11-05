# C3 · `lacentral_demo`

Base para mostrarle el sistema a Sergio. **Datos inventados**: 5 proveedores, 30 artículos con
2 a 4 códigos de proveedor cada uno, y 15 facturas de compra entre agosto y octubre de 2026, con
el mismo artículo comprado a proveedores distintos, a precios distintos y en fechas distintas.

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

## Ensayo del 04/11/2026

Sobre el esquema de `develop` (scripts 01 a 04), en `lacentral_test.demo_ensayo_c3`:
2 usuarios, 5 proveedores, 30 artículos, 90 códigos, 15 facturas, 180 líneas, 15 fechas
distintas. Cada artículo tiene de 2 a 4 compras a proveedores distintos y a precios distintos.
