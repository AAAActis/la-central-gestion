# PR · Fase 2 Sprint 3 — D10 en la aplicación

**Rama:** `feature/d10-historial-baja` → **`fix/deuda-bloqueante-sprint3`** (base provisoria)
**Título sugerido:** `feat: historial de baja D10 con puerto y unidad de trabajo (Fase 2)`

> ⚠️ **Base provisoria.** Esta rama sale de `fix/deuda-bloqueante-sprint3` (Fase 1, `108ac1d`), que
> todavía está en revisión. **Cuando la Fase 1 entre a `develop`, hay que cambiar la base de este PR
> a `develop` y revisar el diff ahí.** Tienen que quedar solo los commits de la Fase 2:
> `b95744f`..`a105f6d`, los arreglos del 2026-10-14 y los de docs.

## Qué hace

| Ítem | Cambio |
|---|---|
| Previo | `ArticuloTest.cs` y `ArticuloPrecioEstimadoTest.cs` se unifican en `Articulos/ArticuloPrecioEstimadoTests.cs`. **No había contradicción:** los dos prueban `costo × (1 + margen/100)`. El caso del 0,01 % es una prueba de precisión sobre **Lista**, no la vieja regla de Costo. Se elimina un duplicado (`SinCosto`). |
| 2.1 | Puerto **`IHistorialBajaRepositorio`** (`AbrirAsync`, `CerrarAsync`, `ObtenerAsync`) para cliente, proveedor, artículo y usuario. Sale de la lógica inline de `ClienteRepositorio` y `ProveedorRepositorio.ActualizarAsync`. Puerto **`IUnidadDeTrabajo`**: la baja (o la reactivación) y su historial van en una sola transacción. Un fallo devuelto o una excepción descartan las dos escrituras. |
| 2.2 | `usuario_baja_id` y `usuario_reactivacion_id` salen de `IContextoUsuario`. Sin usuario en sesión, la operación da **NoAutorizado**. `Cliente.UsuarioBajaId` se persiste. `FechaReactivacion` la fija el caso de uso. Respuesta a la pregunta del plan: **antes, ninguna escritura guardaba `usuario_baja_id`**. |
| 2.2 (2026-10-14) | **`usuario.usuario_baja_id` se perdía.** Las lecturas de `UsuarioRepositorio` no lo traían y `ActualizarAsync` lo escribía en NULL: por ejemplo, restablecer la contraseña de un usuario inactivo borraba quién lo dio de baja. Ahora las dos lecturas lo mapean. `ReactivarUsuarioUseCase` lo limpia de forma explícita, junto con el motivo y la fecha. Antes lo limpiaba solo por accidente del mapeo. |
| 2.3 | `ProveedorRepositorio.ActualizarAsync` devuelve `bool`. Modificar, dar de baja y reactivar proveedor informan NoEncontrado. |
| 2.4 | Dirección sin calle en `ProveedorRepositorio`: se duplicaba en cada actualización. |
| 2.5 | `ModificarCliente` y `ModificarProveedor` ya no filtran las direcciones vacías. Antes, modificar **borraba la dirección sin calle junto con su localidad**. |
| 2.6 | Fixture: `LIKE ... INCLUDING ALL` renombraba los índices del esquema aislado. Ahora se aparean con los originales y se renombran como en el origen. |
| 2.7 | Lista blanca del manejador: `ux_factura_venta_numero_activa` y los dos `*_telefono_*_numero_key` (antes, 500). |
| 2.7 (2026-10-14) | La lista blanca suma los CHECK **`*_hist_fechas_coherentes`** de las cuatro tablas de historial (23514 → 409). Una fecha de baja futura (backfill o reloj desfasado) hacía que la reactivación diera 500. `MensajeDeConflicto` acepta también la `PostgresException` directa, que es lo que lanza `ExecuteUpdate`, sin `DbUpdateException`. Un CHECK que no está en la lista sigue dando 500. |
| 2.8 | `IClienteRepositorio.ObtenerPorCodigoAsync` reemplaza a `ExisteCodigoAsync`: se informa **cuál** cliente tiene el código. |

## Qué NO hace

