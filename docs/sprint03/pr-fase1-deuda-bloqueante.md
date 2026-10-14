# PR · Fase 1 Sprint 3 — Deuda que bloquea todo lo demás

**Rama:** `fix/deuda-bloqueante-sprint3` → `develop`
**Título sugerido:** `fix: deuda bloqueante del Sprint 3 (Fase 1)`

## Qué hace

| Ítem | Cambio |
|---|---|
| 1.1 ALTO 6 | `ManejadorExcepcionesGlobal` (`IExceptionHandler`) + `AddProblemDetails` + `UseExceptionHandler` en `Program.cs`. Una 23505 sobre una restricción de la **lista blanca** → 409 con un mensaje para el usuario. Cualquier otra excepción, incluida una 23505 sobre una restricción no reconocida → 500 genérico. Ninguna respuesta lleva mensaje, tipo ni stack. |
| 1.2 CRÍTICO 3 | `Cliente.Id`. `ActualizarAsync` busca por **Id** y devuelve `bool`. Modificar, baja y reactivación devuelven NoEncontrado si la fila no existe. Modificar rechaza con Conflicto el código de otro cliente. Antes: cambiar el código de A al de B **sobrescribía a B**, y cambiarlo a uno libre no guardaba nada y devolvía éxito. |
| 1.3 ALTO 5 | `IProveedorRepositorio.ObtenerPorCodigoAsync`. El alta valida el código en el núcleo informando **cuál** es el proveedor en conflicto. Ante concurrencia decide el índice `proveedor_codigo_key`, que 1.1 traduce a 409. |
| 1.4 | `Proveedor.FechaBaja`. La fija el caso de uso; el repositorio la persiste y la lee, y ya no la estampa por su cuenta. |
| 1.5 | `Articulo.MotivoBaja` / `FechaBaja` en la entidad de dominio. |
| 1.6 | `enum Modalidad` (Lista, Tarjeta3, Tarjeta6, EfectivoTransferencia, Costo, CasoEspecial) reemplaza a `MargenModalidad.ModalidadId int`. No toca el esquema. |
| 1.7 | `[PostgreSqlFact]` **nunca se omite**: sin conexión, falla con un mensaje que remite a `LaCentral.Tests/README.md`, que es nuevo. `LA_CENTRAL_REQUIRE_POSTGRES` deja de usarse: exigir PostgreSQL es el único comportamiento. |
| 1.8 | Referencia explícita a `Microsoft.EntityFrameworkCore.Relational 10.0.11` en Data. Tests cargaba 10.0.4 porque `Design` (PrivateAssets) no propaga la 10.0.11. |
| 1.9 | 0 warnings. `Direcciones` con `Calle` null se mapea a `""`, según la convención documentada del repositorio y igual que Proveedor; la sincronización compara con la misma convención. `CrearClienteRequest` declara anulables las listas que el caso de uso ya toleraba. Sin usar `!`. |

## Qué NO hace

- No toca el esquema ni `Data/Models/`. No agrega referencias a UseCases (su `.csproj` sigue sin `PackageReference` ni `ProjectReference`).
- No mapea `Articulo` a persistencia: hoy no existe implementación de `IArticuloRepositorio`. 1.5 y 1.6 quedan en el dominio.
- No toca el historial de baja (Fase 2).
- `ProveedorRepositorio.ActualizarAsync` sigue sin devolver `bool` (ver Pendientes).

## Cómo se verificó

- `dotnet build --no-incremental`: **0 advertencias, 0 errores** (antes: 4).
- `dotnet test`: **129 total · 129 passed · 0 failed · 0 skipped** (antes: 108).
- Sin `LACENTRAL_TEST_PG`: los **17** tests de PostgreSQL **fallan** con el mensaje del README. Antes quedaban omitidos.
- Tests nuevos (+21):
  - **PostgreSQL real (+9, total 17):** se relee siempre con un contexto nuevo.
    - `ModificarClientePostgreSqlTests` ×4: código de B → Conflicto y B intacto; código libre → persiste; Id inexistente en el repositorio → false sin tocar al dueño del código; Id inexistente en el caso de uso → NoEncontrado.
    - `ProveedorPostgreSqlTests` ×2: código repetido → Conflicto identificando al primero; la baja trae motivo y fecha.
    - `ClienteRepositorioTests` +1: dirección sin calle.
    - `RestriccionesUnicasPostgreSqlTests` ×2: cada nombre de la lista blanca existe en el esquema real; un duplicado real es reconocido.
  - **WebApplicationFactory (+3):** `ManejoGlobalExcepcionesTests`, corre en Development, el entorno donde la página del desarrollador expondría el stack.
  - **Moq (+6):** Modificar, DarDeBaja y Reactivar de cliente traducen false a NoEncontrado; código repetido en Modificar cliente y en Crear proveedor; fecha de baja de proveedor.
  - **Unitarios (+3):** `PostgreSqlObligatorioTests` ×2 (colección sin paralelismo, porque vacía variables de entorno) y `VersionesEfCoreTests`.
