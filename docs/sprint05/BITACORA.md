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

**Estado:** terminada
- 15:27 · Paso 1: sección en `CLAUDE.md` y esta bitácora en `6de11ab`; `REGLA-BITACORA.md` borrado (no estaba versionado: solo existía en el árbol principal).
- 15:24 · Paso 2: `Distinct()` declarado en el cuerpo del #141, con commit (`9dafabe`) y motivo.
- 15:24 · Paso 3: D-S5-05 en `docs/sprint05/ESTADO.md` del #141 (`b766225`, pusheado): el grafo es de la rama de la 044; reindexar desde `develop` con el #141 y la 044 mergeados.
- 15:26 · Paso 4: `graphifyy[sql]` instalado (`uv tool install --force "graphifyy[sql]"`, v0.9.102); el AST ahora ve los `.sql`: 61 nodos de `backend/db/` y `docs/`. Extracción semántica: 118 archivos, 432 nodos, 856 aristas.
**BLOQUEADO EN:** grabar el grafo. El armado completo da 2635 nodos contra 2995 del `graph.json` vigente (838 nodos de documentos del grafo viejo no reaparecen; ningún archivo falta). La guarda de graphify no lo sobrescribe sin `--force`. Depende de Santi: forzar el reemplazo o conservar el grafo viejo.
- 15:26 · Paso 5: `verificador-tdd` lanzado sobre el #141 (b766225), en su propio worktree. El `auditor-persistencia` va después: nunca dos a la vez.
- 15:27 · `verificador-tdd` devolvió **NO VERIFICADO** sin correr, por su regla R1: además del árbol principal hay 4 worktrees (`bitacora`, `demo`, `rescaffold`, `cierre-s4`). Los cuatro son de Claude, están limpios y pusheados. Quitarlos con `git worktree remove --force` lo bloqueó el clasificador de permisos.
**BLOQUEADO EN:** las dos compuertas del #141. Depende de Santi: que quite los 4 worktrees (o autorice quitarlos) para relanzar `verificador-tdd` y, después, `auditor-persistencia`. Sigue pendiente también la decisión del grafo (paso 4).

---

**Quedó abierto:** las compuertas del #141 y la decisión del grafo, que siguieron en el bloque siguiente.

## 04/11/2026 15:38 · worktrees fuera, compuertas del #141 y el grafo viejo se conserva

**Instrucción recibida (verbatim):**
> Las dos decisiones:
>
> 1 · WORKTREES · sacalos, pero sin --force. El --force es lo que saltea la verificación de
> que están limpios, y si ya confirmaste que lo están, git los va a sacar sin quejarse. Si
> alguno se queja, NO lo fuerces: miralo y decime qué tiene.
>
> Hacelo en tres pasos, no con el bucle:
>   a. git worktree list y git -C <cada uno> status --short --branch, para ver cuál está
>      sucio o sin pushear.
>   b. Confirmá en qué worktree estás parado vos: no saques ese, terminá ahí primero o
>      mudate al principal.
>   c. git worktree remove <ruta>, uno por uno, y al final git worktree prune.
>
> Las ramas no se borran, así que no se pierde nada aunque haya que rehacer un worktree.
>
> Después relanzá verificador-tdd y, cuando termine, auditor-persistencia. De a uno, como
> dijiste.
>
> 2 · EL GRAFO · no lo fuerces. Conservá el viejo. Coincido con tu recomendación y hay un
> motivo más: el grafo nuevo es de la rama de la 044, así que describe código sin el
> re-scaffold y estaría mal igual. La reindexación desde develop ya está agendada para
> cuando entren el #141 y la 044, y ahí van a entrar los .sql y el detalle de los documentos
> juntos. Perder 838 nodos ahora para ganar los .sql dos días antes no vale.
>
> Lo de los siete documentos leídos a una línea del final: anotalo en el ESTADO como algo a
> verificar en la reindexación, no lo persigas ahora. Si es un error de conteo sistemático,
> se está perdiendo la última línea de cada documento, y eso en un .md puede ser una
> conclusión. Se comprueba de una comparando el conteo de líneas de uno de esos siete
> archivos contra lo que el grafo dice que leyó — pero después de la reindexación.

