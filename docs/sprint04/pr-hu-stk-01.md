# PR · Sprint 4, Pista C · C1 — HU-STK-01 · Consulta de existencias por ubicación

**Rama:** `feature/HU-STK-01-existencias-ubicacion` → **`develop`**
**Título sugerido:** `feat: existencias por sucursal y total en la ficha de articulo (HU-STK-01)`
**Issues:** Refs #24 (HU 017 · HU-STK-01).

## Qué hace
- **Sin endpoint nuevo (D19):** la existencia va en la ficha, `GET /api/articulos/{id}`, porque
  los CA dicen "cuando el vendedor abre su detalle".
- **CA-002 · sin movimientos es cero, no vacío:** `ArticuloRepositorio.ObtenerDetallePorIdAsync`
  arma `existencias` desde **todas** las sucursales, ordenadas por Id. Una sucursal sin fila en
  `stock` sale con `cantidad: 0`.
- **CA-001 · total:** `FichaArticuloDto.TotalExistencias` suma todas las sucursales. Se serializa
  como `totalExistencias`. El cambio es aditivo.
- **CA-003 · negativo:** una cantidad negativa se devuelve tal cual y resta en el total. No se
  ignora ni se trunca a cero (es la venta sin stock de HU-STK-03).
- **Frontend:** en `frontend/src/app/articulos.ts` el bloque Stock es una tabla con una fila por
  sucursal y el Total. Los negativos se ven en rojo. Se fue el "Sin stock registrado.".
- **Manual:** `docs/manual/ficha-articulo-stock.md`.

## Qué NO hace
- No cambia el esquema ni `Data/Models/`. Lee `stock`, que existe desde el Sprint 3, y no toca
  `articulo_precio_modalidad` ni `proveedor_articulo` (D18).
- No escribe stock: ni movimientos ni regularización de negativos.

## Decisiones
- **D18 (equipo, 19/10/2026):** arrancar HU-STK-01 antes del re-scaffold global. Riesgo
  aceptado: cuando entren los scripts de C1 y C2, el re-scaffold puede conflictuar con esta rama
  en el contexto, y se rebasea.
- **D19 (agente, 19/10/2026, dentro del alcance de los CA):** la ficha, sin endpoint nuevo, con
  todas las sucursales y `totalExistencias`.

## Cómo se verificó
- **`ArticuloRepositorioTests` contra PostgreSQL real (20/10/2026): 10 total · 10 passed ·
  0 failed · 0 skipped**, en serie. Incluye los dos tests nuevos y el ajustado:
  - `Detalle_SinFilasDeStock_CadaSucursalConCantidadCero`;
  - `Detalle_ExistenciaNegativa_SeDevuelveTalCualPorSucursal`;
  - `Detalle_ConservaCostoNuloOriginalDelCodigoYStockDecimalPorSucursal`: con fila en una
    sucursal y sin fila en la otra, aparecen las dos.
  Todos releen con un contexto nuevo. No siembran Ids.
- **Suite completa (19/10/2026, en serie, según ESTADO.md):** 435 tests, 148 contra PostgreSQL.
  Antes eran 431 y 145. Tests nuevos: 4, de los cuales 3 corren contra PostgreSQL.
- **HTTP:** `ArticulosApiPostgreSqlTests.Get_Ficha_ExistenciasPorSucursalYTotal`, con
  `WebApplicationFactory` contra PostgreSQL. Da de alta un artículo y comprueba `[0, 0]`, total 0.
  Después pone 7 y −3 y comprueba `[7, −3]`, total 4.

### Criterios de aceptación → tests
| CA | Estado | Tests |
|---|---|---|
| CA-001 | verificado | `ObtenerArticuloDetalleUseCaseTests.Ficha_TotalExistencias_SumaLasSucursalesConSuSigno`, `Get_Ficha_ExistenciasPorSucursalYTotal` |
| CA-002 | verificado | `Detalle_SinFilasDeStock_CadaSucursalConCantidadCero`, `Detalle_ConservaCostoNuloOriginalDelCodigoYStockDecimalPorSucursal`, `Get_Ficha_ExistenciasPorSucursalYTotal` |
| CA-003 | verificado | `Detalle_ExistenciaNegativa_SeDevuelveTalCualPorSucursal`, `Ficha_TotalExistencias_SumaLasSucursalesConSuSigno`, `Get_Ficha_ExistenciasPorSucursalYTotal` |

## TDD
| Unidad | test → producción (el rojo de cada test lo confirma el verificador) |
|---|---|
| Todas las sucursales, cero sin fila (PG) | `013b7f5` → `21d7450` |
| Total (Moq) | `79dc66e` (estructura, sin comportamiento) → `88c208a` → `ca30a16` |
| Ficha HTTP (PG) | `d32e218` |
| Frontend | `15a5c1c` |
| Sucursal con fila junto a la sucursal sin fila (PG) | `afc4ee8` |

## Compuerta
**Pendiente:** el verificador-tdd y el auditor-persistencia todavía no corrieron sobre esta rama.

## Deuda nueva
- **De entorno:** con el rol personal `lacentral_lauti`, que tiene tope de conexiones, la suite
  en paralelo da unos 140 fallos `53300` que no son reales. En esa máquina se corre con
  `dotnet test -- xUnit.ParallelizeTestCollections=false` (≈36 min). Ver ESTADO.md.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
