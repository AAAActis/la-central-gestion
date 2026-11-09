# ESTADO · Sprint 6

**Entregable:** al cerrar este sprint, una persona puede entrar al sistema, cargar la lista de
precios de un proveedor, vincular sus renglones con los artículos propios a partir de las
propuestas del sistema, y cargar una factura de compra subiendo el PDF del proveedor y
revisándola antes de confirmar, sobre `lacentral_demo`.

> Única fuente de verdad del estado del sprint. Se actualiza **antes** de cada push, en el
> mismo PR que el código que describe. Reescrito el 09/11/2026 con el plan aprobado
> (`docs/PLAN-SPRINTS-6-7-8.md`), que reemplaza el reparto del 05/11.

**Sprint 6 · 16/11/2026 → 04/12/2026 (fechas ESTIMADAS, tres semanas).** Cierra por entregable,
no por fecha. Vinculación de catálogos + factura por PDF.

---

## Tablero · Sprint 6 (milestone «Sprint 6»)

El tablero y esta tabla son la misma verdad. Si uno cambia, el otro cambia en el mismo push.

| Issue | HU | SP | Asignado | La bloquea | Estado |
|---|---|---|---|---|---|
| #128 | 055 · HU-CMP-08 · lista de precios de un proveedor desde archivo | 5 | Javi | Pregunta 6 a Sergio · C2 | No empezada |
| #126 | 053 · HU-CMP-06 · candidatos por aproximación entre catálogos | 8 | Santi | C2 · C5 · C6 | No empezada |
| #127 | 054 · HU-CMP-07 · vinculación manual con aprendizaje | 8 | Santi | C2 · C5 · la 053 | No empezada |
| #13 | 029 · HU-FCO-05 · carga de factura desde PDF | 13 | Santi | C2 (cotejo de códigos, CA-004) | No empezada |
| #51 | 049 · HU-UI-11 · pantalla de carga desde PDF | 8 | Santi | la 029 | No empezada |

**42 SP: Santi 37, Javi 5** (asignación del 09/11/2026). La pantalla de vinculación va en las
proformas de la 053 y la 054 (C5); sus 16 SP son estimación hasta que existan.

## Compuertas · cada una bloquea solo las HU de su fila

| # | Qué | Responsable | Bloquea | Estado al 09/11 |
|---|---|---|---|---|
| P6 | Pregunta 6 de `PREGUNTAS-SERGIO.md`: de dónde sale «el archivo» | Santi → Sergio | 055 | Abierta. Santi la lleva esta semana; tiene que estar antes del 16/11 |
| C2 | Esquema `proveedor_articulo` y vinculaciones aprendidas (script SQL + re-scaffold) | Santi | 055, 053, 054, cotejo de la 029 | Abierta |
| C5 | Proformas de la 053 y la 054, con la pantalla de vinculación | Santi | 053, 054 | Abierta |
| C6 | Medición sobre 50 artículos | Javi | 053 | Abierta |
| C1 | L2 y retiro de Tarjeta3/6 | Santi | 032, 033, 050 (Sprint 7) | Abierta. Va en el mismo cambio de esquema que C2 |

**Una HU no se empieza hasta que su compuerta esté cerrada**, aunque esté en el milestone.

## Fuera del Sprint 6

Sprint 7 (comparador completo + lo que falta de compra) y Sprint 8 (venta, stock, UI y
artículos): ver `docs/PLAN-SPRINTS-6-7-8.md`. La 031 entra completa en el 7 (D-PL-02, reemplaza
la D16).

---

## PRÓXIMA SESIÓN

El sprint todavía no empezó. Antes de la primera HU: cerrar P6, C2, C5 y C6, y C1 en el mismo
cambio de esquema que C2 (tabla de arriba).
