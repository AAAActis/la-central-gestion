# Estado del Sprint 3

Memoria entre fases. El agente lo lee al empezar y lo actualiza al terminar cada fase.
Si se pierde el contexto, este archivo es la única fuente de verdad sobre qué está hecho.

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
| 2 · D10 en la aplicación | pendiente | | | | |
| 3 · HU-ART-01 | pendiente | | | | |
| 4 · HU-ART-02 | pendiente | | | | |
| 5 · HU-FCO-01 | pendiente | | | | |
| 6 · HU-ART-05 + CA5 | pendiente | | | | |
| 7 · HU-ART-03 | pendiente | | | | |

## Decisiones tomadas durante el sprint
(el agente las asienta acá, con fecha)
- 2026-10-13 · Plan corregido por el equipo: la Fase 2 se redefine como extracción de `IHistorialBajaRepositorio` (más artículo y usuario); en la Fase 4 se verifica el caso exacto "100%_original"; se agregan los ítems 1.7, 1.8 y 1.9 a la Fase 1.
- 2026-10-13 · 1.3: `ObtenerPorCodigoAsync` en lugar de `ExisteCodigoAsync`, porque el criterio exige informar CUÁL proveedor está en conflicto.
- 2026-10-13 · 1.7: `[PostgreSqlFact]` nunca se omite y `LA_CENTRAL_REQUIRE_POSTGRES` deja de leerse: exigir PostgreSQL es el único comportamiento. Se documenta en `LaCentral.Tests/README.md`.
- 2026-10-13 · 1.8: la causa del conflicto era `EFCore.Design` (PrivateAssets) en Data. Se agrega Relational 10.0.11 explícita en Data, que se propaga a Api y Tests.
- 2026-10-13 · 1.9: el warning de ClienteRepositorio era `ClienteDireccion.Calle`, no `Cuit`. Se sigue la convención documentada del repositorio (sin dato = "" en el dominio, NULL en la base) en vez de `List<string?>`. La sincronización compara con la misma convención: sin eso, cada baja o reactivación duplicaba la dirección.
- 2026-10-13 · 1.5 queda sin test (ningún código usa la entidad `Articulo`) y 1.6 es un refactor en verde. Los dos verificadores lo aceptaron.
- 2026-10-13 · Los subagentes verificador-tdd y auditor-persistencia no cargan como tipo en una sesión iniciada en `backend/` (están en `Tesis/.claude/agents/`). Se corrieron como agentes generales con su definición textual. Los dos dieron APTO.

