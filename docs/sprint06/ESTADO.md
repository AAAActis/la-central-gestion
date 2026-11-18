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
| #163 | 056 · HU-FCO-06 · carga de factura de compra con la mecánica del negocio | 13 (estimación) | Santi | — | **Construida y mergeada.** PR #166 (esquema y backend) y #167 (frontend), aprobados y mergeados por Javi el 16/11/2026, con `verificador-tdd` y `auditor-persistencia` APTO (ver la historia abajo). **Issue abierta:** falta el recorrido DoD 10 contra `lacentral_demo`, que hace Javi con Sergio (tabla armada abajo). El 09 en `lacentral`: lo aplica Santi (bitácora, bloque «PARA SANTI» del 17/11). **Cuarto descuento (D-S6-15, 17/11):** rama `feature/163-cuarto-descuento`, apilada sobre la 049: **construida (18/11/2026), PR abierto** con base `feature/51-pantalla-pdf`: script 10, re-scaffold, núcleo, repositorio, API, pantalla y precarga del PDF. `verificador-tdd` NO APTO en la ronda 1 (sobrevivía el regex decimal de «Pago Contado») y APTO en la 2; `auditor-persistencia` APTO. Backend 670/670 sobre `4125c24`; frontend 252/252 sobre `da0412c` (sin cambios después). **E10 cerrada el 18/11/2026** (el 10 en las tres bases): **el #181 puede entrar a `develop` cuando Javi lo revise** |
| #13 | 029 · HU-FCO-05 · carga de factura desde PDF | 13 | Santi | la 056 · C7 | **Construida (17/11/2026), en dos PR apilados.** **Parte 1, interpretación (CA-001 a CA-004): PR #177**: `verificador-tdd` NO APTO en las rondas 1 y 2 y APTO en la 3; `auditor-persistencia` APTO; 637/637 sobre `87456be`. **Parte 2, `origen = 'PDF'` (CA-006): PR abierto** en `feature/13-factura-pdf-registro`: `verificador-tdd` APTO en la ronda 1, auditor APTO, 640/640 sobre `61e4ebb`. CA-005: el backend rechaza una línea sin artículo y la pantalla la bloquea (049). Falta el DoD 10 con los PDF reales. **Revisión de Santi (17/11), arreglos en el #177:** percepciones de ETMAN (la que no se lee va a revisión), número del presupuesto de Fazio desde el nombre del archivo (D-S6-13), proveedor elegido a mano (sus códigos se buscan en él). **Lector de Expoyer:** rama `feature/13-lector-expoyer`, PR propio apilado sobre el del cuarto descuento. Arreglos del #177: `verificador-tdd` NO APTO en la ronda 1 y APTO en la 2, `auditor-persistencia` APTO; backend 652/652 sobre `b0cf239`. #178: 655/655 sobre `5be9f56`. Expoyer: `verificador-tdd` y auditor APTO en la ronda 1; 678/678 sobre `d89a06a`. **D-S6-17 (18/11):** el lector de Fazio toma el «P. Final» como costo; con el real, 323.670,44 (en el #177, mergeado hacia arriba). `verificador-tdd` y `auditor-persistencia` APTO en la ronda 1; backend 654/654 sobre `e0d6004` (#177), 657/657 sobre `ab699e7` (#178; mismo backend que el #179), 672/672 sobre `925ea6e` (#181), 683/683 sobre `465072d` (#180) |
| #51 | 049 · HU-UI-11 · pantalla de carga desde PDF | 8 | Santi | la 029 | **Construida (17/11/2026), PR abierto** en `feature/51-pantalla-pdf`, apilado sobre el #178. El PDF precarga el formulario de la 056, sin pantalla nueva. `verificador-tdd`: NO APTO en las rondas 1 y 2, APTO en la 3; el auditor no aplica. Frontend 238/238 sobre `43edd66`. Falta el DoD 10 contra `lacentral_demo` con los PDF reales (Javi con Sergio). **Revisión de Santi (17/11):** las percepciones sin leer se señalan, y elegir el proveedor después del PDF resuelve sus códigos; `verificador-tdd` NO APTO en la ronda 1 y APTO en la 2; frontend 248/248 sobre `78ff462`; su backend es el del #178 |
| #169 | Factura de compra: el mismo artículo a sucursales distintas (D9), observación de Javi del 14/11 | sin estimar | Santi | — | **Hecha:** PR #170, mergeado por Javi el 16/11/2026 (`verificador-tdd` APTO, 203/203 frontend sobre `b7973d6`). Issue cerrada el 17/11 con referencia al #170 |

| #182 | 057 · HU-FCO-07 · registro de nota de crédito de proveedor (D-S6-18) | 13 (estimación) | Santi | E11 | **Entró el 18/11/2026.** Proforma `docs/historias/057-HU-FCO-07.md` (11 CA; el CA-011, a confirmar). Dos PR: carga manual, después PDF de Expoyer. Script 11 escrito. **Hecho sin esquema (18/11):** PR 1 en `feature/182-nota-credito` (caso de uso con Moq 42/42, pantalla 19/19, módulo en el menú; frontend completo 271/271) y PR 2 en `feature/182-nota-credito-pdf`, apilada encima (lector de la nota de Expoyer: con el real, `0010-00021495`, 115.040,00, dos facturas, nada para revisar; interpretación por CUIT y número 9/9). Sin PR todavía: **frenada por E11** (re-scaffold, repositorio, API, CA-008, endpoint del PDF y precarga). Verificadores: se invocan al cerrar cada PR |

