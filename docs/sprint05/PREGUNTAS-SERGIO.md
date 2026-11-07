# Preguntas para Sergio · reglas de negocio abiertas

Armado el 06/11/2026 a partir del repositorio: el `ESTADO.md` del Sprint 4, del 5 y del 6, los
cuerpos de los PR #150 a #153 y las historias de `docs/historias/`. **Ninguna está respondida
acá.** Donde el repo ya registra una respuesta, se cita con su fecha.

Cada pregunta tiene tres partes: qué se le pregunta a Sergio, en palabras del negocio; de qué
historia o issue depende; y qué hace hoy el sistema mientras no hay respuesta.

| # | Tema | Depende de | Hoy |
|---|---|---|---|
| 1 | Condición fiscal del cliente | #46 · 045 (PR #151) | Opcional |
| 2 | Baja de un artículo con stock por error de stock | #21 · HU-ART-04, CA-003 (PR #153) | No se puede dar de baja |
| 3 | Criterios desactualizados de alta y modificación de artículos | #17 · HU-ART-01 y HU-ART-03 | Sin verificar como están escritos |
| 4 | Precio manual o «caso especial» | #22 · HU-ART-05 · HU nueva sin escribir | No se puede fijar |
| 5 | Código del artículo (compuerta C4) | C4 · HU-ART-01 · HU-UI-08 · N4 | El sistema lo genera |
| 6 | Listas de precios «desde archivo» | #128 · HU-CMP-08 (055), entregable del Sprint 6 | La HU está planteada con un archivo |
| 7 | Provincia en las direcciones | D-S5-02 · 045 | Solo la calle |

---

## 1 · ¿La condición fiscal del cliente es obligatoria?

**La pregunta:** cuando dan de alta un cliente, ¿siempre saben si es responsable inscripto,
monotributista, exento, consumidor final o no categorizado? ¿O hay clientes a los que se les
vende sin saberlo, y se completa después?

**Depende de:** #46 · 045 · HU-UI-07, pantallas de clientes y proveedores (PR #151). También toca
HU-CLI-01 (alta) y HU-CLI-03 (modificación).

**Hoy:** es **opcional**. Se puede guardar vacía y la ficha dice «Sin informar». Lo decidió Santi
el 05/11/2026 («el vacío es un estado válido y se puede guardar vacío», cuerpo del PR #151).
Ninguna historia decía que fuera obligatoria. Antes, un vacío daba error.

**Lo que no se sabe:** no se pudo mirar cuántos clientes de la base real tienen la condición
vacía (las consultas quedaron para Santi, bitácora del 05/11). Si la respuesta es «obligatoria»,
hay que decidir qué pasa con los que ya están vacíos.

---

## 2 · ¿Cómo se registra la baja de un artículo que tiene stock porque el stock estaba mal?

**La pregunta:** a veces el sistema dice que hay unidades de un repuesto que en realidad ya no
existe. Cuando quieren darlo de baja, ¿cómo lo resuelven hoy? ¿Alcanza con que el empleado
declare «el stock está mal» y escriba un motivo, o primero hay que corregir el stock? ¿Cualquier
empleado puede hacerlo o solo un administrador?

**Depende de:** #21 · 014 · HU-ART-04, CA-003 («declarar error de actualización de stock y
registrarlo con el motivo»). PR #153.

**Hoy:** **un artículo con existencia no se puede dar de baja, sin excepción** (CA-002). El
CA-003 está bloqueado porque la base no tiene dónde guardar esa declaración. El PR #153 propone
una columna (`error_de_stock` en el historial de bajas), pero es un cambio de esquema y lo decide
el equipo. Está entre lo que no se hace en esta sesión (script 09).

---

## 3 · Los márgenes de un artículo y el control del código al darlo de alta

Tres criterios de aceptación quedaron escritos para un modelo que ya no es el del sistema. Antes
de reescribirlos, hay que saber qué quiere el negocio. Están en el Sprint 4 como tarea de «Javi
con Sergio» y cierran #17.

**Las preguntas:**

- **a.** ¿Un artículo puede tener un margen de venta propio, distinto del general? Por ejemplo,
  un repuesto que se vende a lista +30 % y no +50 %. ¿O todos los artículos se venden con los
  mismos márgenes?
- **b.** Si un artículo puede tener su propio margen y lo cambian, ¿el precio cambia solo para ese
  artículo?
- **c.** Al cargar un artículo nuevo, ¿qué control tiene que hacer el sistema para no cargarlo dos
  veces? Depende de la pregunta 5: hoy el código lo pone el sistema y no se puede repetir.

**Depende de:** #17. Los criterios son:
- HU-ART-01, **CA-002**: «ya existe un artículo con ese código interno → se rechaza el alta».
  Quedó desactualizado porque el código interno ahora lo genera el sistema.
- HU-ART-01, **CA-006**: «el usuario cargó los márgenes de venta → se registran».
- HU-ART-03, **CA-004**: «el usuario modifica los márgenes de venta → se recalcula el precio solo
  de ese artículo».

