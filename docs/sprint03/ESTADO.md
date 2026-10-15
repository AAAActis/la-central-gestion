# Estado del Sprint 3

Memoria entre fases. El agente lo lee al empezar y lo actualiza al terminar cada fase.
Si se pierde el contexto, este archivo es la única fuente de verdad sobre qué está hecho.

> Rutas: desde el 2026-10-14 las sesiones arrancan en la raíz del repo. Las entradas hasta
> el 2026-10-13 citan rutas relativas a `backend/` (`LaCentral.Data/...` es
> `backend/LaCentral.Data/...`). Las entradas nuevas usan rutas desde la raíz.

## Línea base
- Commit de partida: `1660d5d` (develop, Merge PR #110 fix/saneamiento-bd-d10)
- Tests: 108 passed, 0 failed, 0 skipped
- Esquema: D7, D8, D9 y D10 aplicados en la base

### Medición Fase 0 (2026-10-13)
**dotnet build** (`--no-incremental`, LaCentral.slnx): 0 errores, 4 warnings.
- CS8619 `LaCentral.Data/Repositorios/ClienteRepositorio.cs:134` — `List<string?>` asignado a `List<string>`.
- MSB3277 `LaCentral.Tests` — conflicto Microsoft.EntityFrameworkCore.Relational 10.0.4 (directa en Tests) vs 10.0.11 (vía Data/Api). Se elige 10.0.4.
- CS8625 ×2 `LaCentral.Tests/Clientes/CrearClienteUseCaseTests.cs:201` (col. 67 y 87) — literal null a tipo no anulable.

**dotnet test**: 108 total · 108 passed · 0 failed · 0 skipped (~28 s).
Conexión de test vía `LACENTRAL_TEST_PG` (alias). `LA_CENTRAL_REQUIRE_POSTGRES` NO está seteada.

**Desglose (108):**

| Tipo | Tests | Clases |
|---|---|---|
| PostgreSQL real (`[PostgreSqlFact]`, esquema aislado; verificado en StdOut "Proveedor real: Npgsql") | 8 | ArticuloRepositorioTests (5), ArticuloBusquedaPostgreSqlTests (1), ClienteRepositorioTests (1), ProveedorRepositorioTests (1) |
| Casos de uso con Moq | 59 | Autenticar/Crear/DarDeBaja Usuario; Crear/Consultar/Modificar/DarDeBaja/Reactivar/BajaYReactivacion Cliente; Crear/Consultar/Modificar/BajaYReactivacion Proveedor |
| HTTP con WebApplicationFactory (+ Moq de repositorios, sin base) | 4 | CicloCompletoIntegracionTests (2), LoginIntegracionTests (1), EndpointDiagnosticoRetiradoTests (1) |
| EF InMemory | 1 | UsuarioRepositorioTests |
| Unitarios puros, sin base ni dobles | 36 | PostgreSqlSeguridadTests (8, solo validan nombres de base), ArticuloPrecioEstimadoTests (7), ArticuloRepositorioTests no-PG (7: Theory de validación + ToQueryString), NormalizacionArticuloTests (6), ArticuloTests (4), HistorialBajaModeloTests (4, metadatos del modelo EF) |

**Regla de oro:** `LaCentral.UseCases.csproj` no tiene `PackageReference` ni `ProjectReference`. ✔

Resultado: todo en verde, skipped = 0. Compuerta de Fase 0 superada.

## Fases

| Fase | Estado | Rama | PR | Tests | Pendientes |
|---|---|---|---|---|---|
| 0 · Línea base | hecha (2026-10-13) | — | — | 108/108, 0 skipped (8 PG real) | — |
| 1 · Deuda bloqueante | hecha, PR sin abrir (2026-10-13) | `fix/deuda-bloqueante-sprint3` (pusheada, HEAD `108ac1d`) | sin abrir: no hay `gh`. Cuerpo en `docs/sprint03/pr-fase1-deuda-bloqueante.md` | 129/129, 0 skipped, 0 warnings (17 PG real, +21 tests) | abrir el PR a mano; deuda abajo |
| 2 · D10 en la aplicación | hecha (2026-10-14). Hallazgos 1, 2 y 3 del auditor arreglados. **Auditor APTO, verificador APTO (segunda pasada)** | `feature/d10-historial-baja` (pusheada; código hasta `d378c0f`, sale de `108ac1d`) | sin abrir: no hay `gh`, se abre a mano desde el link de compare. Cuerpo en `docs/sprint03/pr-fase2-d10-historial-baja.md`, reescrito el 2026-10-14. Base provisoria `fix/deuda-bloqueante-sprint3` | **2026-10-14, en `d378c0f`: 176 total · 176 passed · 0 failed · 0 skipped · 0 warnings (43 PG real)** | abrir el PR a mano; deuda abajo |
| D1 · compuerta de esquema en la aplicación | hecha (2026-10-14): re-scaffold, conversión de `Modalidad`, suite en verde. **Auditor APTO, verificador APTO** | `feature/d1-precios-modalidad` (sale de `feature/d10-historial-baja`) | sin abrir. Cuerpo en `docs/sprint03/pr-d1-precios-modalidad.md`. Base provisoria `feature/d10-historial-baja` | **179 total · 179 passed · 0 failed · 0 skipped · 0 warnings (46 PG real)** | abrir el PR a mano |
| — · Merge | **Fases 1, 2 y D1 mergeadas en develop** (2026-10-14): `a276272` (#113), `b0819a9` (#114), `f3c6705` (#115). Ramas borradas, locales y remotas | | | | |
| 3 · HU-ART-01 | **cerrada (2026-10-14)**, con el script 04, las decisiones A-D y H1/H3. Ronda 1 de arreglos: **verificador APTO**, auditor APTO (pasada anterior). Ver "Fase 3" | `feature/HU-ART-01-alta-articulo` (sale de develop `f3c6705`, pusheada; código hasta `a9e7410`) | sin abrir: no hay `gh`, se abre a mano desde el link de compare. Cuerpo en `docs/sprint03/pr-fase3-hu-art-01.md`. Base `develop` | **234 total · 234 passed · 0 failed · 0 skipped · 0 warnings (77 `[PostgreSqlFact]`), en `a9e7410`** | el entregable en `frontend/` lo cubre la Fase 4 (pantalla de alta incluida) |
| 4 · HU-ART-02 | **hecha (2026-10-15)**: búsqueda, ficha con las seis modalidades y su razón, GET por HTTP, enums por nombre y `frontend/` en Angular. **Verificador APTO, auditor APTO (ronda 1)**. Ver "Fase 4" | `feature/HU-ART-02-consultar-articulos` (sale de `feature/HU-ART-01-alta-articulo` `ca84eae`, pusheada; HEAD de código `d44bab7`) | sin abrir: no hay `gh`. Cuerpo en `docs/sprint03/pr-fase4-hu-art-02.md`. Base provisoria `feature/HU-ART-01-alta-articulo`: **cambiar a `develop` cuando entre la Fase 3** | **258 total · 258 passed · 0 failed · 0 skipped · 0 warnings (86 `[PostgreSqlFact]`), en `d44bab7`** | abrir los PR de las Fases 3 y 4 a mano; deuda abajo |
| 5 · HU-FCO-01 | pendiente | | | | |
| 6 · HU-ART-05 + CA5 | pendiente (misma compuerta D1) | | | | |
| 7 · HU-ART-03 | pendiente | | | | |

## Corrección 2026-10-14 · el 28P01 era una credencial, no código

- El `28P01` venía de una **credencial vieja en la variable de entorno** `LACENTRAL_TEST_PG`.
  No era un problema del contenedor ni del código. El equipo la corrigió.
- Suite verificada hoy:
  - corrida del equipo: **167 total · 167 correcto · 0 con errores · 0 omitido · 51,6 s**;
  - re-corrida del agente desde `backend/` (`dotnet test LaCentral.slnx`, HEAD `365a686`):
    **167 · 167 passed · 0 failed · 0 skipped · 44 s**.
  - Los **34 `[PostgreSqlFact]` pasan** contra PostgreSQL real.
- El **NO APTO del verificador-tdd** de abajo tenía **dos causas**. Una se cayó:
  1. ~~Los 34 PG en rojo, 0 PG aprobados~~. **Se cae:** era un artefacto del entorno (28P01).
  2. **Sigue en pie:** el orden test → producción no se puede verificar desde git. En 7 de los 8
     commits el test y la producción vinieron juntos. Además, los 7 PG de historial vieron su rojo
     solo contra stubs `NotImplementedException`.
- **Siguen en pie** todos los hallazgos del auditor-persistencia: 1 atomicidad, 2
  `usuario_baja_id` de usuario, 3 `fechas_coherentes`, 4 concurrencia en transacción y 5 dobles
  inválidos. También siguen los menores del verificador. Los 1, 2 y 3 se arreglan en esta rama.
- Lo que dice abajo sobre el 28P01 queda como registro de lo que se vio en ese momento. No se
  reescribe: esta sección lo corrige.

## Fase 3 · HU-ART-01 · Alta de artículo (2026-10-14)

Rama `feature/HU-ART-01-alta-articulo`, desde develop `f3c6705`.

**Esquema · script 04** (`backend/db/rescaffold/04_Modalidades_Base_Calculo.sql`, aplicado por el equipo en las dos bases):
- `articulo_precio_modalidad.base` varchar(16) NULL, con valores `CostoArticulo` o `PrecioLista`. OJO: `CostoArticulo` es el costo del artículo, NO la modalidad Costo.
- `porcentaje_no_negativo` se reemplazó por `porcentaje_rango` (>= -100), y se agregaron `base_valida`, `base_coherente` (`(porcentaje IS NULL) = (base IS NULL)`) y `base_sin_autorreferencia` (Lista no se calcula sobre PrecioLista). En total, **10 CHECK**.
- Fórmula: `precio = valor_de_la_base × (1 + porcentaje/100)`. Lista +50 sobre CostoArticulo; Costo 0 sobre CostoArticulo; Efectivo -10 sobre PrecioLista; Tarjeta3/6 y CasoEspecial sin base ni porcentaje.
- Antes de scaffoldear se comparó la huella md5 del esquema `public` (columnas, restricciones e índices) de `lacentral_test` contra la de `lacentral`. Dieron lo mismo: `14665b6c6902a9cecdb3a4c38a230ea4`. La de producción la corrió el equipo. Re-scaffold en `d9ff67d`: solo `Base` y los comentarios.

**Commits, por unidad** (cada rojo verificado por aserción, la producción en un commit aparte):

| Unidad | estructura → test (rojo) → producción | Notas |
|---|---|---|
| H3 + D (base texto ↔ `BasePrecio`) | `e2bc35f` → `413308a` (L58, L101) → `362b870` | Mutación `Enum.TryParse(ignoreCase)` → rojo |
| B (fórmula) + A (`PrecioModalidad`) | `1d96eb0` → `fce502c` (9/13 rojos) → `7677fb8` | 4 tests que esperan null pasan contra el esqueleto: 3 mutaciones los ponen en rojo |
| C (`ConfiguracionInicial`) | (estructura en `1d96eb0`) → `eb22ab0` (L17, L33, L47) → `d8a1064` | Mutación "misma instancia" → rojo |
| H1 (13 restricciones) | `9d43f88` | Caracterización: pasan a la primera. Mutación "quitar la restricción en el esquema aislado" → los 13 en rojo |
| Caso de uso (Moq) | `568a29b` → `db51be9` (17 rojos) → `442aeff` | |
| Repositorio (PG) | `c0ff00d` → `c133b8f` (4 rojos) → `2bb8266` | D2, código de otro artículo y proveedor inactivo pasan a la primera (los resuelve el caso de uso): 3 mutaciones → rojo |
| Controlador (HTTP + PG) | `127ab20` → `ef3b6a8` (2 rojos) → `d9ae80f` | El 403 pasa a la primera: mutación `[Authorize]` sin roles → rojo |

Los hashes de esta tabla son los de la rama. Ver `git log develop..HEAD`.

**Decisiones de esta fase:**
- **Código interno = `LC-{id:D6}`**, derivado de la identidad. Sin MAX+1 y sin cambiar el esquema. El alta inserta con un `TMP-<guid>` provisional y en la misma transacción hace `ExecuteUpdate` al definitivo. Ningún código importado de Multisoft empieza con `LC-` (verificado en `lacentral_test`). Test PG con dos altas simultáneas.
- **"Sucursal habilitada":** `sucursal` no tiene esa marca. El stock cero va a TODAS las sucursales (hoy FR y SV).
- La validación previa de códigos no cierra la carrera. La cierran `codigo_unico_global` y `un_codigo_por_proveedor`, que ya están en la lista blanca del manejador (409).
- `GET /api/articulos/{id}` es de la Fase 4. El `Location` del 201 ya apunta a esa ruta.
- **Tests reapuntados:** `ArticuloPrecioEstimadoTests` conserva sus 8 casos. "SinMargenes" pasa a ser Tarjeta sin tasa, y "ModalidadSinMargenCargado" pasa a ser CasoEspecial manual. Ninguno se borró.
- **Regla de esta fase** (instrucción del equipo, que reemplaza a la compuerta "test que pasa a la primera → parar" del plan): lo que pasa a la primera se declara y se verifica con una mutación no commiteada.

### Subagentes sobre `git diff f3c6705..fb90b32` (2026-10-14) · **verificador NO APTO, auditor APTO** → se PARÓ
- **verificador-tdd: NO APTO.** Reprodujo todos los rojos, verdes y mutaciones declarados. El orden test → producción se cumple y no se borró ningún test. Bloquean cuatro mutaciones suyas que sobreviven con la suite en verde:
  1. `ArticulosController.Crear`: `CodigosProveedor ?? new List<>()` no tiene test. Falta un POST sin `codigosProveedor` → 201.
  2. `CrearArticuloUseCase.cs:96-98`: la rama `creado is null` no tiene test. `Alta_EscriturasDentroDeLaUnidadDeTrabajo` no afirma el resultado.
  3. `ModalidadPersistida.Nombre<T>`: el guard `Enum.IsDefined` no tiene test.
  4. `CrearArticuloRequest` (HTTP): `[Required]`/`[MaxLength]` entraron en un commit "sin comportamiento" (`127ab20`) y no tienen test de 400.
  5. Una de sus tres corridas completas dio 17 fallas `53300 too many clients`. Las otras dos dieron 226/226.
  - Menor: `ProveedorId <= 0` en `ArticuloValidaciones` no tiene test (el proveedor 0 igual cae en "no existe").
- **auditor-persistencia: APTO.** H1, H2 y H3 cerrados en el código y en el motor. Atomicidad real de las cuatro tablas, relectura con contexto nuevo, sin Ids sembrados, el `DROP CONSTRAINT` no alcanza `public` y el test HTTP pega contra PostgreSQL. Hallazgos no bloqueantes:
  1. **53300 `too many clients`:** con dos corridas a la vez contra el mismo servidor (`max_connections=100`), la primera corrida suya dio 47 fallas. Causa:
     - `PostgreSqlPruebas` abre un pool por esquema y no lo vacía en `DisposeAsync`;
     - las fábricas de `WithWebHostBuilder` no se liberan.
     - Los dos subagentes corrieron en paralelo y cada uno vio la corrida del otro.
     - **El 226/226 vale solo con el servidor libre.**
  2. Entre `codigo_interno` (`LC-nnnnnn`) y `articulo_codigo_alternativo.codigo`, la unicidad solo la controla la aplicación: no hay restricción entre tablas. Hoy hay 0 casos.
  3. No hay test de dos altas simultáneas con el mismo código de proveedor (lo cierra `codigo_unico_global` → 409). `DosAltasSimultaneas` no fuerza el intercalado.
  4. El comentario de la tabla nombra `CalcularPreciosArticuloUseCase`, que no existe: la regla está en `ReglasPrecioModalidad.Calcular`.
  5. **Entorno:** el rol `lacentral` de `LACENTRAL_TEST_PG` es SUPERUSER y es el mismo que usa la base principal en el mismo servidor. Las dos bases solo las separa `ValidarBase`, que mira el nombre.
- **Estado:** NO se hizo push. `docs/sprint03/pr-fase3-hu-art-01.md` queda como borrador sin commitear, sin los veredictos. Nada de esto se arregló: lo decide el equipo.

### Verificación pedida por el equipo · `AsignarCodigoInternoAsync` (2026-10-14, `c6dc60b`)
- Ya verificaba el conteo: `ExecuteUpdateAsync(...) == 1`. Si da false, el caso de uso devuelve NoEncontrado y la unidad revierte.
- Faltaba el test PG del repositorio real con un Id que no existe, y la afirmación de que ningún `TMP-` sobrevive. Se agregaron los dos en `c6dc60b`: el primero como test propio y el segundo en el camino feliz, en las dos altas simultáneas y en todas las fallas a mitad de camino.
- No hubo commit de producción. Los tests pasan a la primera y se verificaron con dos mutaciones: `>= 0` y "true sin actualizar". Las dos dan rojo.

### Subagentes sobre `git diff f3c6705..c6dc60b` (2026-10-14, segunda pasada, de a uno) · **verificador NO APTO, auditor APTO** → se PARÓ
Corrieron uno después del otro, sin otra suite en paralelo, para no repetir el 53300 de la primera pasada.

**verificador-tdd · NO APTO**
- Qué verificó, en su propio worktree:
  - build `--no-incremental` con 0 warnings;
  - suite en `c6dc60b`: 227 total · 227 passed · 0 failed · 0 skipped;
  - 72 `[PostgreSqlFact]`, y ningún 53300 en 7 corridas completas y 6 filtradas.
- Orden test → producción: se respeta en los 6 pares. Los 6 commits de test no tocan nada fuera de `Tests/`, según `git diff-tree`.
- Hizo checkout de cada commit rojo: todos fallan por aserción, ninguno por compilación ni NRE.
- Las mutaciones declaradas de `c6dc60b` (`>= 0` y "true sin actualizar") dan rojo, tal como dice el commit.
- **Bloquean 4 mutaciones que sobreviven, ya informadas en la primera pasada y sin arreglar:**
  1. `ArticulosController.cs:38`: con `CodigosProveedor!` en lugar de `?? new List<>()`, la suite sigue en verde. Falta un test HTTP de alta sin códigos que espere 201.
  2. `CrearArticuloUseCase.cs:96-97`: con la rama `creado is null` devolviendo `Success`, la suite sigue en verde. Falta un test, o decidir si la rama tiene que existir.
  3. `ModalidadPersistida.cs:34`: con `Enum.IsDefined` → `true`, la suite sigue en verde. Falta un test de que escribir `(Modalidad)99` falla.
  4. `Dtos/CrearArticuloRequest.cs`: sin los 5 `[Required]`/`[MaxLength]` (del chore `127ab20`), la suite sigue en verde. La regla ya la cubre `ArticuloValidaciones`, pero cambia la forma del cuerpo del 400. Hace falta un test HTTP de 400 o sacar los atributos.
- Menor: `Post_Alta_201ConIdYLocation` no compara `CodigoInterno` en la relectura.
- Sin verificar: no revisó el rojo de `9d43f88` (H1) ni mapeó los criterios de aceptación de la HU contra los tests.

**auditor-persistencia · APTO**
- Qué verificó:
  - suite en su worktree: 227/227, 70 tests con esquema aislado según el TRX;
  - catálogo de `lacentral_test`, en una sesión de solo lectura: después de la suite quedan 0 esquemas `pr110_test_*` huérfanos y 0 artículos con `TMP-`;
  - las restricciones de `articulo_precio_modalidad`, `articulo`, los índices de códigos y la PK de stock coinciden con el modelo, y no hay triggers;
  - repitió las dos mutaciones de `c6dc60b`: las dos dan rojo.
- Confirma que ningún camino deja un `TMP-` persistido, ni el feliz, ni la excepción, ni el fallo devuelto. Confirma también la atomicidad de las cuatro tablas, la relectura con contexto nuevo, que no hay Ids sembrados contra PG, que los DROP no salen del esquema aislado y que el test HTTP pega contra PostgreSQL.
- Hallazgos no bloqueantes:
  1. `ArticulosApiPostgreSqlTests.cs:91-94` no compara `CodigoInterno` en la relectura.
  2. Después del `ExecuteUpdate`, el contexto sigue rastreando la entidad con `TMP-`. Si una operación futura reusa el contexto y guarda, podría reescribirlo: es un riesgo latente.
  3. Siguen en pie los de la primera pasada: el rol SUPERUSER compartido con la base principal; la unicidad entre `codigo_interno` y los códigos alternativos, que solo controla la aplicación; que no hay test de carrera con el mismo código de proveedor (409); y el comentario de la tabla que nombra `CalcularPreciosArticuloUseCase`.
- Sin verificar: la base principal, por diseño; el 409 en una carrera HTTP; y el 53300 con corridas simultáneas.

**Estado:** se PARÓ en el punto 2 de la instrucción. NO se tocaron los puntos 3 (cambios al plan), 4 (borrar las 17 ramas viejas) y 5 (push). Arreglar las 4 mutaciones lo decide el equipo.

**Sin entregable en `frontend/`:** la carpeta no existe. El requisito permanente se agregó al plan en esta sesión, después de cerrar la fase. Lo decide el equipo.

### Ronda 1 de arreglos (2026-10-14, `f05cee4`..`a9e7410`) y verificación acotada · **verificador APTO** → Fase 3 CERRADA
Arreglos, según la regla de CLAUDE.md "NO APTO de un subagente":

| Commit | Qué |
|---|---|
| `f05cee4` | test: POST sin códigos → 201, y 400 por las DataAnnotations del DTO HTTP (caracterización, verificada con 6 mutaciones) |
| `f480418` | test: `proveedorId` 0 o negativo → Invalido sin consultar proveedores (caracterización) |
| `f581dac` | test: la unidad de trabajo real devuelve el resultado de la operación y la ejecuta una vez (caracterización) |
| `7c7d5c4` → `e4f0f21` | test en rojo: una modalidad o base sin nombre la rechaza el CHECK del motor → se borra el guard `Enum.IsDefined` de `ModalidadPersistida` (inalcanzable: lo cubre el motor) |
| `f581dac` → `a9e7410` | se borra la rama `creado is null` de `CrearArticuloUseCase` (inalcanzable: la unidad de trabajo devuelve el resultado de la operación) |
| `ace11ee` | definiciones de subagentes: el verificador nuevo (lo escribió el equipo) y R1-R5 + PRESUPUESTO copiados al auditor. Sin código |

**Corrida del equipo sobre `a9e7410`:** 234 total · 234 correcto · 0 con errores · 0 omitido · 109 s.

**verificador-tdd · APTO.** Un solo agente, en worktree propio, con el alcance acotado a los commits posteriores a `903e0fd`. Usó 13 corridas filtradas y 1 suite.
- Orden: `7c7d5c4` (solo test) → `e4f0f21` (solo producción); `f581dac` (solo test) → `a9e7410` (solo producción). `f05cee4`, `f480418` y `f581dac` son de caracterización, declarados así en el commit.
- Reprodujo el rojo de `7c7d5c4` por aserción, en `ArticuloPrecioModalidadPostgreSqlTests.cs:188`: esperaba `DbUpdateException` y llegó `InvalidOperationException "Modalidad sin nombre: 99."`.
- Las 4 mutaciones que sobrevivían ahora mueren:
  - `CodigosProveedor!` → 500 en `Post_SinCodigosProveedor_201YSeGuardaSinCodigos`;
  - quitar cualquiera de los 5 atributos del DTO → rojo en los tests de 400;
  - las ramas `creado is null` e `Enum.IsDefined` ya no existen, y nada las necesitaba.
  - También mueren la mutación de `ProveedorId <= 0` y dos de la unidad de trabajo (M8 y M9).
- Suite sobre HEAD: build `--no-incremental` con 0 warnings; **234 · 234 passed · 0 failed · 0 skipped**; **77 `[PostgreSqlFact]`** contra PostgreSQL real.
- No bloqueantes:
  - `Post_NombreDeMasDe200` mata la mutación con un `KeyNotFoundException`, no con una aserción (`ArticulosApiPostgreSqlTests.cs:156`);
  - en `ProveedorNoIndicado_...` solo discrimina el `Verify(Times.Never)` (`CrearArticuloUseCaseTests.cs:168`);
  - `creado!` depende del contrato de la unidad de trabajo;
  - hubo una falla pasajera en `PostgreSqlPruebas.CrearAsync` (L79): se repitió y se descartó.
- Borró su worktree (con `rm -rf`, por "Filename too long") y corrió `git worktree prune`: solo queda el principal.
- **El auditor no se volvió a correr en esta ronda**, porque el equipo no lo pidió: su APTO es el de `c6dc60b`. Esta ronda agrega tests PG (`7c7d5c4` y `f581dac`) y no toca repositorios.

## Fase 4 · HU-ART-02 · Consulta de artículos (2026-10-15)

Rama `feature/HU-ART-02-consultar-articulos`, que sale de `feature/HU-ART-01-alta-articulo` (`ca84eae`): la Fase 3 todavía no está en develop. **El PR tiene base provisoria en la rama de la Fase 3: cambiarla a `develop` cuando entre la Fase 3.**

**Qué entró:**
- `ConsultarArticulosUseCase`: `ConsultaArticulos.Validar()` devuelve `Result` y la llama el caso de uso. El repositorio ya no valida ni lanza `ArgumentException`.
  - Texto vacío, en blanco o ausente → 400.
  - Más de 10 alternativas, o página / tamaño fuera de rango → 400.
  - Sin resultados → 200 con lista vacía.
- `ObtenerArticuloDetalleUseCase`: la ficha devuelve SIEMPRE las seis modalidades, con `RazonAusenciaPrecio` cuando falta el precio. El orden de evaluación es:
  1. sin fila → `SinConfiguracion`;
  2. manual → `CasoEspecialNoFijado`;
  3. sin base → `TasaSinDefinir`;
  4. con base → `SinCosto`.
- `ArticuloRepositorio`:
  - cada resultado de la búsqueda trae sus códigos de proveedor, en una consulta aparte por Id;
  - `PreciosPorModalidad` deja de ser anulable: sin filas, devuelve una lista vacía.
- `GET /api/articulos` (con `texto`, `incluirInactivos`, `pagina` y `tamanoPagina` explícitos) y `GET /api/articulos/{id}`, para ADMINISTRADOR y OPERADOR. `JsonStringEnumConverter` global.
- `frontend/` en Angular 20 mínimo:
  - login con el token solo en memoria;
  - buscador y lista con código interno, nombre y códigos de proveedor;
  - ficha con las seis modalidades y su razón;
  - alta, que cubre también la pantalla que le faltaba a la Fase 3.

  Se levanta con dos comandos, en el puerto 4200 (ver `frontend/README.md`). Prueba de humo por el proxy contra `lacentral_test`, **solo lecturas**:
  - login malo → 401;
  - sin token → 401;
  - texto vacío → 400;
  - búsqueda "filtro" → 200 con 79 resultados;
  - ficha → seis modalidades por nombre.

  El POST de alta por la pantalla no se probó contra la base, para no escribir en `public`. Lo cubren los tests HTTP.

**Comodín "100%_original":** `Busqueda_PermiteInactivosYNoInterpretaComodinesSQL` usaba el texto exacto, pero NO discriminaba: con LIKE sigue en verde. Se agregó `Busqueda_100PorcientoGuionBajoOriginal_TraeSoloElLiteral`, con señuelos que solo coinciden si `%` y `_` se interpretan. Con la mutación a `EF.Functions.Like`, da rojo con 3 resultados.

**Commits, por unidad:**

| Unidad | estructura → test (rojo) → producción |
|---|---|
| Estructura | `07c418a` |
| Validación en el caso de uso (Moq) | `cac377e` (9 rojos) → `9bdf1ea` |
| Repositorio (PG) | `a5dc91a` (2 rojos) → `beb618f` |
| Ficha y razones (Moq) | `0f542a9` (8 rojos) → `85894da` |
| HTTP (WebApplicationFactory + PG) | `0c095d9` (7 rojos) → `633713d` |
| Frontend | `e794370` |
| Hallazgo del verificador: límite de 10 alternativas | `1d6afc1` (caracterización + 2 mutaciones) |
| H1 del auditor: relectura con contexto nuevo | `d44bab7` |

La Theory `Consulta_RechazaEntradaOPaginaInvalida` se **movió** del repositorio al caso de uso: conserva sus 6 casos y suma texto vacío y en blanco.

### Subagentes (2026-10-15, de a uno, cada uno en su worktree)
**verificador-tdd sobre `ca84eae..e794370` · APTO.**
- El orden test → producción se respeta en las cuatro unidades. Los rojos se reprodujeron por aserción.
- 17 mutaciones: 16 dan rojo, entre ellas todas las declaradas.
- Sobrevivía una sola: quitar el límite de 10 alternativas (`ConsultaArticulos.cs:15`). La línea venía igual de la Fase 3, así que no la contó como bloqueante. **Se arregló igual** en `1d6afc1`: sin el límite → rojo en L52, y con `> 9` → rojo.
- Suite: 257/257, 0 omitidos, 0 warnings, 86 PG.
- No bloqueantes:
  - los 8 rojos de la ficha caen en `IsSuccess` (las razones las respaldan las mutaciones);
  - `BuscarAsync` sin alternativas tira NRE, pero solo lo llama el caso de uso, que valida antes;
  - no reprodujo 4 de las mutaciones declaradas tal cual: probó variantes sobre las mismas líneas.
- **No re-verificó `1d6afc1` ni `d44bab7`**, que solo tocan tests: cada uno se verificó con mutación, y la suite final del auditor corre sobre `d44bab7`.

**auditor-persistencia sobre `ca84eae..1d6afc1` · NO APTO → ronda 1 → APTO sobre `d44bab7`.**
- **H1:** `Detalle_ConservaCostoNulo...` releía con el contexto de la siembra. La mutación que saca `AsNoTracking` y los `Include` de códigos y precios sobrevivía, porque el change tracker devolvía las instancias. **Arreglo `d44bab7`:** el test lee con un contexto nuevo y afirma la razón social y la sucursal. Ahora esa mutación da rojo ("collection was empty"). Quitar solo el `Include` de precios rompe 4 tests.
- No hay Ids sembrados. La búsqueda proyecta a DTO, así que el change tracker no la afecta. Los HTTP usan un contexto por request. El esquema de test es el real, `PostgreSqlSeguridadTests` sigue activo (8/8) y no hay `TRUNCATE`/`DROP` nuevos. Los conteos de `public` (124 / 2108 / 2644) no cambiaron con la suite.
- **Suite final sobre `d44bab7`: 258 total · 258 passed · 0 failed · 0 skipped · 0 warnings · 86 `[PostgreSqlFact]`.**
- Worktrees: los tres subagentes borraron el suyo a mano ("Filename too long") y corrieron `prune`. Solo queda el principal.

## Compuerta de esquema D1 · CERRADA (2026-10-14)

La cerraron dos scripts del equipo, aplicados a mano, que están en `backend/db/rescaffold/`:
- **`02_Modalidades_Precios.sql`** (en `lacentral_test` y en `lacentral`):
  - **`articulo_margen` YA NO EXISTE.** Tenía 0 filas en las dos bases, así que no hubo migración. La clave `numero` (las listas 1/2/3) desapareció con ella.
  - **`articulo.precio_venta_estimado` YA NO EXISTE.**
  - **Nueva `articulo_precio_modalidad`** `(articulo_id, modalidad, porcentaje, precio, calculo_manual, fecha_calculo, usuario_id)`.
    - PK `(articulo_id, modalidad)`. FK a `articulo` (ON DELETE CASCADE) y FK a `usuario`.
    - 13 restricciones: 7 CHECK, la PK, 2 FK y 3 NOT NULL.
    - `modalidad` es varchar(24), con el CHECK `articulo_precio_modalidad_valida` contra los seis nombres exactos del enum `Modalidad`.
    - `precio` NULL significa "no se pudo calcular", nunca cero. Con `calculo_manual = true` el recálculo no pisa el precio.
- **`03_Limpieza_Columnas_Fantasma.sql`** (solo en `lacentral_test`; en `lacentral` no hacía falta):
  - borra `articulo.codigo`, `descripcion` y `cantidad`, que tenían 0 de 2644 filas con dato.
  - Después de aplicarlo, `public.articulo` tiene 10 columnas en las dos bases.

En la aplicación (rama `feature/d1-precios-modalidad`):
- **Re-scaffold** `7801bbd`, contra `lacentral_test`, después de verificar en el catálogo que `articulo` tiene las 10 columnas de los scripts:
  - se generó `ArticuloPrecioModalidad`;
  - `ArticuloMargen.cs` se borró a mano, porque `--force` no lo hace;
  - `Articulo` perdió `PrecioVentaEstimado`;
  - las personalizaciones 1:N de los historiales siguen intactas.
- El comando está documentado en `backend/db/rescaffold/README.md`.
- **Los 42 rojos** tenían una sola causa: EF pedía `precio_venta_estimado`. Después del re-scaffold, la suite dio 176/176 sin tocar nada más. Ningún código C# usaba `PrecioVentaEstimado` ni `ArticuloMargen`.
- **Conversión texto → `Modalidad`:** `ModalidadPersistida.Leer`, en el mapeo del repositorio (`backend/LaCentral.Data/Repositorios/`).
  - Compara por nombre exacto y lanza `InvalidOperationException` ante un valor fuera de los seis.
  - Lo usa `ArticuloRepositorio.ObtenerDetallePorIdAsync`.
  - Test en rojo `91a705e` (L53 y L94, por aserción). Producción en `d8b6814`.
  - Mutación con `Enum.TryParse(ignoreCase)`: vuelve a rojo.
  - El sentido Modalidad → texto todavía no tiene quien escriba: entra con la primera escritura (Fase 3 o 6).
- **`Articulo.CalcularPrecioEstimado` sobrevive:** calcula en memoria y nunca se persistió. Ni en esta rama ni en ninguna otra, local o remota, hay un repositorio, controlador o caso de uso que escriba `PrecioVentaEstimado`.
  - Hasta `20e7c22` existió `PrecioVentaEstimado` como propiedad calculada del dominio, nunca mapeada a la columna.
  - Ojo para la Fase 3: su modelo (`MargenModalidad`, porcentaje sobre costo) sigue sin expresar Efectivo sobre Lista ni CasoEspecial. Ver "Premisas".

### Subagentes sobre `git diff dabcde4..1351393` (2026-10-14) · **los dos APTO**
- **verificador-tdd: APTO.** Trabajó en un worktree propio, que después borró.
  - `91a705e`: rojo en L53 y L94, por aserción y por la razón correcta. `d8b6814`: verde, 3/3.
  - Mutaciones: `Enum.TryParse(ignoreCase)` y "devolver `default`" dan rojo en L94.
  - Suite: 179/179, 0 warnings, 46 `[PostgreSqlFact]`.
  - No reprodujo los 42 rojos previos al re-scaffold. No revisó criterios de aceptación de HU.
- **auditor-persistencia: APTO.**
  - Contra el catálogo de `lacentral_test`, el modelo coincide con `articulo_precio_modalidad` y con `articulo` (comparado en las dos direcciones).
  - Relectura con contexto nuevo, sin Ids sembrados. El `DROP CONSTRAINT` del test no alcanza `public`.
  - Suite: 179/179.
  - Sin verificar: `lacentral`, que está fuera de su alcance.
- **Hallazgos del auditor.** No bloquean esta rama, que solo LEE. **Bloquean la fase que agregue la escritura en `articulo_precio_modalidad`** (Fase 3 o 6). **Los tres quedaron resueltos en la Fase 3 (2026-10-14): H1 `9d43f88`, H2 decisiones A y B, H3 `413308a`→`362b870`.**
  - **H1:** falta test contra el motor de 6 CHECK (`manual_sin_porcentaje`, `calculado_con_porcentaje`, `precio_con_fecha`, `manual_con_usuario` y los dos `*_no_negativo`), de la PK (23505), de las FK (23503) y del ON DELETE CASCADE.
  - **H2:** `MargenModalidad` (`backend/LaCentral.UseCases/Entidades/Articulo.cs:33-43`) no tiene `Precio`, `CalculoManual`, `FechaCalculo` ni `UsuarioId`. Un repositorio no puede cumplir los CHECK sin inventar datos. `CalcularPrecioEstimado` es una segunda regla de cálculo frente al `CalcularPreciosArticuloUseCase` que nombra la tabla.
  - **H3:** el test de lectura inserta porcentaje, manual, fecha y usuario, pero no los relee por valor (el DTO no los expone).
  - Menor: calificar con el esquema el `ALTER TABLE` del test de inválidos.

## Arreglos de los hallazgos del auditor (2026-10-14)

Cada arreglo lleva un commit `test(...)` con el rojo verificado y después un commit de producción.
La excepción es 2.2, que se explica en su punto.
- **2.1 · hallazgo 2, `usuario_baja_id`:** `a516caa` (test, rojo en L37 y L63: `Expected 1, Actual null`) → `83f0f7c` (fix).
  - Las dos lecturas de `UsuarioRepositorio` mapean `UsuarioBajaId`.
  - `ReactivarUsuarioUseCase` lo limpia de forma explícita, por decisión 2.2.
  - El tercer test (`Reactivar_LimpiaLaFila...`) es de caracterización: pasó en el commit de test.
- **2.2 · hallazgo 1, atomicidad:** `b789b0e`. Son 4 tests PG: DarDeBajaProveedor, DarDeBajaUsuario, ReactivarCliente y ReactivarUsuario.
  - **No hubo rojo contra el código actual, y no hay commit de producción.** `UnidadDeTrabajo` ya revertía. No se inventó un cambio para simular el rojo.
  - Los tests discriminan por mutación no commiteada: con `UnidadDeTrabajo` sin transacción, los 4 dan rojo por aserción.
  - Esto contradice el "dos commits por arreglo, sin excepción" de la instrucción. Se declara.
- **2.3 · hallazgo 3, `*_hist_fechas_coherentes`:** `f00a0ae` (test, rojo en L340 y L369) → `d378c0f` (fix).
  - La lista blanca suma los 4 CHECK (23514 → 409).
  - `MensajeDeConflicto` acepta la `PostgresException` directa de `ExecuteUpdate`.
  - `*_hist_motivo_no_vacio` **no** se tocó.
- **D1:** `aef1f83` agrega el DDL del motor a `informe-compuerta-d1.md` (ver ese archivo).

## Re-verificación de la Fase 2 por los subagentes (2026-10-14, sobre `git diff 108ac1d d378c0f`)

Los dos corrieron como `subagent_type` real, cada uno en su worktree. Los dos borraron su worktree al terminar.

### auditor-persistencia · **APTO**
- Suite en `d378c0f`: 176 · 176 passed · 0 skipped. Hay 43 tests contra el motor real: 42 con esquema aislado, confirmados por StdOut en el `.trx`, y 1 de catálogo.
- Verificó contra el motor (catálogo, solo lectura, `lacentral_test`):
  - `*_baja_coherente`, `*_hist_fechas_coherentes`, `*_hist_motivo_no_vacio` y `ux_*_hist_abierta`;
  - identidad `GENERATED ALWAYS`.
  - Todo coincide con `01_Ajustes_Sprint3.sql`. `base_completa.sql` no tiene las tablas de historial.
- Hallazgos 1, 2 y 3: **cerrados**. Las 6 operaciones tienen test PG de falla a mitad de camino:
  - las 3 bajas, con un 23505 real del motor;
  - las 3 reactivaciones, con una excepción forzada por un historial mockeado (repositorio, unidad de trabajo y contexto son reales);
  - además, ReactivarCliente tiene un 23514 real.
- No hizo mutaciones: la consigna no le permitía tocar código.
- Nuevos, ninguno bloqueante:
  - (1) Falta un test de que un CHECK **no** reconocido siga dando 500. Falta también un test HTTP de CHECK → 409.
  - (2) `DarDeBajaClienteUseCase.cs:49` y `DarDeBajaProveedorUseCase.cs:59` no hacen `Trim()` del motivo; usuario sí.
  - (3) En `lacentral_test` quedó un esquema huérfano, `pr110_test_e68bcde698414f5996fd57e4ddf46ef4` (22 tablas), de una corrida abortada. No se tocó.
- Sin verificar: que la base principal tenga aplicado `01_Ajustes_Sprint3.sql`.

### verificador-tdd · **NO APTO**: una sola causa, de documentación
- **Bloqueante:** `docs/sprint03/pr-fase2-d10-historial-baja.md` L46-50.
  - Afirma que "cada test nuevo se vio fallar por aserción antes del código de producción". Para los 8 commits originales, git no lo puede verificar.
  - La L50 admite el rojo contra stubs `NotImplementedException`, y eso contradice la L46.
  - No declara que el orden no se puede verificar.
  - No menciona los 5 commits nuevos, ni que `b789b0e` no tiene commit de producción.
  - Los conteos están desactualizados: dice 34 PG y hoy son 43.
  - Con eso corregido, el veredicto es APTO.
- Commits nuevos, verificados en worktree:
  - `a516caa`: rojo en L37 y L63, por aserción y por la razón correcta. `83f0f7c`: verde. Si se comenta `UsuarioBajaId = null` en la reactivación, el test de caracterización da rojo en L86: protege esa línea.
  - `f00a0ae`: rojo en L340 y L369. `d378c0f`: verde. Mutaciones del manejador: quitar un nombre o quitar la rama `PostgresException` directa → rojo.
  - `b789b0e`: el tratamiento le parece **aceptable**. Rojo con `UnidadDeTrabajo` sin transacción, y también con una mutación por caso de uso (cada uno saltea la unidad): cada test discrimina su propio caso de uso.
- Los 7 PG viejos de historial: **rojo comprobado por mutación hoy**.
  - Mutaciones: `AbrirAsync` que no escribe, `CerrarAsync` que devuelve true sin escribir, y `Rollback` cambiado por `Commit`.
  - Salvedad: en las dos primeras solo miró el [FAIL], no la causa.
- Orden test → producción de los 8 originales: sigue sin poder verificarse. b95744f es el único commit solo de test; e6a2d3b mezcla el test con el fixture; los otros 6 traen test y producción juntos.
  - Por sí solo **no** le impide dar APTO, si se declara en el PR.
- Build `--no-incremental`: 0 warnings. Suite: 176/176, 43 PG, sin `Skip`.
- No bloqueante: `*_hist_motivo_no_vacio` sin test ni lista blanca (o se cierra, o se declara). No hay test HTTP del 409 por CHECK.

**Estado:** se PARÓ en el punto 3, como indica la instrucción ante un NO APTO.
- No se reescribió el cuerpo del PR (punto 4).
- No se hizo push ni se abrió el PR (punto 7).

### verificador-tdd, segunda pasada (2026-10-14, sobre `git diff 108ac1d c4bbfd2`) · **APTO**
El equipo confirmó que se reescribiera el cuerpo del PR (`c4bbfd2`).
- El código no cambió desde `d378c0f`: el diff entre los dos commits toca solo 3 archivos de docs.
- Suite en `c4bbfd2`, en un worktree propio que después borró: 176 · 176 passed · 0 skipped. Build con 0 warnings. 43 `[PostgreSqlFact]`.
- Revisó el cuerpo del PR afirmación por afirmación y no encontró nada falso.
  - Volvió a correr hoy la mutación de `b789b0e` (`UnidadDeTrabajo` sin transacción): rojo en L237, L261, L286 y L311.
  - Comprobó con `git show --stat` la clasificación de los 8 commits originales.
- Marcó dos imprecisiones, ya corregidas en el cuerpo:
  - "solo el historial es un doble": la sesión (`IContextoUsuario`) también lo es;
  - el veredicto sin asentar.
- No repitió las mutaciones anteriores, porque el código es el mismo.

**Fase 2: los dos subagentes dan APTO.** Desbloqueado el push y el PR.

### Limpieza (2026-10-14)
- Se eliminaron el worktree huérfano `.../9c481e3d-.../scratchpad/wt` (`git worktree remove --force` + `prune`) y los worktrees de los subagentes. Queda solo el principal.
- Se borró el esquema huérfano `pr110_test_e68bcde698414f5996fd57e4ddf46ef4` de `lacentral_test`. Antes se verificó lo siguiente:
  - el nombre cumple `^pr110_test_[0-9a-f]{32}$`;
  - la base es `lacentral_test`;
  - las 22 tablas son copias de tablas de `public`;
  - no tenía locks activos;
  - las únicas dependencias externas eran sus 9 tablas TOAST, sin FKs entre esquemas.
  - El borrado se hizo con `DROP SCHEMA ... CASCADE`, en una transacción con `lock_timeout`. Después de borrarlo quedan 0 esquemas `pr110_test_*`.

## Verificación de la Fase 2 por los subagentes (2026-10-14)

Corridos como `subagent_type` real, sobre `git diff b95744f~1 a105f6d` (los 8 commits de la
Fase 2). Cada uno usó un worktree aparte en `a105f6d`. El commit `d348ae4` es solo docs y
config y no forma parte del diff. Hasta esta fecha ningún subagente había corrido sobre la
Fase 2. El "ver ESTADO.md" del cuerpo del PR remite acá.

**Causa común:** la conexión de test está caída. `LACENTRAL_TEST_PG` apunta a
`Host=100.125.169.91;Database=lacentral_test;Username=lacentral`. Tailscale está activo y el
host responde, pero el servidor rechaza la contraseña:
`28P01: password authentication failed for user "lacentral"`. El valor de la variable es el
mismo en el proceso y en el registro de usuario. No se probaron otras credenciales ni la
base principal.

### verificador-tdd · **NO APTO**
- `dotnet test LaCentral.slnx` en el worktree: **167 · 133 passed · 34 failed · 0 skipped**. Los
  34 son exactamente los `[PostgreSqlFact]` y fallan por 28P01 sin llegar a una aserción.
  `[PostgreSqlFact]` falla en vez de saltearse, como se definió en 1.7. **PG real aprobados: 0.**
- No objeta los tests en sí. **Rojo comprobado por mutación, solo en Moq:** 8 experimentos.
  Edita una línea de producción en el worktree, corre el test solo y restaura. Los 8 dan
  rojo por aserción, ninguno por compilación ni por NullReference:
  - 2.3: ignorar el `false` de `ActualizarAsync`, en `ModificarProveedorUseCase:57` y en `DarDeBajaProveedorUseCase:65`.
  - 2.5: volver a filtrar las direcciones vacías en `ModificarProveedorUseCase:55`.
  - 2.1: anular `AbrirAsync` en `DarDeBajaCliente` y `CerrarAsync` en `ReactivarProveedor`, y sacar una escritura fuera de la unidad de trabajo.
  - 2.2: usuario fijo en lugar de `IContextoUsuario`.
  - 2.8: mensaje sin el dueño del código.
- Ningún test repite fórmulas de producción. Los ítems 2.1 a 2.8 y los cuatro tests
  obligatorios contra PostgreSQL existen en el código: `HistorialBajaRepositorioTests` L37, L76,
  L117, L164, L191 y L217. No corrieron.
- **Orden test → producción no verificable con git:** 7 de 8 commits traen test y producción
  juntos. Lo de "se vio fallar antes" es una declaración del autor.
- **Rojo inválido declarado:** los 7 PG de historial "fallaron contra stubs
  `NotImplementedException`". Una excepción no es un rojo por aserción. Pendiente con PG:
  verlos rojos con una `UnidadDeTrabajo` sin transacción y con un `AbrirAsync` que no escriba.
- Menor: la guarda `duenioDelCodigo.Id != cliente.Id` de `ModificarClienteUseCase` (a105f6d) no
  tiene test y probablemente no se alcanza.
- Sin verificar: los 34 PG, incluidos 2.4 y 2.6 del lado del repositorio y la lista blanca 2.7.

### auditor-persistencia · **NO VERIFICADA contra el motor**
- **Contra el motor no verificó nada.** Las consultas de catálogo y los tests PG fallaron
  por 28P01. `PostgreSqlSeguridadTests` dio 8/8 en verde y sigue activo. `dotnet build --no-incremental`:
  0 warnings, 0 errores. El esquema se comparó contra los scripts de `backend/db/`, no
  contra la base.
- Por lectura de código sostiene lo siguiente:
  - `UnidadDeTrabajo` abarca los `SaveChanges` y el `ExecuteUpdate`, porque comparten el DbContext scoped.
  - Los tests PG releen con un contexto nuevo y `AsNoTracking`.
  - No hay Ids sembrados en entidades persistidas.
  - El fixture copia CHECKs, identity e índices, y renombra los índices sin tocar la lista blanca.
  - El único `DROP` es `DROP SCHEMA ^pr110_test_[0-9a-f]{32}$`.
- Hallazgos (detalle en "Deuda nueva detectada"):
  1. Atomicidad: hay test de falla intermedia solo para `DarDeBajaCliente`, `ReactivarProveedor` y el fallo devuelto. Faltan en `ReactivarCliente`, `DarDeBajaProveedor`, `DarDeBajaUsuario` y `ReactivarUsuario`.
  2. `usuario.usuario_baja_id` se pierde en silencio. **Confirmado** en `backend/LaCentral.Data/Repositorios/UsuarioRepositorio.cs`: las lecturas (L56, L76) no lo mapean y `ActualizarAsync` (L98) lo escribe.
  3. CHECKs de historial sin test contra el motor: `*_hist_fechas_coherentes` (23514, hoy saldría 500) y `*_hist_motivo_no_vacio`.
  4. Concurrencia probada solo a nivel repositorio, no con dos `DarDeBaja*` en transacción.
  5. Menor: los dobles `ProveedorInactivo()` y `UsuarioInactivo()` de `HistorialBajaCasosDeUsoTests` modelan un estado que el CHECK prohíbe.

**Conclusión:** la Fase 2 **no** cumple la compuerta "el PR no se abre hasta que los dos den
APTO". Sin conexión no hay evidencia de que la Fase 2 persista bien. Que los 34 fallen no
prueba que la Fase 2 esté mal.

## Decisiones tomadas durante el sprint
(el agente las asienta acá, con fecha)
- 2026-10-13 · Plan corregido por el equipo: la Fase 2 se redefine como extracción de `IHistorialBajaRepositorio` (más artículo y usuario); en la Fase 4 se verifica el caso exacto "100%_original"; se agregan los ítems 1.7, 1.8 y 1.9 a la Fase 1.
- 2026-10-13 · 1.3: `ObtenerPorCodigoAsync` en lugar de `ExisteCodigoAsync`, porque el criterio exige informar CUÁL proveedor está en conflicto.
- 2026-10-13 · 1.7: `[PostgreSqlFact]` nunca se omite y `LA_CENTRAL_REQUIRE_POSTGRES` deja de leerse: exigir PostgreSQL es el único comportamiento. Se documenta en `LaCentral.Tests/README.md`.
- 2026-10-13 · 1.8: la causa del conflicto era `EFCore.Design` (PrivateAssets) en Data. Se agrega Relational 10.0.11 explícita en Data, que se propaga a Api y Tests.
- 2026-10-13 · 1.9: el warning de ClienteRepositorio era `ClienteDireccion.Calle`, no `Cuit`. Se sigue la convención documentada del repositorio (sin dato = "" en el dominio, NULL en la base) en vez de `List<string?>`. La sincronización compara con la misma convención: sin eso, cada baja o reactivación duplicaba la dirección.
- 2026-10-13 · 1.5 queda sin test (ningún código usa la entidad `Articulo`) y 1.6 es un refactor en verde. Los dos verificadores lo aceptaron.
- 2026-10-13 · Los subagentes verificador-tdd y auditor-persistencia no cargan como tipo en una sesión iniciada en `backend/` (están en `Tesis/.claude/agents/`). Se corrieron como agentes generales con su definición textual. Los dos dieron APTO.
- 2026-10-13 · **Fase 2, base:** la Fase 1 no estaba mergeada en develop. La Fase 2 sale de `fix/deuda-bloqueante-sprint3` (`108ac1d`) con base de PR provisoria. Cuando la Fase 1 entre a develop, cambiar la base y revisar que el diff sean solo los 8 commits `b95744f`..`a105f6d`. Sigue sin mergear al 2026-10-14.
- 2026-10-13 · **Fase 2, previo:** `ArticuloTest.cs` y `ArticuloPrecioEstimadoTest.cs` no se contradecían. Los dos prueban `costo × (1 + margen/100)`, y el 0,01 % era un caso de precisión sobre Lista, no la vieja regla de Costo. Se unificaron en `Articulos/ArticuloPrecioEstimadoTests.cs` y se eliminó un duplicado.
- 2026-10-13 · **2.1:** además de `IHistorialBajaRepositorio` (`AbrirAsync`, `CerrarAsync`, `ObtenerAsync`) se agrega el puerto `IUnidadDeTrabajo`. La baja o reactivación y su historial ocurren en una transacción, y un fallo devuelto o una excepción descartan las dos escrituras.
- 2026-10-13 · **2.2:** antes, ninguna escritura guardaba `usuario_baja_id` (respuesta a la pregunta del plan). Sin usuario en sesión, la baja o reactivación de cliente y proveedor da NoAutorizado, igual que la de usuario. `FechaReactivacion` la fija el caso de uso.
- 2026-10-13 · **2.2:** `ReactivarUsuarioUseCase` sigue limpiando `motivo_baja` y `fecha_baja` de la fila de usuario (D10 lo permite) y el rastro queda en `usuario_historial_baja`. Cliente y proveedor los conservan en la fila.
- 2026-10-13 · **2.2:** proveedor no tiene columna `usuario_baja_id`. El usuario queda solo en `proveedor_historial_baja`. No se tocó el esquema.
- 2026-10-13 · **Artículo:** el puerto soporta `articulo_historial_baja` y tiene test PG, pero no hay caso de uso de baja de artículo. Queda para HU-ART-03 (Fase 7).
- 2026-10-13 · **2.5:** el repositorio conserva la dirección sin calle que ya existía y no crea una fila vacía nueva. El filtro se sacó del caso de uso.
- 2026-10-13 · **2.6:** el fixture aparea los índices copiados por tabla, unicidad y definición, y los renombra al original. Si hay ambigüedad, lanza. No se agregaron nombres generados a la lista blanca.
- 2026-10-14 · `.claude/` (agentes y `settings.json`) y `CLAUDE.md` se movieron de `backend/` a la raíz del repo, que también es la raíz de git y del grafo. Verificado en una sesión iniciada en la raíz:
  - `verificador-tdd` y `auditor-persistencia` cargan como `subagent_type` real (sin "Agent type not found").
  - Los hooks `graphify hook-guard` se disparan en Read/Glob y en Bash/Grep.
- 2026-10-14 · `CLAUDE.md`: rutas corregidas para que sean relativas a la raíz (`backend/LaCentral.Data/Models/`, `backend/LaCentral.Data/LaCentralDbContext.Personalizaciones.cs`, `backend/LaCentral.Tests/Infraestructura/PostgreSqlPruebas.cs`, `backend/LaCentral.Api/Program.cs`, `backend/LaCentral.UseCases/LaCentral.UseCases.csproj`, `graphify-out/GRAPH_REPORT.md`). Además se aclara dónde está la solución y dónde corre dotnet.
- 2026-10-14 · `.gitignore`: `settings.json` ya estaba versionado (`d348ae4`), pero la regla `.claude/settings.json` lo seguía marcando como ignorado. Se reemplaza por `.claude/settings.local.json`, para los ajustes de cada máquina. Ojo: `settings.json` tiene la ruta absoluta de `graphify.exe` de una sola máquina.
- 2026-10-14 · **D1, conversión texto ↔ `Modalidad`:** va en el mapeo del repositorio (`ModalidadPersistida`), no como value converter. El modelo scaffoldeado declara `string Modalidad`, y un value converter necesita que la propiedad CLR sea el enum: para eso habría que editar `Models/` o ignorar la propiedad generada y agregar otra a mano. Justificación completa en `docs/sprint03/pr-d1-precios-modalidad.md`.
- 2026-10-14 · **D1:** `ObtenerDetallePorIdAsync` sin filas en `articulo_precio_modalidad` sigue devolviendo `PreciosPorModalidad = null`. Qué mostrar cuando falta una modalidad, y la razón de la ausencia, lo define HU-ART-02 (Fase 4).

## Deuda nueva detectada
(el agente la asienta acá, con archivo y línea)
- ~~Tests PG omitidos sin conexión; conflicto EF Relational; 3 warnings de nulabilidad~~: resueltos en la Fase 1 (1.7, 1.8, 1.9).
- ~~2026-10-13 · **Fase 2:** las escrituras de historial no setean `usuario_baja_id` ni `usuario_reactivacion_id`. `Cliente.UsuarioBajaId` no se persiste. `FechaReactivacion` la estampa el repositorio. Usuario no escribe `usuario_historial_baja`.~~ Resuelto en la Fase 2 (2.1, 2.2), salvo la verificación PG pendiente. Lo que queda está en las entradas del 2026-10-14.
- ~~2026-10-13 · **Fase 2:** `PostgreSqlPruebas.cs` (`LIKE ... INCLUDING ALL`) renombra los índices únicos en el esquema aislado.~~ Resuelto en la Fase 2 (2.6). El test que lo prueba (`EsquemaAislado_ConservaLosNombresDeIndicesDelOrigen`) no se pudo re-correr el 2026-10-14.
- ~~2026-10-13 · **Alta:** `ProveedorRepositorio.ActualizarAsync` compara `b.Calle == d`: una dirección sin calle se duplica en cada actualización.~~ Resuelto en la Fase 2 (2.4).
- ~~2026-10-13 · `ModificarClienteUseCase` y `ModificarProveedorUseCase` filtran las direcciones vacías: modificar borra una dirección sin calle junto con su localidad.~~ Resuelto en la Fase 2 (2.5).
- ~~2026-10-13 · `ProveedorRepositorio.ActualizarAsync` devuelve void y sale en silencio si no encuentra la fila.~~ Resuelto en la Fase 2 (2.3).
- ~~2026-10-13 · `ManejadorExcepcionesGlobal`: a la lista blanca le faltan `ux_factura_venta_numero_activa`, `cliente_telefono_cliente_id_numero_key` y `proveedor_telefono_proveedor_id_numero_key`.~~ Resuelto en la Fase 2 (2.7).
- 2026-10-13 · No hay test de falla a mitad de camino en altas y actualizaciones de cliente y proveedor. **Parcial (2026-10-14):** las altas y modificaciones siguen siendo un solo `SaveChanges`, atómico por construcción. La Fase 2 agregó escrituras en baja y reactivación, y solo 2 de esas 6 operaciones tienen test de falla (ver hallazgo 1 del auditor, abajo).
- 2026-10-13 · `CicloCompletoIntegracionTests` mockea los repositorios: prueba el pipeline HTTP, no la persistencia. El nombre promete más de lo que verifica. El recorrido real contra PostgreSQL es de la Fase 7. **Desde la Fase 2 también mockea el historial y la unidad de trabajo.**
- 2026-10-13 · La baja de usuario solo tiene tests InMemory o con mocks. **Parcial (2026-10-14):** desde la Fase 2 hay camino feliz PG (`HistorialBajaRepositorioTests`, usuario), todavía sin ejecutar con éxito. Falta el test de falla. `backend/LaCentral.Tests/Data/UsuarioRepositorioTests.cs` sigue en InMemory y solo prueba "Id inexistente → false".
- ~~2026-10-13 · **Compuerta de esquema para las Fases 3 y 6:** no hay dónde persistir precios por modalidad con nombre (D1). `articulo_margen` usa la clave `numero` (listas 1/2/3) y hay una sola columna `precio_venta_estimado`.~~ **CERRADA el 2026-10-14** por `02_Modalidades_Precios.sql` y `03_Limpieza_Columnas_Fantasma.sql`. `articulo_margen` ya no existe. Ver "Compuerta de esquema D1 · CERRADA".
- 2026-10-13 · EF Core difiere entre Data (10.0.11) y Api/Tests (10.0.12, por InMemory y Mvc.Testing). La Api referencia `EntityFrameworkCore.InMemory` en producción.
- 2026-10-13 · Pendiente de confirmar por el usuario: si existe `ConnectionStrings__Postgres` en el entorno, a qué base apunta. (Confirmado por el equipo el 2026-10-13: no existe. Ver la revisión de la Fase 1.)
- ~~2026-10-14 · **BLOQUEANTE · conexión de test:** el servidor rechaza la contraseña de `LACENTRAL_TEST_PG` (`28P01`, usuario `lacentral`, base `lacentral_test`, host `100.125.169.91`).~~ Resuelto el 2026-10-14: era una credencial vieja en la variable de entorno, no el contenedor ni el código. La suite da 167/167, con los 34 PG en verde.
- 2026-10-14 · **Fase 2, auditor 1 · atomicidad:** sin test PG de falla intermedia en `ReactivarClienteUseCase` (L42-49), `DarDeBajaProveedorUseCase` (L63-71), `DarDeBajaUsuarioUseCase` (L98-110) y `ReactivarUsuarioUseCase` (L52-59). Los tests Moq `*_EscriturasOcurrenDentroDeLaUnidadDeTrabajo` prueban que se pasa por la unidad, no que se revierta.
- 2026-10-14 · **Fase 2, auditor 2 · pérdida silenciosa de auditoría:** `backend/LaCentral.Data/Repositorios/UsuarioRepositorio.cs`, lecturas en L56 y L76 sin `UsuarioBajaId`. `ActualizarAsync` (L98) lo escribe. Cualquier actualización de un usuario inactivo pone `usuario.usuario_baja_id` en NULL, por ejemplo `RestablecerContrasenaUseCase.cs:43`. `ReactivarUsuario` lo anula por accidente del mapeo, no por decisión. Ningún test PG relee esa columna. El historial sí conserva el dato.
- 2026-10-14 · **Fase 2, auditor 3:** `CerrarAsync` (`backend/LaCentral.Data/Repositorios/HistorialBajaRepositorio.cs:50`) no valida `fecha_reactivacion >= fecha_baja`. Con una `fecha_baja` futura (backfill de Multisoft o desfase de reloj), el CHECK `*_hist_fechas_coherentes` da 23514, que hoy sale como 500. Ni este CHECK ni `*_hist_motivo_no_vacio` tienen test contra el motor.
- 2026-10-14 · **Fase 2, auditor 4:** las bajas concurrentes se prueban solo a nivel repositorio (dos `AbrirAsync` en autocommit). Falta un test con dos `DarDeBaja*` en transacción: el segundo debe dar 23505, devolver 409 y revertir el UPDATE de la fila.
- 2026-10-14 · **Fase 2, menores:**
  - Verificador: la guarda `duenioDelCodigo.Id != cliente.Id` de `ModificarClienteUseCase` no tiene test y probablemente no se alcanza.
  - Auditor: los dobles `ProveedorInactivo()` y `UsuarioInactivo()` de `backend/LaCentral.Tests/Comun/HistorialBajaCasosDeUsoTests.cs` tienen motivo y no tienen fecha, un estado que el CHECK prohíbe.
  - Verificador: los 7 PG de historial vieron su rojo solo contra stubs `NotImplementedException`.
- 2026-10-14 · Worktree huérfano de una sesión anterior registrado en git: `C:/Users/Santi/AppData/Local/Temp/claude/C--Users-Santi-Desktop-ESCMB-UNC-Tesis-Tesis-backend/9c481e3d-.../scratchpad/wt` (`a105f6d`, detached). No se tocó. Para quitarlo: `git worktree remove --force <ruta>` o, si falla por la ruta larga, borrar la carpeta y `git worktree prune`.
- ~~2026-10-14 · `docs/entorno-local.md` está versionado vacío (0 bytes, `d348ae4`).~~ Tiene contenido desde `74f47ed`. Su sección 3 todavía dice "17 tests" en rojo sin la variable; hoy son 46.
- 2026-10-14 · **DEUDA DE PROCESO · la base de pruebas se fue de los scripts sin que nadie lo notara.**
  - `lacentral_test` tenía `articulo.codigo`, `descripcion` y `cantidad`, que no estaban en ningún script. Las dejó una carga de Multisoft contra test.
  - **Producción NO estaba derivada: `lacentral` coincidía con los scripts (10 columnas). La que se fue de los scripts era la base de PRUEBAS.**
  - El scaffold se hace contra `lacentral_test`. Si test difiere de producción, el modelo generado describe una base que no existe: es la misma clase de error que un test sobre InMemory.
  - **Regla:** antes de cada re-scaffold, comparar el esquema de test contra el de producción (consulta en `backend/db/rescaffold/README.md`). Si difieren, no se scaffoldea.
  - Agravante: el fixture de `PostgreSqlPruebas.cs` (`ValidarTablaAsync`) compara en un solo sentido. Detecta columnas que EF mapea y que faltan en la base, pero no columnas que sobran en la base. No es una red para esto.
  - La deriva apareció solo porque se miró el diff del scaffold.
- 2026-10-14 · **Código manual dentro de la carpeta generada:** `backend/LaCentral.Data/Models/HistorialesBaja.Personalizaciones.cs`.
  - Contradice la convención: el código manual va fuera de `Models/` (ver `CLAUDE.md`).
  - El scaffold no lo toca, pero es fácil borrarlo al limpiar `Models/`.
  - **No se movió en la rama de D1:** es un cambio aparte.
- 2026-10-14 · `d8b72c9` dice "limpieza de columnas fantasma" en el mensaje, pero solo renombra el 02. El script 03 se versionó después, en `09f4bc1`.
- 2026-10-14 · `PrecioModalidadConsultaDto.Modalidad` ahora es el enum. Cuando la Fase 4 lo exponga por HTTP, hay que serializarlo por nombre (`JsonStringEnumConverter`), no como número. **Desde la Fase 3 también `Base` (`BasePrecio`).**
- 2026-10-14 · **Fase 3 · sin entregable visible:** `POST /api/articulos` no tiene pantalla, porque `frontend/` no existe en el repo. El requisito permanente del plan, agregado el mismo día, no se cumple para esta fase.
- 2026-10-14 · **Fase 3:** no hay un endpoint para fijar el precio de CasoEspecial ni las tasas de Tarjeta3/6. Las filas nacen con precio null y `calculo_manual` en true o con base NULL. Quedan para una HU que nadie tomó todavía.
- 2026-10-14 · **Fase 3:** el plan dice "PARÁS si un test pasa a la primera". El equipo lo reemplazó en esta fase por "se declara y se verifica con mutación". Hay que decidir si el plan cambia o si fue una excepción.
- 2026-10-14 · Hay 17 ramas locales viejas, mergeadas en develop, que no son de este sprint (`feature/hu-cli-*`, `test/*`, `fix/auditoria-*`, etc.). No se borraron.
- 2026-10-14 · La comparación test/producción del 04 se hizo con la huella md5 que corrió el equipo. El agente no tiene acceso a `lacentral`, así que esa comparación siempre depende del equipo.
- ~~2026-10-14 · Fase 3 · sin entregable visible~~ y ~~`Modalidad`/`Base` por nombre~~: resueltos en la Fase 4 (`frontend/`, `JsonStringEnumConverter`).
- 2026-10-15 · **Fase 4 · `SinCosto` cubre "con base y sin precio".** Hoy eso solo pasa cuando falta el costo. Si alguna vez aparece un artículo con costo y una modalidad con base pero sin precio (un recálculo que no corrió), la ficha lo mostraría como `SinCosto`. En `lacentral_test` hay 0 casos: 2644 artículos, ninguno con costo ni con filas de precio, consultado en solo lectura. **La Fase 6 tiene que garantizar que una compra recalcule en la misma operación.**
- 2026-10-15 · **Fase 4:** `ArticuloRepositorio.BuscarAsync` (L88) ya no valida. Con una consulta sin alternativas, `coincidencias!` es null y tira NRE. Hoy no se alcanza: el único llamador es el caso de uso, que valida antes. El contrato del puerto no lo dice.
- 2026-10-15 · **Auditor · esquema huérfano en `lacentral_test`:** `pr110_test_d4b8a811bf274559bf658b967f26eb7a`, con 1 tabla. Ya estaba antes de la corrida del auditor; probablemente es un fixture cortado a mitad de la creación. No se borró (R3). Lo borra el equipo a mano.
- 2026-10-15 · **Auditor · seguridad de datos:** `lacentral_test.public` tiene 124 proveedores y 2108 clientes, los mismos conteos que `CLAUDE.md` da para los datos reales. Parece una copia de datos reales, en el mismo servidor y con el mismo rol `lacentral` (SUPERUSER) que la base principal. `ValidarBase` solo mira el nombre de la base. La suite no modifica `public`. Lo decide el equipo.
- 2026-10-15 · Los subagentes no pueden borrar su worktree con `git worktree remove` en Windows ("Filename too long", por la ruta larga del scratchpad): lo borran a mano y corren `prune`. Habría que usar una ruta más corta o activar `core.longpaths`.
- 2026-10-15 · `.claude/agents/verificador-tdd.md`, encabezado de PRESUPUESTO: dice "la suite es caraCorrer", le falta un salto de línea. No se tocó porque la definición es del equipo.

## Premisas del plan que no coinciden con el código (revisar antes de la fase)
- 2026-10-13 · **Fase 2:** el plan dice "NINGÚN repositorio escribe en [las tablas de historial]". Falso para cliente y proveedor: `LaCentral.Data/Repositorios/ClienteRepositorio.cs:176-191` y `ProveedorRepositorio.cs:154-169` ya abren y cierran la baja en `*_historial_baja` dentro del repositorio, sin puerto. Lo cubren los tests PG `DosCiclos..._ConservanEstadoEHistorial...`. Artículo y usuario no escriben historial. La Fase 2 pasa a ser extraer un puerto `IHistorialBajaRepositorio` y mover esa lógica, más los usuarios de baja/reactivación desde IContextoUsuario, no construirla desde cero. (Aplicada en la Fase 2.)
- 2026-10-13 · **Fase 4:** el plan dice que el test del comodín ("100%_original") corre sobre InMemory. Hoy `ArticuloRepositorioTests.Busqueda_PermiteInactivosYNoInterpretaComodinesSQL` es `[PostgreSqlFact]` y corrió contra PostgreSQL real. Verificar si cubre ese caso exacto antes de duplicarlo. **Resuelto en la Fase 4 (2026-10-15):** usaba el texto exacto pero no discriminaba. Se agregó `Busqueda_100PorcientoGuionBajoOriginal_TraeSoloElLiteral`.
- 2026-10-14 · **Fases 3, 4 y 6 (D1):** ~~el comentario de `articulo_margen` todavía dice "0,01 % = precio igual al costo", mientras que D1 dice Costo = × 1,00~~ (la tabla ya no existe, 2026-10-14). **Resuelto en la Fase 3 (2026-10-14):** `MargenModalidad` y `CalcularPrecioEstimado` ya no existen. Ahora están `PrecioModalidad`, `BasePrecio` y `ReglasPrecioModalidad`, con Efectivo sobre PrecioLista y CasoEspecial manual. `CalcularPreciosArticuloUseCase` sigue sin existir: es de la Fase 6. Lo que sigue es el texto original. **Seguía en pie:** la tabla nueva tiene dónde guardar `precio` y `calculo_manual`, pero el dominio no se alineó todavía: eso es de la Fase 3. Además, el comentario de la tabla nombra un `CalcularPreciosArticuloUseCase` que todavía no existe. El modelo "porcentaje sobre costo" de `MargenModalidad`/`CalcularPrecioEstimado` no expresa Efectivo (sobre Lista), CasoEspecial (precio fijo) ni la razón de la ausencia. Detalle en `informe-compuerta-d1.md`.

## Revisión de la Fase 1 por el equipo (2026-10-13)
- Fase 1 verificada contra el grafo (`bf845dbc`): los nueve ítems están. El PR todavía no se abrió (no hay `gh`): abrirlo a mano desde la rama `fix/deuda-bloqueante-sprint3` con el cuerpo de `pr-fase1-deuda-bloqueante.md`.
- Confirmado: 1.7 (sin `LA_CENTRAL_REQUIRE_POSTGRES`), 1.3 (`ObtenerPorCodigoAsync`) y 1.9 (era `ClienteDireccion.Calle`).
- La compuerta de esquema de modalidades es de las Fases 3 Y 6: ART-01 da de alta la configuración de precios. **La resuelve el equipo con un script SQL antes de la Fase 3. El agente NO toca el esquema.**
- `ConnectionStrings__Postgres` NO existe en el entorno (verificado en las tres carpetas): los tests HTTP no resuelven ningún DbContext real. `LACENTRAL_TEST_PG` ahora es persistente.
- Los subagentes y los hooks de graphify se copiaron a `backend/.claude/` (agents/ y settings.json). En la sesión de la Fase 1 NO cargaron, porque se leen al iniciar la sesión: `Agent type 'verificador-tdd' not found`. Se reinicia la sesión antes de la Fase 2. (Superado el 2026-10-14: la configuración vive en la raíz y carga.)

## PRÓXIMA SESIÓN — arrancar acá
Arrancar en la **raíz del repo**. Objetivo: **Fase 5 · HU-FCO-01 · Factura de compra (Issue #32)**, rama `feature/HU-FCO-01-factura-compra`.

0. **Al empezar:**
   - confirmar que `git worktree list` muestra solo el principal;
   - confirmar que `dotnet test` da 258/258 con 86 PG;
   - ¿el equipo borró el esquema huérfano `pr110_test_d4b8a811...`? (ver "Deuda").
1. **PRs abiertos a mano, sin `gh`:**
   - Fase 3, `feature/HU-ART-01-alta-articulo` → `develop`. Cuerpo en `pr-fase3-hu-art-01.md`.
   - Fase 4, `feature/HU-ART-02-consultar-articulos` → base provisoria Fase 3. Cuerpo en `pr-fase4-hu-art-02.md`. **Cambiar la base a `develop` cuando entre la Fase 3.**
2. **Antes de la rama:** ¿las Fases 3 y 4 ya están en develop? Si no, PREGUNTAR de qué rama sale la Fase 5. Necesita al menos el alta de artículo de la Fase 3.
3. **Fase 5** (detalle en `PLAN_SPRINT3.md`, FASE 5):
   - es la fase con el test de atomicidad más importante del sprint;
   - entregable visible en `frontend/` (ya existe: Angular, ver `frontend/README.md`).
   - Al cerrar: los dos subagentes (APTO los dos), el PR, este archivo, y PARAR.
