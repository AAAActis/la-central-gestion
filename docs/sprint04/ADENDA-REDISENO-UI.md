# Adenda al rediseño visual · Sprint 4 · 28/10/2026

Se lee **junto con** `docs/sprint04/PROMPT-REDISENO-UI.md`. Donde los dos difieran, manda
esta adenda. Lo visual lo sigue mandando `docs/diseno/SISTEMA-VISUAL.md`.

---

## 1 · Por qué existe

El prompt del rediseño y el replan del Sprint 4 son dos planes distintos para los mismos
tres días hábiles que quedan (miércoles 28, jueves 29, viernes 30). El rediseño rehace
pantallas que ya existen; el replan construye pantallas que no existen. Los dos juntos no
entran.

Además se pisan en dos puntos: la Fase 2 y la HU 041 rehacen las dos la pantalla de acceso,
y la Fase 3 depende de un componente (HU-UI-04) que todavía no está en `develop`.

---

## 2 · Qué entra esta semana y qué no

**Entra:**

- **Fase 1 · fundamentos visuales.** No compite con las HU nuevas: es su prerrequisito. Con
  los tokens, la tipografía, la marca, el fondo y los botones ya en `styles.css`, las HU 042
  y 043 nacen en el sistema nuevo en vez de construirse y rehacerse.
- **Fase 2 · marco y acceso, fusionada con la HU 041.** Son la misma pantalla: la lógica de
  sesión y su aspecto se hacen en la misma rama o se pisan. Queda en una sola mano.

**Pasa al Sprint 5:**

- **Fase 3 · artículos** y **Fase 4 · factura de compra.** Las dos pantallas ya funcionan;
  rediseñarlas no agrega nada que el dueño pueda probar, y el objetivo del sprint es
  cobertura, no terminación. Además sus escrituras solo se verifican contra `lacentral_demo`
  (compuerta C3), que no existe todavía: esta semana quedarían como NO verificado de todos
  modos.
- **Fase 5 · PR** del rediseño completo. Esta semana el PR es de la Fase 1 + 041/Fase 2.

**Sigue vigente del replan:** 042 listado, 043 formulario, 044 confirmación y 045 clientes y
proveedores, en ese orden y con la secuencia por día del replan.

---

## 3 · Reparto de los tres días

| | Javi | Lauti | Santi |
|---|---|---|---|
| **Mié 28** | Fase 1 · fundamentos visuales | HU 041 + Fase 2 (acceso y marco) | merge de #130 y #131, corrección del `ESTADO.md`, commit de `docs/historias/` y `docs/diseno/` |
| **Jue 29** | HU 042 listado, sobre el sistema nuevo | HU 045 clientes y proveedores (listado) | revisión de PR |
| **Vie 30** | HU 043 formulario + HU 044 | HU 045 (formulario) e integración | cierre, evidencia y declaración del sprint |

La Fase 1 se mergea el miércoles aunque sea al final del día: 042 y 045 dependen de ella.

**Si algo se atrasa, se cae en este orden**, y la decisión se registra el viernes en el
`ESTADO.md` con su motivo: primero 044, después la parte de formulario de 045, después 043.
No se caen la Fase 1 ni 041: sin la primera todo lo que venga después se rehace, y sin la
segunda no hay demo.

---

## 4 · Decisiones cerradas · la Fase 0 ya no las pregunta

**Tipografía: autoalojada en `public/fonts/`.** Es un mostrador de repuestos: si la conexión
se cae o un tercero tarda, el sistema tiene que verse igual. Con la pila de respaldo de la §3
de `SISTEMA-VISUAL.md`.

**Componentes de base: clases globales en `styles.css`** para lo puramente visual (avisos,
chips, etiquetas de estado, tarjeta, tabla) y **componentes en `compartido/`** solo para lo
que tiene comportamiento (modal, toast, botón en carga). El criterio es el que pide el prompt
—lo más simple de defender— y la razón es que un componente Angular para pintar un chip
agrega una capa que no resuelve nada.

**Tres tokens que faltan.** La §11 de `SISTEMA-VISUAL.md` prohíbe colores fuera de los
tokens y la §12 hace `grep` de hexadecimales en los componentes, pero la §7 define la
variante `peligro` como `#a91c16` / `#7c110d` y la §8 define el texto del aviso de error como
`#7d1410`: los tres sin token. El documento se contradice con su propia verificación. En el
mismo commit que los demás tokens se agregan:

```
--peligro:       #a91c16
--peligro-osc:   #7c110d
--error-texto:   #7d1410
```

Y la §7 y la §8 de `SISTEMA-VISUAL.md` se corrigen para nombrarlos, no para repetir el hex.

---

## 5 · La sesión · cambio respecto del prompt

El prompt dice que la sesión sigue solo en memoria y que la deuda del F5 no se resuelve en
este trabajo. **Se resuelve, dentro de la HU 041.**

El motivo no es técnico, es de objetivo: `sesion.ts` guarda el token solo en memoria, así que
un F5 desloguea. El dueño va a apretar F5 en la demo y va a concluir que el sistema está
roto. La demo es el objetivo del sprint; no se puede entregar una sesión que no sobrevive un
refresco.

**Decisión: `sessionStorage`.** Sobrevive el F5 en la misma pestaña, muere al cerrarla, no
requiere tocar el backend y son pocas líneas.

**Deuda registrada, con su justificación:** lo riguroso es una cookie `httpOnly` con endpoint
de refresh y protección CSRF, porque un token en `sessionStorage` es legible por JavaScript y
por lo tanto expuesto a XSS. Se eligió `sessionStorage` por restricción de tiempo y porque el
sistema es de uso interno. Queda como insumo del RNF de seguridad: la decisión, su
alternativa y el motivo de haberla postergado.

---

## 6 · Prerrequisitos antes de la Fase 1

**Chromium.** La verificación de cada fase corre
`npx ng test --watch=false --browsers=ChromeHeadless`, y en la máquina de desarrollo hay solo
Firefox. Sin Chromium instalado y `CHROME_BIN` definido, toda fase cierra con su verificación
de frontend como NO verificada.

**`docs/historias/`.** Las 52 proformas tienen que estar commiteadas antes de que el agente
toque una HU, y la regla de `CLAUDE.md` tiene que decir que los CA se leen de ahí y no se
inventan ni se copian del cuerpo del Issue.

**`.gitignore`.** Después de copiar `Claude outputs/rediseno-ui/` a `docs/diseno/`, agregar
`Claude outputs/` al `.gitignore` de forma explícita. El nombre tiene un espacio y está en la
raíz del repo.

---

## 7 · Deuda menor detectada al revisar

- `docs/PROMPT-FASE.md` referencia `docs/sprint03/`. El prompt del rediseño lo parchea con
  una instrucción al agente ("usá `docs/sprint04/`"), que funciona pero deja el documento
  mintiendo. Corregirlo es una línea.
- `frontend/src/app/README.md` anuncia `compartido/`, que no existe. Lo crea la Fase 1 o la
  HU 042; hasta entonces el README describe algo que no está. Fue el hallazgo N1 del PR #130
  y sigue abierto.