**Plan:**
1. Anotar en el ESTADO del #141 lo de los siete documentos (verificar en la reindexación).
2. Grafo: no se fuerza; queda el `graph.json` vigente.
3. Worktrees: a) `git worktree list` + `status --short --branch` de cada uno; b) confirmar
   dónde estoy parado (árbol principal); c) `git worktree remove` uno por uno, sin
   `--force`, y `git worktree prune`. Este worktree (`bitacora`) se saca último, después
   de pushear este bloque; mientras corren las compuertas, las líneas de la bitácora se
   juntan fuera del repo y se agregan al rehacer el worktree.
4. `verificador-tdd` sobre el #141; cuando termine, `auditor-persistencia`. NO APTO →
   arreglar y volver a correr, hasta 3 rondas.

**Estado:** terminada
- 15:39 · Paso 1: D-S5-06 en el ESTADO del #141 (`44df50a`, pusheado).
- 15:39 · Paso 2: grafo no forzado; `graphify-out/graph.json` sin tocar.
- 15:39 · Paso 3: los 4 worktrees limpios y al día con su remoto (`status --short --branch`); parado en el árbol principal; `git worktree remove` sin `--force`, de a uno (cierre-s4, demo, rescaffold, bitacora): ninguno se quejó; `prune`. Queda solo el principal.
- 15:39 · Paso 4: `verificador-tdd` relanzado sobre el #141 (`44df50a`).
- 15:59 · `verificador-tdd` ronda 1: **NO APTO**. Sobreviven M18 (`ClienteRepositorio.cs:134`, orden de las bajas) y F2 (`articulos.ts:317`, error tragado). Además: 13 de los 17 rojos de `f2bb5a9` eran `NotImplementedException`; 3 de los 6 de `694f53c`, `TypeError`; N-11 entró con los tests adaptados en `0307800`. Suite del verificador: 456/456 y 94/94 sobre `44df50a`.
- 15:59 · Arreglo, sin tocar producción: `b5e0ca5` (dos bajas: M18 da rojo, «Expected 2, Actual 1») y `79606e8` (error de la API en la ficha: F2 da rojo). Una corrida completa: **457/457**, frontend **95/95**. ESTADO (`0d41569`) y cuerpo del #141 corregidos, incluido que esos rojos eran stubs.
- 16:40 · `verificador-tdd` ronda 2 sobre `0d41569`: **APTO**. M18 y F2 mueren por aserción; 10 mutaciones re-muestreadas, ninguna sobrevive; 457/457 y 95/95; los rojos declarados coinciden. No bloqueantes: el PR cita `79606e8` (HEAD es `0d41569`, solo docs); no hay test de API que afirme específicamente que OPERADOR recibe 204 en el PUT de ubicación (el rol es de toda la clase).
- 16:40 · `auditor-persistencia` lanzado sobre el #141 (`0d41569`).
- 16:46 · `auditor-persistencia` sobre `0d41569`: **APTO**. Catálogo de `lacentral_test` en solo lectura, 46/46 filtrados contra PostgreSQL, 3 mutaciones detectadas. Observaciones anotadas como D-S5-07 a D-S5-09 en el ESTADO (`7c4ddc1`); cuerpo del #141 actualizado con las dos compuertas.
- 16:46 · Worktrees: el del #141 sacado sin `--force`; este (`bitacora`) se saca después de pushear.

**Quedó abierto:**
- #139, #140, #141 y #142 sin mergear (los mergea Santi).
- 07 y 08 en `lacentral_demo`: el lunes 09/11 o con el #141 mergeado y verificado (S5-06).
- Reindexar el grafo desde `develop` con el #141 y la 044 mergeados (D-S5-05), y ahí verificar los siete documentos a una línea del final (D-S5-06).
- Deuda nueva: D-S5-07 a D-S5-09.
- Pendientes del 04/11: `git merge develop` en la 044 (`formulario.ts`) y el aviso flotante (foco y Escape).

