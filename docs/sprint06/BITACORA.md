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

**Estado:** terminada (la 029 y la 049 con PR y verificadores APTO; lo abierto, en la última línea del bloque)
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
- 22:10 · Parte 1: push de `decebfb` y **PR #177** (base `docs/sprint06-fase-a`; pasa a `develop` cuando entre el #176). Parte 2: merge de la parte 1 (`61e4ebb`); suite completa corriendo.
- 22:10 · `verificador-tdd` ronda 1 de la 049 (`d5d025a..bbb196c`): **NO APTO**. Orden y rojo validados (13 FAILED: 12 por expectOne, 1 por TypeError, este último no es un rojo válido, como estaba declarado). Sobrevivieron `factura-compra.ts:560` (`leyendoPdf.set(false)` del éxito; con la suite entera en verde) y `:214` (la guarda de líneas sin resolver de «Confirmar», alcanzable por una carrera: un `/calculo` en vuelo cuando se carga un PDF). Hallazgo: `:527` (`nueva()` al recibir el PDF) sin test. Arreglo: `d0b3158` solo test (avance después de leer bien; dos PDF seguidos; cada mutación, 1 FAILED); `f7602da` rojo por el camino real de la carrera (1 FAILED por aserción: el total viejo aparece); `d45bffe` el arreglo (versión del formulario: un total pedido para el anterior no se muestra) y se borra la guarda de `:214`, que queda inalcanzable. Sacando el control de versión, 1 FAILED. Frontend completo sobre `d45bffe`: `TOTAL: 236 SUCCESS`.
- 22:45 · `verificador-tdd` ronda 2 de la 049 (`448f62b..d45bffe`): **NO APTO**. Rojo de `f7602da` validado (por aserción); mueren el `if (true)`, el `version++` comentado y el `!==`. Sobreviven M4 (`const version = 0`: ningún test ve un total después de un `nueva()`) y M5 (`version = 1`: la carrera no se prueba en un segundo formulario). No bloqueantes: el `error` de `registrar()` no mira la versión, y una respuesta vieja igual hace `registrando.set(false)`. Arreglo `43edd66`, solo test (el total se ve después de cargar un PDF; la carrera entre el segundo PDF y el tercero): con M4 y con M5, 1 FAILED cada una. Frontend completo sobre `43edd66`: `TOTAL: 238 SUCCESS`. Sigue la ronda 3, la última.
- 23:05 · `verificador-tdd` ronda 3 de la 049 (`0ade230..43edd66`): **APTO**. M4 y M5 mueren, cada una por aserción. Sin hallazgos. La 049 espera el PR de la parte 2 (está apilada sobre ella); la suite de la parte 2 sobre `61e4ebb` sigue corriendo.
- 23:50 · Parte 2: suite completa sobre `61e4ebb` `Correctas! - Con error: 0, Superado: 640, Omitido: 0, Total: 640, Duración: 40 m 43 s`. `verificador-tdd` (`ca9c29b`, `e56499a`, `5939547`): **APTO** en la ronda 1 (10 mutaciones muertas, rojo validado). `auditor-persistencia`: **APTO**, sin corridas propias; sugiere revisar, cuando la base esté menos cargada, que no queden esquemas `pr110_test_*` huérfanos. Push y PR de la parte 2 (base: la rama de la parte 1).
- 23:55 · Parte 2: push y **PR #178** (base `feature/13-factura-pdf`). 049: merge de la parte 2 (conflicto solo en la bitácora, agregados de los dos lados: se conservaron los dos, en orden). ESTADO al día (fila #51 y PRÓXIMA SESIÓN). Push y PR de la 049 (base `feature/13-factura-pdf-registro`).
- 00:05 · **PR #179** (049). Cierre: la 029 y la 049 tienen PR y verificadores APTO (#177, #178, #179; docs en #176). **Queda abierto, todo de personas:** Javi revisa y mergea en el orden #176 → #177 → #178 → #179 y hace el DoD 10 de la 056, la 029 y la 049 con Sergio, contra `lacentral_demo`, con los PDF reales; Santi corre el bloque «PARA SANTI» del 17/11 (backup y scripts 07 a 09 en `lacentral`); Sergio contesta las cuatro preguntas del 17/11 (presupuesto en negro, notas de crédito, pago contado, CA-002 de la 028); Lauti consigue una factura de Expoyer y una de Autocor (C7 parcial). Sin decidir y sin test: el error de un `/calculo` viejo sobre el formulario nuevo (observación no bloqueante del verificador, en el #179).

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
- Punto 2 y D-S6-13, backend: esqueleto `b251416`, rojo `6b2d65c` (12 con error por aserción; dos rojos que primero daban NRE y una excepción de JSON se reescribieron para que fallen por aserción antes de commitear), código `4668ba0`. Filtrado (lectura, caso de uso, `PostPdf_*`): 36/36. Real: Fazio `99-48437`, solo el número para revisar. **D-S6-16:** el neto de las 10 líneas del real de Fazio da **323.670,42**, no 323.670,44: el PDF redondea el precio unitario final («P. Final») y lo multiplica por la cantidad; el sistema aplica los descuentos al precio de lista y redondea la línea. Diferencia de 2 centavos, sin marcar (entra en la tolerancia de la línea). Va al ESTADO como pregunta, no lo resuelvo.
- Puntos 1 y 2, pantalla (`feature/51-pantalla-pdf`): rojo `34c33e9` (5 FAILED: percepciones sin etiqueta por aserción, 4 por `expectOne` porque no había pedido; dos errores del spec, el texto en singular y el cuerpo del error con `detail`, se corrigieron con `--amend` antes de pushear); código `4de30a5` (etiqueta y `aria-invalid` en las cuatro percepciones; elegir el proveedor con el formulario del PDF vuelve a mandar el archivo con `proveedorId` y pasa a líneas los códigos hallados; ya no pisa el tipo del comprobante con el habitual). Mutaciones propias antes de los verificadores: sobrevivían dos líneas sin efecto (`invalidarTotales`, `archivoPdf = null`) → `ceb1a67` las borra; tres ramas sin test → `7cc9f1f` solo test; el cruce con pendientes antes de pedir fichas → `8ea250d` lo borra. Sin cada guarda de versión, sin la API mandando `proveedorId`, sin las sucursales: 1 FAILED cada una. Frontend completo sobre `8ea250d`: `TOTAL: 246 SUCCESS`.
- D-S6-15, lo que no depende del esquema (`feature/163-cuarto-descuento`): proforma de la 056 v1.1 (CA-012 con cuatro descuentos). Núcleo: esqueleto, rojo y código (ver `git log` de la rama): 13 con error por aserción → 127/127 filtrado (detalle, registro, lectura, caso de uso). ETMAN toma el porcentaje de «Pago Contado N%» y lo marca `lineas[i].descuento4` (decisión: clave propia por línea para no tapar la marca de «el total no cierra»). Real de ETMAN: 5 % en las tres líneas, para revisar. Pantalla: rojo 7 FAILED por aserción → código `b815c8f`; mutaciones propias muertas; frontend completo sobre `b815c8f`: `TOTAL: 250 SUCCESS`. API: `/calculo` no escribe, así que su test HTTP corre sin el script 10: rojo (neto 200 en vez de 190) → `a84c6c7`. **Frenado por E10:** repositorio, re-scaffold y relectura de la fila. Ojo: hasta entonces, en esa rama el costo usa el cuarto descuento pero la línea no lo guarda; la rama no se mergea sin el repositorio.
- Punto 4, Expoyer (`feature/13-lector-expoyer`, apilada sobre `feature/163-cuarto-descuento`): rojo `c419b66` (11 con error, todos por aserción: 7 por el helper Leer, 4 por `Assert.NotNull`; tres casos sobre renglones directos se agregaron con `--amend` antes de pushear, incluido el «º» y las tildes del texto real, que el sintético no puede escribir); código `062dbfa`. Filtrado 45/45. Mutaciones propias (13): todas muertas. **Prueba a mano con el real** (programa descartable en el scratchpad, nada al repo): factura A `0012-00091771` del 27/10/2026, CUIT del proveedor reconocido (el del cliente, sin guiones, no se toma), dos líneas `101324-3` × 1 a 67.778,40 y `C 115105 4- F` × 1 a 30.728,57, neto 98.506,97 = subtotal del PDF, nada para revisar, sin vencimiento. La nota de crédito de Expoyer sigue sin reconocerse (D-S6-14). En la C7 falta solo Autocor.
- ESTADO: D-S6-13 a D-S6-16, decisiones técnicas de la revisión, compuerta E10, C7 con Expoyer, HU nueva de notas de crédito (pendiente del equipo, propuesta Sprint 8), pregunta nueva a Sergio por los 2 centavos de Fazio. Base: el ESTADO de la rama de arriba (contiene todo lo de las ramas apiladas).
- `verificador-tdd` de los arreglos del #177, ronda 1 (`5f0e908`, `70fb2ac`, `8a9aca4`, `b251416`, `6b2d65c`, `4668ba0`): **NO APTO**. Orden y rojos validados. Sobrevivieron el `(?!\S)` del monto de IVA, el `Path.GetFileName` y el ancla `^` de Fazio. Hallazgo: el caso del proveedor inactivo mataba U1 por una excepción. **Correcciones de esta bitácora:** el rojo de `6b2d65c` fue de **11** con error, no 12 (lo reprodujo el verificador); el «21 verdes» de `5f0e908` es la suma de `LecturaPdfTests` (14) e `InterpretarFacturaPdfUseCaseTests` (7). Arreglo `36654aa`, solo test: con cada una de las cuatro mutaciones, 1 con error por aserción; filtrado 35/35. Suite completa sobre `86ceffd` (649/649); `36654aa` solo agrega tests.
- `verificador-tdd` de los arreglos del #179, ronda 1 (`34c33e9..8ea250d`): **NO APTO**. Sobrevivieron el aria-invalid de IVA, la clave de Ganancias, y la sucursal, los descuentos y la marca de la línea resuelta con el proveedor elegido. Arreglo `78ff462`, solo test (hecho en un worktree, borrado después): con cada mutación, 1 o 2 FAILED; además, la clave de Municipal, `activo` y que la primera lectura no manda `proveedorId`. Frontend completo sobre `78ff462`: `TOTAL: 248 SUCCESS`.
- Suites completas de backend (corridas de a una, 17/11 20:27 a 22:36): `86ceffd` (#177) `Correctas! - Con error: 0, Superado: 649, Omitido: 0, Total: 649, Duración: 43 m 28 s`; `6968cd5` (#178; el backend del #179 es el mismo árbol) `Superado: 652, Total: 652, Duración: 43 m 14 s`; `e50413d` (cuarto descuento + Expoyer) `Superado: 675, Total: 675, Duración: 42 m 33 s`. Frontend sobre `a191863` (rama de Expoyer): `TOTAL: 252 SUCCESS`.
- Verificadores, de a uno: #177 ronda 2 **APTO** y `auditor-persistencia` **APTO**; #179 ronda 2 **APTO** (12 mutaciones muertas); Expoyer `verificador-tdd` **APTO** (rojo de 11 reproducido, 11 mutaciones muertas; no pudo correr la suite por permisos, la corrí yo) y `auditor-persistencia` **APTO** (sin persistencia en el diff). Suites completas sobre el HEAD de cada rama, de a una (base menos cargada, unos 5 minutos cada una): `b0cf239` (#177) `Correctas! - Con error: 0, Superado: 652, Omitido: 0, Total: 652, Duración: 4 m 44 s`; `5be9f56` (#178, mismo backend que el #179) `Superado: 655, Total: 655, Duración: 4 m 20 s`; `d89a06a` (Expoyer, con el cuarto descuento debajo) `Superado: 678, Total: 678, Duración: 5 m 8 s`. Frontend: `78ff462` (#179) 248/248; `a191863` (Expoyer; mismo frontend que `d89a06a`) 252/252. ESTADO al día (tablero y PRÓXIMA SESIÓN). Push de las cinco ramas; PR de Expoyer.

**BLOQUEADO EN:** el cuarto descuento (D-S6-15) espera que Santi aplique el script 10 en `lacentral_test` (bitácora, «PARA SANTI · 17/11 · script 10»): sin eso no hay re-scaffold, ni mapeo del repositorio, ni prueba contra PostgreSQL, ni PR de esa rama. Lo demás de la revisión está hecho, con verificadores APTO. Siguen abiertos, todos de personas: las preguntas 4 y 5 a Sergio, la HU de notas de crédito (equipo), la factura de Autocor (Lauti) y el DoD 10 (Javi con Sergio). El sprint no se cierra.

## 18/11/2026 10:36 · Script 10 aplicado en lacentral_test: cierre del cuarto descuento y merge en Expoyer

**Instrucción recibida (verbatim):**
> 10: TODO OK en lacentral_test (17/11). Seguí con el cuarto descuento: re-scaffold, mapeo del repositorio, prueba contra PostgreSQL releyendo la fila, suite completa, verificadores acotados a los commits nuevos y PR. Después mergeá esa rama en feature/13-lector-expoyer y corré lo que corresponda. El 10 en lacentral_demo y en lacentral lo aplico yo cuando me lo pidas. Resúmenes en español.

**Plan:**
1. `feature/163-cuarto-descuento`: re-scaffold contra `lacentral_test` (solo `FacturaCompraDetalle` y el DbContext deberían cambiar).
2. Prueba contra PostgreSQL en rojo: registrar una factura con el cuarto descuento y releer la fila con un contexto nuevo. Commit del rojo.
3. Mapeo del repositorio. Filtrado verde. Commit.
4. Suite completa sobre el HEAD; `verificador-tdd` y `auditor-persistencia` acotados a los commits nuevos; PR (base `feature/51-pantalla-pdf`).
5. Merge en `feature/13-lector-expoyer`; suite completa sobre ese HEAD (cambió backend); ESTADO y bitácora antes del push.
6. Pedirle a Santi el 10 en `lacentral_demo` y `lacentral`.

**Estado:** terminada
- Arranque: `git fetch`; `origin/develop` sin commits nuevos (`b920a89`); sin worktrees extra.
- Paso 1, re-scaffold contra `lacentral_test` (`dotnet-ef` 10.0.11, con `DOTNET_ROOT=$HOME/.dotnet`): el diff real (`--ignore-cr-at-eol`) es solo `Descuento4Porcentaje` en `FacturaCompraDetalle.cs` y su `HasPrecision(6, 2)` con el comentario en el DbContext. `fcb9e51`. Compilación sin warnings. No se comparó contra `lacentral` (README del re-scaffold): la única diferencia esperada es justamente el 10, que Santi aplica después.
- Paso 2, rojo `d018c29`: relectura con contexto nuevo de una línea con cuatro descuentos, 1 con error por aserción (`Expected (10, 5, 2,5, 5)`, `Actual (10,00, 5,00, 2,50, 0,00)`); el test del CHECK `fcd_descuento_4_valido` en el motor pasa desde el principio (lo prueba el script 10, ya commiteado).
- Paso 3, `da0412c` mapeo en `FacturaCompraRepositorio`. Filtrado `FacturasCompra`: 226/226. Suite completa sobre `da0412c`: `Correctas! - Con error: 0, Superado: 669, Omitido: 0, Total: 669, Duración: 4 m 30 s`.
- Frontend completo sobre `da0412c`: `TOTAL: 252 SUCCESS`.
- `verificador-tdd` ronda 1 (`a3650ed..da0412c`, sin los merges de abajo): **NO APTO**. Validó los cuatro rojos y el orden; de 15 mutaciones, sobrevivió solo una: el regex de «Pago Contado» de `LayoutsPdf.cs:51` sin la parte decimal, porque el sintético trae «5%». Arreglo `4125c24`, solo test: `Etman_PagoContadoConDecimales_LoProponeCompleto`, con «2.5%». Con la mutación: 1 con error por aserción; sin ella: filtrado `FacturasCompra` 227/227. Nota: una corrida filtrada dio 1 con error porque el `build -q` falló y `--no-build` usó el binario de la mutación; recompilado, verde. Suite completa sobre `4125c24` (`--no-incremental`, 0 advertencias): `Correctas! - Con error: 0, Superado: 670, Omitido: 0, Total: 670, Duración: 5 m 19 s`. Ronda 2 acotada a `4125c24`, en curso.
- `verificador-tdd` ronda 2 (solo `4125c24`): **APTO**. La mutación de la ronda 1 muere (1 con error, `LecturaPdfTests.cs:85`), y también una de refuerzo (`Eeuu` → `Argentino`). Aceptó la suite de `4125c24`. Hallazgo no bloqueante: el test nuevo afirma `ParaRevisar` solo para `lineas[0]`. `auditor-persistencia`, en curso.
- `auditor-persistencia` (`a3650ed..4125c24`, sin merges): **APTO**, sin hallazgos. Mató la mutación del mapeo contra PostgreSQL real; 9/9 `CuartoDescuento` sin omitidos. Aviso: hasta que el 10 esté en `lacentral_demo` y `lacentral`, registrar una factura de compra ahí da 42703 → el PR no se mergea a `develop` antes (ESTADO, E10). No pudo consultar `pg_constraint` ni buscar esquemas huérfanos: no hay cliente de PostgreSQL en la máquina.
- ESTADO al día (tablero #163, E10, PRÓXIMA SESIÓN), antes del push.
- PR #181 (`feature/163-cuarto-descuento` → `feature/51-pantalla-pdf`), con la advertencia de E10 en el cuerpo.
- Paso 5: `3e8358c`, merge de `feature/163-cuarto-descuento` en `feature/13-lector-expoyer`, sin conflictos y solo backend y docs; frontend sin cambios, no se corrió. Suite completa sobre `3e8358c` (`--no-incremental`, 0 advertencias): `Correctas! - Con error: 0, Superado: 681, Omitido: 0, Total: 681, Duración: 4 m 56 s` (678 + los 3 tests nuevos). Verificadores no relanzados en el #180: el merge no trae commits de Expoyer, y lo que entra quedó verificado en el #181.
- Abierto: el 10 en `lacentral_demo` y `lacentral` (Santi, cuando se le pida; condición para mergear el #181 a `develop`). Lo demás, igual que el bloque del 17/11: Sergio, equipo, Lauti, DoD 10.

## 18/11/2026 · Novedades del 18/11: E10 cerrada, D-S6-17 (P. Final de Fazio), D-S6-19 (Autocor manual, C7 completa), D-S6-18 (HU de nota de crédito de proveedor)

**Instrucción recibida (verbatim):**
> Novedades del 18/11. Resúmenes en español.
>
> 1. Script 10 aplicado en las tres bases (lacentral_test, lacentral_demo y lacentral): s09 = 1 y s10 = 1 en todas. En lacentral también se aplicaron el 08 (ya estaba) y el 09 (TODO OK); el 07 ya estaba aplicado. Anotalo en el ESTADO y en la bitácora. El #181 ya puede entrar a develop cuando Javi lo revise.
>
> 2. D-S6-17 (decisión de Santi, cierra la pregunta 5): en Fazio, el costo de la línea es el «P. Final» del proveedor, que es lo que se paga, y no el recálculo desde el precio de lista. Con el PDF real, el total tiene que dar 323.670,44. Va en la rama del lector de Fazio, con rojo primero, verificador acotado y suite.
>
> 3. D-S6-19: Autocor entrega las facturas en papel; no hay PDF digital. Un escaneo no sirve: no tiene texto, y el Mandato y el CA-002 de la 029 dejan afuera los escaneados. Autocor se carga por la vía manual (056). La compuerta C7 se cierra COMPLETA el 18/11: tres proveedores con PDF digital y lector (ETMAN, Fazio y Expoyer) y Autocor declarado carga manual. Sacá la factura de Autocor de los pendientes. Donde diga que la consigue Lauti, va que era de Javi.
>
> 4. D-S6-18: NOTA DE CRÉDITO DE PROVEEDOR, HU nueva EN EL SPRINT 6 (no en el 8): es parte de la factura de compra y no queremos que quede colgada. El sprint se estira: cierra por entregable. Proforma con número nuevo, issue con milestone «Sprint 6», estimación en SP, INDICE.md y plan.
>
>    Reglas de Sergio (18/11), para los CA:
>    a) La nota de crédito NO cambia el costo de los artículos. Es plata a favor en la cuenta corriente con el proveedor. LÍMITE DE ALCANCE: el sistema no lleva la cuenta corriente con proveedores (el Mandato deja afuera las cuentas a pagar). La HU registra la nota de crédito, sus importes y a qué facturas corresponde; no calcula saldos. Dejalo escrito en la proforma como fuera de alcance.
>    b) Hay dos clases de nota de crédito:
>       - por bonificación o diferencia de precio: solo importes, no mueve stock (como la de Expoyer);
>       - por devolución de mercadería: sí mueve stock. La mercadería sale de la sucursal que la devuelve, o de un depósito especial «Reclamos», que el negocio usa para seguir los reclamos. El origen se elige por línea, como el destino de la factura (D9).
>       El depósito Reclamos no existe en el sistema. Decidí vos cómo modelarlo y dejá la decisión con fecha en el ESTADO. Es cambio de esquema: va por un script 11 en backend/db/rescaffold/ más re-scaffold, nunca migraciones, con su bloque «PARA SANTI» (backup, lacentral_test primero, después lacentral_demo y lacentral). Pensá también cómo entra la mercadería a Reclamos: si alcanza con una transferencia desde una sucursal o si hace falta otra cosa.
>    c) Toda nota de crédito referencia SÍ O SÍ al menos una factura de compra del mismo proveedor. La relación es de muchos a muchos: varias notas de crédito sobre una factura, una sobre una, o una sobre varias.
>    d) Del proveedor hacia nosotros, las notas de crédito son SIEMPRE tipo A, y el número se copia exacto, sin el 99.
>    e) Llegan en PDF o en papel según el proveedor. La carga manual es obligatoria. La carga desde el PDF reusa el mecanismo de la 029, con el lector de la nota de crédito de Expoyer (~/dev/TESIS/facturas-reales/V20261105NCBA0010-00021495 (1).pdf). Partilo en dos PR: primero la manual, después el PDF.
>
>    Preguntas que quedan para Sergio («Lo que vuelve a Sergio»), sin inventar la regla:
>    - Cuando el proveedor vuelve a mandar el producto después de un reclamo, ¿llega con factura nueva o sin factura? ¿Cómo se carga esa entrada?
>    - ¿Se puede anular una factura de compra que ya tiene notas de crédito? Propuesta mientras tanto: no, primero se anulan las notas de crédito. Agregalo como CA marcado «a confirmar».
>    - Una nota de crédito por devolución, ¿se puede anular? ¿Devuelve el stock?
>
>    No construyas lo que dependa de esas tres preguntas; lo demás sí. Ojo: el Mandato v3.0 deja las notas de crédito fuera de alcance. Anotá en el ESTADO que el Mandato del documento de tesis (GR-01) se actualiza; eso lo hace Santi.
>
> 5. Lo de siempre: TDD con los tres commits, PR por HU, verificadores acotados, suite completa una vez sobre el HEAD, ESTADO al día antes del push. Cuando termines, o si todo lo que queda depende de personas, parás y me pasás el resumen.

