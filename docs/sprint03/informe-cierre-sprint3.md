# Informe de cierre · Sprint 3

Fase 8 del plan (`docs/sprint03/PLAN_SPRINT3.md`, "FASE 8 · Cierre"). Fecha: 2026-10-17.
Base: `develop` = `153aed3` (Merge #123). Código sin cambios desde `aa2e795`: entre `aa2e795` y
`153aed3` solo se tocaron `CLAUDE.md`, `docs/ONBOARDING-AGENTE.md`, `ESTADO.md`, el plan y el cuerpo
del PR de la Fase 7. Por eso la suite de cierre es la del auditor de la Fase 7 en `aa2e795`; en esta
fase no se volvió a correr.

Criterio de este informe: un CA está **verificado** solo si hay un test que lo ejercita y lo afirma.
Si ningún test lo nombra, figura como **NO verificado**, aunque el código parezca hacerlo.

Abreviaturas de la columna "Tipo": **PG** = `[PostgreSqlFact]` contra PostgreSQL real, con relectura
en un contexto nuevo; **HTTP** = `WebApplicationFactory` contra PostgreSQL real; **Moq** = caso de uso
con dobles; **Unit** = función pura, sin base ni dobles.

Rutas de los tests, relativas a `backend/LaCentral.Tests/`.

---

## 1 · Criterios de aceptación → tests

### HU-ART-01 · Alta de artículo con códigos por proveedor (#14)

| CA | Texto (resumido) | Estado | Tests (archivo · método · tipo) |
|---|---|---|---|
| CA1 | Sin artículo con ese código interno → alta activa con existencia cero en ambas ubicaciones | **Verificado** | `Articulos/CrearArticuloPostgreSqlTests.cs` · `Alta_PersisteArticuloCodigosStockYSeisPrecios` (PG: `Activo`, stock 0 en TODAS las sucursales releído) · `Integracion/ArticulosApiPostgreSqlTests.cs` · `Post_Alta_201ConIdYLocation` (HTTP) · `Articulos/CrearArticuloUseCaseTests.cs` · `Alta_ArmaElArticuloCompleto` (Moq). Atomicidad: `Alta_ExcepcionAMitadDeCamino_NoQuedaArticuloNiCodigosNiStockNiPrecios`, `Alta_FalloDevueltoAMitadDeCamino_NoQuedaNada` (PG) |
| CA2 | Ya existe un artículo con ese código interno → rechazo informando a qué artículo pertenece | **NO verificado tal como está escrito** | Motivo: el alta **no recibe** código interno. Lo genera el sistema (`LC-{id:D6}`, decisión de la Fase 3), así que el usuario no puede ingresar uno repetido. Lo que sí está verificado: dos altas simultáneas no chocan (`CrearArticuloPostgreSqlTests.cs` · `DosAltasSimultaneas_CodigosInternosDistintos`, PG), y un código de proveedor igual al código interno de otro artículo se rechaza informando cuál (`CrearArticuloPostgreSqlTests.cs` · `CodigoDeOtroArticulo_ConflictoInformandoCual`, rama "porInterno", PG; `ArticulosApiPostgreSqlTests.cs` · `Post_CodigoDeOtroArticulo_409InformandoCual`, HTTP). El código interno ingresado a mano existe recién en la modificación (HU-ART-03, ver abajo). **Hay que reescribir el CA.** |
| CA3 | Un solo código alternativo → se registra; no hay mínimo | **Verificado** | `ArticulosApiPostgreSqlTests.cs` · `Post_Alta_201ConIdYLocation` (HTTP, un código, `Assert.Single` en la relectura) · `Post_SinCodigosProveedor_201YSeGuardaSinCodigos` (HTTP, cero códigos) |
| CA4 | Código alternativo ya asignado a otro artículo → rechazo informando el artículo | **Verificado** | `CrearArticuloUseCaseTests.cs` · `CodigoDeOtroArticulo_ConflictoInformandoCual` (Moq) · `CrearArticuloPostgreSqlTests.cs` · `CodigoDeOtroArticulo_ConflictoInformandoCual` (PG, otra grafía y otro proveedor) · `ArticulosApiPostgreSqlTests.cs` · `Post_CodigoDeOtroArticulo_409InformandoCual` (HTTP) |
| CA5 | Código alternativo sin proveedor → se bloquea | **Verificado (solo Moq)** | `CrearArticuloUseCaseTests.cs` · `ProveedorNoIndicado_InvalidoSinConsultarProveedores` (Theory 0 y −5). Sin test HTTP ni PG de este caso: un `proveedorId` omitido llega como 0 y cae en la misma guarda |
| CA6 | El usuario cargó márgenes → se registran y quedan disponibles para el cálculo | **NO verificado tal como está escrito** | Motivo: el alta **no recibe** márgenes. Las seis filas nacen con la configuración global de D1 (respuesta c de Alfredo: los márgenes son globales). Lo que sí está verificado: `Alta_PersisteArticuloCodigosStockYSeisPrecios` (PG, las seis filas con base y porcentaje) · `Articulos/ConfiguracionInicialPreciosTests.cs` · `ConfiguracionInicial_SeisFilasConBaseYPorcentajeDeD1YPrecioNulo`, `ConfiguracionInicial_ConCosto_DaLosPreciosDeD1` (Unit). **Hay que reescribir el CA** (mismo motivo que CA4 de #17) |

Extras verificados (no son CA): D2 (`D2_DosCodigosDelMismoProveedor_ConflictoYNoSeEscribe`, PG), proveedor
inactivo (`ProveedorInactivo_InvalidoYNoSeEscribe`, PG), 403 de un rol sin permiso
(`Post_RolSinPermiso_403YNoSeEscribe`, HTTP), restricciones de `articulo_precio_modalidad` contra el motor
(`Articulos/ArticuloPrecioModalidadRestriccionesPostgreSqlTests.cs`, H1).

### HU-ART-02 · Búsqueda de artículo por nombre o código (#15)

| CA | Texto (resumido) | Estado | Tests |
|---|---|---|---|
| CA1 | Nombre que coincide parcialmente → devuelve todos los coincidentes | **Verificado** | `Articulos/ArticuloRepositorioTests.cs` · `Busqueda_PaginaDespuesDeUnirAlternativasYConservaTotalYOrdenEstable` (PG, 5 de 5) · `Articulos/ArticuloBusquedaPostgreSqlTests.cs` · `BusquedaReal_ConEsquemaRealEIdsGenerados_RespetaRankingYUnicidadD2` (PG, 39 activos + 1 inactivo) · `ArticulosApiPostgreSqlTests.cs` · `Get_PaginacionYFiltroDeActivosExplicitos` (HTTP) |
| CA2 | Búsqueda por código interno → exactamente un artículo | **NO verificado** | Ningún test busca solo por un código interno y afirma un único resultado. Lo que hay: `ArticuloRepositorioTests.cs` · `Busqueda_OrdenaInternoProveedorYNombreSinDuplicarPorAlternativas` (PG) afirma que el código interno rankea primero, en una búsqueda combinada que devuelve tres. Además, con datos importados, un código interno puede coincidir con el código de proveedor de otro artículo, y entonces la búsqueda devuelve los dos y los informa como conflicto (`Conflictos_IncluyeTodosLosIdsAunqueSeanInactivosOFueraDePagina`, PG). Falta un test; el comportamiento ante un conflicto lo tiene que confirmar el equipo |
| CA3 | Búsqueda por código alternativo → devuelve el artículo dueño | **Verificado (indirecto)** | `Busqueda_OrdenaInternoProveedorYNombreSinDuplicarPorAlternativas` (PG): el artículo que coincide por código de proveedor rankea antes que el que coincide solo por nombre; sin la coincidencia por código, el orden se invierte. `BusquedaReal_...` (PG): los dos artículos con `gn-01` rankean antes que el resto. No hay un test que busque solo el código de proveedor y afirme un único resultado |
| CA4 | Código inexistente → mensaje «sin resultados» | **Verificado en la API; NO verificado en la pantalla** | API: `Articulos/ConsultarArticulosUseCaseTests.cs` · `Consulta_SinResultados_ExitoConListaVacia` (Moq) · `ArticulosApiPostgreSqlTests.cs` · `Get_SinResultados_200ConListaVaciaNunca404` (HTTP) · `ArticuloBusquedaPostgreSqlTests.cs` (`"no existe"` → vacío, PG). El mensaje lo muestra `frontend/src/app/articulos.ts:41` ("Sin resultados para «…»"), pero el frontend no tiene tests automáticos |
| CA5 | La ficha despliega códigos con su proveedor, último proveedor, costo vigente y precio de venta estimado | **Parcial** | Códigos con proveedor: `ArticuloRepositorioTests.cs` · `Detalle_ConservaCostoNuloOriginalDelCodigoYStockDecimalPorSucursal` (PG, razón social). Costo vigente: `Integracion/FacturasCompraApiPostgreSqlTests.cs` · `Post_Compra_LaFichaDelArticuloMuestraLosPreciosRecalculados` (HTTP, 100). Precios por modalidad con su razón: `Get_Ficha_200ConLasSeisModalidadesPorNombreYSuRazon` (HTTP) y el mismo `Post_Compra_...` (Lista 150, Costo 100, Efectivo 135). **Último proveedor: NO verificado contra la base.** Solo `Articulos/ObtenerArticuloDetalleUseCaseTests.cs` · `Ficha_CopiaLosDatosDelArticulo` (Moq) copia un `3`; el único test PG que lo mira afirma `null`. La ficha expone el Id, no el nombre del proveedor |

Extras verificados: comodín SQL `100%_original` (`Busqueda_100PorcientoGuionBajoOriginal_TraeSoloElLiteral`, PG),
texto vacío → 400 (`Get_TextoVacioOAusente_400`, HTTP), razones de ausencia de precio (los 8 tests de
`ObtenerArticuloDetalleUseCaseTests.cs`, Moq).

### HU-ART-03 · Modificación de artículo y sus códigos (#17)

| CA | Texto (resumido) | Estado | Tests |
|---|---|---|---|
| CA1 | Artículo activo y datos válidos → actualiza el registro | **Verificado** | `Articulos/ModificarArticuloPostgreSqlTests.cs` · `Modificar_ActualizaPorIdYNoTocaOtroArticulo` (PG) · `Integracion/ModificarArticuloApiPostgreSqlTests.cs` · `Put_OperadorModificaDatosYAgregaCodigo_204YSeGuarda` (HTTP) · `Articulos/ModificarArticuloUseCaseTests.cs` · `DatosValidos_ActualizaPorIdConLosDatosNormalizadosYConservaElCodigoInterno` (Moq) |
| CA2 | Agregar un código con su proveedor → se asocia sin alterar los existentes | **Verificado** | `ModificarArticuloPostgreSqlTests.cs` · `Modificar_AgregaElCodigoDeOtroProveedorYConservaLosExistentes` (PG, la fila existente conserva su Id) · `Modificar_SinCodigos_ConservaLosExistentes` (PG) · `ModificarArticuloUseCaseTests.cs` · `CodigoDeOtroProveedor_SeAgregaSoloEseYConservaLosExistentes` (Moq) |
| CA3 | Código de otro artículo → rechazo informando el conflicto | **Verificado** | `ModificarArticuloPostgreSqlTests.cs` · `Modificar_CodigoDeOtroArticulo_ConflictoInformandoCualYNoEscribeNada` (PG) · `ModificarArticuloApiPostgreSqlTests.cs` · `Put_CodigoDeOtroArticulo_409InformandoCual` (HTTP) · `ModificarArticuloUseCaseTests.cs` · `CodigoDeOtroArticulo_ConflictoInformandoCual` (Moq). El mismo código del propio artículo no es conflicto: `ElMismoCodigoDelPropioArticulo_NoEsConflictoYNoSeVuelveAAgregar` (Moq) |
| CA4 | Modificar los márgenes de venta → recalcula el precio solo de ese artículo | **NO verificado: no implementado** | Motivo: los márgenes son globales, no por artículo (respuesta c de Alfredo). El CA no aplica como está escrito: hay que reescribir la HU. El recálculo de un solo artículo ya existe (`CalcularPreciosArticuloUseCase`, Fase 6) |
| CA5 | Artículo dado de baja → se deniega e indica que debe reactivarse | **Verificado** | `ModificarArticuloUseCaseTests.cs` · `DadoDeBaja_InvalidoEIndicaQueHayQueReactivarlo` (Moq, mensaje) · `ModificarArticuloPostgreSqlTests.cs` · `Modificar_DadoDeBaja_InvalidoYNoEscribeNada` y `Modificar_OtraSesionLoDaDeBajaEntreLecturaYEscritura_InvalidoYNoEscribeNada` (PG) · `ModificarArticuloApiPostgreSqlTests.cs` · `Baja_Modificar_Reactivar_Modificar` (HTTP) |

Extras verificados: permisos desde la sesión (`Put_OperadorCambiaElCodigoInterno_403YNoEscribe`,
`Put_AdministradorCambiaElCodigoInterno_204YSeGuarda`, `Delete_OperadorRetiraUnCodigo_403YElCodigoQueda`,
`Delete_AdministradorRetiraUnCodigo_204YSeBorra`, HTTP; `Login_ContrasenaIncorrecta_DevuelveStatus401` en
`Acceso/LoginIntegracionTests.cs`); D2 en la modificación (`SegundoCodigoDeUnProveedorQueYaTieneCodigo_Conflicto`,
Moq); código interno manual (`CodigoInternoConElPrefijoDelSistema_Invalido`, `CodigoInternoDeOtroArticulo_ConflictoInformandoCual`,
Moq; `Modificar_CodigoInternoIgualAlCodigoDeProveedorDeOtroArticulo_Conflicto`, PG); baja y reactivación con
historial (`Baja_YReactivacion_EscribenLaFilaYElHistorialConLosUsuarios`, `Baja_ElHistorialFallaEnElMotor_ElArticuloSigueActivo`,
`Reactivar_ElHistorialFallaEnElMotor_ElArticuloSigueInactivo`, PG).

### HU-ART-05 · Precio de venta estimado por márgenes (#22)

| CA | Texto (resumido) | Estado | Tests |
|---|---|---|---|
| CA1 | Con márgenes cargados, una compra que actualiza el costo → recalcula el precio estimado | **Verificado** para Lista, Costo y Efectivo | `FacturasCompra/RegistrarFacturaCompraPostgreSqlTests.cs` · `Registrar_RecalculaLosPreciosDeLosComprados_YNoLosDelResto` (PG) · `FacturasCompraApiPostgreSqlTests.cs` · `Post_Compra_LaFichaDelArticuloMuestraLosPreciosRecalculados` (HTTP) · `FacturasCompra/RegistrarFacturaCompraUseCaseTests.cs` · `Valida_RecalculaLosPreciosDeLosComprados_ConLaFechaDelRegistro` (Moq) · `Articulos/RecalcularPreciosTests.cs` · `ConfiguracionInicialYCosto100_Lista150Costo100Efectivo135` (Unit) |
| CA2 | Recalcular un artículo no modifica ningún otro | **Verificado** | `Registrar_RecalculaLosPreciosDeLosComprados_YNoLosDelResto` (PG, el no comprado conserva sus seis filas) · `Articulos/CalcularPreciosArticuloUseCaseTests.cs` · `PideSoloLosArticulosIndicados` (Moq) |
| CA3 | Sin márgenes, al actualizar el costo no calcula y lo indica en la ficha | **Verificado (solo Moq)** | `CalcularPreciosArticuloUseCaseTests.cs` · `ArticuloSinFilasDePrecio_NoSeEscribe` (Moq) · `ObtenerArticuloDetalleUseCaseTests.cs` · `Ficha_SinFilas_SeisModalidadesSinConfiguracionEnElOrdenDelEnum` (Moq). Sin test PG ni HTTP de una compra sobre un artículo sin filas de precio (hoy, solo los anteriores a D1) |
| CA4 | Al registrar una factura de venta se puede ingresar un precio distinto del estimado | **NO verificado** | Motivo: no hay factura de venta en el Sprint 3. Ninguna HU del sprint la toca |

Por qué #22 sigue abierto, además de CA4: el precio de **CasoEspecial** no se puede fijar (no hay
endpoint ni HU), **falta la modalidad L2** (respuesta c) y **Tarjeta3/Tarjeta6 tienen que salir**
(respuesta a). Las dos últimas son cambio de esquema: el CHECK `articulo_precio_modalidad_valida` lista
los seis nombres actuales. Regla del plan: una modalidad sin sus parámetros definidos no se declara
implementada.

Extras verificados: precio manual que no se pisa (`Registrar_PrecioManualFijadoMientrasSeRecalcula_SobreviveALaCompra`,
`Registrar_FilaQuePasaAManualMientrasSeRecalcula_SobreviveALaCompra`, PG); invariante de `SinCosto`
(`Registrar_NingunCompradoQuedaConBaseYSinPrecio`, PG); límite de la columna por los dos lados
(`PrecioJustoEnElLimiteDeSuColumna_InvalidoYNoEscribe`, `PrecioEnElMaximoDeSuColumna_SeEscribe`, Moq;
`PrecioRecalculadoQueNoEntraEnSuColumna_InvalidoYNoQuedaNada`, PG); Lista manual → Efectivo
(`RecalcularPreciosTests.cs` · `ListaManual_EfectivoSobreLaListaManual_YLaListaNoSeToca`, Unit; hoy
decisión confirmada, ver la sección 3).

### HU-FCO-01 · Registro manual de factura de compra (#32)

| CA | Texto (resumido) | Estado | Tests |
|---|---|---|---|
| CA1 | Proveedor activo y al menos una línea → registra, incrementa stock, actualiza último proveedor y costo, agrega historial | **Verificado** | `RegistrarFacturaCompraPostgreSqlTests.cs` · `Registrar_EscribeFacturaStockCostoUltimoProveedorEHistorial_SoloDeLosComprados` (PG, stock por destino D9, solo los comprados) · `FacturasCompraApiPostgreSqlTests.cs` · `Post_Valida_201ConIdYLocation_YLaCompraQuedaEscritaConElUsuarioDelToken` (HTTP) · `RegistrarFacturaCompraUseCaseTests.cs` · `Valida_RegistraLaFacturaConSusDatosYDevuelveElId`, `ProveedorInactivo_Invalido` (Moq). Atomicidad, un test PG por punto de falla: `Atomicidad_FallaAlInsertarLaCabecera_NoQuedaNada`, `..._DetalleDeLaSegundaLinea_...`, `..._StockDeLaSegundaLinea_...`, `..._CostoYUltimoProveedorDeLaSegundaLinea_...`, `..._HistorialDeLaSegundaLinea_...` |
| CA2 | Número activo repetido para ese proveedor → rechazo informando la factura con que colisiona | **Verificado** | `RegistrarFacturaCompraUseCaseTests.cs` · `NumeroActivoRepetido_ConflictoQueInformaLaFacturaConLaQueChoca` (Moq) · `RegistrarFacturaCompraPostgreSqlTests.cs` · `NumeroActivoRepetido_ConflictoYNoEscribeNada`, `ObtenerNumeroActivo_SoloLaActivaDelMismoProveedor`, `Atomicidad_CabeceraRechazadaPorElIndiceDeNumeroActivo_NoQuedaNada` (PG, carrera) · `FacturasCompraApiPostgreSqlTests.cs` · `Post_NumeroActivoRepetido_409QueInformaLaFacturaEnConflicto` (HTTP) |
| CA3 | Sin líneas → se bloquea | **Verificado** | `RegistrarFacturaCompraUseCaseTests.cs` · `SinLineas_Invalido`, `LineasAusentes_Invalido` (Moq) · `FacturasCompraApiPostgreSqlTests.cs` · `Post_SinLineas_400YNoEscribe` (HTTP) |
| CA4 | Emisión, vencimiento y "retenciones" se guardan como datos, sin lógica financiera | **Verificado** (son **percepciones**, D8) | `Registrar_EscribeFacturaStockCostoUltimoProveedorEHistorial_SoloDeLosComprados` (PG, fechas y las cuatro percepciones releídas) · `Post_Valida_201ConIdYLocation_...` (HTTP) · `SinVencimientoNiPercepciones_RegistraNullYCeros` (Moq) |
| CA5 | Al actualizar el costo de un artículo con márgenes → recalcula el precio estimado | **Verificado** | `Registrar_RecalculaLosPreciosDeLosComprados_YNoLosDelResto` (PG) · `Post_Compra_LaFichaDelArticuloMuestraLosPreciosRecalculados` (HTTP) · `Valida_ElRecalculoOcurreDentroDeLaUnidadDeTrabajo`, `RecalculoRechazado_DevuelveSuFalloALaUnidadParaQueSeDescarteTodo` (Moq) · `Atomicidad_FallaAlRecalcularLosPreciosDelSegundoComprado_NoQuedaNada` (PG) |

### Resumen

| HU | Issue | Verificados | NO verificados / parciales |
|---|---|---|---|
| ART-01 | #14 | CA1, CA3, CA4, CA5 (Moq) | CA2 y CA6, no aplican como están escritos (código interno generado, márgenes globales) |
| ART-02 | #15 | CA1, CA3 (indirecto), CA4 (API) | CA2 sin test; CA4 en pantalla sin test; CA5 sin último proveedor contra la base |
| ART-03 | #17 | CA1, CA2, CA3, CA5 | CA4, no implementado |
| ART-05 | #22 | CA1, CA2, CA3 (Moq) | CA4 (sin ventas); CasoEspecial, L2, Tarjeta3/6 |
| FCO-01 | #32 | CA1 a CA5 | — |

---

## 2 · Estado de los Issues

| Issue | HU | Estado | Motivo |
|---|---|---|---|
| #14 | ART-01 | **Cerrado** | CA1, CA3, CA4 y CA5 verificados. CA2 y CA6 no aplican como están escritos: el código interno lo genera el sistema y los márgenes son globales (sección 3, c). Conviene corregir el texto del Issue para que no quede un CA sin tilde |
| #15 | ART-02 | **Cerrado** | Búsqueda, ficha y pantalla entregadas (#118). Quedan como deuda de tests, sin cambio de código: CA2 (un test de búsqueda por código interno solo), CA5 (último proveedor releído de la base) y el mensaje de CA4 en la pantalla |
| #32 | FCO-01 | **Cerrado** | Los cinco CA verificados. CA5 se cerró con la Fase 6 (#121, `Closes #32`) |
| #22 | ART-05 | **ABIERTO** | Falta el precio de CasoEspecial (no hay endpoint ni HU), agregar la modalidad L2 y quitar Tarjeta3/Tarjeta6. Es Sprint 4, y L2 y Tarjeta son cambio de esquema (compuerta). CA4 depende de las ventas |
| #17 | ART-03 | **ABIERTO** | CA4 hay que reescribirlo: los márgenes son globales, no por artículo (sección 3, c) |

> **Ojo, discrepancia con GitHub (consultado el 2026-10-17):** los cinco Issues figuran **cerrados**,
> todos el 2026-10-17 a las 18:14 UTC por `AAAActis`, como "completed". **#17 y #22 hay que
> reabrirlos.** El agente no tiene `gh` y no los toca: lo hace el equipo.

---

## 3 · Respuestas de Alfredo (17/10/2026, vía Javi)

Textuales y ya aclaradas. **No se numeran:** las decisiones las numera el equipo.

**a · Tarjeta3 / Tarjeta6.** Venden en cuotas, pero Alfredo prefiere que **no** estén en el sistema →
**salen del alcance.**
Consecuencia (Sprint 4, no se implementa acá): quitar las dos modalidades es cambio de esquema.
`articulo_precio_modalidad_valida` lista los seis nombres, y el alta inserta seis filas por artículo.
Va con un script del equipo (compuerta) y re-scaffold. Además hay que borrar o reapuntar los tests que
usan las tasas SOLO DE PRUEBA (`RecalcularPreciosTests.TarjetasConTasasDePrueba_AplicanLaFormula`,
`CalcularPreciosArticuloUseCaseTests.PrecioJustoEnElLimiteDeSuColumna_InvalidoYNoEscribe`).

**b · Lista manual → Efectivo.** Sí: **−10 % sobre la lista.** La interpretación de la Fase 6 pasa a
**DECISIÓN confirmada**: si la fila de Lista es manual, Efectivo = Lista manual × 0,90. El código ya lo
hace (`RecalcularPreciosTests.ListaManual_EfectivoSobreLaListaManual_YLaListaNoSeToca`); no cambia nada.

**c · Márgenes.** Tres listas **globales**, iguales para todos los artículos, aparte de Efectivo:
- **L1 +50 %** (= la Lista actual).
- **L2 +40 %** (NUEVA: hoy no existe en el sistema).
- **L3 0 % = al costo** (= el Costo actual; la usan los dueños para sus autos particulares).

Efectivo sigue siendo **L1 −10 %**. Podrían usar L2, pero no lo hacen.

Consecuencias, para el Sprint 4 (**no se implementan acá**):
- Agregar la modalidad L2 y quitar Tarjeta3/Tarjeta6 es **cambio de esquema** (compuerta, script del
  equipo).
- CA4 de #17 ("márgenes por artículo") **no aplica como está escrito**: hay que reescribir la HU.
- Lo mismo vale para CA6 de #14 ("el usuario cargó los márgenes").

**d · Baja de artículo.** **Todos los roles.** La interpretación de la Fase 7 pasa a **DECISIÓN
confirmada**: baja y reactivación de artículo para ADMINISTRADOR y OPERADOR. El código ya lo hace
(`DarDeBajaArticuloUseCase` y `ReactivarArticuloUseCase` no miran el rol); no cambia nada.

---

## 4 · Deuda, agrupada por tipo

Responsable **sugerido**: lo confirma el equipo. "Agente" = una sesión de Claude Code con TDD y los dos
subagentes.

### 4.1 · Regla: toda escritura verifica que escribió (Sprint 4)
Toda operación de escritura devuelve si tocó una fila, y el caso de uso lo consume: un `false` es un
error, no un éxito silencioso. Era el N2 del auditor de la Fase 7; lo subió Santi.

| # | Aparición | Estado |
|---|---|---|
| 1 | Crítico 3 (plan, Fase 7): actualizar por Id, nunca por código | Resuelto (Fase 1) |
| 2 | `ProveedorRepositorio.ActualizarAsync` devolvía `void` | Resuelto (Fase 2, 2.3): devuelve `bool` y lo consumen Modificar, DarDeBaja y Reactivar proveedor |
| 3 | `ExecuteUpdate` del código interno (`AsignarCodigoInternoAsync`, `ActualizarDatosAsync`) | Resuelto (Fases 3 y 7): `filas == 1` |
| 4 | `CerrarAsync` ignorado en `ReactivarArticuloUseCase`, `ReactivarClienteUseCase` y `ReactivarProveedorUseCase` | **Abierto.** Un inactivo sin baja abierta (dato importado) se reactiva sin rastro |
| 5 | `CerrarAsync` ignorado en `ReactivarUsuarioUseCase` (`backend/LaCentral.UseCases/Acceso/ReactivarUsuarioUseCase.cs:58`) | **Abierto** |
| 6 | `ActualizarAsync` ignorado en `RestablecerContrasenaUseCase` (`backend/LaCentral.UseCases/Acceso/RestablecerContrasenaUseCase.cs:43`) | **Abierto** |

Responsable sugerido: **agente**, con TDD (un test en rojo por camino real en cada aparición).
Antes, el equipo decide qué responde una reactivación sin baja abierta: ¿error, o reactivar y abrir un
rastro?

### 4.2 · N4 · carrera código interno / código de proveedor (COMPUERTA del Sprint 4)
La unicidad entre `articulo.codigo_interno` y `articulo_codigo_alternativo.codigo` la controla solo la
aplicación. Con el código interno editable a mano, dos sesiones en carrera pueden dejar la misma clave
en los dos lados. Cerrarla pide **esquema** (un trigger o una tabla única de códigos) o un **lock**.
Responsable sugerido: **equipo (Santi, esquema)** decide; después, el agente implementa.

### 4.3 · Precio manual (HU nueva, Sprint 4)
No hay endpoint ni HU para fijar el precio de CasoEspecial. Los tests de la Fase 6 se adelantan a ese
endpoint. Lo que tiene que resolver:
- **OB1 · lectura desfasada:** si Lista pasa a manual entre `ObtenerAsync` y la escritura, Efectivo se
  escribe con la Lista automática leída. El inverso: una fila manual que vuelve a automática recibe el
  precio manual viejo. Arreglo posible: `SELECT ... FOR UPDATE` sobre los comprados.
- **OB2 · deadlock 40P01:** la compra bloquea `articulo`/`stock` y después `articulo_precio_modalidad`.
  El endpoint de precio manual tiene que respetar ese orden.
- **OB3 (menor):** `ActualizarPreciosAsync` manda un UPDATE sin efecto por cada fila manual.
- **Lista manual sin precio fijado:** Efectivo quedaría con base y sin precio, y la ficha lo mostraría
  como `SinCosto` (falso). Hoy no se alcanza.
- Con L2 (sección 3, c) la HU tiene que decir si L2 también admite precio manual.

Responsable sugerido: **Javi con Alfredo** escriben la HU; el **agente** implementa.

### 4.4 · Historial de códigos retirados
Retirar un código borra la fila de `articulo_codigo_alternativo`: no queda rastro de qué códigos tuvo
un artículo. Guardarlo es esquema. Responsable sugerido: **equipo** decide si hace falta; si sí,
**Santi** (script).

### 4.5 · Esquema huérfano en `lacentral_test`
`pr110_test_5bef5661630743e4b6d09c409f11fafa`, dueño `lacentral_tests`. Lo vio el auditor de la Fase 6
y sigue ahí al 2026-10-17. Responsable: **Santi** (lo borra a mano; los subagentes no borran, R3).

### 4.6 · Resto de la deuda declarada en `ESTADO.md`

**Tests y persistencia** (responsable sugerido: agente)
- Fase 2, auditor 4: las bajas concurrentes se prueban solo a nivel repositorio. Falta un test con dos
  `DarDeBaja*` en transacción (el segundo da 23505, 409, y revierte el UPDATE de la fila).
- Fase 2, auditor: falta un test de que un CHECK **no** reconocido sigue dando 500, y un test HTTP de
  CHECK → 409. `*_hist_motivo_no_vacio` no tiene test ni está en la lista blanca.
- Fase 2: `DarDeBajaClienteUseCase.cs:49` y `DarDeBajaProveedorUseCase.cs:59` no hacen `Trim()` del
  motivo (usuario y artículo sí).
- Fase 2, menores: la guarda `duenioDelCodigo.Id != cliente.Id` de `ModificarClienteUseCase` sin test
  (probablemente inalcanzable: se borra o se prueba); los dobles `ProveedorInactivo()` y
  `UsuarioInactivo()` de `Comun/HistorialBajaCasosDeUsoTests.cs` modelan un estado que el CHECK prohíbe.
- `Integracion/CicloCompletoIntegracionTests.cs` mockea repositorios, historial y unidad de trabajo: el
  nombre promete más de lo que verifica.
- `Data/UsuarioRepositorioTests.cs` sigue en InMemory (solo "Id inexistente → false").
- Fase 6, O2: el comentario de `Atomicidad_FallaAlRecalcularLosPreciosDelSegundoComprado_NoQuedaNada`
  afirma un orden que ningún assert comprueba. O3: ningún test fuerza una falla del recálculo por HTTP.
- Fase 7, N5: no hay test HTTP del 409 que sale de un 23505 en una carrera al modificar.
- Fase 7: el conflicto en carrera de `AgregarCodigosAsync` llega como `DbUpdateException`; por HTTP sale
  409 con el mensaje genérico del índice.
- Los tests de la sección 1 que faltan: ART-02 CA2 (búsqueda por código interno solo), ART-02 CA5 (último
  proveedor releído de la base), ART-01 CA5 por HTTP, ART-05 CA3 por PG.

**Reglas de negocio y validación** (responsable sugerido: equipo decide, agente implementa)
- Fase 7, N3: los códigos de proveedor pueden empezar con `LC-` (`ArticuloValidaciones.ValidarCodigos`),
  aunque el código interno manual no.
- Fase 4: `ArticuloRepositorio.BuscarAsync` sin alternativas tira NRE. Hoy no se alcanza (el caso de uso
  valida antes), pero el contrato del puerto no lo dice.
- Fase 5: el rango de `EntraEnColumna` para el descuento es inalcanzable (antes corre la guarda 0-100).

**Riesgos latentes del DbContext** (responsable sugerido: agente, cuando se reuse el contexto)
- Fase 3: después del `ExecuteUpdate` del código interno, el contexto sigue rastreando la entidad con
  `TMP-`.
- Fase 5: después de un fallo de `RegistrarConEfectosAsync`, el contexto conserva entidades rastreadas.

**Infraestructura y proceso** (responsable sugerido: Santi)
- EF Core difiere entre Data (10.0.11) y Api/Tests (10.0.12, por InMemory y Mvc.Testing). La Api
  referencia `EntityFrameworkCore.InMemory` en producción.
- Deriva de esquema: antes de cada re-scaffold hay que comparar test contra producción (consulta en
  `backend/db/rescaffold/README.md`). La comparación depende siempre del equipo, porque el agente no
  tiene acceso a `lacentral`. `ValidarTablaAsync` compara en un solo sentido.
- Código manual dentro de la carpeta generada: `backend/LaCentral.Data/Models/HistorialesBaja.Personalizaciones.cs`.
- Seguridad de datos: `lacentral_test.public` tiene 124 proveedores y 2108 clientes (parece copia de los
  datos reales), en el mismo servidor que la base principal. `ValidarBase` solo mira el nombre.
- `docs/entorno-local.md`, sección 3: dice "17 tests" en rojo sin la variable; hoy son 145.
- El plan (`PLAN_SPRINT3.md`) todavía dice "un test que pasa a la primera → PARÁS"; el onboarding ya
  dice "se declara y se verifica con mutación". Hay que alinear el plan.
- 17 ramas locales viejas, mergeadas, de antes del sprint. No se borraron.
- `.claude/agents/verificador-tdd.md`: le falta un salto de línea en el encabezado de PRESUPUESTO
  ("caraCorrer"). La definición es del equipo.

**Frontend** (responsable sugerido: Javi)
- `frontend/` no tiene tests automáticos (de ahí lo de CA4 de ART-02).
- Las escrituras de la Fase 7 por la pantalla no se probaron en el navegador: la base de test solo
  tiene SELECT en `public`. Hace falta un entorno con escritura que no sea la copia de datos reales.

**Ya resuelta durante el sprint** (registro; no se cuenta como deuda abierta): NUL → 500 (Fase 6,
`e2118f6`); invariante de `SinCosto` (Fase 6); O1 de la Fase 6 (`e92707c`); hallazgos 1, 2 y 3 del
auditor de la Fase 2 (`b789b0e`, `a516caa`→`83f0f7c`, `f00a0ae`→`d378c0f`); 53300 y esquemas huérfanos de
la suite (`70e0452`); compuerta D1; 401 → 403 por rol (Fase 7, `1a93f7e`); el comentario de la tabla
que nombraba un `CalcularPreciosArticuloUseCase` inexistente (existe desde la Fase 6).

---

## 5 · Números del sprint

### 5.1 · Tests por fase
Siempre 0 omitidos y, desde la Fase 1, 0 warnings con `--no-incremental`. "Dobles / sin base" =
total − PG: Moq, HTTP con repositorios mockeados, unitarios puros y 1 InMemory.

| Momento | Commit | Total | PG real (`[PostgreSqlFact]`) | Dobles / sin base | Δ total | Δ PG |
|---|---|---|---|---|---|---|
| Fase 0 · línea base | `1660d5d` | **108** | 8 | 100 | — | — |
| Fase 1 · deuda bloqueante | `108ac1d` | 129 | 17 | 112 | +21 | +9 |
| Fase 2 · D10 | `d378c0f` | 176 | 43 | 133 | +47 | +26 |
| D1 · compuerta de esquema | — | 179 | 46 | 133 | +3 | +3 |
| Fase 3 · ART-01 | `a9e7410` | 234 | 77 | 157 | +55 | +31 |
| Fase 4 · ART-02 | `d44bab7` | 258 | 86 | 172 | +24 | +9 |
| PASO 0 · limpieza de `PostgreSqlPruebas` | — | 261 | 89 | 172 | +3 | +3 |
| Fase 5 · FCO-01 | `f424771` | 325 | 109 | 216 | +64 | +20 |
| Fase 6 · ART-05 + FCO-01 CA5 | `c575e4e` | 353 | 119 | 234 | +28 | +10 |
| Fase 7 · ART-03 | `aa2e795` | **431** | **145** | 286 | +78 | +26 |

**Sprint: 108 → 431 (+323). PG real: 8 → 145 (×18). Proporción PG: 7 % → 34 %.**
La línea base tenía 1 test InMemory (`UsuarioRepositorioTests`) y sigue estando. Los 145 PG incluyen los
HTTP que levantan la API con `WebApplicationFactory` sobre el esquema aislado.
Fuente: tabla de fases de `ESTADO.md`. El conteo de 145 se re-contó en `develop` con un grep de
`[PostgreSqlFact]`. La suite completa no se re-corrió en esta fase: el código no cambió desde `aa2e795`
(431/431, corrida del auditor de la Fase 7).

### 5.2 · Cada NO APTO de un subagente

| Fase | Subagente | Qué encontró | Cómo se arregló | Resultado |
|---|---|---|---|---|
| 2 | verificador | Primera pasada: los 34 PG en rojo por 28P01 (credencial vieja en la variable, no código), orden test → producción no verificable desde git (7 de 8 commits traían test y producción juntos) y rojo solo contra stubs `NotImplementedException` | El 28P01 lo corrigió el equipo. El rojo de los 7 PG de historial se comprobó por mutación | Siguió NO APTO por documentación (fila de abajo) |
| 2 | auditor | Primera pasada "NO VERIFICADA" (28P01). Hallazgos: 1 atomicidad sin test en 4 operaciones, 2 `usuario_baja_id` perdido en silencio, 3 `*_hist_fechas_coherentes` → 500, 4 concurrencia, 5 dobles inválidos | `b789b0e` (4 tests PG de atomicidad), `a516caa`→`83f0f7c`, `f00a0ae`→`d378c0f` | APTO sobre `d378c0f`. El 4 y el 5 quedan como deuda |
| 2 | verificador | Segunda pasada: el cuerpo del PR afirmaba "cada test se vio fallar antes", y git no lo respaldaba; conteos desactualizados | Con OK del equipo se reescribió el cuerpo para declarar lo no verificable (`c4bbfd2`). El hallazgo ERA el texto falso del PR | APTO sobre `c4bbfd2` |
| 3 | verificador | 4 mutaciones sobrevivían (dos pasadas): alta sin `codigosProveedor`, rama `creado is null`, guarda `Enum.IsDefined`, DataAnnotations sin test de 400 | Ronda 1, `f05cee4`..`a9e7410`: tests de 201 sin códigos y de 400; las dos guardas inalcanzables se **borraron** (`e4f0f21`, `a9e7410`) | APTO |
| 4 | auditor | H1: la ficha se releía con el contexto de la siembra; quitar `AsNoTracking` y los `Include` sobrevivía | `d44bab7`: relectura con un contexto nuevo | APTO |
| 5 | verificador | B1 (sin la guarda "no existe", el test solo miraba "999") y B2 (`<` → `<=` en el vencimiento) | Ronda 1, `12480a2`, `10a0f0b` (tests) | APTO |
| 5 | auditor | H1: valores que NUMERIC no guarda → 23514/22003 → 500 | Ronda 1, `3e110cb` → `f34bbc4` (`EntraEnColumna`) | Siguió NO APTO (fila de abajo) |
| 5 | auditor | La suma al stock existente desbordaba `stock.cantidad` → 500 | Ronda 2, `0b7da32` → `163b2cc` → `b4fca2f` (upsert con `WHERE ... < 1e9`) | APTO |
| 5 | verificador | M11: `<` → `<=` en el WHERE del upsert sobrevivía | Ronda 3 (autorizada), `0190052`: borde suma = 1e9 → Invalido | Siguió NO APTO (fila de abajo) |
| 5 | verificador | M12: `LimiteStock` una milésima menos sobrevivía (faltaba el lado aceptado del borde) | Ronda 4 (autorizada por el equipo, excede el límite), `f424771` | APTO |
| 6 | verificador | M6: `>=` → `>` en `LimitePrecio` sobrevivía | `84967d8` (test en el límite exacto) | APTO |
| 6 | verificador | B3 del PR #121: M3, filtrar las manuales sobre lo leído en vez de en el UPDATE, sobrevivía | `c575e4e` (otra sesión pasa a manual una fila automática al leer) | APTO |
| 7 | auditor | B1: código interno nuevo igual a un código de proveedor nuevo de la misma petición se guardaba. B2: el UPDATE pisaba el código interno que otra sesión había cambiado | Ronda 1, `e060dbe` (7 rojos) → `7858c26` | APTO (y otra vez APTO sobre el cambio a 403) |

Sin NO APTO: Fase 1 (los dos APTO), D1 (los dos APTO), Fase 4 verificador (APTO, aunque marcó una
mutación sobreviviente que igual se arregló en `1d6afc1`) y Fase 7 verificador (APTO en las tres
pasadas).

La Fase 5 es la única que pasó el límite de 3 rondas: el agente PARÓ después de la ronda 3, y el
equipo autorizó la cuarta, solo para M12.