**47 SP** (13 + 13 + 8 + 13; la 057 entró el 18/11, D-PL-06: el sprint se estira). Salieron al Sprint 7 la #126, la #127 y la #128 (21 SP); entraron la
#13 y la #51 (21 SP). Es un intercambio: el total no cambia.

## Compuertas · cada una bloquea solo las HU de su fila

| # | Qué | Responsable | Bloquea | Estado al 13/11 |
|---|---|---|---|---|
| E56 | Esquema de la 056: `proveedor.tipo_comprobante_habitual`, `factura_compra.tipo_comprobante` y `fecha_imputacion`, dos descuentos más por línea (script SQL + re-scaffold, en `lacentral_test` y `lacentral_demo`) | Santi | 056; la 029 y la 049 lo usan | **Script 09 escrito y aplicado en `lacentral_demo`** (13/11) con re-scaffold. **CERRADA 13/11:** Santi aplicó el 09 a `lacentral_test.public` con el superusuario (`09: TODO OK`). La suite corre contra `public`, sin `LA_CENTRAL_TEST_SCHEMA`. La plantilla `fco06_fuente` se borró el 13/11 |
| C7 | Facturas reales de varios proveedores, para decidir parser por reglas o interpretación asistida (nota de la 029) | Javi | 029 | **CERRADA COMPLETA (18/11/2026, D-S6-19)**: ETMAN, Fazio y Expoyer con PDF digital y lector; Autocor entrega en papel y se carga a mano (056). Ver «Compuerta C7» abajo |
| E10 | Script 10 (cuarto descuento, D-S6-15) en `lacentral_test`, `lacentral_demo` y `lacentral` | Santi | el PR del cuarto descuento | **CERRADA 18/11/2026.** Santi aplicó el 10 en las tres bases: `s09 = 1` y `s10 = 1` en todas. En `lacentral` también el 08 (ya estaba) y el 09 (`TODO OK`); el 07 ya estaba. Re-scaffold `fcb9e51` |
| E11 | Script 11 (nota de crédito de proveedor y depósito Reclamos, D-S6-18 y D-S6-20; anulación, D-S6-23) en `lacentral_test` (plantilla `nc057_fuente`, según el equipo el 18/11), después en `lacentral_demo` y `lacentral` | Santi | la 057 (lo que toca el esquema: re-scaffold, repositorio, pruebas contra PostgreSQL, API) | **ABIERTA (18/11/2026).** Script escrito en `feature/182-nota-credito`. **Ahora: plantilla `nc057_fuente`** en `lacentral_test` (copia de `public` con el 11; `public` no se toca, las suites de las otras ramas siguen igual): comandos en la bitácora («PARA SANTI · 18/11 · plantilla nc057_fuente»), ensayados el 18/11 en un esquema descartable. La 057 corre con `LA_CENTRAL_TEST_SCHEMA=nc057_fuente`. **El 11 en `public`, `lacentral_demo` y `lacentral`**, cuando Javi mergee la pila y el PR de la carga manual de la 057; ahí se borra `nc057_fuente` |

C2, C5, C6 y C1 pasaron al Sprint 7 con el comparador (D-PL-05). La P6 se cerró el 11/11.

## Lo mergeado el 16/11/2026 · Javi revisó, aprobó y mergeó todos los PR abiertos

El Sprint 5 está cerrado (etiqueta `sprint-5`). `develop` en `b920a89`.

| PR | Qué | Issue | Verificación con la que entró | Issue |
|---|---|---|---|---|
| #166 | 056 · esquema (script 09) y backend | #163 | `verificador-tdd` APTO (ronda 2), `auditor-persistencia` APTO, 560/560 | abierta: falta DoD 10 |
| #167 | 056 · frontend | #163 | `verificador-tdd` APTO (ronda 4 autorizada), 197/197 | abierta: falta DoD 10 |
| #170 | #169 · mismo artículo a sucursales distintas | #169 | `verificador-tdd` APTO, 203/203 | **cerrada 17/11** |
| #171 | #168 · el PUT de proveedor no borra el tipo habitual | #168 | `verificador-tdd` APTO (ronda 2), auditor APTO, 564/564 | **cerrada 17/11** |
| #172 | docs: D1 de `CLAUDE.md` alineada con D12, D13 y D14 | — | solo docs | — |
| #173 | 028 parte 1 · anulación, backend con doble certificación | #9 | `verificador-tdd` APTO (ronda 2), auditor APTO, 591/591 | abierta (ver abajo) |
| #174 | 026 · consulta de facturas de compra | #33 | `verificador-tdd` APTO (ronda 3), auditor APTO, 577/577 backend, 206/206 frontend | **cerrada 17/11** |
| #175 | 028 parte 2 · pantalla de anulación | #9 | `verificador-tdd` APTO (ronda 2), 608/608 backend, 214/214 frontend | abierta (ver abajo) |

**La 028 y la 026 son del Sprint 7, construidas antes de tiempo.** Su estado vive en
`docs/sprint07/ESTADO.md` y salen de la cuenta de SP del Sprint 7 (`docs/PLAN-SPRINTS-6-7-8.md`,
67 → 56). La #9 queda **abierta**: el CA-002 tiene una interpretación pendiente de Sergio («conserva su
precio de costo previo» = el costo no se toca).

**La 016 (#23):** la 028 **cierra el CA-003** (marca el registro de historial como anulado y vuelve
al registro válido anterior: `Anular_RevierteStockCostoYProveedor_…` y
`Anular_ConVariosRegistrosValidos_VuelveAlMasReciente`). El **CA-004** queda cubierto por el mismo
test que el CA-002 de la 028, con la misma interpretación pendiente: la #23 sigue abierta hasta que
Sergio la confirme.

