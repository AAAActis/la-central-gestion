# Rediseño visual del frontend · La Central Repuestos

> Prompt para el agente. Llevar el diseño del prototipo aprobado al frontend Angular real.
> Leelo completo antes de tocar nada. Se trabaja **por fases**: al terminar cada una, PARÁS,
> reportás y esperás el visto bueno de Javi antes de seguir. Dentro de una fase trabajás de
> corrido, sin pedir confirmación entre commits.

---

## 0 · Material de referencia

En `Claude outputs/rediseno-ui/` (carpeta sin versionar en la raíz del repo):

| Archivo | Qué es |
|---|---|
| `SISTEMA-VISUAL.md` | **La regla permanente de UI**: tokens, tipografía, espaciado, fondos, íconos, botones, componentes, animación y accesibilidad. Manda sobre cualquier otra cosa de este prompt en lo visual. |
| `prototipo/index.html` | Prototipo navegable con CSS y JS en un solo archivo. Usuario `admin.muestra` u `operador.muestra`, contraseña `demo1234` (datos inventados del prototipo). |
| `referencias/01-acceso.png` … `13-botones.png` | Las 13 capturas aprobadas. **Son la fuente de verdad visual.** |

Primer paso de la Fase 1: copiar ese material a `docs/diseno/`
(`docs/diseno/SISTEMA-VISUAL.md`, `docs/diseno/prototipo/index.html`,
`docs/diseno/referencias/*.png`) para que quede versionado junto con el código que lo implementa,
y agregar a `CLAUDE.md` esta línea en la sección de frontend:

> **UI:** toda pantalla o componente de `frontend/` sigue `docs/diseno/SISTEMA-VISUAL.md`.
> No se inventan colores, tamaños ni animaciones fuera de sus tokens.

**El prototipo es una referencia VISUAL, no funcional.** De él se toman los colores, la tipografía,
los espaciados, la estructura de cada pantalla, los estados (vacío, error, carga, deshabilitado)
y las animaciones. **No** se toman:

- su JavaScript (es vanilla y está pensado para el navegador: no se porta a Angular);
- sus datos (son inventados);
- sus reglas de negocio: precios calculados en el cliente, `localStorage`, permisos, validaciones
  de unicidad. Todo eso ya lo resuelve el backend o lo decide el equipo.

---

## 1 · Reglas que no se negocian

Leé antes que nada `CLAUDE.md`, `AGENTS.md`, `docs/PROMPT-FASE.md`, `docs/sprint04/ESTADO.md`,
`frontend/src/app/README.md` y `Claude outputs/rediseno-ui/SISTEMA-VISUAL.md`. Para entender el
código, `graphify query "<pregunta>"` antes de abrir archivos de a uno. Donde `PROMPT-FASE.md`
dice `docs/sprint03/`, usá `docs/sprint04/`.

1. **Solo frontend.** No se toca `backend/` ni el esquema. Si una pantalla del prototipo necesita
   un endpoint que no existe, **no se inventa**: la pantalla o la acción queda afuera y la anotás
   en el reporte. Nada de datos simulados en el frontend real.
2. **Estructura existente.** `modulos.ts` sigue siendo la única fuente del menú y las rutas;
   `nucleo/` (sesión, API, acceso), `diseno/` (marco visual) y `compartido/` (componentes reusables),
   tal como describe `frontend/src/app/README.md`. Componentes standalone, como los actuales.
3. **Encaje con el sprint.** Este rediseño no reemplaza a B2 (HU-UI-04, listado reutilizable)
   ni a B3 (HU-UI-12, comparador): las sostiene. En particular:
   - la búsqueda y la tabla de artículos (Fase 3) se construyen **sobre el componente de listado
     de HU-UI-04** (búsqueda, sin resultados, inactivos, paginación del servidor). Si ese
     componente todavía no está en `develop`, **PARÁ** antes de la Fase 3 y reportalo: no se arma
     una tabla suelta que después haya que rehacer;
   - todo lo que se cree en la Fase 1 queda disponible para B3.
4. **Sin librerías nuevas.** Ni Angular Material, ni Tailwind, ni librerías de íconos o de
   animación. CSS propio e íconos SVG en línea, como dice `SISTEMA-VISUAL.md`.
