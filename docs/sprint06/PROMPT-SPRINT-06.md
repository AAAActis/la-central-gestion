Arrancamos el Sprint 6 en serio. Trabajás solo hasta terminarlo o hasta que algo dependa de una
persona. No me consultes decisiones técnicas: las tomás vos, las anotás con fecha en el
`ESTADO.md` y seguís. Solo parás una pieza por una regla de negocio que falta, y mientras tanto
seguís con lo que no depende de ella.

## 0 · Antes de tocar nada

1. Leé `CLAUDE.md` entero y respetalo. En particular: la bitácora (`docs/sprint06/BITACORA.md`),
   la política de pruebas, la sección de los verificadores y la de `pull`.
2. Leé el último bloque de `docs/sprint06/BITACORA.md` con `Estado: en curso`. Si hay uno,
   seguís desde ahí.
3. Abrí un bloque nuevo en la bitácora con esta instrucción completa, verbatim.
4. `git fetch --all --prune`, pasate a `develop` y traé lo último. Mi copia local estaba parada
   en una rama `feature/9-…` ya mergeada y con cambios sin commitear: mirá `git status`, y si hay
   algo que no sea la carpeta `docs/facturas/` (que ya saqué del repo), anotalo en la
   bitácora y no lo descartes.
5. Leé `docs/sprint06/ESTADO.md`, `docs/PLAN-SPRINTS-6-7-8.md`, `docs/historias/INDICE.md` y las
   proformas `029-HU-FCO-05.md`, `049-HU-UI-11.md` y `056-HU-FCO-06.md`.

## 1 · Lo que cambió desde el último ESTADO

- **Javi revisó, aprobó y mergeó todos los PR abiertos:** #166, #167, #170, #171, #172, #173 y
  #175. El Sprint 5 está cerrado (tag `sprint-5`).
- Eso deja la **056 mergeada**, el **#169 mergeado** y la **028 (anulación de factura de
  compra, #9) construida antes de tiempo**, aunque está planificada para el Sprint 7.
- Falta el recorrido DoD 10 de la 056 contra `lacentral_demo`. Lo hace Javi con Sergio: no es
  tuyo, solo dejalo bien armado.

**Fase A · poner al día la documentación (un PR solo de docs, sin verificadores):**
- `docs/sprint06/ESTADO.md`: tablero con lo mergeado, PR por PR. Lo mismo para la 028 en el
  ESTADO o el plan que corresponda, y si cierra el CA-003 y el CA-004 de la 016, decilo.
- `docs/historias/INDICE.md`: Sprint vigente y Estado de la 056, la 028 y la 016.
- `docs/PLAN-SPRINTS-6-7-8.md`: si la 028 ya está, sale de la cuenta de SP del Sprint 7.
- Revisá con `gh` el estado de las issues y los milestones contra lo que escribiste. Si una issue
  quedó abierta con el PR mergeado y todos sus CA verificados, cerrala con un comentario que
  diga qué PR la cierra. Si tiene un CA sin verificar, no la cierres: anotalo.

**Para Santi (no lo corrés vos, no tenés acceso a `lacentral`):** dejá en la bitácora, en un
bloque «PARA SANTI», los comandos listos para pegar en fish para:
1. hacer el backup de `lacentral` con `/home/atiz/tesis-db/backup.sh` (vía `ssh atiz@arch`);
2. verificar qué scripts de `backend/db/rescaffold/` ya tiene aplicados `lacentral` (una consulta
   de solo lectura que mire columnas o tablas que agrega cada script, del 07 al 09);
3. aplicar los que falten, en orden, con `ON_ERROR_STOP=1`, contra el contenedor `lacentral_db`
   con el superusuario `lacentral`.
Sin marcadores para completar: comandos enteros.

## 2 · Compuerta C7 · ya hay PDFs reales

Están **fuera del repo**, en `~/dev/TESIS/facturas-reales/`. **Nunca van al repo** (es público):
ni el PDF, ni un recorte, ni sus datos copiados en un test. Agregá `docs/facturas/` al
`.gitignore`, en el PR de docs, para que no vuelva a pasar.

| Archivo | Proveedor | Qué es |
|---|---|---|
| `10713-VT-FA0137-25630.pdf` | ETMAN (Multioriginal Parts S.A.) | **Factura A**, 0137-00025630, 17/11/2026. Texto extraíble (PScript + Ghostscript). Por línea: código, descripción, marca, cantidad, precio, **tres columnas de descuento** (Bonif., Bonif., Dto.), total. Percepción IIBB Córdoba aparte. IVA 21 %. **Números en formato estadounidense** (`20,184.21`). |
| `Presupuesto_48437.pdf` | Fazio (Badaloni) | **Presupuesto**, compra **en negro**, no es factura. Texto extraíble. Por línea: código de fábrica, descripción, cantidad, remito, precio de lista, descuento combinado `43,84 (28+22)`, precio final, total. **Números en formato argentino** (`27.396,12`). |
| `V20261105NCBA0010-00021495 (1).pdf` | Expoyer S.A. | **Nota de crédito A** por bonificación, en blanco. **No es una factura de compra.** |

