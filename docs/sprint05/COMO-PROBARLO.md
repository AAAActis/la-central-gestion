# Cómo probarlo a mano · contra `lacentral_demo`

El punto 10 de la Definición de Terminado exige que cada HU se recorra a mano en el navegador
contra `lacentral_demo`. Esto es lo que hay que hacer para eso.

## Una vez por máquina

Necesitás `dotnet` (10), `node`/`npm` y acceso por Tailscale al PostgreSQL del equipo.
Definí la cadena de conexión a la demo. En fish, persistente:

```fish
set -Ux LACENTRAL_DEMO_PG "Host=<host>;Port=5432;Database=lacentral_demo;Username=<tu rol>;Password=<clave>;Maximum Pool Size=5"
```

En bash, en `~/.bashrc`: `export LACENTRAL_DEMO_PG="..."`, con la misma cadena.

Es la misma cadena que tu `LACENTRAL_TEST_PG`, cambiando `lacentral_test` por `lacentral_demo`.

## El comando

Desde la raíz del repo:

```bash
scripts/probar-demo.sh
```

- Levanta la API en `http://localhost:5181`, apuntada a `lacentral_demo`, y el frontend en
  `http://localhost:4200`. El frontend le pasa `/api` a la API (`frontend/proxy.conf.json`).
- La primera vez corre `npm ci` si falta `frontend/node_modules`.
- **No arranca si la cadena no dice `Database=lacentral_demo`.** Probar a mano es escribir, y
  eso nunca va contra `lacentral` (datos reales) ni contra `lacentral_test`.
- La clave JWT se genera en cada arranque: al reiniciar, hay que volver a entrar.
- **Ctrl+C corta los dos.**

## Entrar

1. Abrí `http://localhost:4200`. Está listo cuando la consola muestra
   `Now listening on: http://localhost:5181` y `Local: http://localhost:4200/`.
2. Entrá con uno de los dos usuarios de demo:

   | Usuario | Rol | Sucursal |
   |---|---|---|
   | `admin.demo` | ADMINISTRADOR | Fragueiro |
   | `operador.demo` | OPERADOR | San Vicente |

   La contraseña es la misma para los dos. **No está en el repo:** pedísela a Santi (está en su
   máquina, en `~/.config/lacentral/clave-demo.txt`).
3. Para probar el otro rol, cerrá la sesión y entrá con el otro usuario.

Datos de la demo, todos inventados: 5 proveedores, 30 artículos (por ejemplo, buscá «bujía»),
90 códigos de proveedor y 15 facturas de compra.

## Qué anotar

Lo que se probó, con qué usuario y con qué datos, va en el cuerpo del PR de la HU (sección de
CA) y, al cerrar el sprint, en `docs/sprint05/ESTADO.md` (entregable, condición 1).

## Cuidado: el esquema de la demo y el código tienen que coincidir

`lacentral_demo` tiene el esquema de `develop` **antes** del #141 (scripts 01 a 04, sin el 07).
El 07 se le aplica el lunes 09/11 (S5-06 del ESTADO). **Entre el merge del #141 y la aplicación
del 07, el código de `develop` no coincide con la demo:** la ficha del artículo lee la ubicación
por sucursal, que sin el 07 no existe. En esa ventana, probá desde un commit anterior al #141.

## Verificado el 04/11/2026

Desde `develop` (`9577197`): el comando levantó las dos cosas; por el proxy del frontend,
`admin.demo` y `operador.demo` entraron (200, con su rol y su sucursal) y la búsqueda
«buj» devolvió artículos de la demo. Con una cadena a `lacentral_test`, o sin la variable, no
arrancó. Ctrl+C (señal al grupo) liberó los dos puertos.
