# PR · Fase 6 Sprint 3 — HU-ART-05 · Precios por modalidad + HU-FCO-01 CA5

**Rama:** `feature/HU-ART-05-precios-modalidad` → **`develop`** (sale de `a47a866`)
**Título sugerido:** `feat: recalculo de precios por modalidad al confirmar una compra, en la misma operacion (HU-ART-05, FCO-01 CA5)`
**Issues:** Closes #22 · Closes #32 (CA5 de HU-FCO-01 y CA-001 de HU-ART-05 se verifican en el mismo recorrido HTTP)

## Qué hace

### Unidad 1 · NUL en un texto → 400 en todo el proyecto
- `ManejadorExcepcionesGlobal` traduce la `PostgresException` con SqlState **22021** (`CharacterNotInRepertoire`), directa o dentro de `DbUpdateException`, a un `ProblemDetails` **400** con un detalle fijo ("…caracteres no válidos…"). No expone el mensaje de la excepción. El 409 de la lista blanca y el 500 genérico no cambian.
- El desenvolvimiento de la excepción se extrae a `ExcepcionDePostgres`, compartido con `MensajeDeConflicto`.
- Cada rama del desenvolvimiento la cubre un test HTTP distinto: artículos pasa por `DbUpdateException` (SaveChanges) y facturas de compra por la `PostgresException` directa (la consulta previa del caso de uso).

### Unidad 2 · `CalcularPreciosArticuloUseCase`, única regla de cálculo
- `Calcular` se mueve sin cambios desde `ReglasPrecioModalidad`, que queda solo con la configuración inicial del alta. Ningún código de producción llamaba a `ReglasPrecioModalidad.Calcular`: solo tests, que se reapuntan (12 métodos, 14 casos; ninguno se borra).
- **`Recalcular(configuracion, costo, ahora)`**: Lista primero (es la base de Efectivo); las filas manuales salen intactas; las automáticas, como filas nuevas, con `FechaCalculo = ahora` solo si hay precio. **D11:** si Lista es manual, Efectivo se calcula sobre ese precio de lista (× 0,90).
- `Calcular` pierde la guarda de `CalculoManual`: no se alcanza (una fila manual no tiene porcentaje por el CHECK `manual_sin_porcentaje`, probado contra el motor, y `Recalcular` no le pide precio a las manuales). Regla de CLAUDE.md para guardas no alcanzables.
- API y frontend no calculan precios (grep sin fórmulas en Api, Data y `frontend/src`; `FacturaCompra.CostoUnitario` es el costo de la línea, otra regla).

### Unidad 3 · Integración (CA5 de FCO-01 · CA-001 y CA-002 de ART-05)
- **Puerto `IPreciosArticuloRepositorio`** (`ObtenerAsync`, `ActualizarPreciosAsync`), implementado por `ArticuloRepositorio` sobre el **DbContext scoped** de la petición: el mismo que usan `FacturaCompraRepositorio` y `UnidadDeTrabajo`. Es un puerto aparte para no agrandar `IArticuloRepositorio` ni su doble de `CrearArticuloPostgreSqlTests`.
- **`CalcularPreciosArticuloUseCase.EjecutarAsync(ids, ahora)`**: lee costo y filas, recalcula todo y recién después escribe (por artículo y modalidad, solo precio y fecha). No abre una unidad de trabajo propia. Un precio que no entra en `articulo_precio_modalidad.precio` NUMERIC(12,2) es **Invalido** y no se escribe ninguno (antes de esto, una compra a 6.666.666.666,67 daba 22003 → 500).
- **`RegistrarFacturaCompraUseCase`** lo llama en el punto marcado de CA5, dentro de **la misma** `IUnidadDeTrabajo`, con los ids de las líneas y la fecha del registro, y devuelve su resultado a la unidad de trabajo: un rechazo descarta también la compra.
- Un artículo sin filas de precio (anterior a D1) no se recalcula: la ficha sigue informando `SinConfiguracion`.

## Atomicidad
`RegistrarFacturaCompraPostgreSqlTests` suma un punto de falla: `Atomicidad_FallaAlRecalcularLosPreciosDelSegundoComprado_NoQuedaNada` (trigger en `articulo_precio_modalidad`, UPDATE del segundo comprado, cuando los precios del primero ya se escribieron). La foto de atomicidad ahora incluye las filas de precio, así que los 6 puntos anteriores también afirman que el recálculo no dejó rastro. Mutación sin commitear: `UnidadDeTrabajo` sin transacción → el punto nuevo y el de precio que no entra quedan en rojo por aserción (factura, stock, costo y precios del primero quedaban escritos).