**Hoy:** los márgenes son **globales**, los mismos para todos los artículos (D1: Lista = costo
× 1,50, Costo = × 1,00, Efectivo = lista × 0,90). El alta no pide márgenes y la modificación no
los cambia. Los tres criterios figuran como no verificados tal como están escritos.

---

## 4 · El precio manual o «caso especial»

**La pregunta:** hay artículos que no se venden a la fórmula de siempre: el precio lo pone una
persona. Para esos casos:

- **a.** ¿Quién puede poner ese precio: cualquier empleado o solo un administrador?
- **b.** Si después llega una compra con otro costo, ¿el precio puesto a mano se mantiene o se
  recalcula?
- **c.** ¿Se puede poner a mano también el precio de lista, o solo el de «caso especial»?
- **d.** ¿Hay que guardar quién lo puso y cuándo?

**Depende de:** #22 · HU-ART-05 (sigue abierto por esto) y una **HU nueva de precio manual** que
«escriben Javi y Sergio» (ESTADO del Sprint 4) y que todavía no existe.

**Hoy:** la modalidad «caso especial» existe, pero **no hay forma de fijarle precio**: no hay
pantalla ni operación. La ficha muestra «caso especial no fijado». Cuando llega una compra, el
recálculo no pisa un precio manual (está probado). Las tasas de Tarjeta 3 y Tarjeta 6 tampoco
están definidas, y no se inventan.

**Lo que ya está registrado:** el plan del Sprint 4 (compuerta C1) es agregar una modalidad
**L2 (+40 %)** y retirar **Tarjeta 3 y Tarjeta 6**. Es un cambio de esquema que sigue pendiente.

---

## 5 · El código del artículo (compuerta C4)

**Ya respondida por Sergio el 19/10/2026** (ESTADO del Sprint 4, «Respuestas de Sergio ·
19/10/2026»). Textual:

> Todos los artículos tienen CÓDIGO ORIGINAL (del fabricante) + código del proveedor. El original
> es la clave normalizada; los de proveedor son alternativos. Si dos marcas venden el mismo código
> original, son DOS artículos: se distinguen por marca. El código original se puede corregir
> después de cargado, y lo puede corregir CUALQUIER empleado.

**Lo que falta no es de Sergio, es del equipo:** decidir si se adopta. Contradice dos cosas que ya
están hechas: que el sistema genere el código (`LC-000123`) y que solo un administrador pueda
cambiarlo. Además pide un cambio de esquema (marca y código original) y una HU para cargar los
códigos originales de los artículos que ya existen.

**Lo que sí queda para Sergio** (abiertas en el mismo registro del 19/10):
- **a.** ¿Cuántos artículos hay que completar con su código original?
- **b.** En los artículos que ya existen, ¿de dónde sale la marca?

**Depende de:** C4, HU-ART-01 (CA-002), HU-UI-08 (la pantalla de artículos espera C4) y N4 (la
carrera entre código interno y código de proveedor).

**Hoy:** el código lo genera el sistema y solo un administrador puede cambiarlo.

---

## 6 · Las listas de precios de los proveedores: ¿de dónde sale «el archivo»?

**La pregunta:** la historia del Sprint 6 dice «incorporar la lista de precios de un proveedor
desde un archivo». Pero el 19/10 dijiste que los proveedores no mandan listas en Excel ni CSV: los
precios están en sus páginas web. Entonces:

- **a.** ¿Hay algún proveedor que sí mande un archivo (Excel, CSV, PDF, un mail con una tabla)?
- **b.** Si no, ¿cómo consultan hoy el precio de un proveedor: entran a la web artículo por
  artículo? ¿Algún proveedor permite descargar su lista desde la web?
- **c.** ¿Qué les sirve más en el primer paso: cargar a mano los precios que miran en la web, o
  que el sistema los traiga solo de uno o dos proveedores?

**Respuesta registrada, 19/10/2026** (ESTADO del Sprint 4, punto b):

> No hay Excel/CSV; los proveedores publican precios actualizados en su web, salvo cuando no hay
> stock. HU-CMP-08 (055, pista C) no se resuelve con archivo como está planteada.

**Depende de:** #128 · 055 · HU-CMP-08, que es el **entregable del Sprint 6** (cierra el
27/11/2026). También la bloquea la compuerta C2, que es de esquema. La consulta en vivo a la web
(HU-CMP-02) es la etapa 2, con un proveedor como prueba de concepto (D16).

**Hoy:** la HU sigue planteada con un archivo y no está empezada: espera C2.

---

## 7 · La provincia en las direcciones

**La pregunta:** en las direcciones de clientes y proveedores, ¿hace falta registrar la
provincia? ¿Hay clientes o proveedores de otras provincias? ¿Se usa para algo, como la
facturación, los envíos o las percepciones de Ingresos Brutos?

**Depende de:** D-S5-02 (ESTADO del Sprint 5, Deuda) y 045 · HU-UI-07 (PR #151, «Fuera de este PR,
para decidir»).

**Hoy:** las direcciones son **solo la calle**. La base ya tiene la tabla de las 24 provincias,
cargada en la demo el 05/11, pero ninguna pantalla ni operación la usa. Agregarla cambia el alta y
la modificación de clientes y de proveedores, que son HU cerradas. Por eso lo decide el equipo.