## Compuerta C7 · facturas reales · 17/11/2026

Llegaron tres PDF, **fuera del repo** (`~/dev/TESIS/facturas-reales/`; el repo es público, nunca
entran ni ellos ni sus datos; `docs/facturas/` va al `.gitignore`):

| Proveedor | Qué es | Texto | Números | Línea |
|---|---|---|---|---|
| ETMAN (Multioriginal Parts S.A.) | **Factura A** | extraíble (PScript + Ghostscript) | formato EE. UU. (`20,184.21`) | código, descripción (a veces en dos o tres renglones), marca, cantidad, precio, tres descuentos (Bonif., Bonif., Dto., con signo menos), total. Percepción de IIBB y de IVA aparte; IVA 21 % |
| Fazio (Badaloni) | **Presupuesto**, compra en negro | extraíble | formato argentino (`27.396,12`) | orden, código de fábrica, descripción, cantidad, remito, precio de lista, descuento combinado `43,84 (28+22)`, precio final, total. **El texto no trae el número del presupuesto** (solo el nombre del archivo) **ni el CUIT del emisor** |
| Expoyer S.A. | **Nota de crédito A** por bonificación | extraíble | formato EE. UU. | no es una factura de compra |
| Expoyer S.A. (llegó el 17/11) | **Factura A** | extraíble | formato EE. UU. | dos columnas de artículos por hoja (cantidad, código, descripción, precio unitario, total); los precios ya vienen **netos del descuento global** de la cabecera («Descuento: 61.08%+15»); códigos con espacios y guiones internos («C 115105 4- F»); IVA 21 %; sin fecha de vencimiento en el texto («CUENTA CORRIENTE 35 DIAS») |

**Hechos que salen de los PDF:**
- **Los descuentos son sucesivos, confirmado con dos proveedores.** ETMAN: 20.184,21 × 0,95 × 0,64 =
  12.272,00 (el total de la línea). Fazio: 28 + 22 → 43,84 %. Era el supuesto de la 056 que faltaba
  confirmar: **queda confirmado** y se saca de «Supuestos de la 056».
- **Cada proveedor usa un formato de número distinto**: el lector no puede asumir uno. Cada layout
  declara el suyo.

**D-S6-19 (18/11/2026): C7 CERRADA COMPLETA.** Autocor entrega las facturas **en papel**: no hay PDF
digital, y un escaneo no sirve (no tiene texto; el Mandato y el CA-002 de la 029 dejan afuera los
escaneados). **Autocor se carga por la vía manual (056).** Quedan tres proveedores con PDF digital y
lector (ETMAN, Fazio y Expoyer) y Autocor declarado carga manual. La factura de Autocor sale de los
pendientes. (La conseguía Javi, no Lauti: corregido en la tabla de compuertas.)

**Decisión C7 (17/11/2026, historia): se cerró PARCIAL.** Con dos layouts de compra distintos y texto
extraíble en los tres alcanza para decidir el enfoque (abajo) y construir los lectores de ETMAN y
de Fazio. Quedaba abierta una factura de Autocor (de Javi), que cerró la D-S6-19 del 18/11. La de
Expoyer llegó el 17/11 y su lector está hecho (rama `feature/13-lector-expoyer`). Mientras tanto, un PDF de un layout que el sistema no conoce se rechaza y se
indica la carga manual (como el CA-002 de la 029).

**Decisión de la 029 (17/11/2026): parser por reglas, un lector por layout.** No interpretación
asistida. Por qué:
1. Los tres PDF traen **texto extraíble**: no hace falta OCR, que era donde la IA aportaba.
2. Los layouts son **estables por proveedor** (los genera su sistema de facturación) y **pocos**:
   cuatro proveedores habituales. Un lector por layout es determinístico, se prueba con PDF
   sintéticos y da siempre el mismo resultado; una interpretación asistida no.
3. **Privacidad:** una interpretación asistida manda las facturas reales de la empresa a un servicio
   externo. Con reglas, no sale nada del servidor, y no hay clave que custodiar.
4. **Certeza explícita:** cada lector sabe qué campo encontró y cuál no; lo que no reconoce con
   certeza (el número del presupuesto de Fazio, un proveedor sin CUIT en el texto, un código sin
   artículo) se devuelve marcado para revisar, nunca adivinado.

**Cuánto cuesta un proveedor nuevo con esta opción:** una clase de lector (reconoce el layout por su
encabezado de columnas, declara su formato de número y parte las líneas; del orden de 100 líneas),
un generador de su PDF sintético en los tests con datos inventados, y sus tests. Sin cambios de
esquema, de API ni de pantalla. Unas horas por layout, más el PDF real para mirar.

**Biblioteca:** PdfPig (`UglyToad.PdfPig`, Apache 2.0, .NET puro, sin binarios nativos), solo en
`LaCentral.Api` detrás de un puerto; `LaCentral.UseCases` sigue sin paquetes. La misma biblioteca
escribe PDF, y con eso los tests generan los sintéticos: **ningún test usa los PDF reales ni depende
de que exista `~/dev/TESIS/facturas-reales/`.** Los reales son para analizar el layout y para el
recorrido a mano de la DoD 10.

## Lo que vuelve a Sergio · 17/11/2026

Sin respuesta, la 029 sigue con lo que no depende de esto: lo que falta se marca «para revisar».