## TDD
| Unidad | estructura → test (rojo) → producción |
|---|---|
| NUL → 400 | `95571c7` (2 rojos HTTP: Expected `BadRequest` / Actual `InternalServerError`) → `e2118f6` |
| Mover `Calcular` | `6f2e337` (refactor; 327 = 327, 12 métodos reapuntados) |
| Redondeo | `698cec7` (test, pasa a la primera: **declarado**) |
| `Recalcular` | `6f2e337` (stub que devuelve la entrada) → `71d6cf1` (8 tests, 6 rojos por aserción) → `80d62fd` |
| Guarda inalcanzable | `bc5e98f` (refactor de producción) |
| Recálculo al comprar (Moq) | `bf568db` (estructura) → `a5102ba` (8 tests, 6 rojos por aserción o `Verify`) → `d1b21b7` (producción; además, en `RegistrarFacturaCompraUseCaseTests`, el doble de `IPreciosArticuloRepositorio` devuelve por defecto un diccionario vacío: sin ese setup Moq devolvía null y 3 tests existentes daban `ArgumentNullException`) |
| Repositorio, atomicidad y HTTP (PG) | `fe029b2` (6 rojos por aserción: 5 PG + 1 HTTP) → `fbd4358` |

**Excepciones declaradas** (pasaron a la primera, cada una verificada con una mutación sin commitear que la pone en rojo):
- `Calcular_MitadDeCentavoConDigitoPar_RedondeaLejosDelCero` (`698cec7`): `AwayFromZero` → `ToEven` da Expected 11,17 / Actual 11,16. Se agregó porque `MilesimaCinco` (11,275) da 11,28 con los dos criterios y esa mutación sobrevivía.
- `RecalcularPreciosTests.FilaManual_SaleIntacta` y `NoModificaLasFilasDeEntrada` (`71d6cf1`, el stub devuelve la entrada): no saltear manuales → `FilaManual_SaleIntacta` y `ListaManual_…` en rojo; mutar la fila de entrada → `NoModificaLasFilasDeEntrada` en rojo.
- `CalcularPreciosArticuloUseCaseTests.ArticuloSinFilasDePrecio_NoSeEscribe` y `RegistrarFacturaCompraUseCaseTests.Valida_ElRecalculoOcurreDentroDeLaUnidadDeTrabajo` (`a5102ba`): escribir artículos sin filas → rojo; recalcular también fuera de la unidad → rojo.

**Otras mutaciones verificadas** (todas en rojo): base intercambiada en `Calcular`; Lista manual recalculada (D11, Expected 180 / Actual null); fecha siempre; `LimitePrecio` una centésima menos; escribir antes de validar todos; la compra ignora el rechazo del recálculo; solo el primer comprado; otra fecha; el repositorio sin filtrar por modalidad o por artículo, o sin escribir la fecha (el motor lo rechaza con 23514); la base leída como null.

## Compuerta (subagentes de a uno, cada uno en su worktree; detalle en `ESTADO.md`)
| Ronda | Hallazgo | Arreglo |
|---|---|---|
| verificador sobre `a47a866..264879f` · **NO APTO** | M6: `p.Precio >= LimitePrecio` → `>` sobrevivía (ningún test usaba exactamente 10.000.000.000,00). 4 rojos reproducidos por aserción, 15 mutaciones más murieron, 350/350 | `84967d8` (test): `PrecioJustoEnElLimiteDeSuColumna_InvalidoYNoEscribe`, Tarjeta3 con tasa SOLO DE PRUEBA 25 % sobre una Lista de 8.000.000.000,00. Pasa contra producción (declarado); con M6, Expected False / Actual True |

El borde de `LimitePrecio` queda fijado por los dos lados: 9.999.999.999,99 se escribe y 10.000.000.000,00 es Invalido.

## Tasas de Tarjeta3/6
Siguen **sin definir** (D1). En producción esas filas no tienen base ni porcentaje y la ficha informa `TasaSinDefinir`. `RecalcularPreciosTests` usa `TasaTarjeta3DePrueba = 7` y `TasaTarjeta6DePrueba = 13`, nombradas y comentadas como **solo de prueba**: no son tasas comerciales. Cuando el equipo las defina, entran como configuración (base y porcentaje de la fila), no en el código.

## CasoEspecial
El recálculo **no pisa** una fila manual (política vigente). Fijar el precio de CasoEspecial no tiene endpoint ni HU: esta fase no lo toca.

## Cómo se verificó
2026-10-16, HEAD de código `fbd4358`, rol de test `lacentral_javi` (mismos permisos que `lacentral_tests`, aceptado por el equipo).
- `dotnet build --no-incremental`: 0 advertencias, 0 errores.
- `dotnet test`: **350 total · 350 passed · 0 failed · 0 omitidos**, **117 `[PostgreSqlFact]`** contra PostgreSQL real. La Fase 5 cerró con 325 y 109.
- `LaCentral.UseCases.csproj` sigue sin referencias. Ningún controlador importa `LaCentral.Data`; el acoplamiento nuevo está solo en `Program.cs`.
- Recorrido HTTP (`FacturasCompraApiPostgreSqlTests.Post_Compra_LaFichaDelArticuloMuestraLosPreciosRecalculados`): alta por la API, compra a 100, la ficha da **Lista 150 · Costo 100 · EfectivoTransferencia 135**, Tarjeta3/6 `TasaSinDefinir`, CasoEspecial `CasoEspecialNoFijado`.

## Qué NO hace
- No define tasas de Tarjeta3/6 ni fija precios de CasoEspecial (decisiones del equipo).
- No toca el esquema.
- Lista manual sin precio fijado: Efectivo queda con base y sin precio, y la ficha lo mostraría como `SinCosto`. Hoy no se alcanza (ninguna operación vuelve manual a Lista); queda como deuda.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
