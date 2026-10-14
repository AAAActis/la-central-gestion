# PR · Fase 2 Sprint 3 — D10 en la aplicación

**Rama:** `feature/d10-historial-baja` → **`fix/deuda-bloqueante-sprint3`** (provisorio)
**Título sugerido:** `feat: historial de baja D10 con puerto y unidad de trabajo (Fase 2)`

> ⚠️ **Base provisoria.** Esta rama sale de `fix/deuda-bloqueante-sprint3` (Fase 1), que todavía
> está en revisión. **Cuando la Fase 1 entre a `develop`, cambiar la base de este PR a `develop`
> y revisar el diff ahí**: tiene que quedar solo lo de la Fase 2 (8 commits, de `b95744f` a `a105f6d`).

## Qué hace

| Ítem | Cambio |
|---|---|
| Previo | `ArticuloTest.cs` (namespace `LaCentral.Tests.Entidades`) y `ArticuloPrecioEstimadoTest.cs` (sin namespace) se unifican en `Articulos/ArticuloPrecioEstimadoTests.cs`. **No había contradicción:** los dos prueban la misma fórmula `costo × (1 + margen/100)`. El caso de 0,01 % es sobre **Lista** (precisión de la fórmula), no la vieja regla de Costo; no queda ninguna constante de 0,01 % en el código. Se elimina un duplicado (`SinCosto`). |
| 2.1 | Puerto **`IHistorialBajaRepositorio`** (`AbrirAsync`, `CerrarAsync`, `ObtenerAsync`) para cliente, proveedor, artículo y usuario, extraído de la lógica inline de `ClienteRepositorio` y `ProveedorRepositorio.ActualizarAsync`. Puerto **`IUnidadDeTrabajo`**: la baja (o reactivación) y su historial ocurren en una transacción; un fallo o una excepción descartan las dos escrituras. Los casos de uso de baja y reactivación de cliente, proveedor y usuario lo usan. |
| 2.2 | `usuario_baja_id` y `usuario_reactivacion_id` salen de `IContextoUsuario`. Sin usuario en sesión, la operación da **NoAutorizado** (igual que ya hacía la baja de usuario). `Cliente.UsuarioBajaId` se persiste y se lee. `FechaReactivacion` la fija el caso de uso. Respuesta a la pregunta del plan: **antes, ninguna escritura guardaba `usuario_baja_id`**. |
| 2.3 | `ProveedorRepositorio.ActualizarAsync` devuelve `bool`; modificar, dar de baja y reactivar proveedor informan NoEncontrado (CRÍTICO 3 en proveedor). |
| 2.4 | Dirección sin calle en `ProveedorRepositorio`: comparaba `b.Calle == d` y la duplicaba en cada actualización. Mismo bug que la 1.9 en cliente. |
| 2.5 | `ModificarCliente` y `ModificarProveedor` ya no filtran las direcciones vacías: modificar devolviendo lo leído **borraba la dirección sin calle y su localidad** (pérdida silenciosa, presente en develop). El repositorio conserva la que existía y no crea una fila vacía nueva. |
| 2.6 | Fixture: `LIKE ... INCLUDING ALL` renombraba los índices del esquema aislado. Ahora se aparean por tabla, unicidad y definición y se renombran al original. **No** se agregaron nombres generados a la lista blanca. |
| 2.7 | Lista blanca del manejador: `ux_factura_venta_numero_activa`, `cliente_telefono_cliente_id_numero_key`, `proveedor_telefono_proveedor_id_numero_key` (antes, 500). |
| 2.8 | `IClienteRepositorio.ObtenerPorCodigoAsync` reemplaza a `ExisteCodigoAsync`: alta y modificación informan **cuál** cliente tiene el código. |

## Qué NO hace

