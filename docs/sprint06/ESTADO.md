# ESTADO · Sprint 6

**Entregable:** al cerrar este sprint, una persona puede entrar al sistema y cargar una factura de
compra con la mecánica del negocio, a mano o desde el PDF del proveedor, sobre `lacentral_demo`.
(13/11/2026, D-PL-05: el Sprint 6 es factura de compra; el comparador pasa completo al Sprint 7.)

> Única fuente de verdad del estado del sprint. Se actualiza **antes** de cada push, en el
> mismo PR que el código que describe. Reescrito el 13/11/2026 con la D-PL-05
> (`docs/PLAN-SPRINTS-6-7-8.md`), que reemplaza la D-PL-04 del 12/11.

**Sprint 6 · 16/11/2026 → 04/12/2026 (fechas ESTIMADAS, tres semanas).** Cierra por entregable,
no por fecha. Factura de compra: la 056 primero, el PDF después, contra el esquema ya rehecho.

---

## Tablero · Sprint 6 (milestone «Sprint 6»)

El tablero y esta tabla son la misma verdad. Si uno cambia, el otro cambia en el mismo push.

| Issue | HU | SP | Asignado | La bloquea | Estado |
|---|---|---|---|---|---|
| #163 | 056 · HU-FCO-06 · carga de factura de compra con la mecánica del negocio | 13 (estimación) | Santi | E56 (esquema) | En curso. Proforma escrita; CA-007 aceptado (lo cumple la 025). **Esquema y backend: PR #166.** **Frontend: PR #167.** #166: `verificador-tdd` y `auditor-persistencia` APTO contra `lacentral_test.public` (13/11). #166 **APROBADO por Javi** (14/11; deuda en #168). #167: cambios pedidos por Javi el 14/11 hechos; cuarta ronda (autorizada el 13/11) y su arreglo: **`verificador-tdd` APTO** sobre `4b624d3`, 196/196 frontend; re-review pedida a Javi. #169: artículo repetido por sucursal en la pantalla. Orden de merge: #164 → #165 (mergeados) → #166 y #167 juntos; el 09 en `lacentral` lo aplica Santi después del merge. Falta el recorrido en el navegador contra `lacentral_demo` (DoD 10, Santi o Lauti) |
| #13 | 029 · HU-FCO-05 · carga de factura desde PDF | 13 | Santi | la 056 · C7 | No empezada |
| #51 | 049 · HU-UI-11 · pantalla de carga desde PDF | 8 | Santi | la 029 | No empezada |

**34 SP** (13 + 13 + 8). Salieron al Sprint 7 la #126, la #127 y la #128 (21 SP); entraron la
#13 y la #51 (21 SP). Es un intercambio: el total no cambia.

## Compuertas · cada una bloquea solo las HU de su fila

| # | Qué | Responsable | Bloquea | Estado al 13/11 |
|---|---|---|---|---|
| E56 | Esquema de la 056: `proveedor.tipo_comprobante_habitual`, `factura_compra.tipo_comprobante` y `fecha_imputacion`, dos descuentos más por línea (script SQL + re-scaffold, en `lacentral_test` y `lacentral_demo`) | Santi | 056; la 029 y la 049 lo usan | **Script 09 escrito y aplicado en `lacentral_demo`** (13/11) con re-scaffold. **CERRADA 13/11:** Santi aplicó el 09 a `lacentral_test.public` con el superusuario (`09: TODO OK`). La suite corre contra `public`, sin `LA_CENTRAL_TEST_SCHEMA`. La plantilla `fco06_fuente` se borró el 13/11 |
| C7 | Facturas reales de varios proveedores, para decidir parser por reglas o interpretación asistida (nota de la 029) | Lauti | 029 | Abierta |

C2, C5, C6 y C1 pasaron al Sprint 7 con el comparador (D-PL-05). La P6 se cerró el 11/11.

**Una HU no se empieza hasta que su compuerta esté cerrada**, aunque esté en el milestone.

## Respuestas de Sergio · 13/11/2026

Llegaron el 13/11 (vía Santi). Responden «Lo que vuelve a Sergio» del 11/11.

