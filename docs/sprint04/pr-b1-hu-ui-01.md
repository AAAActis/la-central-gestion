# B1 · 039 · HU-UI-01 · Estructura base y navegación

Refs #40

Pista B (Javi). Base: `develop` (c0c6b86), con `docs/sprint04-estado` ya mergeado (#129).

## Qué hace

- **Rutas con el router de Angular.** Cada pantalla tiene su dirección: `/articulos` y
  `/factura-compra`. Sin sesión, cualquier dirección muestra el login. Con sesión, una dirección
  que no existe, o la de un módulo que el rol no tiene, lleva al primer módulo del rol.
- **Un solo lugar para los módulos.** `frontend/src/app/modulos.ts` lista cada módulo con los
  roles que lo habilitan. De esa lista salen el menú y las rutas. Un módulo nuevo se agrega ahí.
  Los roles son los mismos del `[Authorize]` de su controlador.
- **Marco común.** El encabezado lleva la marca, el menú con los módulos del rol y, a la
  derecha, el usuario con **Salir**. Los botones-pestaña pasan a ser enlaces (`routerLink`). El
  módulo activo lleva una raya ámbar debajo y `aria-current="page"`. En pantallas angostas el
  encabezado se acomoda en dos líneas.
- **Carpetas** donde van a vivir B2, B3 y el Sprint 5: `nucleo/` (sesión, API, acceso por rol),
  `diseno/` (menú), `compartido/` (para el listado de HU-UI-04) y `pantallas/<pantalla>/`. Están
  descritas en `frontend/src/app/README.md`.
- **Tests de frontend con `ng test`**, usando Karma + Jasmine, el stack que instala `ng new` en
  Angular 20. Antes no había tests de frontend.
- `docs/manual/navegacion.md`: la nota de uso (punto 9 de la DoD).
- `docs/sprint04/ESTADO.md`: las respuestas de Sergio del 19/10, textuales. C4 queda
  "respondida por Sergio, pendiente de decisión del equipo".

## Qué NO hace

- **No rehace las pantallas.** Login, Artículos y Factura de compra se movieron de carpeta sin
  cambiar su código, salvo la ruta de los imports (commit 0b7cda7).
- No crea una pantalla de gestión de usuarios ni deja un enlace a una ruta que no existe.
- **Ocultar del menú no es autorizar.** El backend sigue protegiendo con `[Authorize]`. No se
  tocó `backend/` ni el esquema.
- La sesión sigue solo en memoria: **F5 la pierde**. Queda como deuda, no se resuelve acá.
- No agrega librerías de estado, de UI ni de tests aparte de las del stack de Angular.

## Criterios de aceptación (DoD punto 8)

| CA | Estado | Cómo |
|---|---|---|
| CA1 · menú con los módulos del rol | **Verificado** | `acceso.spec.ts` (filtro por rol, con la lista real para los dos roles de la base). En `app.spec.ts`: el menú con sus rutas, la marca del activo (`aria-current` y la raya visible) y el cableado real de `appConfig`. |
| CA2 · el Operador no ve gestión de usuarios | **Verificado parcialmente** | Se verificó el filtro: un módulo exclusivo de ADMINISTRADOR, que existe solo en el test, aparece para el administrador y no para el operador, y si el operador escribe la ruta queda en su primer módulo. **Motivo:** no existe pantalla de gestión de usuarios en `frontend/`, así que no hay una opción real que ocultar. |
| CA3 · conserva la sesión sin recargar | **Verificado** | `app.spec.ts`: un clic en el menú cambia de pantalla con el router, el `header` sigue siendo el mismo nodo del DOM y el token sigue en memoria. Verificación manual: lo mismo en Chrome real (ver abajo). F5 no es parte del CA. |
| CA4 · baja resolución sin desplazamiento horizontal | **Verificado** | Test en Chrome headless a 1024, 768 y 480 px, en las dos pantallas. Verificación manual en Chrome a 1024×768 y 1366×768 (y 480×800). |

### Verificación manual de CA4 y CA3

En Chrome 155 real, controlado por el protocolo de depuración (CDP), contra `ng serve`. La
extensión de Chrome no estaba conectada. **Sin backend:** la sesión se cargó desde la consola de
desarrollo de Angular (`ng.getComponent(app-root).sesion.iniciar(...)`, rol OPERADOR, token
falso). Lo que se ve es el layout, no datos.

| Ventana | Pantalla | Ancho del documento | Encabezado | Menú |
|---|---|---|---|---|
| 1024×768 | login · artículos · factura | 1024 = 1024 | 1 línea, 50 px | Artículos*, Factura de compra |
| 1366×768 | login · artículos · factura | 1366 = 1366 | 1 línea, 50 px | ídem |
| 480×800 | login · artículos · factura | 480 = 480 | 2 líneas, 99 px | ídem |

Después del clic en "Factura de compra": la URL pasa a `/factura-compra`, cambia la pantalla y el
token sigue en memoria (`sesionSigue: true`). Las capturas están en `docs/sprint04/evidencia/b1/`.

### DoD punto 10 · prueba a mano contra `lacentral_demo`

**NO verificada.** La compuerta C3 (base `lacentral_demo`) está pendiente.

## Cómo se verificó

- **Frontend:** `ng test`, 21 tests, 21 pasan, 0 fallan, 0 omitidos. 0 contra PostgreSQL (el
  frontend no toca la base). `ng build` de producción sin warnings.
- **Backend:** sin cambios. `dotnet build --no-incremental`: 0 warnings. `dotnet test`: 431 de
  431, 0 omitidos, 145 contra PostgreSQL real. Mismo conteo antes y después.
- **verificador-tdd:** ronda 1 **NO APTO**: sobrevivían tres mutaciones (el cableado de `appConfig` sin test, `routerLinkActive="otra-clase"` y `nav a.activo { }` vacío). Se arregló con tests en 42c085c. Ronda 2, acotada a 42c085c: **APTO**. Las cuatro mutaciones (las tres más el encabezado sin sesión) dan rojo, y no hay dobles absurdos.
- **auditor-persistencia:** no se corrió, porque no se tocó backend ni la base.

## TDD

| Unidad | Estructura | Test (rojo) | Producción |
|---|---|---|---|
| Infraestructura `ng test` | 2d94b43 | — | — |
| Movimiento de pantallas (sin cambio de código) | 0b7cda7 | — | — |
| 1 · Acceso por rol (CA1, CA2) | eb01b01 | dab92b4 · rojo por aserción en `acceso.spec.ts:22, :26, :35, :58` | 265679a |
| 2 · Marco, menú y rutas (CA1–CA4) | 5563f4b | 231c8fe · rojo por aserción en `app.spec.ts:77, :84-85, :92, :101-107, :120, :162-163, :171` | edfee12 |
| Caracterización CA4 a 480 px | — | 1a5ec20 (pasa a la primera) | — |
| Ronda 1 del verificador | — | 42c085c (pasa a la primera) | — |

El chore 5563f4b se enmendó antes del test rojo: el stub de rutas devolvía `[]` y el rojo salía
por `NG04002` (error del router), no por aserción. Pasó a devolver un comodín vacío.

## Excepciones declaradas: pasaron a la primera y se verificaron con mutación no commiteada

| Test | Por qué pasó | Mutación que lo pone en rojo |
|---|---|---|
| sin rol no muestra módulos | el stub devolvía `[]` | `modulosPara` devuelve todos los módulos |
| sin sesión no entra | el stub devolvía `false` | sin sesión, el rol vale OPERADOR |
| rol no habilitado no entra | el stub devolvía `false` | `habilitadoPara` ignora los roles |
| sin sesión muestra el login | la App vieja ya lo hacía | `conSesion` siempre true; el encabezado se muestra sin sesión |
| Salir vuelve al login | la App vieja ya lo hacía | no re-resolver la URL al cambiar la sesión |
| CA4 a 1024 y 768 | el layout viejo ya entraba | `main` de ancho fijo 1100 px |
| CA4 a 480 (1a5ec20) | caracteriza el `flex-wrap` del encabezado | `header` sin wrap y con `white-space: nowrap` (630 > 480) |
| appConfig (42c085c) | prueba producción existente | `MODULOS` vacío / `rutasDe([])` en `app.config.ts` |
| la marca se ve (42c085c) | prueba producción existente | `routerLinkActive="otra-clase"` / `nav a.activo { }` vacío |

En el feat 265679a se tocó `acceso.spec.ts:30` (`undefined` → `''`) por un cambio de firma: el
caso es el mismo. La guarda `rol ? … : []` se sacó porque era redundante: con `''` el filtro ya
da vacío.

## Decisiones

- **Karma + Jasmine** para `ng test`: es lo que instala `ng new` en Angular 20. Siete
  devDependencies del stack oficial, ninguna librería de terceros encima.
- **El rol sale de `SesionActual`**, o sea del `rol` que devuelve el login junto con el token. No
  se decodifica el JWT en el navegador. Es el mismo dato que ya usaba Artículos.
- **El login es la ruta comodín cuando no hay sesión.** Al entrar o salir se vuelve a resolver la
  URL actual con el router, sin recargar. Efecto: si salís estando en Factura de compra y volvés
  a entrar, quedás en Factura de compra.
- La redirección "al primer módulo del rol" supone que cada rol tiene al menos uno. Los dos roles
  de la base (`OPERADOR`, `ADMINISTRADOR`, en `backend/db/base_completa.sql:45`) los tienen: lo
  prueba `acceso.spec.ts`.

## Pendiente y deuda

- **F5 pierde la sesión** (`frontend/src/app/nucleo/sesion.ts`, sesión solo en memoria). No es
  parte de CA3. Resolverlo es una decisión de seguridad: dónde guardar el token.
- **CA2 verificado parcialmente** hasta que exista la pantalla de gestión de usuarios.
- **El `flex-wrap` del `nav`** no tiene test: con dos módulos nunca se activa a 480 px o más.
  Cuando haya más módulos, CA4 a 480 lo va a cubrir.
- `frontend/src/index.html` sigue con el título "La Central · Artículos".
- `npm install` / `npm ci` avisan que bloquearon los install scripts de esbuild, lmdb y
  msgpackr-extract, que son dependencias del build de Angular y no de esta rama. Compila y corre
  igual. No se investigó si eso pasaba antes de esta rama.
- DoD 10: la prueba contra `lacentral_demo` queda pendiente de C3.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
