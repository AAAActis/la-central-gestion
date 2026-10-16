# PR · Fase 5 Sprint 3 — HU-FCO-01 · Factura de compra

**Rama:** `feature/HU-FCO-01-factura-compra` → **`develop`**
**Título sugerido:** `feat: registro manual de factura de compra con stock por linea, costo e historial, atomico (HU-FCO-01)`
**Issue:** Refs #32 (**no lo cierra**: CA5, el recálculo de precios, llega con la Fase 6)

> Las Fases 3 (#117) y 4 (#118) ya están en `develop`: el diff contra `develop` es solo la Fase 5.

## Qué hace

### Backend
- **Entidades** `FacturaCompra` y `FacturaCompraDetalle` en `UseCases/Entidades/`. El costo de la línea vive en un solo lugar: `FacturaCompraDetalle.CostoUnitario` = precio unitario **neto del descuento de la línea, sin IVA y sin prorratear percepciones**, redondeado a centavos con el medio centavo lejos del cero (decisión del equipo, 2026-10-15).
- **Puerto `IFacturaCompraRepositorio`**: `ObtenerNumeroActivoAsync` (el plan decía `ExisteNumeroActivoAsync`; CA2 pide informar CON QUÉ factura choca, así que devuelve `FacturaCompraExistente(Id, Numero, FechaEmision)` o null), `ObtenerEstadoArticulosAsync` y `RegistrarConEfectosAsync` → `Result<int>`.
- **`RegistrarFacturaCompraUseCase`**:
  - CA1: proveedor existente y activo; cada artículo existente y **activo** (uno dado de baja no admite compras, confirmado por el equipo); sucursal de destino real por línea (D9).
  - CA2: número activo repetido para ese proveedor → **409** informando la factura en conflicto. Ante una carrera decide `ux_factura_compra_numero_activa`, que el manejador global ya traducía a 409.
  - CA3: sin líneas → **400**.
  - CA4/D8: emisión, vencimiento y las cuatro percepciones se guardan como datos (≥ 0, sin cálculo).
  - El núcleo rechaza lo que las columnas NUMERIC no pueden guardar: (12,3) cantidad, (14,2) precio y percepciones, (6,2) descuento. Antes eso terminaba en 23514/22003 → 500.
  - Todo dentro de **una** `IUnidadDeTrabajo`. El punto de integración de CA5 quedó marcado ahí.
- **`FacturaCompraRepositorio.RegistrarConEfectosAsync`**: cabecera + detalle; por línea, stock del destino con `INSERT ... ON CONFLICT DO UPDATE SET cantidad = stock.cantidad + EXCLUDED.cantidad WHERE ... < 1e9` (el motor suma, dos compras simultáneas no se pisan; si la suma no entra en `stock.cantidad`, 0 filas → `Invalido`, no 22003); costo y último proveedor **por Id**; historial de compra. Solo los artículos comprados.
- **`POST /api/facturas-compra`** (ADMINISTRADOR y OPERADOR): 201 con `Location`, 400, 409. El usuario sale de la sesión.

### Infraestructura de tests
- `PostgreSqlPruebas` ya no deja esquemas huérfanos ni pools llenos (causa del 53300): creación en una transacción, `ClearPool` y liberación de fábricas.

### Frontend (`frontend/src/app/factura-compra.ts`)
- Pestaña nueva: proveedor, cabecera, percepciones y líneas con destino. No calcula costos: al registrar, relee la ficha de cada artículo comprado.

## Atomicidad (el test más importante del sprint)
`RegistrarFacturaCompraPostgreSqlTests`: un test PG por punto de falla. Cada uno hace fallar **al motor** con un trigger en el esquema aislado, en la segunda línea, cuando la primera ya escribió: índice de número activo (carrera real), cabecera, detalle, stock, artículo e historial. Todos releen con un contexto nuevo y afirman que no quedó cabecera, detalle, stock, costo, último proveedor ni historial.
Mutaciones sin commitear: `UnidadDeTrabajo` sin transacción → 3 de 6 en rojo (los otros 3 caen dentro del primer `SaveChanges`); además, stock antes que la cabecera → 6 de 6.

## TDD
| Unidad | estructura → test (rojo) → producción |
|---|---|
| Limpieza de `PostgreSqlPruebas` | `1473f77` → `3037344` → `70e0452` |
| Estructura HU-FCO-01 | `2dda597` |
| Caso de uso (Moq) | `05241ab` (31 rojos) → `42cad4a` |
| Repositorio y atomicidad (PG) | `ba4044c` (10 rojos) → `97cdbea` |
| HTTP (WebApplicationFactory + PG) | `f760405` (5 rojos) → `c69ca86` |
| Frontend | `cdee847` (sin tests automáticos; ver "Cómo se verificó") |

**Excepciones declaradas** (pasaron a la primera, cada una con su mutación): `CostoUnitario(100,100)`; `f7f8b27` (sin emisión → 400; sin `[Required]` da 500).

## Compuerta (subagentes de a uno, cada uno en su worktree; detalle en `ESTADO.md`)
| Ronda | Hallazgo | Arreglo |
|---|---|---|
| 1 · verificador NO APTO | B1 (inexistente vs. dado de baja), B2 (`<` → `<=` en el vencimiento) | `12480a2`, `10a0f0b` (tests) |
| 1 · auditor NO APTO | H1: valores que NUMERIC no guarda → 500 | `3e110cb` rojo → `f34bbc4` |
| 2 · auditor NO APTO | la suma al stock existente desborda `stock.cantidad` → 500 | `0b7da32` → `163b2cc` rojo → `b4fca2f` |
| 2 · auditor | — | **APTO** sobre `f34bbc4..b4fca2f` |
| 2 · verificador NO APTO | M11: `<` → `<=` en el WHERE del upsert sobrevivía | — |
| 3 (autorizada por el equipo) | test de borde: stock 0,001 + 999.999.999,999 → Invalido, sin escritura | `0190052` (con `<=` da 22003 → rojo) |
| 3 · verificador NO APTO | M11 cerrado; sobrevive M12 (`LimiteStock` una milésima menos): falta el lado aceptado del borde | — |
| 4 (autorizada por el equipo, solo M12) | test del lado aceptado: stock 0,001 + 999.999.999,998 → se registra, relectura = 999.999.999,999 | `f424771` (con M12 da Invalido → rojo) |
| 4 · verificador | M12 muere por aserción; M11 sigue muriendo | **APTO** sobre `26c02fc..f424771` |

El borde de `stock.cantidad` queda fijado por los dos lados: `0190052` (suma = 1e9 → Invalido) y `f424771` (suma = 999.999.999,999 → se guarda).

## Cómo se verificó
2026-10-15, HEAD de código `f424771` (corrida del verificador en su worktree).
- `dotnet build --no-incremental`: 0 advertencias, 0 errores.
- `dotnet test`: **325 total · 325 passed · 0 failed · 0 omitidos**, **109 `[PostgreSqlFact]`** contra PostgreSQL real (rol `lacentral_tests`). La Fase 4 tenía 258.
- `LaCentral.UseCases.csproj` sigue sin referencias. Ningún controlador importa `LaCentral.Data`.
- Prueba de humo del frontend por el proxy con el rol `lacentral_tests`, **solo lecturas** (proveedores, ficha, POST sin token → 401, POST sin líneas → 400). `public.factura_compra` sigue con 0 filas.

## Qué NO hace
- **CA5** (recalcular precios por modalidad de los comprados): Fase 6. Por eso **no cierra #32**.
- Sin GET de facturas (es otra HU), sin lectura de PDF ni cotejo (HU-FCO-05).
- Deuda conocida: NUL en un texto → 500 (22021) en todo el proyecto. Se resuelve en la Fase 6, en el manejador global.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