- No toca el esquema ni `Data/Models/`. UseCases sigue sin `PackageReference` ni `ProjectReference`.
- **Artículo:** el puerto soporta `articulo_historial_baja` y está probado contra PostgreSQL, pero falta el caso de uso de baja de artículo. Es de HU-ART-03 (Fase 7).
- `ReactivarUsuarioUseCase` limpia de la fila de usuario el motivo, la fecha y `usuario_baja_id`. El rastro queda en `usuario_historial_baja`. Cliente y proveedor los conservan en la fila.
- La tabla de proveedor no tiene columna `usuario_baja_id`: el usuario queda solo en el historial.

## Cómo se verificó (2026-10-14, HEAD `d378c0f`)

- `dotnet build --no-incremental`: **0 advertencias, 0 errores**.
- `dotnet test`: **176 total · 176 passed · 0 failed · 0 omitidos** (la base, Fase 1, tenía 129).
- **Contra PostgreSQL real: 43 tests** (la base tenía 17). Son 42 con esquema aislado y 1 de catálogo. Todo se relee con un contexto nuevo.
- Los dos subagentes corrieron sobre `git diff 108ac1d d378c0f`. Los veredictos y el detalle están en `docs/sprint03/ESTADO.md`:
  - **auditor-persistencia: APTO.** Cierra sus hallazgos 1, 2 y 3. Verificó el esquema contra el motor (catálogo de `lacentral_test`).
  - **verificador-tdd:** su NO APTO anterior era por este cuerpo de PR. El veredicto sobre esta versión está en ESTADO.md.
- Hasta el 2026-10-14 la suite PG no se pudo correr por un `28P01`. Era una credencial vieja en la variable de entorno, no el código.

## TDD: lo que el historial muestra y lo que no

**Los 8 commits originales (`b95744f`..`a105f6d`): el orden test → producción NO se puede verificar desde el historial.**
- `b95744f` es el único commit que solo toca tests, y es una unificación de tests existentes.
- `e6a2d3b` trae el test junto con el arreglo del fixture `PostgreSqlPruebas.cs`.
- Los otros 6 traen el test y el código de producción en el mismo commit.
- Que esos tests "se vieron fallar antes" es una declaración del autor, no algo que git demuestre.
- **No se reescribe el historial para simular un rojo que no existió: se declara.**

Lo que sí se comprobó después, por mutación (el verificador-tdd, el 2026-10-14, en un worktree aparte, sin commitear):
- Los tests Moq de 2.1, 2.2, 2.3, 2.5 y 2.8 dan rojo por aserción cuando se revierte la línea de producción que protegen. Fueron 8 experimentos.
- **Excepción declarada · los 7 PG de `HistorialBajaRepositorioTests`:** en su momento vieron su rojo **contra stubs `NotImplementedException`, no por aserción**. Una excepción no es un rojo válido. El 2026-10-14 se les buscó el rojo por mutación, y los 7 dan rojo con al menos una de estas:
  - `AbrirAsync` que no escribe;
  - `CerrarAsync` que devuelve true sin escribir;
  - `RollbackAsync` cambiado por `CommitAsync`.
  - En las dos primeras, el verificador solo confirmó el [FAIL], sin revisar la causa de cada uno.

**Los arreglos del 2026-10-14 sí vienen con commits separados:**
- `a516caa` (test) → `83f0f7c` (fix), `usuario_baja_id`.
  - En `a516caa`, dos tests dan rojo por aserción: L37 `ObtenerPorIdAsync` y L63 después de restablecer la contraseña, los dos `Expected 1, Actual null`.
  - El tercero (`Reactivar_LimpiaLaFila...`) es de caracterización y pasaba. Protege la limpieza explícita de `83f0f7c`: si se comenta esa línea, da rojo en L86.
- `f00a0ae` (test) → `d378c0f` (fix), `*_hist_fechas_coherentes`.
  - En `f00a0ae`, los dos dan rojo por aserción (L340 y L369), con el 23514 real del motor sobre `cliente_hist_fechas_coherentes`.
- **Excepción declarada · `b789b0e`, los 4 tests de falla a mitad de camino: pasaron a la primera y no tienen commit de producción.**
  - `UnidadDeTrabajo` ya revertía bien, así que no había nada que corregir y no se inventó un cambio.
  - Se probó que discriminan con una mutación **sin commitear**: `UnidadDeTrabajo.EjecutarAsync` sin transacción (corre la operación directo, sin `BeginTransaction`). Con eso, los 4 dan rojo por aserción (L237, L261, L286, L311), porque la fila queda escrita.
  - El verificador también saltó la unidad de trabajo en cada caso de uso por separado: cada test da rojo solo con la mutación de su propio caso de uso.

