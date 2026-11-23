# ESTADO · Sprint 7

**Entregable:** sin escribir. Lo define el equipo al abrir el sprint (§1 de `CLAUDE.md`). Este
archivo se creó el 14/11/2026 solo para dejar la pregunta de la 027. El estado de lo adelantado
del Sprint 7 (028, 026) está abajo, en «Construido antes de tiempo».

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
equipo las decida. Se registran; no se implementan.

### C7-01 · IVA 10,5 % (artículos de electricidad)

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

### C7-02 · Costo al anular una factura

- **Qué dijo Sergio:** al anular, el costo vuelve al anterior a la factura. Ejemplo: costo 1450, factura a 1500, se
  anula: 1450.
- **Hoy:** `AnularConEfectosAsync` vuelve al costo del último registro válido de `articulo_historial_compra`; si no hay
  registro anterior (valor importado o cambio manual), conserva el actual.
- **Para cumplir el ejemplo en todos los casos** habría que guardar el costo previo en cada línea de la factura al
  registrarla (script 12).
- **Respuesta de Sergio (23/11/2026) sobre el cambio a mano:** si el costo se cambió a mano entre la factura y la anulación,
  **queda el que se puso a mano** (no se vuelve al anterior a la factura).
- **Pendiente:** el esquema (la regla ya está completa).
- Reemplaza la «interpretación pendiente» del CA-002 de la 028 (`docs/historias/028-HU-FCO-04.md`, actualizado el
  23/11 con la respuesta; el código no cambia). El CA-004 de la 016 (#23) tenía la misma interpretación.

### C7-03 · Anulación con un solo administrador

- **Qué dijo Sergio:** «con uno solo sí se puede anular».
- **Choque:** el CA-006 de la 028 exige que autorice **otra** persona con rol ADMINISTRADOR, distinta de quien pide: rechaza la
  autoautorización (pedido de la Dirección del 02/11/2026). Con un solo administrador, la anulación no se puede hacer.
- **No se cambia el código en esta tarea.**
- **Respuesta de Sergio (vía Javi, 23/11/2026):** **el mismo administrador puede anular y autorizarse solo.** Contradice el
  CA-006 y el pedido de la Dirección del 02/11/2026 (doble certificación): **lo decide el equipo/Dirección** antes de tocar
  el CA-006 y su código.

### C7-04 · Módulo de devoluciones a proveedor (Reclamos, alerta de pendientes, PDF)

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
  datos y cómo se envía.

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