## 04/11/2026 21:32 · conteo del #141, orden de merge, política de tests en CLAUDE.md, Trait y README de tests

**Instrucción recibida (verbatim):**
> Antes del merge, dos cosas:
>
> 1 · Conciliá el conteo. A la tarde reportaste la suite completa en 456/456 sobre a4179f4,
> y el verificador reporta 457/457 backend y 95/95 frontend sobre 0d41569. Averiguá de dónde
> salió el test de más (y el de frontend), y dejá en el cuerpo del #141 y en el ESTADO el
> número del ÚLTIMO commit, no el de la corrida del mediodía. Un PR con dos conteos
> distintos es un PR que nadie puede auditar.
>
> 2 · Decile a Javi en qué orden mergear, y escribilo en el cuerpo de los PR:
>    a. #141 primero. Es el que desbloquea develop: hasta que entre, cualquier rama nueva
>       que toque backend hereda los 148 tests de PostgreSQL en rojo.
>    b. Después #139, #140 y #142, rebaseando cada uno sobre develop a medida que entren.
>       Ojo con #139 y #141: los dos tocan frontend, así que puede haber conflicto en
>       styles.css.
>    c. Con los cuatro adentro, UNA corrida completa sobre develop y el número en el ESTADO.
>       Ese es el estado que hereda todo el mundo.
>
> Y lo que quedó sin hacer de lo que te pedí: el ENSAYO DE LA DEMO y el
> docs/sprint05/GUION-DEMO.md. Es pasado mañana y es lo único que todavía no está probado de
> punta a punta por el frontend contra lacentral_demo. En cuanto Javi mergee, eso va primero,
> antes del merge de develop en la 044 y antes del aviso flotante.
> Agregá la política de tests a CLAUDE.md: está en docs/sprint05/REGLA-TESTS.md, copiá el
> bloque de nivel 2 tal cual y borrá el archivo después, como hicimos con la bitácora. Hasta
> ahora la tenías como instrucción de sesión y eso se pierde en la próxima.
>
> Dos cosas de implementación que van con eso:
>
> 1 · Confirmame si el [Trait("Base","PostgreSQL")] está puesto en las clases que usan
> PostgreSqlPruebas. Si no está, ponelo: es lo que habilita el `--filter "Base!=PostgreSQL"`
> y es la diferencia entre correr 287 unitarias en segundos o 457 en dos minutos y medio.
>
> 2 · Dejá en LaCentral.Tests/README.md los dos comandos del bucle rápido y de la completa,
> con la cuenta hilos × pool ≤ CONNECTION LIMIT del rol que pide D-27b. Que el próximo no
> tenga que deducirlo.