1. ~~Presupuesto en negro (Fazio): ¿B con 99? ¿Qué número?~~ Respondida el 17/11: D-S6-13.
2. ~~Notas de crédito de proveedor: ¿se cargan?~~ Respondida el 17/11: sí (D-S6-14). Es una HU
   nueva, pendiente de decisión del equipo (ver «Fuera del Sprint 6»).
3. ~~Pago contado de ETMAN: ¿se ignora o se registra?~~ Respondida el 17/11: cuarto descuento
   (D-S6-15).
4. **CA-002 de la 028 y CA-004 de la 016** (abierta desde el 14/11): «conserva su precio de costo
   previo», ¿significa que el costo no se toca al anular, porque no hay registro del costo anterior?
5. ~~**Redondeo de 2 centavos en el presupuesto de Fazio (17/11).**~~ **Respondida el 18/11: D-S6-17**
   (el costo es el «P. Final»; con el real da 323.670,44). Con D-S6-16 el total que se registra
   es el neto sin IVA, y Sergio lo da como **323.670,44**. El sistema registra **323.670,42**: el PDF
   redondea el precio final por unidad («P. Final») y lo multiplica por la cantidad; el sistema aplica
   los descuentos sucesivos al precio de lista y redondea la línea (como en la 056). ¿Se acepta la
   diferencia, o el costo tiene que salir del precio final del proveedor? Mientras tanto, se registra
   lo que calcula el sistema; el lector no marca la línea, porque entra en la tolerancia.~~

La pregunta del IVA del presupuesto de Fazio (17/11) no llegó a esta lista: la respondió D-S6-16.

## Lo que vuelve a Sergio · 18/11/2026 · nota de crédito de proveedor (057)

Las cuatro preguntas del 18/11 (reposición después de un reclamo, anular una factura con notas,
anular una devolución, stock negativo) las **decidió el equipo el 18/11: D-S6-21 a D-S6-25**
(«Decisiones · 18/11/2026»). Se le muestran a Sergio; si corrige alguna, se cambia.

## Decisiones · 18/11/2026

| # | Decisión | Quién | Impacto |
|---|---|---|---|
| D-S6-17 | En Fazio, **el costo de la línea es el «P. Final» del proveedor**, que es lo que se paga, no el recálculo desde el precio de lista. Con el PDF real, el total da **323.670,44**. Cierra la pregunta 5 del 17/11. | Santi | El lector de Fazio pone el P. Final como precio unitario y la línea va sin descuentos (ya están adentro); con más de tres partes ya no se revisa. En el #177 (`a688cba`, `aab80b9`, `e0d6004`), mergeado hacia arriba de la pila |
| D-S6-18 | **Nota de crédito de proveedor: HU nueva en el Sprint 6** (no en el 8): 057, HU-FCO-07, issue #182, 13 SP estimados. Es parte de la factura de compra. El sprint se estira: cierra por entregable (D-PL-06). Reemplaza la propuesta de la D-S6-14 (Sprint 8). | Santi | Proforma, índice y plan. Script 11 (E11). Dos PR: carga manual, después PDF de Expoyer |
| D-S6-19 | **Autocor entrega en papel**: se carga por la vía manual (056). Un escaneo no sirve. **C7 cerrada completa.** | Santi | La factura de Autocor sale de los pendientes |
| D-S6-20 | **El depósito Reclamos es una fila de `sucursal`** con `es_reclamos = true` (a lo sumo una, índice único parcial), con su stock en `stock`, como cualquier ubicación. **La mercadería entra a Reclamos con una transferencia** desde una sucursal (HU-STK-04, 020, Sprint 8): no hace falta otra cosa, porque Reclamos es una ubicación más. Mientras no esté la 020, una devolución sale directo de la sucursal. | agente, 18/11/2026 | Script 11: la columna, el índice, la fila «RC · Reclamos (depósito)» y una fila de stock en 0 por artículo. **Reclamos no es destino de una factura de compra** (CA-008 de la 057): la API lo rechaza y la pantalla no lo ofrece. Sí aparece en las existencias del artículo y como origen de una devolución. Descartado: una tabla de stock aparte para Reclamos, que obligaría a la 020 a tratarla como caso especial |

| D-S6-21 | **Reposición después de un reclamo:** el proveedor la manda con **factura nueva**, que se carga como cualquier factura de compra (056 o 029). La 057 no tiene circuito de reposición. | equipo (práctica de los sistemas de gestión y RG 4540 de AFIP) | Fuera de la 057 |
| D-S6-22 | **No se anula una factura de compra con notas de crédito vigentes:** primero se anulan las notas y después la factura. El mensaje lo dice y nombra las notas. | equipo | CA-011 de la 057, sin «a confirmar». Toca la anulación de la 028 |
| D-S6-23 | **Una nota de crédito se anula igual que una factura (028):** con motivo y con la autorización de otro ADMINISTRADOR con su contraseña. Si era por devolución, la mercadería **vuelve al mismo origen** (sucursal o Reclamos) en la misma transacción. | equipo | CA nuevo de la 057 (CA-012). Script 11 con las columnas de anulación y el número único solo entre vigentes |
| D-S6-24 | **Una devolución que deja el stock del origen en negativo se registra igual**, pero la pantalla avisa antes de confirmar: qué artículo, en qué sucursal y cuánto queda. | equipo | CA nuevo de la 057 (CA-013). Reemplaza el supuesto del 18/11 |
| D-S6-25 | **Regla de cantidades:** en una devolución, cada línea es de un artículo que figura en las facturas referenciadas, y lo devuelto acumulado por artículo no supera lo comprado en esas facturas, sin contar las notas anuladas. Un artículo dado de baja se puede devolver. | equipo | CA nuevo de la 057 (CA-014), con prueba contra PostgreSQL de la suma acumulada y de dos devoluciones simultáneas |

