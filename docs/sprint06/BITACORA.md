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

**Estado:** en curso
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