Dos hechos que salen de los PDF y conviene registrar en el ESTADO:
- **Los descuentos son sucesivos, confirmado con dos proveedores:** en ETMAN,
  20.184,21 × 0,95 × 0,64 = 12.272,00 (el total de la línea); en Fazio, 28 + 22 = 43,84 %. Era el
  supuesto de la 056 que faltaba confirmar.
- **Cada proveedor usa un formato de número distinto**, así que el parser no puede asumir uno.

Lo que te toca:
1. Decidí si con esto alcanza para cerrar la C7, o si se cierra parcial. Con lo que hay, falta
   una **factura** de Expoyer: la de ellos es una nota de crédito. Anotá la decisión con fecha.
2. La nota de la 029 pide decidir entre un parser por reglas o una interpretación asistida.
   Decidilo vos con estos tres layouts a la vista. Dejá escrito el porqué y qué cuesta agregar
   un proveedor nuevo con la opción elegida. Si la opción usa un servicio externo, la clave va
   en variables de entorno y nunca en el repo.
3. **Los tests no usan los PDF reales.** Generá en los tests PDF sintéticos que imiten cada
   layout, con datos inventados (proveedor, CUIT, códigos y montos ficticios). Ningún test
   depende de que exista `~/dev/TESIS/facturas-reales/`: la DoD prohíbe pruebas omitidas. Los
   reales se usan para analizar el layout y para el recorrido a mano de la DoD 10.
4. Antes de construir, preguntas para Sergio (no las inventes, dejalas en el ESTADO en «Lo que
   vuelve a Sergio» y seguí con lo que no dependa de ellas):
   - El presupuesto en negro de Fazio, ¿se carga como comprobante **B** con el prefijo **99**
     (D-S6-10)? ¿Qué número lleva, el del presupuesto (48437)?
   - Las notas de crédito de proveedor, ¿se cargan en el sistema? El Mandato deja las notas de
     crédito fuera de alcance: confirmarlo.
   - ETMAN informa «Pago contado 5 %» y vencimientos con descuento por pronto pago. ¿El sistema
     lo ignora (el costo es el de la línea) o tiene que registrarlo?

## 3 · Construcción · 029 y después 049

- Una rama y un PR por HU (§2 de `CLAUDE.md`). Si la 029 pasa de 500 líneas de diff, partila en
  dos PR con sentido propio (sección de ahorro de tokens, punto 4).
- TDD con los tres commits. Persistencia contra PostgreSQL real, releyendo con contexto nuevo.
- La 029 nunca persiste antes de la confirmación del operador y nunca crea artículos por su
  cuenta (RF-011). El código del proveedor se busca en `articulo_codigo_alternativo`. Lo que no
  reconoce con certeza lo marca para revisar, no lo adivina.
- La 049 sigue `docs/diseno/SISTEMA-VISUAL.md` y reutiliza la pantalla de la 056: el PDF
  precarga el mismo formulario, no hay un formulario nuevo.
- Verificadores: una invocación por PR, al cerrar la HU, con el alcance acotado a sus commits y
  con el hash, la línea literal y el conteo de la suite completa si ya la corriste. Worktrees
  afuera antes de invocarlos. Ante un NO APTO, las 3 rondas de `CLAUDE.md`.
- `ESTADO.md` actualizado antes de cada push, en el mismo push.

## 4 · Lo que NO es tuyo en este sprint

- **Scrapes de proveedores (Sprint 7, D-PL-02):** los armamos a mano Javi, Lauti y Santi en n8n,
  fuera de la cuenta de SP. Son cuatro: Expoyer, Autocor, Nafazio (Fazio) y ETMAN (Link3).
  **Las credenciales de esos portales no las vas a ver nunca:** van en el almacén de
  credenciales de n8n (RNF-007). Si en algún archivo, issue o mensaje aparece un usuario o
  contraseña de proveedor, no lo copies a ningún lado y avisá en la bitácora. Lo único que
  hacés es anotar en el ESTADO, en «Fuera del Sprint 6», que los scrapes de esos cuatro
  arrancaron el 17/11/2026 y que el destino de sus datos es la compuerta C2 del Sprint 7.
- No arranques ninguna HU del Sprint 7 ni del 8.

## 5 · Cuándo parás

Parás y me escribís un resumen corto (qué quedó hecho, con PR, hash y conteo de la última suite
completa; qué quedó abierto y de quién depende) cuando pase cualquiera de estas cosas:
- la 029 y la 049 quedaron con PR abierto y verificadores APTO;
- todo lo que queda depende de una respuesta de Sergio o de una acción de Santi;
- 3 rondas de NO APTO sin salida.

Antes de parar, la línea `**BLOQUEADO EN:**` o el `Estado: terminada` en la bitácora, según
corresponda.