## Deuda nueva detectada
(el agente la asienta acá, con archivo y línea)
- ~~Tests PG omitidos sin conexión; conflicto EF Relational; 3 warnings de nulabilidad~~: resueltos en la Fase 1 (1.7, 1.8, 1.9).
- 2026-10-13 · **Fase 2:** las escrituras de historial no setean `usuario_baja_id` ni `usuario_reactivacion_id` (`ClienteRepositorio.cs:183-188`, `ProveedorRepositorio.cs` en `ActualizarAsync`). `Cliente.UsuarioBajaId` no se persiste. `FechaReactivacion` la estampa el repositorio. Usuario no escribe `usuario_historial_baja`.
- 2026-10-13 · **Fase 2:** `PostgreSqlPruebas.cs` (`LIKE ... INCLUDING ALL`) renombra los índices únicos en el esquema aislado: `codigo_unico_global` pasa a `articulo_codigo_alternativo_codigo_key`, `ux_*_hist_abierta` a `<tabla>_<col>_idx`, `ux_factura_compra_numero_activa` a `factura_compra_proveedor_id_numero_idx`. Confirmado contra el motor. Arreglarlo en el fixture (renombrar al nombre original) y NO agregar los nombres generados a la lista blanca del manejador.
- 2026-10-13 · **Alta:** `ProveedorRepositorio.ActualizarAsync` compara `b.Calle == d`: una dirección sin calle se duplica en cada actualización. Es el mismo bug que se corrigió en cliente en la 1.9.
- 2026-10-13 · `ModificarClienteUseCase` y `ModificarProveedorUseCase` filtran las direcciones vacías: modificar borra una dirección sin calle junto con su localidad.
- 2026-10-13 · `ProveedorRepositorio.ActualizarAsync` devuelve void y sale en silencio si no encuentra la fila (equivalente al Crítico 3 de cliente).
- 2026-10-13 · `ManejadorExcepcionesGlobal`: a la lista blanca le faltan `ux_factura_venta_numero_activa`, `cliente_telefono_cliente_id_numero_key` y `proveedor_telefono_proveedor_id_numero_key`.
- 2026-10-13 · No hay test de falla a mitad de camino en altas y actualizaciones de cliente y proveedor. Hoy es un solo SaveChanges; hace falta cuando la Fase 2 sume escrituras.
- 2026-10-13 · `CicloCompletoIntegracionTests` mockea los repositorios: prueba el pipeline HTTP, no la persistencia. El nombre promete más de lo que verifica. El recorrido real contra PostgreSQL es de la Fase 7.
- 2026-10-13 · La baja de usuario solo tiene tests InMemory o con mocks (`UsuarioRepositorioTests` usa InMemory).
- 2026-10-13 · **Compuerta de esquema para las Fases 3 y 6:** no hay dónde persistir precios por modalidad con nombre (D1). `articulo_margen` usa la clave `numero` (listas 1/2/3) y hay una sola columna `precio_venta_estimado`.
- 2026-10-13 · EF Core difiere entre Data (10.0.11) y Api/Tests (10.0.12, por InMemory y Mvc.Testing). La Api referencia `EntityFrameworkCore.InMemory` en producción.
- 2026-10-13 · Pendiente de confirmar por el usuario: si existe `ConnectionStrings__Postgres` en el entorno, a qué base apunta. Los tests WebApplicationFactory resolverían ese DbContext; hoy los repositorios están mockeados. El auditor no pudo inspeccionarlo.

## Premisas del plan que no coinciden con el código (revisar antes de la fase)
- 2026-10-13 · **Fase 2:** el plan dice "NINGÚN repositorio escribe en [las tablas de historial]". Falso para cliente y proveedor: `LaCentral.Data/Repositorios/ClienteRepositorio.cs:176-191` y `ProveedorRepositorio.cs:154-169` ya abren y cierran la baja en `*_historial_baja` dentro del repositorio, sin puerto. Lo cubren los tests PG `DosCiclos..._ConservanEstadoEHistorial...`. Artículo y usuario no escriben historial. La Fase 2 pasa a ser extraer un puerto `IHistorialBajaRepositorio` y mover esa lógica, más los usuarios de baja/reactivación desde IContextoUsuario, no construirla desde cero.
- 2026-10-13 · **Fase 4:** el plan dice que el test del comodín ("100%_original") corre sobre InMemory. Hoy `ArticuloRepositorioTests.Busqueda_PermiteInactivosYNoInterpretaComodinesSQL` es `[PostgreSqlFact]` y corrió contra PostgreSQL real. Verificar si cubre ese caso exacto antes de duplicarlo.

## Revisión de la Fase 1 por el equipo (2026-10-13)
- Fase 1 verificada contra el grafo (`bf845dbc`): los nueve ítems están. El PR todavía no se abrió (no hay `gh`): abrirlo a mano desde la rama `fix/deuda-bloqueante-sprint3` con el cuerpo de `pr-fase1-deuda-bloqueante.md`.
- Confirmado: 1.7 (sin `LA_CENTRAL_REQUIRE_POSTGRES`), 1.3 (`ObtenerPorCodigoAsync`) y 1.9 (era `ClienteDireccion.Calle`).
- La compuerta de esquema de modalidades es de las Fases 3 Y 6: ART-01 da de alta la configuración de precios. **La resuelve el equipo con un script SQL antes de la Fase 3. El agente NO toca el esquema.**
- `ConnectionStrings__Postgres` NO existe en el entorno (verificado en las tres carpetas): los tests HTTP no resuelven ningún DbContext real. `LACENTRAL_TEST_PG` ahora es persistente.
- Los subagentes y los hooks de graphify se copiaron a `backend/.claude/` (agents/ y settings.json). En la sesión de la Fase 1 NO cargaron, porque se leen al iniciar la sesión: `Agent type 'verificador-tdd' not found`. Se reinicia la sesión antes de la Fase 2.

