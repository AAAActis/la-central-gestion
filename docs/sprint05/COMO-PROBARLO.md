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

   La contraseña es la misma para los dos y **no está en el repo**. Si no la tenés, ponés una
   vos (ver «Restablecer la clave de los usuarios de demo», abajo) y se la pasás al resto.
3. Para probar el otro rol, cerrá la sesión y entrá con el otro usuario.

Datos de la demo, todos inventados: 5 proveedores, 30 artículos (por ejemplo, buscá «bujía»),
90 códigos de proveedor y 15 facturas de compra.

## Qué anotar

Lo que se probó, con qué usuario y con qué datos, va en el cuerpo del PR de la HU (sección de
CA) y, al cerrar el sprint, en `docs/sprint05/ESTADO.md` (entregable, condición 1).

## Restablecer la clave de los usuarios de demo

Pone una clave nueva a `admin.demo` y `operador.demo`, las dos iguales. No hace falta saber la
anterior. Solo escribe en `lacentral_demo`: con otra base, no arranca. Desde la raíz del repo, en
fish:

```fish
set -x LACENTRAL_DEMO_CLAVE (read -s -P "Clave nueva (mínimo 6): ")
dotnet run backend/db/demo/restablecer_clave_demo.cs
```

En bash: `read -rs LACENTRAL_DEMO_CLAVE && export LACENTRAL_DEMO_CLAVE`, y el mismo `dotnet run`.

Imprime `Clave restablecida para admin.demo y operador.demo.` La clave no se escribe en ningún
archivo del repo. Avisá al equipo que la cambiaste: deja de andar la que tenían los demás.

## El esquema de la demo y el código tienen que coincidir

Desde el 05/11/2026 `lacentral_demo` tiene los scripts 01 a 04, 07 y 08: el mismo esquema que
`develop` con el #141. Sin el 07, la ficha del artículo da 500 (`column a.codigo_barras does not
exist`). Si entra a `develop` un script nuevo, aplicalo también a la demo.

## Verificado el 04/11/2026

Desde `develop` (`9577197`): el comando levantó las dos cosas; por el proxy del frontend,
`admin.demo` y `operador.demo` entraron (200, con su rol y su sucursal) y la búsqueda
«buj» devolvió artículos de la demo. Con una cadena a `lacentral_test`, o sin la variable, no
arrancó. Ctrl+C (señal al grupo) liberó los dos puertos.

## Verificado el 05/11/2026

Desde `develop` (`dd22a7d`), con el 07 y el 08 en la demo: por el navegador (Chromium), los dos
usuarios entraron. Se buscó un artículo, se abrió la ficha y se guardó una ubicación. El
restablecimiento se probó con la misma clave que ya tenían los dos usuarios, y con una cadena a
`lacentral_test` no arrancó.
