# Plan de los sprints 6, 7 y 8

Aprobado por Santi el 09/11/2026. Todo lo que queda abierto entra en estos tres sprints: no se
recorta funcionalidad y no hay trabajo futuro. El tablero (milestones de GitHub), el índice de
historias (`docs/historias/INDICE.md`) y el `ESTADO.md` de cada sprint dicen lo mismo que este
archivo; si uno cambia, los otros cambian en el mismo push.

## Reglas del reparto

1. **Cada sprint entrega features completas, nunca dos a medias** (regla en `CLAUDE.md`, junto a
   la §1).
2. Las áreas grandes se llevan un sprint cada una; las chicas se agrupan, todas terminadas.
3. Una feature nunca se parte entre dos sprints.
4. **El sprint cierra por entregable, no por fecha.** Las fechas son **estimaciones** de tres
   semanas. El proyecto termina a fines de enero de 2027.

## Los tres sprints

| Sprint | Fechas **estimadas** | Contenido | SP |
|---|---|---|---|
| 6 | 16/11/2026 → 04/12/2026 | Vinculación de catálogos + factura por PDF | 42 |
| 7 | 07/12/2026 → 25/12/2026 | Comparador completo + lo que falta de factura de compra | 46 |
| 8 | 28/12/2026 → 15/01/2027 | Factura de venta + stock + UI y artículos | 53 + ítems sin SP |

**Total: 141 SP** con SP conocidos, contra unos 34 SP por sprint de dos semanas (velocidad del
Sprint 3: 3,4 SP/día con tres personas). El Sprint 5 entregó unos 19. Por eso se estiran a tres
semanas en lugar de recortar (decisión D-PL-01).

### Sprint 6 · vinculación de catálogos + factura por PDF · 42 SP

**Entregable:** al cerrar este sprint, una persona puede entrar al sistema, cargar la lista de
precios de un proveedor, vincular sus renglones con los artículos propios a partir de las
propuestas del sistema, y cargar una factura de compra subiendo el PDF del proveedor y
revisándola antes de confirmar, sobre `lacentral_demo`.

| Issue | HU | SP | Área |
|---|---|---|---|
| #128 | 055 · HU-CMP-08 · lista de precios de un proveedor desde archivo | 5 | Vinculación |
| #126 | 053 · HU-CMP-06 · candidatos por aproximación entre catálogos | 8 | Vinculación |
| #127 | 054 · HU-CMP-07 · vinculación manual con aprendizaje | 8 | Vinculación |
| #13 | 029 · HU-FCO-05 · carga de factura desde PDF | 13 | PDF |
| #51 | 049 · HU-UI-11 · pantalla de carga desde PDF | 8 | PDF |

- **La pantalla de vinculación entra en las proformas de la 053 y la 054**, cuando se escriban
  en C5. La 050 no tiene ningún CA de vinculación. Los 16 SP de la 053 y la 054 son estimaciones
  hasta que existan sus proformas.
- El PDF va con la vinculación porque el cotejo de códigos (CA-004 de la 029) usa la tabla de C2:
  un solo cambio de esquema para todo.

### Sprint 7 · comparador + lo que falta de compra · 46 SP

**Entregable:** al cerrar este sprint, una persona puede entrar al sistema, buscar un repuesto y
ver los precios de todos sus proveedores ordenados (consultados en vivo, o el último conocido con
su fecha), alternar entre la vista de compra y la de venta, abrir el sitio del proveedor, y
consultar, corregir y anular facturas de compra, sobre `lacentral_demo`.

| Issue | HU | SP | Área |
|---|---|---|---|
| #16 | 030 · HU-CMP-01 · búsqueda comparativa por nombre | 5 | Comparador |
| #18 | 031 · HU-CMP-02 · consulta en vivo a los sitios de proveedores | 13 | Comparador |
| #19 | 032 · HU-CMP-03 · vista Compra | 3 | Comparador |
| #20 | 033 · HU-CMP-04 · vista Venta | 3 | Comparador |
| #35 | 034 · HU-CMP-05 · redirección al sitio del proveedor | 1 | Comparador |
| #52 | 050 · HU-UI-12 · pantalla del comparador | 8 | Comparador |
| #33 | 026 · HU-FCO-02 · consulta de facturas de compra | 3 | Compra |
| #34 | 027 · HU-FCO-03 · modificación acotada de factura de compra | 2 | Compra |
| #9 | 028 · HU-FCO-04 · anulación de factura de compra | 8 | Compra |

- **La 050 no se parte:** su CA-002 y su CA-003 necesitan la 031.
- **La 031 entra completa**, no como prueba de concepto (D-PL-02). Armar los scrapes de cada sitio
  lo hacen a mano Javi, Lauti y Santi en paralelo, **fuera del conteo de SP**.
