# ESTADO · Sprint 7

**Entregable (23/11/2026):** al cerrar este sprint, una persona puede entrar al sistema y comparar los precios de un
repuesto entre todos sus proveedores —a partir del catálogo que n8n incorpora desde cada scrape y de la vinculación de
sus renglones con los artículos propios—, en la vista de compra o de venta y con el sitio del proveedor a un clic, y
consultar, corregir y anular facturas de compra (anula un administrador, y el costo vuelve al de antes de la factura),
con el IVA discriminado por alícuota, sobre `lacentral_demo`.

Abierto el 23/11/2026. El Sprint 6 **todavía no está cerrado**: falta el detalle del recorrido DoD 10 (Javi con
Sergio). Si ese recorrido encontró algo roto, va antes que todo lo de acá.

---

## Reparto · 25/11/2026

Lo pidió Santi el 25/11/2026, para que cada uno (y su Claude) sepa qué es suyo.

**Santi (su Claude):**
- Script 13 (#202).
- Script 16 y vinculación: 053 (#126) y 054 (#127).
- Comparador: 030 (#16), 031 (#18, en lo que toca al sistema), 032 (#19), 033 (#20), 034 (#35) y 050 (#52).
- Compras: 026, 027 (#34), 028 (#9), 016 (#23) y #195.
- Scrapes y workflows de n8n: fuera del repo.

**Javi (su Claude):**
- Fix E2 (proveedor sin CUIT, ver D-S7-12).
- Fix F F.3 a F.5 (vencimiento por proveedor; usa las columnas del 14, #201).
- La 058 (#194, alícuotas, script 15), cuando Sergio conteste la P7-02.

**Regla:**
- Nadie toca lo del otro.
- `ESTADO` y bitácora: cada uno agrega sus bloques y nunca reescribe los del otro. Los conflictos se resuelven con merge, conservando los dos lados.
- Scripts: el 13 y el 16 son de Santi; el 14 y el 15, de Javi. Si uno necesita tocar una tabla del script del otro, avisa antes.

---

## Tablero · Sprint 7 (milestone «Sprint 7») · 23/11/2026

| Issue | HU | SP | Espera | Estado |
|---|---|---|---|---|
| #128 | 055 · HU-CMP-08 · incorporar al catálogo del proveedor el resultado de un scrape (proforma reescrita, v2.0) | 5 | C2 (script 12, Santi) | Script 12 (datos reales de Expoyer) y `CONTRATO-N8N.md`: 9 pruebas contra PostgreSQL, verificadores APTO. **#199 mergeado (`0633b3b`, 24/11).** El 12 en `lacentral_test` y `lacentral_demo`; `lacentral` con el bloque fish de Santi. Re-scaffold contra `public`: 0 diferencias de modelo |
| #126 | 053 · HU-CMP-06 · candidatos por aproximación entre catálogos | 8 | C2, C5, C6 | Sin proforma (C5) |
| #127 | 054 · HU-CMP-07 · vinculación manual con aprendizaje | 8 | C2, C5 y **el CA3 contra D2 (equipo)** | Sin proforma (C5). **No se construye** hasta que el equipo decida el CA3 |
| #16 | 030 · HU-CMP-01 · búsqueda comparativa por nombre | 5 | C2 y filas de catálogo | Pendiente |
| #18 | 031 · HU-CMP-02 · consulta en vivo a los sitios de proveedores | 13 | C2 | Pendiente. Los workflows de n8n los arma Santi, fuera de SP (D-PL-02) |
| #19 | 032 · HU-CMP-03 · vista Compra | 3 | C1 | Pendiente |
| #20 | 033 · HU-CMP-04 · vista Venta | 3 | C1 | Pendiente |
| #35 | 034 · HU-CMP-05 · redirección al sitio del proveedor | 1 | — | Pendiente |
| #52 | 050 · HU-UI-12 · pantalla del comparador | 8 | 031; su CA-004, C1 | Pendiente. No se parte (D-PL-03) |
| #34 | 027 · HU-FCO-03 · modificación acotada de factura de compra | 2 | P7-01 (Sergio) | **Sigue bloqueada:** la D-S6-29 no responde la P7-01 (abajo) |
| #9 (y #23) | 028 · HU-FCO-04 CA-002 y 016 · HU-ART-06 CA-004 · costo al anular (D-S7-03) | 3 | Script 13 | 028 v1.2 y 016 v1.1. #9 y #23 siguen abiertas hasta este cambio |
| #195 | D-S7-04 · anulación con un administrador (028 CA-006 y CA-007, 057 CA-012) | 2 | Script 13 | 028 v1.2 y 057 v1.3 |
| #194 | 058 · HU-FCO-08 · IVA por alícuota en la factura de compra y la nota de crédito (D-S7-02) | 8 | Script 15 y una pregunta a Sergio (abajo) | Proforma v1.0 |

**69 SP** (los 56 del plan + 3 + 2 + 8). Contra unos 34 por sprint: **el sprint se estira, no se recorta** (§1 de
`CLAUDE.md`, D-PL-01). Los SP de la 053, la 054, la 058 y de los cambios de D-S7-03 y D-S7-04 son estimación.

## Compuertas · cada una bloquea solo las HU de su fila

| Compuerta | Bloquea | Estado |
|---|---|---|
| ~~Qué entrega un scrape~~ | 055 | **Cerrada el 23/11/2026 por la D-S7-01:** n8n escribe directo en la base |
| **C2** · catálogo de proveedores (script 12: `scrape_corrida`, `proveedor_articulo`, `proveedor_articulo_historial`, rol `lacentral_n8n`) | 055, 053, 054, 030, 031, 050 | Script escrito con los datos de Expoyer y ensayado; contrato en `docs/sprint07/CONTRATO-N8N.md`; **lo aplica Santi** (bitácora, «PARA SANTI · 23/11 · script 12») |
| C5 · proformas de la 053 y la 054 (con la pantalla de vinculación) | 053, 054 | Sin escribir. El CA3 de la 054 choca con la D2: lo decide el equipo antes |
| C6 · medición sobre 50 artículos | 053 | Con los primeros scrapes de Santi |
| C1 · modalidad L2 y retiro de Tarjeta3/6 | 032, 033 y el CA-004 de la 050 | **Propuesta (para el equipo):** no va en el 12. El plan la ponía junto con C2, pero C2 destraba los scrapes y no tiene que esperar el cambio de modalidades, que además toca código de precios. Script propio cuando arranquen la 032 y la 033 |
| E13 · script 13 (costo previo por línea, D-S7-03; anulación sin doble certificación, D-S7-04) | #9, #23, #195 | Escrito y ensayado (25/11/2026) en `feature/s7-script-13`: `13: TODO OK.` en un esquema descartable de `lacentral_test` (borrado después), idempotente, con una guarda nueva (corta si hay facturas anuladas sin quién las anuló). 11 pruebas PostgreSQL (`Script13PostgreSqlTests`) que corren el archivo tal cual. **Lo aplica Santi** en test, demo y la real (con backup); el bloque fish está en `docs/sprint07/BITACORA.md`. Después: re-scaffold en PR aparte |
| E15 · script 15 (alícuotas, D-S7-02) | #194 | Pendiente de escribir; espera la pregunta P7-02 a Sergio |
| E14 · script 14 (vencimiento de facturas por proveedor, Fix F) | #197 | Mergeado a `develop` (`54569b8`, 25/11/2026), antes que el #200, que quedó con conflicto en la bitácora y se resolvió con merge (`4300cd5`). Aplicado por Santi (25/11) en `lacentral_test`, `lacentral_demo` y `lacentral`, con «14: TODO OK.» en las tres (backup de la real antes, en `~/dev/TESIS/backups/lacentral-antes-14.dump`). Re-scaffold en `chore/rescaffold-14` (`99d6320`): solo `VencimientoModo` y `VencimientoDias` en `Proveedor` y su configuración. Era el «12» del #197, renumerado el 24/11/2026 (decisión de Santi: el 12 es C2) |

**Por qué tres scripts (12, 13 y 15) y no uno:** (el 14 es el Fix F del #197, otro tema) el 12 solo crea tablas nuevas y destraba a Santi hoy; no espera a
nadie. El 13 junta dos cambios chicos y ya definidos sobre las tablas de la anulación (`factura_compra_detalle`,
`nota_credito_compra`), con un solo re-scaffold. El 15 (alícuotas) toca artículo, líneas, nota de crédito, lectores de
PDF y pantallas, y tiene una pregunta abierta sobre la nota de crédito: meterlo en el 13 frenaría lo que ya está
decidido. El número 06, reservado para C2 desde el Sprint 4, queda sin usar.

## Decisiones · 23/11/2026

| # | Decisión | Quién | Impacto |
|---|---|---|---|
| D-S7-01 | **Scrapes:** los arma el equipo en n8n, y n8n **escribe directo en la base**. Responde «qué entrega un scrape». | Equipo, 23/11/2026 | Cierra esa compuerta. C2 (script 12) da las tablas y un rol `lacentral_n8n` con permisos mínimos. La 055 se reescribe (v2.0). Los workflows de Expoyer, Autocor, Nafazio (Fazio) y ETMAN los arma Santi |
| D-S7-02 | **IVA por alícuota** (cierra C7-01, entra): 21 % y 10,5 % son tipos de alícuota. La alícuota suele ser del artículo (valor por defecto), pero en cada línea de la factura de compra se puede cambiar con un desplegable. El IVA se discrimina y se suma por alícuota en el total. Sergio pasa más alícuotas después: el diseño no supone solo dos. La nota de crédito hereda la alícuota de la línea de la factura original. | Equipo, 23/11/2026 | 058 nueva (#194). Hoy está fija en 21 % en `RegistrarFacturaCompraUseCase`, `RegistrarNotaCreditoCompraUseCase`, el lector de ETMAN y el control del neto de Expoyer. Script 15 |
| D-S7-03 | **Costo al anular** (cierra C7-02, entra): al anular una factura, el costo del artículo vuelve al que tenía justo antes de esa factura. Si entre la factura y la anulación alguien lo cambió a mano, queda el de a mano. Exige guardar el costo previo en cada línea al registrarla. | Equipo, 23/11/2026 | Reemplaza la interpretación del CA-002 de la 028 (v1.2) y el CA-004 de la 016 (v1.1). Script 13. #9 y #23 |
| D-S7-04 | **Anulación con UN administrador** (cierra C7-03, entra): se saca la doble certificación. Anula un ADMINISTRADOR, con motivo obligatorio, y queda registrado quién anuló. **Revierte el pedido de la Dirección del 02/11/2026 (B.4 del script 07); Santi avisa a quien lo pidió.** | Equipo, 23/11/2026 | Cambia el CA-006 y el CA-007 de la 028 (v1.2: un OPERADOR ya no pide la anulación), el CA-012 de la 057 (v1.3) y `Comun/DobleCertificacion`. Script 13 (el CHECK `ncc_anulacion_coherente` hoy exige quien autoriza). #195 |
| D-S7-05 | **Numeración de los scripts** (choque de dos «12»): el **12** es el catálogo de proveedores (C2, #199), que está verificado y es el que necesita el scrape. El **13** es la anulación (D-S7-03 y D-S7-04). El Fix F del #197 (vencimiento de facturas por proveedor) pasa a ser el **14**: C2 no lo cubre, porque su `vencido` es del renglón del catálogo, no de la factura. Las alícuotas (D-S7-02) pasan al **15**. | Santi, 24/11/2026 | #197 renumerado en `3eef61b`; comentario en el #197 |
| D-S7-12 | **Factura sin CUIT** (decisión de negocio de Javi): «una factura sin CUIT es remito o presupuesto: compra en negro». Para el Fix E2 queda sugerido usar `proveedor.cuit_simulado`: la única coincidencia por nombre con `cuit_simulado = true`, marcada «Revisar». Lo decide e implementa Javi. | Javi, 25/11/2026 | Fix E2 (Javi). Los números D-S7-06 a D-S7-11 los usan las ramas de Santi todavía sin mergear (#202 y la del 16) |

### Primer scrape · Expoyer · relevado el 23/11/2026

Portal con API JSON y token Bearer. Lo que entró al diseño de C2 (script 12 y `CONTRATO-N8N.md`):

- **Volumen:** unos 103.900 productos en 194 rubros. El buscador corta en 10.000 por consulta: n8n baja rubro por
  rubro, de a 500 (unos 400 pedidos por corrida); el único rubro de más de 10.000 se baja ordenado en los dos sentidos
  y el upsert absorbe los repetidos. **Unos 100.000 renglones por proveedor.** Todo lo que corre por renglón usa la
  clave `(proveedor_id, codigo)` o el id; el vencimiento recorre el catálogo del proveedor una vez por corrida.
- **Stock:** no viene cantidad, viene un **estado por depósito** del proveedor (CASEROS y CORDOBA): 1 = hay, 3 = no hay,
  2 = a confirmar. Se guarda el de **CORDOBA** como `HAY`, `NO_HAY` o `A_CONFIRMAR`; la cantidad queda NULL.
- **Precios:** lista (`basePrice`) y neto con el descuento del cliente (`discountedPrice`), validado contra la factura
  real: lista × (1 − 61,08 %) × (1 − 15 %). Pago anticipado (`earlyPaymentPrice`), opcional. **`salePrice` (neto + 80 %
  de la vista cliente) no se guarda.**
- **Códigos:** `commercialCode` («C 000100 14 E», el de las facturas) es el código y se cruza con
  `articulo_codigo_alternativo`; `internalCode` («C00010014E») es el id externo.
- **Además:** número original (`familyCode`, varios con « / »), marca del producto (`supplier.name`; `brandName` es la
  marca del vehículo), rubro del proveedor (`category.name`), URL de imagen, IVA (`vatRate` viene null: queda NULL y
  el comparador usa la alícuota del artículo o 21 %, D-S7-02).

**P7-03 · para Sergio · 23/11/2026 · abierta:** ¿qué quiere decir el estado 2 de stock de Expoyer? ¿«pocas»? Hasta que
conteste se guarda como `A_CONFIRMAR`.

**Riesgo (para el equipo):** el historial crece unos 100.000 registros por corrida y por proveedor (RF-016 pide uno por
corrida). Con varios proveedores y corridas nocturnas son decenas de millones por año en una base que corre en una
netbook. No se resuelve en C2: se mide con los primeros scrapes. Opción si hace falta: registrar solo cuando cambian el
precio o el stock.

**Pendiente del equipo:** el CA3 de la 054 (#127) contra la D2 (dos códigos del mismo proveedor para el mismo
artículo). **La 054 no se construye hasta que se decida.**

### P7-01 contra la D-S6-29 · revisión del 23/11/2026

La D-S6-29 dice que la mercadería entra al depósito de la sucursal que compró, y que eso se define **en la carga** de
la factura. Confirma la interpretación (a) de la P7-01 —la «ubicación de ingreso» es la sucursal de destino— **pero no
responde la pregunta**, que es sobre **corregirla después** de registrada. **La 027 sigue bloqueada.** Falta que Sergio
diga:

1. si se corrige línea por línea o una sucursal para toda la factura;
2. si en la sucursal original ya se vendió parte, si se permite que quede en negativo (como en la anulación, 028 CA-003);
3. quién la corrige: ¿alcanza con OPERADOR, o solo ADMINISTRADOR? (Con la D-S7-04 ya no hay doble certificación.)

Con (a), corregir mueve stock entre sucursales: la 027 hay que reestimarla (hoy 2 SP).

### P7-02 · 058 · nota de crédito por bonificación con dos alícuotas · 23/11/2026 · abierta

La nota de crédito guarda un solo importe neto, sin precios por línea (la de devolución tiene artículos y cantidades,
no importes). «Hereda la alícuota de la línea de la factura original» se resuelve en una devolución, pero no en una
**bonificación** sobre una factura con líneas al 21 % y al 10,5 %: **¿cómo se reparte el neto entre las alícuotas?**
¿El comprobante del proveedor ya trae el neto discriminado por alícuota? Sin esa respuesta el script 15 no se escribe.

---

## Construido antes de tiempo · 17/11/2026

Construidas el 14/11 (pedido de Santi) y mergeadas por Javi el 16/11/2026. Salen de la cuenta de
SP del Sprint 7 (`docs/PLAN-SPRINTS-6-7-8.md`: 67 → 56).

| Issue | HU | SP | PR | Estado |
|---|---|---|---|---|
| #33 | 026 · HU-FCO-02 · consulta de facturas de compra | 3 | #174 | **Terminada.** Los 5 CA con test (`ConsultarFacturasCompraPostgreSqlTests`, pantalla «Compras registradas»). Issue cerrada el 17/11 con referencia al #174. Observación no bloqueante del auditor: el desempate por id con la misma emisión no tiene test |
| #9 | 028 · HU-FCO-04 · anulación de factura de compra | 8 | #173 (backend) y #175 (pantalla) | **Construida. Issue abierta:** el CA-002 está probado con una interpretación pendiente de Sergio («conserva su precio de costo previo» = el costo no se toca). Pregunta en «Lo que vuelve a Sergio · 17/11» del `docs/sprint06/ESTADO.md` |

La 028 **cierra el CA-003 de la 016 (#23)**; el CA-004 de la 016 queda con la misma interpretación
pendiente que el CA-002 de la 028, y la #23 sigue abierta hasta la respuesta.

Recorrido DoD 10 de la 026 y la 028 contra `lacentral_demo`: sin hacer, va con el del Sprint 7.

---

## Preguntas para Sergio

### P7-01 · 027 (#34) · ¿qué es la «ubicación de ingreso» que se puede corregir? · 14/11/2026 · abierta

La 027 **no se arranca** hasta que Sergio conteste (decisión de Santi del 14/11).

**Frase textual de la proforma** (`docs/historias/027-HU-FCO-03.md` v1.0):

> **CA-001** · Dado que la factura está activa · cuando el usuario modifica la ubicación de
> ingreso · entonces el sistema guarda el cambio

> **Notas** · La ubicación es editable justamente porque no proviene del comprobante sino de una
> selección manual durante la carga (HU-STK-02).

La 018 (HU-STK-02) usa la misma frase para la **sucursal** que recibe la mercadería: «el sistema
incrementa la existencia de cada artículo del detalle en esa ubicación»; «si la factura ingresa
mercadería a San Vicente […] no modifica la existencia de Fragueiro».

**Interpretaciones:**

- **(a) La sucursal de destino de la mercadería (San Vicente o Fragueiro).** Es lo que dice la
  018. Desde la D9, el destino se elige **por línea** (`factura_compra_detalle.sucursal_id`), no por
  factura. Corregirlo no es un dato administrativo: hay que mover stock que ya entró, restarlo en
  una sucursal y sumarlo en la otra. Para eso, Sergio tiene que contestar:
  - ¿se corrige línea por línea, o una sucursal para toda la factura?
  - si en la sucursal original ya se vendió parte, ¿se permite que quede en negativo, como en la
    anulación (028 CA-003)?
  - ¿alcanza con OPERADOR, o pide la doble certificación de la anulación (028 CA-006)?
- **(b) La ubicación física en el depósito** (estante, pasillo: `stock.ubicacion_deposito`, hasta
  60 caracteres). No es un dato de la factura sino del artículo en cada sucursal, y ya se edita con
  `PUT /api/articulos/{id}/ubicaciones/{sucursalId}`. Con esta lectura, el CA-001 no agrega nada, y
  la 027 queda solo con los rechazos (CA-002 a CA-004).

**Lectura del agente:** la (a), porque la nota remite a la 018. Pero la D9 la vuelve un movimiento
de stock, más cerca de la 020 (transferencia entre ubicaciones, 8 SP) que de los 2 SP de la 027.
Si es la (a), la 027 hay que reestimarla.

---

## Candidatas desde el cierre del Sprint 6 · 23/11/2026

Salen de lo que Sergio contestó (relevado por Javi el 23/11/2026) y de lo que quedó pendiente al
cerrar el Sprint 6. **Ninguna está decidida ni estimada**: no tienen SP, y no entran al sprint hasta que el
equipo las decida. Se registran; no se implementan. **C7-01, C7-02 y C7-03 las decidió el equipo el 23/11/2026 (D-S7-02 a D-S7-04)
y entran al sprint**; C7-05 y C7-06 siguen como candidatas.

### C7-01 · IVA 10,5 % (artículos de electricidad) · **cerrada por la D-S7-02 (23/11/2026): entra como 058, #194**

- **Qué dijo Sergio:** hay artículos de electricidad con IVA 10,5 %.
- **Hoy:** la tasa 21 % está fija en el código (`TasaIva = 0.21m` en `RegistrarFacturaCompraUseCase` y en
  `RegistrarNotaCreditoCompraUseCase`). El lector de ETMAN y el control del neto de la nota de crédito de Expoyer
  también asumen 21 %. El esquema **no tiene alícuota** por artículo ni por línea.
- **Decisión de equipo pendiente:** dónde vive la alícuota, en el artículo o en la línea de la factura. Lleva un
  script 12 (esquema + re-scaffold). La nota de crédito tendría que heredar la alícuota de la factura original.
- **Respuesta de Sergio (vía Javi, 23/11/2026):** la alícuota suele ser del producto, pero siempre puede variar, así que
  tiene que haber un desplegable con los tipos de alícuota. Si la factura tiene dos artículos, uno con la general (21 %) y
  otro con la diferencial (10,5 %), el IVA de ambos se suma en el total (se discrimina por alícuota). Hay más tipos, pero hoy
  se usan 21 y 10,5: **Sergio pasa las otras cuando las tenga**. Sigue pendiente la decisión de equipo (artículo con
  posibilidad de cambiarla en la línea, o solo línea) y el script 12.

### C7-02 · Costo al anular una factura · **cerrada por la D-S7-03 (23/11/2026): entra, #9 y #23**

- **Qué dijo Sergio:** al anular, el costo vuelve al anterior a la factura. Ejemplo: costo 1450, factura a 1500, se
  anula: 1450.
- **Hoy:** `AnularConEfectosAsync` vuelve al costo del último registro válido de `articulo_historial_compra`; si no hay
  registro anterior (valor importado o cambio manual), conserva el actual.
- **Para cumplir el ejemplo en todos los casos** habría que guardar el costo previo en cada línea de la factura al
  registrarla (script 13; el 12 es el catálogo de proveedores, C2).
- **Respuesta de Sergio (23/11/2026) sobre el cambio a mano:** si el costo se cambió a mano entre la factura y la anulación,
  **queda el que se puso a mano** (no se vuelve al anterior a la factura).
- **Regla al anular (Sergio, 23/11/2026; D-S7-03 en el #198):** el costo **vuelve al de justo antes de la factura**,
  salvo que lo hayan cambiado a mano después de la factura: en ese caso **queda el de a mano**. Es lo que quiere decir
  «costo a mano se respeta» en la bitácora del #192.
- **Pendiente:** el esquema (la regla ya está completa).
- Reemplaza la «interpretación pendiente» del CA-002 de la 028 (`docs/historias/028-HU-FCO-04.md`, actualizado el
  23/11 con la respuesta; el código no cambia). El CA-004 de la 016 (#23) tenía la misma interpretación.

### C7-03 · Anulación con un solo administrador · **cerrada por la D-S7-04 (23/11/2026): entra, #195**

- **Qué dijo Sergio:** «con uno solo sí se puede anular».
- **Choque:** el CA-006 de la 028 exige que autorice **otra** persona con rol ADMINISTRADOR, distinta de quien pide: rechaza la
  autoautorización (pedido de la Dirección del 02/11/2026). Con un solo administrador, la anulación no se puede hacer.
- **No se cambia el código en esta tarea.**
- **Respuesta de Sergio (vía Javi, 23/11/2026):** **el mismo administrador puede anular y autorizarse solo.** Contradice el
  CA-006 y el pedido de la Dirección del 02/11/2026 (doble certificación): **lo decide el equipo/Dirección** antes de tocar
  el CA-006 y su código.

### C7-05 · H1 · vencimiento según proveedor y H2 · reconocimiento de proveedor y sucursal en PDF

Ya descriptas en la spec de correcciones del Sprint 6. Quedan como candidatas. **Sergio todavía no contestó (23/11); falta:**

- la tabla de condiciones de pago pasada a texto;
- qué significa «quincena», y la fecha base;
- cómo se marca el vencimiento manual (Fric-Rot, Etman);
- los PDF reales de Fragueiro y San Vicente, y los números de cuenta.

### C7-06 · Deuda técnica menor

- Mover `LectorTextoPdf` (PdfPig) de `Api` a `Data` y loguear su `catch`.
- Mover `NormalizacionArticulo.Palabras` y la cadena de `Replace` de acentos (repetida en tres repositorios) a un lugar
  común.
- Limitar los intentos de contraseña en el login y en la anulación.
- Unificar el parser de fechas abreviadas en las demás pantallas con fechas.

## Fuera de la tesis · Sprint 9 · post-tesis

**Decisión del equipo (Javi y Santi, 23/11/2026), D-S6-30:** lo de esta sección queda **fuera de la tesis**. Se construye
después de la defensa, como mejora para el negocio. No entra a los Sprints 7 ni 8.

### S9-01 · Módulo de devoluciones a proveedor (Reclamos, alerta de pendientes, PDF) · antes C7-04

- HU nueva, con esquema nuevo. Depende de la 020 (transferencias, Sprint 8): la mercadería entra a Reclamos con una
  transferencia (D-S6-20).
- **Ya existen:** el depósito Reclamos, la nota de crédito con líneas devueltas, su anulación y la reposición con factura
  nueva (D-S6-21).
- **Respuestas de Sergio (vía Javi, 23/11/2026):**
  - motivos: el proveedor mandó roto o mal; nosotros pedimos mal; el cliente pidió mal;
  - dos salidas: **devolución** y **nota de crédito**;
  - la alerta la ve el ADMINISTRADOR y se muestra siempre: desde 0 días sin nota de crédito hasta que se cargue;
  - **no se cierra con una nota de crédito parcial:** es una devolución y se espera la nota de crédito.
- **Todavía abiertas para la proforma:** estados exactos; qué es el PDF (remito de devolución o pedido de reclamo), con qué
  datos y cómo se envía; el depósito **«Vélez»**, que Sergio nombró como origen de las devoluciones y que no existe en
  el sistema (¿es una sucursal, otro nombre de un depósito existente o un depósito nuevo?).

---

## PRÓXIMA SESIÓN

**25/11/2026 · arranque del Sprint 7 · script 13.**
- **Bitácora nueva:** `docs/sprint07/BITACORA.md` (lo anterior del Sprint 7 está en la del Sprint 6).
- **#201 (re-scaffold del 14) sigue abierto:** no se toca `Proveedor` hasta que entre. Fix E2 y Fix F.3–F.5 son de Javi; los workflows de n8n y el scrape, de Santi (fuera del repo).
- **Script 13** en `feature/s7-script-13`: ensayado, con pruebas, en PR (ver la fila E13). Espera que Santi lo aplique en las tres bases; después, re-scaffold en PR aparte, y recién ahí la 028 y la 016 (D-S7-03) y el #195 (D-S7-04) en código.
- **Sigue:** la 053 y la 054 (vinculación automática por código, después candidatos por pg_trgm). La 054 tiene el CA3 contra la D2 sin decidir: se pregunta antes de construirla.
**27/11/2026 · Fix «Enter sobre un código exacto lo elige» (Javi): PR #205 abierto contra `develop`, esperando revisión de Santi. Tarea terminada de este lado.**
- Rama `fix/enter-codigo-exacto` (merges de `develop`: `de068fd` y `5be5732`; al día con `bda2692`). Solo frontend, sin backend ni esquema. Bitácora: `docs/sprint06/BITACORA.md`, bloque del 25/11.
- Commits: `cca1741` (la regla pasa a `compartido/`, sin cambio de comportamiento; su spec es de caracterización), `591a4d4` (specs en rojo) y `0676f4f` (Enter elige en la NCA, compras registradas y notas registradas); `7708097` (rojo) y `59f21ba` (la NCA no agrega dos veces un artículo sin origen).
- Sobre `59f21ba` (26/11): suite de frontend `TOTAL: 408 SUCCESS`, solo Chromium; `ng build` sin advertencias; las dos pruebas de backend del orden «exacto primero», contra `lacentral_test`: 2/2 (no es la suite de backend).
- **27/11 · `verificador-tdd`:** ronda 1 NO APTO (4 mutaciones sin cubrir), arreglado solo con pruebas en `60ccf7e`; ronda 2 **APTO**. Suite de frontend sobre `60ccf7e`: `TOTAL: 417 SUCCESS`, solo Chromium; `ng build` sin advertencias.
- **27/11 · doble Enter en el proveedor de la NCA** (pedido por Javi): `b171fd9` (rojo) y `d01c064` (guarda). `verificador-tdd`: ronda 3 NO APTO (sobrevivía la mutación M4, la guarda comparando por referencia en lugar de por id, porque el spec devolvía el mismo objeto), arreglado en `b1ddce0`, solo pruebas; reverificación **APTO**. Suite de frontend sobre `b1ddce0`: `TOTAL: 419 SUCCESS`, solo Chromium; `ng build` sin advertencias.
- **Deuda de este fix (no bloqueante):** `quitarLinea` y `cambiarProveedor` no limpian el aviso de repetido de la NCA. Por lectura, sin probar: dos búsquedas de proveedor que respondan en orden inverso dejan el primero; las facturas de un proveedor que lleguen tarde se muestran bajo el elegido después.
- **Anterior a este fix:** la NCA no tiene botón para reintentar las facturas (se reintenta con «Cambiar»); en la factura de compra un segundo Enter vuelve a proponer el comprobante habitual.
- **Para el cuerpo del PR, comportamiento que cambia aparte del Enter** (confirmado por Javi, 26/11): en la NCA el clic tampoco agrega dos veces un artículo mientras la primera línea no tiene origen.
- **A confirmar (sin implementar):** normalizar los códigos de proveedor a 5 dígitos; control de repetido al **cambiar el origen** de una línea en la NCA.
- **Sin verificar:** el recorrido en el navegador (no figura en la bitácora).
- **Abierto:** revisión de Santi del #205, sin merge de este lado (`auditor-persistencia` no aplica); el recorrido de DoD 10 en el navegador contra `lacentral_demo`; el conflicto en este archivo entre #204 y #205, que se resuelve mergeando `develop` en la rama que entre segunda y conservando los dos bloques.
**27/11/2026 · Fix E2: PR #204 abierto, esperando revisión de Santi. Cerrado de nuestro lado.**
- **PR #204** (`fix/proveedor-razon-social-exacta` → `develop`), abierto el 27/11, sin merge. Código en `2d008a0`, script de demo en `782a871` y después solo documentación. Verificadores dados por válidos por Javi (detalle en «Fix E2 · implementado según la D-S7-12»).
- **Suite completa sobre `ec3d0ee`** (27/11, 10:35–10:53, pedida por Javi): `Correctas! - Con error:     0, Superado:   864, Omitido:     0, Total:   864, Duración: 17 m 38 s`. Build limpio 0/0; 0 conexiones de otros roles en las muestras de antes y durante.
- **Deuda:**
  - `marcar_en_negro_demo.sql` sin aplicar (lo aplica Javi o Santi).
  - Recorrido de DoD 10 con PDF reales contra `lacentral_demo`, pendiente.
  - Issue de la factura A de Fazio sin abrir (lo decide Javi).
  - Alta en negro: la app no puede dar de alta ni marcar un proveedor en negro.
- El nombre del titular de «cuenta y orden» y los CUIT no entran al repo.

**25/11/2026 · el 14 aplicado en las tres bases y su re-scaffold en PR.**
- **#197 (script 14, Fix F) mergeado (`54569b8`)** antes que el #200; el #200 se destrabó con merge de `develop` y está mergeado (`07fd8e7`).
- **El 14 está aplicado** (Santi, 25/11) en `public` de `lacentral_test`, en `lacentral_demo` y en `lacentral`, con «14: TODO OK.» en las tres. Backup de la real antes: `~/dev/TESIS/backups/lacentral-antes-14.dump`.
- **Re-scaffold del 14** en `chore/rescaffold-14` (`99d6320`) contra `public` de `lacentral_test`: solo `VencimientoModo` y `VencimientoDias` en `Proveedor` y su configuración en `LaCentralDbContext.cs` (+18 líneas, sin cambios de finales de línea). **PR #201.** Suite completa sobre `4064b1e`: **858/858**, 0 advertencias. Sin verificadores (no hay lógica nueva).
- **Sigue:** Fix F.3–F.5 (usar el vencimiento por proveedor), que esperaba el 14 aplicado y este re-scaffold.

**24/11/2026 (tarde) · el #198 entró por el #199.**
- **El #198 se mergeó a una rama muerta (24/11/2026):** lo mergearon contra `docs/sprint06-cierre-correcciones` (`8521ae7`),
  cuando el #193 ya había entrado a `develop`, así que sus commits no llegaron a `develop`. Entran por el #199:
  `origin/docs/sprint07-apertura` mergeada en `feature/128-catalogo-scrape` (`e0f68d5`, sin rebase) y el #199
  retargeteado a `develop`. `git log origin/develop..HEAD` trae los commits del #198.
- **El 12 ya está aplicado** (Santi, 24/11) en `public` de `lacentral_test` y en `lacentral_demo`. El rol `lacentral_n8n`
  ya tiene clave (no se anota). Suite completa sobre `e0f68d5` contra `public` de `lacentral_test`: **858/858**, 0
  advertencias (40 más que la anterior porque entró el Fix E, #196, desde `develop`).
- **#199 mergeado (`0633b3b`).** Re-scaffold contra `public`: 0 diferencias de modelo (`chore/rescaffold-12`, suite 858/858 sobre `f353c36`). Santi tiene el bloque fish de lo que sigue.
- **Falta (Santi, con el bloque fish del 24/11):** el 12 en `lacentral` (con backup antes) y borrar `nc057_fuente`, `c2_fuente` y
  `c2_ensayo_e6807bae` de `lacentral_test`.

**24/11/2026 · C2 en PR (#128, `feature/128-catalogo-scrape`).**
- Script 12, `docs/sprint07/CONTRATO-N8N.md` (las siete sentencias de n8n, de una línea), proforma 055 v2.0. Suite
  completa sobre `d57efa0` contra la copia descartable `c2_ensayo_e6807bae`: **818/818**, 0 advertencias.
  `verificador-tdd` APTO en la ronda 3 (rondas 1 y 2 NO APTO, arreglado solo en test); `auditor-persistencia` APTO en la
  ronda 2 (la 1 encontró que un e) tardío vencía lo leído por una corrida nueva; arreglado con rojo antes).
- **Espera a Santi:** aplicar el 12 (bitácora, «PARA SANTI · 23/11 · script 12»: `c2_fuente` en `lacentral_test`,
  `public` de `lacentral_demo`, clave de `lacentral_n8n`) y armar el workflow con el contrato. Después del merge: el 12
  en `public` de `lacentral_test` y en `lacentral`, re-scaffold contra `public` (tiene que dar 0 diferencias), y se
  borran `c2_fuente` y `c2_ensayo_e6807bae`.
- **Sigue (orden del pedido del 23/11):** D-S7-04 (#195) y D-S7-03 (#9, #23) con el script 13, en borrador en
  `feature/s7-script-13` (`f944258`, sin ejecutar ni probar; exige el 12); después la 058 (#194), que espera la P7-02 y
  el script 15.
- **Numeración (D-S7-05, Santi, 24/11):** 12 = C2 (#199), 13 = anulación, 14 = Fix F (#197, renumerado en `3eef61b`),
  15 = alícuotas.
- **Esperan a personas:** cierre del Sprint 6 (detalle del recorrido de Javi); re-aprobación del #193 (Javi; review
  respondido el 24/11) y merge de #193 → #198 → #199; CA3 de la 054 contra la D2 (equipo); P7-01, P7-02 y P7-03 (Sergio).

---

## Fix E2 · implementado según la D-S7-12 · 26/11/2026

- **La spec del Fix E2 («razón social exacta primero») queda superada por la D-S7-12.** Los bloques anteriores que la describen (`docs/sprint06/ESTADO.md`, 24/11 y 25/11) no se tocan: valen como historia, no como regla vigente.
- **Regla vigente** (`fix/proveedor-razon-social-exacta`, commits `ea6b734`, `8632734` y `00710c0`): elegido a mano > CUIT de un proveedor cargado (un CUIT de proveedor de baja no se reemplaza) > si ningún CUIT del texto pertenece a un proveedor cargado **y el comprobante leído no es A**, el único proveedor ACTIVO con `cuit_simulado = true` cuya razón social contiene el término del layout, siempre «Revisar»; con 0 o 2 o más, nada. Sin cambios de esquema ni de frontend.
- **«No es A» incluye el tipo no reconocido** (código de ARCA que el lector no traduce): hoy se asigna. A confirmar.
- **Datos:** los pares blanco/negro de `cuit_simulado` se verificaron **solo en `lacentral_test`** (25/11, solo lectura). **`lacentral` (la real) está sin verificar.**
- **Demo:** `backend/db/demo/marcar_en_negro_demo.sql` marca **solo a Fazio** (`DEMO-07`) desde `782a871` (cambio de Javi, 26/11/2026: salen Expoyer y ETMAN, este se decide aparte); **sin aplicar** (lo aplica Javi o Santi). Un PDF de ETMAN no asigna proveedor por razón social.
- **Deuda (va también en el cuerpo del PR):** un proveedor en negro no se puede dar de alta ni marcar desde la app: la app no escribe `cuit_simulado` y queda en `false`; hoy se corrige por SQL.
- **PR #204** abierto el 27/11, esperando revisión de Santi; sin merge. Verificadores corridos: ver abajo.
- **Ronda posterior al APTO (26/11/2026):** `f71533e` (prueba de caracterización: `ActualizarAsync` no toca `cuit_simulado`) y `2d008a0` (salen dos cláusulas inalcanzables que ya estaban en `develop`). Suite completa sobre `2d008a0`: `Con error! - Con error:    95, Superado:   769, Omitido:     0, Total:   864, Duración: 1 h 17 m` (89 de Npgsql y 6 «transient failure», #190); reintento de los 95: `Correctas! - Con error:     0, Superado:    95, Omitido:     0, Total:    95`.
- **Verificadores:** ronda 1, `verificador-tdd` y `auditor-persistencia` APTO. Ronda 2, `verificador-tdd` APTO sobre `f71533e` y `2d008a0`; `auditor-persistencia` sin bloqueantes sobre `782a871`.
- **Deuda:** el tipo `string` no impide una razón social vacía en un layout nuevo; hay que sumarlo a `CadaLayoutAportaSuRazonSocial`.
- **Deuda de la demo:** `lacentral_demo` ya tiene `00037` «NESTOR FAZIO S.A.», activo y con CUIT (sin comprobar que sea el real); por esa fila, volver a correr `sembrar_proveedores_pdf.sql` cortaría (deducido, sin ejecutar), y su comentario describe la regla anterior.
- **Compartida:** los tres esquemas `pr110_test_*` de `lacentral_javi` que quedaron huérfanos tras la suite del 26/11 (`959576…`, `9f995e…`, `ef4770…`) **se borraron el 27/11, 3 de 3**. Quedan en `lacentral_test` los de `lacentral_santi`: `demo_ensayo_c3` y tres `pr110_test_*` (`2264c6…`, `691bc5…`, `7c31ee…`), **a resolver por Santi**.
- **27/11/2026 · decisiones de Javi:** valen los verificadores del 26/11 (no se relanzan); ningún CUIT entra al repo (es público).
- **27/11/2026 · reintento repetido** de los 95, sobre los binarios de `2d008a0` y sin conexiones de otros roles antes ni después: `Correctas! - Con error:     0, Superado:    95, Omitido:     0, Total:    95, Duración: 7 m 42 s`.
- **27/11/2026 · tres PDF reales por el lector** (`LectorTextoPdf` y `LayoutsPdf`, sin base ni caso de uso completo): presupuesto de Fazio → «Fazio» (texto fijo del layout), tipo B, toma el CUIT del cliente (BOSAC); en `lacentral_demo` ningún proveedor lo tiene (0 de 10, solo lectura). Factura de ETMAN → «ETMAN», tipo A, `descuento4` para revisar. **Factura A de Fazio → ningún layout la reconoce.**
- **Hallazgo fuera de alcance (no se arregla en este PR):** no hay layout para la factura A de Fazio, solo el del presupuesto. El texto sí se extrae; al escribirlo, el CUIT del proveedor es el primero y el de «cuenta y orden», el último. Issue propuesto, sin abrir: lo decide Javi.
- **Sin verificar:** si algún proveedor de `lacentral` tiene el CUIT del cliente (BOSAC no tiene que estar cargado como proveedor); si alguna base tiene los CUIT de esos emisores; importes y percepciones contra los PDF; el recorrido de DoD 10 en `lacentral_demo`.
- **Verificación sobre `187ba6a` (26/11/2026):** suite completa `Con error! - Con error:    42, Superado:   821, Omitido:     0, Total:   863, Duración: 1 h 36 m` (los 42, de conexión a la base, #190); reintento filtrado de los 42: 34 superados y 8 fallos; segundo reintento de esos 8: `Correctas! - Con error: 0, Superado: 8, Total: 8`. `dotnet build --no-incremental` en una carpeta de salida limpia: 0 advertencias, 0 errores.
- **Script de demo (`bb87162`, reemplazado por `782a871`: solo Fazio):** aprobado por Javi; **fuera de la corrida de la suite** (commit posterior, ninguna prueba lo ejecuta) y **sin ejecutar**: lo aplica Javi o Santi. Va así declarado en el cuerpo del PR.
- **Deuda (script de demo aparte, no va en este PR):** sembrar los gemelos en blanco con CUIT real (Fazio y Expoyer) y ETMAN, según los PDFs de Javi.
- **Confirmado por Javi (26/11/2026):** un comprobante de tipo no reconocido (`null`) se asigna; la regla es «no es A». Reemplaza el «A confirmar» de más arriba.
- **Riesgo (va en el cuerpo del PR):** un código de ARCA que el lector no traduce, en una factura A real de un proveedor cuyo CUIT no está cargado, caería a la cuenta en negro, con «Revisar».
- **Tipos que traduce el lector de facturas de compra:** ETMAN `01` → A, `06` → B; Expoyer `1` → A, `6` → B; Fazio, siempre B (presupuesto, no lee código). Cualquier otro código, o un código que no se encuentra, da `null`.
