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
