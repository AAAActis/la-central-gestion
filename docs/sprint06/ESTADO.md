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
| #128 | 055 · HU-CMP-08 · lista de precios de un proveedor desde archivo | 5 | Javi | C2 · **reescritura (D-S6-01)** | No empezada. **11/11:** no se construye como está escrita; ver «Revisión de la 053, 054 y 055» |
| #126 | 053 · HU-CMP-06 · candidatos por aproximación entre catálogos | 8 | Santi | C2 · C5 · C6 | No empezada |
| #127 | 054 · HU-CMP-07 · vinculación manual con aprendizaje | 8 | Santi | C2 · C5 · la 053 | No empezada |
| #13 | 029 · HU-FCO-05 · carga de factura desde PDF | 13 | Santi | C2 (cotejo de códigos, CA-004) | No empezada |
| #51 | 049 · HU-UI-11 · pantalla de carga desde PDF | 8 | Santi | la 029 | No empezada |

**42 SP: Santi 37, Javi 5** (asignación del 09/11/2026). La pantalla de vinculación va en las
proformas de la 053 y la 054 (C5); sus 16 SP son estimación hasta que existan.

## Compuertas · cada una bloquea solo las HU de su fila

| # | Qué | Responsable | Bloquea | Estado al 09/11 |
|---|---|---|---|---|
| P6 | Pregunta 6 de `PREGUNTAS-SERGIO.md`: de dónde sale «el archivo» | Santi → Sergio | 055 | **Cerrada el 11/11/2026:** no hay archivo, la fuente son los scrapes (D-S6-01). Abre otra: qué entrega un scrape |
| C2 | Esquema `proveedor_articulo` y vinculaciones aprendidas (script SQL + re-scaffold) | Santi | 055, 053, 054, cotejo de la 029 | Abierta |
| C5 | Proformas de la 053 y la 054, con la pantalla de vinculación | Santi | 053, 054 | Abierta |
| C6 | Medición sobre 50 artículos | Javi | 053 | Abierta |
| C1 | L2 y retiro de Tarjeta3/6 | Santi | 032, 033, 050 (Sprint 7) | Abierta. Va en el mismo cambio de esquema que C2 |

**Una HU no se empieza hasta que su compuerta esté cerrada**, aunque esté en el milestone.

## Respuestas de Sergio · 11/11/2026

Llegaron el 11/11 (vía Lauti). Responden `docs/sprint05/PREGUNTAS-SERGIO.md`; la numeración de
Sergio no es la del archivo, por eso va la columna «Preg.».

