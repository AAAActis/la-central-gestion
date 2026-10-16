# PR · Fase 4 Sprint 3 — HU-ART-02 · Consulta de artículos

**Rama:** `feature/HU-ART-02-consultar-articulos` → **`develop`**
**Título sugerido:** `feat: busqueda y ficha de articulo con razon de ausencia de precio, y pantalla en Angular (HU-ART-02)`
**Issue:** Closes #15

> La Fase 3 (`feature/HU-ART-01-alta-articulo`) ya está en `develop` (#117), así que el diff contra `develop` es solo de la Fase 4.

## Qué hace

### Backend
- **`ConsultarArticulosUseCase`**: aplica `ConsultaArticulos.Validar()`, que ahora devuelve `Result` en lugar de lanzar `ArgumentException`. El repositorio ya no valida.
  - Texto vacío, en blanco o ausente → **400**.
  - Alternativas inválidas o paginación fuera de rango (página < 1, tamaño fuera de 1..100) → **400**.
  - Sin resultados → **200 con lista vacía**, nunca 404.
- **`ObtenerArticuloDetalleUseCase`**: la ficha devuelve **SIEMPRE las seis modalidades**. Si una no tiene precio, informa la razón (`RazonAusenciaPrecio`), en este orden:
  | Situación | Razón |
  |---|---|
  | no hay fila para esa modalidad | `SinConfiguracion` |
  | `calculo_manual` y precio null | `CasoEspecialNoFijado` |
  | sin base (Tarjeta3/6) y precio null | `TasaSinDefinir` |
  | con base y precio null (falta el costo, directo o vía Lista) | `SinCosto` |
  Los precios son los que están guardados en la tabla, sin recalcular. Un precio ausente nunca sale como cero.
- **`ArticuloRepositorio`**:
  - cada resultado de la búsqueda trae su **Id** y sus **códigos de proveedor** (id, razón social, código tal como se cargó). Los códigos salen de una consulta aparte por los Ids de la página, para que la paginación no se multiplique;
  - `PreciosPorModalidad` deja de ser anulable: sin filas, devuelve una lista vacía.
- **`GET /api/articulos?texto=&incluirInactivos=false&pagina=1&tamanoPagina=20`** y **`GET /api/articulos/{id}`**, para ADMINISTRADOR y OPERADOR. El filtro de activos y la paginación son explícitos, y la respuesta trae `total`, `pagina` y `tamanoPagina`.
- **`JsonStringEnumConverter`** global: `Modalidad`, `BasePrecio` y `RazonAusenciaPrecio` viajan por nombre (`"Lista"`, `"CostoArticulo"`, `"SinCosto"`), no como número.

### Frontend (`frontend/`, nuevo)
- Angular 20 mínimo, sin librería de estado ni framework de UI.
- Incluye:
  - login contra `POST /api/acceso/login`, con el token solo en memoria;
  - buscador sobre `GET /api/articulos`, con alternativas separadas por `*`, inactivos opcionales y paginación;
  - lista con código interno, nombre y códigos de proveedor;
  - ficha con las seis modalidades y su razón;
  - formulario de alta contra `POST /api/articulos`, que cubre también la pantalla que le faltaba a la Fase 3.
- `ng serve` usa un proxy a la API (`localhost:5181`), así que no hace falta CORS. Cómo levantarla: `frontend/README.md` (dos comandos, puerto 4200).

## El comodín "100%_original"
- `ArticuloRepositorioTests.Busqueda_PermiteInactivosYNoInterpretaComodinesSQL` ya usaba ese texto exacto, **pero no discriminaba**: su señuelo ("Filtro común") no contiene "100". Con la búsqueda cambiada a `LIKE '%100%_original%'`, ese test sigue en verde.
- Se agregó `Busqueda_100PorcientoGuionBajoOriginal_TraeSoloElLiteral` (PG). Sus señuelos, "Filtro 100 de original" y "Filtro 1005_original", solo coinciden si `%` y `_` se interpretan como comodines. Con la mutación a `EF.Functions.Like`, el test trae 3 resultados y da rojo.

## TDD
| Unidad | estructura → test (rojo) → producción |
|---|---|
| Estructura | `07c418a` |
| Validación en el caso de uso (Moq) | `cac377e` (9 rojos, L34/L45) → `9bdf1ea` |
| Repositorio (PG) | `a5dc91a` (2 rojos, L171/L123) → `beb618f` |
| Ficha y razones (Moq) | `0f542a9` (8 rojos) → `85894da` |
| HTTP (WebApplicationFactory + PG) | `0c095d9` (7 rojos) → `633713d` |
| Frontend | `e794370` (sin tests automáticos; ver "Cómo se verificó") |

**Excepciones declaradas** (pasaron a la primera). Cada una se verificó con una mutación no commiteada que la pone en rojo:
- `TextoDe500`, `Valida` y `SinResultados` pasaban contra el stub que delega. Mutaciones: pasarle otra consulta al repositorio; devolver NoEncontrado si no hay resultados; `Length >= 500`.
- El comodín: mutación `EF.Functions.Like`.
- En la ficha, cada razón se verificó con su propia mutación, porque los 8 rojos caían en `IsSuccess`. Fueron siete: base antes que manual; razón aunque haya precio; sin fila → SinCosto; resto → TasaSinDefinir; devolver solo las filas que hay; inexistente → Invalido; precio null → 0.
- HTTP: sin el conversor de enums, los valores salen como números; con `[AllowAnonymous]` en la ficha, el 403 se vuelve 404; con `tamanoPagina` por defecto 10, la respuesta dice 10.

**Test movido, no borrado:** la Theory `Consulta_RechazaEntradaOPaginaInvalida` pasó de `ArticuloRepositorioTests` a `ConsultarArticulosUseCaseTests`. Conserva sus 6 casos y suma texto vacío y en blanco. Antes esperaba `ArgumentException` del repositorio; ahora espera `Invalido` del caso de uso, sin que se consulte el repositorio.

## Cómo se verificó
2026-10-15, HEAD de código `d44bab7`.
- `dotnet build --no-incremental`: **0 advertencias, 0 errores**.
- `dotnet test`: **258 total · 258 passed · 0 failed · 0 omitidos**. La Fase 3 tenía 234.
- **Contra PostgreSQL real: 86 tests `[PostgreSqlFact]`** (Fase 3: 77). Corrieron todos, porque no hubo omitidos. Los 7 HTTP nuevos levantan la API con `WebApplicationFactory` y el `DbContext` en el esquema aislado, sin mocks.
- `LaCentral.UseCases.csproj` sigue sin referencias. Ningún controlador importa `LaCentral.Data`.
- Prueba de humo del frontend por el proxy contra `lacentral_test`, **solo lecturas**:
  - login malo → 401;
  - sin token → 401;
  - texto vacío → 400;
  - búsqueda → 200 con Id y códigos;
  - ficha → seis modalidades por nombre.

  El alta por la pantalla no se probó contra la base, para no escribir en `public`. La cubren los tests HTTP.
- Subagentes, de a uno y cada uno en su worktree (detalle en `ESTADO.md`):
  - **verificador-tdd sobre `ca84eae..e794370`: APTO.**
    - Orden test → producción respetado, rojos reproducidos por aserción.
    - 16 de 17 mutaciones dan rojo. Sobrevivía la que quita el límite de 10 alternativas: se arregló en `1d6afc1` y ahora da rojo.
  - **auditor-persistencia sobre `ca84eae..1d6afc1`: NO APTO** por H1: la ficha del repositorio se releía con el contexto de la siembra, y una mutación que quitaba `AsNoTracking` y los `Include` sobrevivía.
    - Arreglo en `d44bab7`, con relectura en un contexto nuevo.
    - **Ronda 1 sobre `d44bab7`: APTO.** La misma mutación ahora da rojo.
    - Suite: 258/258, 86 PG.

Commits de arreglo después de los subagentes (solo tests):
| Commit | Qué |
|---|---|
| `1d6afc1` | 11 alternativas → Invalido, 10 consultan el repositorio (sin el límite → rojo L52; `> 9` → rojo) |
| `d44bab7` | la ficha del repositorio se relee con un contexto nuevo y afirma razón social y sucursal |

## Qué NO hace
- No fija precios de CasoEspecial ni tasas de Tarjeta3/6: no hay HU para eso.
- **`SinCosto` cubre "con base y sin precio".** Hoy eso solo pasa cuando falta el costo, porque el costo lo escribe la compra (Fases 5 y 6) y el recálculo va en la misma operación. Si alguna vez aparece un artículo con costo y con una modalidad con base pero sin precio, la ficha lo informaría como `SinCosto`. En `lacentral_test` hay 0 casos (consulta de solo lectura: 2644 artículos, ninguno con costo ni con filas de precio, así que todos muestran `SinConfiguracion`). La Fase 6 tiene que mantener ese invariante.
- El frontend no tiene tests automáticos ni build de producción servido por la API.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
