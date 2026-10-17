# PR · Fase 6 Sprint 3 — HU-ART-05 · Precios por modalidad + HU-FCO-01 CA5

**Rama:** `feature/HU-ART-05-precios-modalidad` → **`develop`** (sale de `a47a866`)
**Título sugerido:** `feat: recalculo de precios por modalidad al confirmar una compra, en la misma operacion (HU-ART-05, FCO-01 CA5)`
**Issues:** Refs #22 · Closes #32 (CA5 de HU-FCO-01 y CA-001 de HU-ART-05 se verifican en el mismo recorrido HTTP). #22 queda abierto: las tasas de Tarjeta3/6 y el precio de CasoEspecial siguen sin definir.

## Qué hace

### Unidad 1 · NUL en un texto → 400 en todo el proyecto
- `ManejadorExcepcionesGlobal` traduce la `PostgresException` con SqlState **22021** (`CharacterNotInRepertoire`), directa o dentro de `DbUpdateException`, a un `ProblemDetails` **400** con un detalle fijo ("…caracteres no válidos…"). No expone el mensaje de la excepción. El 409 de la lista blanca y el 500 genérico no cambian.
- El desenvolvimiento de la excepción se extrae a `ExcepcionDePostgres`, compartido con `MensajeDeConflicto`.
- Cada rama del desenvolvimiento la cubre un test HTTP distinto: artículos pasa por `DbUpdateException` (SaveChanges) y facturas de compra por la `PostgresException` directa (la consulta previa del caso de uso).

### Unidad 2 · `CalcularPreciosArticuloUseCase`, única regla de cálculo
- `Calcular` se mueve sin cambios desde `ReglasPrecioModalidad`, que queda solo con la configuración inicial del alta. Ningún código de producción llamaba a `ReglasPrecioModalidad.Calcular`: solo tests, que se reapuntan (12 métodos, 14 casos; ninguno se borra).
- **`Recalcular(configuracion, costo, ahora)`**: Lista primero (es la base de Efectivo); las filas manuales salen intactas; las automáticas, como filas nuevas, con `FechaCalculo = ahora` solo si hay precio. **Interpretación pendiente · Lista manual → Efectivo** (pendiente de confirmación del equipo, Alfredo): si Lista es manual, el código calcula Efectivo sobre ese precio de lista (× 0,90). Pregunta abierta: si alguien fija a mano el precio de lista, ¿el de efectivo lo sigue solo al −10 % o son dos decisiones separadas? Hoy no se alcanza (ninguna operación vuelve manual a Lista).
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
| O1 · fila manual no se pisa (PG, pedido de revisión del PR) | `e37c4ab` (1 rojo por aserción: Expected 95 / 2026-10-02, Actual 80,00 / 2026-09-01) → `e92707c` → `c575e4e` (ronda 1 del verificador, test declarado) |

**Excepciones declaradas** (pasaron a la primera, cada una verificada con una mutación sin commitear que la pone en rojo):
- `Calcular_MitadDeCentavoConDigitoPar_RedondeaLejosDelCero` (`698cec7`): `AwayFromZero` → `ToEven` da Expected 11,17 / Actual 11,16. Se agregó porque `MilesimaCinco` (11,275) da 11,28 con los dos criterios y esa mutación sobrevivía.
- `RecalcularPreciosTests.FilaManual_SaleIntacta` y `NoModificaLasFilasDeEntrada` (`71d6cf1`, el stub devuelve la entrada): no saltear manuales → `FilaManual_SaleIntacta` y `ListaManual_…` en rojo; mutar la fila de entrada → `NoModificaLasFilasDeEntrada` en rojo.
- `CalcularPreciosArticuloUseCaseTests.ArticuloSinFilasDePrecio_NoSeEscribe` y `RegistrarFacturaCompraUseCaseTests.Valida_ElRecalculoOcurreDentroDeLaUnidadDeTrabajo` (`a5102ba`): escribir artículos sin filas → rojo; recalcular también fuera de la unidad → rojo.
- `RegistrarFacturaCompraPostgreSqlTests.Registrar_FilaQuePasaAManualMientrasSeRecalcula_SobreviveALaCompra` (`c575e4e`): filtrar las manuales sobre lo leído en vez de en el UPDATE (M3) da Expected 170 / 2026-10-02, Actual 180,00 / fecha del registro.

**Otras mutaciones verificadas** (todas en rojo): base intercambiada en `Calcular`; Lista manual recalculada (Interpretación pendiente · Lista manual → Efectivo, Expected 180 / Actual null); fecha siempre; `LimitePrecio` una centésima menos; escribir antes de validar todos; la compra ignora el rechazo del recálculo; solo el primer comprado; otra fecha; el repositorio sin filtrar por modalidad o por artículo, o sin escribir la fecha (el motor lo rechaza con 23514); la base leída como null.

## Compuerta (subagentes de a uno, cada uno en su worktree; detalle en `ESTADO.md`)
| Ronda | Hallazgo | Arreglo |
|---|---|---|
| verificador sobre `a47a866..264879f` · **NO APTO** | M6: `p.Precio >= LimitePrecio` → `>` sobrevivía (ningún test usaba exactamente 10.000.000.000,00). 4 rojos reproducidos por aserción, 15 mutaciones más murieron, 350/350 | `84967d8` (test): `PrecioJustoEnElLimiteDeSuColumna_InvalidoYNoEscribe`, Tarjeta3 con tasa SOLO DE PRUEBA 25 % sobre una Lista de 8.000.000.000,00. Pasa contra producción (declarado); con M6, Expected False / Actual True |