Las cinco son del equipo, tomadas el 18/11 sobre la práctica de los sistemas de gestión comunes y la
RG 4540 de AFIP. **Si Sergio corrige alguna, se cambia.**

**Reglas de Sergio para la 057 (18/11/2026), resumidas** (texto completo en la bitácora del 18/11):
a) la nota de crédito no cambia el costo de los artículos; es plata a favor en la cuenta corriente
con el proveedor, que **el sistema no lleva** (el Mandato deja afuera las cuentas a pagar): se
registran la nota, sus importes y sus facturas, sin saldos; b) dos clases: **bonificación o
diferencia de precio** (solo importes, no mueve stock) y **devolución** (mueve stock; origen por
línea: la sucursal que devuelve o Reclamos); c) toda nota referencia **al menos una factura de
compra del mismo proveedor**, muchos a muchos; d) siempre **tipo A**, el número se copia exacto,
sin 99; e) carga manual obligatoria, y desde el PDF con el mecanismo de la 029 (lector de la nota de
Expoyer), en un segundo PR.

**Mandato:** el Mandato v3.0 deja las notas de crédito fuera de alcance. **El Mandato del documento
de tesis (GR-01) se actualiza; lo hace Santi.**

## Respuestas de Sergio · 17/11/2026

Llegaron el 17/11 (vía Santi). Responden «Lo que vuelve a Sergio · 17/11».

| # | Decisión | Responde | Impacto |
|---|---|---|---|
| D-S6-13 | Lo comprado en negro se carga como comprobante **B** con prefijo **99** y el **número del presupuesto**. | 1 del 17/11 | El texto del PDF de Fazio no trae el número; el nombre del archivo sí («Presupuesto_48437.pdf»): el lector propone `99-48437` y lo deja **para revisar** (un nombre puede cambiarse a mano). El tipo B ya no va a revisión. En el #177 |
| D-S6-14 | Las **notas de crédito de proveedor sí se cargan**. | 2 del 17/11 | **Choca con el Mandato v3.0**, que las deja fuera de alcance, y no hay HU. ~~No se construye: HU nueva pendiente de decisión del equipo, propuesta para el Sprint 8.~~ **Reemplazado por la D-S6-18 (18/11):** la 057 entra en el Sprint 6 |
| D-S6-15 | La línea de la factura de compra tiene **cuatro descuentos sucesivos**, no tres. Los tres primeros son los que traen los proveedores; el cuarto, los **externos por marca o por artículo**. Ejemplo ETMAN: siempre trae el 36 % y además un 5 % que no figura en la factura, porque es por forma de pago; el operador lo carga en la línea. **Reemplaza** la versión pendiente de la misma fecha (dónde iba el 5 % si la línea ya usa los tres). | 3 del 17/11 | **Cambio de esquema:** script 10 (`factura_compra_detalle.descuento_4_porcentaje`, mismo CHECK) + re-scaffold, compuerta E10. CA-012 de la 056 reescrito (proforma v1.1). Toca el costo, el total, la pantalla de la 056 y la precarga de la 049. El lector de ETMAN propone el porcentaje de «Pago Contado N%» en el cuarto descuento, marcado para revisar. PR propio, antes del de Expoyer |
| D-S6-16 | Lo comprado en negro es comprobante B y **ese IVA no se paga**: el IVA que muestra el presupuesto de Fazio se ignora. El total que se registra es **323.670,44**, sin IVA. | pregunta nueva del 17/11 | El sistema ya no suma IVA a un B (CA-014 de la 056, D-S6-11). **Diferencia de 2 centavos** con el PDF real: vuelve a Sergio (pregunta 5 de arriba) |

### Decisiones técnicas · revisión de Santi del 17/11/2026

- **Percepciones de ETMAN.** El texto real (PdfPig) **sí trae** «Percepcion IIBB: 3,920.23» y
  «Percepcion de IVA 2,940.17», en el mismo renglón que el código de barras; «Cordoba : 3920.23» es el
  detalle por jurisdicción. El lector ya daba 3.920,23 con el real. Se corrigió lo que sí fallaba: un
  monto ilegible quedaba null en silencio (ahora va a revisión), y «1.000,50» se leía como 1,00. El
  sintético imita la forma del texto real. Se lee el total y no el detalle: con varias
  jurisdicciones, el total es la suma.
- **Proveedor elegido a mano después del PDF.** `POST /api/facturas-compra/pdf` acepta un
  `proveedorId`: la pantalla vuelve a mandar el mismo archivo con el proveedor elegido, y las líneas
  sin resolver cuyo código aparece en `articulo_codigo_alternativo` de ese proveedor pasan a líneas.
  Con el formulario del PDF, elegir el proveedor ya no pisa el tipo del comprobante con el habitual.
- **Cuarto descuento del PDF.** Se marca con una clave propia por línea, `lineas[i].descuento4`, y la
  pantalla lo señala en su celda: marcar la línea entera ocultaría la marca de «el total no cierra».
  El porcentaje sale del renglón «Pago Contado N%» del PDF, no de un número fijo en el código.
