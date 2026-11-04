# BITÁCORA · Sprint 5

> Rastro de cada tarea mientras pasa. Se agrega, nunca se reescribe. La regla está en
> `CLAUDE.md` § «Bitácora de tarea · obligatoria». El estado del sprint está en
> `docs/sprint05/ESTADO.md`.

---

## 04/11/2026 (jornada) · cierre del Sprint 4, C3 lacentral_demo y re-scaffold con el 07

> Bloque **reconstruido** el 04/11/2026 15:25 desde los PR #139, #140 y #141 y sus commits:
> la regla de la bitácora se adoptó al final de esta jornada. Las instrucciones fueron cuatro
> mensajes de Santi; no se transcriben verbatim porque este bloque no se escribió mientras
> pasaba. Lo que sigue es lo que decidieron, con su evidencia.

**Instrucción recibida (reconstruida, no verbatim):**
> · #138: lo mergea Santi; §8 sigue: Claude no mergea ningún PR. Milestones #42, #44 y #45 a
> Sprint 4. Línea de base 436; #137 es un Issue. Las diferencias visuales van como lista en
> `docs/sprint04/ESTADO.md` (texto / estructura), sin arreglarlas. Tokens `--filete: 3px` y
> `--filete-marca: 4px`.
> · C3: `lacentral_demo` (creada por Santi, owner `lacentral_santi`) con Parte A + 01 a 04 y
> sembrada; API con cadena de conexión propia, sin tocar la configuración por defecto; el 07
> todavía no.
> · Re-scaffold con el 07 (primer número libre), después de que Santi comparara los esquemas.
> Arreglar lo que rompa (ubicación a `stock`, `provincia_id`, `usuario_*_id` desde el
> historial, clave de tres columnas). `stock.ubicacion_deposito` a 60; la ubicación sale del
> alta y de la modificación y se carga desde la ficha, por sucursal. Los 7 tests del esquema y
> una corrida completa. `docs/sprint05/ESTADO.md` con las decisiones y su motivo.
> · Correcciones posteriores: correr de nuevo la suite completa (456); el test de negocio de
> N-7 (20 bujías, 10 por sucursal); declarar los 5 tests eliminados; 08 idempotente;
> `provincia_id` para la HU 045; clave de demo a una ruta estable fuera del repo.