| verificador sobre `264879f..638afe4` | M6 muere por aserción (`:124`); límite una centésima menos muere (`:137`). 351/351, 117 PG | **APTO** |
| auditor sobre `a47a866..638afe4` | Sin bloqueantes. Atomicidad verificada con mutación (recálculo fuera de la unidad de trabajo → 2 tests PG en rojo); CHECK de `articulo_precio_modalidad` cumplidos; relectura con contexto nuevo; sin Ids sembrados ni InMemory nuevos | **APTO** |
| verificador sobre `f7f1437..e92707c` (O1, pedido de cambios del PR) · **NO APTO** | Rojo de `e37c4ab` reproducido por aserción; quitar o invertir el filtro → rojo. M3, filtrar las manuales sobre lo leído en vez de en el UPDATE, sobrevivía (19/19): la fila del test ya era manual al leer. 352/352, 118 PG | `c575e4e` (test): otra sesión pasa a manual la fila Costo, automática al leer. Pasa contra producción (declarado); con M3, Expected 170 / Actual 180,00 |
| verificador sobre `e92707c..c575e4e` | M3 muere por aserción (`:271`); M1 y M2 rojos en los dos tests de O1 (`:236`, `:271`); M2 también rojo en el recálculo automático (`:189`). CHECK cumplidos por la escritura de la otra sesión; el doble envuelve el repositorio real. 353/353, 119 PG | **APTO** |
| auditor sobre `f7f1437..c575e4e` | Sin bloqueantes. Motor real y relectura con contexto nuevo; atomicidad intacta (20/20); CHECK verificados en el catálogo; con el filtro en el UPDATE, PostgreSQL reevalúa el WHERE sobre la versión confirmada. Latentes, a la deuda: OB1 lectura desfasada (Efectivo calculado sobre una Lista que pasó a manual, o una fila que vuelve a automática con el precio manual viejo), OB2 orden de bloqueos para el futuro endpoint, OB3 UPDATE sin efecto por fila manual | **APTO** |

El borde de `LimitePrecio` queda fijado por los dos lados: 9.999.999.999,99 se escribe y 10.000.000.000,00 es Invalido.

Observaciones no bloqueantes del auditor: O1, `ActualizarPreciosAsync` también reescribía las filas manuales con lo que leyó — **arreglada en este PR** (ver "CasoEspecial"); quedan como deuda en `ESTADO.md` O2, el orden "el primero ya escribió" del test de atomicidad del recálculo no se afirma; O3, ningún test fuerza una falla del recálculo por HTTP.

## Tasas de Tarjeta3/6
Siguen **sin definir** (D1). En producción esas filas no tienen base ni porcentaje y la ficha informa `TasaSinDefinir`. `RecalcularPreciosTests` usa `TasaTarjeta3DePrueba = 7` y `TasaTarjeta6DePrueba = 13`, nombradas y comentadas como **solo de prueba**: no son tasas comerciales. Cuando el equipo las defina, entran como configuración (base y porcentaje de la fila), no en el código.

## CasoEspecial
El recálculo **no escribe** una fila manual (política vigente), y lo decide la base al escribir: `ActualizarPreciosAsync` filtra `NOT calculo_manual` en el mismo UPDATE (`e92707c`). Así, un precio que otra sesión fija a mano entre la lectura y la escritura del recálculo sobrevive a la compra, tanto si la fila ya era manual (`Registrar_PrecioManualFijadoMientrasSeRecalcula_SobreviveALaCompra`) como si era automática al leer y pasó a manual (`Registrar_FilaQuePasaAManualMientrasSeRecalcula_SobreviveALaCompra`). Los dos usan el repositorio de producción con una escritura confirmada por otra sesión entre `ObtenerAsync` y `ActualizarPreciosAsync` (READ COMMITTED: la lectura no bloquea), y releen con contexto nuevo.

**Hoy ese intercalado no ocurre en producción:** fuera del recálculo, ningún código actualiza `articulo_precio_modalidad` (el alta solo inserta). Los tests se adelantan al endpoint de precio manual, que todavía no existe. Fijar el precio de CasoEspecial no tiene endpoint ni HU: esta fase no lo agrega.

## Cómo se verificó
2026-10-16, HEAD `c575e4e` (corrida del verificador en su worktree, después de los cambios pedidos en la revisión del PR #121), rol de test `lacentral_javi` (mismos permisos que `lacentral_tests`, aceptado por el equipo).
- `dotnet build --no-incremental`: 0 advertencias, 0 errores.
- `dotnet test`: **353 total · 353 passed · 0 failed · 0 omitidos**, **119 `[PostgreSqlFact]`** contra PostgreSQL real. La Fase 5 cerró con 325 y 109.
- `LaCentral.UseCases.csproj` sigue sin referencias. Ningún controlador importa `LaCentral.Data`; el acoplamiento nuevo está solo en `Program.cs`.
- Recorrido HTTP (`FacturasCompraApiPostgreSqlTests.Post_Compra_LaFichaDelArticuloMuestraLosPreciosRecalculados`): alta por la API, compra a 100, la ficha da **Lista 150 · Costo 100 · EfectivoTransferencia 135**, Tarjeta3/6 `TasaSinDefinir`, CasoEspecial `CasoEspecialNoFijado`.

## Qué NO hace
- No define tasas de Tarjeta3/6 ni fija precios de CasoEspecial (decisiones del equipo).
- No toca el esquema.
- Lista manual sin precio fijado: Efectivo queda con base y sin precio, y la ficha lo mostraría como `SinCosto`. Hoy no se alcanza (ninguna operación vuelve manual a Lista); queda como deuda.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
