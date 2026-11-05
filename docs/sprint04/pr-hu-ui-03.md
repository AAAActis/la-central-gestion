## 041 · HU-UI-03 · Control de sesión, marco y acceso (con la Fase 2 del rediseño)

Rama `feature/hu-ui-03-control-de-sesion`, base `develop` (6a4a942, con la Fase 1 #135 y la
HU-UI-05 #134). La HU 041 y la Fase 2 van juntas por la adenda §2: son la misma pantalla.

Los CA salen de `docs/historias/041-HU-UI-03.md`, que queda en E4.

### Qué se hizo

- **Sesión en `sessionStorage`** (adenda §5). Sobrevive a F5 en la misma pestaña y muere al
  cerrarla. Salir la borra. Un valor ilegible cuenta como sin sesión. El token no se decodifica
  en el navegador: el rol es el que devuelve el login (decisión de B1).
- **Vencimiento (CA-002).** Un 401 con el token vigente cierra la sesión, y el acceso dice «Tu
  sesión venció. Volvé a entrar.». Un 403 no la cierra. Un 401 tardío de un token anterior no
  cierra la sesión nueva.
- **Marco.**
  - `diseno/marca.ts` montada por primera vez: compacta en el encabezado, completa en el acceso.
  - Filete rojo bajo el encabezado grafito, y usuario y rol con Salir (`oscuro`) a la derecha.
  - Pie en `--grafito-2`.
  - El activo conserva la raya blanca y `aria-current="page"`.
- **Acceso.**
  - Tarjeta partida sobre `.fondo-hex`, armada con `compartido/formulario/`: es su primer
    consumidor.
  - Ingresar es el único `.primario`.
  - Los errores van al aviso flotante como `{ texto }`, uno por intento.
  - Un 401 dice siempre «Usuario o contraseña incorrectos.», sin confirmar que el usuario existe.
  - Al abrirse, el foco queda en Usuario, también después de Salir o de un vencimiento.
- Manual (DoD 9): `docs/manual/acceso.md`, y `navegacion.md` corregido (F5 y la raya blanca).

### ⚠ Tres cambios en código ajeno ya mergeado · para revisar sabiendo que están

**1 · Extensión de `compartido/formulario/` (HU-UI-05, recién mergeado).** El acceso es el primer
uso real del componente, y el componente no lo soportó:
- `CampoFormulario.tipo` no admitía `'password'`, así que la contraseña se hubiera visto en claro;
- no había `autocomplete`, y sin `username` y `current-password` los gestores de contraseñas no
  completan el formulario: el operador escribe a mano todos los días.

Se agregaron `'password'` a la unión de `tipo` y `autocompletar?: string`, enlazado como
`[attr.autocomplete]`. Van en este PR y no en uno aparte por calendario: el sprint cierra el
30/10, y eran dos líneas. Son tres commits (`b19a5a4`, `039479d` y `4fa08e5`) y
cuatro tests en `formulario.spec.ts`:
- `type="password"`;
- el valor escrito no queda en el marcado;
- cada campo lleva su `autocomplete`;
- un campo sin `autocompletar` no lleva el atributo.

**Ojo:** `feature/hu-ui-06-confirmacion-con-motivo` también modifica `formulario.ts`. Cuando entre,
necesita un merge con esto.

**2 · `button.primario` en `styles.css`: hueco de la Fase 1 que este PR cierra.** La clase se
declaraba (en el formulario y en el acceso), pero ninguna regla la pintaba, en ninguna rama: el
botón se veía secundario. La regla sale de la §7: relleno `--rojo`, texto `--blanco` y `--rojo-osc`
al apoyar el cursor. La capa, el foco, la carga y el deshabilitado son los de la base. Va al final
de las variantes de botón, así que un conflicto con otro cambio en el archivo es trivial. La prueba
está en `compartido/botones.spec.ts`, que afirma diferencias entre variantes y no valores de color.

**3 · `button.peligro` sin texto blanco: otro hueco de la Fase 1, cerrado acá.** La regla solo
ponía el relleno, y el texto quedaba en `--tinta` sobre `--peligro`: **2,32:1, no cumple AA**. Se
agregaron `color: var(--blanco)` y el borde propio, con el mismo criterio que `.primario`.
`botones.spec.ts` ahora prueba **las cinco variantes de la §7, un test por variante**: cada una se
distingue de las otras cuatro, su texto contrasta AA con su relleno (calculado de los valores
calculados, sin afirmar colores) y lleva su propio color de texto. Sin esta última afirmación
sobrevivía una mutación: `peligro-borde` sin su texto `--rojo`. Es la familia de defectos de la
Fase 1 (variantes incompletas); el test la cierra para la próxima.

**Filete del encabezado y del panel de marca:** usa `--grosor-foco` (3 px), igual que la raya del
ítem activo. Corresponde un token propio, `--filete`, cuando llegue `01-acceso.png` y se pueda
medir. No se cambió a ciegas.

### Criterios de aceptación

| CA | Estado | Prueba |
|---|---|---|
| CA-001 · el token viaja solo | Verificado (caracterización: ya se cumplía desde B1) | `CA-001 · con sesión activa, toda llamada…`, `CA-001 · el login no lleva token` |
| CA-002 · sesión vencida → al ingreso, informando | Verificado | `CA-002 · un 401 con la sesión vigente…`, `un 403 no cierra…`, `el 401 de un token anterior…`, `CA-002 · si la API rechaza el token vencido…`, `CA-002 · si la sesión anterior venció…` |
| CA-003 · sin sesión, una URL interna lleva al ingreso | Verificado (caracterización) | `CA-003 · sin sesión, escribir la dirección…` |
| CA-004 · Salir descarta el token y volver atrás no deja operar | Verificado | `CA-004 · Salir descarta el token…`, `CA-004 · después de Salir, las llamadas…`, `CA-004 · después de Salir, volver a la pantalla anterior…`; en Chrome, `history.back()` muestra el acceso |

**Qué entrega este PR contra criterios de aceptación, y qué no.** De los cuatro CA de la
proforma, este PR entrega dos: **CA-002 y CA-004**. **CA-001 y CA-003 son caracterización**: ya se
cumplían desde B1 (#130), y acá solo quedan cubiertos por tests. Además, **todo el trabajo visual
de la Fase 2** (marco, marca, tarjeta partida, fondo hexagonal) **no responde a ningún criterio de
aceptación**: viene del prompt del rediseño (`docs/sprint04/PROMPT-REDISENO-UI.md` §3, Fase 2), no
de una HU. Su único criterio es la comparación con las referencias aprobadas, y esa comparación
está **NO verificada**, porque `docs/diseno/referencias/` no existe en el repo: no está
`01-acceso.png` ni ninguna de las otras doce capturas, y tampoco `docs/diseno/prototipo/`. Lo
visual de este PR está construido sobre `SISTEMA-VISUAL.md` y sin contrastar con su referencia.

### Verificación

- `npx ng build`: OK.
- `npx ng test --watch=false --browsers=ChromeHeadless`: **88/88** (48 previos + 40 nuevos).
- **Mutaciones:** 41 distintas, sobre `sesion.ts`, `login.ts`, `app.ts`, `formulario.ts` y las reglas
  de las variantes de botón. Todas dejan al menos un test en rojo.
- `dotnet test`: **436 en total; 288 verdes y 148 en rojo por `Timeout during connection attempt`**
  contra el PostgreSQL de Tailscale. No hubo `53300`. Son exactamente los 148 de PostgreSQL, y este
  PR no toca `backend/`.
- **Evidencia en `docs/sprint04/evidencia/hu-ui-03/`** (`capturas-cdp.mjs`, `mediciones-cdp.txt`):
  - 13 capturas a 1024, 768 y 480 px, sin desplazamiento horizontal;
  - F5 conserva la ruta y la sesión, y `localStorage` queda vacío;
  - recorrido con teclado; Enter en Salir deja el foco en Usuario;
  - con movimiento reducido, sin transiciones ni animación;
  - `grep` de hexadecimales fuera de `styles.css`: ninguno.
- **Bug encontrado por la evidencia, no por los tests:** una referencia `#usuario` tapaba al
  `ngModel`, y el campo mostraba `[object HTMLInputElement]`. Ciclo propio: test en rojo
  (`un ingreso envía lo que el usuario escribió…`) y el arreglo en `d14a0bd`.

### Qué NO se hizo, y por qué

- **Acceso y marco contra su referencia: NO verificado.** `docs/diseno/referencias/` no está en el
  repo (los sube Javi). Quedaron, a confirmar: el filete abajo, el pie «La Central Repuestos» y el
  título «Acceso al sistema».
- **Fondo hexagonal: NO verificado.** `public/fondo-hex.jpg` no está. Se ve el respaldo
  `--fondo-hex-respaldo`.
- **DoD 10: NO verificada.** `lacentral_demo` (C3) sigue pendiente, y el recorrido fue con la API
  interceptada.
- **Aviso flotante: registrado como deuda, no arreglado.** Pierde el foco al cerrarse y no se cierra
  con Escape. Hay propuesta de agregado a la §8 en `ESTADO.md`.
- **Deuda de seguridad (insumo del RNF):** cookie `httpOnly` con refresco y CSRF en vez de
  `sessionStorage`. Se postergó porque exige tocar el backend. Detalle en `ESTADO.md`.
- La suite de frontend corre solo en Chromium.

**Issue de la 041:** no figura entre los creados uno por HU (`ESTADO.md`, Issues de GitHub). Falta crearlo, con el milestone Sprint 4.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