| # | Decisión | Responde | Impacto |
|---|---|---|---|
| D-S6-08 | Documento del cliente: **alcanza con informar uno cualquiera** de los tres (CUIL, CUIT o DNI). | 2 del 11/11 | Es de la HU de **alta de cliente**, no de la 056. Se aplica cuando se escriba la regla de D-S6-02 (riesgo R1). |
| D-S6-09 | Precio manual: **lo puede cambiar cualquiera**, pero tiene que quedar **registrado quién** lo cambió. | 3 del 11/11 | Es de la **HU de precio manual**, que todavía no existe (D-S6-05). Va como CA cuando se escriba. No toca la 056. |
| D-S6-10 | Número de comprobante: **el 999 no se usa nunca.** El **B** lleva **99** en el primer tramo; el **A** no lleva prefijo, se copia el número exacto de la factura. **Depende del tipo de comprobante, no del proveedor.** | 4 del 11/11 | CA-005 de la 056 desbloqueado y reescrito. |
| D-S6-11 | Un mismo proveedor **puede entregar A y B indistintamente** (todo B, mitad y mitad, todo A: es variable). | nota de la #163 | CA-004 y CA-014 de la 056 reescritos: el tipo es **de la factura**; el del proveedor es solo un valor por defecto (nullable). |
| D-S6-12 | El código de barras **se escanea para encontrar el artículo** y es la **clave de unicidad** (el mismo artículo siempre trae el mismo). El **código original** del fabricante es **otro dato**, aparte. | 5 del 11/11 | **Cierra R2.** Corrige la lectura de D-S6-06. |

### Supuestos de la 056 para confirmar con Sergio

- Los tres descuentos de una línea son **sucesivos** (10 + 5 = 14,5 %). Escrito en la proforma;
  con un solo descuento el costo no cambia respecto de la 025.

### Riesgos

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
- ~~**R2 · código de barras ≠ código original.**~~ **Cerrado el 13/11/2026 (D-S6-12).** El código
  de barras es la clave de búsqueda y ya es único en `articulo`; el código original queda como
  dato aparte, **no único**, y **no se hace ningún backfill** desde el código de barras. La D2 no
  se toca.

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

### Lo que vuelve a Sergio (11/11) · respondido el 13/11

1. ~~Qué es «la segunda lista» y cuánto va para Tarjeta 3 y Tarjeta 6.~~ Ya respondido (D12 y
   D13, 16/10/2026). La D1 de `CLAUDE.md` («tasas SIN DEFINIR») sigue desactualizada: **para
   corregir por el equipo**.
2. ~~¿Alcanza con informar uno de los tres documentos?~~ Sí: D-S6-08.
3. ~~¿Quién puede modificar el precio a mano?~~ Cualquiera, con registro de quién: D-S6-09.
4. ~~El 99 o 999, ¿del proveedor o del tipo de comprobante?~~ Del tipo; el 999 no se usa: D-S6-10.
5. ~~¿Número del código de barras o escanearlo?~~ Escanearlo: D-S6-12.

## Recorrido en el navegador contra lacentral_demo (DoD 10)

