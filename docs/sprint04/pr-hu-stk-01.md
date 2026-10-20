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
- **Suite completa (20/10/2026, en `cf416df`, en serie):** `dotnet build --no-incremental` con
  0 warnings. **435 total · 435 passed · 0 failed · 0 skipped**, 148 `[PostgreSqlFact]` contra
  PostgreSQL real. Antes eran 431 y 145. Tests nuevos: 4, de los cuales 3 corren contra PostgreSQL.
- **HTTP:** `ArticulosApiPostgreSqlTests.Get_Ficha_ExistenciasPorSucursalYTotal`, con
  `WebApplicationFactory` contra PostgreSQL. Da de alta un artículo y comprueba `[0, 0]`, total 0.
  Después pone 7 y −3 y comprueba `[7, −3]`, total 4. **No cubre el caso "sin fila en
  `stock`"**: el alta ya crea las dos filas en cero. Ese caso de CA-002 lo cubre
  `Detalle_SinFilasDeStock_CadaSucursalConCantidadCero`.

### Criterios de aceptación → tests
| CA | Estado | Tests |
|---|---|---|
| CA-001 | verificado | `ObtenerArticuloDetalleUseCaseTests.Ficha_TotalExistencias_SumaLasSucursalesConSuSigno`, `Get_Ficha_ExistenciasPorSucursalYTotal` |
| CA-002 | verificado | `Detalle_SinFilasDeStock_CadaSucursalConCantidadCero`, `Detalle_ConservaCostoNuloOriginalDelCodigoYStockDecimalPorSucursal`, `Get_Ficha_ExistenciasPorSucursalYTotal` |
| CA-003 | verificado | `Detalle_ExistenciaNegativa_SeDevuelveTalCualPorSucursal`, `Ficha_TotalExistencias_SumaLasSucursalesConSuSigno`, `Get_Ficha_ExistenciasPorSucursalYTotal` |

## TDD
| Unidad | estructura → test (rojo) → producción |
|---|---|
| Todas las sucursales, cero sin fila (PG) | `013b7f5`: `Detalle_SinFilasDeStock_...` en rojo por aserción (esperaba dos sucursales en cero, obtuvo `[]`) → `21d7450`. El test viejo pasa de `Assert.Single` a `[0]`, porque la ficha ahora trae dos sucursales |
| Total (Moq) | `79dc66e` (stub `=> 0m`) → `88c208a`: rojo por aserción (esperaba 3,5, obtuvo 0) → `ca30a16` |
| Ficha HTTP (PG) | `d32e218`: caracterización, sin producción después |
| Frontend | `15a5c1c` |
| Sucursal con fila junto a la sucursal sin fila (PG) | `afc4ee8`: caracterización, sin producción después |

**Excepciones declaradas.** Estos tests pasaron a la primera. El verificador comprobó que
discriminan con mutaciones sin commitear:
- `Detalle_ExistenciaNegativa_SeDevuelveTalCualPorSucursal`, que entró en `013b7f5`: el código
  anterior ya devolvía las filas tal cual cuando existían en las dos sucursales. Lo matan M3
  (truncar los negativos), M6 (orden invertido) y M7 (cruce por `!=`).
- `Get_Ficha_ExistenciasPorSucursalYTotal` (`d32e218`): lo matan M3, M4, M5, M6 y M7. M1 y M2 los
  sobrevive solo, porque nunca pasa por "sin fila", y los matan los tests de repositorio.
- `Detalle_ConservaCostoNulo...` ajustado en `afc4ee8`: lo matan M1, M2, M6 y M7.

## Compuerta (subagentes de a uno, cada uno en su worktree)
| Pasada | Alcance | Veredicto | Qué |
|---|---|---|---|
| verificador | `c0c6b86..cf416df` | **APTO** | Orden test → producción limpio en las dos unidades con producción. Los dos rojos se reproducen por aserción. 7 mutaciones, ninguna sobrevive: M1 solo sucursales con fila, M2 `?? 1m`, M3 `Math.Max(0, …)`, M4 total con `Math.Abs`, M5 total solo con positivos, M6 `OrderByDescending`, M7 `!=` en el cruce. Suite 435/435. No bloqueantes: las caracterizaciones sin declarar y el HTTP sin el caso "sin fila" (→ este cuerpo) |
| auditor | `c0c6b86..cf416df` | **APTO** | `stock_pkey (articulo_id, sucursal_id)` en el script, en el modelo y en el motor (un duplicado da 23505): `SingleOrDefault` no puede lanzar. `cantidad NUMERIC(12,3)`, 1,2345 se relee como 1,235 con un contexto nuevo. Sin Ids sembrados, relectura con contexto nuevo, sin InMemory, `PostgreSqlSeguridadTests` activo. Dos viajes por ficha, sin N+1 |

## Deuda nueva
- **De entorno:** con el rol personal `lacentral_lauti`, que tiene tope de conexiones, la suite
  en paralelo da unos 140 fallos `53300` que no son reales. En esa máquina se corre con
  `dotnet test -- xUnit.ParallelizeTestCollections=false` (≈36 min). Ver ESTADO.md.
- **Auditor:** la ficha hace un JOIN sin dividir de códigos, stock y precios. Ya era así antes, y
  hoy está acotado: 2 sucursales y 6 modalidades. Si crecen las sucursales o los códigos,
  conviene `AsSplitQuery()`.
- **Auditor, sin verificar:** que no hayan quedado esquemas `pr110_test_*` huérfanos en
  `lacentral_test`.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