## PRÓXIMA SESIÓN — arrancar acá
0. **Al empezar:** confirmar que `verificador-tdd` y `auditor-persistencia` cargan como subagentes de verdad (subagent_type, no texto leído) y que los hooks de graphify se disparan. Si no cargan, PARAR y reportar.
1. **Verificación previa a la Fase 2:** revisar si `LaCentral.Tests/Articulo/ArticuloTest.cs` y `LaCentral.Tests/Articulos/ArticuloPrecioEstimadoTest.cs` se contradicen. Uno tiene `ObtenerPrecioEstimado_CostoAlCeroPorciento_RetornaMismoCosto` (D1: Costo = ×1,00) y el otro `CalcularPrecioEstimado_MargenMinimo_CalculaCorrectamente`, que suena al viejo 0,01 %. Si las dos reglas están en verde a la vez, la regla vieja sigue viva. Unificar en `Articulos/` con el namespace correcto (hoy `ArticuloTest.cs` está en `LaCentral.Tests.Entidades`) y reportar qué se encontró.
2. **FASE 2**, rama `feature/d10-historial-baja`, en este orden:
   1. Extraer `IHistorialBajaRepositorio` de la lógica inline de `ClienteRepositorio` y `ProveedorRepositorio`, llevarla al puerto y extenderla a articulo y usuario.
   2. `usuario_baja_id` y `usuario_reactivacion_id` salen de `IContextoUsuario`. `Cliente.UsuarioBajaId` existe en el dominio y no se persiste: persistirlo. `FechaReactivacion` la fija el caso de uso, no el repositorio (el mismo patrón que se quitó en 1.4).
   3. `ProveedorRepositorio.ActualizarAsync` devuelve bool y los casos de uso lo consumen (es el CRÍTICO 3 sin corregir en proveedor).
   4. Bug de dirección sin calle en `ProveedorRepositorio` (`b.Calle == d`), el mismo de 1.9.
   5. `ModificarCliente` y `ModificarProveedor` filtran las direcciones vacías: modificar borra una dirección sin calle junto con su localidad. Pérdida de datos silenciosa, presente en develop.
   6. Fixture: renombrar los índices únicos del esquema aislado a su nombre original. NO agregar los nombres generados a la lista blanca.
   7. Lista blanca: agregar `ux_factura_venta_numero_activa`, `cliente_telefono_cliente_id_numero_key` y `proveedor_telefono_proveedor_id_numero_key`.
   8. Unificar `ObtenerPorCodigoAsync` en los dos puertos (`IClienteRepositorio` todavía tiene `ExisteCodigoAsync` y no puede informar CUÁL cliente choca).
   - **Tests obligatorios contra PostgreSQL:**
     - baja → reactivación → baja deja DOS registros con motivos y fechas distintas;
     - dos bajas abiertas simultáneas dan 23505 en el índice único parcial;
     - la reactivación cierra la baja abierta y conserva motivo y fecha en la fila;
     - test de falla a mitad de camino: la Fase 2 suma escrituras y la atomicidad deja de estar dada por construcción.
   - **Al cerrar:** los dos subagentes, el PR, ESTADO.md, y PARAR.
3. Base de la rama: el PR de la Fase 1 todavía no está mergeado en develop. Si al arrancar sigue sin mergear, preguntar si la Fase 2 sale de `fix/deuda-bloqueante-sprint3` o se espera el merge.
