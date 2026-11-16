# ESTADO · Sprint 7

**Entregable:** sin escribir. Lo define el equipo al abrir el sprint (§1 de `CLAUDE.md`). Este
archivo se creó el 14/11/2026 solo para dejar la pregunta de la 027. El estado de lo adelantado
del Sprint 7 (028, 026) sigue en `docs/sprint06/ESTADO.md`, «PRÓXIMA SESIÓN».

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
