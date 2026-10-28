# Sistema visual · La Central

> **Regla permanente del frontend.** Toda pantalla, componente o cambio visual de
> `frontend/` se hace con este sistema: colores, tipografía, espaciado, fondos, íconos,
> estados y animaciones. No se inventan valores nuevos.
>
> - **Fuente de verdad visual:** las 13 capturas de `docs/diseno/referencias/`.
> - **Origen de los valores:** el bloque `:root` de `docs/diseno/prototipo/index.html`.
> - **Implementación:** los tokens viven en `frontend/src/styles.css`. Un componente usa
>   `var(--token)`, **nunca** un color, tamaño o duración escrito a mano.
> - **Cambiar este documento** (un token nuevo, otra variante de botón) lo aprueba Javi
>   antes de implementarlo, y se cambia en el mismo commit que el código.

---

## 1 · Principios · son requisito, no gusto

El sistema que reemplazamos es *"una cosa llena de botones por todos lados"*, y el personal
aprende en el momento porque no hay manuales. Cada pantalla tiene que poder usarse sin que
nadie la explique:

1. **Una sola acción principal por pantalla.** Un solo botón `primario` visible a la vez.
   El resto, `secundario` o en un menú.
2. **Lo importante, primero y grande.** El dato que decide (precio, existencia, estado)
   se lee sin buscarlo.
3. **Cada dato con su fecha** cuando puede estar viejo (costos, precios de proveedor).
4. **Lo seguro separado de lo probable.** Coincidencias exactas arriba, aproximaciones abajo.
5. **Nunca un cero que no es cero.** Sin dato es "Sin costo" o el motivo que manda la API,
   nunca `$ 0,00`. Un vacío es un estado vacío con mensaje, nunca una tabla vacía.
6. **Lo destructivo pide confirmación** en un modal propio, nunca `window.confirm`.
7. **El frontend no decide reglas de negocio.** Muestra lo que devuelve la API. Ocultar una
   acción según el rol no es autorizar: el backend sigue mandando.

---

## 2 · Color · tema claro únicamente

| Token | Valor | Uso |
|---|---|---|
| `--rojo-marca` | `#e82217` | **Solo la marca**: la «C» del logo y piezas de identidad. Es el rojo de la imagen de marca de La Central. |
| `--rojo` | `#c4211b` | Botón primario, filete del encabezado, acento, enlaces |
| `--rojo-osc` | `#931510` | Relleno del primario al apoyar el cursor |
| `--rojo-tinte` | `#fbebe9` | Relleno de botones blancos al apoyar el cursor, fondo de aviso de error |
| `--rojo-borde` | `#e3a6a2` | Borde de `peligro-borde` y foco de botones blancos |
| `--rojo-foco` | `rgba(196,33,27,.32)` | Contorno de foco (3 px) |
| `--peligro` | `#a91c16` | Botón `peligro` |
| `--peligro-osc` | `#7c110d` | Relleno del botón `peligro` al apoyar el cursor |
| `--error-texto` | `#7d1410` | Texto del aviso de error |
| `--tinta` | `#1c1c1f` | Texto principal |
| `--texto-2` | `#5b5c62` | Texto secundario, íconos en reposo |
| `--texto-3` | `#7b7c83` | Texto terciario, ayudas |
| `--grafito` | `#2a2a2e` | Encabezado |
| `--grafito-2` | `#1f1f22` | Pie, superficies oscuras |
| `--fondo` | `#f2f2f3` | Fondo de la aplicación |
| `--superficie` | `#ffffff` | Tarjetas, modales, tablas |
| `--superficie-2` | `#f7f7f8` | Filas alternas, zonas secundarias |
| `--chip` | `#f0f0f2` | Chips de código, hover de botón-ícono, aviso info |
| `--linea` | `#e3e3e6` | Bordes y separadores |
| `--linea-2` | `#c9c9cf` | Bordes de campos y botón secundario |
| `--aviso-fondo` / `--aviso-texto` / `--aviso-chip` | `#fcf4e7` / `#6b4a0e` / `#f8ead2` | Alertas, dato vencido |
| `--ok-fondo` / `--ok-texto` | `#e8f3ec` / `#1e6a3b` | Éxito, estado Activo |
| `--deshab-fondo` / `--deshab-texto` | `#e7e7ea` / `#8b8c92` | Deshabilitado |
| `--blanco` | `#ffffff` | Texto sobre rojo y grafito |
| `--sombra` | `0 18px 48px rgba(0,0,0,.35)` | Solo modales |
| `--borde-oscuro` | `rgba(255,255,255,.32)` | Borde del botón `oscuro` (sale de la §7) |
| `--velo` | `rgba(14,14,16,.12)` | Velo detrás del modal (sale de la §5) |
| `--fondo-hex-respaldo` | `#2b2b2e` | Color de respaldo del fondo hexagonal (sale de la §5) |
| `--grosor-foco` | `3px` | Grosor del contorno de foco (sale de la §7.3) |
| `--dur-giro` | `800ms` | Una vuelta del indicador de carga (sale de la §9; valor decidido el 28/10/2026) |