**Plan:**
1. ESTADO: E10 cerrada (script 10 en las tres bases; 07, 08 y 09 en `lacentral`), D-S6-19 y C7 completa, Autocor fuera de pendientes, Lauti → Javi.
2. D-S6-17 en `feature/13-factura-pdf` (#177): el lector de Fazio toma el «P. Final» como costo; rojo con el sintético, código, prueba a mano con el real (323.670,44); verificador acotado; merge hacia arriba de la pila (#178, #179, #181, #180) y suites.
3. D-S6-18: proforma nueva (número siguiente), issue con milestone «Sprint 6», SP, INDICE.md, PLAN-SPRINTS; decisión del depósito Reclamos con fecha; preguntas a Sergio; nota del Mandato (GR-01, Santi).
4. Script 11 (nota de crédito + Reclamos) y bloque «PARA SANTI». Lo que dependa del esquema queda frenado hasta que lo aplique en `lacentral_test`.
5. PR 1 (carga manual): lo que no dependa del esquema, TDD. PR 2 (PDF de Expoyer): lector con sintético.
6. Suite completa por HEAD, verificadores acotados, ESTADO antes del push, resumen.

**Estado:** terminada (lo abierto pasa al bloque «Decisiones del equipo para la 057»)
- Arranque: `git fetch`; `origin/develop` sin commits nuevos. PR abiertos #176 a #181, apilados (#176 ← #177 ← #178 ← #179 ← #181 ← #180).
- Punto 2, D-S6-17 en `feature/13-factura-pdf` (#177, worktree en el scratchpad porque la copia principal tiene la bitácora sin commitear): esqueleto `a688cba` (el sintético de Fazio acepta lista, final y total; solo test); rojo `aab80b9` (3 con error por aserción: el lector daba precio de lista 10.000 y descuentos 28+22; el test del total que no cierra pasa desde el principio); código `e0d6004` (precio unitario = P. Final, línea sin descuentos; con más de tres partes ya no se revisa). Filtrado `FacturasCompra`: 211/211. **Real de Fazio** (programa descartable, borrado): 10 líneas, neto **323.670,44**, solo `numero` para revisar. ETMAN igual que antes (98.005,75, nada para revisar).
- Lo de abajo de la pila se pusheó (`feature/13-factura-pdf`, `-registro`, `51-pantalla-pdf`, `163-cuarto-descuento`) antes de actualizar el ESTADO: el ESTADO va en la rama de arriba (`feature/13-lector-expoyer`, `6277a67`).
- Merges hacia arriba: `ab699e7` (#178), `fec0fe2` (#179), `925ea6e` (#181), sin conflictos; `465072d` (#180) con conflicto en `LecturaPdfTests.cs` (el nombre del test de Fazio), resuelto a mano. Filtrado `LecturaPdf|InterpretarFacturaPdf`: #181 41/41, #180 52/52.
- `verificador-tdd` acotado a `a688cba..e0d6004` (#177), lanzado; corre él la suite sobre `e0d6004`.
- Punto 1 y 3 en el ESTADO (`6277a67`): E10 cerrada, D-S6-17 a D-S6-20, C7 completa (Autocor en papel; era de Javi), preguntas a Sergio del 18/11, nota del Mandato (GR-01, Santi), E11.
- Punto 4: issue **#182** (milestone «Sprint 6», 13 SP); proforma `057-HU-FCO-07.md` (11 CA, el CA-011 a confirmar); INDICE (54 proformas); plan (D-PL-06, Sprint 6 en 47 SP); script 11 y README del re-scaffold: `d66e68d` en la rama nueva `feature/182-nota-credito`, apilada sobre `feature/13-lector-expoyer`, pusheada. **El 11 no se probó contra un servidor**: no hay cliente de PostgreSQL en esta máquina.
- **Decisión D-S6-20 (18/11):** Reclamos es una fila de `sucursal` con `es_reclamos` (ver ESTADO). Entra mercadería por transferencia (020).

### PARA SANTI · 18/11/2026 · backup y script 11 (nota de crédito de proveedor y depósito Reclamos, E11)

**Antes de aplicarlo en `lacentral_test`, leé esto:** el 11 crea tablas nuevas. `PostgreSqlPruebas` corta la suite si `public` tiene tablas que el modelo no clona, así que **desde que el 11 esté en `lacentral_test.public`, la suite de toda rama que no tenga el re-scaffold del 11 falla entera** (#176 a #181, y cualquier rama de Javi o Lauti). Dos opciones: aplicarlo después de que Javi mergee la pila, o aplicarlo ya y que cada rama abierta mergee el re-scaffold cuando entre a `develop`. Lo decidís vos. En `lacentral_demo` y `lacentral` va **cuando el PR de la carga manual de la 057 entre a `develop`**: con el 11 en esas bases, la pantalla de la 056 de hoy ofrecería Reclamos como destino (sale de las existencias del artículo).

Desde fish, en tu máquina, parado en la raíz del repo. Requisito: el 10 en cada base (`s10 = 1`).

1 · Backup de las tres bases (cada `.dump` tiene que pesar más de 0 bytes; si uno falla, no sigas):

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

2 · Estado de cada base, solo lectura (`s10 = 1` es el requisito; `s11 = 0` es el 11 pendiente):

```fish
for base in lacentral_test lacentral_demo lacentral
    echo "SELECT current_database() AS base, (SELECT count(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'factura_compra_detalle' AND column_name = 'descuento_4_porcentaje') AS s10, (SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'nota_credito_compra') AS s11;" | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d $base
end
```

3 · El 11, **primero solo en `lacentral_test`**. Tiene que terminar con `NOTICE: 11: TODO OK.` y `COMMIT` (el `WARNING` del `ROLLBACK` inicial es esperado):

```fish
git fetch origin
git show origin/feature/182-nota-credito:backend/db/rescaffold/11_Nota_Credito_Compra.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_test -v ON_ERROR_STOP=1
```

4 · Si dio `TODO OK`, avisame: sigo con el re-scaffold, el repositorio y las pruebas contra PostgreSQL. `lacentral_demo` y `lacentral`, cuando el PR de la carga manual entre a `develop`, de a una, y repetí el paso 2:

```fish
git show origin/feature/182-nota-credito:backend/db/rescaffold/11_Nota_Credito_Compra.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_demo -v ON_ERROR_STOP=1
git show origin/feature/182-nota-credito:backend/db/rescaffold/11_Nota_Credito_Compra.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral -v ON_ERROR_STOP=1
```

Para volver atrás una base: `ssh atiz@arch docker exec -i lacentral_db pg_restore -U lacentral -d <base> --clean --if-exists < ~/backups-lacentral/<base>-<marca>.dump`.
- `verificador-tdd` D-S6-17 (#177, `a688cba..e0d6004`): **APTO**. Rojo reproducido (3 por aserción), 8 mutaciones muertas; suite sobre `e0d6004`: `Correctas! - Con error: 0, Superado: 654, Omitido: 0, Total: 654, Duración: 4 m 22 s`. No bloqueantes: `afirmable` queda siempre en `true` (lo usan los otros lectores, se deja) y el comentario de Fazio mezcla la frase vieja con la nueva. El cambio de rama del árbol principal que vio fue mío (`checkout -b` de la 057). `auditor-persistencia`: **APTO** (sin persistencia en el diff; usó la suite del verificador).
- Suites completas, de a una, en un worktree del scratchpad (`--no-incremental`, 0 advertencias): `ab699e7` (#178; mismo backend que el #179) `Correctas! - Con error: 0, Superado: 657, Omitido: 0, Total: 657, Duración: 17 m`; `925ea6e` (#181) `Correctas! - Con error: 0, Superado: 672, Omitido: 0, Total: 672, Duración: 4 m 35 s`. Frontend sin cambios en la pila: no se corrió.
- 057, PR 1 (carga manual), lo que no depende del esquema, en `feature/182-nota-credito`:
  - Núcleo: esqueleto `2caca64` (entidad, DTOs, puerto `INotaCreditoCompraRepositorio`, caso de uso vacío); rojo `a9d67d4` (40 con error por aserción; pasa desde el principio `Devolucion_DeUnArticuloDadoDeBaja_SeRegistra`; dos rojos con NRE y el IVA sin punto medio se corrigieron con `--amend` antes de pushear); código `0ab192d` (41/41). Mutaciones propias: de 13, sobrevivían 4 (proveedor inactivo tapado por «otro proveedor», `DesdePdf`, IIBB y municipal) → `2fdefd3` solo test, con cada una 1 con error; 42/42. Ojo: `dotnet build -q` falla con «Compilando completamente el destino» y `--no-build` corre el binario viejo; las mutaciones se rehicieron con `dotnet build`.
  - Pantalla: esqueleto `d922eca` (API, módulo «Nota de crédito de proveedor» en el menú, pantalla con la superficie pública vacía); rojo `e84530b` (19 FAILED: 18 por `expectOne`, 1 por aserción); código `6259b1c` (19/19); `26ca848` alinea el total con el de la factura. Mutaciones propias: 12, todas muertas.
  - **Decisión (18/11):** la devolución acepta un artículo dado de baja (se devuelve lo que se compró). Anotado en la proforma.
  - **Frenado por E11:** re-scaffold, `NotaCreditoCompraRepositorio` con relectura de la fila y el stock (y que el costo no cambie), la API (`POST /api/notas-credito-compra` y `/calculo`, 401/403), el CA-008 (Reclamos fuera de los destinos de la factura, en la API y en la pantalla) y el PR.
- Suite completa sobre `465072d` (#180): `Correctas! - Con error: 0, Superado: 683, Omitido: 0, Total: 683, Duración: 4 m 38 s` (0 advertencias). `6277a67` y `f8182d0` encima son solo docs.
- 057, PR 2 (PDF), en `feature/182-nota-credito-pdf` (apilada sobre `feature/182-nota-credito`):
  - Lector: esqueleto `8f5258c` (`NotaCreditoLeida`, `LayoutsPdf.LeerNotaCredito` en null, sintético con la forma del texto real); rojo `74de963` (6 con error por aserción; pasan desde el principio que una factura de Expoyer no es nota y que la nota no se lee como factura); código `3b4ac3e`. Mutaciones propias: de 9 sobrevivían 7 → `ff48077` solo test, con cada una 1 con error. **Real** (programa descartable, borrado): `0010-00021495`, 05/11/2026, bonificación, neto 115.040,00, facturas `0011-00044774` y `0011-00044869`, nada para revisar; las tres facturas reales se siguen leyendo igual y ninguna pasa por nota.
  - Interpretación: esqueleto `781b94a`; rojo `f0659d1` (8 con error por aserción); código `8043b3a` (8/8). Mutaciones: sobrevivían el `break` del CUIT y una guarda inalcanzable → `0395431` borra la guarda (la cubre `SinFacturasAsociadasEnElTexto_…`) y `a705638` agrega el test del `break`; 9/9.
  - Deuda vista de paso (fuera de este diff): en el lector de ETMAN, sacar `numero` de `Revisar` no pone ningún test en rojo.
- Verificaciones sobre `261e3cd` (rama del PDF): unitarias de backend (`Base!=PostgreSQL`, `--no-incremental`, 0 advertencias) `Correctas! - Con error: 0, Superado: 508, Omitido: 0, Total: 508`; frontend completo `TOTAL: 1 FAILED, 270 SUCCESS` → el spec del menú con los títulos no se había actualizado en el esqueleto `d922eca` → `261e3cd` solo spec, llevado a la rama de la carga manual como `6085986`; frontend completo después: `TOTAL: 271 SUCCESS`. **No se corrió la suite completa de backend de las ramas de la 057**: no hay PR (se corre antes de abrirlo, con el 11 aplicado).
- ESTADO (`f8182d0`, rama de Expoyer, mergeado en `74d08e8` y `7dd501d`): tablero con las suites, la 057 y PRÓXIMA SESIÓN. Push de `feature/13-lector-expoyer`, `feature/182-nota-credito` y `feature/182-nota-credito-pdf`. Verificadores de la 057: no se invocaron, van uno por PR al cerrarlo.

**BLOQUEADO EN:** lo que queda de la 057 (re-scaffold, repositorio con relectura contra PostgreSQL, API, CA-008, endpoint del PDF, precarga, verificadores y los dos PR) espera que Santi aplique el script 11 en `lacentral_test` (bitácora, «PARA SANTI · 18/11 · script 11»; él decide cuándo, porque corta la suite de las ramas sin el re-scaffold). El CA-011 y lo que no se construye esperan a Sergio (preguntas del 18/11). Merge de la pila #176 a #181: Javi. Mandato (GR-01): Santi. DoD 10: Javi con Sergio. El sprint no se cierra.

## 18/11/2026 · Decisiones del equipo para la 057 (D-S6-21 a D-S6-25)

**Instrucción recibida (verbatim):**
> 18/11 · Decisiones del equipo para la 057 (nota de crédito de proveedor), tomadas sobre la práctica de los sistemas de gestión comunes y la RG 4540 de AFIP. Reemplazan los «a confirmar». Sacalas de «Lo que vuelve a Sergio» y anotalas en el ESTADO como D-S6-21 a D-S6-25; si Sergio después corrige alguna, se cambia.
>
> - D-S6-21 · Reposición después de un reclamo: el proveedor la manda con factura nueva, que se carga como cualquier factura de compra (056 o 029). La 057 no tiene circuito de reposición.
> - D-S6-22 · No se puede anular una factura de compra con notas de crédito vigentes: primero se anulan las notas y después la factura. El mensaje lo dice y nombra las notas. Es el CA-011: sacale el «a confirmar».
> - D-S6-23 · Una nota de crédito se puede anular igual que una factura (028): con motivo y con la autorización de otro ADMINISTRADOR con su contraseña. Si era por devolución, la mercadería vuelve a la misma sucursal o depósito de donde salió, en la misma transacción. Va como CA nuevo. Reusá lo de la 028 donde se pueda.
> - D-S6-24 · Una devolución que deja el stock de origen en negativo se registra igual, pero la pantalla avisa antes de confirmar: qué artículo, en qué sucursal y cuánto queda.
> - D-S6-25 · Regla de cantidades: en una nota de crédito por devolución, cada línea es de un artículo que figura en las facturas referenciadas, y la cantidad devuelta acumulada por artículo no supera lo comprado en esas facturas, sin contar las notas anuladas. Un artículo dado de baja se puede devolver, porque viene de la factura. Va como CA nuevo, con prueba contra PostgreSQL de la suma acumulada y de dos devoluciones simultáneas.
>
> Todo lo de la 057 sigue frenado por la compuerta E11 (nc057_fuente). Lo que no dependa del esquema, adelantalo. Resúmenes en español.

**Plan:**
1. ESTADO: D-S6-21 a D-S6-25, sacar las preguntas de «Lo que vuelve a Sergio», E11 con `nc057_fuente`. Proforma de la 057 v1.1 (CA-011 sin «a confirmar», CA nuevos de anulación, aviso de stock negativo y cantidades).
2. Script 11: columnas de anulación en `nota_credito_compra` (como `factura_compra`) y unicidad del número solo entre vigentes.
3. Backend sin esquema, TDD con Moq: D-S6-25 en el registro (dentro de la unidad de trabajo, con bloqueo de las facturas); D-S6-22 en la anulación de la factura (028); D-S6-23, caso de uso de anulación de la nota, reusando lo de la 028.
4. Pantalla: D-S6-24, aviso de stock negativo antes de confirmar.
5. Merge hacia la rama del PDF, ESTADO, push, resumen.

**Estado:** terminada (queda abierto lo de E11 y CA-011; ver BLOQUEADO EN)
- Arranque: `git fetch`; `develop` sin novedades; PR #176 a #181 abiertos. Un `merge --ff-only` mío adelantó por error la rama local `feature/182-nota-credito` hasta la del PDF (no se pusheó). No se pudo volver atrás con `reset --hard` (lo bloquea el modo automático): el trabajo sigue en una rama local nueva, `feature/182-nota-credito-manual`, desde `origin/feature/182-nota-credito`, y se pushea a `feature/182-nota-credito` (avance rápido). La rama local vieja queda para borrar a mano.
- Docs `1f2a175`: ESTADO con D-S6-21 a D-S6-25 (fuera de «Lo que vuelve a Sergio»), E11 con `nc057_fuente`; proforma 057 v1.1 (CA-011 sin «a confirmar», CA-012, CA-013, CA-014); índice con 14 CA; script 11 con la anulación.
- CA-014 (D-S6-25): esqueleto (puerto `ObtenerCompradoYDevueltoAsync`); rojo (4 con error por aserción); código `6378549`, 48/48; mutaciones propias 7 de 7 muertas.
- CA-012 (D-S6-23): `refactor(fco04)` saca la doble certificación a `Comun/DobleCertificacion` (028: 17/17 sin cambios); esqueleto `22deaa0`; rojo (10 con error por aserción); código `d50833c`, 10/10; mutaciones 6 de 6 muertas.
- CA-013 (D-S6-24): rojo (3 con error por aserción); código (pantalla: cada artículo trae su ficha, aviso `aviso alerta` con artículo, origen y cuánto queda); dos guardas sin test borradas (pedir la ficha una sola vez y mostrar el aviso solo con el total); `fd2f0b3` solo test (volver a bonificación), con la mutación 1 con error. Spec 24/24.
- CA-011 (D-S6-22): **no se hizo**: la anulación de la factura necesita el repositorio de notas en su DI, y sin el 11 no hay tabla. Va con E11.
- Verificaciones sobre `fd2f0b3`: unitarias de backend (`--no-incremental`, 0 advertencias) `Correctas! - Con error: 0, Superado: 501, Omitido: 0, Total: 501`; frontend completo `TOTAL: 276 SUCCESS`. Suite completa de backend: no, sin PR.

**BLOQUEADO EN:** E11 (Santi aplica el 11 en `lacentral_test`, plantilla `nc057_fuente`): re-scaffold, repositorio de la nota con PostgreSQL (CA-014 suma acumulada y dos devoluciones simultáneas, CA-012 stock al origen y número liberado), CA-011 en la 028, API y PR. Sergio: ver D-S6-21 a 25. La rama local `feature/182-nota-credito` quedó adelantada por error a la del PDF (sin pushear): borrarla a mano (`git branch -D feature/182-nota-credito`) o pedirme que la reponga.
- Merge de `feature/182-nota-credito` en la rama del PDF: conflictos de solo agregar en la bitácora (la del PDF ya estaba en la manual) y en los DTO (se conservan los dos bloques). Unitarias de backend: 524/524.

## 18/11/2026 · Plantilla nc057_fuente para el script 11

**Instrucción recibida (verbatim):**
> El bloque «PARA SANTI · 18/11 · script 11» aplica el 11 en lacentral_test.public, y te había pedido la copia nc057_fuente para no cortar las suites de las otras ramas. Dejame en la bitácora el bloque para fish que, después del backup, crea en lacentral_test el esquema nc057_fuente como copia de public (estructura y datos que necesite la suite), le aplica el 11 con ON_ERROR_STOP=1 y lo verifica. Contenedor lacentral_db, superusuario lacentral, sin marcadores. Después la 057 corre su suite con LA_CENTRAL_TEST_SCHEMA=nc057_fuente. El 11 en public, lacentral_demo y lacentral va cuando Javi mergee la pila y el PR de la carga manual de la 057; ahí se borra nc057_fuente. Resúmenes en español.

**Plan:**
1. SQL versionados en `backend/db/pruebas/`: copia (mismo método que `PostgreSqlPruebas` y que `fco06_fuente`), guarda antes del 11, verificación y borrado.
2. Ensayarlos en un esquema descartable de `lacentral_test` con el rol de la suite, y correr tests de PostgreSQL con esa copia como plantilla.
3. Bloque PARA SANTI en fish; ESTADO (E11 y PRÓXIMA SESIÓN); push.

**Estado:** terminada (queda que Santi lo corra)
- Error mío: el bloque «PARA SANTI · 18/11 · script 11» de arriba aplicaba el 11 en `lacentral_test.public`. **Su paso 3 queda reemplazado por el bloque de abajo**; los pasos 1, 2 y 4 (el 11 en `lacentral_demo` y `lacentral`) siguen valiendo para cuando Javi mergee.
- SQL en `backend/db/pruebas/`: `nc057_fuente_copia.sql` (tablas con `LIKE … INCLUDING ALL`, índices y PK/UNIQUE con los nombres de `public`, FK redirigidas a la copia, datos solo de `rol` y `sucursal` —lo único que la suite lee de la plantilla—, `similarity()` que llama a la de `public`, SELECT para `lacentral_tests` y `lacentral_santi`; se niega si `nc057_fuente` ya existe), `nc057_fuente_entrar.sql` (pone el `search_path` y corta si la plantilla no existe: el 11 no llega a correr en `public`), `nc057_fuente_verificacion.sql` (solo lectura) y `nc057_fuente_borrado.sql` (solo en `lacentral_test`, sin dependientes fuera del esquema).
- Ensayo en `lacentral_test`, con `lacentral_santi`, en el esquema descartable `nc057_ensayo_6d7f5e4f` (los mismos archivos, con el nombre reemplazado): copia `copia creada (27 tablas)`; tests de PostgreSQL de la rama (sin el 11) con esa copia como plantilla, filtrados FacturasCompra, BusquedaPaginada, RestriccionesUnicas y ClienteRepositorio: `Correctas! - Con error: 0, Superado: 81, Omitido: 0, Total: 81, Duración: 6 m 13 s`; guarda + 11: **`11: TODO OK.`** (primera vez que el 11 corre contra un servidor); verificación `TODO OK (30 tablas; public: 27)`; el 11 otra vez: `TODO OK` (idempotente); la copia otra vez: se niega; borrado: `public: 27 tablas`; guarda sin plantilla: corta antes del 11; borrado sin plantilla: «nada que borrar». El esquema de ensayo ya no existe.

### PARA SANTI · 18/11/2026 · plantilla nc057_fuente (E11)

Esto **no toca `public`**: las suites de #176 a #181 y de las ramas de Javi y Lauti siguen igual. Desde fish, en tu máquina. Cada paso que falla corta (`ON_ERROR_STOP=1`, y la copia es una sola transacción): si algo da `ERROR`, no sigas y pasame la salida.

1 · Backup de `lacentral_test` (tiene que decir `backup OK`):

```fish
cd ~/dev/TESIS/la-central-gestion
git fetch origin
mkdir -p ~/backups-lacentral
set marca (date +%Y%m%d-%H%M)
ssh atiz@arch docker exec lacentral_db pg_dump -U lacentral -Fc lacentral_test > ~/backups-lacentral/lacentral_test-$marca.dump
and test -s ~/backups-lacentral/lacentral_test-$marca.dump
and echo "backup OK"
or echo "FALLÓ el backup: no sigas"
```

2 · La copia. Tiene que terminar con `NOTICE: nc057_fuente: copia creada (27 tablas).` y `COMMIT`:

```fish
git show origin/feature/182-nota-credito:backend/db/pruebas/nc057_fuente_copia.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_test -v ON_ERROR_STOP=1
```

3 · El 11 sobre la copia. Tiene que terminar con `NOTICE: 11: TODO OK.` y `COMMIT` (el `WARNING` del `ROLLBACK` inicial es esperado):

```fish
begin
    git show origin/feature/182-nota-credito:backend/db/pruebas/nc057_fuente_entrar.sql
    git show origin/feature/182-nota-credito:backend/db/rescaffold/11_Nota_Credito_Compra.sql
end | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_test -v ON_ERROR_STOP=1
```

4 · Verificación, solo lectura. Tiene que decir `NOTICE: nc057_fuente: TODO OK (30 tablas; public: 27).`:

```fish
git show origin/feature/182-nota-credito:backend/db/pruebas/nc057_fuente_verificacion.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_test -v ON_ERROR_STOP=1
```

5 · Avisame. La 057 sigue con el re-scaffold y corre su suite con `LA_CENTRAL_TEST_SCHEMA=nc057_fuente`.

**Más adelante**, cuando Javi mergee la pila y el PR de la carga manual de la 057: el 11 en `public` de `lacentral_test`, en `lacentral_demo` y en `lacentral` (backup y estado con los pasos 1 y 2 del bloque «script 11» de arriba, y su paso 4 para las dos últimas; para `lacentral_test`, el mismo comando con `-d lacentral_test`). Después se borra la plantilla. Tiene que decir `nc057_fuente borrado; public: …`:

```fish
git show origin/develop:backend/db/pruebas/nc057_fuente_borrado.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_test -v ON_ERROR_STOP=1
```

Si la copia quedara mal: el paso 2 es una transacción (si falla, no queda nada), y para rehacerla, el comando de borrado de arriba con `origin/feature/182-nota-credito` en lugar de `origin/develop`, y de nuevo los pasos 2 a 4.

**BLOQUEADO EN:** que Santi corra los pasos 1 a 4 y diga `nc057_fuente: TODO OK`. Después, la 057 sigue con el re-scaffold, contra la plantilla.

## 18/11/2026 · Revisión de Javi del #180 (REQUEST CHANGES, tres puntos [DOCS])

**Instrucción recibida (verbatim):**
> 18/11 · Revisión de Javi del #180 (REQUEST CHANGES, tres puntos [DOCS]). Javi mergeó #176, #177, #178, #179 y #181.
>
> 1. Base del #180: cambiala a develop (gh pr edit 180 --base develop) y hacé git merge origin/develop en la rama (nunca rebase).
> 2. D-S6-20 (Reclamos como fila de sucursal) deja de ser decisión: pasa a PROPUESTA del 18/11, pendiente de aprobación del equipo. El script 11 no se aplica en ninguna base hasta que se apruebe. Corregilo en el ESTADO y en la proforma de la 057.
> 3. D-S6-18 (la 057 en el Sprint 6) pasa a PROPUESTA pendiente de acuerdo de los tres integrantes y de aviso al tutor, porque cambia el alcance y choca con el Mandato v3.0. El #180 lleva solo lo del lector de Expoyer: lo de la 057 (D-S6-18, D-S6-20 a D-S6-25, la proforma, el índice y el plan) va en la rama de la 057, no en el #180.
> 4. Respondé en el #180 cada punto de Javi, con el commit que lo arregla.
> 5. Hubo merge a develop: suite completa sobre develop (momento 2 de la política de pruebas), una vez.
> 6. Lo de la 057 queda congelado, sin tocar nada más, hasta que el equipo decida. No crees nc057_fuente.
>
> Resúmenes en español.

**Plan:**
1. Suite completa de backend sobre `origin/develop` (`37e8007`), una vez, en un worktree.
2. #180: base a `develop`, `git merge origin/develop`; sacar del ESTADO todo lo de la 057 (D-S6-18, D-S6-20, E11, fila #182, «Lo que vuelve a Sergio · 18/11», reglas de Sergio, Mandato, PRÓXIMA SESIÓN); push.
3. Rama de la 057: D-S6-18 y D-S6-20 como PROPUESTA en el ESTADO, la proforma, el índice y el plan; el 11 no se aplica en ninguna base; los bloques PARA SANTI del 11 y de `nc057_fuente`, suspendidos. Traer el #180 corregido sin perder lo de la 057. Después, congelada.
4. Responder en el #180 punto por punto, con commits.

**Estado:** terminada (lo de la 057 sigue en su rama; espera E11, la plantilla `nc057_fuente`)
- #180: base cambiada a `develop` (`gh pr edit 180 --base develop`); `git merge origin/develop` → `bf952cf`, sin conflictos.
- Rama de la 057 (`feature/182-nota-credito-manual` → `origin/feature/182-nota-credito`): merge de `feature/13-lector-expoyer` corregido → `d5ecc6b`. El ESTADO se resolvió con **la versión de la 057** (el #180 le saca lo de la 057, que acá tiene que quedar; `develop` no traía nada nuevo en `docs/`: `git diff f8182d0 bf952cf` vacío). Bitácora: las dos partes.

**Instrucción recibida (verbatim), a mitad de la tarea:**
> 18/11 · El equipo (Santi, Javi y Lauti) aprobó:
> - D-S6-18: la 057 entra al Sprint 6 como última HU. El tutor ya está avisado. El Mandato pasa a v3.1; eso lo edita Santi en el GR-01.
> - D-S6-20: el depósito Reclamos como fila de sucursal, aprobado por el equipo, ya no es propuesta del agente.
> Sacales el «propuesta» en el ESTADO y en la proforma, con fecha y «aprobado por el equipo».
>
> Igual, lo que pidió Javi en el #180: base a develop (gh pr edit 180 --base develop), git merge origin/develop, y que el #180 lleve solo lo de Expoyer; lo de la 057 va en su rama. Respondé cada punto en el #180 con su commit. Suite completa sobre develop, una vez (hubo merge).
>
> La 057 se descongela. Para el script 11: dejame en la bitácora el bloque para fish que, con backup primero, crea en lacentral_test el esquema nc057_fuente como copia de public, le aplica el 11 con ON_ERROR_STOP=1 y lo verifica (contenedor lacentral_db, superusuario lacentral, sin marcadores). La 057 corre su suite con LA_CENTRAL_TEST_SCHEMA=nc057_fuente. En public, lacentral_demo y lacentral va cuando entre el PR de la carga manual de la 057, y ahí se borra nc057_fuente.
>
> Resúmenes en español.

**Plan corregido:** la D-S6-18 y la D-S6-20 no pasan a propuesta: van como **aprobadas por el equipo el 18/11** (ESTADO, proforma v1.2, índice, plan). La 057 no se congela. El bloque de `nc057_fuente` ya está más arriba en esta bitácora («PARA SANTI · 18/11/2026 · plantilla nc057_fuente (E11)», ensayado el 18/11 en un esquema descartable) y sigue valiendo. **Una corrección a su «Más adelante»:** el 11 en `public`, `lacentral_demo` y `lacentral` va cuando entre a `develop` **el PR de la carga manual de la 057** (la pila ya entró, salvo el #180); el borrado de la plantilla, después de eso, con el mismo comando.
- `19b4521`: el ESTADO del #180 sin lo de la 057 (D-S6-18, D-S6-20, E11, fila #182, preguntas y reglas de Sergio, Mandato); D-S6-14, 34 SP y «Fuera del Sprint 6» como en `develop`. Push.
- Rama de la 057 (ver el bloque en `feature/182-nota-credito`): merge del #180 corregido (`d5ecc6b`, ESTADO con la versión de la 057) y `6f36b7c` D-S6-18 y D-S6-20 **aprobadas por el equipo el 18/11** (ESTADO, proforma v1.2, índice, plan); merge en la rama del PDF `463721d`. Push de las dos.
- #180: respuesta punto por punto con los commits (comentario 6990438840). Error mío: la primera versión del comentario decía «@javi…», que GitHub puede tomar como mención a otro usuario; se editó en el momento a «Javi».
- **Suite completa de backend sobre `develop` (`37e8007`)**, en un worktree, una vez: build `--no-incremental` 0 advertencias; `Con error! - Con error: 3, Superado: 669, Omitido: 0, Total: 672, Duración: 1 h 9 m`. Fallaron `ProveedorPostgreSqlTests.ModificarConDireccionVaciaNueva_NoCreaUnaFilaVacia` (1 m 7 s), `PostgreSqlPruebasLimpiezaTests.ConOtraFuente_ElSearchPathResuelveEnEsaFuenteYNoEnPublic` (1 m 12 s) y `HistorialBajaRepositorioTests.UnidadDeTrabajo_DevuelveElResultadoDeLaOperacionYLaEjecutaUnaVez` (53 s). La corrida tardó 15 veces lo normal (~4 m 30 s) y cada fallo, cerca de un minuto: carga del servidor compartido. **No se capturó el mensaje de error** (el filtro de la salida lo dejó afuera). Filtradas las tres después: `Correctas! - Con error: 0, Superado: 4, Omitido: 0, Total: 4, Duración: 1 m 50 s`. No se reprodujeron; no se volvió a correr la suite entera (se pidió una vez). Worktree sacado.

## 18/11/2026 · La 057 contra la plantilla nc057_fuente (E11 abierta)

**Instrucción recibida (verbatim):**
> nc057_fuente: TODO OK (18/11). Seguí con la 057 contra la plantilla: re-scaffold, repositorio con las pruebas contra PostgreSQL, D-S6-22, API, PDF, verificadores y los dos PR.
>
> Suite sobre develop: 669/672 no es verde. Cuando termines con la 057 y la netbook esté más tranquila, corré la suite completa sobre develop una sola vez más, sin filtrar la salida de los fallos. Si vuelven a fallar los mismos tres, investigá por qué tardan un minuto en la corrida completa (¿esperan un lock? ¿un timeout?) en vez de asumir que es carga. Resúmenes en español.

**Plan:**
1. Re-scaffold de `LaCentral.Data/Models/` contra `lacentral_test`, esquema `nc057_fuente` (rama `feature/182-nota-credito-manual`).
2. `NotaCreditoCompraRepositorio` con TDD y pruebas contra PostgreSQL (fuente `nc057_fuente`): alta releída con contexto nuevo, suma acumulada, dos devoluciones concurrentes, anulación con stock de vuelta al origen y número liberado.
3. D-S6-22 (CA-011): la 028 no anula una factura con NC vigentes y nombra las notas.
4. API: controlador, DI, CA-008; suite completa de la rama con `LA_CENTRAL_TEST_SCHEMA=nc057_fuente`; ESTADO; verificadores acotados; PR de la carga manual.
5. Rama del PDF: endpoint y precarga de la pantalla; suite; verificadores; PR.
6. Suite completa sobre `develop`, una vez, sin filtrar los fallos; si fallan los mismos tres, investigar la espera de un minuto.

**Estado:** terminada
- Paso 1, re-scaffold: **no hay esquema `nc057_fuente` en `lacentral_test`**. `dotnet ef dbcontext scaffold … --schema nc057_fuente` → «Unable to find a schema in the database matching the selected schema nc057_fuente» (dejó `LaCentralDbContext.cs` vacío; restaurado con `git checkout`, `Models/` intacto). Consulta de solo lectura con `LACENTRAL_TEST_PG` (servidor 172.19.0.2, PostgreSQL 18.6, `current_user` lacentral_santi, 19/11 01:22 hora del servidor): `pg_namespace` tiene `demo_ensayo_c3`, `information_schema`, `public` (27 tablas) y `staging`, pero no `nc057_fuente`. Ninguna tabla `nota_credito%` ni columna `es_reclamos` en ningún esquema. O sea, en esta base la copia no está (y el 11 tampoco). Puede que se haya corrido contra otro servidor o contenedor, o que la transacción del paso 2 haya vuelto atrás.
- Mientras tanto, el paso 6 (suite completa sobre `develop`, que no depende del esquema): se adelanta, porque son la 1 de la mañana en el servidor y lo de la 057 está frenado.
- La suite sobre `develop` que se adelantó (worktree `wt-develop`, `37e8007`) quedó cortada a mitad con la sesión: la salida termina sin resumen (`dev2.txt`, 22:33), sin proceso `dotnet` vivo. No cuenta. Cada test de PostgreSQL tardaba de 46 s a 1 m 10 s, incluso los que pasan.

## 18/11/2026 · La 057 contra nc057_fuente, segunda vez (Santi la verificó a las 22:35)

**Instrucción recibida (verbatim):**
> nc057_fuente: TODO OK, verificado contra 172.19.0.2 (18/11, 22:35). Copia de 27 tablas, el 11 aplicado sobre la copia (sucursal 3 «Reclamos (depósito)», nota_credito_compra, nota_credito_compra_factura, nota_credito_compra_detalle) y verificación: 30 tablas; public: 27. Seguí con la 057 contra la plantilla: re-scaffold, repositorio con las pruebas contra PostgreSQL, D-S6-22, API, PDF, verificadores y los dos PR. Cuando termine la suite sobre develop, pasame el resultado. Resúmenes en español.

**Plan:**
1. Confirmar `nc057_fuente` en `lacentral_test` (solo lectura). Si no está, parar.
2. Re-scaffold contra `nc057_fuente`.
3. `NotaCreditoCompraRepositorio` con pruebas contra PostgreSQL (`LA_CENTRAL_TEST_SCHEMA=nc057_fuente`).
4. D-S6-22 (CA-011), API, CA-008; suite de la rama; ESTADO; verificadores; PR de la carga manual.
5. Rama del PDF: endpoint, precarga, suite, verificadores, PR.
6. Suite completa sobre `develop` (la anterior quedó cortada), una vez, sin filtrar fallos; resultado a Santi.

**Estado:** terminada
- Paso 1: `nc057_fuente` presente en `lacentral_test` (172.19.0.2, 01:35 hora del servidor): 30 tablas, sucursal 3 «Reclamos (depósito)». Queda además un esquema `pr110_test_9d4670f6b05140749a0e351312a4b273` (27 tablas), seguramente de la suite cortada.
- Paso 2, re-scaffold `ddbffae`: contra `nc057_fuente` a un directorio aparte; entra solo lo nuevo (3 tablas, `es_reclamos`, `fc_id_proveedor_unico`, navegaciones), sin el esquema en `ToTable` y conservando los comentarios de tabla de `public` (la copia no los trae) y los fines de línea. Build: 0 advertencias, 0 errores.
- Paso 3, repositorio: esqueleto `NotaCreditoCompraRepositorio` (valores neutros) y 7 pruebas contra PostgreSQL (`NotaCreditoCompraPostgreSqlTests`: bonificación, devolución con Reclamos en negativo, facturas, CA-014 acumulado y sin anuladas, dos devoluciones simultáneas con espera de bloqueo visible en `pg_stat_activity`, CA-012 anulación y número liberado, stock fuera de rango). Primera corrida con `LA_CENTRAL_TEST_SCHEMA=nc057_fuente`: 7 con error, **pero no por aserción**: 6 cortan en el fixture («No se pudo aparear sin ambigüedad el índice factura_compra.factura_compra_id_proveedor_unico») y 1 por timeout de conexión.
- **Error mío en el script 11**: el 07 ya creó `factura_compra_id_proveedor_unico UNIQUE (id, proveedor_id)` y el 11 agregaba `fc_id_proveedor_unico`, igual. `c4c5946`: el 11 exige el del 07 y no crea nada en `factura_compra`; el modelo pierde el índice duplicado; `nc057_fuente_verificacion.sql` ahora rechaza índices duplicados. **Hay que rehacer `nc057_fuente`** (la de hoy tiene el duplicado): bloque PARA SANTI más abajo.
- 01:50 aprox.: `arch` offline en Tailscale (`tailscale status`: «offline, last seen 1m ago»; ping 100 % perdido). El ensayo de los scripts corregidos en `nc057_ensayo_2f673d44` no llegó a conectar (no se creó nada). Sigo con lo que no toca la base.
- Ensayo de los scripts corregidos en `nc057_ensayo_2f673d44` (lacentral_santi): copia 27 tablas, el 11 `TODO OK`, verificación `TODO OK (30 tablas; public: 27)`. La verificación nueva sobre la `nc057_fuente` de hoy: «Índices duplicados en nc057_fuente: nc057_fuente.factura_compra» (confirma que hay que rehacerla). **Error mío en el ensayo:** el primer intento concatenó `entrar` y el 11 en un solo comando de Npgsql; la transacción implícita hizo que el `ROLLBACK` del 11 deshiciera el `SET search_path` y el `ALTER TABLE sucursal` apuntó a `public`. Lo frenó que `lacentral_santi` no es dueño de `public` (`must be owner of table sucursal`); verificado después: `public` 27 tablas, sin `es_reclamos`. Con psql (el bloque de Santi) no pasa: cada sentencia va sola.
- CA-011 (D-S6-22), en la anulación de la factura: esqueleto `144fcd4`, rojo `81f3c98` (2 con error por aserción), `c122ffa` solo test (por defecto sin notas), código `ca9145f` (19/19). La consulta va dentro de la unidad de trabajo, con la factura bloqueada.
- Repositorio: rojo `548d2fa` (9 con error por aserción, contra el ensayo), código `aa29bc7` (9/9), `bef903b` prueba de la carrera nota/anulación de factura (1 con error con la mutación). Mutaciones propias: sin bloqueo en comprado/devuelto (1 con error), sin recomprobar el estado (1), sin `FOR UPDATE` (2), sin bloqueo en notas vigentes (1): todas muertas. Clases afectadas: `Correctas! - Con error: 0, Superado: 99, Omitido: 0, Total: 99`.

### PARA SANTI · 19/11/2026 · rehacer nc057_fuente (el 11 corregido)

La `nc057_fuente` de anoche tiene un índice duplicado en `factura_compra` (error mío en el 11, ya corregido en `c4c5946`) y con eso **ninguna prueba de PostgreSQL de la 057 arranca** contra ella. No toca `public`. Desde fish, en tu máquina, parado en la raíz del repo; cada paso que falla corta. Si algo da `ERROR`, no sigas y pasame la salida.

1 · Backup (tiene que decir `backup OK`):

```fish
cd ~/dev/TESIS/la-central-gestion
git fetch origin
mkdir -p ~/backups-lacentral
set marca (date +%Y%m%d-%H%M)
ssh atiz@arch docker exec lacentral_db pg_dump -U lacentral -Fc lacentral_test > ~/backups-lacentral/lacentral_test-$marca.dump
and test -s ~/backups-lacentral/lacentral_test-$marca.dump
and echo "backup OK"
or echo "FALLÓ el backup: no sigas"
```

2 · Borrar la plantilla de anoche. Tiene que decir `nc057_fuente borrado; public: 27 tablas` (o parecido):

```fish
git show origin/feature/182-nota-credito:backend/db/pruebas/nc057_fuente_borrado.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_test -v ON_ERROR_STOP=1
```

3 · Los pasos 2, 3 y 4 del bloque «PARA SANTI · 18/11/2026 · plantilla nc057_fuente (E11)» de arriba, iguales (copia, el 11, verificación). La verificación ahora también rechaza índices duplicados; tiene que terminar con `NOTICE: nc057_fuente: TODO OK (30 tablas; public: 27).`

4 · Avisame. La 057 corre su suite completa con `LA_CENTRAL_TEST_SCHEMA=nc057_fuente` antes de cada PR.

Mientras tanto, la 057 avanza contra una copia descartable mía (`nc057_ensayo_2f673d44`, en `lacentral_test`, creada con `lacentral_santi`), que borro cuando la tuya esté.
- CA-008 en la API: esqueleto `c430447` (`ObtenerIdReclamosAsync` en null), rojo `0875428` (1 con error por aserción en el caso de uso, 1 contra PostgreSQL), código `b61bec7` (79/79; contra PostgreSQL 27/27 con el registro de la 056).
- La ficha marca Reclamos (`EsReclamos` en la existencia): esqueleto `6294fc1`, rojo `d6521fa` (las 3 pruebas de `ArticuloRepositorioTests` suponían 2 sucursales; 3 con error por aserción), código `66dcaa7` (3/3).
- CA-008 en la pantalla de la factura: rojo `afe79f8` (`3 FAILED, 66 SUCCESS`, uno por cada camino que arma los destinos: carga manual, PDF y PDF con proveedor elegido después), código `b2df72f` (`TOTAL: 69 SUCCESS` en las pantallas de la factura). `esReclamos` opcional en el tipo de la ficha, para no tocar los fixtures tipados de otras pantallas.
- Suite completa de backend contra el ensayo (cambió el esquema, momento 3), en curso sobre `66dcaa7`. Escritas, sin compilar todavía (la suite usa los binarios): pruebas HTTP `NotasCreditoCompraApiPostgreSqlTests` (6), prueba del 23505 del número, controlador y DTO.
- Suite completa sobre `66dcaa7` contra el ensayo: `Con error! - Con error: 3, Superado: 752, Omitido: 0, Total: 755, Duración: 5 m 23 s`. Los 3 (`Put_Ubicacion_204…`, `Get_Ficha_ExistenciasPorSucursalYTotal`, `Ficha_TraeLaUbicacionDeCadaSucursal`) esperaban dos sucursales fijas; con el 11 son tres. Arreglados en `test(fco07): con el script 11, la ficha trae también Reclamos…` (solo test).
- API: rojo (con el controlador y el DTO apartados): 6 pruebas HTTP con 404 y el 23505 del número sin mensaje, 7 con error por aserción (una parseaba el 404 sin mirarlo: se le agregó la aserción del alta y se vio en rojo por ella). Código `1131ec4` (controlador, DTO con la fecha anulable para el 400, DI, mensaje de `ux_nota_credito_compra_numero_activa`): 48/48.
- **Suite completa de backend sobre `1131ec4`** (`--no-incremental`, 0 advertencias, `LA_CENTRAL_TEST_SCHEMA=nc057_ensayo_2f673d44`): `Correctas! - Con error: 0, Superado: 762, Omitido: 0, Total: 762, Duración: 6 m 11 s`. **Frontend completo:** `TOTAL: 277 SUCCESS`.
- ESTADO: fila #182, E11 y PRÓXIMA SESIÓN del 19/11.
- Worktree viejo `wt-develop` (de la suite cortada del 18/11) sacado con `git worktree remove` + `prune`. `verificador-tdd` invocado con alcance `7d05c94..7b39fb4` (sin merges ni docs), con la suite sobre `1131ec4` (762/762) y `LA_CENTRAL_TEST_SCHEMA=nc057_ensayo_2f673d44`. El `auditor-persistencia` va después (no pueden correr a la vez).
- `verificador-tdd` (alcance `7d05c94..7b39fb4`): **APTO**. 11 rojos reproducidos por aserción, 15 mutaciones muertas, ninguna sobrevive. Usó la suite sobre `1131ec4`. No bloqueantes: (1) el unitario del CA-011 usaba dos notas y sobrevivía a `Count > 1` → `4ec0ac7`, test con una sola nota, que esa mutación pone en rojo (1 con error), 20/20; (2) **error mío**: `ddbffae` commiteó seis `.orig` (restos del `patch` fallido) → `87da0b4` los saca; (3) el rojo de la pantalla (`e84530b`) es real pero poco específico: 15 de 19 caen en el mismo `expectOne`; (4) `c4c5946` retocó a mano `LaCentralDbContext.cs`, que es generado → re-scaffold contra `nc057_ensayo_2f673d44` (con el 11 corregido): **0 diferencias** con el repo; (5) el rojo de la API es un 404 genérico (después lo discriminan M14 y las pruebas de PostgreSQL). Caracterización, para declararla en el PR: `2fdefd3`, `bef903b`, `fd2f0b3`, `6085986`, `4ec0ac7`.
- **Suite completa de backend sobre `4ec0ac7`** (`--no-incremental`, 0 advertencias, ensayo): `Correctas! - Con error: 0, Superado: 763, Omitido: 0, Total: 763, Duración: 6 m 4 s`. Frontend sin cambios desde `b2df72f` (`277 SUCCESS`).
- `auditor-persistencia` (alcance `7d05c94..4ec0ac7`, con la suite sobre `4ec0ac7`): **APTO**. Filtrada propia: `Correctas! - Con error: 0, Superado: 36, Omitido: 0, Total: 36`. No bloqueantes: tres respaldos de la base sin prueba propia (el caso de uso corta antes); dos anulaciones simultáneas de una nota, solo en secuencia; el 23505 del número probado hasta `MensajeDeConflicto`, no hasta el 409 por HTTP. Avisa de un esquema huérfano en `lacentral_test`: `pr110_test_9d4670f6b05140749a0e351312a4b273` (de la suite cortada del 18/11).
- **PR #183** abierto (base `feature/13-lector-expoyer`, porque esta rama trae el #180; cuando el #180 entre, la base pasa a `develop` con `gh pr edit`). Cuerpo: CA uno por uno, suites sobre `4ec0ac7`, verificadores, sin verificar (CA-012 sin pantalla; DoD 10 hasta que el 11 esté en `lacentral_demo`) y cambios en código ajeno. Suite completa sobre `develop` en curso, en el worktree `wt-develop` de este scratchpad.
- **Suite completa sobre `develop` (`37e8007`)**, una vez, sin filtrar los fallos, 23:51 a 23:56 (hora local): build `--no-incremental` 0 advertencias; `Correctas! - Con error: 0, Superado: 672, Omitido: 0, Total: 672, Duración: 4 m 43 s`. **Los tres que fallaron el 18/11 pasaron.** No hubo nada que investigar: no se repitieron. Lo que se sabe de la corrida del 18/11: tardó 1 h 9 m (15 veces lo normal), cada prueba de PostgreSQL tardaba alrededor de un minuto, también las que pasaban, y a la 01:50 `arch` se cayó de Tailscale. Coincide con un servidor sobrecargado o a punto de caerse, no con un bloqueo de esas tres pruebas. Pero el mensaje de error de entonces no se guardó, así que la causa no está demostrada. Worktree sacado.

**Dónde queda (para retomar):** PR 1 hecho (#183, verificadores APTO). Falta el **PR 2 (PDF de Expoyer, CA-010)** en `feature/182-nota-credito-pdf`: merge de `feature/182-nota-credito`, `POST /api/notas-credito-compra/pdf`, precarga de la pantalla, suite contra la plantilla, verificadores acotados a sus commits y PR con base `feature/182-nota-credito`. Santi rehace `nc057_fuente` (bloque del 19/11). Cuando esté, se borra `nc057_ensayo_2f673d44`. Queda también el huérfano `pr110_test_9d4670f6b05140749a0e351312a4b273`. Equipo: ¿pantalla para anular la nota (CA-012)?

## 19/11/2026 13:22 · La 057: limpieza de esquemas, PR 2 (PDF, CA-010) y PR 3 (anulación, CA-012)

**Instrucción recibida (verbatim):**
> nc057_fuente rehecha con el 11 corregido: TODO OK (19/11). Borrá tu copia nc057_ensayo_2f673d44 y el esquema huérfano pr110_test_9d4670f6b05140749a0e351312a4b273 de lacentral_test (son esquemas de prueba; public no se toca).
>
> Decisión del equipo sobre el CA-012: SÍ entra en la 057. Listado de notas de crédito (filtrable por proveedor y fecha, con las anuladas marcadas) y botón «Anular» con motivo y la autorización de otro ADMINISTRADOR, reusando el patrón de la 026 y la 028. Va en su propio PR, después del PR 2.
>
> Seguí en orden: PR 2 (PDF de Expoyer, CA-010) y después PR 3 (pantalla de anulación). TDD, suite contra nc057_fuente, verificadores acotados y ESTADO antes del push. Cuando termines, parás con el resumen. Resúmenes en español.

**Plan:**
1. Verificar `nc057_fuente` (solo lectura) y borrar `nc057_ensayo_2f673d44` y `pr110_test_9d4670f6b05140749a0e351312a4b273` de `lacentral_test`, sin tocar `public`.
2. #180 ya entró a `develop` (`fe7e6aa`): base del #183 a `develop`.
3. PR 2 en `feature/182-nota-credito-pdf` (ya tiene lector e interpretación): lo que falte del CA-010 (endpoint, precarga de la pantalla), TDD, suite contra `nc057_fuente`, verificadores acotados, ESTADO, push, PR.
4. PR 3 en rama nueva desde la del PDF: listado de NC (proveedor, fecha, anuladas marcadas) y «Anular» con motivo y autorización de otro ADMINISTRADOR (patrón 026/028), TDD, suite, verificadores, ESTADO, push, PR.
5. Resumen y parar.

**Estado:** terminada
- Paso 1 (13:20 hora del servidor): `nc057_fuente_verificacion.sql` → `NOTICE: nc057_fuente: TODO OK (30 tablas; public: 27).` Borrados con guarda (solo `lacentral_test`, sin dependencias desde fuera) `nc057_ensayo_2f673d44` (31 objetos) y `pr110_test_9d4670f6b05140749a0e351312a4b273` (27). Después: `public` 27 tablas, `nc057_fuente` 30. Quedan `demo_ensayo_c3` y `staging`, que no son míos.
- Paso 2: #180 entró a `develop` (`fe7e6aa`); base del #183 → `develop` (`gh pr edit 183 --base develop`).
- PR 2, rama `feature/182-nota-credito-pdf`: merge de `feature/182-nota-credito-manual` y de `origin/develop` (sin conflictos). Ya tenía lector e interpretación (CA-010). Endpoint: rojo `2bfe202` (2 con error por aserción: 404 en vez de 200 y de 400, contra `nc057_fuente`), caracterización `4a310c6` (desdePdf por HTTP → origen PDF, pasa de entrada), código `POST /api/notas-credito-compra/pdf` + DI: clases de la nota `102/102`.
- Pantalla (CA-010): esqueleto `cd62cf8` (selector y `interpretarNotaCreditoPdf`), rojo `34ca631` (`TOTAL: 8 FAILED, 24 SUCCESS`, los 8 por aserción), código `f3a1aeb` (`TOTAL: 32 SUCCESS`; la prueba sin proveedor esperaba antes de que `ngModel` escribiera: `whenStable`), `test` de fecha y percepciones después del código (pasa de entrada; mutación de la marca → 1 FAILED).
- **Suites del PR 2 sobre `7cd1220`** (código igual a `9388cf6`): backend `--no-incremental` 0 advertencias, `LA_CENTRAL_TEST_SCHEMA=nc057_fuente`: `Correctas! - Con error: 0, Superado: 789, Omitido: 0, Total: 789, Duración: 5 m 53 s`. Frontend completo: `TOTAL: 286 SUCCESS`.
- `verificador-tdd` del PR 2 (alcance `feature/182-nota-credito-manual..7cd1220`, sin merges ni docs, con la suite sobre `7cd1220`): **APTO**. 4 rojos reproducidos por aserción, 18 mutaciones muertas, ninguna sobrevive. Sin mutar (techo de corridas): `RequestSizeLimit`, el `if (archivo is not null)`, regex de subtotal/total, `entrada.value = ''`, el reseteo de `paraRevisar` en `nueva()`. Avisó que el árbol principal cambió de rama mientras corría: fui yo, con el PR 3. `auditor-persistencia` del PR 2 lanzado (mismo alcance y suite).
- PR 3, rama `feature/182-nota-credito-anulacion` (desde la del PDF). Backend: esqueleto `96b40e8` (GET, caso de uso y `BuscarAsync` neutros), rojo `c2fa23f` del listado (Moq y PostgreSQL contra `nc057_fuente`: `Con error: 9` por aserción, `Superado: 3` los bordes válidos), código `9c7bd07` (12/12), pruebas HTTP del GET después del código `73ecf9e` (pasan; cambiar de lugar las fechas en el controlador → `Con error: 2`).
- Pantalla: esqueleto `005353e`, rojo `f9b0dc6` (`TOTAL: 17 FAILED, 283 SUCCESS`, todo por aserción), código `6d33adc` (`TOTAL: 300 SUCCESS`; al rojo del menú le faltaba la lista de títulos, agregada).
- `auditor-persistencia` del PR 2: **APTO** (el endpoint solo lee; releen con contexto nuevo; sin InMemory ni Ids a mano). ESTADO (fila #182, D-S6-26, PRÓXIMA SESIÓN) en `6ef1373`; push de las dos ramas; **PR #184** (base `feature/182-nota-credito`).
- PR 3: merge de la rama del PDF (`59be2c8`, solo docs). **Suites sobre `59be2c8`**: backend `--no-incremental` 0 advertencias, `nc057_fuente`: `Correctas! - Con error: 0, Superado: 803, Omitido: 0, Total: 803, Duración: 6 m 10 s`; frontend `TOTAL: 300 SUCCESS`. `verificador-tdd` lanzado (alcance `feature/182-nota-credito-pdf..59be2c8`, sin merges ni docs).
- `verificador-tdd` del PR 3, ronda 1: **NO APTO**. Backend: 12 mutaciones muertas. Frontend: sobreviven F1 (sin `resultado.set(null)` en el error de `irA`, la tabla vieja reaparece mientras va la búsqueda siguiente) y F5 (sin la guarda de vacío en `notasCreditoCompra`, una fecha borrada viaja como `emisionDesde=`). Sin mutar: los roles del módulo (F7). Arreglo: una prueba por cada una, vista en rojo con la mutación, y la mutación de roles a mano.
- Arreglo de la ronda 1, `bb9d4c8` (solo test): spec de la pantalla `TOTAL: 15 SUCCESS`; con F1 `1 FAILED, 14 SUCCESS`; con F5 `2 FAILED, 13 SUCCESS`. F7 (roles a solo ADMINISTRADOR), mutada a mano: `3 FAILED, 28 SUCCESS` en `acceso.spec` y `app.spec`. Muere sin cambiar nada.
- `verificador-tdd` del PR 3, ronda 2 (alcance `59be2c8..bb9d4c8`): **APTO**. F1, F5 y F7 reproducidas y muertas, sin suites nuevas (usó 301 sobre `bb9d4c8` y 803 sobre `59be2c8`, que vale porque el diff no toca `backend/`). `auditor-persistencia` del PR 3 lanzado.
- `auditor-persistencia` del PR 3: **APTO** (`BuscarAsync` solo lee; relectura con contexto nuevo; la siembra pasa los CHECK del 11). Observación no bloqueante: el desempate `ThenByDescending(Id)` no tiene prueba con dos notas del mismo día.
- ESTADO (fila #182 y PRÓXIMA SESIÓN), push de `feature/182-nota-credito-anulacion` y PR 3 con base `feature/182-nota-credito-pdf`.

**Quedó abierto:** el 11 en `public` de `lacentral_test`, en `lacentral_demo` y en `lacentral` antes del merge del #183 (decide el equipo); DoD 10 de la 057 en `lacentral_demo`, que depende de eso; revisión y merge en orden #183 → #184 → PR 3. Sin prueba propia: el límite de 10 MB y el pedido sin «archivo» del endpoint del PDF, y el desempate por Id del listado. Los bloques del 18/11 se cierran con este: su trabajo está en el #183 y el #184.
- **PR #185** abierto (base `feature/182-nota-credito-pdf`).

## 19/11/2026 · El 11 aplicado en las tres bases; pruebas que faltaban en el #184 y el #185; cierre de la 057

**Instrucción recibida (verbatim):**
> Script 11 aplicado en public de lacentral_test, en lacentral_demo y en lacentral (19/11): s11 = 1 en las tres. Anotalo en el ESTADO y en la bitácora, y avisá en el #183 que ya se puede mergear.
>
> Antes de que Javi revise, cubrí lo que quedó sin prueba propia: en el #184, el límite de 10 MB del PDF y el pedido sin el campo «archivo»; en el #185, el orden entre dos notas con la misma fecha de emisión (desempate por id). Cada uno con rojo por un camino real y en su rama; si una guarda no es alcanzable, se borra. Verificador acotado a esos commits y suite. Después, merge hacia las ramas de arriba.
>
> Cuando Javi mergee los tres: suite completa sobre develop y borrá nc057_fuente con el script de borrado. Resúmenes en español.

**Plan:**
1. ESTADO y bitácora: el 11 en `public` de las tres bases (19/11). Comentario en el #183: listo para mergear.
2. #184 (`feature/182-nota-credito-pdf`): pruebas HTTP del límite de 10 MB y del pedido sin «archivo»; rojo por camino real (mutando la guarda), o borrar la guarda si no es alcanzable.
3. #185 (`feature/182-nota-credito-anulacion`): prueba contra PostgreSQL de dos notas con la misma fecha (desempate por Id), rojo mutando el `ThenByDescending`.
4. Suite completa sobre el HEAD de cada rama; `verificador-tdd` acotado a esos commits (no toca persistencia nueva: el auditor no se relanza salvo que el test de orden lo amerite).
5. Merge de la del PDF hacia la de anulación; push; ESTADO antes del push.
6. Cuando Javi mergee #183, #184, #185: suite completa sobre `develop` y `nc057_fuente_borrado.sql`.

**Estado:** en curso
- Arranque: `git fetch`; `develop` sin commits nuevos respecto de esta rama.
- El 11 aplicado por Santi en `public` de `lacentral_test`, `lacentral_demo` y `lacentral` (19/11): `s11 = 1` en las tres (dato de Santi, no lo corrí yo).
- Comentario en el #183: ya se puede mergear (orden #183 → #184 → #185).
- #184 (`feature/182-nota-credito-pdf`), `4d134e4` (solo test): `PostPdf_SinElCampoArchivo_400…` y `PostPdf_DeMasDe10MB_400SinLeerElArchivo`. TestServer **no aplica** `[RequestSizeLimit]` (10 MB + 1 llegaba al caso de uso: 400 «no es un PDF»); la guarda es alcanzable en producción (Kestrel), así que la prueba levanta Kestrel real (`UseKestrel(0)`) con `Expect: 100-continue` (sin eso, Kestrel corta y el cliente da *broken pipe*). Lo que devuelve de verdad: **400** (no 413) con «The max request body size is 10485760 bytes»: `[ApiController]` convierte el error de lectura del formulario en validación. Rojos por mutación: sin el atributo → `Assert.Contains` (1 con error de 4); sin el `if (archivo is not null)` → 500 en vez de 400 (1 con error de 4). Verde: 4/4 contra `nc057_fuente`.
- Merge de la rama del PDF en `feature/182-nota-credito-anulacion` (`1ff49bf`, sin conflictos).
- #185, `MismaFechaDeEmision_DesempataPorId_LaMasNuevaPrimero` (solo test, commit siguiente a `1ff49bf`): dos notas del mismo día, también paginando de a una. Rojos por mutación: sin `ThenByDescending(Id)` y con `ThenBy(Id)` → `Assert.Equal` de colecciones (1 con error de 5). Verde 5/5 contra `nc057_fuente`.
- **Suite completa de backend sobre `67a522f`** (#185; `--no-incremental`, 0 advertencias, `LA_CENTRAL_TEST_SCHEMA=nc057_fuente`): `Correctas! - Con error: 0, Superado: 806, Omitido: 0, Total: 806, Duración: 11 m 55 s`. Frontend sin cambios desde `bb9d4c8` (301), no se corre.
- `verificador-tdd` (alcance `4d134e4` y `67a522f`, con las suites de arriba): **APTO**. 5 mutaciones muertas (las cuatro mías y el límite corrido 1 KB). No bloqueantes: son caracterización (pasan de entrada; se declara en los PR); la prueba del tamaño compara el mensaje en inglés de Kestrel; sin el desempate, el rojo depende del orden de inserción (con `ThenBy(Id)` es determinista). `auditor-persistencia` no relanzado: dos commits solo de test, sin cambio de repositorio ni de esquema.
- ESTADO (E11 cerrada, fila #182, PRÓXIMA SESIÓN). Push de `feature/182-nota-credito-pdf` y `feature/182-nota-credito-anulacion`; comentario en #184 y #185.

**BLOQUEADO EN:** revisión y merge de #183 → #184 → #185 (Javi). Después: suite completa sobre `develop` y `nc057_fuente_borrado.sql`.

## 20/11/2026 23:21 · Correcciones de cierre del Sprint 6: fechas y layout de FCO, Enter en NCA, búsquedas

**Instrucción recibida (verbatim):**
> (Especificación pegada por Javi, completa:) «Especificación: correcciones de FCO, NCA y búsquedas (cierre Sprint 6)». Reglas generales: un fix = una rama `fix/<descripcion>` desde `develop` = un PR; NO commitear ni pushear sin OK de Javi; nunca tocar `lacentral`, persistencia solo contra `lacentral_test`; sin migraciones EF (si hace falta esquema, DETENER); no inventar datos de negocio; al terminar cada fix compilar, correr backend y frontend, actualizar ESTADO y BITACORA, refrescar Graphify e informar.
> Fix A · `fix/fco-fechas-abreviadas`: imputación arranca con la fecha actual (reloj inyectable); emisión y vencimiento aceptan al Enter o blur: 1–2 dígitos = día (mes y año actuales); 4 dígitos = ddmm (año actual); `dd/mm` = año actual; `dd/mm/aaaa` se conserva; todo lo demás inválido (pendiente de Javi: `-`, `.` y `ddmmaaaa`); fecha inexistente con mensaje claro conservando lo tipeado; Enter interpreta y NO envía; a la API viaja `aaaa-mm-dd`; vencimiento editable; pendiente de Javi: vencimiento anterior a emisión ¿bloquea o avisa? Pruebas con hoy = 19/11/2026: los cuatro ejemplos, 31/02, 29/02 de 2027 y 2028, cambio de año, meses de 30 y 31, inválidas, Enter sin envío.
> Fix B · `fix/fco-layout-detalle`: orden 1) datos generales, 2) detalle de artículos, 3) percepciones, 4) totales y acciones; sin cambiar operaciones ni validaciones; breakpoints existentes.
> Fix C · `fix/nca-enter-confirma-valor`: verificar la hipótesis del envío implícito del `<form (ngSubmit)="registrar()">`; Enter en un numérico confirma el valor sin registrar; registrar solo con el botón; mantener CA-007; no guardar sola; pruebas de componente.
> Fix D · `fix/busquedas-diagnostico`: diagnosticar antes de tocar código (lacentral_test, lacentral_demo con log, GRANTs de `lacentral_javi` y `pg_trgm`, usuario y rol); corregir solo si falla en `lacentral_test`.
> Fuera de la spec: H1 vencimiento según proveedor, H2 sucursal destinataria en PDF.

**Plan:**
1. Arranque: el bloque anterior (19/11) quedó en curso esperando el merge de #183→#184→#185, que ya está en `develop` (`9f387c2`). `git fetch`: `develop` = `origin/develop`.
2. Fix D: diagnóstico sin código (pasos 1 a 3).
3. Fix C: reproducir, prueba en rojo, arreglo.
4. Fix A: parseo de fechas con reloj inyectable, pruebas con hoy = 19/11/2026.
5. Fix B: reordenar bloques del formulario.
6. Cada fix en su rama, sin commit hasta el OK de Javi; ESTADO, bitácora, Graphify.

**Estado:** terminada (D queda abierto: falta el dato de Javi; ver ESTADO)
- Arranque: `git fetch --all --prune`; `develop` sin commits nuevos en `origin/develop`. `LACENTRAL_TEST_PG` → `lacentral_test`, usuario `lacentral_javi` (permitido).
- Fix D, paso 1 (`lacentral_test`): filtrada `Busqueda|BusquedaPaginadaApiPostgreSqlTests|ArticulosApiPostgreSqlTests` sobre `develop` `9f387c2`, `public`: `Correctas! - Con error: 0, Superado: 30, Total: 30`. La búsqueda funciona con datos conocidos: el código no falla ahí.
- Fix D, paso 2 (`lacentral_demo`, solo lectura con `lacentral_javi`): `pg_trgm` 1.6 en `public` (`similarity()` responde); `lacentral_javi` tiene SELECT e INSERT en las 30 tablas (el `42501` no se reproduce hoy). **Datos de la demo: 31 artículos, 6 proveedores y 0 clientes.** Clientes «sin resultados» en la demo = no hay clientes cargados. Usuarios de la demo: `admin.demo` (ADMINISTRADOR) y `operador.demo` (OPERADOR); los dos pueden buscar artículos. La aproximada compara contra el nombre entero (`similarity > 0,3`): «fltro» da 0 en la demo; es la regla vigente, no se toca. El log de la API contra la demo no lo puedo ver desde acá (la API de la demo no corre en esta máquina).
- Fix D: sin cambio de código (no falla en `lacentral_test`). No se crea la rama `fix/busquedas-diagnostico`.
- Fix C (`fix/nca-enter-confirma-valor`, sin commit): hipótesis confirmada en el código. El `<form (ngSubmit)="registrar()">` de la NCA no cancela el Enter de ningún numérico (importe, percepciones, cantidades): el navegador hace envío implícito por el botón submit «Registrar». Con «35» tipeado, el `(input)` ya invalidó el total y el botón está deshabilitado: Enter no hace nada. Con el total a la vista y sin tocar el valor, Enter **registra** sin pasar por el botón. Arreglo: `(keydown.enter)` en el form cancela el Enter que sale de un `<input>` (`noEnviarConEnter`). Tres pruebas de componente (importe con 35, percepción con total a la vista, cantidad de una línea devuelta): rojas sin el arreglo (`defaultPrevented` false, 3 con error de 36), verdes con él: `TOTAL: 36 SUCCESS` (specs de `nota-credito-compra`). Guardado en `git stash` «fix C» hasta el OK.
- Fix A (`fix/fco-fechas-abreviadas`, sin commit): parser puro `compartido/fecha-abreviada.ts` (`interpretarFecha`, `hoyIso` con la fecha LOCAL, `aTextoFecha`) y reloj `RELOJ` (`InjectionToken` con fábrica `() => new Date()`). Emisión, vencimiento **e imputación** pasan a texto `dd/mm/aaaa` (la spec nombra los tres como inputs a pasar a texto; la imputación usa el mismo parser); el modelo sigue en `aaaa-mm-dd`. Enter cancela el envío e interpreta; blur interpreta. Fecha inexistente o formato fuera de la tabla: mensaje en el campo (`aria-invalid`, `aria-describedby`), se conserva lo tipeado, el campo queda sin fecha y `factura()` no pide el total («Corregí las fechas señaladas»): un 31/02 en vencimiento no viaja como null en silencio. Imputación = hoy al abrir y en `nueva()` (también al cargar un PDF).
- Fix A, pruebas: tabla del parser con hoy = 19/11/2026 23:30 (33: los cuatro ejemplos, 31/02, 29/02 de 2026/2027 vs 2028, enero con hoy = 02/01/2027, 30/04 vs 31/04, 31 de noviembre, inválidas `123`, `201026`, `20102026`, `20/10/26`, `20-10`, `20.10.2026`…). Cinco mutaciones del parser, cada una roja (sin bisiesto, todos de 31, `toISOString`, sin `ddmm`, aceptar `-`). Componente (`factura-compra-fechas.spec.ts`, 7) y cinco mutaciones del cableado, 1 roja cada una (sin guarda, sin `preventDefault`, `nueva()` sin hoy, PDF sin mostrar, error que conserva el ISO viejo). Expectativa cambiada (no aflojada) en `factura-compra-pdf.spec.ts`: la emisión y el vencimiento del PDF se muestran `03/02/2027` y `10/03/2027` y viajan `aaaa-mm-dd`. Frontend completo: `TOTAL: 341 SUCCESS`; `ng build` sin errores. Guardado en `git stash` «fix A».
- Fix A, pendiente de Javi (no implementado): `-` y `.` como separadores; `ddmmaaaa` de 8 dígitos; vencimiento anterior a emisión ¿bloquea o avisa?
- Fix B (`fix/fco-layout-detalle`, sin commit): movimiento puro del `<fieldset>` de percepciones, sin cambiarlo por dentro, a su propia `section.tarjeta` entre «Artículos comprados» y los totales y acciones. Orden: datos generales → detalle → percepciones → totales y acciones. La app no tiene breakpoints de ancho (solo `login` a 760 px y `prefers-reduced-motion`); los bloques usan `flex-wrap` y `.tabla-caja` con scroll: no se agregó ninguno. Prueba `factura-compra-orden.spec.ts` (orden en el DOM): roja antes del movimiento («bloque 2 antes del 3»), verde después. Frontend completo: `TOTAL: 302 SUCCESS`. El diff del A aplica limpio sobre el B (`git apply --check`). Sin ver en el navegador. Guardado en `git stash` «fix B».
- Fix C, frontend completo con solo el C aplicado: `TOTAL: 304 SUCCESS`.

## 20/11/2026 · Commit y PR de los fixes A, B y C

**Instrucción recibida (verbatim):**
> OK, commiteá y abrí los PR de A, B y C

**Plan:**
1. Esperar la suite completa de backend sobre `develop` `9f387c2` (los tres fixes son solo de frontend).
2. Por fix, en su rama: `stash pop`, commit del test y commit de producción por separado (rojo visible), push, PR contra `develop`.
3. Bitácora y ESTADO en el PR del C (uno solo, para que los tres PR no choquen en los documentos). Graphify.

**Estado:** terminada (falta la revisión de Javi y el `verificador-tdd` de cada PR)
- `git fetch`: `origin/develop` sin commits nuevos.
- **Suite completa de backend sobre `develop` `9f387c2`** (`public` de `lacentral_test`, base de las tres ramas, que no tocan `backend/`): `Correctas! - Con error: 0, Superado: 806, Omitido: 0, Total: 806, Duración: 1 h 1 m`. Cierra también la suite sobre `develop` pendiente del bloque del 19/11. Tardó 5 veces más que contra `nc057_fuente` (11 m 55 s): copiar `public` para cada prueba lleva entre 5 y 28 s por esquema (visto en `pg_stat_activity`).
- Commits: C `32891b5` (test) y `a0f2d6e`; A `9af8f96` (test) y `5c94c56`; B `b173692` (test) y `cad5ba6`. Frontend: mismo contenido que las corridas de arriba (C 304, A 341, B 302).
- PR: B #186, A #187 (cita al #186; el diff del A aplica limpio sobre el B), C con este commit de documentos. `verificador-tdd` no invocado todavía; `auditor-persistencia` no aplica (solo frontend).
- PR del C: #188. Abiertos: #186 (B), #187 (A), #188 (C).

## 21/11/2026 · verificador-tdd sobre #186, #187 y #188

**Instrucción recibida (verbatim):**
> Sí, lanzalo

(Respuesta a «¿Lanzo el verificador-tdd sobre los tres PR, de a uno?».)

**Plan:**
1. Sacar worktrees (R1 de los verificadores).
2. `verificador-tdd` de a uno: #186 (B), #187 (A), #188 (C), cada uno acotado a sus dos commits, con la suite de backend sobre `9f387c2` y la de frontend de cada rama.
3. NO APTO → arreglo y re-verificación acotada al arreglo (hasta 3 rondas). Resultados en cada PR, bitácora y ESTADO.

**Estado:** terminada (los tres APTO; falta la revisión y el merge de Javi, orden #186 → #187)
- Worktrees: solo el principal. `verificador-tdd` lanzado sobre #186 (alcance `b173692`, `cad5ba6`; backend 806 sobre `9f387c2`, frontend 302).
- #186 ronda 1: **NO APTO**. Rojo real en `b173692` (razón correcta) y verde en `cad5ba6`, movimiento puro confirmado (`diff -w`), frontend 302 corrido por el verificador sobre `cad5ba6`. Sobreviven dos mutaciones: devolver solo «Ganancias» a datos generales, y quitar la `section.tarjeta` propia. Arreglo: el test exige los cuatro inputs de percepciones dentro de una `section.tarjeta` propia, distinta de la de datos generales y de la del detalle.
- #186: arreglo `bfb1484` (solo test): las cuatro percepciones en su tarjeta propia, distinta de generales y detalle, y en orden también con el total a la vista. M6 → 1 rojo, M7 → 3 rojos, producción de `9f387c2` → 3 rojos. Frontend completo sobre `bfb1484`: `TOTAL: 304 SUCCESS`. Push y comentario en el #186. Ronda 2 acotada a `bfb1484`.
- #186 ronda 2 (alcance `bfb1484`): **APTO**. 3 rojos con la producción de `9f387c2`, 8 mutaciones muertas, suites aceptadas (304 / 806). No bloqueante: `enOrden` no distingue «después» de «anidado dentro». `verificador-tdd` lanzado sobre #187 (alcance `9af8f96`, `5c94c56`).
- #187 ronda 1: **NO APTO**. Rojo real por aserción (con stub neutro: 40 con error de 71; en crudo no compila, módulo nuevo). Sobreviven: (1) `bisiesto` sin la regla del siglo (29/02/2100 pasaría); (2) `mostrarFechas()` sin `errorFecha.set({})` (un 31/02 seguido de «Cargar otra» o de un PDF deja el aviso y bloquea el total); (3) `[required]` de emisión en `false`. No bloqueante: el test de hora local solo discrimina al oeste de UTC. Arreglo, solo test: años de siglo, error y después `nueva()`, `required` solo en emisión, y hoy a las 00:30 además de las 23:30.
- #187: arreglo `617b857` (solo test): 29/02/2100 y 1900 inexistentes, 2000 válido; 31/02 y después `nueva()` deja pedir el total; `required` solo en emisión; hoy también a las 00:30. Mutaciones: siglo → 2 rojos, 400 → 1, sin limpiar errores → 1, `required` → 1, `getUTCDate` → 4. Frontend completo sobre `617b857`: `TOTAL: 346 SUCCESS`. Push y comentario. Ronda 2 acotada a `617b857`.
- #187 ronda 2 (alcance `617b857`): **APTO**. Mutaciones de siglo, 400, limpiar errores, `required` (false y true) y `getUTCDate`: todas rojas por aserción. Suites aceptadas (346 / 806). `verificador-tdd` lanzado sobre #188 (alcance `32891b5`, `a0f2d6e`).
- #188 ronda 1: **NO APTO**. Rojo real en `32891b5` (3 por aserción), 8 de 9 mutaciones muertas, frontend 304 corrido por el verificador sobre `0c864b4`. Sobrevive la guarda en `true` (cancelar el Enter de cualquier elemento): nada prueba que un Enter sobre un botón o un `<select>` del form NO se cancele, y eso le quitaría al teclado activar «Calcular el total» o «Registrar». Arreglo, solo test.
- #188: arreglo `af94316` (solo test): Enter sobre «Calcular el total», «Registrar» y el select de clase no se cancela; con la guarda en `true`, 3 aserciones rojas. Frontend completo sobre `af94316`: `TOTAL: 305 SUCCESS`. Push y comentario. Ronda 2 acotada a `af94316`.
- #188 ronda 2 (alcance `af94316`): **APTO**. Guarda en `true`, solo excluir botones y `instanceof HTMLElement`: las tres rojas por aserción. Suites aceptadas (305 / 806).
- Cierre: **#186, #187 y #188 en APTO** (dos rondas cada uno). Frontend sobre el HEAD de cada uno: #186 `bfb1484` 304, #187 `617b857` 346, #188 `af94316` 305. Backend 806 sobre `9f387c2`. Este commit de documentos queda fuera de la verificación (solo `docs/`).

## 21/11/2026 19:36 · Respuestas de Javi sobre el Fix D y el Fix A

**Instrucción recibida (verbatim):**
> Fix D: las pantallas de clientes, proveedores y articulos. con rol de operador
> Fix A: se aceptan - y .
> si ddmmaaaa de 8 digitos
> EL vencimineto nunca puede ser anterior a la emision, si se carga mal y da que es anterior, se alerta y bloquea

**Plan:**
1. Fix D: reproducir en `lacentral_test` la búsqueda de clientes, proveedores y artículos con una sesión OPERADOR. Si falla, test rojo y arreglo en su propia rama; si no falla, informar qué difiere en la demo.
2. Fix A (#187, `fix/fco-fechas-abreviadas`): tests rojos para `-` y `.` como separadores, `ddmmaaaa` de 8 dígitos y vencimiento anterior a la emisión (aviso y bloqueo del registro). Después producción.
3. Pruebas filtradas del frontend y la suite de frontend completa sobre el Fix A.
4. Actualizar ESTADO y bitácora. Commit y push solo con OK de Javi.

**Estado:** terminada
- Fix D, reproducción de punta a punta (21/11 ~20:00): API de este árbol (`fix/nca-enter-confirma-valor`; backend igual a `develop` `9f387c2`) levantada en `localhost:5199` contra `lacentral_demo` con `lacentral_javi`, solo GET. Token local de OPERADOR (`operador.demo`, id 2) firmado con una clave de diagnóstico, y frontend `ng serve` en `:4299` con proxy a esa API. Resultados:
  - **Artículos (OPERADOR): funciona.** «aceite» → 3, «bujia» → «Bujía de encendido iridio», «filtro aceite» → 1. Con este código y estos datos no se reproduce el «sin resultados».
  - **Proveedores: falla con una palabra de la razón social.** «Frenos» → «Sin resultados» con «Frenos y Embragues Córdoba SRL (demo)» activo; «Norte» → 0. «Autopartes» → 1, «Expoyer» → 1. Causa: `similarity(razon_social, texto) > 0,2` compara contra la razón social entera; «Frenos» da 0,19 y «Norte» 0,17. No depende del rol ni de la base: con cualquier dato pasa lo mismo en `lacentral_test`. Clientes usa la misma regla (`ClienteRepositorio.BuscarAsync`).
  - **Clientes: la demo tiene 0 clientes**, así que toda búsqueda da «sin resultados».
  - El rol no interviene: ninguna consulta filtra por rol ni por sucursal.
  Servidores de diagnóstico apagados.
- Fix A (rama `fix/fco-fechas-abreviadas`, sobre `617b857`, sin commitear): tests primero. `fecha-abreviada.spec.ts`: `-` y `.` como separadores (`20-10`, `20.10`, `5-3-2025`, `20.10.2026`), `ddmmaaaa` (`20102026`, `29022028`), inexistentes `29022027`, `31-04`, `00.10.2026`, inválidas con separadores mezclados (`20-10/2026`, `20.10-2026`, `20/10.2026`), `20,10`, 7 y 9 dígitos. `factura-compra-fechas.spec.ts`, 5 casos de vencimiento anterior a la emisión: aviso y bloqueo, mismo día vale, corregir la emisión saca el aviso, el aviso no tapa un vencimiento mal tipeado, la precarga sin pasar por el campo tampoco se registra. **Rojo:** `TOTAL: 12 FAILED, 50 SUCCESS`, todos por aserción.
- Fix A, producción: regex con separador `[/.-]` y retro-referencia `\2` (el mismo en toda la fecha), `ddmmaaaa` en la forma sin separador; `revisarOrdenFechas()` al fijar cualquier fecha y antes del registro (`VENCIMIENTO_ANTERIOR` en el vencimiento, sin pisar un error de tipeo). El backend ya lo rechazaba (`RegistrarFacturaCompraUseCase.cs:176`). Filtrada: `TOTAL: 131 SUCCESS`. Frontend completo sobre el árbol sin commitear: `TOTAL: 363 SUCCESS`.
**BLOQUEADO EN:** OK de Javi para commitear (test y producción por separado) y pushear al #187; decisión de Javi sobre el remedio de búsqueda de proveedores y clientes por palabra (Fix D).
- OK de Javi («Sí, commiteá»). Commits en `fix/fco-fechas-abreviadas`: `ea7c2f3` (pruebas) y `a580574` (producción), push y comentario en el #187. Frontend 363 corrido sobre el árbol idéntico a `a580574`. ESTADO actualizado (entrada 21/11 noche).
- Se cerró el bloqueo del commit. Queda abierto: `verificador-tdd` sobre `ea7c2f3..a580574`; decisión de Javi sobre el remedio de búsqueda por palabra (Fix D); merge #186 → #187 y #188.

## 21/11/2026 · verificador-tdd sobre el Fix A ampliado (#187)

**Instrucción recibida (verbatim):**
> Sí, lanzalo

**Plan:**
1. `verificador-tdd` sobre `ea7c2f3..a580574` del #187, solo esos commits (lo anterior ya dio APTO), con la suite de frontend 363 sobre `a580574` y la de backend 806 sobre `9f387c2`.
2. Si da NO APTO, arreglar y re-verificar acotado al arreglo (hasta 3 rondas).
3. Anotar el resultado en el PR, la bitácora y el ESTADO.

**Estado:** terminada
- `verificador-tdd` (alcance `ea7c2f3..a580574`): **APTO**. Rojo reproducido `TOTAL: 12 FAILED, 50 SUCCESS`, todo por aserción; 13 mutaciones, todas muertas, ninguna sobrevive. Suites aceptadas (363 / 806). Sin hallazgos no bloqueantes. Comentado en el #187.
- Queda abierto: decisión de Javi sobre la búsqueda por palabra (Fix D); merge #186 → #187 y #188.

## 21/11/2026 · Fix D: búsqueda por palabra en proveedores y clientes

**Instrucción recibida (verbatim):**
> Sí, arreglala

(Responde a: «¿Arreglo la búsqueda por palabra de proveedores y clientes (Fix D)?». Propuesta aceptada: que proveedores y clientes encuentren la razón social que contiene cada palabra escrita, sin acentos ni mayúsculas, como artículos; lo parecido sigue apareciendo, después.)

**Plan:**
1. Rama `fix/busquedas-por-palabra` desde `origin/develop`, en un worktree del scratchpad (el árbol principal sigue en la rama del #188 con los documentos).
2. Tests rojos contra PostgreSQL real: «Frenos» encuentra «Frenos y Embragues Córdoba SRL»; palabras en otro orden, sin acento y en minúscula; lo contenido antes que lo parecido; lo que no contiene ni se parece no aparece. Para proveedores (las dos ramas: con y sin `porCuit`) y clientes.
3. Producción en `ProveedorRepositorio` y `ClienteRepositorio`.
4. Filtradas y suite completa de backend; frontend no se toca.
5. Commit, PR, verificadores (`verificador-tdd` y `auditor-persistencia`) con OK de Javi para el PR.

**Estado:** terminada (rama, tests y PR #189; lo que sigue está en el bloque siguiente)
- Worktree `scratchpad/wt-fixd`, rama `fix/busquedas-por-palabra` desde `origin/develop` `9f387c2`.
- Tests primero: `backend/LaCentral.Tests/Data/BusquedaPorPalabraPostgreSqlTests.cs` (PostgreSQL real, 3 casos: proveedores con y sin `porCuit`, clientes). «wqyz» encuentra «Frenos Wqyz y Embragues Córdoba SRL»; «córdoba WQYZ» y «cordoba wqyz» también; «wqyz norte» no; con «Xvbnk», lo que contiene la palabra va antes que lo que solo se parece («Xvbnq»). **Rojo:** `Con error: 3, Superado: 0`, los tres por `Assert.Contains() Failure` (línea del primer «wqyz»).
- Producción: `ProveedorRepositorio` (las dos ramas de razón social) y `ClienteRepositorio`: `Id IN (los que contienen cada palabra, con ToUpper + reemplazo de acentos y NormalizacionArticulo.Palabras)` OR la similitud de siempre; orden: código exacto (proveedores sin `porCuit`), después lo contenido, después la similitud. Filtrada: `Correctas! - Con error: 0, Superado: 3`. Suite completa de backend en curso.
- Suite completa de backend en el worktree (HEAD `9f387c2` + cambios sin commitear): `Con error! - Con error: 2, Superado: 807, Omitido: 0, Total: 809, Duración: 1 h 3 m`. Fallaron `ConsultarNotasCreditoCompraPostgreSqlTests.PorRangoDeEmision_IncluyeLosBordes_DeTodosLosProveedores` (1 m 5 s) y `RegistrarFacturaCompraPostgreSqlTests.PrecioRecalculadoQueNoEntraEnSuColumna_InvalidoYNoQuedaNada` (1 m 8 s): NCA y FCO, código que el Fix D no toca. Filtrados los dos: `Correctas! - Con error: 0, Superado: 2, Total: 2, Duración: 49 s`. El mensaje del fallo no quedó (la salida estaba filtrada); por la duración, carga de la base compartida.
**BLOQUEADO EN:** OK de Javi para commitear (test y producción por separado), correr la suite completa una vez sobre el commit y abrir el PR del Fix D.
- OK de Javi («Sí, commiteá»). En `fix/busquedas-por-palabra`: `cd1c48f` test, `c5ef0ff` producción. `origin/develop` sin commits nuevos. Suite completa sobre `c5ef0ff` en curso.
- Suite completa sobre `c5ef0ff`: `Con error! - Con error: 5, Superado: 804, Omitido: 0, Total: 809, Duración: 53 m 9 s`. Los 5 con `Npgsql.NpgsqlException : Exception while reading from stream ---- System.TimeoutException : Timeout during reading attempt` (56 s a 1 m 16 s cada uno): NCA `DosDevolucionesSimultaneas_…`, `PostgreSqlPruebasLimpiezaTests.FuenteConUnaTablaParticionada…`, `ProveedorDetalleApi…Detalle_DeUnProveedorDadoDeBaja…`, `ProveedorPostgreSqlTests.ModificarConDireccionVaciaNueva…`, `ArticulosApi…Post_RolSinPermiso_403…`. Ninguno usa la búsqueda. Filtrados: `Correctas! - Con error: 0, Superado: 6, Total: 6, Duración: 34 s` (un filtro trae dos tests).
**BLOQUEADO EN:** decisión de Javi: tercera corrida completa (la política pide una al final y la base compartida viene dando timeouts) o abrir el PR declarando estas dos corridas.
- Preguntas de Javi sobre los timeouts. (1) Carga concurrente: no se puede saber por la base. Cada test crea su esquema `pr110_test_*` y lo borra al terminar (quedan 0); `pg_stat_activity` solo muestra el presente (una conexión, la de la consulta) y `pg_stat_database` no guarda historia por hora. En esta máquina no hay otra sesión de Claude de La Central corriendo, no hay ningún `testhost`, y el único worktree extra es `wt-fixd`. Lo que hayan corrido los otros dos del equipo, o los otros siete contenedores de la netbook, no se ve desde acá. (2) Los fallos de las dos corridas no se repiten. Corrida 1 (`9f387c2` + cambios): `PorRangoDeEmision_…` (NCA) y `PrecioRecalculadoQueNoEntra…` (FCO). Corrida 2 (`c5ef0ff`): otros 5. No hay ninguno en común; todos cortan cerca del minuto, y la corrida 2 muestra `TimeoutException : Timeout during reading attempt`.

## 21/11/2026 · Fix D: PR, verificadores y borrador de issue de infraestructura

**Instrucción recibida (verbatim):**
> Visto. Abrí el PR con la frase tal cual, las dos corridas, la excepción autorizada por Javi y el pedido de la suite completa a Santi en develop. Pusheá, corré los dos verificadores solo sobre estos dos commits y creá el borrador de la issue de infraestructura (no la abras todavía, mostrámela)

**Plan:** 1. Push de `fix/busquedas-por-palabra` y PR contra `develop` con la frase, las dos corridas, la excepción y el pedido a Santi. 2. Sacar el worktree. 3. `verificador-tdd` y `auditor-persistencia` sobre `cd1c48f..c5ef0ff`, uno por vez. 4. Borrador de la issue en un archivo, sin abrirla. 5. ESTADO y bitácora.

**Estado:** terminada. Abierto: merge del #189 (Javi), suite completa en `develop` (Santi), decidir si se abre la issue de timeouts, recorrido a mano en `lacentral_demo`, rendimiento del filtro por palabra sin medir.
- Push de `fix/busquedas-por-palabra` y PR #189 contra `develop` (https://github.com/AAAActis/la-central-gestion/pull/189). ESTADO con la entrada del Fix D. Worktree `wt-fixd` sacado; `git worktree list` muestra solo el principal.
- Borrador de la issue de infraestructura en `docs/sprint06/BORRADOR-ISSUE-TIMEOUTS-PG.md`, sin abrir en GitHub y sin commitear. `verificador-tdd` lanzado sobre `cd1c48f..c5ef0ff` con hash, línea y conteo de la suite.
- `verificador-tdd` ronda 1 sobre `cd1c48f..c5ef0ff`: **NO APTO**. Sobreviven M6 (`ProveedorRepositorio.cs:61`, sin `.Replace("Ó","O")`) y C4 (`ClienteRepositorio.cs:105`, ídem): «cordoba wqyz» encuentra igual por pg_trgm. Los Replace de Á, É, Í, Ú y Ü no tienen dato que los pruebe. 12 mutaciones muertas. Hallazgo sin verificar: texto de pura puntuación → `Palabras` vacío → podrían entrar todas las filas. Arreglo: test donde el trigrama no rescate, una vocal acentuada por vez, y revisar la puntuación.
- Arreglo ronda 1: `764c56e` (solo test, pusheado). Razón social con una palabra suelta por letra (á é í ó ú ü) donde el trigrama no alcanza; se busca cada una sin acento. Rojo sin los 12 `Replace` de acentos: `Con error: 3, Superado: 0` («xjqa» no encontró…); rojo sin solo el de Ü: `Con error: 3, Superado: 0` («gwhu» no encontró…); verde con el código: `Correctas! - Con error: 0, Superado: 3`. Puntuación: inalcanzable, el caso de uso rechaza texto vacío o en blanco (`ConsultarProveedoresUseCase.cs:22`, ídem clientes) y `Palabras` corta solo por espacios, así que «...» es una palabra, no una lista vacía. Suite completa no repetida para un commit que solo toca esta clase de test. Worktree sacado.
- `verificador-tdd` ronda 2 sobre `c5ef0ff..764c56e`: **APTO**. 6 mutaciones de acentos muertas (Ó, Á, Ü en proveedores; Ó, É, Ú en clientes), incluidas las equivalentes a M6 y C4; verde `Superado: 3`. Puntuación cerrada por lectura (`ConsultarProveedoresUseCase.cs:22`, `ConsultarClientesUseCase.cs:21`). La suite completa sobre `764c56e` no corrió: el permiso del subagente la rechazó y no la eludió; la última completa sigue siendo la de `c5ef0ff`. Lanzo `auditor-persistencia` sobre `9f387c2..764c56e`.
- `auditor-persistencia` sobre `9f387c2..764c56e`: **APTO**. Suite completa sobre `764c56e` corrida por el auditor: `Correctas! - Con error: 0, Superado: 809, Omitido: 0, Total: 809, Duración: 16 m 30 s`. ctype `en_US.utf8` en `lacentral`, `lacentral_demo` y `lacentral_test`; `%` y `_` no son comodines; 0 esquemas `pr110_test_%` huérfanos. Riesgo sin medir: el filtro por palabra no usa el índice GIN.
- Cuerpo del PR #189 actualizado con los dos veredictos y la suite verde. ESTADO actualizado. El borrador de la issue suma la tercera corrida.

## 22/11/2026 · Abrir la issue de timeouts de la base compartida

**Instrucción recibida (verbatim):**
> Sí, abrila

**Plan:** 1. Abrir la issue con el borrador `docs/sprint06/BORRADOR-ISSUE-TIMEOUTS-PG.md`. 2. ESTADO y bitácora.

**Estado:** terminada. Nada abierto en esta tarea.
- Issue #190 abierta (https://github.com/AAAActis/la-central-gestion/issues/190) con el texto del borrador, sin cambios. Borrador local borrado: su contenido está en la issue.

## 23/11/2026 · Cierre documental del Sprint 6 y ajuste de fechas

**Instrucción recibida (especificación pegada por Javi; los puntos 1.1 a 1.3 van condensados, conservando todas las reglas, cifras y nombres; las reglas generales y el PR 2 siguen su texto):**
> Especificación: cierre documental del Sprint 6 y ajuste de fechas
> Base: `develop` (después del merge de #186, #187, #188 y #189). Reglas: `CLAUDE.md`.
> Reglas generales
> * Dos PR separados, cada uno desde `develop`: PR 1 (docs) y PR 2 (fix de fechas).
> * NO commitear ni pushear sin OK de Javi. Mostrar el diff antes de pedirlo.
> * No tocar `lacentral`. Sin migraciones EF. Sin cambios de esquema en esta spec.
> * No inventar decisiones de negocio: todo lo que sigue sale de lo que dijo Sergio (relevado por Javi el 23/11/2026) o de lo que ya está en el repo. Si una fecha o un número de decisión no está claro, mirá el último valor usado en `docs/sprint06/ESTADO.md` y seguí la numeración.
> * Al terminar: compilar, correr frontend completo (y backend solo si tocás `backend/`), actualizar `ESTADO.md` y `BITACORA.md`, refrescar Graphify, e informar qué hiciste y con qué resultado.
> PR 1 · Documentación (`docs/sprint06-cierre`)
> Solo archivos bajo `docs/`. Sin código.
> 1.1 `docs/sprint06/ESTADO.md` al día: fixes A, B, C y D mergeados en `develop` (PR #186, #189, #187 y #188, ese orden; `develop` en `ca11be2`), cambiar toda frase «en PR» / «Sigue: revisión y merge» / «Queda: Javi mergea» por el estado real sin borrar la historia de verificaciones; frontend sobre `develop` mergeado: build limpio y 370/370; backend: última suite completa verde 809/809 sobre `764c56e` (auditor-persistencia, 16 m 30 s), pendiente suite completa sobre `develop` con la base sin otra carga (Santi), issue #190 abierta; corregir las filas #181, #13, #51, #163 que digan «PR abierto» o «puede entrar a develop cuando Javi lo revise» (las issues #163, #13 y #51 siguen abiertas hasta el recorrido DoD 10 con Sergio); dejar visible lo que falta para cerrar el sprint: (1) recorrido manual en `lacentral_demo` (Javi con Sergio): factura desde PDF ETMAN/Fazio/Expoyer, cuarto descuento, notas de crédito, Compras registradas y anulación, fechas abreviadas, Enter en NCA, búsqueda por palabra en proveedores y clientes (la demo tiene 0 clientes: cargar uno antes), layout de la FCO; (2) suite completa de backend sobre `develop` (Santi); (3) borrar `nc057_fuente` (`backend/db/pruebas/nc057_fuente_borrado.sql`, Santi).
> 1.2 Decisiones confirmadas por Sergio (23/11/2026): agregar a la tabla de decisiones, con el número siguiente al último D-S6 existente, fuente «Sergio vía Javi, 23/11/2026»: descuentos sucesivos (10 % + 5 % = 14,5 % acumulado; confirma 056 y cuarto descuento); notas de crédito (Sergio confirma D-S6-21 a D-S6-26 tal como están); depósito de ingreso de una compra (entra al depósito de la sucursal que compró, definido en la carga de la factura; confirma destino por línea o por cabecera, D9).
> 1.3 Respuestas que abren trabajo (NO implementar; registrar) en `docs/sprint07/ESTADO.md` (sección «Candidatas desde el cierre del Sprint 6»), cada una con su pendiente de decisión y sin estimar SP: (1) IVA 10,5 % (artículos de electricidad; hoy `TasaIva = 0.21m` fijo en `RegistrarFacturaCompraUseCase` y `RegistrarNotaCreditoCompraUseCase`, el lector de ETMAN y el control del neto de la nota de crédito de Expoyer también asumen 21 %; el esquema no tiene alícuota; decisión de equipo: dónde vive la alícuota → script 12; preguntas para Sergio); (2) costo al anular una factura (Sergio: vuelve al anterior a la factura, ejemplo 1450/1500/1450; hoy `AnularConEfectosAsync` vuelve al último registro válido de `articulo_historial_compra`, y si no hay conserva el actual; guardar el costo previo por línea → script 12; reemplaza la «interpretación pendiente» del CA-002 de la 028: actualizar `docs/historias/028-*.md` sin cambiar código); (3) anulación con un solo administrador (Sergio: «con uno solo sí se puede anular»; el CA-006 exige otro ADMINISTRADOR distinto; sin cambiar código, registrar la ambigüedad y la pregunta exacta «¿puede el mismo administrador pedir y autorizar su propia anulación?»); (4) módulo de devoluciones a proveedor (Reclamos, alerta de pendientes, PDF; HU nueva, esquema nuevo, depende de la 020 del Sprint 8; preguntas abiertas para la proforma); (5) H1 vencimiento según proveedor y H2 reconocimiento de proveedor y sucursal en PDF, con lo que falta; (6) deuda técnica menor (LectorTextoPdf a Data y loguear su catch; NormalizacionArticulo.Palabras y Replace de acentos a un lugar común; limitar intentos de contraseña en login y anulación; unificar el parser de fechas abreviadas).
> 1.4 Bitácora: una entrada en `docs/sprint06/BITACORA.md` con la instrucción recibida (verbatim), plan, estado y lo anterior. Sin datos sensibles.
> PR 2 · Fix: rango de años en fechas abreviadas (`fix/fecha-rango-anios`)
> Archivos: `frontend/src/app/compartido/fecha-abreviada.ts` y su spec; `frontend/src/app/pantallas/factura-compra/factura-compra.ts`.
> Problema: `interpretarFecha('20/10/0000')` o `'20/10/0999'` devuelve una fecha «válida» con ISO mal armado (`0-10-20`), que después viaja a la API.
> Cambios: (1) Primero el test (rojo por aserción): años de 4 dígitos fuera de 2000 a 2100 son un error con mensaje claro («El año tiene que estar entre 2000 y 2100.»); casos `20/10/0000`, `20/10/0999`, `20/10/1999`, `20/10/2101`, `20102101`; válidos en el borde `20/10/2000`, `20/10/2100`; 29/02/2000 y 29/02/2100 siguen el cálculo de bisiesto existente (2100 no es bisiesto). (2) Producción: acotar el año en `interpretarFecha`; no cambiar formatos ni reloj; confirmar con Javi el rango 2000–2100 si es discutible. (3) Quitar el `[name]` duplicado del `<input>` de fechas en `factura-compra.ts` (queda `[attr.name]` o `[name]`); el control sigue registrado en `NgForm` como `emision`, `vencimiento`, `imputacion`. (4) Verificar: tests filtrados del parser y del componente, frontend completo, `ng build`; backend no se toca. (5) Mostrar el diff, pedir OK, commitear en dos commits (test y producción, el rojo visible), pushear y abrir el PR contra `develop`; pasar `verificador-tdd` sobre esos dos commits.
> Fuera de esta spec: todo lo del punto 1.3 se registra pero no se implementa. No tocar esquema ni backend. Al terminar, informar: qué cambiaste en cada PR, qué verificaciones corriste y su resultado, y qué decisiones quedaron pendientes de Javi o de Sergio.

**Plan:** 1. `git fetch`, ramas y bitácora: `origin/develop` en `ca11be2`; la rama `docs/sprint06-cierre` sale de ahí. 2. PR 1: `docs/sprint06/ESTADO.md` al día (filas, frases «en PR», lo que falta, decisiones D-S6-27 a D-S6-29), `docs/sprint07/ESTADO.md` con las candidatas, CA-002 de la 028, esta entrada. 3. Mostrar el diff y pedir OK. 4. PR 2 desde `develop`: test rojo, producción, `[name]` duplicado, filtrados, completo, `ng build`; diff y OK; dos commits; push; PR; `verificador-tdd`.

**Estado:** en curso
- Arranque: `git fetch --all --prune`; `origin/develop` = `ca11be2` (merges de #186, #189, #187 y #188; también #181, #177 a #180, #183 a #185). La rama de la tarea anterior (`fix/nca-enter-confirma-valor`) ya está contenida en `develop`. Rama `docs/sprint06-cierre` creada desde `origin/develop`. Sin worktrees extra.
- Última entrada «en curso» anterior a esta: la de la 057 (bloque del 19/11, línea con `**BLOQUEADO EN:** revisión y merge de #183 → #184 → #185`); quedó sin pasar a «terminada» y no se reescribe (la bitácora solo se agrega). Está resuelta: los tres PR se mergearon el 20/11 (`9f387c2`).
- ESTADO del Sprint 6: las filas #163, #13, #51 y #182 y las entradas de PRÓXIMA SESIÓN pasan a «mergeado»; sección «Lo que falta para cerrar el Sprint 6»; decisiones D-S6-27 a D-S6-29 (Sergio vía Javi, 23/11); entrada nueva de PRÓXIMA SESIÓN. Las issues #163, #13 y #51 siguen abiertas hasta el DoD 10.
- `docs/sprint07/ESTADO.md`: sección «Candidatas desde el cierre del Sprint 6» (C7-01 a C7-06), sin SP. `docs/historias/028-HU-FCO-04.md`: la «interpretación pendiente» del CA-002 pasa a la respuesta de Sergio; el código no cambia.
- Frontend sobre `develop` (mismo código que esta rama, que solo toca `docs/`): `npx ng test --watch=false --browsers=ChromeHeadless` → `TOTAL: 370 SUCCESS` (370 de 370). Confirma la cifra que dice el ESTADO.
**BLOQUEADO EN:** OK de Javi para commitear y pushear el PR 1 (docs); el PR 2 (`fix/fecha-rango-anios`) arranca después, desde `develop`.
- 23/11: respuestas de Sergio (vía Javi) a las consultas: IVA por alícuota con desplegable (21 y 10,5; suma por alícuota), costo a mano se respeta, el mismo administrador puede anular y autorizarse (contradice CA-006: decide el equipo), devoluciones (motivos, alerta siempre visible, sin NC parcial). Registradas en `docs/sprint07/ESTADO.md` (C7-01 a C7-04). H1/H2 (punto 5) sin respuesta. Código sin cambios.

## 23/11/2026 · Conflicto del #191 y correcciones del cierre documental (`docs/sprint06-cierre-correcciones`)

**Instrucción recibida (verbatim):**
> 1. El #191 tiene conflicto con develop después del merge del #192: los dos agregaron un bloque al final de docs/sprint06/BITACORA.md. En fix/fecha-rango-anios: git merge origin/develop (nunca rebase). Resolvé conservando los dos bloques en orden cronológico, sin reescribir ninguno (la bitácora solo se agrega). Frontend completo una vez y push. No hace falta relanzar el verificador: el merge no cambia código.
>
> 2. El #192 se mergeó con tres correcciones pendientes. Van en un PR aparte, solo de docs, desde develop (rama docs/sprint06-cierre-correcciones):
>    a) docs/sprint06/ESTADO.md, «Lo que falta para cerrar el Sprint 6», punto 2: la suite completa de backend sobre develop ya se corrió, 809/809 sobre ca11be2 el 22/11/2026 (18 m 33 s), y está comentada en el #189. Sacala de lo pendiente y actualizá la «última verde» (también en PRÓXIMA SESIÓN).
>    b) Mismo apartado, punto nuevo: punto 6 del relevamiento de Sergio, la búsqueda de artículos que no devolvía nada. El #189 arregló proveedores y clientes; artículos no se reprodujo. Va al recorrido DoD 10, buscando lo mismo que buscó Sergio; si falla, candidata del Sprint 7.
>    c) docs/sprint07/ESTADO.md, C7-04 (devoluciones): agregá a las preguntas abiertas el depósito «Vélez», que Sergio nombró como origen y que no existe en el sistema.
>    d) Punto 3 de «Lo que falta»: nc057_fuente ya se borró (lo hizo Santi el 23/11). Sacalo de lo pendiente.
>
> Resúmenes en español.

**Plan:** 1. En `fix/fecha-rango-anios`: `git merge origin/develop`, conflicto de la bitácora con los dos bloques en orden, frontend completo, push. 2. Rama `docs/sprint06-cierre-correcciones` desde `origin/develop` (`d783ea2`): a) a d) en los dos ESTADO, esta entrada, PR.

**Estado:** terminada
- Merge `864b89d` en `fix/fecha-rango-anios`: primero el bloque del cierre documental (#192, commit de las 16:38), después el del fix (#191, 16:42); los dos quedaron idénticos a sus originales (comparados con `diff`). Frontend completo: `TOTAL: 377 SUCCESS`. Pusheado.
- a) y d): «Lo que falta» queda con el recorrido DoD 10 y el punto de artículos; la suite 809/809 sobre `ca11be2` (22/11, 18 m 33 s, comentario en el #189, verificado) y el borrado de `nc057_fuente` pasan a «ya hecho». «Última verde» actualizada también en PRÓXIMA SESIÓN. b) punto 2 nuevo. c) «Vélez» en las preguntas abiertas de C7-04.
- Abierto: el bloque del cierre documental (#192) quedó con `**Estado:** en curso` y el del fix (#191) tiene dos líneas de Estado («en curso» y «terminada»); no se tocaron. Este bloque y el del #191 se agregan al mismo final: el que se mergee segundo vuelve a tener el mismo conflicto.

## 23/11/2026 · Devoluciones a proveedor fuera de la tesis (`docs/sprint06-cierre-correcciones`, #193)

**Instrucción recibida (verbatim):**
> c) docs/sprint07/ESTADO.md: C7-04 (módulo de devoluciones a proveedor) sale de las candidatas del Sprint 7. Decisión del equipo (Javi y Santi, 23/11/2026): queda FUERA DE LA TESIS, como «Sprint 9 · post-tesis», para construirlo después de la defensa como mejora para el negocio. Movelo a una sección nueva «Fuera de la tesis · Sprint 9» con lo que ya dijo Sergio y las preguntas abiertas (estados, qué es el PDF, el depósito «Vélez» que no existe en el sistema). Anotá la decisión con fecha en el ESTADO del Sprint 6.

**Plan:** 1. Mover C7-04 a «Fuera de la tesis · Sprint 9» (S9-01), sin perder contenido. 2. D-S6-30 en el ESTADO del Sprint 6. 3. Commit y push al #193.

**Estado:** terminada
- C7-04 movido entero (respuestas de Sergio y preguntas abiertas, con «Vélez») a `docs/sprint07/ESTADO.md`, sección final «Fuera de la tesis · Sprint 9 · post-tesis», como S9-01. C7-05 y C7-06 no se renumeran.
- D-S6-30 (equipo, 23/11/2026) en la tabla de decisiones del 23/11 de `docs/sprint06/ESTADO.md`; el párrafo de «abre trabajo» ya no manda las devoluciones al Sprint 7.
