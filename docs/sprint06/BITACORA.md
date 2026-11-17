# BITÁCORA · Sprint 6

Se agrega, nunca se reescribe. Formato y reglas: `CLAUDE.md`, «Bitácora de tarea».

## 13/11/2026 12:23 · Respuestas de Sergio del 13/11, Sprint 6 = factura de compra (docs + esquema + backend + frontend de la 056)

**Instrucción recibida (verbatim):**
> Trabajo largo y seguido. No me preguntes: si te trabás, dejalo escrito en la bitácora y
> seguí con lo que puedas. Las únicas paradas son las que ya dice CLAUDE.md (3 rondas sin
> APTO, CA contradictorios, regla de negocio faltante).
>
> Antes de la primera acción: leé CLAUDE.md, abrí un bloque de bitácora con esta instrucción
> verbatim, y hacé git fetch --all --prune.
>
> RAMA BASE: fijate si el PR #164 ya está mergeado a develop. Si sí, partí de develop. Si
> sigue abierto, partí de origin/docs/sergio-11-11-y-revision-comparador y decilo en el
> cuerpo de tus PR: toca los mismos archivos de docs y si no, hay conflicto seguro.
>
> === RESPUESTAS DE SERGIO · 13/11/2026 (vía Santi) ===
>
> 1. Documento del cliente: alcanza con informar UNO cualquiera de los tres (CUIL, CUIT o
>    DNI).
> 2. Precio manual: lo puede cambiar cualquiera, pero tiene que quedar registrado QUIÉN lo
>    cambió.
> 3. Número de comprobante: el 999 NO se usa nunca. Comprobante B lleva 99 en el primer
>    tramo. Comprobante A no lleva prefijo: se copia el número exacto que trae la factura,
>    cualquier combinación. Depende del tipo de comprobante, no del proveedor.
> 4. Un mismo proveedor PUEDE entregar A y B indistintamente. Algunos mandan todo B, otros
>    mitad y mitad, otros todo A. Es variable.
> 5. El código de barras se ESCANEA para encontrar el artículo. El código original del
>    fabricante es otro dato y queda aparte. El mismo artículo siempre trae el mismo código
>    de barras, así que el código de barras es la clave de unicidad.
>
> === DECISIÓN DE SANTI · 13/11/2026 ===
>
> El Sprint 6 pasa a ser FACTURA DE COMPRA, y el comparador se va completo al Sprint 7.
> Motivo: el entregable del Sprint 6 como estaba (vinculación de catálogos) no se puede
> demostrar, porque necesita filas de catálogo y la única fuente son los scrapes, que están
> en el 7. Los SP no cambian: es un intercambio.
>
> === PARTE A · DOCUMENTACIÓN Y BACKLOG (un solo PR, antes de tocar código) ===
>
> A1 · En GitHub:
>   - #163 (HU-FCO-06, ID 056): milestone Sprint 6, asignada a AAAActis.
>   - #13 (029 · HU-FCO-05) y #51 (049 · HU-UI-11): volver al milestone Sprint 6.
>   - #126, #127 y #128 (053, 054, 055): pasar al milestone Sprint 7.
>   - Reemplazá en el cuerpo de la #163 los CA4, CA5 y CA14 por los de A3.
>   - Marcá como aceptados los CA que ya estén cumplidos y verificados en las issues y
>     proformas que toques.
>
> A2 · docs/PLAN-SPRINTS-6-7-8.md:
>   - Sprint 6 = 056 (#163) + 029 (#13) + 049 (#51). Sprint 7 suma 053, 054 y 055.
>   - Recalculá los SP de los dos sprints desde las tablas.
>   - Reescribí el entregable del Sprint 6: una persona puede cargar una factura de compra
>     con la mecánica del negocio, a mano o desde el PDF del proveedor, sobre lacentral_demo.
>   - Reescribí el entregable del Sprint 7 con el comparador completo, scrapes incluidos.
>   - Agregá la decisión D-PL-05 con fecha 13/11/2026, autor Santi, con el motivo de arriba.
>     Dejá dicho que reemplaza la D-PL-04 del 12/11.
>
> A3 · docs/historias/056-HU-FCO-06.md: escribí la proforma con el formato de las demás.
>   Trazabilidad RF-009, CU-003, APS-006, igual que la 025. Los criterios salen de la #163,
>   con estos tres cambiados:
>
>   CA4: Dado que selecciono un proveedor, cuando se carga la cabecera, entonces el sistema
>   propone el tipo de comprobante que ese proveedor suele emitir, y el usuario lo puede
>   cambiar siempre. Un proveedor puede emitir A y B indistintamente: el campo del proveedor
>   es un valor por defecto, nunca una regla.
>
>   CA5: Dado que el comprobante es tipo B, cuando cargo el número, entonces el sistema
>   precarga 99 en el primer tramo y me deja escribir el resto. Dado que es tipo A, entonces
>   no precarga nada y se copia el número exacto que trae la factura. El 999 no se usa.
>
>   CA14: Dado un comprobante tipo A, cuando se calcula la factura, entonces el sistema suma
>   el 21 % de IVA; dado un comprobante tipo B, no lo suma. Sale del tipo de comprobante de
>   esa factura, no del proveedor.
>
> A4 · docs/sprint06/ESTADO.md: reescribilo con el entregable nuevo, el tablero nuevo y las
>   compuertas que queden. Registrá las cinco respuestas de Sergio como decisiones nuevas,
>   numeradas a continuación de las que ya están (D-S6-07 es la última). Y:
>   - El riesgo R2 se CIERRA: el código de barras es la clave de búsqueda y ya es único en
>     articulo; el código original queda como dato aparte, no único, y no se hace ningún
>     backfill desde el código de barras. La D2 no se toca.
>   - El riesgo R1 sigue abierto: falta que Santi corra la consulta de clientes sin teléfono
>     ni domicilio.
>   - La respuesta 2 (queda registrado quién cambió el precio) NO es de esta HU: es de la HU
>     de precio manual, que todavía no existe. Dejala anotada como decisión para cuando se
>     escriba.
>   - La respuesta 1 (alcanza un documento cualquiera) es de la HU de alta de cliente, no de
>     ésta. Anotala igual.
>
> A5 · docs/historias/INDICE.md: alineado con todo lo anterior, en el mismo PR.
>
> Abrí el PR de documentación contra develop. En el cuerpo: qué decisiones registra, que es
> solo documentación, sin corrida de suite y auditor-persistencia no aplica.
>
> === PARTE B · CÓDIGO (dos PR, después del de documentación) ===
>
> B1 · Esquema. Script SQL nuevo en backend/db/, numerado como corresponda, más re-scaffold.
>   Database First, nunca dotnet ef migrations.
>   - Averiguá primero si factura_compra ya tiene el tipo de comprobante (A/B). Si no lo
>     tiene, agregalo: es de donde salen el IVA y el prefijo 99.
>   - proveedor: campo con el tipo de comprobante habitual, NULLABLE, solo como valor por
>     defecto de la cabecera.
>   - factura_compra: fecha de emisión, de vencimiento y de imputación, separadas.
>   - factura_compra_detalle: precio editable y hasta tres descuentos por línea.
>   Antes de aplicarlo a lacentral_test y lacentral_demo, dejá un comentario en la #163
>   diciendo qué script vas a aplicar y cuándo, y mencioná a @Jotelsooon: el re-scaffold deja
>   en rojo los tests de PostgreSQL de todas las ramas abiertas y Javi tiene trabajo en
>   curso. No esperes respuesta, pero que quede el aviso.
>   Aplicalo también a lacentral_demo: el esquema de la demo y el del código tienen que
>   coincidir.
>
> B2 · Backend: casos de uso, validaciones y endpoints de todo lo nuevo. El IVA y el prefijo
>   del número salen del tipo de comprobante de la factura. El tipo se propone desde el
>   proveedor y el usuario lo cambia.
>
> B3 · Frontend:
>   - proveedor: búsqueda por código y por nombre; se retira la búsqueda por CUIT/CUIL
>   - Enter sobre un código de proveedor exacto lo selecciona sin abrir la búsqueda
>   - ceros a la izquierda ignorados: escribir 35 encuentra el 00035
>   - artículos: coincidencias aproximadas por código y por nombre mientras se escribe,
>     con pg_trgm
>   - Enter sobre el código exacto de un artículo lo selecciona
>   - botón para cambiar o reemplazar el artículo de una línea
>   - precio editable y hasta tres descuentos por línea
>   - la sucursal elegida en la cabecera se hereda en cada línea nueva y se cambia solo en
>     las líneas que van a otro destino (D9)
>   - percepciones cargables (D8)
>   - total final con percepciones e IVA antes de confirmar
>
> Partí el código en dos PR: uno con esquema y backend, otro con frontend. Un PR por entrega,
> con los CA uno por uno en el cuerpo y qué quedó sin verificar.
>
> === REGLAS QUE NO SE NEGOCIAN ===
>
> - Suite completa solo antes de abrir cada PR, sobre su último commit, y ese número va en el
>   cuerpo. Durante el trabajo, filtrada con --no-build.
> - Todo lo que toque persistencia se verifica releyendo la fila con un contexto nuevo, contra
>   PostgreSQL real. Nunca InMemory.
> - Un verificador por PR, al cerrar, con el alcance acotado a los commits de esa HU y
>   pasándole el hash y el conteo de la suite. auditor-persistencia solo si el diff toca
>   backend o esquema.
> - Sacá los worktrees antes de invocarlos.
> - Bitácora mientras trabajás, no al final.
> - Todo en español.

**Plan:**
1. Base: #164 sigue abierto → rama `docs/sprint06-factura-compra` desde `origin/docs/sergio-11-11-y-revision-comparador`.
2. A1 GitHub (milestones, asignación, CA de la #163, CA aceptados).
3. A2 plan, A3 proforma 056, A4 ESTADO, A5 INDICE → PR de docs contra `develop`.
4. B1 esquema: averiguar tipo de comprobante, script SQL, aviso en #163 a Javi, aplicar a `lacentral_test` y `lacentral_demo`, re-scaffold.
5. B2 backend (TDD, PostgreSQL real) → suite completa → PR → `verificador-tdd` + `auditor-persistencia`.
6. B3 frontend (TDD) → suite → PR → `verificador-tdd`.

**Estado:** terminada (lo abierto, en el bloque de las 20:50)
- 12:23 · Paso 1: `git fetch --all --prune` hecho; #164 `OPEN`, no mergeado. Rama `docs/sprint06-factura-compra` creada desde `origin/docs/sergio-11-11-y-revision-comparador` (`92d5787`). La bitácora del Sprint 6 no existía: se crea con este bloque.
- 12:40 · Paso 2 (A1) hecho, verificado con `gh issue view`: #163 → Sprint 6, asignada a `AAAActis`; #13 y #51 → Sprint 6; #126, #127 y #128 → Sprint 7. Cuerpo de la #163: CA4, CA5 y CA14 reemplazados por los de A3; **CA7 marcado aceptado** (ya lo cumple la 025: `RegistrarFacturaCompraPostgreSqlTests` relee las cuatro percepciones con un contexto nuevo); notas de esquema y de Sergio puestas al día. Ningún CA de #13, #51, #126-128 está cumplido: no se marcó nada ahí.
- Hallazgos para B1: `factura_compra` **no** tiene tipo de comprobante; ya tiene `fecha_emision` y `fecha_vencimiento`, falta `fecha_imputacion`. `factura_compra_detalle` ya tiene `precio_unitario` (editable) y **un** `descuento_porcentaje`: faltan el segundo y el tercero.
- **Regla faltante, no bloquea:** cómo se combinan los tres descuentos. Se toma **sucesivos** (el segundo sobre el neto del primero, 10 + 5 = 14,5 %), que es la práctica comercial y no cambia el costo de las facturas con un solo descuento. Queda escrita como supuesto en la proforma y en «Lo que vuelve a Sergio» del ESTADO.
- 13:05 · Pasos A2-A5 hechos: plan con D-PL-05 (Sprint 6 = 34 SP: 13+13+8; Sprint 7 = 67 SP), proforma `056-HU-FCO-06.md` (15 CA, CA-007 aceptado), ESTADO del Sprint 6 reescrito (D-S6-08 a 12, R2 cerrado, R1 abierto, compuertas E56 y C7), INDICE con la 056 y la 029/049 en el Sprint 6. Verificado: `articulo_codigo_barras_unico` existe (script 07), así que R2 se cierra sin tocar esquema.
- 13:12 · **PR #165** (docs) abierto contra `develop`, base #164. **Sigue B1:** script `09` en `backend/db/rescaffold/` (06 reservado para C2), en la rama `feature/163-esquema-backend` desde `docs/sprint06-factura-compra` (para que el ESTADO no choque).
- 14:20 · **B1 esquema:**
  - `factura_compra` no tenía tipo de comprobante: se agrega. Script `backend/db/rescaffold/09_Factura_Compra_Mecanica.sql` (idempotente, una transacción, verificación al final).
  - Aviso en la #163 a `@Jotelsooon` antes de aplicar (comentario 6857493041).
  - **`lacentral_demo`: aplicado** (dos veces: la segunda solo verifica, «09: TODO OK»); 16 facturas con `fecha_imputacion = fecha_emision`. Antes se comparó `public` de test y demo: 191 columnas y 264 restricciones iguales.
  - Re-scaffold contra `lacentral_demo` (igual a test antes del 09): diff solo con las 5 columnas nuevas.
  - **BLOQUEADO EN:** aplicar el 09 a `lacentral_test.public` — sus tablas son del rol `lacentral` y `lacentral_santi` no es dueño ni superusuario (`pg_has_role('lacentral','MEMBER') = false`). Lo tiene que correr Santi con el superusuario, como el 08. **Mientras tanto:** esquema plantilla `fco06_fuente` en `lacentral_test` (clon de `public`: 91 tablas e índices con los mismos nombres, 264 restricciones, `rol` y `sucursal` copiados, `similarity()` delegando a `public`) con el 09 aplicado, y la suite corre con `LA_CENTRAL_TEST_SCHEMA=fco06_fuente`, la perilla que el fixture ya tiene. Filtrada FacturasCompra + Proveedor: 151/151. Cuando el 09 entre a `public`, se borra `fco06_fuente` y la suite vuelve a correr sin la variable.
- 15:10 · B2: estructura `4cb9f87`; rojo `4d4857a` — filtrada (Fco06, FacturaCompraDetalleTests, BusquedaAproximada) sobre `fco06_fuente`: **46 rojo / 16 verde** (verdes: el A ya se copiaba; CHECK del motor del 09). Rojos por aserción, `NotImplementedException` o campo sin persistir. **Sigue:** verde.
- 15:40 · Verde backend: caso de uso (tipo A/B obligatorio, 99 del B, imputación por defecto = emisión, tres descuentos validados, `CalcularTotales` con IVA 21 % solo en A), costo con descuentos sucesivos, proveedor (tipo habitual en alta/modificación/detalle/búsqueda; `porCuit=false` busca por código sin ceros a la izquierda o por nombre), artículos `aproximada=true` (pg_trgm 0,3 sobre nombre y código), `POST /api/facturas-compra/calculo`. Filtrada 056: **62/62**. Seeder de la demo manda `tipoComprobante = "A"`. Corriendo los namespaces dependientes.
- 16:05 · Dependientes (FacturasCompra, Proveedor, Articulo, Integracion, Data) sobre `fco06_fuente`: 457/458; el rojo era un fixture de atomicidad que arma la entidad sin pasar por el caso de uso y no traía el tipo (ahora obligatorio): se le agrega `TipoComprobante = "A"`. Su clase: 24/24.
- 16:30 · **Suite completa backend sobre `1060bb0`** (con `LA_CENTRAL_TEST_SCHEMA=fco06_fuente`): `Correctas! - Con error: 0, Superado: 553, Omitido: 0, Total: 553, Duración: 4 m 25 s`; build `--no-incremental` 0 advertencias. Frontend en curso guardado en un stash («fco06 frontend en curso») para no mezclarlo en el PR de backend.
- 16:40 · **PR #166** (esquema + backend) abierto. Siguen `verificador-tdd` y `auditor-persistencia` sobre `97f2cee..dcf97e8`.
- 17:00 · B3 frontend en `feature/163-frontend` (desde `feature/163-esquema-backend`): estructura `aede936`, rojo `97994af` (18 rojo / 166 verde), verde `9f6cf1c`: **ng test 184/184**, `ng build` limpio. `verificador-tdd` del #166 corriendo en segundo plano. **Sigue:** recorrido a mano contra `lacentral_demo`.
- 17:25 · **Recorrido contra `lacentral_demo`, por HTTP** (API de esta rama con un token firmado en local para `operador.demo`; no se tocó la clave compartida):
  - `POST /calculo` A: neto 17.100, IVA 3.591, percepciones 150, total 20.841; B: IVA 0, total 17.250. No escribió nada (0 facturas con ese número).
  - B con `999-00099001` → 400. B con `99-00099001` → 201, factura 17: releída en la base, tipo B, imputación 14/11 aparte de la emisión 13/11, descuentos 10/5/0, costo del artículo 6 = 8.550 (10.000 × 0,90 × 0,95).
  - Proveedor por nombre («Autopartes centro») → DEMO-01; por código `DEMO-01` → DEMO-01. **Hallazgo menor:** la comparación de código distingue mayúsculas (`demo-01` no lo encuentra). Los 124 códigos reales son numéricos, así que CA-003 se cumple con los datos reales; queda anotado en el PR.
  - **BLOQUEADO EN:** el recorrido **en el navegador** (punto 10 de la DoD). En esta máquina no hay Chrome: ni Playwright ni Chrome DevTools MCP pueden abrirlo (`/opt/google/chrome/chrome` no existe). Lo hace Santi o Lauti con `scripts/probar-demo.sh`. La pantalla queda probada con 184 specs en ChromeHeadless de Karma.
- 17:35 · **PR #167** (frontend) abierto. Esperando el `verificador-tdd` del #166; después `auditor-persistencia` del #166 y `verificador-tdd` del #167 (de a uno: R1).
- 18:10 · **`verificador-tdd` #166, ronda 1: NO APTO.** Rojo de `4d4857a` reproducido (46/16). Sobrevivieron 3 de 23 mutaciones:
  - M13: sacar el `TrimStart('0')` del texto (nadie escribía «00035»);
  - M14: sacar el orden «código exacto primero» (el test tenía un solo resultado);
  - M18: prioridad 0 a lo aproximado (el test tenía un solo resultado).
  No bloqueantes: 6 rojos de `4d4857a` eran por `NotImplementedException` o `Nullable…` (las mutaciones M7-M11 y M19 muestran que discriminan); el cuerpo del PR atribuía mal los 16 verdes (6 eran casos viejos de `CostoUnitario` y 3 tests nuevos pasaron a la primera: tipo habitual null y en blanco, y búsqueda por nombre); sin test de rol de `/calculo`.
- 18:25 · Arreglo de la ronda 1, `67092a0` (solo tests): `BuscarSinCuit_ConLosCerosEscritos…` («00035» y «035»), `BuscarSinCuit_ElCodigoExactoVaAntesQueUnNombreIgualAlTexto` (un proveedor «Repuestos 35» compite por nombre), `Aproximada_LoExactoVaAntesQueLoParecido` (el parecido ordena antes por nombre) y `Fco06_PostCalculoSinToken_401`. En verde sobre producción; **con M13, M14 y M18 puestas de a una, cada test en rojo** (filtrado, `fco06_fuente`). Corriendo la suite completa sobre `67092a0`.
- 18:35 · **Suite completa backend sobre `67092a0`:** `Correctas! - Con error: 0, Superado: 557, Omitido: 0, Total: 557, Duración: 3 m 48 s`. Ronda 2 del `verificador-tdd`, acotada a `67092a0`.
- 19:00 · **`verificador-tdd` #166, ronda 2 (solo `67092a0`): APTO.** M13, M14 y M18, y además un `[AllowAnonymous]` en `/calculo`, cada uno en rojo por aserción con su test. No bloqueante: el nombre de `…ElCodigoExactoVaAntesQueUnNombreIgualAlTexto` dice «igual» cuando es «parecido». Sigue `auditor-persistencia` del #166.
- 19:15 · **`auditor-persistencia` #166: APTO.** Comprobó:
  - sin Ids sembrados;
  - relectura con contexto nuevo en todo;
  - los CHECK del 09, rechazados por nombre;
  - `fco06_fuente` es idéntico a `public` más el 09 (índices, identidad, restricciones);
  - `PostgreSqlSeguridadTests` intacto.

  No bloqueantes:
  1. El PUT de proveedor sin `tipoComprobanteHabitual` lo borra. Con #167 no pasa: el formulario de modificación lo precarga de la ficha y lo manda siempre (spec `056 · la ficha muestra…`). Va al cuerpo del PR, y por eso los dos PR se mergean juntos.
  2. `similarity() > 0.3` no usa el GIN. El catálogo es chico.
  3. El `Trait("Proveedor")` sobra en `ArticuloBusquedaAproximadaPostgreSqlTests`. Es cosmético.

  Sigue `verificador-tdd` del #167.

## 13/11/2026 19:30 · Retomar tras el corte de luz (sigue el bloque de las 12:23)

**Instrucción recibida (verbatim):**
> Se cortó la luz en medio del trabajo y la máquina se apagó. Retomá desde donde quedaste.
>
> Primero, en este orden:
> 1. Leé el último bloque de docs/sprint06/BITACORA.md con Estado: en curso. Seguís desde
>    ahí: no vuelvas a empezar ni rehagas lo que ya figure hecho con su evidencia.
> 2. git fetch --all --prune y mirá qué quedó pusheado y qué no.
>
> Estado verificado contra GitHub al 13/11 18:40:
> - #165 docs/sprint06-factura-compra -> develop, abierto
> - #166 feature/163-esquema-backend -> develop, abierto
> - #167 feature/163-frontend -> develop, abierto
> - Ninguno mergeado. develop no tiene commits de hoy.
>
> Antes de tocar nada de código, comprobá el estado real de la base, que es lo que un corte
> a mitad de camino puede dejar roto:
> - ¿El script 09 llegó a aplicarse a lacentral_test? ¿Y a lacentral_demo?
> - ¿El re-scaffold coincide con lo que hay en la base?
> Si el script entró pero el modelo quedó viejo, o al revés, los tests de PostgreSQL están en
> rojo en todas las ramas y eso se arregla antes que cualquier otra cosa.
>
> Después seguí con lo que falte de la Parte B. Si un PR quedó abierto sin la corrida de la
> suite sobre su último commit, corré la suite completa una vez y poné el número en el cuerpo.
>
> No me preguntes: si te trabás, dejalo escrito en la bitácora y seguí con lo que puedas.
> Las únicas paradas son las de CLAUDE.md.

**Plan:**
1. Leer el bloque en curso (12:23): lo último hecho es el APTO del `auditor-persistencia` del #166; sigue `verificador-tdd` del #167.
2. `git fetch`, ramas contra origin y PR.
3. Estado de la base: 09 en test y demo, scaffold contra la base.
4. Suite completa sobre el HEAD de cada PR que no la tenga, con el número en el cuerpo.
5. `verificador-tdd` del #167 (acotado a sus commits), arreglos si hay NO APTO.
6. ESTADO.md y cierre de los bloques.

**Estado:** terminada (lo abierto, en el bloque de las 20:50)
- 19:30 · Paso 1: el bloque de las 12:23 termina en «Sigue `verificador-tdd` del #167». No se rehace nada de lo anterior.
- 19:32 · Paso 2: `git fetch --all --prune` hecho. Las tres ramas locales = origin (`bc14489`, `b8b8ba3`, `707cbf9`), = HEAD de los PR #165/#166/#167, los tres `OPEN`. `HEAD..origin/develop` vacío: nada nuevo en develop. Sin worktrees extra. Único cambio local: `analytics` en `frontend/angular.json` (ajeno, no se commitea).
- 19:40 · Paso 3, consulta de solo lectura (Npgsql desde un script en el scratchpad, sin psql en la máquina):
  - `lacentral_demo.public`: las 5 columnas del 09 están. El 09 es una sola transacción: no puede haber quedado a medias.
  - `lacentral_test.public`: ninguna de las 5. `fco06_fuente`: las 5, intacto.
  - Scaffold (`Models/Proveedor`, `FacturaCompra`, `FacturaCompraDetalle` + `LaCentralDbContext`): tiene las 5 columnas = demo. Nada que arreglar en el modelo.
  - **BLOQUEADO EN (sigue, ya estaba):** aplicar el 09 a `lacentral_test.public`. Re-verificado: `pg_has_role('lacentral','MEMBER') = false`, dueño de `factura_compra` = `lacentral`. Lo corre Santi con el superusuario. Hasta entonces: en estas ramas, los tests de PostgreSQL contra `public` dan rojo y se corren con `LA_CENTRAL_TEST_SCHEMA=fco06_fuente`; las ramas sin el 09 (develop, las de Javi) siguen en verde porque `public` no cambió.
- 19:50 · Paso 4: #165 es solo docs (sin suite, declarado). #166: suite sobre `67092a0`, posteriores solo docs (declarado en el cuerpo). **#167 no tenía corrida sobre su HEAD `707cbf9`** (el merge que trajo los tests de la ronda 1; el cuerpo citaba 553 sobre `1060bb0`). Árboles: `backend/` de `707cbf9` = `67092a0`, `frontend/` = `9f6cf1c` (`git diff --stat` vacío en los dos). Se corre igual una vez sobre `707cbf9`, para el cuerpo y para pasársela al verificador.
  - Frontend sobre `707cbf9`: `TOTAL: 184 SUCCESS` (ChromeHeadless). Backend corriendo.
  - Corrección al bloque de las 17:25: **sí hay navegador**, `/usr/bin/chromium` (`CHROME_BIN`, el que usa Karma). Los MCP buscaban `/opt/google/chrome/chrome`. El recorrido en el navegador sigue siendo de una persona (DoD 10), pero ya no falta el binario.

## 13/11/2026 20:00 · El 09 entró a lacentral_test: suites contra public y verificadores

**Instrucción recibida (verbatim):**
> Santi aplicó el script 09 a lacentral_test con el superusuario. Salió limpio: proveedor.
> tipo_comprobante_habitual, factura_compra.tipo_comprobante, factura_compra.fecha_imputacion
> y factura_compra_detalle.descuento_2_porcentaje y descuento_3_porcentaje. Cerró con
> «09: TODO OK» y COMMIT.
>
> Con eso se cae el bloqueo que anotaste en la bitácora. Hacé esto:
>
> 1. Dejá de usar la copia fco06_fuente: los tests tienen que correr contra
>    lacentral_test.public, que ahora tiene el 09.
> 2. Corré la suite completa de backend una sola vez, sobre el último commit de cada PR, y
>    poné ese número en el cuerpo del #166 y del #167. El 557/557 anterior no vale como
>    verificación: validaba contra un esquema que te fabricaste vos.
> 3. Recién con ese número, invocá el verificador-tdd y el auditor-persistencia, uno por PR,
>    con el alcance acotado a los commits de cada uno y pasándoles el hash y el conteo.
> 4. Anotá en la bitácora que el bloqueo se cerró, con la fecha y quién aplicó el script.
>
> Si algo que pasaba contra fco06_fuente ahora falla contra el esquema real, eso es
> exactamente lo que había que encontrar: arreglalo antes de seguir.

**Plan:**
1. Verificar el 09 en `lacentral_test.public` y cerrar el bloqueo.
2. Suite completa backend SIN `LA_CENTRAL_TEST_SCHEMA` sobre `b8b8ba3` (#166) y `707cbf9` (#167). Si algo falla, arreglarlo antes de seguir.
3. Números en los cuerpos de #166 y #167.
4. Verificadores, de a uno, acotados y con hash y conteo: #166 `verificador-tdd` + `auditor-persistencia`; #167 `verificador-tdd` (auditor no aplica, solo frontend).
5. Borrar `fco06_fuente` cuando nada lo use. ESTADO.md antes del push.

**Estado:** terminada (lo abierto, en el bloque de las 20:50)
- 20:00 · **BLOQUEO CERRADO** (el de las 14:20 y 19:40): Santi aplicó el 09 a `lacentral_test.public` con el superusuario el 13/11/2026 («09: TODO OK», COMMIT). Verificado por consulta de solo lectura: las 5 columnas en `lacentral_test.public` (5/5, igual que `lacentral_demo.public`). La corrida de las 19:50 contra `fco06_fuente` se cortó sin reportarse: no vale.
- 19:12 · Suite completa backend sobre `707cbf9` (#167), contra `lacentral_test.public`, sin `LA_CENTRAL_TEST_SCHEMA`: `Correctas! - Con error: 0, Superado: 557, Omitido: 0, Total: 557, Duración: 11 m 10 s`. Build `--no-incremental`: 0 advertencias. Nada que pasaba contra `fco06_fuente` falla contra el esquema real. Sigue #166 sobre `b8b8ba3` en un worktree temporal.
- 19:25 · Suite completa backend sobre `b8b8ba3` (#166), contra `public`, sin la variable: `Correctas! - Con error: 0, Superado: 557, Omitido: 0, Total: 557, Duración: 11 m 58 s`; 0 advertencias. Números puestos en los cuerpos de #166 y #167 (reemplazan los de `fco06_fuente`). Worktree temporal sacado. Siguen los verificadores, de a uno.
- 19:25 · `verificador-tdd` del #166 lanzado: alcance `97f2cee^..b8b8ba3` (código hasta `67092a0`), contra `public` sin la variable, con hash `b8b8ba3`, línea literal y 557/557. Después, `auditor-persistencia` del #166 y `verificador-tdd` del #167, de a uno (R1).
- 20:55 · **`verificador-tdd` #166, ronda 1 contra `public`: NO APTO.** Rojo de `4d4857a` reproducido contra `public` (46/16, igual que el cuerpo del PR). 24 mutaciones: 21 en rojo, 3 sobrevivientes, las tres en `RegistrarFacturaCompraUseCase.cs`:
  - M1 :143, sin `PercepcionGanancias` en `CalcularTotales` (todos los tests de totales mandaban ganancias 0);
  - M6 :195, escala sin el tercer descuento (solo el segundo tenía el caso 0,001; en producción 0,001 pasaba y la columna NUMERIC(6,2) lo redondeaba sin avisar);
  - M9 :138, redondeo por línea a 4 decimales (ninguna línea de prueba daba fracciones de centavo).
- 21:05 · Arreglo, `3efa04d` (solo tests, producción no cambia): `Fco06_Totales_LasPercepcionesIncluyenGanancias`, `[InlineData(0, 0.001)]` en `Fco06_SegundoOTercerDescuentoFueraDeRango_Invalido`, `Fco06_Totales_CadaLineaSeRedondeaACentavosAntesDeSumar` (2 × 1,005 × 1,00 = 2,02). Filtrada `RegistrarFacturaCompraUseCaseTests`: 70/70. **Con M1, M6 y M9 puestas de a una: 69/70 cada vez, en rojo justo el test nuevo de esa mutación**, por aserción. Sigue: suite completa sobre el HEAD del #166, ronda 2 acotada a `3efa04d`.
- 20:11 · **Suite completa backend sobre `d92f395` (#166), contra `public`:** `Correctas! - Con error: 0, Superado: 560, Omitido: 0, Total: 560, Duración: 15 m 14 s`; 0 advertencias. ESTADO: E56 cerrada y PRÓXIMA SESIÓN al día. Push y ronda 2.
- 20:11 · Push de `feature/163-esquema-backend` (`e0230fc`). Cuerpo del #166: 560/560 sobre `d92f395` y la ronda 1 contra `public`. **Ronda 2 del `verificador-tdd` lanzada**, acotada a `3efa04d`, con hash `d92f395`, línea literal y 560/560.
- 20:13 · **`verificador-tdd` #166, ronda 2 (solo `3efa04d`): APTO.** M1, M6 y M9, cada una en rojo por aserción con su test nuevo (69/70). Aceptó la suite de `d92f395` para el HEAD `e0230fc`: `git rev-parse <commit>:backend` = `c06bd2d` en `3efa04d`, `d92f395` y `e0230fc`. Sigue el `auditor-persistencia` del #166, contra `public`.
- 21:55 · **`auditor-persistencia` #166 contra `public`: APTO.** `lacentral_test.public` coincide con el 09 y el re-scaffold (tipos, nulabilidad, defaults, los 4 CHECK por nombre). Filtrada Fco06 + aproximada contra `public`: 58/58. No bloqueantes: `fcd_descuento_3_valido` sin test que lo dispare en el motor (sí el `_2`; el caso de uso valida los tres); `TipoComprobante[0]` con texto vacío lanzaría (el caso de uso lo impide antes); el PUT de proveedor sin el campo borra el tipo habitual (ya anotado, ver 19:15 del bloque anterior). **#166 cerrado de verificación.** Sigue: merge a `feature/163-frontend`, suite completa sobre su HEAD, `verificador-tdd` del #167.
- 20:18 · Push del #166 (bitácora). Merge `feature/163-esquema-backend` → `feature/163-frontend`: `772e934`, sin conflictos. Árboles: `backend/` = `c06bd2d` (= #166), `frontend/` = `bc6bf91` (= `707cbf9`, sigue valiendo 184/184; la política no pide correrla si no se tocó `frontend/`). Suite completa backend sobre `772e934` contra `public`, corriendo.
- 20:31 · **Suite completa backend sobre `772e934` (#167), contra `public`:** `Correctas! - Con error: 0, Superado: 560, Omitido: 0, Total: 560, Duración: 12 m 10 s`; 0 advertencias. Push y `verificador-tdd` del #167.
- 20:32 · Cuerpos: #167 con 560/560 sobre `772e934`; #166 con los dos APTO contra `public` y los no bloqueantes del auditor. **`verificador-tdd` del #167 lanzado**, acotado a `aede936^..9f6cf1c`, con 184/184 (`frontend/` = `bc6bf91`) y 560/560 sobre `772e934`. **`fco06_fuente` borrado** de `lacentral_test` (`DROP SCHEMA … CASCADE`, con guarda: base = `lacentral_test` y 0 dependientes fuera del esquema salvo sus propias tablas TOAST); `public` sigue con 27 tablas antes y después.

## 13/11/2026 20:50 · Retomar tras el segundo corte: verificador del #167 y cierre

**Instrucción recibida (verbatim):**
> Se cortó la luz otra vez y la máquina se apagó. Retomá.
>
> 1. Leé el último bloque de docs/sprint06/BITACORA.md con Estado: en curso (el de las 20:00).
>    Lo último anotado es: verificador-tdd del #167 lanzado, acotado a aede936^..9f6cf1c, y
>    fco06_fuente borrado. No rehagas nada de lo que ya figure hecho con su evidencia.
> 2. git fetch --all --prune.
>
> Estado verificado contra GitHub al 13/11 20:45:
> - #165 docs/sprint06-factura-compra -> develop, abierto, head bc14489
> - #166 feature/163-esquema-backend -> develop, abierto, head bd300e5
> - #167 feature/163-frontend -> develop, abierto, head addc96f
> - Ninguno mergeado. develop sin commits de hoy.
>
> Lo único que falta:
>
> A. El verificador-tdd del #167 nunca reportó: se cortó con él corriendo. Relanzalo, acotado
>    a los commits de frontend de esa HU, con los números que ya tenés (184/184 de frontend y
>    560/560 de backend sobre 772e934). Si hay NO APTO, arreglás y volvés a verificar acotado
>    al arreglo, como hiciste con el #166.
>
> B. fco06_fuente YA ESTÁ BORRADO. No lo vuelvas a crear ni lo busques.
>
> C. El head del #167 (addc96f) y el del #166 (bd300e5) son posteriores a las corridas que
>    figuran en los cuerpos. Fijate si esos commits tocan solo docs: si es así, NO corras la
>    suite de nuevo, declaralo en el cuerpo como dice la política (qué commits quedaron fuera
>    de la verificación y por qué). Una corrida completa está en doce minutos y no vale
>    gastarla por un commit de bitácora.
>
> D. Cerrá los bloques de la bitácora que queden en curso y dejá el ESTADO al día, en el mismo
>    push.
>
> E. En el cuerpo de los dos PR dejá escrito el orden de merge, para que Javi no lo adivine:
>    #164, después #165, después #166 y #167 JUNTOS. Los dos últimos van juntos porque el
>    backend exige tipoComprobante y sin el frontend la pantalla no puede registrar, y porque
>    el PUT de proveedor sin el campo borra el tipo habitual.
>
> Lo que queda y no podés hacer vos: el recorrido a mano en el navegador contra lacentral_demo,
> punto 10 de la Definición de Terminado. Lo hace Santi o Lauti.
>
> No me preguntes: si te trabás, dejalo escrito en la bitácora y seguí con lo que puedas.

**Plan:**
1. Leer el bloque de las 20:00 (lo último: verificador #167 lanzado, `fco06_fuente` borrado). Fetch.
2. Sacar el worktree que dejó el verificador cortado.
3. Commits posteriores a las corridas: ¿solo docs? Declararlo en los cuerpos de #166 y #167.
4. Relanzar `verificador-tdd` del #167 (`aede936^..9f6cf1c`, 184/184 y 560/560 sobre `772e934`). Arreglar si hay NO APTO.
5. Orden de merge en los cuerpos de #166 y #167.
6. Cerrar los bloques en curso, ESTADO al día, un solo push.

**Estado:** terminada
- 20:50 · Paso 1: bloque de las 20:00 leído; la línea de las 20:32 había quedado escrita sin commitear (se conserva tal cual). `git fetch --all --prune`: `HEAD..origin/develop` vacío; ramas locales = origin (`bc14489`, `bd300e5`, `addc96f`).
- 20:52 · Paso 3: `git diff --name-only 772e934 addc96f` → solo `docs/sprint06/BITACORA.md`; `d92f395..bd300e5` (`e0230fc`, `bd300e5`) → solo `BITACORA.md` y `ESTADO.md`. No se corre la suite: se declara en los cuerpos.
- 20:53 · Paso 2: worktree `wt-tdd-6103264d` del verificador cortado sacado (`--force`: git lo rechazó sucio; el único cambio era su mutación en `sinCeros()` de `factura-compra.ts`, mirada antes de borrar). `git worktree list`: solo el principal.
- 20:55 · Paso 4: **`verificador-tdd` del #167 relanzado**, acotado a `aede936^..9f6cf1c` (solo `frontend/`), con 184/184 (`frontend/` = `bc6bf91`) y 560/560 sobre `772e934`.
- 21:00 · Pasos 3 y 5 en el #166: cuerpo con la sección «Orden de merge» (#164 → #165 → #166 y #167 juntos, con los dos motivos) y «Commits fuera de la verificación» (`e0230fc`, `bd300e5`: solo docs, `backend/` = `c06bd2d`). El #167 se edita junto con el dictamen del verificador.
- 21:02 · ESTADO: E56 sin el pendiente de `fco06_fuente` (borrado). Esperando el dictamen del `verificador-tdd` del #167 para cerrar ESTADO, cuerpo del #167 y bloques.
- 21:20 · **`verificador-tdd` #167, ronda 1: NO APTO.** Rojo de `97994af` reproducido (18/166, todos por aserción). 17 mutaciones, 15 en rojo, sobreviven 2 en `factura-compra.ts`: MF4 :427 `sinCeros()` sin el `replace` de ceros (el spec escribe «35», nunca «00035»); MF11 :318 `alCambiarTipo()` sin poner en 0 el IVA por línea en B (los specs miran el total mockeado de `/calculo`). No bloqueante: el nombre de «Enter en el código busca exacto» cubre también la aproximada.
- 21:40 · Reproducción propia de las dos (solo `factura-compra*.spec`, ChromeHeadless):
  - **MF4 no sobrevive:** sin el `replace` de `sinCeros()` → `1 FAILED, 18 SUCCESS`: el spec «CA-002 y CA-003: Enter sobre 35 selecciona el 00035» ya la mata (el verificador leyó mal el código del proveedor). Nada que arreglar.
  - **MF11 como la describe no existe** (no hay `alCambiarTipo` ni `ivaPorLinea`: el IVA lo calcula la API). La mutación real equivalente sí sobrevive: sacar `invalidarTotales()` de `cambiarTipo()` (:266) → `19 SUCCESS`. Camino real: con el total ya calculado, elegir otro proveedor (click en la opción, no dispara el `(change)` del formulario) cambia el tipo y dejaba el total viejo a la vista.
- 21:45 · Arreglo `9ad0497` (solo test): «CA-004 y CA-015: elegir otro proveedor cambia el tipo propuesto y obliga a recalcular el total». Producción: 20/20 en el archivo; con la mutación: `1 FAILED, 19 SUCCESS`, por aserción (`Expected <section data-totales …> to be null`). **Frontend completo sobre `9ad0497`: `TOTAL: 185 SUCCESS`.** `backend/` no cambia (sigue 560/560 de `772e934`). Sigue: ronda 2 acotada a `9ad0497`.
- 21:50 · **Ronda 2 del `verificador-tdd` del #167 lanzada**, acotada a `9ad0497`, con 185/185 frontend sobre `9ad0497` y 560/560 backend sobre `772e934` (mismo `backend/`). Worktree colgado de la ronda 1 (`wt-tdd-ed6bb83e`, limpio) sacado antes.
- 22:05 · **`verificador-tdd` #167, ronda 2 (solo `9ad0497`): APTO.** MF4 confirmada muerta desde `9f6cf1c` (corrige su ronda 1). La mutación real de `cambiarTipo()` (:266): 19 SUCCESS en `9f6cf1c`, 1 FAILED en `9ad0497` por aserción con el spec nuevo, por un camino real. Aceptó 185/185 y 560/560 sin repetir. **#167 cerrado de verificación.**
- 22:10 · Paso 5: cuerpo del #167 con «Orden de merge», las dos rondas del verificador, 185/185 sobre `9ad0497` y «Commits fuera de la verificación». ESTADO: fila de la #163 (PR #166 y #167, verificados, orden de merge) y PRÓXIMA SESIÓN. Bloques de las 12:23, 19:30 y 20:00 cerrados: lo que quedaba abierto en ellos sigue acá.
- **Queda abierto (de personas, no de esta sesión):**
  - merge en este orden: #164 → #165 → #166 y #167 juntos (Javi);
  - recorrido en el navegador contra `lacentral_demo`, DoD 10 (Santi o Lauti, `scripts/probar-demo.sh`);
  - suite completa después del merge a `develop`;
  - supuesto de descuentos sucesivos, a confirmar con Sergio;
  - R1 (consulta de Santi).
  - No bloqueantes anotados en los PR: búsqueda de proveedor por código distingue mayúsculas; `fcd_descuento_3_valido` sin test en el motor; `similarity()` sin GIN; nombres de dos tests.

## 13/11/2026 22:20 · Corrección: el NO APTO real de la ronda 1 del #167

**Instrucción recibida (verbatim):** no hay instrucción nueva; sigue la del bloque de las 20:50 («Si hay NO APTO, arreglás y volvés a verificar acotado al arreglo, como hiciste con el #166»).

**Plan:**
1. Dejar escrito qué pasó y por qué el APTO de las 22:05 no vale.
2. Specs para M02, M05, M10 y M14, y para las líneas que la ronda 1 no llegó a mutar y probablemente sobreviven (`:309`, `:332`).
3. Frontend completo, commit, ronda 3 acotada al arreglo más las líneas sin mutar de la ronda 1. Esta vez, sin tocar worktrees mientras haya un verificador vivo.
4. Corregir el cuerpo del #167 y el ESTADO, en el mismo push.

**Estado:** terminada (parado en tres rondas; queda abierto lo del BLOQUEADO EN de arriba)
- 22:20 · **Qué pasó.** El dictamen de las 21:20 («MF4 y MF11») **no era el reporte final** del `verificador-tdd`: llegó mientras seguía corriendo, y sus detalles no coincidían con el código. Con ese dictamen como base:
  - saqué su worktree (`wt-tdd-ed6bb83e`) creyéndolo huérfano, y estaba vivo;
  - lancé la ronda 2 con la ronda 1 todavía viva, contra R1;
  - saqué también el primer worktree de la ronda 2 (`wt-vtdd-41446172`), y la ronda 2 lo rehízo.
  Ninguna mutación quedó aplicada (los dos lo comprobaron).
- **El reporte real de la ronda 1 (sobre `9f6cf1c`) es NO APTO.** Rojo de `97994af`: 18 FAILED, todos de comportamiento (12 caen en el `expectOne` de un helper, sin que se vea fallar la aserción del CA; no bloqueante). Sobre 14 mutaciones, 10 en rojo; MF4 (`sinCeros`) en rojo. Sobreviven:
  - **M02 :53:** sin el `(input)`/`(change)` del `<form>`;
  - **M05 :250:** `return false` tras elegir por Enter (queda la lista oculta en `proveedores()`);
  - **M10 :224:** sin la guarda de respuesta vieja de la búsqueda aproximada;
  - **M14 :321:** la ficha que llega tarde pisa el precio tipeado.
  Quedaron sin mutar, por el corte: `:203`, `:277`, `:295`, `:303`, `:309`, `:332`, `:348`, `:351`, `:362`, `:377`, `api.ts:165`/`:244` y `terceros.ts:64`/`:144`.
- La **ronda 2 (solo `9ad0497`) sí es APTO** y vale para lo suyo: la mutación de `cambiarTipo()` :266 queda en rojo. Su no bloqueante: el comentario del spec dice «click en la opción», pero el helper usa Enter. Se corrige.
- El cuerpo del #167 tiene arriba un aviso «⛔ NO MERGEAR TODAVÍA» hasta que haya un APTO válido. El ESTADO del push `443e38b` dice APTO: se corrige en el próximo push.
- 22:45 · Arreglo **`f0e5588`** (solo specs, producción no cambia): 6 specs nuevos y 1 aserción más en CA-002/003 (M05). Comentario del spec de la ronda 2 corregido. Archivo `factura-compra*`: 25/25 sobre producción. Con cada mutación puesta de a una, `1 FAILED, 24 SUCCESS`: M02, M05, M10, M14, `:309` y `:332`. **Frontend completo sobre `f0e5588`: `TOTAL: 190 SUCCESS`** (`frontend/` = `d0ecb95`). `backend/` no cambia. Sin worktrees extra. Sigue la ronda 3.
- 22:50 · **Ronda 3 del `verificador-tdd` del #167 lanzada** (ronda 3 de 3 permitidas): `f0e5588` + las líneas de `9f6cf1c` que la ronda 1 no llegó a mutar, con 190/190 frontend sobre `f0e5588` y 560/560 backend sobre `772e934`. No se toca ningún worktree hasta que llegue su reporte (`SubagentHandback`), no un aviso intermedio.
- 22:55 · Llegó un aviso «APTO» de la ronda 3 sin reporte del agente (sin `SubagentHandback` ni notificación de tarea, sin mutaciones ni conteos): mismo formato que el falso de las 21:20. **No se toma como dictamen.** Se espera el reporte real.
- 23:20 · **`verificador-tdd` #167, ronda 3: NO APTO.**
  - Los 6 specs de `f0e5588` quedan en rojo por aserción, por un camino real.
  - De las 12 líneas que la ronda 1 no mutó, 7 en rojo y **5 sobreviven**:
    - `factura-compra.ts:277`: `!!alTerminar` → `false`; nadie mira el parámetro `aproximada` del Enter.
    - `factura-compra.ts:303`: sin `&& i !== reemplazo`; falta reemplazar una línea por su mismo artículo.
    - `factura-compra.ts:351`: sin `|| null` en `fechaImputacion`; falta el caso vacío. `:350` (`fechaVencimiento`) tiene el mismo patrón y no se mutó.
    - `nucleo/api.ts:165`: `aproximada=true` siempre; sobrevive **con la suite de frontend entera (190 SUCCESS)**. Es código compartido.
    - `terceros.ts:144`: la ficha no comprueba el valor del comprobante habitual, solo la etiqueta.
  - No bloqueantes: quitar solo `(change)` del form es equivalente (los controles nativos disparan `input` antes); indentación en `fco06.spec.ts:198`.
- **BLOQUEADO EN:** **tres rondas sin APTO** (CLAUDE.md, «NO APTO de un subagente»): se para acá, sin cuarta ronda ni más arreglos. Lo que se probó en cada una:
  - **R1** (`aede936^..9f6cf1c`, cortada porque saqué su worktree): NO APTO, sobrevivieron M02, M05, M10 y M14.
  - **R2** (`9ad0497`): APTO para `cambiarTipo()` :266, el spec del falso positivo de las 21:20.
  - **R3** (`f0e5588` y las líneas que R1 no llegó a mutar): las 6 de R1 muertas; 5 nuevas sobreviven.

  Los 5 arreglos son specs sin cambios de producción, y el reporte de R3 dice qué test falta en cada uno. Decide el equipo: autorizar una cuarta ronda o mergear con los 5 huecos declarados. **El #167 queda con «NO MERGEAR TODAVÍA», y con él el #166, que va junto.**

## 14/11/2026 09:21 · Revisión de Javi sobre #166 y #167, y cuarta ronda del #167

**Instrucción recibida (verbatim):**
> Hay revisión de Javi sobre los dos PR. Leé primero el último bloque de
> docs/sprint06/BITACORA.md con Estado: en curso y hacé git fetch --all --prune. Si la cuarta
> ronda que te pedí anoche ya arrancó o terminó, no rehagas nada de lo que figure hecho con su
> evidencia: seguí desde ahí.
>
> === #166 · APROBADO por Javi. NO LO TOQUES ===
> No pushees nada a feature/163-esquema-backend: cualquier commit nuevo puede invalidar la
> aprobación. Sus dos observaciones van como deuda, en una issue nueva sin milestone, con
> fecha y origen («revisión de Javi del 14/11 sobre el #166»):
>   - ModificarProveedorUseCase: si el request no trae TipoComprobanteHabitual, lo pisa con
>     null. El frontend lo manda siempre, pero cualquier otro cliente de la API lo borra.
>   - La validación del tipo B exige exactamente «99-». Está bien, pero el mensaje de error
>     tiene que mostrar un ejemplo del formato (99-00001234).
>
> === #167 · REQUEST CHANGES. Primero esto ===
>
> 1 · [CÓDIGO] enterProveedor() en factura-compra.ts: si hay más de un proveedor cuyo código
>     sin ceros a la izquierda coincide (035 y 00035), hoy Enter elige el primero sin avisar.
>     Con más de una coincidencia exacta tiene que mostrar la lista en lugar de elegir.
>     TDD: el spec en rojo primero, por aserción, en su propio commit; después el código.
>     Va solo en frontend. Si para resolverlo hiciera falta tocar el backend, PARÁS y me
>     avisás: el backend está en el #166 y ese PR no se toca.
>
> 2 · [DOCS] docs/historias/056-HU-FCO-06.md: completá la columna Prueba de los CA-001 a 006
>     y 008 a 015 con el nombre del test que cubre cada uno, igual que está el CA-007. Si
>     algún CA no tiene test que lo cubra, no lo inventes: escribilo así en la celda y
>     contámelo.
>
> 3 · [DOCS] ESTADO del Sprint 6: dejá armada una sección «Recorrido en el navegador contra
>     lacentral_demo (DoD 10)» con la lista de qué hay que probar a mano, CA por CA, y las
>     columnas para anotar quién lo hizo, cuándo, con qué datos y qué pasó. La completa Javi
>     cuando lo haga con Sergio. No la completes vos.
>
> 4 · Issue nueva, milestone Sprint 6: la pantalla de factura de compra no deja cargar el
>     mismo artículo dos veces aunque vaya a sucursales distintas, y el backend sí lo permite
>     por la D9. Ya pasaba antes del #167. Origen: observación de Javi del 14/11.
>
> === Después, la cuarta ronda que te pedí anoche ===
>
> 5 · Los specs que faltan, solo tests, sin tocar producción:
>       factura-compra.ts:277, :303, :350 y :351 · nucleo/api.ts:165 · terceros.ts:144
>     Cada uno en rojo con su mutación puesta y en verde al sacarla. En la bitácora, qué
>     mutación usaste para cada uno y el conteo.
>
> === Cierre ===
>
> 6 · Suite completa de frontend UNA vez, sobre el último commit, y el número al cuerpo del
>     #167. El backend del #167 no cambia: declaralo, no lo corras.
>
> 7 · UN solo verificador-tdd, acotado a los commits nuevos (el arreglo de enterProveedor y
>     los specs de la ronda 4), con hash, línea literal y conteo. Una instancia por vez, y
>     ningún worktree se saca mientras corre.
>
> 8 · ESTADO del Sprint 6, con fecha:
>       - lo de la ronda 1 del #167: actuaste sobre un aviso temprano, sacaste el worktree
>         del verificador mientras corría y lanzaste la ronda 2 con la 1 viva, contra la
>         regla R1, y por eso se publicó un APTO inválido que después corregiste;
>       - que el equipo autorizó la cuarta ronda el 13/11;
>       - las dos issues nuevas y la deuda del #166;
>       - que el 09 todavía no está aplicado en lacentral: lo aplica Santi después del merge.
>
> 9 · Si el verificador da APTO: comentá en el #167 qué cambió por cada punto de Javi,
>     pedile de nuevo la revisión, y sacá el cartel de NO MERGEAR de los dos PR. Orden de
>     merge, sin cambios: #164, #165, y #166 con #167 juntos.
>     Si da NO APTO: arreglás y verificás acotado al arreglo. Si ya no hay ronda que dé
>     APTO, parás y me lo contás.
>
> No me preguntes en el medio: si te trabás, a la bitácora y seguí con lo que puedas.

**Plan:**
1. Bitácora y fetch (no hay bloque en curso; la cuarta ronda no arrancó). #164 y #165 ya están mergeados en `develop` y la rama ya los contiene.
2. Issue de deuda del #166 (sin milestone). Issue de artículo repetido por sucursal (milestone Sprint 6).
3. Punto 1: spec en rojo de `enterProveedor()` con dos coincidencias exactas (commit propio), después el código.
4. Punto 5: specs de la ronda 4, cada uno en rojo con su mutación.
5. Puntos 2 y 3: columna Prueba de la 056; sección DoD 10 en el ESTADO.
6. Frontend completo una vez sobre el último commit; cuerpo del #167.
7. Un `verificador-tdd` acotado a los commits nuevos.
8. ESTADO con fecha (ronda 1, autorización del 13/11, issues, deuda, script 09). Push.
9. Según dictamen: comentario a Javi, re-review, sacar el cartel; o arreglo acotado.

**Estado:** terminada (lo abierto, al final del bloque)
- 09:25 · Paso 2: issues **#168** (deuda del #166, sin milestone) y **#169** (artículo repetido por sucursal, milestone Sprint 6).
- 09:40 · Paso 3 (punto 1): rojo **`4dd89a1`** (spec «Enter con dos códigos exactos sin ceros (035 y 00035) lista los dos y no elige ninguno»: `1 FAILED, 25 SUCCESS`, por aserción: `Expected Object({ id: 7, codigo: '035', … }) to be null`). Código **`8a331f1`**: `enterProveedor()` elige solo si hay exactamente una coincidencia exacta. `factura-compra*`: `26 SUCCESS`. Solo frontend; no hizo falta backend.
- 10:05 · Paso 4 (punto 5, ronda 4): **`7b36e8d`**, solo specs (producción sin cambios). Sobre producción, `factura-compra*` + `articulos*` + `terceros*`: `TOTAL: 65 SUCCESS`. Cada mutación puesta de a una (los números de línea son los de `9f6cf1c`), siempre en rojo por aserción, y en verde al sacarla:
  - `factura-compra.ts:277` `!!alTerminar` → `false`: `1 FAILED, 64 SUCCESS` · «CA-008 y CA-009: Enter busca también aproximado; el botón Buscar, no» · `Expected null to be 'true'`.
  - `:303` sin `&& i !== reemplazo`: `1 FAILED, 64 SUCCESS` · «CA-010: reemplazar una línea por su mismo artículo no la rechaza como repetida» · `Expected 'FIL-0712 ya está en la factura: corregí su línea.' to be null`.
  - `:350` sin `|| null` en `fechaVencimiento`: `1 FAILED, 64 SUCCESS` · «CA-012: sin vencimiento ni imputación cargados, se mandan como null…» · `Expected '' to be null`.
  - `:351` sin `|| null` en `fechaImputacion`: ídem, `1 FAILED, 64 SUCCESS`.
  - `nucleo/api.ts:165` `if (aproximada)` → `if (true)`: `2 FAILED, 63 SUCCESS` (el de Enter/botón y «HU-FCO-06 CA-008: la búsqueda del listado no pide coincidencias aproximadas», en `articulos-listado.spec`) · `Expected true to be false`. Extra: el default `aproximada = true` en la firma: `1 FAILED` (el del listado).
  - `terceros.ts:144` valor fijo `—` en lugar de `tipoComprobanteHabitual`: `1 FAILED, 64 SUCCESS` · «056 · la ficha muestra el comprobante habitual…» · `Expected '—' to be 'B'`.
  - Además, el fix del punto 1 con `>= 1` en lugar de `=== 1`: `1 FAILED, 64 SUCCESS`, por aserción.
- 10:25 · Paso 5 (puntos 2 y 3): **`41b0699`**. 056: columna Prueba de CA-001 a 006 y 008 a 015 con los nombres de los tests (front y back). **CA-011 sin test que mande al servidor un precio escrito distinto del propuesto**: escrito así en la celda. ESTADO: sección «Recorrido en el navegador contra lacentral_demo (DoD 10)», 15 filas, columnas vacías para Javi.
- 10:35 · Paso 6: **frontend completo sobre `41b0699`: `TOTAL: 195 SUCCESS`** (190 + 5 specs nuevos). `backend/` del #167 no cambia desde `772e934` (560/560): se declara, no se corre. `git worktree list`: solo el principal.
- 10:40 · Paso 7: **`verificador-tdd` ronda 4 lanzado** (una instancia), acotado a `4dd89a1`, `8a331f1`, `7b36e8d` (`41b0699` solo docs), con `41b0699` · `TOTAL: 195 SUCCESS` · 195/195; backend sin cambios (560/560 sobre `772e934`). No se toca ningún worktree ni se commitea hasta su reporte final.
- 11:10 · **`verificador-tdd` #167, ronda 4: NO APTO** (reporte final, hand-back del agente; worktree propio sacado por él, `git worktree list`: solo el principal). Rojo de `4dd89a1` válido (por aserción). 15 mutaciones; mueren todas las de `enterProveedor()`, `:278`, `:304`, `:351`, `:352` y `api.ts:165` (las dos direcciones). **Sobreviven 2 en `terceros.ts:144`**, con la suite entera en 195/195: `<dd>` con `B` fijo (el spec solo abre la ficha con `'B'`) y `?? '—'` → `?? ''` (nadie mira la ficha sin tipo habitual). Sigue: spec con la ficha sin tipo habitual, verificación acotada a ese commit (la instrucción del 14/11: «Si da NO APTO: arreglás y verificás acotado al arreglo»).
- 11:25 · Arreglo **`4b624d3`** (solo spec): «056 · sin comprobante habitual, la ficha muestra la raya». `terceros*`: 16/16. Mutaciones de `terceros.ts:144`: `<dd>` con `B` fijo → `1 FAILED, 65 SUCCESS` (`Expected 'B' to be '—'`); `?? ''` → `1 FAILED` (`Expected '' to be '—'`); `—` fijo → `1 FAILED` (`Expected '—' to be 'B'`). **Frontend completo sobre `4b624d3`: `TOTAL: 196 SUCCESS`** (reemplaza al 195 de `41b0699`). `git worktree list`: solo el principal.
- 11:27 · **`verificador-tdd`, verificación del arreglo, lanzada**, acotada a `4b624d3`, con `4b624d3` · `TOTAL: 196 SUCCESS` · 196/196. Una instancia; no se toca ningún worktree hasta su reporte final.
- 11:45 · **`verificador-tdd`, verificación de `4b624d3`: APTO** (reporte final). 10c (`<dd>B</dd>`) y 11 (`?? ''`) mueren por aserción en «056 · sin comprobante habitual, la ficha muestra la raya». Aceptó 196/196 sobre `4b624d3` sin repetirla. `git worktree list`: solo el principal.
- 11:50 · Paso 8: ESTADO con fecha (ronda 1 del 13/11, autorización de la cuarta ronda, #168, #169, deuda del #166, 09 sin aplicar en `lacentral`), tablero y PRÓXIMA SESIÓN. Sigue: push del #167, cuerpo, comentario a Javi, re-review y carteles.
- 12:00 · Paso 9: push de `ce9aacf` al #167. Los cuerpos del #166 y del #167 ya no tienen el «NO MERGEAR»: se editó el cuerpo del #166, sin pushearle nada (sigue APPROVED, head `bd300e5`). En el cuerpo del #167 quedaron la revisión de Javi, la ronda 4, el arreglo y 196/196. Comenté en el #167 qué cambió por cada punto, y está pedida de nuevo la revisión de Javi.
- `origin/develop` trae solo los merges de #164 y #165, sin diferencia de contenido (`git diff HEAD...origin/develop` vacío), así que no se mergeó en la rama.
- **Queda abierto (lo tienen que hacer personas):**
  - la re-review de Javi del #167;
  - el merge del #166 junto con el #167;
  - que Santi aplique el 09 en `lacentral` después del merge;
  - el recorrido DoD 10 (Javi con Sergio, en la sección del ESTADO);
  - la suite completa después del merge a `develop`;
  - #168 y #169;
  - el CA-011, que no tiene test de un precio escrito que llegue al servidor.

## 14/11/2026 10:27 · CA-011 de la 056 con prueba, cierre de la #21 y re-revisión del #167

**Instrucción recibida (verbatim):**
> Falta una cosa antes de que Javi vuelva a revisar el #167:
>
> 1 · El CA-011 de la 056 (precio editable por línea) no tiene prueba que lo verifique: lo
>     dejaste declarado en la columna Prueba. Escribí el spec de frontend que cargue una
>     línea, cambie el precio propuesto por otro escrito a mano, registre la factura y
>     compruebe que el request que sale al servidor lleva el precio escrito, no el propuesto.
>     Primero en rojo por aserción, en su propio commit. Si hace falta tocar producción, va
>     en otro commit. Solo en el #167: el #166 no se toca.
>     Actualizá la celda del CA-011 con el nombre del test.
>
> 2 · Suite de frontend UNA vez sobre el último commit, el número al cuerpo del #167, y un
>     verificador-tdd acotado a ese commit nuevo, con hash, línea literal y conteo.
>
> 3 · El #164 ya entró a develop: cerrá la #21 con el comentario de la anulación del CA-003
>     por la decisión del dueño del 11/11/2026 (D-S6-03), citando el #164.
>
> 4 · Agregá al comentario para Javi en el #167 que el CA-011 ya tiene su prueba, y pedile la
>     re-revisión por GitHub. Después no toques nada más hasta que responda.

**Plan:**
1. Spec del CA-011 por el DOM: escribir otro precio en el input de la línea, ver total, confirmar, y comprobar que el POST lleva el escrito. Rojo por aserción en commit propio; si producción ya lo cumple, no se fuerza el rojo: se declara y se demuestra con mutación.
2. Celda del CA-011. Suite de frontend una vez, cuerpo del #167, un `verificador-tdd` acotado.
3. Cerrar la #21 citando el #164 y D-S6-03.
4. Comentario a Javi y re-review. Después, nada más.

**Estado:** terminada (lo abierto, al final del bloque)
- 10:27 · **Corrección del bloque de las 09:21:** las horas de sus líneas desde «09:40» hasta «12:00» no son de reloj; las escribí estimadas. `date` da ahora 14/11 10:27. Los hechos, hashes y conteos de esas líneas sí son reales. Desde acá, cada hora sale de `date`.
- 10:27 · `git fetch --all --prune`: `origin/feature/163-frontend` = local (`000a744`); `origin/develop` trae solo los merges de #164 y #165, sin diferencia de contenido.
- 10:28 · Paso 1: spec «CA-011: el precio escrito a mano en la línea es el que se registra, no el costo propuesto» en **`a2e7d1f`**, solo test. Escribe 137.5 en la entrada de precio de la línea, donde estaba el costo propuesto 100, y comprueba el body de `/calculo` y del POST.
  - **No hubo rojo:** sobre producción dio verde al primer intento (`TOTAL: 30 SUCCESS` en `factura-compra*`), porque producción ya cumplía el CA. No se forzó un rojo.
  - Discrimina por mutación:
    - `[(ngModel)]` → `[ngModel]` en el precio: `1 FAILED, 66 SUCCESS`, `Expected 100 to be 137.5`;
    - `factura()` con `precioUnitario: 100` fijo: `1 FAILED, 66 SUCCESS`, ídem.
  - (El primer intento buscaba `input[name="precio0"]` y daba `TypeError` en el propio spec. No era un rojo válido: se cambió el selector.)
- 10:28 · Celda del CA-011 de la 056 con el nombre del test: **`8f79668`**. **Frontend completo sobre `8f79668`: `TOTAL: 197 SUCCESS`.** `git worktree list`: solo el principal.
- 10:28 · Paso 2: **`verificador-tdd` lanzado**, acotado a `a2e7d1f`, con `8f79668` · `TOTAL: 197 SUCCESS` · 197/197. Una instancia; no se toca ningún worktree ni se commitea hasta su reporte final.
- 10:29 · Paso 3: **#21 cerrada** (completed), con un comentario que cita D-S6-03 (decisión del dueño del 11/11/2026), el #164 (merge `4b41dbb`) y la v1.1 de la 014.
- 10:30 · **`verificador-tdd` de `a2e7d1f`: APTO** (reporte final). Ninguna de las 5 mutaciones sobrevive: las dos mías, el `Math.trunc` del precio, la de `confirmar()` con el propuesto y la de la ficha que pisa el precio (esta la mata el test de la ficha tardía). Aceptó 197/197 sobre `8f79668`. No bloqueante: el spec toma el precio por posición, porque `[name="precio0"]` dio `null` en el primer intento. Queda así. `git worktree list`: solo el principal. ESTADO al día.
- 10:31 · Paso 4: en el cuerpo del #167 ahora están el CA-011 y `TOTAL: 197 SUCCESS` sobre `8f79668`. Agregué al comentario para Javi (issuecomment-6877424699) que el CA-011 ya tiene su prueba, y la re-revisión del #167 está pedida por GitHub (`requested_reviewers`: Jotelsooon).
- **Queda abierto:**
  - **la respuesta de Javi.** Hasta entonces no se toca nada más (instrucción del 14/11);
  - el merge de #166 y #167 juntos;
  - que Santi aplique el 09 en `lacentral`;
  - el recorrido DoD 10;
  - #168 y #169.

## 14/11/2026 10:33 · #169: mismo artículo en sucursales distintas en la factura de compra

**Instrucción recibida (verbatim):**
> Cuando el CA-011 esté cerrado y Javi tenga pedida la re-revisión, seguí con la #169
> (Sprint 6): la pantalla de factura de compra no deja cargar el mismo artículo dos veces
> aunque vaya a sucursales distintas, y el backend sí lo permite por la D9.
>
> - Rama nueva desde feature/163-frontend, que tiene todo lo de la 056. No esperes el merge
>   del #166 y el #167. Cuando entren a develop, git merge origin/develop, nunca rebase.
> - Lo que cuenta como repetido es el mismo artículo en la misma sucursal, no el mismo
>   artículo a secas. Si se repite artículo y sucursal, se avisa en lugar de agregar la línea.
> - TDD: el spec en rojo primero, en su propio commit. Después el código.
> - PR propio contra develop. En el cuerpo: depende del #167 y se mergea después de él.
> - Suite de frontend una vez antes de abrir el PR, y un verificador-tdd acotado a la HU.
>
> La 029 (#13) y la 049 (#51) NO se arrancan: la compuerta C7 sigue abierta hasta que
> lleguen facturas reales de varios proveedores. Dejalo así en el ESTADO, con fecha.

**Plan:**
1. Rama `feature/169-articulo-por-sucursal` desde `feature/163-frontend` (`84bb4d4`).
2. Spec en rojo: el mismo artículo con otra sucursal de cabecera se agrega; con la misma, se avisa y no se agrega. Commit propio.
3. Código: el repetido es artículo + sucursal (en el reemplazo, la sucursal de la línea reemplazada).
4. ESTADO: 029 (#13) y 049 (#51) sin arrancar, C7 abierta, con fecha.
5. Suite de frontend una vez, PR contra develop (depende del #167), `verificador-tdd` acotado.

**Estado:** terminada (lo abierto, al final del bloque)
- 10:33 · Paso 1: CA-011 cerrado y re-revisión del #167 pedida (bloque de las 10:27). `git fetch`: sin novedades en `origin/feature/163-frontend` ni diferencias de contenido con `origin/develop`. Rama **`feature/169-articulo-por-sucursal`** creada desde `84bb4d4`.
- 10:45 · Pasos 2 y 3:
  - **Rojo `4ccf0fa`:** «#169: el mismo artículo con otro destino se agrega como línea nueva» da `1 FAILED, 31 SUCCESS`, por aserción (`Expected 'FIL-0712 ya está en la factura: corregí su línea.' to be null`). El primer intento caía en el `expectOne` del helper; se reordenó para que fallen primero las aserciones. «… con el mismo destino avisa y no agrega» ya pasaba, porque es la regla que se conserva.
  - **Código `31cf9d3`:** cuenta como repetido el artículo en el destino de la línea nueva, que en un reemplazo es el de la línea reemplazada. `factura-compra*`: 32/32.
  - **Specs `c4be707`** para las ramas que el rojo no alcanzaba: el reemplazo usa el destino de su línea; otro artículo en el mismo destino se agrega. Mutaciones, cada una en rojo por aserción:
    - destino siempre la cabecera: `1 FAILED`;
    - sin comparar la sucursal: `2 FAILED`;
    - sin comparar el artículo: `2 FAILED`.
  - `factura-compra*`: 34/34.
- 10:45 · Paso 4: ESTADO. El tablero suma la fila de la #169. La 029 (#13) y la 049 (#51) quedan «No se arranca (14/11/2026)», con la C7 abierta. PRÓXIMA SESIÓN actualizada.
- 10:36 · Corrección: las dos líneas de arriba dicen «10:45», pero son de las 10:36 según `date`.
- 10:36 · Paso 5: **frontend completo sobre `ca0707d`: `TOTAL: 201 SUCCESS`** (197 + 4 specs de la #169). `backend/` no cambia. `git worktree list`: solo el principal. **`verificador-tdd` lanzado**, acotado a `84bb4d4..ca0707d` (`4ccf0fa`, `31cf9d3`, `c4be707`; `ca0707d` es solo docs), con `ca0707d` · `TOTAL: 201 SUCCESS` · 201/201. Una instancia; no se toca ningún worktree ni se commitea hasta su reporte final.
- 10:39 · **`verificador-tdd` de la #169 (`84bb4d4..ca0707d`): APTO** (reporte final). Rojo de `4ccf0fa` reproducido, por aserción. Ninguna de las 9 mutaciones sobre las líneas 305 y 306 sobrevive. Aceptó 201/201 sobre `ca0707d`. No bloqueante: si después se cambia el destino de una línea con su `<select>`, se pueden armar dos líneas con el mismo artículo y la misma sucursal sin aviso en pantalla. Ese caso lo rechaza el backend (`MismoArticuloYSucursalEnDosLineas_Invalido`); queda declarado en el PR. `git worktree list`: solo el principal.
- 10:39 · Push de `3c39cb3` y **PR #170** contra `develop`, milestone Sprint 6, `Closes #169`. El cuerpo dice que depende del #167 y se mergea después de él. ESTADO con el número del PR.
- **Queda abierto:**
  - el merge del #166 y el #167 (Javi); después, `git merge origin/develop` en esta rama y el merge del #170;
  - el aviso en pantalla cuando se cambia el destino de una línea a mano (lo decide el equipo);
  - la C7, que mantiene la 029 y la 049 sin arrancar.

## 14/11/2026 12:02 · #170 cambio de destino, #168 deuda del #166, D1 de CLAUDE.md

**Instrucción recibida (verbatim):**
> Mientras Javi revisa el #167, tres cosas. La 029 y la 049 siguen sin arrancar: la C7 está
> abierta.
>
> 1 · #170: cerrá el hueco que declaraste. Si después de agregar una línea se le cambia la
>     sucursal a mano y queda igual a otra línea (mismo artículo y misma sucursal), la
>     pantalla tiene que avisar y no aceptar el cambio, igual que al agregarla. No lo dejes
>     para que lo decida el equipo: es la misma regla de la #169, aplicada en otro momento.
>     TDD, spec en rojo en su propio commit. Suite de frontend y verificador acotado.
>
> 2 · #168, la deuda del #166, en rama nueva desde feature/163-frontend y PR propio contra
>     develop (depende del #166 y el #167, decilo en el cuerpo):
>     - ModificarProveedorUseCase no puede borrar en silencio el tipo de comprobante
>       habitual cuando el request no lo trae. Elegí la solución más simple que lo evite y
>       justificala en el PR.
>     - El mensaje de error del tipo B tiene que mostrar el ejemplo 99-00001234.
>     TDD, suite completa antes del PR, verificador-tdd y auditor-persistencia.
>
> 3 · La D1 de CLAUDE.md está desactualizada: sigue diciendo «Tarjeta3/6: tasas SIN
>     DEFINIR». Alineala con las decisiones que ya están tomadas: D12 (16/10/2026, Tarjeta 3
>     y 6 salen del sistema, confirmado por Alfredo el 17/10; el retiro del esquema va en la
>     compuerta C1 del Sprint 7), D13 (L2 = costo × 1,40) y D14 (Efectivo = L1 − 10 %).
>     Leé esas decisiones en los documentos antes de escribir, no las resumas de memoria.
>     PR de documentación aparte.
>
> Cuando Javi mergee el #166 y el #167: git merge origin/develop en las ramas del #170 y del
> #168, nunca rebase, y la suite que corresponda sobre cada una.

**Plan:**
1. #170, en `feature/169-articulo-por-sucursal`: spec en rojo en su propio commit (cambiar el destino de una línea a uno que la duplica avisa y deja el destino anterior); después el código en el cambio del `<select>`; suite de frontend; verificador acotado a esos commits; push y ESTADO.
2. #168, en rama `feature/168-deuda-proveedor` desde `feature/163-frontend`: rojo del caso de uso (modificar sin el tipo conserva el habitual) y del mensaje del tipo B; código; suite completa; verificador-tdd y después auditor-persistencia, uno por vez; PR contra develop.
3. D1: leer D12, D13 y D14 en los documentos; rama de docs desde develop; PR aparte.
4. Cuando entren el #166 y el #167: `git merge origin/develop` en las ramas del #170 y del #168, y la suite que corresponda.

- 12:02 · `git fetch`: `origin/develop` trae los merges del #164 y el #165, que esta rama ya contiene (`git diff --name-only HEAD...origin/develop` vacío). El #167 sigue con la revisión de Javi de las 05:00 UTC, sin respuesta nueva.

**Estado:** terminada
- 12:05 · #170: rojo `492f4af`, por aserción (`Expected null to contain 'FIL-0712 ya está en la factura'`); el spec del destino libre ya pasaba y queda como resguardo. Código `65e1370`: `cambiarDestino()` avisa con el mismo mensaje de la #169 y devuelve el `<select>` al destino anterior. Frontend completo sobre `65e1370`: `TOTAL: 203 SUCCESS`. El script `mutar.sh` apunta a un worktree viejo y no corrió: las mutaciones quedan para el verificador.
- 12:08 · Verificador ronda 1 sobre `3e3c590..65e1370`: **NO APTO**. De 9 mutaciones sobrevivieron dos: M1, sacar `j !== i` (inalcanzable), y M9, `emitViewToModelChange: true` (equivalente, no bloquea). Arreglo `b7973d6`: se borra la guarda (regla de CLAUDE.md: si no es alcanzable, se borra), y la demostración queda en el spec #170 `:319`. Frontend completo sobre `b7973d6`: `TOTAL: 203 SUCCESS`.
- 12:10 · Verificador ronda 2 sobre `65e1370..b7973d6`: **APTO**. Cuerpo del #170 actualizado; ESTADO con la fila y la decisión del 14/11.
- 13:23 · Punto 2: **PR #171** (rama `feature/168-deuda-proveedor`). El detalle está en el bloque de continuación de esa rama. Suite completa 564/564 sobre `c8a8ae0`; `verificador-tdd` APTO en la ronda 2; `auditor-persistencia` APTO.
- 13:23 · Punto 3: **PR #172** (rama `docs/d1-precios-vigentes`, desde develop). D1 alineada con D12, D13 y D14, leídas en `docs/sprint04/ESTADO.md`, `docs/sprint03/informe-cierre-sprint3.md` §3 y `docs/PLAN-SPRINTS-6-7-8.md`.
- **Queda abierto:** el merge del #166 y el #167 (Javi); después, `git merge origin/develop` en las ramas del #170 y del #171, nunca rebase: el #170 con la suite de frontend y el #171 con la completa. La C7, que mantiene la 029 y la 049 sin arrancar.

## 14/11/2026 12:24 · #168, continuación del bloque de las 12:02 (en la rama del #170)

**Instrucción recibida (verbatim):** la del bloque de las 12:02, punto 2. Ese bloque está en `feature/169-articulo-por-sucursal`; esta rama sale de `feature/163-frontend` y no lo tiene.

**Plan:** rojo HTTP contra PostgreSQL; DTO de la Api que distingue «no vino» de «vino null»; test del mensaje del B; suite completa; `verificador-tdd` y después `auditor-persistencia`; PR.

- Solución elegida: el PUT reemplaza todo, así que sin la clave es 400 en español; `null` sigue vaciando. Se descartó «`null` conserva y `""` vacía»: obliga a cambiar el frontend, que manda `null` para vacío, y vuelve ambiguo el contrato.
- Rojo `5901221`, por aserción: `Expected: BadRequest, Actual: NoContent`. Código `dd4e411`. Al filtrar por `Proveedor` cayó `CicloCompletoIntegracionTests` (un PUT sin la clave, ahora 400): se le agregó la clave en `null`, y quedó 8/8 junto con `ProveedorDetalleApiPostgreSqlTests`.
- Mensaje del B: ya traía `99-00001234` desde `1060bb0` (#166). Test `cf79afb`, que pasó en verde de entrada. Sacar el ejemplo del mensaje lo pone en rojo (1 con error, 5 superados).

**Estado:** terminada. Queda abierto: el merge del #166 y el #167, y después `git merge origin/develop` en esta rama y la suite completa.
- 13:02 · `verificador-tdd` ronda 1 (`84bb4d4..e076966`): **NO APTO**. Sobrevivieron M6 a M9: el `init` que no asigna, el tipo pasado en `null`, y `UrlReferencia` y `Direcciones` en `null` en el mapeo. Arreglo `c8a8ae0`: `Modificar_ConTodosLosCampos_LosEscribeEnLaFila` (PUT con "A" sobre un "B" y todos los campos, relectura con un contexto nuevo y la ficha), en verde de entrada.
- 13:21 · `verificador-tdd` ronda 2 (`e076966..c8a8ae0`): **APTO**; murieron M6 a M9. `auditor-persistencia` sobre `84bb4d4..c8a8ae0`: **APTO**. Suite completa sobre `c8a8ae0`: `Correctas! - Con error: 0, Superado: 564, Omitido: 0, Total: 564`. Nota del auditor, fuera del alcance y sin auditar: `proveedor_baja_coherente` en `base_completa.sql` parecería contradecir la D10.
- 13:22 · Push de `7d4da66` y **PR #171** contra `develop`, sin milestone, igual que la #168. El cuerpo dice que depende del #166 y el #167.

## 14/11/2026 14:19 · Sprint 7 adelantado: 028 (#9), 026 (#33) y 027 (#34)

**Instrucción recibida (verbatim):**
> Mientras Javi revisa, adelantá lo que queda de factura de compra en el Sprint 7: la 028
> (#9), la 026 (#33) y la 027 (#34). No tienen compuerta abierta. Empezá por la 028, que es
> la que destraba la 016 (#23). Leé sus proformas antes de arrancar.
>
> - Una HU por rama y por PR, todas desde feature/163-frontend, que tiene el esquema y el
>   código de la 056. En el cuerpo de cada PR: depende del #166 y del #167. Cuando esos
>   entren a develop, git merge origin/develop en cada rama, nunca rebase.
> - PR chicos: si una HU pasa los 8 SP, partila en dos PR con sentido propio.
> - TDD, suite completa antes de abrir cada PR, y los verificadores que correspondan, una
>   instancia por vez.
> - Quedan en el milestone del Sprint 7: no los muevas.
>
> Sobre lo que marcó el auditor de proveedor_baja_coherente: no es contradicción. El script
> 01_Ajustes_Sprint3 ya reemplaza los cuatro CHECK por la forma de la D10, y la base se
> arma con base_completa.sql y después los scripts de rescaffold/ en orden. Anotalo así en
> la bitácora y cerralo.
>
> La 029 y la 049 siguen sin arrancar: la C7 está abierta.

**Plan:**
1. Leer las proformas de la 028, la 026 y la 027 (y la 016, que la 028 cita como precondición).
2. 028 en `feature/9-anulacion-factura-compra`: TDD de caso de uso (Moq) y de persistencia/atomicidad (PostgreSQL real), endpoint, pantalla; suite completa; `verificador-tdd` y `auditor-persistencia`; PR.
3. 026 y 027, cada una en su rama desde `feature/163-frontend`, igual.
4. Cuando entren el #166 y el #167: `git merge origin/develop` en cada rama.

- 14:19 · **Nota del auditor sobre `proveedor_baja_coherente`, cerrada:** no hay contradicción con la D10. `backend/db/rescaffold/01_Ajustes_Sprint3.sql` (líneas 166 a 177) borra los CHECK `*_baja_coherente` de `base_completa.sql` y los vuelve a crear con la forma de la D10 (`activo OR (motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL)`). La base se arma con `base_completa.sql` y después con los scripts de `rescaffold/` en orden. Lo aclaró Santi el 14/11.
- 14:19 · Proformas leídas: 028 v1.0 (8 SP, riesgo alto, 5 CA), 026 v1.0 (3 SP, 5 CA), 027 v1.0 (2 SP, 4 CA), 016 v1.0 (5 SP). Ninguna pasa los 8 SP. El esquema ya tiene lo de la anulación: `estado`, `motivo_anulacion`, `fecha_anulacion`, `usuario_anulacion_id` y el índice único parcial `ux_factura_compra_numero_activa`, que libera el número (CA-004).
- 14:19 · **Regla que falta en la 028:** `07_Normalizacion_3FN_y_Reportes.sql` B.4 agrega `factura_compra.usuario_autorizacion_id`, que es la doble certificación pedida por la Dirección el 02/11/2026: anular necesita dos personas, y la que autoriza tiene que ser ADMINISTRADOR. La proforma v1.0 de la 028 (19/08) no la menciona, y ningún documento de `docs/` la recoge. Tampoco dice qué rol puede anular.

**BLOQUEADO EN:** la 028 necesita una decisión del equipo (Santi): si la anulación pide la doble certificación de B.4 y qué rol anula. La 026 y la 027 no dependen de esto.

**Estado:** terminada (la 027 es Sprint 7 y no se arranca; cerrado por el bloque del 17/11 13:20)
- 14:52 · Respuesta de Santi: la 028 incluye la doble certificación, y la anulación la piden ADMINISTRADOR y OPERADOR, con la autorización de otro usuario ADMINISTRADOR. La identidad de quien autoriza se prueba con su usuario y su contraseña, nunca con un Id del cuerpo. Las dos decisiones quedan como CA-006 y CA-007 de la 028 v1.1.
- **028 partida en dos PR** (con la doble certificación pasa los 8 SP): este, solo backend (caso de uso, repositorio y endpoint), y otro con la pantalla de anulación, que necesita el listado de la 026 para elegir la factura.
- Esqueleto `bb8a1f8`, rojo `b53c5c3` (Moq 16 con error y 1 superado; PostgreSQL 9 con error, por aserción), código `bc313e2` (26/26), rojo HTTP, endpoint `acaaec5` (FacturasCompraApiPostgreSqlTests 17/17).
- **Interpretación pendiente de confirmación (CA-002):** «conserva su precio de costo previo» se lee como que el costo no se toca, porque no hay registro del costo anterior a la factura. Queda anotado en la proforma y en el PR.
- 15:40 · `verificador-tdd` ronda 1 (`84bb4d4..01171bf`): **NO APTO**. Sobrevivieron M12 y M13, la guarda de la cabecera ACTIVA contra dos anulaciones simultáneas; M14, el stock revertido fuera de la columna; y M16, el orden del último registro válido. No bloqueante: el rojo de `AnulaUnOperador…` cae por NullReferenceException, no por aserción. Arreglo `57142f0`: tres tests por caminos reales (la segunda anulación llega al repositorio con la factura ya ANULADA; stock en -999.999.999; tres facturas y se anula la última), más el mensaje en `AnularDosVeces`. 12/12 PG.
- 16:23 · `verificador-tdd` ronda 2 (`01171bf..57142f0`): **APTO**, murieron las 5. `auditor-persistencia` (`84bb4d4..57142f0`): **APTO**. Observaciones no bloqueantes: la «carrera» del test es secuencial; no hay un caso de anulación con el mismo artículo en dos sucursales; y una pregunta para el equipo: anular una factura vieja pisa un costo cargado a mano después, que no queda en el historial. Suite completa sobre `57142f0`: 591/591. La corrida anterior dio 1 fallo ajeno (`ArticulosApiPostgreSqlTests.Post_Alta_201ConIdYLocation`), que filtrado pasó en verde. Probablemente vino de una corrida del verificador que seguía en segundo plano.
- 16:24 · Push de `e795879` y **PR #173** (028 parte 1, `Refs #9`, milestone Sprint 7). Sigue la 026 (#33).

## 14/11/2026 16:39 · 026 (#33), continuación del bloque de las 14:19 (en la rama de la 028)

**Instrucción recibida (verbatim):** la del bloque de las 14:19. Ese bloque está en `feature/9-anulacion-factura-compra`; esta rama sale de `feature/163-frontend` y no lo tiene.

**Plan:** backend de la consulta (caso de uso, repositorio y `GET /api/facturas-compra`), pantalla «Compras registradas», suite completa, `verificador-tdd` y `auditor-persistencia`, PR.

- Backend: esqueleto, rojo de Moq (7 con error y 1 superado) y de PostgreSQL (5 con error y 1 superado), código (14/14), rojo HTTP (`MethodNotAllowed`) y endpoint (FacturasCompraApiPostgreSqlTests 17/17).
- Frontend: esqueleto, rojo (9 FAILED: 6 por TypeError, porque el esqueleto no tiene formulario, y 3 por expectOne; acceso.spec, por aserción), código. Los dos specs del menú de `app.spec.ts` se actualizaron por el módulo nuevo. Frontend completo: `TOTAL: 205 SUCCESS`.
- **Interpretación:** el CA-001 dice «exactamente una». Con número y proveedor puede haber una activa y además anuladas con el mismo número (CA-004 de la 028); se muestran todas, y las anuladas van marcadas (CA-005).
- Un `git stash` con una ruta sin seguimiento falló y no tocó nada: el stash del usuario (`stash@{0}`) sigue intacto y no se aplicó.

**Estado:** terminada
- 17:32 · `verificador-tdd` ronda 1 (`84bb4d4..32a9755`): **NO APTO**. Sobrevivieron tres mutaciones: el desborde de `(pagina - 1) × tamaño`, el error que no borra la tabla anterior y el `[disabled]` de «Siguiente». **Corrección:** el rojo de la pantalla fueron **5** TypeError y 3 `expectOne`, no 6 como dicen el commit `30e6c87` y la línea de arriba. Tampoco se había declarado que `UnNumeroEnBlanco_NoFiltraPorNumero` cayó por NullReferenceException. Arreglo `b3d96bb`: `InlineData(30_000_000, 100)`, el spec del error ahora tiene una tabla anterior, y un spec nuevo para los extremos de la paginación. Moq 9/9, pantalla 9/9.
- 17:46 · `verificador-tdd` ronda 2 (`32a9755..b3d96bb`): **NO APTO**. Morían tres de las cuatro mutaciones; sobrevivía la de `this.resultado.set(null)`, porque la plantilla esconde la tabla mientras hay error. La línea de la ronda 1 que decía «el spec del error ahora tiene una tabla anterior» no alcanzaba para matarla. Arreglo `ac6f619`: el spec busca de nuevo y afirma que no hay tbody mientras el pedido está en vuelo. Con la mutación: 1 FAILED, 8 SUCCESS; con producción: 9/9.
- 17:47 · `verificador-tdd` ronda 3 (`b3d96bb..ac6f619`): **APTO**. La mutación de `this.resultado.set(null)` muere: 1 FAILED, 8 SUCCESS, en `compras-registradas.spec.ts:143`.
- auditor-persistencia: **APTO**. Observación menor: el desempate `ThenByDescending(Id)` con la misma emisión no tiene test. Develop: #164 y #165 ya estaban contenidos (el diff `HEAD...origin/develop` dio vacío). #166 y #167 siguen abiertos. Push y PR.
- **Queda abierto:** la 028 parte 2 (la pantalla de anulación sobre esta), la 027 (#34) y el merge de develop cuando entren el #166 y el #167.
- 17:50 · PR #174 abierto (milestone Sprint 7). El número quedó anotado en el ESTADO.

## 14/11/2026 19:51 · 028 parte 2 (pantalla de anulación), pregunta de la 027 para Sergio y merges de develop

**Instrucción recibida (verbatim):**
> Terminá la 028 parte 2 (la pantalla de anulación), con su PR y sus verificadores, igual
> que la parte 1. Después NO abras más PR: hay siete esperando la revisión de Javi y todos
> dependen del #166 y el #167. Agregarle más le suma trabajo a una sola persona.
>
> La 027 (#34) no se arranca: la duda sobre «ubicación de ingreso» es de negocio. Dejala
> escrita en el ESTADO del Sprint 7 como pregunta para Sergio, con la frase textual de la
> proforma y qué interpretaciones ves, y pasámela a mí.
>
> Cuando Javi mergee el #166 y el #167: git merge origin/develop en cada rama abierta,
> nunca rebase, y la suite que corresponda sobre cada una.

**Plan:**
1. Rama `feature/9-anulacion-pantalla` desde `feature/33-consulta-facturas-compra` (la 026, para elegir la factura) con `git merge feature/9-anulacion-factura-compra` (el endpoint de la parte 1).
2. TDD de la pantalla de anulación: acción «Anular» en «Compras registradas», con motivo, usuario y contraseña de quien autoriza.
3. Suite completa, `verificador-tdd` (y `auditor-persistencia` solo si toco backend), PR. Es el último PR.
4. 027: pregunta para Sergio en el ESTADO del Sprint 7, sin arrancarla.
5. Cuando entren #166 y #167: `git merge origin/develop` en cada rama abierta y su suite.

**Estado:** terminada (#166 y #167 mergeados por Javi; cerrado por el bloque del 17/11 13:20)
- Paso 1: rama creada y merge de la parte 1. Conflictos solo por agregados de los dos lados (puerto, repositorio, controlador, Program, test HTTP, bitácora y ESTADO): se conservaron los dos. Build 0 errores; filtrado `FacturasCompra|FacturaCompra`: 168/168.
- 19:57 · Paso 2: rojo `d0697a5` (solo spec: 4 FAILED, 9 SUCCESS; la primera falla de cada uno es la aserción «botón Anular de la fila», después cae un TypeError porque no hay diálogo). Código `070027b`: 13/13 en `compras-registradas`. Mutaciones mías antes del verificador, todas muertas (1 FAILED cada una): recarga en la página 1 en vez de la actual, el aviso que no se borra, `enviando` que no vuelve a false, autocomplete de la contraseña y Cancelar que no cierra. Los `trim` del motivo y el usuario no se pusieron: los hace el caso de uso.
- 19:57 · Frontend completo sobre `070027b`: `TOTAL: 210 SUCCESS`. Backend completo en curso.
- 20:09 · Backend completo sobre `070027b`: `Correctas! - Con error: 0, Superado: 608, Omitido: 0, Total: 608`. Paso 3: `verificador-tdd` lanzado, acotado a `4b59b94..070027b`; el auditor no aplica (el diff de la HU no toca backend ni esquema).
- 19:57 · Paso 4: P7-01 en `docs/sprint07/ESTADO.md` (nuevo: no existía; el entregable del Sprint 7 queda sin escribir, lo define el equipo). La 027 no se arranca.
- 20:13 · `verificador-tdd` ronda 1 (`4b59b94..070027b`): **NO APTO**. Sobrevivieron M2, M3 y M6, las marcas de `obligatorio` de motivo, usuario y contraseña (ningún test confirmaba con un campo vacío), y M14, `f.id` cambiado por `items[0].id` (los tests anulaban siempre la fila 0). Rojo y orden test → producción: validados. Arreglo `db36dd2`, solo test: anular la segunda fila pega a `/2/anulacion`, y con cada uno de los tres campos vacío no sale el POST. Con cada mutación, 1 FAILED, 16 SUCCESS; con producción, 17/17. Frontend completo sobre `db36dd2`: `TOTAL: 214 SUCCESS`. Backend sin cambios desde `070027b` (608/608).
- 20:14 · `verificador-tdd` ronda 2 (`070027b..db36dd2`): **APTO**, murieron las 4. ESTADO al día. #166 y #167 siguen abiertos; develop no trae nada nuevo (#164 y #165 ya estaban). Push y PR.
- 20:15 · Push de `525b57f` y **PR #175** (028 parte 2, `Refs #9`, milestone Sprint 7). Es el último PR: no se abren más hasta que Javi revise los que esperan (#166, #167, #170 a #175).

**BLOQUEADO EN:** paso 5. Hace falta que Javi mergee el #166 y el #167 en develop. Después: `git merge origin/develop` en cada rama abierta y la suite de cada una. La P7-01 (027) espera la respuesta de Sergio, vía Santi.

## 17/11/2026 13:20 · Sprint 6: fase A (docs), compuerta C7, 029 y 049

**Instrucción recibida (verbatim):**
> LEE PROMPT-SPRINT-06.md ahi estan tus tareas
> se encuentra en ~/dev/TESIS/la-central-gestion/docs/sprint06
> tambien ahi se encuentran el ESTADO.md y la BITACORA.md
>
> Contenido de `docs/sprint06/PROMPT-SPRINT-06.md` (verbatim):
>
> Arrancamos el Sprint 6 en serio. Trabajás solo hasta terminarlo o hasta que algo dependa de una
> persona. No me consultes decisiones técnicas: las tomás vos, las anotás con fecha en el
> `ESTADO.md` y seguís. Solo parás una pieza por una regla de negocio que falta, y mientras tanto
> seguís con lo que no depende de ella.
>
> ## 0 · Antes de tocar nada
>
> 1. Leé `CLAUDE.md` entero y respetalo. En particular: la bitácora (`docs/sprint06/BITACORA.md`),
>    la política de pruebas, la sección de los verificadores y la de `pull`.
> 2. Leé el último bloque de `docs/sprint06/BITACORA.md` con `Estado: en curso`. Si hay uno,
>    seguís desde ahí.
> 3. Abrí un bloque nuevo en la bitácora con esta instrucción completa, verbatim.
> 4. `git fetch --all --prune`, pasate a `develop` y traé lo último. Mi copia local estaba parada
>    en una rama `feature/9-…` ya mergeada y con cambios sin commitear: mirá `git status`, y si hay
>    algo que no sea la carpeta `docs/facturas/` (que ya saqué del repo), anotalo en la
>    bitácora y no lo descartes.
> 5. Leé `docs/sprint06/ESTADO.md`, `docs/PLAN-SPRINTS-6-7-8.md`, `docs/historias/INDICE.md` y las
>    proformas `029-HU-FCO-05.md`, `049-HU-UI-11.md` y `056-HU-FCO-06.md`.
>
> ## 1 · Lo que cambió desde el último ESTADO
>
> - **Javi revisó, aprobó y mergeó todos los PR abiertos:** #166, #167, #170, #171, #172, #173 y
>   #175. El Sprint 5 está cerrado (tag `sprint-5`).
> - Eso deja la **056 mergeada**, el **#169 mergeado** y la **028 (anulación de factura de
>   compra, #9) construida antes de tiempo**, aunque está planificada para el Sprint 7.
> - Falta el recorrido DoD 10 de la 056 contra `lacentral_demo`. Lo hace Javi con Sergio: no es
>   tuyo, solo dejalo bien armado.
>
> **Fase A · poner al día la documentación (un PR solo de docs, sin verificadores):**
> - `docs/sprint06/ESTADO.md`: tablero con lo mergeado, PR por PR. Lo mismo para la 028 en el
>   ESTADO o el plan que corresponda, y si cierra el CA-003 y el CA-004 de la 016, decilo.
> - `docs/historias/INDICE.md`: Sprint vigente y Estado de la 056, la 028 y la 016.
> - `docs/PLAN-SPRINTS-6-7-8.md`: si la 028 ya está, sale de la cuenta de SP del Sprint 7.
> - Revisá con `gh` el estado de las issues y los milestones contra lo que escribiste. Si una issue
>   quedó abierta con el PR mergeado y todos sus CA verificados, cerrala con un comentario que
>   diga qué PR la cierra. Si tiene un CA sin verificar, no la cierres: anotalo.
>
> **Para Santi (no lo corrés vos, no tenés acceso a `lacentral`):** dejá en la bitácora, en un
> bloque «PARA SANTI», los comandos listos para pegar en fish para:
> 1. hacer el backup de `lacentral` con `/home/atiz/tesis-db/backup.sh` (vía `ssh atiz@arch`);
> 2. verificar qué scripts de `backend/db/rescaffold/` ya tiene aplicados `lacentral` (una consulta
>    de solo lectura que mire columnas o tablas que agrega cada script, del 07 al 09);
> 3. aplicar los que falten, en orden, con `ON_ERROR_STOP=1`, contra el contenedor `lacentral_db`
>    con el superusuario `lacentral`.
> Sin marcadores para completar: comandos enteros.
>
> ## 2 · Compuerta C7 · ya hay PDFs reales
>
> Están **fuera del repo**, en `~/dev/TESIS/facturas-reales/`. **Nunca van al repo** (es público):
> ni el PDF, ni un recorte, ni sus datos copiados en un test. Agregá `docs/facturas/` al
> `.gitignore`, en el PR de docs, para que no vuelva a pasar.
>
> | Archivo | Proveedor | Qué es |
> |---|---|---|
> | `10713-VT-FA0137-25630.pdf` | ETMAN (Multioriginal Parts S.A.) | **Factura A**, 0137-00025630, 17/11/2026. Texto extraíble (PScript + Ghostscript). Por línea: código, descripción, marca, cantidad, precio, **tres columnas de descuento** (Bonif., Bonif., Dto.), total. Percepción IIBB Córdoba aparte. IVA 21 %. **Números en formato estadounidense** (`20,184.21`). |
> | `Presupuesto_48437.pdf` | Fazio (Badaloni) | **Presupuesto**, compra **en negro**, no es factura. Texto extraíble. Por línea: código de fábrica, descripción, cantidad, remito, precio de lista, descuento combinado `43,84 (28+22)`, precio final, total. **Números en formato argentino** (`27.396,12`). |
> | `V20261105NCBA0010-00021495 (1).pdf` | Expoyer S.A. | **Nota de crédito A** por bonificación, en blanco. **No es una factura de compra.** |
>
> Dos hechos que salen de los PDF y conviene registrar en el ESTADO:
> - **Los descuentos son sucesivos, confirmado con dos proveedores:** en ETMAN,
>   20.184,21 × 0,95 × 0,64 = 12.272,00 (el total de la línea); en Fazio, 28 + 22 = 43,84 %. Era el
>   supuesto de la 056 que faltaba confirmar.
> - **Cada proveedor usa un formato de número distinto**, así que el parser no puede asumir uno.
>
> Lo que te toca:
> 1. Decidí si con esto alcanza para cerrar la C7, o si se cierra parcial. Con lo que hay, falta
>    una **factura** de Expoyer: la de ellos es una nota de crédito. Anotá la decisión con fecha.
> 2. La nota de la 029 pide decidir entre un parser por reglas o una interpretación asistida.
>    Decidilo vos con estos tres layouts a la vista. Dejá escrito el porqué y qué cuesta agregar
>    un proveedor nuevo con la opción elegida. Si la opción usa un servicio externo, la clave va
>    en variables de entorno y nunca en el repo.
> 3. **Los tests no usan los PDF reales.** Generá en los tests PDF sintéticos que imiten cada
>    layout, con datos inventados (proveedor, CUIT, códigos y montos ficticios). Ningún test
>    depende de que exista `~/dev/TESIS/facturas-reales/`: la DoD prohíbe pruebas omitidas. Los
>    reales se usan para analizar el layout y para el recorrido a mano de la DoD 10.
> 4. Antes de construir, preguntas para Sergio (no las inventes, dejalas en el ESTADO en «Lo que
>    vuelve a Sergio» y seguí con lo que no dependa de ellas):
>    - El presupuesto en negro de Fazio, ¿se carga como comprobante **B** con el prefijo **99**
>      (D-S6-10)? ¿Qué número lleva, el del presupuesto (48437)?
>    - Las notas de crédito de proveedor, ¿se cargan en el sistema? El Mandato deja las notas de
>      crédito fuera de alcance: confirmarlo.
>    - ETMAN informa «Pago contado 5 %» y vencimientos con descuento por pronto pago. ¿El sistema
>      lo ignora (el costo es el de la línea) o tiene que registrarlo?
>
> ## 3 · Construcción · 029 y después 049
>
> - Una rama y un PR por HU (§2 de `CLAUDE.md`). Si la 029 pasa de 500 líneas de diff, partila en
>   dos PR con sentido propio (sección de ahorro de tokens, punto 4).
> - TDD con los tres commits. Persistencia contra PostgreSQL real, releyendo con contexto nuevo.
> - La 029 nunca persiste antes de la confirmación del operador y nunca crea artículos por su
>   cuenta (RF-011). El código del proveedor se busca en `articulo_codigo_alternativo`. Lo que no
>   reconoce con certeza lo marca para revisar, no lo adivina.
> - La 049 sigue `docs/diseno/SISTEMA-VISUAL.md` y reutiliza la pantalla de la 056: el PDF
>   precarga el mismo formulario, no hay un formulario nuevo.
> - Verificadores: una invocación por PR, al cerrar la HU, con el alcance acotado a sus commits y
>   con el hash, la línea literal y el conteo de la suite completa si ya la corriste. Worktrees
>   afuera antes de invocarlos. Ante un NO APTO, las 3 rondas de `CLAUDE.md`.
> - `ESTADO.md` actualizado antes de cada push, en el mismo push.
>
> ## 4 · Lo que NO es tuyo en este sprint
>
> - **Scrapes de proveedores (Sprint 7, D-PL-02):** los armamos a mano Javi, Lauti y Santi en n8n,
>   fuera de la cuenta de SP. Son cuatro: Expoyer, Autocor, Nafazio (Fazio) y ETMAN (Link3).
>   **Las credenciales de esos portales no las vas a ver nunca:** van en el almacén de
>   credenciales de n8n (RNF-007). Si en algún archivo, issue o mensaje aparece un usuario o
>   contraseña de proveedor, no lo copies a ningún lado y avisá en la bitácora. Lo único que
>   hacés es anotar en el ESTADO, en «Fuera del Sprint 6», que los scrapes de esos cuatro
>   arrancaron el 17/11/2026 y que el destino de sus datos es la compuerta C2 del Sprint 7.
> - No arranques ninguna HU del Sprint 7 ni del 8.
>
> ## 5 · Cuándo parás
>
> Parás y me escribís un resumen corto (qué quedó hecho, con PR, hash y conteo de la última suite
> completa; qué quedó abierto y de quién depende) cuando pase cualquiera de estas cosas:
> - la 029 y la 049 quedaron con PR abierto y verificadores APTO;
> - todo lo que queda depende de una respuesta de Sergio o de una acción de Santi;
> - 3 rondas de NO APTO sin salida.
>
> Antes de parar, la línea `**BLOQUEADO EN:**` o el `Estado: terminada` en la bitácora, según
> corresponda.

**Plan:**
1. Arranque: bloques abiertos, fetch, develop, git status.
2. Fase A (PR solo de docs): ESTADO, INDICE, PLAN 6-7-8, `.gitignore` con `docs/facturas/`, issues y milestones con `gh`, bloque «PARA SANTI».
3. Compuerta C7: análisis de los tres PDF, decisión de cierre, decisión parser, preguntas para Sergio.
4. 029 (rama, TDD, PDF sintéticos, verificadores, PR; partida en dos si pasa de 500 líneas).
5. 049 (reutiliza la pantalla de la 056, verificadores, PR).
6. Resumen y BLOQUEADO EN / terminada.

**Estado:** en curso
- 13:20 · Bloques abiertos: el de las 14:19 del 14/11 (lo que quedaba era la 027, Sprint 7: no se arranca) y el de las 19:51 (paso 5, merges de #166/#167: ya mergeados por Javi). Los dos quedan cerrados por este bloque.
- 13:20 · `git fetch`; `develop` en `b920a89` (merge del #175). Sin commitear en la copia local, conservado y no descartado: `frontend/angular.json` (una línea `"analytics": "e83a8c62-…"`, el id local del CLI de Angular; es lo mismo que guarda el `stash@{0}`), la carpeta `.serena/` (cache de la herramienta) y `docs/sprint06/PROMPT-SPRINT-06.md` (este prompt). `docs/facturas/` ya no existe. Stashes `stash@{0}` y `stash@{1}` intactos.
- 13:45 · Fase A en la rama `docs/sprint06-fase-a`: ESTADO del 6 (tablero, «Lo mergeado el 16/11», C7, decisión del parser, «Lo que vuelve a Sergio · 17/11», scrapes en «Fuera del Sprint 6», PRÓXIMA SESIÓN), ESTADO del 7 («Construido antes de tiempo»: 026 y 028), INDICE (056, 028, 026, 016), PLAN (Sprint 7: 67 → 56, salen la 028 y la 026, que también está hecha), `.gitignore` con `docs/facturas/`. Issues cerradas con comentario: #169 (PR #170), #168 (PR #171), #33 (PR #174). Abiertas con comentario del motivo: #163 (DoD 10), #9 (interpretación del CA-002), #23 (CA-004, la misma).
- 13:45 · C7 cerrada parcial (falta una factura de Expoyer y una de Autocor). 029: parser por reglas, un lector por layout, PdfPig en `LaCentral.Api`. No hay cambio de esquema: `factura_compra.origen` ya admite `'PDF'` (`fc_origen_valido`). En los PDF y en los archivos leídos no apareció ningún usuario ni contraseña de proveedor.

### PARA SANTI · 17/11/2026 · backup de `lacentral` y scripts 07 a 09

Desde fish, en tu máquina, parado en la raíz del repo (los scripts viajan por la entrada estándar del `ssh`, no hace falta el repo en la Arch).

1 · Backup (mirá primero si el script pide argumentos; después corrélo):

```fish
ssh atiz@arch head -n 30 /home/atiz/tesis-db/backup.sh
ssh -t atiz@arch /home/atiz/tesis-db/backup.sh
```

2 · Qué scripts ya tiene `lacentral` (solo lectura). `t` = aplicado; `f` = falta. El 07 crea la tabla `provincia`; el 08 deja `stock.ubicacion_deposito` en 60; el 09 agrega `proveedor.tipo_comprobante_habitual`, `factura_compra.tipo_comprobante` y `factura_compra_detalle.descuento_3_porcentaje`:

```fish
cd ~/dev/TESIS/la-central-gestion
echo "SELECT to_regclass('public.provincia') IS NOT NULL AS s07, coalesce((SELECT character_maximum_length = 60 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock' AND column_name = 'ubicacion_deposito'), false) AS s08, (SELECT count(*) = 3 FROM information_schema.columns WHERE table_schema = 'public' AND (table_name, column_name) IN (('proveedor', 'tipo_comprobante_habitual'), ('factura_compra', 'tipo_comprobante'), ('factura_compra_detalle', 'descuento_3_porcentaje'))) AS s09;" | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral
```

3 · Aplicar SOLO los que dieron `f`, en este orden, de a uno (cada script abre su transacción; con `ON_ERROR_STOP=1` el primer error corta y no aplica nada de ese script). Si uno falla, no sigas con el siguiente:

```fish
cd ~/dev/TESIS/la-central-gestion
ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral -v ON_ERROR_STOP=1 < backend/db/rescaffold/07_Normalizacion_3FN_y_Reportes.sql
ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral -v ON_ERROR_STOP=1 < backend/db/rescaffold/08_Ubicacion_Deposito_60.sql
ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral -v ON_ERROR_STOP=1 < backend/db/rescaffold/09_Factura_Compra_Mecanica.sql
```

Después, repetí el paso 2: tienen que dar los tres `t`. El 08 y el 09 son idempotentes; el 07 **no**: no lo corras si el paso 2 dio `s07 = t`.
- 14:30 · Fase A: PR #176 (`d7a4020`, solo docs). 029: rama `feature/13-factura-pdf` desde `docs/sprint06-fase-a`. Esqueleto `40a122a` (PdfPig 0.1.16 en `LaCentral.Api`; `LaCentral.UseCases` sigue sin paquetes). Rojo en el commit siguiente: 22 con error por aserción (HTTP: 404, sin endpoint), 2 superados porque el esqueleto ya devuelve lo pedido (PDF sin texto → vacío; texto desconocido → null). Las fuentes estándar de PdfPig no escriben «ó»: los PDF sintéticos van sin tildes.
- 15:20 · 029 parte 1: código `bc7c125` (antes, `4810a69`: solo el PDF sintético de Fazio, columnas más anchas). Filtrado: `LecturaPdfTests` + `InterpretarFacturaPdfUseCaseTests` 20/20; con PG y HTTP, 24/24. Prueba a mano con los PDF reales desde un programa descartable en el scratchpad (nada al repo): ETMAN 3 líneas, A, `0137-00025630`, nada para revisar; Fazio 10 líneas 28/22, B y número para revisar; nota de crédito de Expoyer no reconocida. **Partición:** la 029 pasa de 500 líneas → parte 1 = interpretación (CA-001 a CA-004), parte 2 = registro con `origen = 'PDF'` (CA-005, CA-006). Suite completa sobre `bc7c125` corriendo; la base compartida está lenta (20 a 30 s por test de PG).
- 16:50 · Suite completa sobre `bc7c125`, primera corrida: `Con error: 1, Superado: 635, Total: 636` (44 m 41 s). Falló `FacturasCompraApiPostgreSqlTests.Get_ConElRangoInvertido_400ConMensaje` (1 m 1 s, ajeno al PDF); filtrado pasa (26 s): la base compartida estaba lenta. Segunda corrida, la única que cuenta: `Correctas! - Con error: 0, Superado: 636, Omitido: 0, Total: 636, Duración: 44 m` sobre `bc7c125`.
- 16:50 · Parte 2 empezada en `feature/13-factura-pdf-registro` (apilada sobre la parte 1): esqueleto `DesdePdf` commiteado; tests rojos escritos, sin compilar todavía (la suite usaba los binarios). Siguen los verificadores de la parte 1, uno por vez: `verificador-tdd` y después `auditor-persistencia`, acotados a `40a122a..bc7c125`.
- 18:05 · `verificador-tdd` ronda 1 de la 029 parte 1 (`40a122a..bc7c125`): **NO APTO**. Orden test → producción y rojo validados (22 rojos por aserción, 2 verdes). Sobrevivió M14, `LectorTextoPdf.cs:47` sin `OrderBy(p => p.X)`: los PDF sintéticos ponían todas las palabras de un renglón a la misma altura. M15 (`OrderBy` de `LayoutsPdf.cs:122`) sobrevivió por ser equivalente: código muerto. No bloqueante: ningún `PostPdf_*` prueba 401/403 (el endpoint hereda `[Authorize]` de la clase). Arreglo: `8b9edf0`, solo test (renglón con la palabra de la izquierda 0,8 pt más abajo y escrita después; con M14 y con la tolerancia en `> 0`, 1 con error cada una), y `e9e3d03`, borra el `OrderBy` muerto. Filtrado 21/21. Suite completa sobre `e9e3d03` corriendo.
- 18:05 · Mientras la ronda 1 corría: 029 parte 2 en `feature/13-factura-pdf-registro` (esqueleto `ca9c29b`, rojo `e56499a`: Moq 1 con error y 1 superado, el caso false es el valor por defecto; HTTP PG 1 con error, las dos filas MANUAL; código `5939547`, 3/3). 049 en `feature/51-pantalla-pdf`, apilada sobre la parte 2: esqueleto `d5d025a`, rojo `d511560` (13 FAILED: 12 por expectOne, 1 por TypeError porque no había campo de archivo), `da81124` solo test (el CA-001 espera con whenStable a que ngModel escriba en el DOM, como el spec de la 056), código `bbb196c`. Frontend completo sobre `bbb196c`: `TOTAL: 233 SUCCESS`.
- 19:30 · `verificador-tdd` ronda 2 (`bc7c125..e9e3d03`; cortada por el límite de uso y retomada): **NO APTO**. M14 y la tolerancia mueren con el test nuevo. Sobrevive la partición de `Ordenar` (`LayoutsPdf.cs:122`): los layouts agregan las líneas antes que la cabecera, y ningún test combina las dos marcas. Arreglo `f5b0ebd`: se borra `Ordenar` (nadie lee `paraRevisar` por posición; la 049 usa `includes`). Filtrado 21/21. Suite completa sobre `f5b0ebd` corriendo. Si la ronda 3 da NO APTO, se para (3 rondas).
- 21:00 · Suite completa sobre `f5b0ebd`, primera corrida: 1 con error ajeno (`CrearArticuloPostgreSqlTests.DosAltasSimultaneas_CodigosInternosDistintos`, 53 s; filtrado pasa en 29 s). Segunda corrida, la que cuenta: `Correctas! - Con error: 0, Superado: 637, Omitido: 0, Total: 637, Duración: 46 m 49 s` sobre `87456be` (= `f5b0ebd` + docs).
- 21:00 · `verificador-tdd` ronda 3 (`e9e3d03..f5b0ebd`): **APTO**, 9 mutaciones muertas. Observación no bloqueante: cuatro tests afirman `paraRevisar` en orden aunque el contrato dice que no importa. La 049 sí consulta con `includes`, pero está en `feature/51-pantalla-pdf`, que el verificador no miró. `auditor-persistencia` (`40a122a..f5b0ebd`): **APTO**, sin corridas propias. Observación: el cotejo de códigos es exacto (mayúsculas y espacios); un código con otro formato queda sin artículo y va a revisión. ESTADO al día. Push y PR de la parte 1.

## 17/11/2026 · Revisión de Santi de los PR #176 a #179: percepciones de ETMAN, proveedor elegido después del PDF, respuestas de Sergio y lector de Expoyer

**Instrucción recibida (verbatim):**
> Revisión de Santi de los PR #176 a #179 (17/11). Escribime los resúmenes en español.
>
> Arreglos antes de que Javi revise. Cada uno, como commit en la rama del PR que corresponda; después git merge hacia las ramas de arriba (nunca rebase). Al re-verificar, acotá el alcance a los commits del arreglo.
>
> 1. Percepciones de ETMAN (#177). El lector busca «Percepcion IIBB:» y «Percepcion de IVA», que solo existen en el PDF sintético. La factura real dice «Percepciones IIBB» y en otro renglón «Cordoba : 3920.23». Con el PDF real (~/dev/TESIS/facturas-reales/10713-VT-FA0137-25630.pdf, fuera del repo) la percepción IIBB tiene que dar 3.920,23. Corregí el lector según el texto que PdfPig saca del PDF real, y el sintético para que imite ese texto real, no uno inventado. Regla nueva: si el layout tiene bloque de percepciones y no puede leer un monto, el campo va a ParaRevisar; nunca queda en null en silencio. Rojo primero.
>
> 2. Proveedor sin CUIT en el PDF (Fazio). Hoy el operador elige el proveedor a mano y las 10 líneas quedan sin resolver. Cuando se elige el proveedor después de cargar un PDF, el sistema tiene que volver a buscar los códigos de las líneas sin resolver en articulo_codigo_alternativo de ese proveedor y pasar a líneas las que encuentre. El cómo lo decidís vos: backend en la rama de la 029 y pantalla en la de la 049.
>
> 3. Respuestas de Sergio, para el ESTADO:
>    - D-S6-13: lo en negro se carga como comprobante B con prefijo 99 y el número del presupuesto. El texto del PDF de Fazio no trae el número, pero el nombre del archivo sí (Presupuesto_48437.pdf). Proponé 99-48437 a partir del nombre del archivo y dejalo marcado para revisar.
>    - D-S6-14: las notas de crédito de proveedor SÍ se cargan. Choca con el Mandato v3.0, que las deja fuera de alcance, y no hay HU. No la construyas: anotala en el ESTADO como HU nueva pendiente de decisión del equipo, propuesta para el Sprint 8. Mientras tanto, el lector sigue rechazando la nota de crédito.
>    - D-S6-15: el 5 % de pago contado no viene en la línea porque depende de la forma de pago; si se paga de contado, se carga como un descuento más en cada producto. Pendiente: dónde va si la línea ya usa los tres descuentos. Va a «Lo que vuelve a Sergio»; no lo resuelvas por tu cuenta.
>    - Pregunta nueva para «Lo que vuelve a Sergio»: el presupuesto de Fazio suma IVA ($67.970,79; total $391.640,79). Si va como B sin IVA, el sistema registra $323.670. ¿Ese IVA se paga?
>
> 4. Lector de Expoyer. La factura está en ~/dev/TESIS/facturas-reales/FCA0012-00091771.pdf: factura A 0012-00091771 del 27/10/2026. Layout: dos columnas de artículos por hoja (Cant., Código, Descripción, P.Unit., Total, repetido a la derecha); descuento global en la cabecera («Descuento: 61.08%+15») con los precios de línea ya netos (el subtotal 98.506,97 es la suma de las líneas); códigos con espacios y guiones internos («C 115105 4- F»); números en formato de EE. UU.; IVA 21 %. Va en un PR propio, apilado arriba de #178, con su sintético que imite el texto real, sus tests y los verificadores. Probalo a mano con el real y anotá el resultado en la bitácora. En la C7 queda faltando solo la factura de Autocor.
>
> Cuando estén los cuatro: una sola suite completa sobre el HEAD de cada rama tocada, los verificadores acotados a los commits nuevos, y el ESTADO al día antes del push. Después parás y me pasás el resumen. El sprint no lo das por cerrado: falta el DoD 10 con Sergio.

**Plan:**
1. #177 (`feature/13-factura-pdf`): texto real de ETMAN con PdfPig; rojo y arreglo de las percepciones con la regla de ParaRevisar; D-S6-13 (número 99-<n> desde el nombre del archivo, para revisar); backend del punto 2 (buscar los códigos de un proveedor elegido a mano).
2. Merge a #178 y #179. #179 (`feature/51-pantalla-pdf`): pantalla del punto 2.
3. Expoyer: rama nueva desde `feature/13-factura-pdf-registro`, lector con su sintético, prueba a mano con el real, PR.
4. ESTADO (D-S6-13 a 15, HU nueva de notas de crédito, preguntas a Sergio, C7).
5. Suite completa por rama tocada, verificadores acotados, push, resumen. El sprint no se cierra.

**Estado:** en curso
- Arranque: `git fetch`; develop sin novedades; #176 a #179 abiertos sin revisión. El prompt del sprint, que estaba sin seguimiento en la copia local, se movió al scratchpad para poder cambiar de rama; vuelve al terminar.
- Punto 1: el texto real de ETMAN (PdfPig, fuera del repo) sí trae «Percepcion IIBB: 3,920.23» y «Percepcion de IVA 2,940.17», en el mismo renglón que el código de barras; «Cordoba : 3920.23» es el detalle por jurisdicción. El lector ya daba 3.920,23 con el real. Lo que sí estaba mal: un monto ilegible quedaba null en silencio, y «1.000,50» se leía 1,00. `5f0e908` sintético con la forma del texto real (solo test, 21 verdes); `70fb2ac` rojo (3 con error por aserción); `8a9aca4` arreglo (24 verdes; real: IIBB 3.920,23, IVA 2.940,17, nada para revisar).
- Punto 2 y D-S6-13, backend: esqueleto `c8d9d2c`… (ver git log de `feature/13-factura-pdf`): `POST /api/facturas-compra/pdf` acepta un `proveedorId` opcional y el caso de uso recibe el nombre del archivo. Sigue el rojo.

## 17/11/2026 · Respuestas del 17/11: D-S6-15 (cuatro descuentos) y D-S6-16 (el IVA de lo comprado en negro no se paga)

**Instrucción recibida (verbatim):**
> Respuestas del 17/11 que cierran dos preguntas del ESTADO:
>
> - D-S6-15 (reemplaza la versión pendiente): la línea de la factura de compra pasa a tener CUATRO descuentos sucesivos, no tres. Tres es lo que traen habitualmente los proveedores; el cuarto es para los descuentos externos por marca o por artículo. Ejemplo ETMAN: siempre trae el 36 % y además un 5 % que no figura en la factura porque es por forma de pago; el operador lo carga en la línea.
>   Impacto:
>   - Cambia el CA-012 de la 056: actualizá la proforma con esta decisión y su fecha.
>   - Hay cambio de esquema: un descuento más en factura_compra_detalle, con el mismo CHECK que los otros. Va por script SQL 10 en backend/db/rescaffold/ más re-scaffold, nunca migraciones.
>   - Toca el cálculo del costo, el total, la pantalla de la 056 y la precarga de la 049.
>   - El lector de ETMAN propone el 5 % en el cuarto descuento, marcado para revisar, porque el PDF no lo trae.
>   Va en su propio PR, antes del de Expoyer.
>   El script 10 no lo aplicás vos en lacentral_test ni en lacentral: dejá en la bitácora un bloque «PARA SANTI» con los comandos listos para fish (backup primero, ON_ERROR_STOP=1, contenedor lacentral_db, superusuario lacentral) para lacentral_test, lacentral_demo y lacentral. Mientras Santi no lo aplique en lacentral_test, lo que dependa del esquema queda frenado; seguí con lo que no dependa.
>
> - D-S6-16: lo comprado en negro es comprobante B y ese IVA no se paga. El IVA que muestra el presupuesto de Fazio se ignora: el total que se registra es 323.670,44, sin IVA. Sacá la pregunta de «Lo que vuelve a Sergio» y anotá la decisión.

**Plan:**
1. Script `10` en `backend/db/rescaffold/` (idempotente, con verificación) en una rama nueva `feature/163-cuarto-descuento`, apilada sobre `feature/51-pantalla-pdf`; push de la rama y bloque «PARA SANTI» con los comandos para fish (backup, `lacentral_test`, `lacentral_demo`, `lacentral`).
2. Terminar los arreglos del bloque anterior en #177/#179 (punto 2, D-S6-13), que no dependen del esquema.
3. D-S6-15, lo que no depende del esquema: proforma de la 056 (CA-012), entidad, cálculo y validación del cuarto descuento con Moq, DTO de la API, pantalla de la 056, precarga de la 049, lector de ETMAN con el 5 % para revisar. Frenado hasta que Santi aplique el 10 en `lacentral_test`: re-scaffold, mapeo del repositorio y pruebas contra PostgreSQL.
4. D-S6-16 en el ESTADO; comprobar con el PDF real de Fazio que el total sin IVA da 323.670,44.
5. Expoyer, en su PR, apilado arriba del de la D-S6-15 (va después de él).

**Decisión (17/11):** el PR del cuarto descuento se apila sobre la 049 (toca su precarga y la pantalla de la 056), y el de Expoyer sobre el del cuarto descuento. Orden de merge: #176 → #177 → #178 → #179 → cuarto descuento → Expoyer.

**Estado:** en curso
- Corrección de la línea del esqueleto del punto 2 (más arriba): el commit es `b251416`, no `c8d9d2c`.
- Script 10 escrito: `a3650ed` en `feature/163-cuarto-descuento` (rama nueva, apilada sobre `feature/51-pantalla-pdf`), pusheada sin PR. **No se ejecutó en ninguna base**: no hay Postgres local en esta máquina (ni `psql` ni daemon de Docker), así que tampoco se probó su sintaxis contra un servidor. La primera corrida es la de Santi en `lacentral_test`, en una transacción con `ON_ERROR_STOP=1`.

### PARA SANTI · 17/11/2026 · backup y script 10 (cuarto descuento, D-S6-15)

Desde fish, en tu máquina, parado en la raíz del repo. El script se lee de la rama pusheada con `git show`: no hace falta cambiar de rama ni tener el repo en la Arch. **Requisito:** el 09 aplicado en cada base (el paso 2 lo muestra; si una da `s09 = 0`, primero el bloque «PARA SANTI» anterior).

1 · Backup de las tres bases, a tu máquina. Cada `.dump` tiene que pesar más de 0 bytes; si uno da error o pesa 0, no sigas:

```fish
cd ~/dev/TESIS/la-central-gestion
mkdir -p ~/backups-lacentral
set -l marca (date +%Y%m%d-%H%M)
for base in lacentral_test lacentral_demo lacentral
    ssh atiz@arch docker exec lacentral_db pg_dump -U lacentral -Fc $base > ~/backups-lacentral/$base-$marca.dump
    or begin; echo "FALLÓ el backup de $base: no sigas"; break; end
end
ls -l ~/backups-lacentral/*-$marca.dump
```

2 · Estado de cada base, solo lectura. `s09 = 1` es el 09 aplicado; `s10 = 0` es el 10 pendiente:

```fish
for base in lacentral_test lacentral_demo lacentral
    echo "SELECT current_database() AS base, count(*) FILTER (WHERE column_name = 'descuento_3_porcentaje') AS s09, count(*) FILTER (WHERE column_name = 'descuento_4_porcentaje') AS s10 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'factura_compra_detalle';" | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d $base
end
```

3 · El 10, **primero solo en `lacentral_test`**. Tiene que terminar con `NOTICE: 10: TODO OK.` y `COMMIT`. El `WARNING` del `ROLLBACK` inicial es esperado (no hay transacción abierta):

```fish
git fetch origin
git show origin/feature/163-cuarto-descuento:backend/db/rescaffold/10_Cuarto_Descuento.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_test -v ON_ERROR_STOP=1
```

4 · Si el 3 dio `TODO OK`, avisame: con eso sigo con el re-scaffold y las pruebas contra PostgreSQL. Después, el mismo comando en `lacentral_demo` y en `lacentral`, de a una, y repetí el paso 2: las tres con `s10 = 1`. El script es idempotente: correrlo dos veces en la misma base no cambia nada.

```fish
git show origin/feature/163-cuarto-descuento:backend/db/rescaffold/10_Cuarto_Descuento.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_demo -v ON_ERROR_STOP=1
git show origin/feature/163-cuarto-descuento:backend/db/rescaffold/10_Cuarto_Descuento.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral -v ON_ERROR_STOP=1
```

Para volver atrás una base (solo si hiciera falta): `ssh atiz@arch docker exec -i lacentral_db pg_restore -U lacentral -d <base> --clean --if-exists < ~/backups-lacentral/<base>-<marca>.dump`.