- No toca el esquema ni `Data/Models/`. UseCases sigue sin `PackageReference` ni `ProjectReference`.
- **Artículo:** el puerto soporta `articulo_historial_baja` y está probado contra PostgreSQL, pero no hay caso de uso de baja de artículo: eso es HU-ART-03 (Fase 7).
- `ReactivarUsuarioUseCase` sigue limpiando `motivo_baja` y `fecha_baja` de la fila de usuario (D10 lo permite). El rastro queda en `usuario_historial_baja`. Cliente y proveedor los conservan en la fila.
- Proveedor no tiene columna `usuario_baja_id` en su tabla: el usuario queda solo en el historial.

## Cómo se verificó

- `dotnet build --no-incremental`: **0 advertencias, 0 errores**.
- `dotnet test`: **167 total · 167 passed · 0 failed · 0 skipped** (antes: 129).
- **PostgreSQL real: 34 tests** (antes 17, **+17**). Siempre se relee con un contexto nuevo.
  - `HistorialBajaRepositorioTests` ×7:
    - baja → reactivación → baja en **las cuatro tablas**: dos registros con motivos, fechas y usuarios distintos; el cerrado conserva motivo y fecha;
    - dos bajas abiertas **simultáneas** (`Task.WhenAll`, dos conexiones) en las cuatro tablas: una gana y la otra da 23505;
    - cerrar sin baja abierta devuelve false (inactivos importados de Multisoft sin historial);
    - casos de uso de cliente, proveedor y usuario: los usuarios salen de la sesión y el cliente persiste `usuario_baja_id`;
    - **falla a mitad de camino:** baja con una baja abierta huérfana (23505 después de actualizar la fila) → la fila sigue activa; reactivación con un historial que lanza después de actualizar → la fila sigue inactiva; unidad de trabajo que devuelve fallo → no queda nada escrito.
  - `ProveedorPostgreSqlTests` +3: Id inexistente → false; modificar conserva la dirección sin calle con su localidad; no crea una dirección vacía.
  - `ProveedorRepositorioTests` +1: dirección sin calle no se duplica en baja → reactivación.
  - `ModificarClientePostgreSqlTests` +3: dirección sin calle conservada; sin fila vacía nueva; alta con código repetido identifica al cliente.
  - `RestriccionesUnicasPostgreSqlTests` +3: el esquema aislado conserva los nombres de índices del origen; baja abierta duplicada → `ux_cliente_hist_abierta` reconocida; teléfonos y factura de venta duplicados → reconocidos.
- **Moq: +22.** `HistorialBajaCasosDeUsoTests` ×15 (abre y cierra con usuario y fecha; NoAutorizado sin sesión; las escrituras ocurren dentro de la unidad de trabajo); NoEncontrado de proveedor ×3; direcciones vacías ×2; código de cliente en conflicto ×2.
- TDD: cada test nuevo se vio fallar por aserción antes del código de producción. Excepciones, declaradas:
  - `DarDeBajaCliente_FilaInexistenteAlActualizar...` pasó a la primera: es de caracterización (el NoEncontrado ya existía) y protege que no se abra historial.
  - Los dos `ModificarConDireccionVaciaNueva_NoCreaUnaFilaVacia` pasaban con el filtro viejo. Se vieron fallar al quitarlo, antes de agregar la guarda del repositorio.
  - Los 7 PG del repositorio de historial fallaron primero contra stubs `NotImplementedException`.
- `verificador-tdd` y `auditor-persistencia`: ver el resultado en `docs/sprint03/ESTADO.md`.

## Pendiente

- Baja y reactivación de **artículo** (Fase 7, HU-ART-03): el puerto ya está.
- `CicloCompletoIntegracionTests` sigue mockeando los repositorios (y ahora también el historial y la unidad de trabajo): prueba el pipeline HTTP, no la persistencia. El recorrido real es de la Fase 7.
- La compuerta de esquema de modalidades (D1) sigue abierta para las Fases 3 y 6: la resuelve el equipo con un script SQL.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