- La 028 cierra el CA-003 y el CA-004 de la 016 (#23) y destraba la 048 para el Sprint 8.

### Sprint 8 · venta + stock + UI y artículos · 53 SP + ítems sin SP

**Entregable:** al cerrar este sprint, una persona puede entrar al sistema, registrar, consultar,
corregir y anular una factura de venta con su descuento de stock, transferir stock entre
sucursales, y cargar artículos con su código original y su marca, sobre `lacentral_demo`.

| Issue | HU | SP | Área |
|---|---|---|---|
| #26 | 019 · HU-STK-03 · descuento de stock por factura de venta | 5 | Venta |
| #28 | 021 · HU-FVE-01 · registro de factura de venta (pide `medio_cobro`, D-S5-01) | 8 | Venta |
| #29 | 022 · HU-FVE-02 · consulta de facturas de venta | 3 | Venta |
| #30 | 023 · HU-FVE-03 · modificación acotada | 2 | Venta |
| #31 | 024 · HU-FVE-04 · anulación | 5 | Venta |
| #50 | 048 · HU-UI-10 · pantallas de facturas (venta y compra; CA-005 necesita la 024 y la 028) | 5 | Venta |
| #27 | 020 · HU-STK-04 · transferencia entre ubicaciones | 8 | Stock |
| #49 | 047 · HU-UI-09 · pantalla de stock y transferencias | 3 | Stock |
| #41 | 040 · HU-UI-02 · pantalla de inicio de sesión (existe; CA sin verificar) | 2 | UI |
| #47 | 046 · HU-UI-08 · pantalla de artículos (existe; espera C4) | 5 | UI y artículos |
| #53 | 051 · HU-UI-13 · manejo global de errores y avisos | 2 | UI |
| #54 | 052 · HU-UI-14 · documentación del frontend | 5 | UI |
| #15 | 012 · HU-ART-02 · CA2, CA4 y CA5 sin test desde el Sprint 3 | — | Artículos |
| #160 | CA-003 de la 014 · columna para la baja por error de stock | — | Artículos |

**Ítems sin SP**, que esperan una respuesta de Sergio (`docs/sprint05/PREGUNTAS-SERGIO.md`) o su
proforma: C4 (código original y marca, reabre la 011 y la 013; pregunta 5), precio «caso
especial» (HU sin escribir; pregunta 4), CA-003 de la 014 (#160; pregunta 2), provincia en las
direcciones (D-S5-02; pregunta 7), validar la condición fiscal (pregunta 1), y el bug de la
compra que suma stock a un artículo dado de baja. Cada uno lleva SP cuando tenga su regla.

**Fuera del plan, por ser mantenimiento:** #137 (Deuda27) y lo que queda de la fase 2 visual.

## Compuertas

| Compuerta | Bloquea | Se cierra |
|---|---|---|
| **Pregunta 6 a Sergio** (de dónde sale «el archivo») | 055 | **Antes del 16/11.** La lleva Santi esta semana |
| C2 · tabla `proveedor_articulo` y vinculaciones aprendidas | 055, 053, 054 y el cotejo de la 029 | Sprint 6, al arrancar |
| C5 · escribir las proformas de la 053 y la 054 (con la pantalla de vinculación) | 053, 054 | Sprint 6, al arrancar |
| C6 · medición sobre 50 artículos | 053 | Sprint 6, al arrancar |
| C1 · modalidad L2 y retiro de Tarjeta3/6 | 032, 033 y el CA-004 de la 050 | Sprint 6, en el mismo cambio de esquema que C2; se usa en el 7 |
| C4 · código original y marca | 046; reabre la 011 y la 013 | Sprint 8, cuando Sergio responda |

Una HU no se empieza hasta que su compuerta esté cerrada, aunque esté en el milestone.

## Decisiones

| # | Decisión | Fecha | Quién | Motivo |
|---|---|---|---|---|
| D-PL-01 | No se recorta nada: los sprints se estiran a tres semanas estimadas. | 09/11/2026 | Santi | El sprint cierra por entregable, no por fecha (§1 de `CLAUDE.md`). 141 SP contra unos 102. |
| D-PL-02 | **La 031 entra completa, no como prueba de concepto. Reemplaza la D16** (18/10/2026: «la ingesta automatizada es etapa 2, con un proveedor como prueba de concepto»). | 09/11/2026 | Santi | No queda trabajo futuro: todo lo abierto entra en los sprints 6, 7 y 8. Los scrapes los arman a mano Javi, Lauti y Santi en paralelo, fuera del conteo de SP. |
| D-PL-03 | La 050 no se parte; la pantalla de vinculación va en las proformas de la 053 y la 054. | 09/11/2026 | Santi | Todos los CA de la 050 son del comparador. |
