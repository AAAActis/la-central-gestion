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
| 3 · HU-ART-01 | **cerrada (2026-10-14)**, con el script 04, las decisiones A-D y H1/H3. Ronda 1 de arreglos: **verificador APTO**, auditor APTO (pasada anterior). Ver "Fase 3" | `feature/HU-ART-01-alta-articulo` (sale de develop `f3c6705`, pusheada; código hasta `a9e7410`) | **#117, MERGEADO en develop (`c958901`)**. Cuerpo en `docs/sprint03/pr-fase3-hu-art-01.md` | **234 total · 234 passed · 0 failed · 0 skipped · 0 warnings (77 `[PostgreSqlFact]`), en `a9e7410`** | el entregable en `frontend/` lo cubre la Fase 4 (pantalla de alta incluida) |
| 4 · HU-ART-02 | **hecha (2026-10-15)**: búsqueda, ficha con las seis modalidades y su razón, GET por HTTP, enums por nombre y `frontend/` en Angular. **Verificador APTO, auditor APTO (ronda 1)**. Ver "Fase 4" | `feature/HU-ART-02-consultar-articulos` (sale de `feature/HU-ART-01-alta-articulo` `ca84eae`, pusheada; HEAD de código `d44bab7`) | **#118, MERGEADO en develop (`56c1e3e`, 2026-10-15 22:27)**. Cuerpo en `docs/sprint03/pr-fase4-hu-art-02.md` | **258 total · 258 passed · 0 failed · 0 skipped · 0 warnings (86 `[PostgreSqlFact]`), en `d44bab7`** | deuda abajo |
| — · Merge | **Fases 3 y 4 mergeadas en develop** (2026-10-15): `c958901` (#117, Fase 3) y `56c1e3e` (#118, Fase 4, 22:27). `develop` = `56c1e3e` | | | | |
| 5 · HU-FCO-01 | **cerrada (2026-10-15)** tras la ronda 4 (autorizada por el equipo, solo M12): **verificador APTO** (`26c02fc..f424771`), **auditor APTO** (`f34bbc4..b4fca2f`; las rondas 3 y 4 son solo de test). Ver "Compuerta de la Fase 5" | `feature/HU-FCO-01-factura-compra` (sale de `a23bb26`, que ya está en develop; pusheada, HEAD de código `f424771`) | **#119, MERGEADO en develop** (`d9ab678`; develop = `a47a866` con #120). `Refs #32` (no lo cierra). Cuerpo en `docs/sprint03/pr-fase5-hu-fco-01.md` | **325 total · 325 passed · 0 failed · 0 skipped · 0 warnings (109 `[PostgreSqlFact]`), en `f424771`**. En `0190052`: 324 con **108** PG (el "109" que figuraba contaba una mención en un comentario) | — |
| 6 · HU-ART-05 + FCO-01 CA5 | **cerrada (2026-10-16)** y **cambios pedidos por Santi en el PR #121 aplicados (2026-10-16)**: NUL → 400, `CalcularPreciosArticuloUseCase` única regla, Interpretación pendiente · Lista manual → Efectivo (sin confirmar), recálculo al comprar en la misma unidad de trabajo, O1 resuelta. Ver "Fase 6" | `feature/HU-ART-05-precios-modalidad` (sale de develop `a47a866`; pusheada) | **#121, MERGEADO en develop (`0580a16`)**. Refs #22 · Closes #32 | **353 total · 353 passed · 0 failed · 0 skipped · 0 warnings (119 `[PostgreSqlFact]`), en `c575e4e`** | revisión de Santi; confirmar con Alfredo la Interpretación pendiente · Lista manual → Efectivo |
| — · Merge | **Fase 6 mergeada en develop** (#121). `develop` = `0580a16`, suite esperada 353/353, 119 `[PostgreSqlFact]` (re-corrida el 2026-10-16 al empezar la Fase 7: 353/353, 0 omitidos, 0 warnings) | | | | |
| 7 · HU-ART-03 | **cerrada (2026-10-17)**: modificación, códigos, retiro (solo admin), baja y reactivación, rol de la sesión y `frontend/`. Ronda 1 (B1, B2 del auditor). Rol sin permiso → 403 (Javi). **Verificador APTO, auditor APTO.** CA4 sin implementar (falta regla). Ver "Fase 7" | `feature/HU-ART-03-modificar-articulo` (sale de develop `0580a16`, pusheada) | **sin abrir** (a mano, sin `gh`). `Refs #17`. Cuerpo en `docs/sprint03/pr-fase7-hu-art-03.md` | **431 total · 431 passed · 0 failed · 0 skipped · 0 warnings (145 `[PostgreSqlFact]`), en `aa2e795`** | CA4; deuda N2-N5 |

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
- 2026-10-14 · **D1:** `ObtenerDetallePorIdAsync` sin filas en `articulo_precio_modalidad` sigue devolviendo `PreciosPorModalidad = null`. Qué mostrar cuando falta una modalidad, y la razón de la ausencia, lo define HU-ART-02 (Fase 4). **Superado en la Fase 4 (abajo).**
- 2026-10-15 · **Fase 4 · validación de la búsqueda en el caso de uso:** `ConsultaArticulos.Validar()` devuelve `Result` y la llama `ConsultarArticulosUseCase`. El repositorio ya no valida ni lanza `ArgumentException`.
- 2026-10-15 · **Fase 4 · la ficha devuelve SIEMPRE las seis modalidades**, con `RazonAusenciaPrecio` cuando falta el precio (`SinConfiguracion`, `CasoEspecialNoFijado`, `TasaSinDefinir`, `SinCosto`, en ese orden). `PreciosPorModalidad` deja de ser anulable.
- 2026-10-15 · **Fase 4 · enums por nombre en HTTP:** `JsonStringEnumConverter` global (`Modalidad`, `BasePrecio`, `RazonAusenciaPrecio`).
- 2026-10-15 · **Fase 5 · `CostoUnitario` = precio unitario neto del descuento de la línea, SIN IVA y SIN prorratear percepciones.** Redondeado a centavos, el medio centavo lejos del cero. Las percepciones son de la cabecera (D8: datos sin lógica financiera). Vive en un solo lugar: `FacturaCompraDetalle.CostoUnitario`. **Confirmado por el equipo.**
- 2026-10-15 · **Fase 5 · un artículo dado de baja no admite compras** (`Invalido`, "reactivalo antes"), igual que el proveedor inactivo (CA1). **Confirmado por el equipo.**
- 2026-10-15 · **Fase 5 · `ObtenerNumeroActivoAsync` reemplaza a `ExisteNumeroActivoAsync`** (el nombre del plan), porque CA2 pide informar CON QUÉ factura choca. Devuelve `FacturaCompraExistente(Id, Numero, FechaEmision)` o null. Mismo criterio que 1.3 (`ObtenerPorCodigoAsync`).
- 2026-10-15 · **Fase 5 · stock con el incremento en el motor:** `INSERT ... ON CONFLICT DO UPDATE ... WHERE stock.cantidad + EXCLUDED.cantidad < 1e9`. 0 filas → `Invalido` y la unidad de trabajo revierte. El borde está fijado por los dos lados (`0190052` rechazo, `f424771` aceptado).
- 2026-10-15 · **Ramas (corrige la entrada de "Decisiones" de abajo):** las Fases 3 y 4 YA están en develop (#117 y #118). La Fase 5 abre su PR contra `develop`.

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
- ~~2026-10-15 · NUL en un texto → 500~~ y ~~el invariante de `SinCosto`~~: **resueltos en la Fase 6** (`e2118f6`; `d1b21b7` + `fbd4358`, con `Registrar_NingunCompradoQuedaConBaseYSinPrecio`). Lo que sigue es el texto original.
- 2026-10-15 · **NUL en un texto → 500 en TODO el proyecto (22021).** POST `/api/facturas-compra` con `numero = "A\u0000B"` da `invalid byte sequence for encoding "UTF8": 0x00` y termina en 500 (sin escribir nada). Ni los casos de uso ni `ManejadorExcepcionesGlobal` tratan el 22021, así que pasa en cualquier endpoint que guarde texto. **Decisión del equipo:** se traduce el 22021 a 400 en el manejador global. Es la primera unidad de la Fase 6: test HTTP en rojo (500) en facturas de compra y en al menos un segundo endpoint (artículos o clientes), para demostrar que es global.
- 2026-10-15 · **Invariante de `SinCosto` que la Fase 6 tiene que mantener.** La ficha informa `SinCosto` para toda modalidad "con base y sin precio" (Fase 4). Es correcto solo mientras un artículo con costo tenga sus precios calculados. Desde la Fase 5 la compra escribe el costo, y todavía NO recalcula: hoy una compra deja artículos con costo y precio null, que la ficha mostraría como `SinCosto`, lo cual es falso. **La Fase 6 tiene que recalcular en la MISMA unidad de trabajo de `RegistrarFacturaCompraUseCase`** (el punto de integración está marcado ahí), con un test PG que relea, con un contexto nuevo, costo y precios de cada comprado. En `lacentral_test` hay 0 casos (ninguna compra registrada en `public`).
- ~~2026-10-16 · Fase 6, auditor O1~~ **Resuelto el 2026-10-16 por el pedido de cambios del PR #121** (`e37c4ab` rojo PG → `e92707c`: `ActualizarPreciosAsync` filtra `NOT calculo_manual` en el UPDATE; test `Registrar_PrecioManualFijadoMientrasSeRecalcula_SobreviveALaCompra`). Lo que sigue es el texto original. **Fase 6, auditor O1 · filas manuales reescritas con lo leído:** `backend/LaCentral.Data/Repositorios/ArticuloRepositorio.cs`, `ActualizarPreciosAsync`, escribe precio y fecha de TODAS las filas que recibe, incluidas las manuales (vuelven de `Recalcular` sin cambios). Hoy no tiene efecto: ningún endpoint fija precios manuales. Cuando exista el de precio amigo, una compra concurrente podría volver a poner el valor viejo sin error. Arreglo sugerido: `WHERE NOT calculo_manual` o no mandar las manuales, con un test que lo discrimine (hoy esa mutación sería equivalente).
- 2026-10-16 · **Fase 6, auditor O2:** el comentario de `Atomicidad_FallaAlRecalcularLosPreciosDelSegundoComprado_NoQuedaNada` dice que falla "cuando los del primero ya se escribieron"; ningún assert lo comprueba (depende del orden del diccionario de `ObtenerAsync`). La atomicidad igual queda probada: cuando falla el recálculo, la compra, el stock y el costo ya están escritos.
- 2026-10-16 · **Fase 6, auditor O3:** ningún test fuerza una falla del recálculo por HTTP (`WebApplicationFactory`). Con el DI real, la atomicidad descansa en que `AddDbContext` es scoped y los tres repositorios y la unidad de trabajo comparten el contexto (`Program.cs`).
- 2026-10-16 · **Fase 6 · Lista manual sin precio fijado:** con la Interpretación pendiente · Lista manual → Efectivo (sin confirmar), Efectivo quedaría con base y sin precio, y la ficha lo mostraría como `SinCosto` (falso). Hoy no se alcanza: ninguna operación vuelve manual a Lista. Lo resuelve la HU que agregue precios manuales.
- 2026-10-16 · **PR #121, auditor OB1 · lectura desfasada que el filtro no cubre (latente):** si Lista pasa a manual entre `ObtenerAsync` y la escritura, Lista se saltea, pero Efectivo (base PrecioLista) se escribe con la Lista automática leída: queda distinto de Lista manual × 0,90 hasta el próximo recálculo (depende además de la Interpretación pendiente · Lista manual → Efectivo, sin confirmar). Inverso: una fila manual al leer que pasa a automática antes de escribir recibe el precio manual viejo. Sin error, inconsistencia silenciosa. Hoy no se alcanza (ningún código fuera del recálculo actualiza `articulo_precio_modalidad`). Arreglo posible: `SELECT ... FOR UPDATE` en `ObtenerAsync` sobre los comprados, o no mandar las manuales. Lo decide la HU de precio manual.
- 2026-10-16 · **PR #121, auditor OB2 · deadlock 40P01 a futuro:** la compra bloquea `articulo`/`stock` y después `articulo_precio_modalidad`. El endpoint de precio manual tiene que respetar ese orden.
- 2026-10-16 · **PR #121, auditor OB3 (menor):** `ActualizarPreciosAsync` igual manda un UPDATE por cada fila manual que no afecta filas. Inocuo.
- 2026-10-16 · **Auditor · esquema huérfano en `lacentral_test`:** `pr110_test_5bef5661630743e4b6d09c409f11fafa`, dueño `lacentral_tests`, ya existía antes de la auditoría (las corridas de la Fase 6 usaron `lacentral_javi` y no dejaron esquemas). Al final había 6 conexiones abiertas a `lacentral_test` de origen desconocido. No se tocó (R3): lo borra el equipo.

- 2026-10-17 · **Fase 7 · CA4 de #17 sin implementar** (modificar los márgenes de venta de un artículo). Falta la regla: qué márgenes se editan por artículo, si eso contradice D1 o D1 es solo el valor inicial, si las tasas de tarjeta son por artículo o globales, y quién las edita. El recálculo de un artículo ya existe (`CalcularPreciosArticuloUseCase`).
- 2026-10-17 · ~~**Fase 7 · `NoAutorizado` → 401** para un usuario autenticado sin permiso.~~ **Resuelto el mismo día** (decisión de Javi): `TipoError.Prohibido` → 403, ver "Decisiones".
- 2026-10-17 · **SPRINT 4 · "Regla: toda escritura verifica que escribió"** (era el N2 del auditor de la Fase 7; lo subió Santi al aprobar el PR). Toda operación de escritura devuelve si tocó una fila, y el caso de uso lo consume: un `false` es un error, no un éxito silencioso. Las apariciones (las cuatro originales y dos sumadas por Javi):
  1. **Crítico 3** (lección del plan, `docs/sprint03/PLAN_SPRINT3.md:376`): actualizar por Id, nunca por código.
  2. **`ProveedorRepositorio.ActualizarAsync`:** devolvía `void` y salía en silencio si no encontraba la fila. Desde la Fase 2 (2.3) devuelve `bool`, y lo consumen `ModificarProveedorUseCase`, `DarDeBajaProveedorUseCase` y `ReactivarProveedorUseCase`.
  3. **`ExecuteUpdate` del código interno** (`ArticuloRepositorio`): `AsignarCodigoInternoAsync` y `ActualizarDatosAsync` devuelven `filas == 1`, y lo consumen `CrearArticuloUseCase` y `ModificarArticuloUseCase` (B2 de la Fase 7).
  4. **`CerrarAsync` ignorado en Reactivar:** `ReactivarArticuloUseCase`, `ReactivarClienteUseCase` y `ReactivarProveedorUseCase` no miran el `bool`. Un inactivo sin baja abierta en el historial (un dato importado) se reactiva sin rastro. **Sigue abierto.**
  5. **`CerrarAsync` ignorado en `ReactivarUsuarioUseCase`** (`backend/LaCentral.UseCases/Acceso/ReactivarUsuarioUseCase.cs:58`): mismo caso que el punto 4, para usuarios. **Abierto.** Lo sumó Javi el 2026-10-17.
  6. **`ActualizarAsync` ignorado en `RestablecerContrasenaUseCase`** (`backend/LaCentral.UseCases/Acceso/RestablecerContrasenaUseCase.cs:43`): no mira el `bool`, así que un restablecimiento que no tocó ninguna fila se informa como éxito. **Abierto.** Lo sumó Javi el 2026-10-17.
- 2026-10-17 · **COMPUERTA DEL SPRINT 4 · auditor N4 de la Fase 7:** carrera entre el código interno y los códigos de proveedor. La unicidad entre `articulo.codigo_interno` y `articulo_codigo_alternativo.codigo` la controla solo la aplicación. Con el código interno editable a mano, dos sesiones en carrera pueden dejar la misma clave en los dos lados. Cerrarla pide esquema (trigger o tabla única de códigos) o un lock. **Lo decide el equipo antes de tocar código.**
- **Deuda declarada de la Fase 7 (2026-10-17):**
  - **N3 del auditor:** los códigos de proveedor pueden empezar con `LC-` (`ArticuloValidaciones.ValidarCodigos`), aunque el código interno manual no. Viene del alta.
  - **N5 del auditor:** no hay test HTTP del 409 que sale de un 23505 en una carrera al modificar (`codigo_unico_global`, `articulo_codigo_interno_key`). El caso PG afirma `DbUpdateException`.
  - **Historial de códigos retirados:** retirar un código borra la fila. No queda rastro de qué códigos tuvo un artículo.
  - **409 genérico de la carrera** (verificador): el conflicto en carrera de `ArticuloRepositorio.AgregarCodigosAsync` llega al caso de uso como `DbUpdateException`, no como `Result`. Por HTTP sale 409, pero con el mensaje genérico del índice.
  - **Esquema huérfano** `pr110_test_5bef5661630743e4b6d09c409f11fafa` en `lacentral_test`. Lo vio el auditor de la Fase 7 y lo borra el equipo.
- 2026-10-17 · **Fase 7:** el frontend se compiló (`ng build`) y se probó por el proxy solo sin escrituras. Las escrituras por la pantalla no se probaron en el navegador, porque `public` de `lacentral_test` es la copia de datos reales y el rol de test solo tiene SELECT allí.


## Premisas del plan que no coinciden con el código (revisar antes de la fase)
- 2026-10-13 · **Fase 2:** el plan dice "NINGÚN repositorio escribe en [las tablas de historial]". Falso para cliente y proveedor: `LaCentral.Data/Repositorios/ClienteRepositorio.cs:176-191` y `ProveedorRepositorio.cs:154-169` ya abren y cierran la baja en `*_historial_baja` dentro del repositorio, sin puerto. Lo cubren los tests PG `DosCiclos..._ConservanEstadoEHistorial...`. Artículo y usuario no escriben historial. La Fase 2 pasa a ser extraer un puerto `IHistorialBajaRepositorio` y mover esa lógica, más los usuarios de baja/reactivación desde IContextoUsuario, no construirla desde cero. (Aplicada en la Fase 2.)
- 2026-10-13 · **Fase 4:** el plan dice que el test del comodín ("100%_original") corre sobre InMemory. Hoy `ArticuloRepositorioTests.Busqueda_PermiteInactivosYNoInterpretaComodinesSQL` es `[PostgreSqlFact]` y corrió contra PostgreSQL real. Verificar si cubre ese caso exacto antes de duplicarlo. **Resuelto en la Fase 4 (2026-10-15):** usaba el texto exacto pero no discriminaba. Se agregó `Busqueda_100PorcientoGuionBajoOriginal_TraeSoloElLiteral`.
- 2026-10-14 · **Fases 3, 4 y 6 (D1):** ~~el comentario de `articulo_margen` todavía dice "0,01 % = precio igual al costo", mientras que D1 dice Costo = × 1,00~~ (la tabla ya no existe, 2026-10-14). **Resuelto en la Fase 3 (2026-10-14):** `MargenModalidad` y `CalcularPrecioEstimado` ya no existen. Ahora están `PrecioModalidad`, `BasePrecio` y `ReglasPrecioModalidad`, con Efectivo sobre PrecioLista y CasoEspecial manual. `CalcularPreciosArticuloUseCase` sigue sin existir: es de la Fase 6. Lo que sigue es el texto original. **Seguía en pie:** la tabla nueva tiene dónde guardar `precio` y `calculo_manual`, pero el dominio no se alineó todavía: eso es de la Fase 3. Además, el comentario de la tabla nombra un `CalcularPreciosArticuloUseCase` que todavía no existe. El modelo "porcentaje sobre costo" de `MargenModalidad`/`CalcularPrecioEstimado` no expresa Efectivo (sobre Lista), CasoEspecial (precio fijo) ni la razón de la ausencia. Detalle en `informe-compuerta-d1.md`.

## Decisiones
- 2026-10-17 · **Fase 7 · rol sin permiso → 403 (Javi).** `TipoError.Prohibido` → 403 en `MapeoResultado`, usado SOLO en los dos chequeos de rol: cambiar el código interno (`ModificarArticuloUseCase`) y retirar un código (`RetirarCodigoProveedorUseCase`). Los demás `NoAutorizado` (login inválido, sesión sin usuario) siguen en 401. Ver "Fase 7 · 403".
- 2026-10-17 · **Fase 7 · confirmadas por Javi:** el código interno manual no puede empezar con `LC-`; retirar un código de un artículo dado de baja → 400.
- 2026-10-17 · **Interpretación pendiente · baja y reactivación de artículo para ADMINISTRADOR y OPERADOR.** Pendiente de confirmación (Alfredo): no es una decisión ni lleva número. Lo reclasificó Santi al aprobar el PR de la Fase 7. **Incoherencia:** hoy un OPERADOR puede sacar un artículo del catálogo pero no puede cambiarle el código interno, y sacarlo del catálogo tiene más consecuencias. El código no cambia: `DarDeBajaArticuloUseCase` y `ReactivarArticuloUseCase` no miran el rol.
- 2026-10-17 · **Fase 7 · rol de la sesión (Javi):** `IContextoUsuario.EsAdministrador` sale del claim de rol en `ContextoUsuario`. El caso de uso da `Prohibido` (403; hasta `aa2e795`, `NoAutorizado`) si un OPERADOR cambia el código interno o retira un código. El `[Authorize(Roles="ADMINISTRADOR,OPERADOR")]` se mantiene. Cierra la contradicción de la PRÓXIMA SESIÓN anterior.
- 2026-10-17 · **Fase 7 · para confirmar con el equipo:** la ficha suma `ubicacionDeposito` y `motivoBaja`. Detalle en "Fase 7".
- 2026-10-16 · **Interpretación pendiente · Lista manual → Efectivo.** Pendiente de confirmación del equipo (Alfredo): no es una decisión ni lleva número. Lo que hace el código: si la fila de Lista es manual, Efectivo = Lista manual × 0,90 (la base `PrecioLista` es el precio de lista vigente, manual o calculado). **Pregunta para Alfredo:** si alguien fija a mano el precio de lista, ¿el de efectivo lo sigue solo al −10 % o son dos decisiones separadas? Hoy no se alcanza (ninguna operación vuelve manual a Lista). Reclasificada el 2026-10-16 por el pedido de cambios del PR #121 (Santi); el código no cambia. Test `RecalcularPreciosTests.ListaManual_EfectivoSobreLaListaManual_YLaListaNoSeToca` (`71d6cf1` rojo → `80d62fd`).
- 2026-10-16 · **Rol de test `lacentral_javi` aceptado.** Javi verificó en el catálogo que tiene los mismos permisos que `lacentral_tests` (no superusuario, sin CONNECT a `lacentral`, CREATE en `lacentral_test`). Las corridas abortan si el usuario de `LACENTRAL_TEST_PG` (ámbito User) no es `lacentral_tests` ni `lacentral_javi`. Corrige la regla del 2026-10-15 de abajo.
- 2026-10-16 · **Fase 6 · forma del recálculo (Javi):** dos unidades. Unidad 2, el núcleo puro (`Calcular` + `Recalcular`); unidad 3, `EjecutarAsync` con puerto e integración. El puerto (`IPreciosArticuloRepositorio`) usa el MISMO DbContext scoped que `RegistrarFacturaCompraUseCase`; la atomicidad la prueba el test PG.
- 2026-10-16 · **Fase 6 · precio que no entra en `articulo_precio_modalidad.precio` NUMERIC(12,2) → Invalido**, sin escribir ninguno, mismo criterio que H1 y el desborde de stock de la Fase 5 (antes, 22003 → 500). Borde fijado por los dos lados (9.999.999.999,99 se escribe; 10.000.000.000,00 no).
- 2026-10-16 · **Fase 6 · la guarda `CalculoManual` de `Calcular` se borró** por inalcanzable (CHECK `manual_sin_porcentaje`, probado contra el motor), regla de CLAUDE.md.
- 2026-10-15 · **Rol de test `lacentral_tests`.** La suite corre con un rol sin superusuario: CONNECT + CREATE sobre `lacentral_test`, solo SELECT en `public`, sin acceso a `lacentral`. Lo aplicó el equipo y quedó versionado en `backend/db/rescaffold/05_rol_tests.sql` (sin contraseña), reconstruido leyendo el catálogo. Con este rol: 261/261, ningún 42501.
- 2026-10-15 · **`LACENTRAL_TEST_PG` del proceso estaba vieja.** El valor del ámbito User apunta a `lacentral_tests`, pero el proceso de la sesión heredaba uno anterior con `lacentral`. Las corridas toman el valor del ámbito User y abortan si no es `lacentral_tests`.
- 2026-10-15 · **Ramas.** Las Fases 3 y 4 NO están en `develop` (`develop` = `f3c6705`). La Fase 5 sale del HEAD de `feature/HU-ART-02-consultar-articulos` (`a23bb26`), con esa rama como base provisoria del PR. Las Fases 6 y 7 se apilan sobre la anterior.
- 2026-10-15 · **PRs con `gh`.** `gh` está instalado en `C:\Program Files\GitHub CLI\` (fuera del PATH de bash) y autenticado: desde la Fase 5, los PRs los abre el agente.
- 2026-10-15 · **PR #117 (para el equipo):** el head es `feature/HU-ART-01-alta-articulo` (Fase 3), pero el título describe HU-ART-02 (Fase 4). La rama de la Fase 4 no tiene PR. No se tocó.
- 2026-10-15 · **Decisiones del equipo (consigna de la sesión):**
  - Se autoriza la 3ª ronda de la Fase 5. De acá en adelante, el límite de rondas es el de CLAUDE.md (3), no 2.
  - **Costo de la línea confirmado:** precio unitario neto del descuento de la línea, SIN IVA, sin prorratear percepciones. Si el modelo guardara el precio de la línea con IVA incluido, se para y se informa.
  - **Un artículo dado de baja no admite compras.** Confirmado.
  - **NUL en un texto:** se resuelve en `ManejadorExcepcionesGlobal`, traduciendo el 22021 a 400 (un solo lugar para todos los endpoints). Primera unidad de la Fase 6, con TDD: primero el test HTTP en rojo (500), con al menos un segundo endpoint (artículos o clientes) para demostrar que es global.

## PASO 0 de la sesión de Fases 5-7 (2026-10-15)
- `git worktree list`: solo el principal. `core.longpaths true`. El esquema `pr110_test_d4b8a811...` **ya no existe** (lo borró el equipo). Hay un esquema `staging` que no es de la suite: no se tocó.
- Línea base con `lacentral_tests`: 253/258. Los 5 que fallaron eran 53300; ninguno 42501.
- **Causa del 53300 (TDD, `1473f77` estructura → `3037344` rojo → `fix` siguiente):** (a) si la conexión de administración se cortaba a mitad de la creación, el esquema quedaba huérfano (`DisposeAsync` solo borraba con la conexión abierta); (b) cada esquema tiene su propia cadena y, por lo tanto, su propio pool, que nunca se vaciaba; (c) las fábricas de `WithWebHostBuilder` vivían hasta el final de la clase. Arreglo: creación en UNA transacción (el DDL de PostgreSQL es transaccional), `ClearPool` y `Liberar()` en `DisposeAsync`, y reapertura de la conexión de administración antes del DROP. Tests: `PostgreSqlPruebasLimpiezaTests` (3 PG, rojo por aserción).
- **Compuerta:** dos corridas completas seguidas, **261/261 (89 PG)**, sin 53300. Al terminar: 0 conexiones idle de `lacentral_tests` y 0 esquemas `pr110_test_*`.
- Hook de graphify: `graphify` por PATH, verificado (se dispara). El grafo se actualiza solo con el hook post-commit.

## Fase 7 · HU-ART-03 · Modificación y códigos (2026-10-17)
Rama `feature/HU-ART-03-modificar-articulo`, sale de develop `0580a16`. Línea base re-corrida al empezar: 353/353, 0 omitidos, 0 warnings, rol `lacentral_javi`.

**Criterios de #17** (leídos del Issue): CA1 actualiza el registro · CA2 agrega un código con su proveedor sin alterar los existentes · CA3 código de otro artículo → rechazo informando el conflicto · **CA4 modificar los márgenes de venta → recalcula el precio solo de ese artículo** · CA5 dado de baja → se deniega e indica que debe reactivarse.

**Commits (TDD por unidad):**
| Unidad | estructura → test (rojo) → producción |
|---|---|
| Estructura | `901d55f` (puerto `EsAdministrador`, firmas del repositorio, esqueletos que devuelven Success, endpoints sin DataAnnotations) |
| Modificación (Moq) | `02c2d53` (27 rojos: 21 Assert, 6 Verify) → `a9dbd7f` |
| Retiro, baja y reactivación (Moq) | `2c16031` (20 rojos: 16 Assert, 4 Verify) → `2983318` |
| Repositorio (PG) | `9ff4ac2` (14 tests, 11 rojos por aserción, 3 declarados) → `e72d569` |
| HTTP (WebApplicationFactory + PG) | `b9e42b1` (10 tests, 5 rojos por aserción, 5 declarados) → `4f9a5f5` |
| Frontend | `7bc47c4` |

**Pasaron a la primera, declarados y verificados con mutación sin commitear** (todas en rojo): M1 sin `Activo` en el UPDATE → la carrera de baja; M2 `ObtenerConflictosCodigoAsync` sin códigos de proveedor → CA3 y código interno = código de proveedor; M3 UPDATE sin Id → otro artículo modificado; M4 retiro sin artículo → borra el del otro artículo; M5 reactivar borra motivo y fecha; H1 `EsAdministrador => true` → los dos 401; H2 el PUT ignora los códigos → 204 sin el código; H3 sin el chequeo de conflicto del caso de uso → el 409 sale del índice sin decir cuál artículo; H4 inexistente → Invalido.

**Decisiones (2026-10-17):**
- **Rol (decisión de Javi, consigna de la sesión):** `IContextoUsuario.EsAdministrador`, leído del claim de rol en `ContextoUsuario` (`IsInRole("ADMINISTRADOR")`, el mismo claim que firma `GeneradorToken`). El caso de uso devuelve `Prohibido` (403, desde `1a93f7e`; antes `NoAutorizado`, 401) si un OPERADOR cambia el código interno o retira un código. El `[Authorize(Roles="ADMINISTRADOR,OPERADOR")]` del controlador se mantiene. Cierra la contradicción que estaba en "PRÓXIMA SESIÓN".
- Endpoints: `PUT /api/articulos/{id}` (204), `DELETE /api/articulos/{id}/codigos/{proveedorId}` (el proveedor identifica el código, D2), `POST /{id}/baja` (motivo, `DarDeBajaRequest`) y `POST /{id}/reactivacion`.
- `CodigosProveedor` del PUT son los que se asocian: un código que el artículo ya tiene (mismo proveedor y misma clave normalizada) no es conflicto ni se revalida su proveedor; uno omitido NO se retira. Cambiar el código de un proveedor = retirar (admin) + agregar.
- **Código interno manual (para que lo confirme el equipo):** 1-40 caracteres, normalizado como los códigos (trim + mayúsculas), **sin el prefijo `LC-`** (lo genera el alta desde la identidad: uno puesto a mano chocaría con un alta futura) y sin coincidir con ningún código interno ni de proveedor, incluidos los del propio artículo.
- Retirar un código de un artículo dado de baja → Invalido (retirar también es modificar, CA5).
- Baja y reactivación: ADMINISTRADOR y OPERADOR (las reglas de permisos solo restringen el código interno y el retiro). **Interpretación pendiente de confirmación (Alfredo)**, no una decisión: ver "Decisiones". `articulo` no tiene `usuario_baja_id`: el usuario queda solo en `articulo_historial_baja` (como proveedor). La reactivación conserva motivo y fecha en la fila (D10).
- La condición de estado va en el WHERE del UPDATE (`activo`): si otra sesión da de baja entre la lectura y la escritura, la modificación da 400 y no escribe nada (test PG).
- La ficha suma `ubicacionDeposito` y `motivoBaja` (parámetros opcionales al final de los DTOs; aditivo).

**CA4 · NO implementado (compuerta: falta una regla de negocio / choca con D1).** D1 fija las bases y porcentajes (Lista +50 % sobre costo, Costo 0 %, Efectivo −10 % sobre Lista), Tarjeta3/6 tienen tasas "configurables, sin definir" y CasoEspecial es precio manual, cuyo endpoint es otra HU (con OB1/OB2 pendientes). Para "modificar los márgenes de un artículo" hace falta decidir: qué márgenes se pueden editar por artículo; si cambiar el +50 % de Lista de UN artículo contradice D1 o D1 es solo el valor inicial; si las tasas de tarjeta son por artículo o globales; y quién puede editarlos. No se inventó ninguna. El recálculo "solo de ese artículo" ya existe (`CalcularPreciosArticuloUseCase`, Fase 6), así que cuando haya regla el trabajo es chico. **Por esto el PR va con `Refs #17`, no `Closes`.**

**Suite (2026-10-17, en `7bc47c4`):** 424 total · 424 passed · 0 failed · 0 skipped · 0 warnings (`--no-incremental`), **143 `[PostgreSqlFact]`** (antes 353 y 119), rol `lacentral_javi`, 5 min 18 s.

**Frontend:** en la ficha de `frontend/src/app/articulos.ts`: modificar nombre, ubicación y código interno (habilitado solo para ADMINISTRADOR, visual: decide la API), agregar códigos, "Retirar" (solo admin), baja con motivo y reactivación. `ng build` OK. Prueba de humo por el proxy contra `lacentral_test`, **sin escrituras** (el rol de test solo tiene SELECT en `public`, que es la copia de datos reales): PUT, DELETE de código, baja y reactivación sin token → 401 (las rutas existen: una inexistente da 404); login malo → 401; pantalla → 200. **Las escrituras por la pantalla no se probaron en el navegador**; las cubren los tests HTTP contra PostgreSQL.

**Commits posteriores a la primera pasada:** `a1f4143` (test del borde de 40, no bloqueante del verificador; caracterización, con `>=` → rojo) · `e060dbe` → `7858c26` (ronda 1 del auditor, ver abajo) · `acc8f9d` (la guarda B1 por los dos lados, no bloqueantes N-a/N-b del verificador; caracterización, con `Any` → `All` y `==` → `!=` → rojo).

**Compuerta (subagentes de a uno, cada uno en su worktree con ruta corta; los cuatro borraron el suyo):**
| Pasada | Alcance | Veredicto | Qué |
|---|---|---|---|
| verificador | `0580a16..7bc47c4` | **APTO** | Orden test → producción limpio. Los 4 rojos se reproducen por aserción. 25 mutaciones (las 9 declaradas y 13 propias) mueren. 424/424, 143 PG. No bloqueantes: el borde de 40 sin test (→ `a1f4143`); M3 lo frena el índice, no la aserción; el conflicto en carrera es una excepción (409 por el manejador); 401/403 |
| auditor | `0580a16..a1f4143` | **NO APTO** | **B1:** el código interno nuevo igual a un código de proveedor nuevo de la misma petición se guardaba. **B2:** el UPDATE reescribía el código interno leído y pisaba un cambio de otra sesión. Reproducidos contra el motor. 425/425. CHECK, índices, FK (nada referencia `articulo_codigo_alternativo`), identidad, relectura y seguridad: sin hallazgos. No bloqueantes N1-N5 |
| ronda 1 | `e060dbe` (7 rojos) → `7858c26` | — | B1 → Conflicto. B2 → `string? codigoInterno` en el puerto; con null, la columna queda fuera del SET. N1 → el test de reactivación afirma `articulo_hist_fechas_coherentes` |
| verificador | `a1f4143..7858c26` | **APTO** | 7 rojos reproducidos. 11 mutaciones muertas (B1, B2, las dos ramas del repositorio, `filas == 1`). 428/428, 145 PG |
| auditor | `a1f4143..acc8f9d` | **APTO** | B1 y B2 cerrados contra el motor, con sus mutaciones en rojo. SQL capturado: la rama null no lleva `codigo_interno` y `activo` sigue en los dos WHERE. **430/430, 145 PG** |

**Suite final (corrida del auditor, en `acc8f9d`):** 430 total · 430 passed · 0 failed · 0 skipped, 0 warnings, **145 `[PostgreSqlFact]`** (antes 353 y 119: +77 tests, 26 PG), rol `lacentral_javi`.

**Fase 7 · 403 para un rol sin permiso (2026-10-17, decisión de Javi):**
| Paso | Commit | Qué |
|---|---|---|
| Estructura | `0abcd8c` | `TipoError.Prohibido`, sin mapear |
| Rojo | `70af035` | 2 Moq (Expected Prohibido / Actual NoAutorizado) y 2 HTTP+PG (Expected Forbidden / Actual Unauthorized) |
| Producción | `1a93f7e` | `Prohibido` → 403 en `MapeoResultado` y en los dos chequeos de rol |
| Caracterización | `aa2e795` | `Login_ContrasenaIncorrecta_DevuelveStatus401`: pasa a la primera, declarado; con `NoAutorizado` → 403 en el mapeo da rojo |

| Pasada | Alcance | Veredicto | Qué |
|---|---|---|---|
| verificador | `b4a1ff0..aa2e795` | **APTO** | 4 rojos reproducidos por aserción. 6 mutaciones muertas: cada caso de uso vuelto a `NoAutorizado`, `Prohibido` → 401, la línea del mapeo borrada (→ 400), `NoAutorizado` → 403, y un `NoAutorizado` de sesión vuelto `Prohibido`. Los 11 `NoAutorizado` restantes son de login o de sesión |
| auditor | `b4a1ff0..aa2e795` | **APTO** | Los dos 403 releen con un contexto nuevo y no hay escritura; el chequeo de rol va antes de escribir y fuera de la unidad de trabajo (mutaciones "escribir antes del chequeo" → rojo en los dos). **431/431, 0 omitidos, 145 PG** |

**Issue:** `Refs #17` (CA4 sin implementar). CA1, CA2, CA3 y CA5 verificados: el mapa de tests está en el cuerpo del PR. Cuerpo: `docs/sprint03/pr-fase7-hu-art-03.md`.

## Fase 6 · HU-ART-05 + FCO-01 CA5 (2026-10-16)
Rama `feature/HU-ART-05-precios-modalidad`, sale de develop `a47a866` (Fase 5 ya mergeada, #119). La hizo Javi con el agente; Santi retoma la semana siguiente. Pusheada con OK de Javi; **PR #121** (abierto a mano, sin `gh`). Santi pidió cambios (B1-B3) el 2026-10-16: aplicados abajo. Cuerpo del PR: `docs/sprint03/pr-fase6-hu-art-05.md`.

**Commits (TDD por unidad):**
- Unidad 1 · NUL → 400: `95571c7` rojo HTTP (2: artículos y facturas, Expected BadRequest / Actual InternalServerError) → `e2118f6` fix en `ManejadorExcepcionesGlobal` (22021 directo o en `DbUpdateException`). Cada rama la cubre un test distinto (artículos: `DbUpdateException`; facturas: directa), verificado con mutación.
- Unidad 2 · única regla: `6f2e337` refactor (`Calcular` → `CalcularPreciosArticuloUseCase`, 12 métodos reapuntados, 327 = 327) → `698cec7` test de redondeo (declarado; mata ToEven) → `71d6cf1` rojo `Recalcular` (6 de 8) → `80d62fd` → `bc5e98f` (guarda inalcanzable borrada).
- Unidad 3 · integración: `bf568db` estructura (puerto `IPreciosArticuloRepositorio`, DI) → `a5102ba` rojo Moq (6 de 8) → `d1b21b7` → `fe029b2` rojo PG/HTTP (6) → `fbd4358` repositorio.
- Cierre: `264879f` PR · `84967d8` ronda 1 (test, M6) · `638afe4` PR · `f7f1437` ESTADO.
- **Cambios pedidos por Santi en el PR #121 (2026-10-16):** B1, `Closes #22` → `Refs #22` (Tarjeta3/6 y CasoEspecial sin definir; `Closes #32` queda). B2, la regla que estaba numerada como decisión pasa a ser la Interpretación pendiente · Lista manual → Efectivo, pendiente de confirmación de Alfredo (ver "Decisiones"; el código no cambia). B3, O1: `e37c4ab` rojo PG (Expected 95 / 2026-10-02, Actual 80,00 / 2026-09-01) → `e92707c` (`ActualizarPreciosAsync` filtra `NOT calculo_manual` en el UPDATE) → `c575e4e` ronda 1 del verificador (test declarado: una fila automática al leer que pasa a manual antes de escribir). El intercalado de los dos tests hoy no ocurre en producción (ningún código fuera del recálculo actualiza `articulo_precio_modalidad`): se adelantan al endpoint de precio manual.

**Compuerta (subagentes de a uno, cada uno en su worktree):**
| Pasada | Alcance | Veredicto | Qué |
|---|---|---|---|
| verificador | `a47a866..264879f` | NO APTO | M6: `>=` → `>` en `LimitePrecio` sobrevivía (ningún test en 10.000.000.000,00 exacto). 4 rojos reproducidos por aserción, 15 mutaciones muertas, 350/350. No bloqueante: `d1b21b7` también tocaba un test (el doble por defecto), no declarado en el PR → declarado en `638afe4`. |
| ronda 1 (test) | `84967d8` | — | `PrecioJustoEnElLimiteDeSuColumna_InvalidoYNoEscribe`: Tarjeta3 con tasa SOLO DE PRUEBA 25 % sobre Lista 8.000.000.000,00 → 1e10 → Invalido. |
| verificador | `264879f..638afe4` | **APTO** | M6 muere (`:124`), límite una centésima menos muere (`:137`). 351/351, 117 PG. |
| auditor | `a47a866..638afe4` | **APTO** | Atomicidad con mutación (recálculo fuera de la unidad → 2 PG en rojo), CHECK cumplidos, relectura con contexto nuevo. Observaciones O1-O3 en "Deuda nueva detectada". Suite no repetida (usa la del verificador). |
| verificador (B3) | `f7f1437..e92707c` | NO APTO | Rojo de `e37c4ab` reproducido; M1 (sin filtro) y M2 (invertido) rojos. M3, filtrar las manuales sobre lo leído en vez de en el UPDATE, sobrevivía (19/19): la fila del test ya era manual al leer. 352/352, 118 PG. No bloqueante: declarar que el intercalado hoy no ocurre en producción → declarado en el PR. |
| ronda 1 (test) | `c575e4e` | — | `Registrar_FilaQuePasaAManualMientrasSeRecalcula_SobreviveALaCompra`: otra sesión pasa a manual la fila Costo (automática al leer). Declarado; con M3, Expected 170 / Actual 180,00. |
| verificador | `e92707c..c575e4e` | **APTO** | M3 muere (`:271`); M1 y M2 rojos en los dos tests de O1 (`:236`, `:271`) y M2 en el recálculo automático (`:189`). CHECK cumplidos; el doble envuelve el repositorio real. 353/353, 119 PG. |
| auditor (B3) | `f7f1437..c575e4e` | **APTO** | Motor real, relectura con contexto nuevo, sin InMemory ni Ids. Atomicidad intacta (20/20 de la clase); las automáticas se siguen escribiendo. CHECK verificados en el catálogo (10). READ COMMITTED: con el filtro en el UPDATE, PostgreSQL reevalúa el WHERE sobre la versión confirmada. Sin deuda nueva; OB1-OB3 latentes en "Deuda nueva detectada". Suite no repetida. |

**Suite:** 353 total · 353 passed · 0 failed · 0 skipped · 0 warnings, **119 `[PostgreSqlFact]`**, en `c575e4e`, rol `lacentral_javi` (antes de B3: 351/351, 117 PG, en `638afe4`).

**Issues:** el PR **cierra #32** y **referencia #22** (Refs, no Closes: tasas de Tarjeta3/6 y precio de CasoEspecial siguen sin definir; cambio pedido por Santi en el PR #121). CA5 de FCO-01 y CA-001 de ART-05 se verifican en el mismo recorrido HTTP: alta, compra a 100, ficha Lista 150 · Costo 100 · Efectivo 135.

## Fase 5 · HU-FCO-01 · Factura de compra (2026-10-15)
Rama `feature/HU-FCO-01-factura-compra`, sale de `a23bb26` (HEAD de la Fase 4). Suite al cerrar la unidad HTTP + frontend: **309/309, 105 `[PostgreSqlFact]`**, 0 warnings.

**Premisas contrastadas con el código:**
- El plan nombra `ExisteNumeroActivoAsync`. CA2 pide informar CON QUÉ factura choca, así que el puerto es `ObtenerNumeroActivoAsync` y devuelve `FacturaCompraExistente(Id, Numero, FechaEmision)`, o null.
- `ux_factura_compra_numero_activa` ya existe y el manejador global ya lo reconocía (409).
- `IContextoUsuario` NO expone el rol (solo `UsuarioId`, `NombreUsuario`, `SucursalId`). Eso importa para la Fase 7.
- `factura_compra.percepcion_*` tienen DEFAULT 0. Las cuatro van siempre, con 0 si no se informan.

**Decisiones técnicas (2026-10-15):**
- **Costo de la línea = precio unitario neto del descuento de la línea**, redondeado a centavos con el medio centavo lejos del cero (el criterio de PostgreSQL para NUMERIC). Las percepciones son de la cabecera y no se prorratean al costo (D8: datos sin lógica financiera). Vive en un solo lugar: `FacturaCompraDetalle.CostoUnitario`. **Para que lo confirme el equipo:** si el costo tuviera que ser el precio bruto, se cambia esa propiedad y sus 6 casos de test.
- Un artículo **dado de baja** no admite compras (`Invalido`, "reactivalo antes"), igual que el proveedor inactivo (CA1). Confirmar con el equipo.
- Validaciones del núcleo: número 1-30 caracteres, vencimiento ≥ emisión, percepciones ≥ 0, y por línea cantidad > 0, precio ≥ 0, descuento 0-100, un artículo por línea (es la PK del detalle) y sucursal real (D9).
- Stock: `INSERT ... ON CONFLICT DO UPDATE SET cantidad = stock.cantidad + EXCLUDED.cantidad`. El motor hace el incremento (dos compras simultáneas no se pisan) y crea la fila si no existía.
- 201 con `Location: /api/facturas-compra/{id}`, sin GET (la consulta de facturas es otra HU), con el mismo precedente que la Fase 3.
- CA5: el punto de integración quedó marcado dentro de la unidad de trabajo de `RegistrarFacturaCompraUseCase`. **#32 NO se cierra hasta la Fase 6.**

**Atomicidad (el test más importante del sprint).** En `RegistrarFacturaCompraPostgreSqlTests` hay un test PG por punto de falla. Cada uno fuerza al MOTOR a fallar con un trigger en el esquema aislado, en la segunda línea, cuando la primera ya escribió. Puntos: índice de número activo (carrera real), cabecera, detalle, stock, artículo (costo y último proveedor) e historial. Todos afirman con un contexto nuevo que no quedó cabecera, detalle, stock, costo, último proveedor ni historial. Mutaciones sin commitear: `UnidadDeTrabajo` sin transacción → 3 de 6 en rojo (los otros 3 caen dentro del primer SaveChanges, que ya es atómico); si además se escribe el stock antes que la cabecera → 6 de 6 en rojo.

**Commits (TDD por unidad):** `2dda597` estructura → `05241ab` rojo Moq (31 rojos; `CostoUnitario(100,100)` pasa a la primera, se verificó con mutación) → `42cad4a` caso de uso → `ba4044c` rojo PG (10) → `97cdbea` repositorio → `f760405` rojo HTTP (5) → `c69ca86` controlador → `f7f8b27` 400 sin emisión (pasa a la primera; sin `[Required]` da 500) → `cdee847` frontend.

**Frontend:** `frontend/src/app/factura-compra.ts`, una pestaña nueva en el header. No calcula costos: al registrar, relee la ficha de cada artículo comprado. Prueba de humo por el proxy, API con el rol `lacentral_tests` (solo SELECT en public), **solo lecturas**: proveedores sin token → 401; proveedores "a" → 200 con 4; ficha → 6 modalidades y 2 sucursales; POST sin token → 401; POST sin líneas → 400 (valida antes de escribir). `public.factura_compra` sigue con 0 filas.

### Compuerta de la Fase 5 (2026-10-15), subagentes de a uno, cada uno en su worktree · **SE PARÓ**
| Pasada | Alcance | Veredicto | Qué |
|---|---|---|---|
| verificador | `a23bb26..505301c` | NO APTO | B1: sin la guarda "no existe", el test solo miraba "999". B2: `<` → `<=` en el vencimiento no lo detectaba nada. 20 mutaciones más murieron. |
| ronda 1 (tests) | `12480a2` (+ `10a0f0b` no bloqueante) | — | mensaje exacto de "no existe"; vencimiento el mismo día de la emisión; el proveedor inexistente afirma su mensaje. |
| verificador | `505301c..12480a2` | **APTO** | B1/B2 y 8 mutaciones extra en rojo. 310/310. |
| auditor | `a23bb26..10a0f0b` | NO APTO | H1: el núcleo aceptaba valores que NUMERIC no guarda (cantidad 0,0004 → 23514; 1e10 → 22003) → 500. |
| ronda 1 (producción) | `3e110cb` rojo → `f34bbc4` | — | `EntraEnColumna`: (12,3) cantidad, (14,2) precio y percepciones, (6,2) descuento. |
| auditor | `10a0f0b..f34bbc4` | NO APTO | H1 cerrado. Residuo: la suma al stock existente puede desbordar `stock.cantidad` (22003 → 500). |
| ronda 2 (producción) | `0b7da32` estructura → `163b2cc` rojo → `b4fca2f` | — | upsert con `WHERE stock.cantidad + EXCLUDED.cantidad < 1e9`; 0 filas → Invalido, el caso de uso lo propaga, la UoW revierte. |
| auditor | `f34bbc4..b4fca2f` | **APTO** | residuo cerrado contra el motor: borde exacto, stock negativo previo, sin fila previa y dos compras simultáneas (una 201, otra 400). 323/323. |
| verificador | `10a0f0b..b4fca2f` | **NO APTO** | M11: `<` → `<=` en el WHERE del upsert sobrevive (65/65 en verde). El test de desborde usa una suma muy por encima del límite y no fija el borde. Las otras 10 mutaciones murieron. 323/323. |
| ronda 3 (test, autorizada 2026-10-15) | `0190052` | — | `StockQueLlegaJustoAlLimiteDeSuColumna_InvalidoYNoQuedaNada`: stock previo 0,001 + 999.999.999,999 → Invalido, foto igual, sin excepción. Con `<=` (sin commitear) → rojo por 22003. |
| verificador | `89e0202..0190052` | **NO APTO** | M11 cerrado (rojo por 22003 con `<=`; también mueren "sin `+ EXCLUDED.cantidad`" y `<` → `>`). **M12 sobrevive:** `LimiteStock = 1_000_000_000m` → `999_999_999.999m` (55/55 FacturaCompra + 8/8 API en verde). Ningún test fija el lado ACEPTADO del borde: una suma de exactamente 999.999.999,999 entra en NUMERIC(12,3) y la mutación la rechazaría. 324/324. |
| ronda 4 (test, autorizada por el equipo 2026-10-15, solo M12) | `f424771` | — | `StockQueQuedaEnElMaximoDeSuColumna_SeRegistraYSeGuarda`: stock previo 0,001 + 999.999.999,998 → éxito, relectura con contexto nuevo = 999.999.999,999. Pasa contra producción (la constante es correcta); el rojo es contra M12. |
| verificador | `26c02fc..f424771` | **APTO** | `f424771` toca solo `Tests/`. **M12 muere** por aserción (`RegistrarFacturaCompraPostgreSqlTests.cs:325`, `IsSuccess` falso con el Invalido de producción "superaría el máximo"). **M11 sigue muriendo** (22003 capturado en L297/300). Build `--no-incremental` 0 warnings; **325/325**, 109 `[PostgreSqlFact]`. Worktree borrado. |

**Fase 5 CERRADA (2026-10-15, ronda 4):** los dos subagentes dan APTO. Lo que sigue (por qué se paró después de las rondas 2 y 3) queda como registro.

**Por qué se paró (2026-10-15, después de la ronda 3):** CLAUDE.md y la consigna: "NO APTO después de 3 rondas → PARÁS". Rondas: 1 (B1/B2 + H1), 2 (desborde de stock), 3 (borde M11). El verificador dio NO APTO sobre la ronda 3 por M12. No se hizo push ni se abrió el PR; `docs/sprint03/pr-fase5-hu-fco-01.md` queda escrito para cuando haya APTO. Las Fases 6 y 7 no se empezaron.

**Lo que haría falta para APTO (solo un test, sin cambios de producción):** el espejo del test de la ronda 3. Un caso PG con stock previo 0,001 y una línea de 999.999.999,998 (suma 999.999.999,999), que espere éxito y relea la fila de stock con un contexto nuevo (= 999.999.999,999). Con la constante mutada daría Invalido → rojo. Requiere que el equipo autorice una 4ª ronda (excede el límite de CLAUDE.md).

**Antecedente (pasada anterior):** la consigna de la sesión dice "PARÁS si después de dos rondas de arreglos sigue habiendo un NO APTO". Las dos rondas ya se hicieron (ronda 1: B1/B2 + H1; ronda 2: el desborde de stock) y el verificador dio NO APTO sobre el código de la ronda 2. No se hizo push ni se abrió el PR.

**Lo que haría falta para APTO (solo un test, sin cambios de producción):** un caso PG en el borde, con stock previo 0,001 y una línea de 999.999.999,999 (suma exacta de 1e9) que espere Invalido, foto igual y ninguna excepción. Con `<=` la suma pasaría el WHERE y daría 22003, y el test quedaría en rojo. El auditor ya probó a mano que, con `<`, ese caso da 400 sin escritura (sonda S2b). Si el equipo autoriza una tercera ronda (CLAUDE.md admite hasta 3), es ese único commit de test, seguido de la verificación acotada del verificador.

**Deuda nueva (la decide el equipo):**
- **NUL en un texto → 500 (todo el proyecto).** El auditor lo encontró: POST `/api/facturas-compra` con `numero = "A\u0000B"` da 22021 (`invalid byte sequence for encoding "UTF8": 0x00`) y termina en 500, sin escribir nada. Ningún caso de uso ni el manejador global trata el 22021, así que muy probablemente pase en todo endpoint con texto (solo se probó en facturas de compra). La decisión es si se traduce el 22021 a 400 de forma global, o si se rechazan los caracteres de control al recibir el JSON.
- Después de un fallo de `RegistrarConEfectosAsync`, el DbContext conserva entidades rastreadas. Hoy no tiene efecto, porque el contexto vive lo que dura la petición; habría que limpiarlo si algún día se reutiliza.
- El rango de `EntraEnColumna` para el descuento (|x| < 10.000) es inalcanzable: antes corre la guarda 0-100. No es una guarda propia: es la función compartida.

## Revisión de la Fase 1 por el equipo (2026-10-13)
- Fase 1 verificada contra el grafo (`bf845dbc`): los nueve ítems están. El PR todavía no se abrió (no hay `gh`): abrirlo a mano desde la rama `fix/deuda-bloqueante-sprint3` con el cuerpo de `pr-fase1-deuda-bloqueante.md`.
- Confirmado: 1.7 (sin `LA_CENTRAL_REQUIRE_POSTGRES`), 1.3 (`ObtenerPorCodigoAsync`) y 1.9 (era `ClienteDireccion.Calle`).
- La compuerta de esquema de modalidades es de las Fases 3 Y 6: ART-01 da de alta la configuración de precios. **La resuelve el equipo con un script SQL antes de la Fase 3. El agente NO toca el esquema.**
- `ConnectionStrings__Postgres` NO existe en el entorno (verificado en las tres carpetas): los tests HTTP no resuelven ningún DbContext real. `LACENTRAL_TEST_PG` ahora es persistente.
- Los subagentes y los hooks de graphify se copiaron a `backend/.claude/` (agents/ y settings.json). En la sesión de la Fase 1 NO cargaron, porque se leen al iniciar la sesión: `Agent type 'verificador-tdd' not found`. Se reinicia la sesión antes de la Fase 2. (Superado el 2026-10-14: la configuración vive en la raíz y carga.)


## PRÓXIMA SESIÓN — arrancar acá
Arrancar en la **raíz del repo**. Estado al 2026-10-17:

- **`develop` = `0580a16`**: Fases 1 a 6 mergeadas (#113, #114, #115, #117, #118, #119, #121) y #120 (onboarding).
- **Fase 7 (HU-ART-03, #17): CERRADA (2026-10-17)**, **PR SIN ABRIR**: lo abre Javi a mano desde el link de compare (no hay `gh`), con el cuerpo de `docs/sprint03/pr-fase7-hu-art-03.md`. Rama `feature/HU-ART-03-modificar-articulo`, pusheada. **`Refs #17`, no Closes: CA4 no está implementado.** Verificador APTO y auditor APTO (después de la ronda 1, B1 y B2, y otra vez sobre el cambio a 403). Suite: **431/431, 145 `[PostgreSqlFact]`**, 0 omitidos, 0 warnings, en `aa2e795`. No se mergea sin el equipo.
- **Ya decidido por Javi (2026-10-17):** `LC-` prohibido en el código interno manual; retirar en un artículo dado de baja → 400; rol sin permiso → **403** (`TipoError.Prohibido`, hecho en `0abcd8c..aa2e795`).
- **Santi aprobó el PR de la Fase 7 (2026-10-17)** con dos correcciones de documentación, aplicadas: `TipoError` con seis valores en CLAUDE.md, el plan y el onboarding; y baja y reactivación para los dos roles pasan a interpretación pendiente (Alfredo).
- **Decisiones que tiene que tomar el equipo antes de cerrar #17:**
  - **CA4 · márgenes por artículo:** qué márgenes se editan (¿el +50 % de Lista de UN artículo contradice D1 o D1 es solo el valor inicial?), si las tasas de tarjeta son por artículo o globales, y quién los edita. Hasta entonces no se implementa.
  - **Alfredo:** ¿baja y reactivación de artículo también para OPERADOR? (interpretación pendiente, ver "Decisiones").
- **Sprint 4:** el ítem "Regla: toda escritura verifica que escribió" (ex N2) y la compuerta N4 (carrera código interno / código de proveedor), los dos en "Deuda nueva detectada". N3, N5, historial de códigos retirados, 409 genérico y el esquema huérfano quedan como deuda declarada.
- **Pendientes de antes:** tasas de Tarjeta3/6; HU de precio manual (CasoEspecial y OB1/OB2); confirmar con Alfredo la Interpretación pendiente · Lista manual → Efectivo; borrar el esquema huérfano `pr110_test_5bef5661...` (sigue ahí al 2026-10-17); deuda O2-O3 de la Fase 6.

### Fase 8 · Cierre del sprint (plan, sección "FASE 8")
- **Sin rama nueva.** Es un reporte, no código. Arrancar cuando el PR de la Fase 7 esté abierto (y, si el equipo quiere, mergeado).
- Contenido:
  - tests antes y después: línea base 108 (8 PG) → 431 (145 PG), con el desglose PG / dobles y 0 omitidos;
  - CA por HU: ART-01 (#14), ART-02 (#15), FCO-01 (#32, cerrado por #121), ART-05 (#22, Refs: Tarjeta3/6 y CasoEspecial sin definir), ART-03 (#17, Refs: CA4);
  - la deuda de este archivo, con un responsable sugerido;
  - qué Issues se cierran y cuáles siguen abiertos, con el motivo.
- No declarar terminada una HU con un CA sin verificar, ni una modalidad sin sus parámetros (plan, final de la Fase 8).

### Cómo correr los tests
La `LACENTRAL_TEST_PG` del proceso puede estar vieja. Exportá la del ámbito User antes de `dotnet test` (desde `backend/`) y abortá si el usuario no es `lacentral_tests` ni `lacentral_javi` (decisión del 2026-10-16):
`export LACENTRAL_TEST_PG="$(powershell.exe -NoProfile -Command "[Environment]::GetEnvironmentVariable('LACENTRAL_TEST_PG','User')" | tr -d '\r')"`

La suite completa tarda ~5 min 15 s (431 tests); un test PG filtrado, ~15-20 s.

En la máquina de Javi `gh` no está instalado: el PR se abre a mano. En PowerShell 5.1, los mensajes de commit con comillas dobles se rompen: usar `git commit -F <archivo>`.
