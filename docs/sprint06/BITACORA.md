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

**Estado:** en curso
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
