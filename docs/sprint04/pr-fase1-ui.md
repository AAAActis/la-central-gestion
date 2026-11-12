## Rediseño visual · Fase 1 · fundamentos

Rama `feature/hu-ui-fase1-fundamentos-visuales`. **Base: `docs/proformas-y-sistema-visual`**, que
trae `SISTEMA-VISUAL.md`, el prompt y la adenda del rediseño. Cuando esa base entre en `develop`,
GitHub reapunta este PR solo.

Material: `docs/diseno/SISTEMA-VISUAL.md` y `docs/sprint04/ADENDA-REDISENO-UI.md`, que manda sobre
el prompt. La Fase 1 no es una HU: pone los cimientos que usan HU-UI-05 en adelante. No crea
pantallas.

### Qué se hizo

- **Tokens** de la §2 a la §4 y la §9 en `frontend/src/styles.css`: color, escala tipográfica,
  espaciado, radios y duraciones. Tres nuevos que la §11 exige y el documento no definía:
  `--peligro`, `--peligro-osc` y `--error-texto`. Cinco sin nombre que entran a la §2:
  `--borde-oscuro`, `--velo`, `--fondo-hex-respaldo`, `--grosor-foco` y `--dur-giro`.
- **Tipografía autoalojada** en `public/fonts/`: Barlow Semi Condensed, Noto Sans y JetBrains
  Mono, subconjunto latin, licencia OFL al lado de cada archivo, con la pila de respaldo de la §3.
- **Marca provisoria** en `diseno/marca.ts`, variantes completa y compacta. No está montada: la
  monta la Fase 2.
- **Clases globales de base** (adenda §4): avisos, chip de código, etiqueta de estado, tarjeta,
  tabla y fondo hexagonal.
- **Botones con los seis requisitos de la §7.** Un `button` sin clase es secundario; el primario
  se declara con `.primario`. Hasta las Fases 3 y 4, artículos, factura y acceso no tienen
  primario.
- **`prefers-reduced-motion`** global: la capa del botón tarda 0,3 s, y 0 s con movimiento
  reducido.
- **Componentes de `compartido/`:**
  - botón en carga: deshabilitado mientras dura la operación, sin doble envío;
  - modal: `<dialog>` con `showModal` (el resto queda inerte), Escape cierra, el navegador
    devuelve el foco;
  - aviso flotante: `role=status` (`alert` si es error). Éxito, información y alerta se van a los
    5 s (`--dur-aviso`); **el error queda hasta que el usuario lo cierra**. Un aviso nuevo
    reemplaza al anterior, y uno que repite el texto se vuelve a anunciar.

### Migración de tokens viejos a nuevos

| Token viejo | Dónde se usaba | Token nuevo |
|---|---|---|
| `--fondo` | fondo de la página | `--fondo` (valor nuevo) |
| `--tarjeta` | fondo de la tarjeta | `--superficie` |
| `--texto` | texto base · fondo del encabezado | `--tinta` · `--grafito` |
| `--tenue` | encabezados de tabla, filas inactivas, `.tenue`, `.vacio` | `--texto-2` |
| `--tenue` | `.etiqueta` | `--deshab-fondo` y `--deshab-texto` |
| `--borde` | tarjeta, `fieldset`, celdas de tabla | `--linea` |
| `--borde` | `input`, `select`, botón secundario | `--linea-2` |
| `--acento` | fondo del botón primario | `--rojo` (`.primario`) |
| `--acento` | contorno de foco | `--rojo-foco`, con `--grosor-foco` |
| `--error` | `.error` | `--error-texto` sobre `--rojo-tinte` |
| `--aviso` | `.razon` | `--aviso-texto` |
| `--senal` | raya del menú activo | `--blanco` |

Ningún componente fuera de `styles.css` usaba los tokens viejos, así que la migración no toca
otros archivos. CA1 de `app.spec.ts` se reescribió para afirmar que el activo se distingue del
resto, no de qué color es: es la regla nueva de `CLAUDE.md`.

### Verificación

- `npx ng test --watch=false --browsers=ChromeHeadless`: **35/35**. Corre solo en Chromium: lo que
  depende del motor, como que `<dialog>` devuelva el foco, no está probado en Firefox ni en
  Safari.
- Componentes de `compartido/` con el ciclo de tres commits y mutaciones sin commitear, cada una
  en rojo. El detalle está en los mensajes de commit.
- El backend no se tocó y no se corrió.

**Fuentes, medidas por CDP** (`docs/sprint04/evidencia/fase1/mediciones-cdp.txt`):
- las 3 familias en sus 8 pesos y estilos: `document.fonts.load()` da `loaded` y
  `document.fonts.check()` da `true`;
- un chip de código de ejemplo resuelve a JetBrains Mono;
- control negativo: una cara que apunta a un archivo inexistente da `error` y `check() = false`;
- los archivos llegan a `dist/`.

**Desplazamiento horizontal, en las 9 combinaciones** (1024, 768 y 480 px × acceso, artículos y
factura de compra): en ninguna el ancho del documento supera el de la ventana. Capturas y estados
del botón (reposo, cursor encima, foco) en `docs/sprint04/evidencia/fase1/`.

### Decisiones (28/10/2026)

- `--dur-giro` 800 ms; `--dur-aviso` 5 s, salvo el error, que no se va solo.
- Detrás del modal va el velo, no el panal. La ambigüedad de la §5 queda marcada en el documento.
- Un spec no afirma valores de color (regla nueva en `CLAUDE.md`).

### NO verificado

- **Comparación con el prototipo y las 13 referencias**, incluida la forma de la marca. Motivo:
  las referencias todavía no están subidas.
- **`public/fondo-hex.jpg`**: la imagen no está en el repositorio. Hoy el fondo hexagonal muestra
  el color de respaldo (`--fondo-hex-respaldo`).

🤖 Generated with [Claude Code](https://claude.com/claude-code)