**Plan:** 1. Cierre del Sprint 4 (#139). 2. C3: armar y sembrar `lacentral_demo` (#140).
3. Re-scaffold, arreglos del modelo, ubicación por sucursal y tests (#141). 4. Correcciones.

**Estado:** terminada

- #139 (`docs/sprint04-cierre`): merge de `develop` (`862d593`), tokens del filete (`d57759f`),
  ESTADO del Sprint 4 (`3b34b29`, `332037b`). Frontend 88/88 en Chromium. Abierto, sin mergear.
- #140 (`feature/c3-lacentral-demo`): `backend/db/demo/sembrar_demo.cs` + README (`e0f8217`).
  `lacentral_demo` con Parte A + 01, 02, 04 (el 03 aborta por diseño); huella igual a la del
  esquema de `develop`. Sembrada: 2 usuarios, 5 proveedores, 30 artículos, 90 códigos,
  15 facturas, 180 líneas, 15 fechas. Login de `operador.demo` y búsqueda verificados por HTTP.
  **C3 cerrada.** Clave en `~/.config/lacentral/clave-demo.txt` (600), fuera del repo.
- #141 (`feature/normalizacion-3fn-rescaffold`): 07 versionado (`8cbe45f`), re-scaffold
  (`92c7545`), estructura (`0307800`), rojo backend (`f2bb5a9`), producción (`c089717`), rojo y
  verde de la UI (`694f53c`, `2b4d662`), `medio_cobro` en un test (`7acfe72`), ESTADO
  (`5578bc4`), saca el `Distinct()` sin test (`9dafabe`), test de las 20 bujías (`20022d7`,
  rojo con la guarda vieja por mutación), 08 idempotente (`a4179f4`), ESTADO final (`f6a14b5`).
- Tests: backend **456/456** en una corrida completa sobre `a4179f4` (436 − 5 + 25); frontend
  94/94. 08 probado en `lacentral_test` en sesión de solo lectura: «ya era VARCHAR(60)».

**Quedó abierto:**
- #139, #140 y #141 sin mergear (los mergea Santi).
- `verificador-tdd` y `auditor-persistencia` sin correr sobre el #141.
- 07 y 08 en `lacentral_demo`: el lunes 09/11, o con el #141 mergeado y verificado; nunca
  jueves ni viernes (S5-06).
- `git merge develop` en la 044 (`formulario.ts`) y el aviso flotante (foco y Escape).
- `graphify update` sin correr después de los cambios.

---

## 04/11/2026 15:25 · bitácora en CLAUDE.md, compuertas del #141, grafo y SQL de graphify

**Instrucción recibida (verbatim):**
> La sección está en docs/sprint05/REGLA-BITACORA.md. Leela y hacé las dos cosas: copiá el
> bloque de nivel 2 a CLAUDE.md tal cual, y creá docs/sprint05/BITACORA.md con el bloque de
> hoy reconstruido de los PR #139, #140 y #141 y de los commits, con Estado: terminada y lo
> que quedó abierto. Borrá REGLA-BITACORA.md después: su contenido queda en CLAUDE.md y
> tenerlo en dos lugares es otra cosa que se desactualiza.
>
> Tres cosas más:
>
> 1 · Corré verificador-tdd y auditor-persistencia sobre el #141 antes de que yo lo mergee.
> El #141 toca backend y el esquema, así que el auditor aplica de lleno — es compuerta, no
> opcional. Si alguno da NO APTO, arreglá y volvé a correrlo.
>
> 2 · El grafo que estás armando es de la rama de la 044, porque es la que tiene el árbol
> principal. Eso significa que describe código sin el re-scaffold, sin la ubicación por
> sucursal y sin el 08: si alguien le pregunta al grafo por el esquema nuevo, le va a
> contestar mal. Dejalo anotado en el ESTADO y volvé a indexar desde develop cuando el #141
> y la 044 estén mergeados.
>
> 3 · Instalá el soporte de SQL de graphify: pip install "graphifyy[sql]" (o
> uv tool install "graphifyy[sql]"). Sin tree_sitter_sql los .sql quedan afuera del grafo, y
> en este proyecto el esquema ES el SQL de backend/db/ — es Database First, los scripts son
> la fuente de verdad. Un grafo que no los ve se pierde justo la parte que más consultamos.
>
> Sobre el Distinct() que sacaste: está bien sacarlo, era tuyo y no tenía test. Pero declaralo
> en el cuerpo del PR con el motivo, que es lo que pide la regla de cambios en código ajeno.

**Plan:**
1. Sección de nivel 2 a `CLAUDE.md` tal cual; esta bitácora con el bloque de hoy; borrar
   `REGLA-BITACORA.md`. Rama `docs/sprint05-bitacora` desde `develop`, con su PR.
2. Declarar el `Distinct()` en el cuerpo del #141.
3. Anotar en el ESTADO (#141) que el grafo actual es de la rama de la 044 y que se reindexa
   desde `develop` con el #141 y la 044 mergeados.
4. Instalar `graphifyy[sql]` y terminar el armado del grafo con los `.sql`.
5. `verificador-tdd` y `auditor-persistencia` sobre el #141, uno por vez, cada uno en su
   worktree. NO APTO → arreglar y volver a correr, hasta 3 rondas.

**Estado:** en curso
- 15:27 · Paso 1: sección en `CLAUDE.md` y esta bitácora en `6de11ab`; `REGLA-BITACORA.md` borrado (no estaba versionado: solo existía en el árbol principal).
- 15:24 · Paso 2: `Distinct()` declarado en el cuerpo del #141, con commit (`9dafabe`) y motivo.
- 15:24 · Paso 3: D-S5-05 en `docs/sprint05/ESTADO.md` del #141 (`b766225`, pusheado): el grafo es de la rama de la 044; reindexar desde `develop` con el #141 y la 044 mergeados.
- 15:26 · Paso 4: `graphifyy[sql]` instalado (`uv tool install --force "graphifyy[sql]"`, v0.9.102); el AST ahora ve los `.sql`: 61 nodos de `backend/db/` y `docs/`. Extracción semántica: 118 archivos, 432 nodos, 856 aristas.
**BLOQUEADO EN:** grabar el grafo. El armado completo da 2635 nodos contra 2995 del `graph.json` vigente (838 nodos de documentos del grafo viejo no reaparecen; ningún archivo falta). La guarda de graphify no lo sobrescribe sin `--force`. Depende de Santi: forzar el reemplazo o conservar el grafo viejo.
- 15:26 · Paso 5: `verificador-tdd` lanzado sobre el #141 (b766225), en su propio worktree. El `auditor-persistencia` va después: nunca dos a la vez.
- 15:27 · `verificador-tdd` devolvió **NO VERIFICADO** sin correr, por su regla R1: además del árbol principal hay 4 worktrees (`bitacora`, `demo`, `rescaffold`, `cierre-s4`). Los cuatro son de Claude, están limpios y pusheados. Quitarlos con `git worktree remove --force` lo bloqueó el clasificador de permisos.
**BLOQUEADO EN:** las dos compuertas del #141. Depende de Santi: que quite los 4 worktrees (o autorice quitarlos) para relanzar `verificador-tdd` y, después, `auditor-persistencia`. Sigue pendiente también la decisión del grafo (paso 4).