Reglas:
- **Dos rojos, y no se mezclan.** `--rojo-marca` (`#e82217`) es el de la identidad y va solo
  en el logo. Para la interfaz se usa `--rojo` (`#c4211b`): con texto blanco encima da un
  contraste de ~5,9:1 (cumple AA), mientras que el rojo de marca da ~4,5:1 y queda justo en el
  límite para texto chico. Por eso un botón nunca usa `--rojo-marca`.
- **Rojo = marca y acción**, no decoración. Para errores se usa el aviso de error
  (`--rojo-tinte` con texto `--error-texto`), no texto rojo suelto.
- Un valor negativo (por ejemplo, stock) se marca con el estilo de error, no con otro rojo.
- No hay tema oscuro. `color-scheme: light`.

---

## 3 · Tipografía

| Token | Familia | Uso |
|---|---|---|
| `--f-titulo` | *Barlow Semi Condensed*, `'Arial Narrow', Arial, sans-serif` | Títulos `h1`–`h3`, 700 |
| `--f-texto` | *Noto Sans*, `system-ui, 'Segoe UI', Roboto, Arial, sans-serif` | Todo el texto |
| `--f-mono` | *JetBrains Mono*, `ui-monospace, Consolas, 'Courier New', monospace` | Códigos (interno, de proveedor), números de comprobante |

Pesos que existen: Barlow 500, 600, 700 y 800 itálica (solo la marca); Noto Sans 400, 500, 600;
JetBrains Mono 400, 500. **No se usan otros.**

| Elemento | Tamaño | Notas |
|---|---|---|
| Texto base (`body`) | 14 px / 1.5 | `-webkit-font-smoothing: antialiased` |
| `h1` | 30 px / 1.15 | Uno por pantalla |
| `h2` | 19 px (17 px dentro de tarjeta) | |
| Tabla | 13.5 px | |
| Etiqueta de campo, aviso | 13 px | |
| Ayuda, metadato, fecha | 12.5 px | |
| Chip, etiqueta de estado | 11.5–12 px | |
| Título de la pantalla de acceso | `clamp(34px, 4.3vw, 52px)` | Solo ahí |

Títulos con `letter-spacing: .005em` y `text-wrap: balance`. «REPUESTOS» de la marca con
`letter-spacing: .32em`. Las fuentes se cargan como se decidió en la Fase 1 del rediseño
(ver `docs/sprint04/ESTADO.md`), **siempre** con la pila de respaldo.

**Números:** montos con `Intl.NumberFormat('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })`
(el pipe `number` de Angular con locale `es-AR` es equivalente). El frontend formatea, no
redondea por su cuenta ni recalcula.

---

## 4 · Espaciado, bordes y layout

- **Escala de espacios:** 6, 8, 10, 12, 14, 16, 18, 20, 24 px. Lo más común: `gap: 12px`
  entre elementos, 8 px entre etiqueta y campo, 16–20 px entre bloques.
- **Radios:** 2–3 px en botones, campos, chips y avisos; 4 px en tarjetas; 5 px en modales.
  Nada redondeado más que eso, salvo indicadores circulares (`50%`).
- **Tarjeta:** cabecera `18px 22px`, cuerpo `22px 24px`, pie `14px 22px`, separadas por
  `1px solid var(--linea)`.