- TDD: cada unidad tiene un commit `test(...)` en rojo antes del commit de producción. Algunos commits de test incluyen solo el andamiaje para compilar (firma o propiedad, sin comportamiento), declarado en el mensaje.
- Revisión de los subagentes **verificador-tdd** y **auditor-persistencia**: los dos dieron **APTO**.
  - El verificador hizo checkout de cada commit de test, confirmó el rojo por la razón correcta y aplicó 7 mutaciones en HEAD: todas detectadas.
  - El auditor confirmó contra el catálogo del motor y en solo lectura que la lista blanca existe en la plantilla.

## Excepciones a TDD, declaradas

- **Tests que pasaron a la primera, verificados por mutación:**
  - `RestriccionesUnicasPostgreSqlTests`: resguardo escrito después de la implementación. Renombrar una clave lo pone en rojo.
  - `ModificarConIdInexistente_DevuelveNoEncontradoYNoTocaNada`: caracterización pedida por el verificador. Quitar el chequeo de null lo pone en rojo.
- **1.5 sin test:** ningún código lee ni escribe la entidad `Articulo`. La prueba contra PostgreSQL bajo el CHECK `articulo_baja_coherente` va en HU-ART-03.
- **Refactors en verde:**
  - 1.6: el test "modalidad 99" pasó a "modalidad sin margen cargado", verificado por mutación.
  - `CrearClienteRequest` con listas anulables.
- **Rojo de 1.3 contra PostgreSQL:** fue un `DbUpdateException 23505`, no un assert. Es el síntoma exacto (el núcleo no validaba) y el verificador lo aceptó. El test Moq del mismo commit sí falla en el assert.

## Decisiones

- **1.3:** se implementó `ObtenerPorCodigoAsync` y no `ExisteCodigoAsync`, porque el criterio exige identificar al proveedor en conflicto y un `bool` no alcanza.
- **1.9:** el warning no era el `Cuit`, que ya es nullable en la entidad, sino `ClienteDireccion.Calle`. Se siguió la convención del repositorio (sin dato = `""` en el dominio, NULL en la base) en lugar de `List<string?>`. Con solo el cambio de mapeo, baja y reactivación **duplicaban** la fila; el test lo detecta.

## Pendiente y por qué

Detectado en esta fase, no arreglado por estar fuera de alcance. Con la fase sugerida:

1. **Fase 2:** las escrituras de historial **no guardan `usuario_baja_id` ni `usuario_reactivacion_id`** (`ClienteRepositorio.cs:183-188`, `ProveedorRepositorio.cs` en `ActualizarAsync`). `Cliente.UsuarioBajaId` no se persiste. `FechaReactivacion` la estampa el repositorio, el mismo patrón que se quitó en 1.4. Usuario no escribe `usuario_historial_baja`.
2. **Fase 2:** el fixture (`CREATE TABLE ... LIKE ... INCLUDING ALL`) **renombra los índices únicos**. En el esquema aislado, `codigo_unico_global`, `un_codigo_por_proveedor`, `ux_*_hist_abierta` y `ux_factura_compra_numero_activa` tienen nombres generados. El auditor lo confirmó contra el motor. Hoy no afecta a ningún test, pero un test de 409 sobre esas restricciones daría 500. **No agregar los nombres generados a la lista blanca.** Arreglarlo en el fixture.
3. **Alta prioridad:** el mismo bug de dirección sin calle de 1.9 sigue en `ProveedorRepositorio` (`b.Calle == d`), y estaba en develop. Duplica la dirección en silencio en cada actualización.
4. `ModificarCliente` y `ModificarProveedor` filtran las direcciones vacías, así que **modificar borra una dirección sin calle** junto con su localidad.
5. `ProveedorRepositorio.ActualizarAsync` sigue devolviendo `void`: lo que 1.2 hizo para cliente.
6. A la lista blanca le faltan `ux_factura_venta_numero_activa` y las unicidades de teléfono: hoy salen como 500.
7. No hay test de falla a mitad de camino en altas y actualizaciones de cliente y proveedor. Hoy es un solo `SaveChanges`, así que la atomicidad está dada por construcción. Hace falta en la Fase 2.
8. `CicloCompletoIntegracionTests` mockea los repositorios: prueba el pipeline HTTP, no la persistencia. El nombre promete más de lo que verifica. El recorrido real contra PostgreSQL es de la Fase 7.
9. La baja de usuario solo tiene tests InMemory o con mocks.
10. **Para la Fase 3/6:** el esquema **no tiene dónde persistir precios por modalidad con nombre**. `articulo_margen` usa la clave `numero`, que son las listas 1/2/3, y hay una sola columna `precio_venta_estimado`. D1 dice "persistidos por artículo". Hay que decidirlo antes de la Fase 6; es compuerta de esquema.
11. EF Core (no Relational) difiere entre Data (10.0.11) y Api/Tests (10.0.12, por InMemory y Mvc.Testing 10.0.12). La Api referencia `EntityFrameworkCore.InMemory` en producción.