Armada el 14/11/2026 para la 056 (#163, PR #166 y #167). **La completa Javi cuando lo haga con
Sergio.** Se recorre en la pantalla «Factura de compra», contra `lacentral_demo`, después del
merge y con el script 09 aplicado. Cada fila: quién, cuándo, con qué datos (proveedor, artículos,
número de comprobante) y qué pasó; si no pasó lo esperado, el número de issue.

| CA | Qué se prueba a mano | Quién | Cuándo | Con qué datos | Qué pasó |
|----|----------------------|-------|--------|---------------|----------|
| CA-001 | Escribir parte del nombre de un proveedor y buscar: aparece. Escribir su CUIT: no lo encuentra por CUIT | | | | |
| CA-002 | Escribir el código completo de un proveedor y Enter: queda elegido sin abrir la lista | | | | |
| CA-003 | Proveedor con código con ceros a la izquierda (p. ej. 00035): escribir 35 y Enter lo elige. Si hay dos que coinciden sin ceros (035 y 00035), Enter muestra la lista y no elige | | | | |
| CA-004 | Elegir un proveedor con comprobante habitual cargado: se propone ese tipo; cambiarlo a mano funciona. Cargar o cambiar el comprobante habitual desde la ficha del proveedor | | | | |
| CA-005 | Tipo B: el número arranca con «99-» y se completa el resto. Tipo A: no se precarga nada y se copia el número tal cual. Un B con «999-» se rechaza | | | | |
| CA-006 | Cargar emisión, vencimiento e imputación distintas; registrar y comprobar que quedan las tres | | | | |
| CA-007 | Cargar las cuatro percepciones (IVA, IIBB, municipal, ganancias) y comprobar que quedan registradas | | | | |
| CA-008 | En el artículo, con tres caracteres o más aparecen coincidencias aproximadas mientras se escribe (probar con un error de tipeo) | | | | |
| CA-009 | Código exacto de un artículo y Enter: se agrega la línea sin lista | | | | |
| CA-010 | «Reemplazar» en una línea con cantidad cargada: cambia el artículo, conserva cantidad, destino y descuentos | | | | |
| CA-011 | El precio propuesto es el costo vigente; sin costo no propone nada (nunca cero). Escribir otro precio y comprobar que se registra el escrito | | | | |
| CA-012 | Tres descuentos en una línea: se registran los tres y el costo es el neto sucesivo | | | | |
| CA-013 | Cambiar la sucursal de la cabecera: las líneas nuevas la heredan, las ya cargadas no cambian; una línea a otra sucursal | | | | |
| CA-014 | Misma factura como A y como B: el A suma el 21 % de IVA, el B no | | | | |
| CA-015 | Antes de confirmar se ve el total con percepciones e IVA, sin registrar nada; cambiar algo borra el total y hay que recalcularlo | | | | |

---

## Fuera del Sprint 6

Sprint 7 (comparador completo, scrapes incluidos, + lo que falta de compra) y Sprint 8 (venta,
stock, UI y artículos): ver `docs/PLAN-SPRINTS-6-7-8.md`. La 031 entra completa en el 7 (D-PL-02,
reemplaza la D16). La revisión de abajo queda acá como historia y es la entrada de C5.

### Revisión de la 053, 054 y 055 contra D-S6-01 · pasa al Sprint 7 (D-PL-05)

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

---

## PRÓXIMA SESIÓN

**14/11 · Revisión de Javi sobre #166 y #167, y cuarta ronda del #167.**
- **#166: APROBADO por Javi (14/11). No se le pushea nada.** Sus dos observaciones quedan como
  deuda en la **#168** (sin milestone): `ModificarProveedorUseCase` pisa
  `TipoComprobanteHabitual` con null si el request no lo trae; el mensaje del tipo B tiene que
  mostrar el ejemplo `99-00001234`.
- **#169** (milestone Sprint 6, observación de Javi del 14/11): la pantalla no deja cargar el
  mismo artículo dos veces aunque vaya a sucursales distintas; el backend lo permite (D9). Ya
  pasaba antes del #167.
- **#167, REQUEST CHANGES de Javi (14/11):** `enterProveedor()` con dos coincidencias exactas sin
  ceros (035 y 00035) ahora lista en lugar de elegir (rojo `4dd89a1`, código `8a331f1`, solo
  frontend); columna Prueba de la 056 completa (CA-011 sin test que mande al servidor un precio
  escrito distinto del propuesto); sección DoD 10 armada arriba, para Javi.
- **Cuarta ronda autorizada por el equipo el 13/11.** Specs de las 5 mutaciones de la ronda 3 en
  `7b36e8d` (solo tests). Ronda 4 del `verificador-tdd`: **NO APTO** (sobrevivían 2 mutaciones en
  `terceros.ts:144`); arreglo `4b624d3` (solo spec), verificación acotada al arreglo: **APTO**
  (14/11). Frontend completo sobre `4b624d3`: 196/196. Backend sin cambios (560/560 sobre
  `772e934`), no se corrió. Se saca el «NO MERGEAR» de los dos PR; se pide de nuevo la revisión.
- **Corrección sobre la ronda 1 del #167 (13/11):** se actuó sobre un aviso temprano del
  verificador como si fuera su reporte, se sacó su worktree mientras corría y se lanzó la ronda 2
  con la 1 viva, contra la regla R1. Por eso se publicó un APTO inválido (push `443e38b`), que se
  corrigió en `69b4356`.
- **El script 09 todavía no está aplicado en `lacentral`:** lo aplica Santi después del merge.
- Orden de merge, sin cambios: #164 y #165 (ya mergeados el 14/11), y #166 con #167 juntos.

**13/11 noche (cierre) · #167 parado en tres rondas sin APTO.** #166 verificado (`verificador-tdd` y
`auditor-persistencia` APTO contra `public`). #167: ronda 1 NO APTO (M02, M05, M10, M14), cortada
porque se le sacó el worktree por error; ronda 2 APTO solo para `9ad0497`; ronda 3 NO APTO: los
specs de `f0e5588` matan lo de la ronda 1, pero sobreviven `factura-compra.ts:277`, `:303`, `:351`,
`nucleo/api.ts:165` (sobrevive con la suite entera) y `terceros.ts:144`. Son specs faltantes, sin
cambios de producción. Frontend 190/190 sobre `f0e5588`; backend 560/560 sobre `772e934`.
**Decide el equipo:** autorizar una cuarta ronda (los 5 specs) o mergear con los huecos declarados.
Hasta entonces, «NO MERGEAR» en el #167, y el #166 va junto. Orden de merge, cuando toque: #164 →
#165 → #166 y #167 juntos. Siguen de personas: esa decisión, el merge (Javi) y el recorrido en el
navegador contra `lacentral_demo`, DoD 10 (Santi o Lauti). `fco06_fuente` borrado.