## Falla a mitad de camino: qué cubren los tests y qué no

Las 6 operaciones de baja y reactivación tienen un test contra PostgreSQL que provoca una falla **después** de actualizar la fila. Los 6 releen la fila y el historial con un contexto nuevo.

| Operación | Cómo falla | Test |
|---|---|---|
| DarDeBaja cliente | **real del motor:** una baja abierta huérfana → 23505 en `ux_cliente_hist_abierta` | `DarDeBaja_FallaAlAbrirHistorial_LaFilaNoQuedaInactiva` |
| DarDeBaja proveedor | real del motor, 23505 | `DarDeBajaProveedor_FallaAlAbrirHistorial_...` |
| DarDeBaja usuario | real del motor, 23505 | `DarDeBajaUsuario_FallaAlAbrirHistorial_...` |
| Reactivar cliente | **forzada:** `IHistorialBajaRepositorio` mockeado que lanza al cerrar. También **real del motor:** 23514 por baja futura | `ReactivarCliente_FallaAlCerrarHistorial_...`, `ReactivarCliente_ConBajaFutura_...` |
| Reactivar proveedor | forzada (mock) | `Reactivar_FallaAlCerrarHistorial_LaFilaSigueInactiva` |
| Reactivar usuario | forzada (mock). El rollback devuelve motivo, fecha y `usuario_baja_id` | `ReactivarUsuario_FallaAlCerrarHistorial_...` |

En los casos forzados, el repositorio de la entidad, la unidad de trabajo y el contexto son reales. Solo el historial es un doble.

**Lo que NO cubren:**
- Reactivar proveedor y usuario no tienen una falla real del motor, solo la excepción del mock. Alcanza para probar el rollback del UPDATE, pero no ejercita un error de PostgreSQL en el `ExecuteUpdate`.
- Concurrencia: dos `DarDeBaja*` simultáneos, cada uno en su transacción. Solo está probado a nivel de repositorio, con dos `AbrirAsync` en autocommit.
- La composición de la API: repositorios y unidad de trabajo sobre el DbContext scoped de `Program.cs`. No tiene un test PG de punta a punta y se sostiene por lectura.
- Las altas y modificaciones de cliente y proveedor no tienen test de falla. Son un solo `SaveChanges`.

## Pendientes

No bloqueantes, señalados por los subagentes el 2026-10-14:
- **`*_hist_motivo_no_vacio`** no tiene test contra el motor y no está en la lista blanca del manejador. Hoy los casos de uso no llegan a violarlo, porque `IsNullOrWhiteSpace` es más estricto que `btrim`. Si llegara a violarse, saldría 500.
- **Falta un test de que un CHECK no reconocido siga dando 500**, por ejemplo `cliente_baja_coherente`. Si alguien hiciera que todo 23514 diera 409, ningún test lo detectaría.
- **Falta el test HTTP (`WebApplicationFactory`) del 409 por CHECK.** Hoy se prueba el estático `MensajeDeConflicto`, no el pipeline.
- **Cliente y proveedor guardan el motivo de baja sin `Trim()`** (`DarDeBajaClienteUseCase.cs:49`, `DarDeBajaProveedorUseCase.cs:59`). Usuario sí lo recorta.

Deuda que ya estaba declarada:
- Concurrencia en transacción (hallazgo 4 del auditor), ver arriba.
- Los dobles `ProveedorInactivo()` y `UsuarioInactivo()` de `HistorialBajaCasosDeUsoTests` tienen motivo pero no tienen fecha: el CHECK prohíbe ese estado.
- La guarda `duenioDelCodigo.Id != cliente.Id` de `ModificarClienteUseCase` no tiene test y probablemente nunca se alcanza.
- `CicloCompletoIntegracionTests` mockea los repositorios, el historial y la unidad de trabajo. Prueba el pipeline HTTP, no la persistencia. El recorrido real es de la Fase 7.
- Baja y reactivación de **artículo**: Fase 7, HU-ART-03. El puerto ya está.

Sin verificar:
- **Que la base principal tenga aplicado `01_Ajustes_Sprint3.sql`** igual que `lacentral_test`. Está prohibido apuntarle. Todo lo de este PR vale para la base de test.

Fuera de este PR:
- La compuerta de esquema D1 sigue abierta para las Fases 3 y 6. El DDL del motor está en `docs/sprint03/informe-compuerta-d1.md`. El script SQL lo escribe el equipo.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
