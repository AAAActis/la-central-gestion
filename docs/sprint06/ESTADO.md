# ESTADO · Sprint 6

**Entregable:** al cerrar este sprint, una persona puede entrar al sistema, cargar la lista de
precios de un proveedor desde un archivo, ver los artículos propios que el sistema le propone
para cada renglón y vincularlos en un clic —y desde ahí el código del proveedor encuentra el
artículo para siempre—, sobre `lacentral_demo`.

> Única fuente de verdad del estado del sprint. Se actualiza **antes** de cada push, en el
> mismo PR que el código que describe. Escrito el 05/11/2026, al rehacer el tablero (S5-08 del
> `ESTADO.md` del Sprint 5).

**Sprint 6 · 16/11/2026 → 27/11/2026** (fechas estimadas: sprint de dos semanas después del 5).
Comparador, etapa 1, sobre datos propios (D16). La comparación propiamente dicha (030 y 032)
es el Sprint 7: vincular ya le sirve al dueño aunque todavía no compare (opción a, Santi,
05/11/2026).

---

## Tablero · Sprint 6 (milestone «Sprint 6», cierra el 27/11/2026)

El tablero y esta tabla son la misma verdad. Si uno cambia, el otro cambia en el mismo push.

| Issue | HU | SP | Asignado | La bloquea | Nota |
|---|---|---|---|---|---|
| #128 | 055 · HU-CMP-08 · lista de precios de un proveedor desde archivo | 5 | Javi | C2 | De dónde sale «lo que cuesta» |
| #126 | 053 · HU-CMP-06 · candidatos por aproximación entre catálogos | 8 | Santi | C2 · C5 · C6 | Sin proforma: C5 es escribirla |
| #127 | 054 · HU-CMP-07 · vinculación manual con aprendizaje | 8 | Santi | C2 · C5 · la 053 | Sin proforma: C5 es escribirla |
| #52 | 050 · HU-UI-12 · pantalla del comparador y de vinculación | 8 | Javi | la 053 | En este sprint, solo la parte de vinculación; la comparación llega con la 030 y la 032 |

29 SP: Santi 16, Javi 13.

## Compuertas · cada una bloquea solo las HU de su fila (D17)

| # | Qué | Responsable | Bloquea | Estado al 05/11 |
|---|---|---|---|---|
| C2 | Esquema `proveedor_articulo` (script SQL + re-scaffold) | Santi | 055, 053, 054 | Abierta |
| C5 | Escribir las proformas de la 053 y la 054 | Santi | 053, 054 | Abierta |
| C6 | Medición sobre 50 artículos | Javi | 053 | Abierta |

**Una HU no se empieza hasta que su compuerta esté cerrada**, aunque esté en el milestone.

## Fuera del Sprint 6

- **Sprint 7 (milestone «Sprint 7», cierra el 11/12/2026):** #16 · 030 · HU-CMP-01, búsqueda
  comparativa (Javi), y #19 · 032 · HU-CMP-03, vista Compra (Santi). La 032 espera a C1 (L2 y
  retiro de Tarjeta3/6) y a la 016.
- **CMP-02 (#18), consulta en vivo a los sitios:** afuera. Es etapa 2 (D16), con un proveedor
  como prueba de concepto.
- **Ventas** (#26, #28, #29, #31) y la pantalla de login (#41): sin milestone (backlog). Son otro
  entregable.

---

## PRÓXIMA SESIÓN

El sprint todavía no empezó. Antes de la primera HU: cerrar C2, C5 y C6 (tabla de arriba).