- **Ancho máximo del contenido:** 1440 px; formularios y fichas, ~1020 px.
- **Cortes:** 980 px y 760 px. **Ninguna pantalla tiene desplazamiento horizontal a
  1024, 768 ni 480 px** (CA4 de HU-UI-01). Una tabla ancha puede desplazarse **dentro de su
  caja** (`.tabla-caja { overflow-x: auto }`), nunca la página.

---

## 5 · Fondos

- **Aplicación:** `--fondo` liso.
- **Hexagonal:** solo en la pantalla de acceso y detrás de los modales. Es el panal de la
  imagen de marca de La Central: hexágonos en relieve, grises oscuros (de `#131313` a `#4e4c52`),
  con luz que aclara hacia abajo. Imagen estática `public/fondo-hex.jpg` (1600×1100, JPEG,
  < 250 KB) con color de respaldo `#2b2b2e`. **No** se dibuja con canvas ni JavaScript.
  Sobre el panal, el texto va en blanco y con peso alto, como en la imagen de marca.
- Detrás del modal: velo `--velo`, sobre la pantalla, que sigue a la vista.
  **Ambigüedad, a confirmar contra la captura de referencia:** el punto anterior dice que el
  panal va "detrás de los modales", y este define un velo de 0,12 que deja ver la pantalla.
  Se implementó el velo (28/10/2026): reemplazar el contenido por el panal mientras hay un
  modal abierto le saca al usuario el contexto que necesita para decidir.
- No se agregan degradados, imágenes ni texturas que no estén en las referencias.

---

## 6 · Íconos y marca

- **Íconos:** SVG en línea, `viewBox="0 0 24 24"`, `fill="none"`, `stroke="currentColor"`,
  `stroke-width="1.8"`, puntas y uniones redondeadas, `aria-hidden="true"` y
  `focusable="false"`. Toman el color del texto. **Sin librerías de íconos.**
- Un botón que es solo ícono lleva `aria-label`.
- **Marca:** la identidad de La Central es «REPUESTOS» arriba y «LA CENTRAL» abajo, en
  blanco, letra pesada e itálica, con la «C» de CENTRAL como un arco rojo (`--rojo-marca`).
  Va en un solo componente, `diseno/marca.ts`, con dos variantes: completa (pantalla de acceso)
  y compacta (encabezado). No se redibuja ni se recolorea fuera de ese componente.
- **El logo es provisorio:** el del prototipo se va a reemplazar. Como vive en un único
  componente, cambiarlo es tocar `diseno/marca.ts` y nada más. Ninguna pantalla dibuja su
  propia versión del logo.

---

## 7 · Botones

Alto 40 px (`chico`: 32 px, `padding: 0 12px`, 13 px). Un `button` **sin clase es
`secundario`**: el `primario` se declara con la clase `.primario` (§1, una sola acción
principal). Variantes:

| Variante | Reposo | Relleno al apoyar el cursor |
|---|---|---|
| `primario` | `--rojo`, texto blanco | `--rojo-osc` |
| `peligro` | `--peligro`, texto blanco | `--peligro-osc` |
| `secundario` | `--superficie`, borde `--linea-2`, texto `--tinta` | `--rojo-tinte` |
| `peligro-borde` | `--superficie`, borde `--rojo-borde`, texto `--rojo` | `--rojo-tinte` |
| `oscuro` (sobre grafito) | transparente, borde `--borde-oscuro`, texto blanco | `--rojo` |

Comportamiento, igual en todas:
1. Al apoyar el cursor, una capa (`::before`) entra **desde el borde superior** y llena el
   botón: `transform: scaleY(0 → 1)`, `transform-origin: top`, `var(--dur-relleno)` (300 ms),
   `var(--ease)` (`cubic-bezier(.4,0,.2,1)`). Al salir, se retrae hacia arriba.
2. Texto e ícono siempre encima: `isolation: isolate` en el botón, `z-index: -1` en la capa.
   El color del texto no cambia durante la transición.
3. `:focus-visible`: la misma capa más `outline: var(--grosor-foco) solid var(--rojo-foco); outline-offset: 2px`.
4. Al presionar: se oscurece, **sin moverse ni cambiar de tamaño**.
5. **Cargando:** conserva el ancho, indicador giratorio (`@keyframes girar`), texto
   "Guardando…" (o el que corresponda), `disabled`, `aria-busy="true"`, sin doble envío.