| # | Decisión | Preg. | Impacto |
|---|---|---|---|
| D-S6-01 | **No se construye importador de planillas.** Solo Sur Piezas y GM mandan lista, por WhatsApp y casi nunca. La fuente de precios del comparador son los **scrapes web**. | 6 | Cierra P6. La 055 (#128) **no se construye como está escrita**: ver «Revisión de la 053, 054 y 055». |
| D-S6-02 | Condición fiscal del cliente: **opcional**, se completa después. Obligatorios: **documento (CUIL, CUIT o DNI), teléfono y domicilio**. Email y el resto, opcionales. | 1 | Riesgo R1. Teléfono y domicilio son tablas 1:N (`cliente_telefono`, `cliente_direccion`): un NOT NULL no los puede exigir, va en el caso de uso. |
| D-S6-03 | Baja de artículo con stock: **primero se corrige la existencia, después se da de baja.** No hay baja «por error de stock». | 2 | **#160 se cierra** sin construir; el CA-003 de la 014 (#21) pasa a «no aplica». Sale del Sprint 8. |
| D-S6-04 | Márgenes: **L1 +50 %, L2 +40 %, L3 0 % sobre el costo**, iguales para todos los artículos y rubros. | 3 | **Confirma D13 (16/10/2026)**, no la contradice: Efectivo = L1 − 10 % (D14) es otra modalidad, aparte de L2. Los CA de márgenes por artículo (#17) no aplican. |
| D-S6-05 | Precio manual: es habitual, sobre todo para redondear. **Se mantiene y NO se recalcula** con las compras siguientes. | 4b | Es lo que ya hace el recálculo (está probado). Falta la HU de precio manual. |
| D-S6-06 | Los 2644 artículos deberían tener el **código original**, que «se obtiene del código de barras». La **marca** sale de la información del proveedor. | 5 | Riesgo R2. Responde a y b de la pregunta 5; C4 sigue abierta. |
| D-S6-07 | Provincia: dato necesario, **sin uso operativo hoy**. La normalización (07) está bien; no hay más trabajo. | 7 | D-S5-02 se cierra sin combo ni JOIN nuevos. |

### Lo que vuelve a Sergio

1. ~~Qué es «la segunda lista» y cuánto va para Tarjeta 3 y Tarjeta 6.~~ **No hace falta
   preguntarlo:** ya está respondido. L2 = costo × 1,40 (D13, 16/10/2026); Tarjeta 3 y 6
   **salen del sistema** (D12, 16/10/2026, confirmado por Alfredo el 17/10), con el cambio de
   esquema pendiente en C1. Lo desactualizado es la D1 de `CLAUDE.md` («tasas SIN DEFINIR»),
   que no recogió D12 ni D13: **para corregir por el equipo**.
2. ¿Alcanza con informar **uno** de los tres documentos (CUIL, CUIT o DNI)?
3. ¿**Quién** puede modificar el precio a mano? (pregunta 4a, sin respuesta).
4. El 99 o 999 del número de factura, ¿depende del **proveedor** o del **tipo de comprobante**?
5. (Sugerida, por R2) Cuando dice que el código original sale del código de barras, ¿es el
   **número** del código de barras, o **escanearlo** para encontrar el artículo?

### Riesgos antes de codear

- **R1 · clientes importados sin teléfono o domicilio.** Hay que contarlos antes de escribir la
  regla. **No se pudo contar el 11/11:** la base de pruebas no respondía
  (`TcpTestSucceeded: False`), y `lacentral` no se lee con el rol de desarrollo (S5-05). Consulta
  de solo lectura para quien tenga acceso (`lacentral_test.public` tiene los mismos 2108):

  ```sql
  SELECT count(*) AS total,
         count(*) FILTER (WHERE NOT EXISTS (SELECT 1 FROM cliente_telefono t
                          WHERE t.cliente_id = c.id AND btrim(t.numero) <> '')) AS sin_telefono,
         count(*) FILTER (WHERE NOT EXISTS (SELECT 1 FROM cliente_direccion d
                          WHERE d.cliente_id = c.id AND coalesce(btrim(d.calle), '') <> '')) AS sin_domicilio,
         count(*) FILTER (WHERE c.cuit_cuil IS NULL OR c.cuit_simulado) AS sin_documento_real
  FROM cliente c;
  ```

  Si son muchos, la regla aplica al alta y no puede bloquear la modificación de un cliente
  viejo por un dato que nunca tuvo. Ojo con `cuit_simulado`: un CUIT inventado no es un
  documento.
- **R2 · código de barras ≠ código original.** El EAN no es el código del fabricante. Además,
  el 19/10 Sergio dijo que dos marcas pueden vender **el mismo código original** y son dos
  artículos: el código original **no es único**, y la D2 exige unicidad global de códigos. Si se
  toma literal y se cargan como códigos, se rompe la D2. Para decidir con la pregunta 5 de arriba.

### Revisión de la 053, 054 y 055 contra D-S6-01

- **#128 · 055 · lista desde archivo: no se construye como está escrita.** Su premisa («un
  archivo que el proveedor ya emite») es falsa para casi todos los proveedores, y Sergio dijo que
  no. Pero **sus CA2, CA4, CA5 y CA6 no son del archivo, son de incorporar un catálogo**, y los
  scrapes los necesitan igual: actualizar por código sin duplicar ni perder vinculaciones, dejar
  registro de cada incorporación, no borrar lo que no vino y marcar su precio como vencido,
  rechazar un proveedor dado de baja. CA1 (previsualizar el archivo) y CA3 (columnas mínimas)
  mueren con el archivo. **Propuesta:** reescribir la 055 como «incorporar al catálogo del
  proveedor el resultado de un scrape», con esos cuatro CA. Para eso falta saber **qué entrega un
  scrape** (archivo, JSON, dónde corre, quién lo dispara). **Lo decide el equipo.**
- **#126 · 053 · candidatos por aproximación: los CA siguen valiendo**, porque buscan sobre filas
  de catálogo, vengan de donde vengan. Cambia su dependencia: decía «depende de la 055». **Sin
  filas de catálogo no hay sobre qué buscar ni con qué medir C6** (50 artículos reales).
- **#127 · 054 · vinculación: los CA siguen valiendo**, con dos observaciones:
  - **CA4** (existencia: el máximo, nunca la suma) supone que el catálogo trae existencia. Si un
    scrape no la trae, el CA no tiene dato. Verificarlo con los primeros scrapes.
  - **CA3 choca con la D2, y no por la respuesta 1:** CA1 dice que vincular «agrega el código del
    proveedor a los códigos del artículo»; CA3, que dos códigos del mismo proveedor se vinculan al
    mismo artículo. Eso son dos códigos vigentes del mismo proveedor para el mismo artículo, y la
    D2 permite uno. **CA contradictorios: lo decide el equipo en C5**, antes de la proforma.
- **Consecuencia para el plan (para decidir, no está decidido):** el entregable del Sprint 6
  («cargar la lista de precios de un proveedor, vincular sus renglones…») no se puede demostrar
  sin una fuente de filas de catálogo, y la única fuente ahora son los scrapes, que el plan pone
  en el Sprint 7 (031). Opciones: **(a)** traer al 6 la incorporación de scrapes de uno o dos
  proveedores (la 055 reescrita) antes de la 053; **(b)** pasar la vinculación al 7, junto con
  la 031, y que el 6 sea factura por PDF + las correcciones de Javi a la factura de compra.

### Correcciones de Javi a la carga de factura de compra · HU nueva, sin escribir

Alcance nuevo sobre HU-FCO-01 (entregada y aceptada): va como **historia propia**, no como bug.
Propuesta: **dos HU y dos PR**, porque una no necesita esquema y la otra sí.

- **Solo pantalla** (sin compuerta): buscar proveedor por código o nombre; Enter sobre el código
  exacto selecciona sin abrir la búsqueda; ceros a la izquierda ignorados (35 → 00035); artículos
  por código o nombre con aproximación mientras se escribe (pg_trgm); Enter sobre el código exacto
  selecciona; botón para cambiar el artículo de una línea; sucursal heredada de la cabecera y
  editable por línea (D9); total final con percepciones (D8) e IVA antes de confirmar.
- **Con esquema** (script en `backend/db/` + re-scaffold): proveedor en blanco o en negro, del que
  salen el tipo A/B y el 21 % de IVA automáticos; fechas de emisión, vencimiento e imputación en
  `factura_compra`; precio editable y hasta tres descuentos por línea en
  `factura_compra_detalle`. Conviene que vaya en la misma ventana de esquema que C2 y C1.

## Fuera del Sprint 6

Sprint 7 (comparador completo + lo que falta de compra) y Sprint 8 (venta, stock, UI y
artículos): ver `docs/PLAN-SPRINTS-6-7-8.md`. La 031 entra completa en el 7 (D-PL-02, reemplaza
la D16).

---

## PRÓXIMA SESIÓN

**11/11 · respuestas de Sergio registradas (D-S6-01 a 07).** El sprint todavía no empezó, y
**el Sprint 5 todavía no llegó a `main`**: #161 y #162 siguen abiertos (ver el `ESTADO.md` del
Sprint 5). Antes de la primera HU del comparador, el equipo decide: qué entrega un scrape y la
reescritura de la 055; la opción (a) o (b) del plan; CA3 de la 054 contra D2; la D1 de
`CLAUDE.md`. Siguen C2, C5 y C6, y C1 en el mismo cambio de esquema que C2. Pendientes: contar
R1 cuando la base responda, cerrar #160, y escribir la HU de las correcciones de Javi.