**Plan:**
1. Conciliar el conteo: de dónde salen el +1 de backend y el +1 de frontend.
2. `[Trait("Base","PostgreSQL")]` en las clases que usan `PostgreSqlPruebas` (rama del #141).
3. `LaCentral.Tests/README.md`: los dos comandos y la cuenta hilos × pool ≤ CONNECTION LIMIT.
4. Una corrida completa (backend y frontend) sobre el último commit del #141; ese número va
   al ESTADO y al cuerpo del #141.
5. Política de tests en `CLAUDE.md` (bloque de nivel 2 tal cual) en esta rama (#142);
   borrar `REGLA-TESTS.md`.
6. Orden de merge en el cuerpo de #139, #140, #141 y #142, y en el ESTADO.
7. Ensayo de la demo y `GUION-DEMO.md`: después del merge de Javi (no antes).

**Estado:** en curso
- 21:40 · Paso 1: conteo conciliado. +1 backend = `b5e0ca5` (`HistorialBajaRepositorioTests`, dos bajas, M18); +1 frontend = `79606e8` (spec del error de la API en la ficha, F2). Mediodía 456/94 (`a4179f4`) → 457/95 (`79606e8`) → 458/95 (`4f4e17e`, por el test del Trait).
- 21:45 · Paso 2: el Trait **no estaba** en ninguna clase (los traits existentes usan otras claves: `Proveedor`, `Api`, `Esquema`…). `5cb63b1` rojo (`TraitBasePostgreSqlTests`, Assert.Empty con las 20 clases) y `59c0327` verde: el Trait en las 20 clases con `[PostgreSqlFact]`. `--filter "Base!=PostgreSQL"`: 294/294 en 0,7 s; 164 contra PostgreSQL.
- 21:48 · Paso 3: README de tests con los dos comandos y T × (1 + P) + m ≤ L (`4f4e17e`). La cuenta ya estaba desde D-27b; se agregaron los comandos y el ejemplo con los números de hoy.
- 21:52 · Paso 4: una corrida completa sobre `4f4e17e`: **458/458**, 0 omitidos, 2 min 46 s. Frontend no se volvió a correr: `frontend/` idéntico a `79606e8` (árbol `4fa0404`), 95/95. ESTADO (`7c132c9`, pusheado) y cuerpo del #141 con ese número y la tabla de conciliación.
- 21:55 · Paso 5: política de pruebas en `CLAUDE.md`, bloque de nivel 2 tal cual (verificado con `diff`); `REGLA-TESTS.md` borrado (no estaba versionado).
- 21:58 · Paso 6: orden de merge en el cuerpo de #139, #140, #141 y #142 (verificado: los cuatro tienen la sección) y en el ESTADO del #141 (`7c132c9`).
**BLOQUEADO EN:** paso 7, el ensayo de la demo y `docs/sprint05/GUION-DEMO.md`. Depende de que Javi mergee los cuatro PR en el orden escrito; después es lo primero, antes del merge de `develop` en la 044 y del aviso flotante. También abierto: las compuertas del #141 son sobre `0d41569`; `5cb63b1`, `59c0327` y `4f4e17e` (solo tests y README) no pasaron por `verificador-tdd` ni `auditor-persistencia`.

## 04/11/2026 22:28 · reglas de método en CLAUDE.md y en los agentes, entregable del Sprint 5

**Instrucción recibida (verbatim):**
> Cambio de método. Está en docs/sprint05/REGLAS-METODO-SPRINT.md:
>
> · Las secciones 1 a 5 van a CLAUDE.md tal cual.
> · La sección 6 son CUATRO ediciones puntuales a .claude/agents/verificador-tdd.md y
>   .claude/agents/auditor-persistencia.md. Son agregados, no reescrituras: el resto de esos
>   archivos está bien y no se toca.
> · Borrá REGLAS-METODO-SPRINT.md después, como con las otras reglas.
>
> Y definí el entregable del Sprint 5 en docs/sprint05/ESTADO.md con la forma de la sección 1:
> acceso con los dos roles, artículos (ficha con existencia y ubicación por sucursal, alta,
> baja, reactivación), clientes y proveedores, y factura de compra. Todo usable sobre
> lacentral_demo. Sin comparador y sin factura por PDF: esos son el Sprint 6.
>
> Después de los merges de Javi, el orden es: COMO-PROBARLO.md y el ensayo de la demo primero
> —ni yo ni el dueño probamos nunca el sistema—, y después la 042 y la 045, una HU por PR.

(Una versión anterior de esta instrucción llegó a las 22:26 y se interrumpió antes de actuar;
vale esta.)

**Plan:**
1. `git fetch` y comparar contra `origin/develop` (§3 nueva).
2. Secciones 1 a 5 a `CLAUDE.md`, tal cual (rama del #142).
3. Las cuatro ediciones de la §6 en los dos agentes, solo agregados (rama del #142).
4. Borrar `REGLAS-METODO-SPRINT.md`.
5. Entregable del Sprint 5 y el orden posterior al merge en `docs/sprint05/ESTADO.md` (rama del #141).
6. Revisar contradicciones de lo nuevo con lo ya escrito (cuerpos de PR, política de pruebas).

**Estado:** terminada
- 22:30 · Paso 1: `git fetch`; `origin/develop` no trae nada que no tengan las ramas del #141 y el #142 (0 commits).
- 22:34 · Pasos 2 a 4: secciones 1 a 5 en `CLAUDE.md` (copia comprobada con `diff`), los cuatro agregados de la §6 en los dos agentes (12 + 3 líneas, nada borrado), `REGLAS-METODO-SPRINT.md` borrado (no estaba versionado). `aa68f7d`, pusheado; cuerpo del #142 actualizado.
- 22:40 · Paso 5: entregable del Sprint 5 en el ESTADO del #141, con la forma de la §1, lo que queda fuera (Sprint 6) y las dos condiciones de cierre, pendientes. PRÓXIMA SESIÓN: COMO-PROBARLO.md y ensayo, después 042 y 045, una HU por PR. `5656743`, pusheado.
- 22:42 · Paso 6: contradicciones. (a) La regla 3 prohíbe rebasear ramas pusheadas y el orden de merge decía «rebaseando»: corregido a `git merge origin/develop` en el ESTADO y en el cuerpo de los cuatro PR. (b) La política de pruebas pide la completa antes de abrir un PR; la regla 4 dice que al cerrar una HU no. No se tocó (las dos son texto tal cual): decide Santi. (c) La demo del 06/11 no muestra la ubicación por sucursal (S5-06): anotado en el entregable.
**Quedó abierto:** la contradicción (b); `GUION-DEMO.md` (pedido antes) frente a `COMO-PROBARLO.md` (pedido ahora): ¿uno o dos archivos?; el ensayo, que espera los merges de Javi (bloque de las 21:32).

## 04/11/2026 22:33 · aclaración de la bitácora, COMO-PROBARLO.md, HU 042 y HU 045

**Instrucción recibida (verbatim):**
> Resolución de la contradicción que encontraste, y va a CLAUDE.md como aclaración de la
> regla de la bitácora:
>
> La única línea de un bloque que se reescribe es su propio `**Estado:**`, de "en curso" a
> "terminada". Todo lo demás del bloque es de solo agregar: si algo cambió, va en una línea
> nueva o en un bloque nuevo. El motivo es el que vos mismo viste: si el Estado se duplica
> en vez de reemplazarse, la próxima sesión lee el bloque como abierto y rehace trabajo.
>
> Y ahora las HU. Una por PR, y en este orden:
>
> 1 · PRIMERO, y son 15 minutos: docs/sprint05/COMO-PROBARLO.md con UN comando que levante la
> API apuntada a lacentral_demo y el frontend, y los pasos para entrar. No es un desvío: el
> punto 10 de la Definición de Terminado exige que cada HU se pruebe a mano en el navegador
> contra lacentral_demo, así que sin esto ninguna HU de acá en adelante puede cerrar. Se hace
> una vez y sirve para todas.
>
> 2 · HU 042 · HU-UI-04 · componente de listado reutilizable. Rama desde develop, PR propio.
> Es frontend puro, así que no espera el merge del #141. Los criterios de la PISTA B del
> ESTADO del Sprint 4 son requisito, no preferencia: búsqueda como única cosa que hacer al
> entrar, sin resultados, inactivos, paginación del servidor. Antes de escribir HTML, leé la
> habilidad de diseño de frontend y docs/diseno/SISTEMA-VISUAL.md, que ahora sí está en el
> repo con las 13 referencias.
>
> 3 · HU 045 · clientes y proveedores, que reutiliza la 042 y el formulario de la 043.
> Acordate de lo que dejó anotado la 043: cuando una pantalla necesite un campo de selección,
> tiene que llegar con su criterio de aceptación — qué pasa si la lista está vacía y qué pasa
> si el valor guardado ya no está en la lista son preguntas de negocio. Si las necesitás,
> preguntame.
>
> Para cada una: tests de la HU filtrados durante el trabajo, una corrida de integración de su
> propio camino al cerrarla, y los verificadores UNA vez por PR con el alcance acotado a sus
> commits. La suite completa recién al cerrar el sprint.
>
> El GUION-DEMO.md lo hacemos mañana, antes de que Sergio la vea.

**Plan:**
1. Aclaración de la regla de la bitácora en `CLAUDE.md` (rama del #142).
2. `docs/sprint05/COMO-PROBARLO.md`: un comando que levante la API contra `lacentral_demo` y el
   frontend, y los pasos para entrar. Rama propia desde `develop`, PR propio. Probarlo de verdad.
3. HU 042 (HU-UI-04): rama desde `develop`; leer la PISTA B del ESTADO del Sprint 4, la
   habilidad de diseño de frontend y `SISTEMA-VISUAL.md`; rojo → verde; prueba a mano contra
   `lacentral_demo`; verificador una vez, alcance acotado; PR.
4. HU 045: reutiliza la 042 y el formulario de la 043; preguntar los CA de los campos de
   selección si hacen falta. Rama, PR, verificadores una vez.

`git fetch`: `origin/develop` sigue en `9577197`; #139–#142 abiertos.

**Estado:** terminada
- 22:36 · Paso 1: aclaración en `CLAUDE.md`, regla de la bitácora: la única línea que se reescribe es el `**Estado:**`.
- 22:38 · Corrección del paso 1: `00e58c9` salió sin el cambio en `CLAUDE.md` (la edición falló y no lo vi antes del commit). La aclaración entra en el commit siguiente.
- 22:50 · Paso 2: `COMO-PROBARLO.md` + `scripts/probar-demo.sh` en `docs/como-probarlo` (`c4cd149`), **PR #143** contra develop. Verificado: las dos cosas arriba; login de admin.demo y operador.demo (200, rol y sucursal) y búsqueda por el proxy; la guarda corta con `lacentral_test` y sin variable; señal al grupo libera los puertos. Riesgo anotado: entre el merge del #141 y el 07 en la demo, develop no coincide con su esquema.
- 23:20 · Paso 3, HU 042 (`feature/hu-ui-04-listado-reutilizable`, desde develop): `cbbe916` estructura, `0c5be6e` rojo (12/13 por aserción; «etiqueta asociada» nace verde), `6a38a47` verde (13/13), `b3b3571` artículos usa el listado (test junto con el cambio: orden no visible en git), `a71f2c5` ajustes contra las referencias 02 y 12. Frontend 104/104 (88 + 13 + 3). `ng build` limpio; sin colores a mano.
- 23:20 · Camino propio contra `lacentral_demo` (recorrido por CDP en Chromium headless, con `admin.demo` y `operador.demo`, 9/9 cada uno): al entrar solo la búsqueda y enfocada; «buj» → 2; 10 alternativas → 23 en 2 páginas (1–20, «Siguiente» → 21–23); «zx999» → estado vacío; sin desplazamiento horizontal a 1024/768/480. CA-003 no se probó contra la demo: no hay artículos de baja y la demo no se toca hasta después del 06/11 (C3); lo cubre el spec.
- 23:20 · Hallazgo para la 045: `GET /api/clientes` y `GET /api/proveedores` no paginan (solo `texto` e `incluirInactivos`). Para usar el listado, la 045 necesita backend.
- 23:20 · Incidente: un `pgrep -f` con la cadena del puerto de Chromium encontró al propio shell y lo mató (exit 144), como dice la memoria. Desde ahí, PID por puerto (`ss -ltnp`).
- 23:35 · `verificador-tdd` ronda 1 sobre `a71f2c5`: **NO APTO**. Sobreviven S1 (sin `cargando.set(false)` en el error, el botón queda trabado en «Buscando…») y S2 (`volverABuscar()` vacío: el test no movía el foco). 16 mutaciones matadas; m04 y m17 equivalentes (no bloqueantes). El rojo de `0c5be6e` confirmado: 12 por aserción, 0 TypeError.
- 23:45 · Arreglo, solo tests: `d50ffbb`. S1, S2 y m04 comprobadas en rojo una por una; HEAD 13/13; frontend 104/104. Ronda 2 lanzada sobre `d50ffbb`.
**BLOQUEADO EN:** la 045 necesita tres decisiones de Santi: (1) la HU-UI-06 (044) es precondición y no está mergeada; (2) `GET /api/clientes` y `/api/proveedores` no paginan: agregar paginación en el backend (con su test contra PostgreSQL) o usar el listado sin paginar; (3) campos de selección: condición fiscal (cinco valores fijos por CHECK) y condición de pago (texto libre de 60). Qué pasa con lista vacía y con un valor guardado que ya no está en la lista.

## 05/11/2026 00:05 · respuestas de Santi: 044 primero, paginación aparte, 045 parcial, campos de selección

**Instrucción recibida (verbatim, respuestas a cuatro preguntas; dos llegaron truncadas):**
> Paginación: «PR de backend aparte».
>
> Dependencia: Opción 2, con un paso previo.
>
> PRIMERO, y son 15 minutos: terminá la 044. El git merge develop resolviendo formulario.ts
> sin perder 'password' ni autocompletar, la suite de frontend, y el PR abierto. Eso ya estaba
> en la lista y es lo que la tiene detenida. Avisame cuando esté para que Javi la revise.
>
> DESPUÉS arrancá la 045 desde develop con consulta, alta y modificación. La baja queda afuera
> por ahora y el CA-001 se declara PARCIAL con el motivo: depende de la 044, que está en PR y
> sin mergear. Eso es exactamente lo que permite el punto 8 de la Definición de Terminado — un
> CA que no se puede verificar va declarado con su motivo, no escondido.
>
> No encadenes la rama de la 045 a la de la 044. Ya tuvimos cuatro ramas encadenadas esta
> semana y el costo fue una tarde de rebases.
>
> Y una cosa: si la 044 entra en develop ANTES de que termines la 045, hacé git merge develop
> en la rama de la 045 y agregá la baja con su confirmación en el MISMO PR, antes de abrirlo.
> En ese caso el CA-001 queda completo y la HU no se parte en dos. Es el escenario preferible,
> así que apuntá a eso; partirla es el plan de respaldo si Javi tarda.
>
> Condición fiscal: Opción 4. Antes de implementar, verificá el supuesto:
>
> docker exec -i lacentral_db psql -U lacentral -d lacentral -tAc "SELECT conname, pg_get_constraintdef(oid) FROM pg_constraint WHERE conrelid='cliente'::regclass AND contype='c' AND pg_get_constraintdef(oid) ILIKE '%condicion_fiscal%';"
> docker exec -i lacentral_db psql -U lacentral -d lacentral -c "SELECT coalesce(condicion_fiscal,'(NULL)') v, count(*) FROM cliente GROUP BY 1 ORDER BY 2 DESC;"
>
> La regla, que vale en los dos casos:
>
> 1 · EL VACÍO ES UN ESTADO VÁLIDO Y SE PUEDE GUARDAR VACÍO. El campo aparece sin
> seleccionar, con un texto de ayuda que diga que no está informada, y se puede guardar así.
> NO se obliga a elegir.
>
> El motivo es concreto: los clientes importados de Multisoft vienen con muchos campos sin
> informar — en condicion_pago    [TRUNCADO AQUÍ]
>
> Condición de pago: Opción 1: texto libre, como está.
>
> Motivo, que ya está medido: los valores reales de condicion_pago en la base son 12 e incluyen
> VOUCHER RAPITAXI, VALES TELETAXIS y ORDEN DE TRANSMITAXI en 384 clientes —instrumentos de las
> flotas de taxis, que son parte grande de la clientela—, dos plazos distintos de cuenta
> corriente y tres escrituras de contado. Por eso el hallazgo N-6 salió del script de
> normalización: la lista cerrada es una decisión de negocio que espera a Sergio. Está
> documentado en la §A.4 de ese script.
>
> Y DE PASO, algo que dijiste y que hay que mirar: decís que el campo es OBLIGATORIO. Pero
> 923    [TRUNCADO AQUÍ]

**Plan:**
1. 044: `git merge origin/develop` en `feature/hu-ui-06-confirmacion-con-motivo`, resolviendo
   `formulario.ts` sin perder `password` ni `autocompletar`; suite de frontend; PR. Avisar a Santi.
2. PR de backend aparte: paginación de `GET /api/clientes` y `/api/proveedores` (con test contra PostgreSQL).
3. 045 desde develop: consulta, alta y modificación; baja fuera, CA-001 PARCIAL con motivo.
   Si la 044 entra antes, merge de develop y la baja en el mismo PR.
4. Antes de la condición fiscal: verificar el supuesto con las dos consultas (solo lectura).
5. Preguntar lo que quedó truncado de las dos respuestas.

**Estado:** en curso
- 00:20 · Paso 1, 044: `git merge origin/develop` (`922e2b8`), un solo conflicto, en `formulario.ts`; se conservan `password`, `autocompletar`, `maximo` y `coincideCon`. Frontend 99/99, `ng build` limpio. ESTADO (`d174566`), push, **PR #144**. `verificador-tdd` lanzado sobre el #144 (la 044 nunca había pasado por él).
- 00:20 · **PR #145** de la 042 (ronda 2 APTO), con los CA, el recorrido contra la demo y el aviso de posible conflicto con el #141 en `articulos.ts`.
- 00:30 · Paso 4: no se pudo verificar el supuesto de la condición fiscal. `lacentral_db` no corre en esta máquina (no hay `docker` ni `psql`; la base está en la netbook por Tailscale). Las dos consultas las tiene que correr Santi.
- 00:30 · Leyendo el backend para el PR de paginación (sin worktree: el `verificador-tdd` del #144 está corriendo y su R1 aborta con worktrees): (a) `ClienteResumenDto` no tiene `Id`, y el detalle es `GET /api/clientes/{id}`: desde la lista no se puede abrir un cliente (proveedores sí tiene `Id`); (b) sin resultados, clientes y proveedores responden **404** con mensaje (CA-003 de HU-CLI-02 y de la HU de proveedores), no una página vacía. El listado de la 042 muestra el 404 como alerta de error, no como estado vacío.
**BLOQUEADO EN:** (1) qué hace el 404 de «sin resultados» con la paginación: pasa a 200 con página vacía o se queda; toca el CA-003 de dos HU cerradas. Depende de Santi. (2) las dos consultas de la condición fiscal, y lo que quedó truncado de sus dos respuestas. Depende de Santi.
- 00:45 · `verificador-tdd` sobre el #144, ronda 1: **NO APTO** (sobrevivían M11 y M12: `Validators.maxLength` y su mensaje en `formulario.ts`). Guarda inalcanzable en producción → se borró (`38aa3c6`); queda el test del atributo `maxlength` (M13). Frontend 99/99. Ronda 2: **APTO**. Cuerpo del #144 actualizado. **La 044 queda lista para Javi.**
- 00:50 · Cierre de sesión. Versionado `docs/sprint05/21-INSTRUCCIONES-CLAUDE-CODE-04-11.md`, que el ESTADO cita como instrucciones de arranque. En el árbol principal quedan sin commitear, a propósito: 371 cambios de modo `100644 → 100755` (ruido del sistema de archivos, sin contenido), `CLAUDE.md` con un borrador viejo de la regla de la bitácora (la versión vigente está en el #142) y `.serena/` (caché local de una herramienta). El principal está en la rama de la 044 en `04d7c41`, 45 commits detrás de su remoto. No se actualizó: los cambios de modo tocan archivos que el merge trae.
- **Quedé en:** paso 1 hecho (#144, APTO). Paso 2, el PR de backend con la paginación de clientes y proveedores, no empezado. Lo sigue la 045, desde develop.
- **Qué sigue, en orden:** (1) la respuesta de Santi al 404; (2) el PR de paginación, que suma el `Id` a `ClienteResumenDto`; (3) la 045 con consulta, alta y modificación, y la baja si la 044 ya entró; (4) `GUION-DEMO.md` el 05/11, antes de que Sergio vea la demo.
**BLOQUEADO EN:** (1) el 404 de «sin resultados» de clientes y proveedores: ¿pasa a 200 con página vacía al paginar? Toca el CA-003 de dos HU cerradas y lo decide Santi. (2) Las dos consultas de condición fiscal contra `lacentral`: las corre Santi en la netbook. (3) El resto de sus respuestas sobre condición fiscal y condición de pago, que llegaron cortadas. (4) Los merges de Javi, en este orden: #141, después #139, #140, #142, #143, #144 y #145.