6. **Deshabilitado:** `--deshab-fondo` / `--deshab-texto`, sin capa, `cursor: not-allowed`.

---

## 8 · Componentes de base

Se usan los que existen en `compartido/` o las clases globales de `styles.css`. Antes de
crear uno nuevo, buscá si ya está.

| Componente | Reglas |
|---|---|
| Campo | Etiqueta arriba (13 px), 8 px de separación, ayuda debajo en `--texto-3`, error debajo como aviso de error. Etiqueta asociada con `for`/`id`. |
| Tarjeta | §4. Cabecera con título y, como mucho, una acción. |
| Tabla | 13.5 px, encabezado fijo dentro de la caja, filas alternas con `--superficie-2`. |
| Aviso | `info` (`--chip`), `alerta` (`--aviso-*`), `error` (`--rojo-tinte` / `--error-texto`), `ok` (`--ok-fondo`). Padding `12px 16px`, radio 3 px, con ícono. |
| Etiqueta de estado | Activo (`--ok-*`) / Inactivo (`--deshab-*`). Texto, no solo color. |
| Chip de código | `--f-mono`, fondo `--chip`. |
| Estado vacío | Ícono, una frase que diga qué pasó y, si aplica, una acción. Nunca una tabla vacía. |
| Modal | Ancho por defecto 560 px, cabecera `22px 26px 20px`, cuerpo `24px 26px`. Escape cierra, el foco queda atrapado adentro y vuelve al botón que lo abrió. |
| Aviso flotante (toast) | Ancho `min(420px, calc(100vw - 40px))`, aparece con `subir` y se puede cerrar. Éxito, información y alerta se van solos a los `--dur-aviso`; **un error no se va solo**, queda hasta que el usuario lo cierra (el operador puede estar atendiendo a un cliente). **Un aviso nuevo reemplaza al anterior, no se suma**: tres intentos fallidos dejan un solo cartel, el último, y el aviso nuevo dura su tiempo completo. |

---

## 9 · Animación

| Qué | Duración | Detalle |
|---|---|---|
| Relleno de botón | 300 ms | `var(--ease)`, §7 |
| Modal | 200–220 ms | opacidad + `translateY(10px) scale(.985) → none` |
| Contenido que aparece | `aparecer` | opacidad + `translateY(8px) → none` |
| Toast | `subir` | opacidad + `translateY(10px) → none` |
| Toast a la vista | `--dur-aviso` (5 s) | Éxito, información y alerta. El error no se va solo (§8). |
| Carga | `girar`, `--dur-giro` (800 ms) | `rotate(360deg)` en bucle |

- **`prefers-reduced-motion: reduce`** elimina transiciones y el giro. Es obligatorio en todo
  componente con movimiento.
- No hay otras animaciones: nada de rebotes, parallax ni transiciones de página.

---

## 10 · Accesibilidad mínima

- Contraste AA en texto. El rojo y el grafito sobre blanco, y el blanco sobre ellos, cumplen.
- Todo lo que se hace con el mouse se hace con el teclado, con foco visible.
- El ítem activo del menú lleva `aria-current="page"`.
- Los errores y avisos se anuncian (`role="alert"` o `aria-live`), no solo se pintan.
- El estado no se comunica solo con color: va con texto o ícono.

---

## 11 · Lo que NO se hace

- Librerías de UI, CSS o íconos: Angular Material, Tailwind, Bootstrap, Font Awesome, etc.
- Colores, tamaños o duraciones fuera de estos tokens.
- Copiar del prototipo su JavaScript, sus datos o sus reglas de negocio.
- Recalcular o redondear precios y stock en el cliente.
- Más de un botón `primario` visible a la vez.

---

## 12 · Cómo se verifica

Al cerrar una pantalla:
- captura al lado de su referencia de `docs/diseno/referencias/`;
- sin desplazamiento horizontal a 1024, 768 y 480 px;
- recorrida con teclado (Tab, Escape en modales);
- con movimiento reducido, sin transiciones;
- `grep` de colores hexadecimales en los componentes nuevos: no debería haber ninguno fuera
  de `styles.css`.