**13/11 noche · E56 cerrada, verificación contra el esquema real.** Santi aplicó el 09 a
`lacentral_test.public`. Suites completas contra `public` (sin `fco06_fuente`): #166 560/560 sobre
`d92f395`. El `verificador-tdd` del #166 contra `public` dio NO APTO en ronda 1 (3 mutaciones en
`CalcularTotales` y en la escala del tercer descuento); arreglado con tests en `3efa04d`. Siguen:
ronda 2 del verificador acotada a `3efa04d`, `auditor-persistencia` del #166, merge a
`feature/163-frontend` con su suite y `verificador-tdd` del #167. Después, borrar `fco06_fuente`.
Detalle en la bitácora.

**13/11 tarde · 056 esquema + backend.** Script `09_Factura_Compra_Mecanica.sql` aplicado en
`lacentral_demo` y re-scaffold. **Pendiente de Santi: aplicar el 09 a `lacentral_test`** (con el
superusuario, como el 08); mientras tanto la suite corre con `LA_CENTRAL_TEST_SCHEMA=fco06_fuente`.
Backend en su PR; el frontend de la 056 va en otro PR encima. El backend exige `tipoComprobante`:
**los dos PR se mergean juntos** (si entra solo el backend, la pantalla actual no puede registrar).

**13/11 · D-PL-05 y respuestas de Sergio (D-S6-08 a 12).** El Sprint 6 es factura de compra
(056 + 029 + 049); el comparador (053, 054, 055) pasó al Sprint 7. Proforma de la 056 escrita,
#163 con CA4, CA5 y CA14 nuevos. Sigue E56 (script de esquema + re-scaffold, aviso a Javi en la
#163 antes de aplicarlo), después el PR de esquema y backend de la 056 y el de frontend. La
bitácora de la sesión está en `docs/sprint06/BITACORA.md`. R1 sigue abierto: falta que Santi
corra la consulta.

**Anterior (historia):**

**12/11 · el Sprint 5 ya está en `main`** (`f0187d37`, etiqueta `sprint-5`; ver el `ESTADO.md`
del Sprint 5) y **#160 está cerrada**. Lo de abajo del 11/11 queda como historia en esos dos
puntos. D-PL-04: entra la 056 (correcciones de Javi a la carga de factura de compra) y el PDF
(#13, #51) pasa al Sprint 7. Falta escribir la proforma de la 056 y abrir su issue, y sumar sus
tres cambios al script de esquema del sprint, junto con C2 y C1.

**11/11 · respuestas de Sergio registradas (D-S6-01 a 07).** El sprint todavía no empezó, y
**el Sprint 5 todavía no llegó a `main`**: #161 y #162 siguen abiertos (ver el `ESTADO.md` del
Sprint 5). Antes de la primera HU del comparador, el equipo decide: qué entrega un scrape y la
reescritura de la 055; la opción (a) o (b) del plan; CA3 de la 054 contra D2; la D1 de
`CLAUDE.md`. Siguen C2, C5 y C6, y C1 en el mismo cambio de esquema que C2. Pendientes: contar
R1 cuando la base responda, cerrar #160, y escribir la HU de las correcciones de Javi.