5. **Las reglas de negocio vienen del backend, no del prototipo.** En particular:
   - **Modalidades de precio:** se muestran las que devuelve la API, en su orden. No se hardcodean
     «Tarjeta 3 cuotas» ni «Tarjeta 6 cuotas»: la compuerta **C1** (alta de L2 +40 % y retiro de
     Tarjeta3/6) está pendiente. Un precio sin importe muestra el motivo que manda la API; un
     costo desconocido se muestra «Sin costo», nunca `$ 0,00`.
   - **Código del artículo:** la compuerta **C4** sigue abierta (Sergio respondió código original +
     marca; falta la decisión del equipo y el esquema). No se agregan campos de código original ni
     de marca, ni un campo editable que el backend hoy no acepte. Se muestra lo que la API permite.
   - **Permisos:** el rol se lee de la sesión (`SesionActual`) para mostrar u ocultar acciones,
     pero ocultar no es autorizar: el backend sigue mandando. Un 401 o 403 se muestra como
     mensaje, no rompe la pantalla.
   - **Precios y stock** los calcula el backend. El frontend no recalcula ni redondea por su cuenta.
6. **Sesión:** sigue solo en memoria. La deuda de F5 no se resuelve en este trabajo.
7. **Todo en español:** clases, métodos, variables, textos, commits.
8. **Git:** rama nueva saliendo de `develop`, con `git checkout -b`. Antes de crearla, confirmá con
   Javi el ID de la HU para el nombre (`feature/hu-ui-nn-rediseno-visual`). Conventional Commits.
   Lo que tenga comportamiento sigue el ciclo de `PROMPT-FASE.md` (estructura → test en rojo por
   aserción → producción); lo puramente visual (CSS, marcado sin lógica) puede ir en un commit
   `style(...)` por pantalla. **Sin push** hasta que Javi lo apruebe.
9. **ESTADO.md** se actualiza antes del push y va en el mismo push.

---

## 2 · El sistema visual

**Está en `SISTEMA-VISUAL.md` y no se reinventa.** En la Fase 1 sus tokens pasan a
`frontend/src/styles.css` **reemplazando** los actuales (`--acento` azul, `--senal` ámbar, etc.).
Antes de borrar un token, buscá dónde se usa y migrá ese uso. Al terminar, ningún componente
tiene un color, tamaño o duración escrito a mano: todo es `var(--token)`.

Decisiones que la Fase 0 le pregunta a Javi:
- **Tipografía:** servirla desde `public/fonts/` (recomendado: no depende de internet ni le pide
  nada a un tercero cada vez que se abre el sistema en el local) o cargarla desde Google Fonts en
  `index.html`. En los dos casos, con la pila de respaldo de `SISTEMA-VISUAL.md`.
- **Componentes de base:** clases globales en `styles.css` o componentes en `compartido/`, lo que
  sea más simple de defender. Proponé cuál y por qué.

Lo que no está en `SISTEMA-VISUAL.md` y es de este trabajo:
- **Marca:** copiar el SVG del prototipo a `diseno/marca.ts`, con las dos variantes de la §6 de
  `SISTEMA-VISUAL.md`. **Es provisorio**: el logo se va a cambiar, así que tiene que vivir solo en
  ese componente. La «C» usa `--rojo-marca`, no `--rojo`.
- **Fondo hexagonal:** la fuente es la imagen de marca de La Central (panal en relieve) que Javi
  agrega en `Claude outputs/rediseno-ui/marca/`. Si no está, exportá el canvas del prototipo.
  En los dos casos, **una vez**, a `public/fondo-hex.jpg` (1600×1100, JPEG, menos de 250 KB),
  usado como `background-image` con color de respaldo `#2b2b2e`. En la Fase 0, preguntale a Javi
  si esa imagen es propia de La Central o comprada en un banco de imágenes (cambia si se puede
  versionar).
- **Botones:** los seis requisitos de la §7 de `SISTEMA-VISUAL.md` son el requisito principal del
  trabajo. Modales: §8. Animación: §9.

---

## 3 · Fases

### Fase 0 · Inspección y plan · SIN código

1. `git status`: hay cambios sin versionar (`.codex/`, `AGENTS.md`, `Claude outputs/`). No se tocan
   ni se commitean (salvo copiar `rediseno-ui/` a `docs/diseno/` en la Fase 1).
2. Recorré `frontend/src/app` completo y `nucleo/api.ts`: qué endpoints consume hoy cada pantalla.
3. Recorré los controladores de `backend/LaCentral.Api` para saber qué existe y qué roles exige.
4. Revisá si el componente de listado de HU-UI-04 ya está en `develop`.
5. Armá esta tabla y **pará**:

| Pantalla / acción del prototipo | Referencia | Endpoint real | Roles | ¿Se implementa? | Observación |
|---|---|---|---|---|---|
| Acceso | 01 | … | … | … | … |
| Búsqueda de artículos (filtro, inactivos, orden, paginación) | 02, 12 | … | … | … | ¿la API pagina y ordena? ¿está HU-UI-04? |
| Ficha de artículo | 03, 07 | … | … | … | |
| Nuevo artículo | 04 | … | … | … | C4 |
| Editar artículo | 05 | … | … | … | C4 |
| Dar de baja / reactivar | 06, 07 | … | … | … | |
| Retirar código de proveedor | 08 | … | ADMINISTRADOR | … | |
| Factura de compra (proveedor, líneas, registro) | 09, 10, 11 | … | … | … | |

Sumale: las diferencias entre el prototipo y las reglas vigentes (C1, C4, D1 a D16 y las
respuestas de Sergio), los tests existentes que se van a ver afectados y las decisiones que
necesitás de Javi (como mínimo, las dos de la §2). Nada se implementa hasta que responda.

### Fase 1 · Fundamentos visuales

Material en `docs/diseno/` y línea en `CLAUDE.md`, tokens, tipografía, marca, fondo hexagonal y
componentes de base, con los botones y todos sus estados. Sin cambiar todavía la lógica de
ninguna pantalla.

### Fase 2 · Marco y acceso

Encabezado grafito con filete rojo, marca, menú desde `modulos.ts` (la marca del activo pasa a
ser una raya blanca debajo, conservando `aria-current="page"`), usuario y rol a la derecha con
**Salir**, pie. Pantalla de acceso con la tarjeta partida (formulario / panel de marca) sobre el
fondo hexagonal. Se conserva el comportamiento actual de `acceso.ts`, `sesion.ts` y las rutas.

### Fase 3 · Artículos

Sobre el componente de listado de HU-UI-04 (§1, regla 3): búsqueda, tabla de resultados, estado
vacío (12), ficha (03 y 07) y, **solo si el endpoint existe**, alta, edición, baja, reactivación y
retiro de código. Si la API no pagina ni ordena, no se simula paginando en el cliente: lo
planteás en el reporte de la Fase 0.

### Fase 4 · Factura de compra

Proveedor (búsqueda y elegido con «Cambiar»), datos del comprobante, percepciones (D8), líneas
con **destino por línea** (D9), agregar artículo, registro y pantalla de resultado (11), según lo
que devuelva la API.

### Fase 5 · Verificación y PR

Ver la sección 4. `verificador-tdd` sobre el diff de la rama (no hace falta `auditor-persistencia`:
no se toca backend). Cuerpo del PR en `docs/sprint04/pr-rediseno-ui.md`, contra `develop`, con: qué
se hizo, qué **no** se hizo y por qué, los CA verificados uno por uno y capturas comparadas con
cada referencia. Link de compare en el reporte. No mergees.

---

## 4 · Verificación · antes de declarar terminada cada fase

Desde `frontend/`:

```bash
npx ng build
npx ng test --watch=false --browsers=ChromeHeadless
```

Y desde `backend/`, `dotnet test`, para confirmar que el conteo no cambió.

- **Los tests que ya existían siguen en verde.** Si alguno falla porque cambió el marcado (por
  ejemplo, la raya del menú activo en `app.spec.ts`), se actualiza el test para que pruebe el
  mismo comportamiento con el nuevo marcado. Prohibido borrarlo o debilitarlo para que pase.
- Tests nuevos para lo que tiene comportamiento: botón en carga (deshabilitado, `aria-busy`, sin
  doble envío), modal (Escape, foco atrapado y devuelto), confirmación antes de lo destructivo,
  acciones ocultas según el rol.
- **CA4 de HU-UI-01** sigue cumpliéndose: sin desplazamiento horizontal a 1024, 768 y 480 px.
- La lista de la §12 de `SISTEMA-VISUAL.md` (capturas, teclado, movimiento reducido, sin
  hexadecimales fuera de `styles.css`).
- **Recorrido en el navegador:**
  - lecturas (login con los dos roles, búsqueda, ficha, proveedores) contra `lacentral_test`,
    **nunca** contra la base principal;
  - escrituras (alta, edición, baja, factura): el rol de prueba solo tiene lectura en `public`, así
    que se recorren contra `lacentral_demo` (compuerta C3). Si `lacentral_demo` no existe, ese
    recorrido va como **NO verificado** con ese motivo: no se busca otra base para escribir.

**Reporte al cierre de cada fase, con este formato:**

- **QUÉ HICE:** 1 a 3 líneas.
- **EVIDENCIA:** archivos modificados, comandos con su resultado, números de tests.
- **QUÉ FALTA:** lo que no se pudo verificar y por qué (obligatorio, aunque sea "nada").
- **PREGUNTAS:** las decisiones para Javi, una por línea.

No declares nada terminado porque "parece correcto".