- **Lector de Expoyer.** El código de la línea son los tokens del principio que tienen un dígito o
  son una sola letra (atajo con su límite comentado en el código: un código de dos letras sin dígitos
  se corta mal y la línea queda sin artículo). Sin vencimiento en el texto, no se calcula. Sin una
  factura real con percepciones, si el texto las menciona las dos van a revisión.
- **Orden de los PR:** #176 → #177 → #178 → #179 → cuarto descuento → Expoyer, cada uno apilado sobre
  el anterior.

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

- ~~Los tres descuentos de una línea son **sucesivos** (10 + 5 = 14,5 %).~~ **Confirmado el
  17/11/2026 con las facturas reales de ETMAN y Fazio** (ver «Compuerta C7»).

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
   corregir por el equipo**. **14/11/2026:** alineada con D12, D13 y D14 en un PR de documentación aparte (pedido de Santi del 14/11).
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

**Notas de crédito de proveedor:** ya no están fuera: entraron al Sprint 6 el 18/11/2026 como la 057
(D-S6-18, #182).

**Scrapes de proveedores (Sprint 7, D-PL-02):** los de **Expoyer, Autocor, Nafazio (Fazio) y ETMAN
(Link3)** arrancaron el **17/11/2026**, a mano, en n8n, por Javi, Lauti y Santi, fuera de la cuenta de
SP. Las credenciales de los portales van en el almacén de credenciales de n8n (RNF-007), nunca en el
repo. **El destino de sus datos es la compuerta C2 del Sprint 7** (`proveedor_articulo`).

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

**18/11 (tarde) · D-S6-21 a D-S6-25 del equipo cargadas; la 057 sigue frenada por E11 (`nc057_fuente`).**
- Proforma 057 v1.1 (14 CA): CA-011 sin «a confirmar», CA-012 anulación, CA-013 aviso de stock negativo, CA-014 cantidades.
- Script 11 (sin aplicar): `estado` y columnas de anulación en `nota_credito_compra`, número único solo entre vigentes
  (`ux_nota_credito_compra_numero_activa`), motivo `TEXT`; la verificación espera 12 restricciones.
- Hecho sin esquema, en `feature/182-nota-credito`: CA-014 en el registro (dentro de la transacción; el repositorio tiene que
  bloquear las facturas con `FOR UPDATE`), CA-012 caso de uso de anulación (reusa la doble certificación de la 028, ahora en
  `Comun/DobleCertificacion`), CA-013 aviso en la pantalla.
- **Falta, con el 11:** CA-011 / D-S6-22 en la anulación de la factura (necesita el repositorio de notas en el DI de la 028);
  `ObtenerCompradoYDevueltoAsync`, `ObtenerParaAnularAsync` y `AnularConEfectosAsync` del repositorio, con PostgreSQL: suma
  acumulada, dos devoluciones simultáneas, stock que vuelve al origen y número liberado; endpoint y pantalla de la anulación.

**18/11 · novedades del 18/11 hechas; lo que queda es de personas o espera el script 11.**
Orden de merge de la pila: #176 → #177 → #178 → #179 → #181 → #180; la 057 va arriba (`feature/182-nota-credito`, y encima
`feature/182-nota-credito-pdf`).
- **E10 cerrada:** el #181 puede entrar a `develop` cuando Javi lo revise.
- **D-S6-17 (Fazio, P. Final):** en el #177, verificadores APTO, mergeada hacia arriba con suites sobre cada HEAD (tablero).
- **C7 cerrada completa (D-S6-19).** Autocor, carga manual.
- **057 (#182):** proforma, issue, índice, plan (D-PL-06), decisión de Reclamos (D-S6-20) y script 11. Lo que no depende del
  esquema está hecho en las dos ramas (tablero). **Cuando Santi diga «nc057_fuente: TODO OK»** (plantilla en `lacentral_test`, bitácora «PARA SANTI · 18/11 ·
  plantilla nc057_fuente»; `public` no cambia): re-scaffold, y la suite de la 057 con `LA_CENTRAL_TEST_SCHEMA=nc057_fuente`;
  `NotaCreditoCompraRepositorio` (cabecera, relación con facturas, líneas, stock del origen) con relectura de la fila con un
  contexto nuevo, que el costo y el historial no cambian, y las restricciones del 11 (`nccf_factura_mismo_proveedor`,
  `ux_nota_credito_compra_numero_activa`, `nccd_cantidad_positiva`); `NotasCreditoCompraController` (`POST /api/notas-credito-compra`, `/calculo`,
  401/403); CA-008 (Reclamos fuera de los destinos de la factura: API y pantalla); verificadores y PR 1. Después, en la rama del
  PDF: `POST /api/notas-credito-compra/pdf`, la precarga en la pantalla, verificadores y PR 2.
- **Sergio:** mostrarle D-S6-21 a D-S6-25 (si corrige alguna, se cambia) y la 4 del 17/11 (028 y 016).
- **Santi:** el script 11 (cuándo, por el efecto en las suites) y el Mandato del documento de tesis (GR-01).
- **Javi:** revisar y mergear la pila; DoD 10 con Sergio. **El sprint no está cerrado.**
- Deuda vista en una mutación (no es de este trabajo): en el lector de ETMAN, sacar `numero` de `Revisar` no pone ningún test
  en rojo.

**18/11 · el cuarto descuento cerró con verificadores APTO y tiene PR; Expoyer lo tiene mergeado.**
Orden de merge: #176 → #177 → #178 → #179 → cuarto descuento → Expoyer (#180).
- **Cuarto descuento (D-S6-15):** PR #181 abierto, base `feature/51-pantalla-pdf` (tablero). **Antes del merge a `develop`:** Santi
  aplica el 10 en `lacentral_demo` y en `lacentral` (E10, paso 4 del bloque «PARA SANTI · 17/11 · script 10»).
- **Expoyer (#180):** `feature/163-cuarto-descuento` mergeada en `feature/13-lector-expoyer` (`3e8358c`); backend 681/681 sobre `3e8358c`.
- Lo demás, igual que el 17/11 (abajo): Sergio (preguntas 4 y 5), equipo (notas de crédito), Lauti (Autocor), Javi con
  Sergio (DoD 10). **El sprint no está cerrado.**

**17/11 noche · arreglos de la revisión de Santi, con verificadores APTO. Cuatro PR abiertos, y una rama frenada por el esquema.**
Orden de merge: #176 → #177 → #178 → #179 → cuarto descuento → Expoyer. Las ramas están apiladas; ante un cambio, `git merge`
hacia arriba (nunca rebase).
- **#177, #178, #179:** arreglos de los puntos 1, 2 y 3 de la revisión, con verificadores APTO y suites sobre el HEAD (tablero).
- **Cuarto descuento (D-S6-15), `feature/163-cuarto-descuento`, sin PR:** **frenada por la compuerta E10.** Cuando Santi
  diga «10: TODO OK» en `lacentral_test`: re-scaffold, `Descuento4Porcentaje` en el mapeo del repositorio
  (`FacturaCompraRepositorio`), rojo y verde contra PostgreSQL con relectura de la fila (y el CHECK
  `fcd_descuento_4_valido` rechazando 101), los dos verificadores acotados a la rama, y su PR. Hasta entonces el costo usa el
  cuarto descuento pero la fila no lo guarda: **no se mergea**.
- **Expoyer, `feature/13-lector-expoyer`:** PR abierto, apilado sobre el del cuarto descuento. Se revisa ya; se mergea después.
- **Sergio:** pregunta 4 (028 y 016) y 5 (2 centavos de Fazio) de «Lo que vuelve a Sergio · 17/11».
- **Equipo:** la HU de notas de crédito de proveedor (D-S6-14), contra el Mandato v3.0; propuesta Sprint 8.
- **Lauti:** la factura de Autocor (lo único que falta de la C7).
- **Javi con Sergio:** DoD 10 de la 056, la 029 y la 049 contra `lacentral_demo`, con los PDF reales. **El sprint no está cerrado.**

**17/11, cierre de la sesión · la 029 y la 049 tienen PR y verificadores APTO.** Orden de merge: #176
(docs) → #177 (029 parte 1) → #178 (029 parte 2) → el PR de la 049. Están apilados: cada uno apunta a
la rama del anterior, y GitHub los pasa a `develop` cuando entra el de abajo. Lo que queda **no es del
agente**:
- **Javi:** revisar y mergear; DoD 10 de la 056 y de la 029/049 con Sergio, contra `lacentral_demo`, con
  los PDF reales de `~/dev/TESIS/facturas-reales/` (fuera del repo).
- **Santi:** backup de `lacentral` y scripts 07 a 09 (bitácora, «PARA SANTI», 17/11).
- **Sergio:** las cuatro preguntas de «Lo que vuelve a Sergio · 17/11».
- **Lauti:** facturas de Expoyer y Autocor (C7 parcial). Cada una suma un lector en `LayoutsPdf`.

**17/11 noche · 029 parte 1 con PR.** Interpretación del PDF por reglas (ETMAN y Fazio, PdfPig), sin guardar
nada. Rondas del `verificador-tdd`: 1 NO APTO (el orden por X del lector sin test → test del renglón torcido
`8b9edf0`), 2 NO APTO (la partición de `Ordenar` sin test → se borró, `f5b0ebd`), 3 APTO. Auditor APTO.
Siguen, en este orden y sin arrancar nada del Sprint 7: la **parte 2** (`feature/13-factura-pdf-registro`:
suite completa, los dos verificadores, PR) y la **049** (`feature/51-pantalla-pdf`: `verificador-tdd`, el
auditor no aplica, PR). Las ramas están apiladas: cuando una se actualiza, `git merge` en la siguiente.
La base compartida está lenta: cada suite completa tarda unos 45 minutos.

**17/11 · Sprint 6 en serio (prompt de Santi del 17/11).** Javi mergeó todo el 16/11 (tabla «Lo
mergeado»). Fase A, este PR de docs: tablero, índice, plan (Sprint 7 en 56 SP), issues #169, #168 y #33
cerradas, #9, #23 y #163 abiertas con el motivo comentado, `docs/facturas/` en el `.gitignore`.
C7 cerrada parcial, parser por reglas con PdfPig. Sigue: **029 (#13)**, rama `feature/13-factura-pdf`,
y después **049 (#51)**. Para Santi: backup de `lacentral` y scripts 07 a 09 (bitácora, «PARA SANTI»,
17/11). Para Javi con Sergio: DoD 10 de la 056. Para Sergio: las cuatro preguntas del 17/11.

**14/11 · 028 (#9) parte 2, la pantalla de anulación: PR #175. Rama `feature/9-anulacion-pantalla`**: sale de la
026 (`feature/33-consulta-facturas-compra`) con la parte 1 mergeada (`4b59b94`). Depende del #166, el #167,
el #173 y el #174. Agrega «Anular» en cada fila activa de «Compras registradas»: pide motivo, usuario y
contraseña de quien autoriza, y después recarga la misma página. Verificación: `verificador-tdd` NO APTO en
la ronda 1 (4 mutaciones vivas, arregladas en `db36dd2`), APTO en la ronda 2. `auditor-persistencia`: no
aplica, porque el diff de la HU es solo de frontend. Frontend 214/214 sobre `db36dd2`; backend 608/608 sobre
`070027b`. **Es el último PR hasta que Javi revise los que esperan** (instrucción de Santi del 14/11).
- **027 (#34): no se arranca.** La pregunta P7-01 para Sergio («ubicación de ingreso») está en
  `docs/sprint07/ESTADO.md`.
- **Cuando entren el #166 y el #167:** `git merge origin/develop` (nunca rebase) en cada rama abierta, y
  la suite que corresponda sobre cada una.

**14/11 · Sprint 7 adelantado (028, 026 y 027), pedido de Santi.** Siguen en el milestone del
Sprint 7. Cada HU tiene su rama, que sale de `feature/163-frontend`, y su PR depende del #166 y del #167.
- **028 (#9), parte 1, backend:** rama `feature/9-anulacion-factura-compra`. Decisiones de Santi
  del 14/11: hay **doble certificación**, porque la autoriza otro usuario ADMINISTRADOR con su
  contraseña (B.4 del 07), y la anulación la piden ADMINISTRADOR y OPERADOR. Quedaron como CA-006
  y CA-007 de la proforma v1.1. Interpretación pendiente en el CA-002: el costo no se toca. La
  parte 2, la pantalla, va después de la 026. **PR #173** (14/11), milestone Sprint 7: verificador APTO en la ronda 2, auditor APTO, 591/591 sobre `57142f0`.
- Nota del auditor sobre `proveedor_baja_coherente`: **cerrada, no es contradicción.** El 01
  reemplaza los CHECK de `base_completa.sql` por la forma de la D10.

**14/11 · 026 (#33), rama `feature/33-consulta-facturas-compra`** (sale de `feature/163-frontend`;
depende del #166 y del #167): `GET /api/facturas-compra` con proveedor, número y rangos de emisión
y de vencimiento, y la pantalla «Compras registradas», que es un módulo nuevo en el menú. Las
anuladas se muestran marcadas. La 028 parte 2, la pantalla de anulación, se monta sobre esta.
Verificación (14/11): `verificador-tdd` NO APTO en la ronda 1 (3 mutaciones vivas) y en la ronda 2 (1 viva),
APTO en la ronda 3 (`ac6f619`). `auditor-persistencia` APTO. Backend 577/577 sobre `b3d96bb` (después solo
cambió un spec de frontend); frontend 206/206 sobre `ac6f619`. PR #174 contra develop, Sprint 7, `Closes #33`.
Pendiente: el desempate por id cuando dos facturas tienen la misma emisión no tiene test (observación
del auditor, no bloquea).

**14/11 · Revisión de Javi sobre #166 y #167, y cuarta ronda del #167.**
- **#166: APROBADO por Javi (14/11). No se le pushea nada.** Sus dos observaciones quedan como
  deuda en la **#168** (sin milestone): `ModificarProveedorUseCase` pisa
  `TipoComprobanteHabitual` con null si el request no lo trae; el mensaje del tipo B tiene que
  mostrar el ejemplo `99-00001234`.
- **14/11 · #168 en curso** (pedido de Santi del 14/11), rama `feature/168-deuda-proveedor` desde `feature/163-frontend`; PR contra `develop` que depende del #166 y el #167. Un PUT de proveedor **sin** la clave `tipoComprobanteHabitual` es 400 con mensaje en español y la fila no cambia; con la clave en `null` lo sigue vaciando, que es lo que manda la pantalla. El mensaje del B ya traía `99-00001234` desde el #166: queda un test que lo fija. Rojo `5901221`, código `dd4e411`, test del mensaje `cf79afb`. `CicloCompletoIntegracionTests` ahora manda la clave. `verificador-tdd`: NO APTO en la ronda 1 (M6 a M9, el mapeo sin un PUT completo releído) y APTO en la ronda 2 sobre `c8a8ae0`. `auditor-persistencia`: APTO. Suite completa sobre `c8a8ae0`: 564/564. **PR #171.**
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
- **14/11 · #169 → PR #170** (`verificador-tdd` APTO, 201/201 sobre `ca0707d`), en `feature/169-articulo-por-sucursal`, rama desde `feature/163-frontend`. Repetido = mismo artículo **en la misma sucursal**. PR contra `develop`, que se mergea después del #167; cuando entren el #166 y el #167, `git merge origin/develop`, nunca rebase.
- **14/11 · #170, cambio de destino a mano:** el hueco declarado en el PR se cierra (pedido de Santi del 14/11: es la misma regla de la #169). Cambiar el destino de una línea a uno donde ya está el mismo artículo avisa y el `<select>` vuelve al anterior. `verificador-tdd` APTO en la ronda 2 sobre `b7973d6`; 203/203 frontend.
- **14/11 · la 029 (#13) y la 049 (#51) no se arrancan:** la compuerta C7 sigue abierta hasta que lleguen facturas reales de varios proveedores.
- **14/11 · CA-011 con prueba:** spec `a2e7d1f` (solo test; producción ya lo cumplía, sin rojo por la
  rama real, discrimina por mutación); `verificador-tdd` acotado a `a2e7d1f`: **APTO** (5 mutaciones,
  ninguna sobrevive). Frontend completo sobre `8f79668`: 197/197. **#21 cerrada** (CA-003 anulado
  por D-S6-03, entró con el #164). Re-revisión del #167 pedida a Javi: **no se toca nada hasta que
  responda.**

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
