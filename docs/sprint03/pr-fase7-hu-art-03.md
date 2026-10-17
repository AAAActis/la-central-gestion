# PR · Fase 7 Sprint 3 — HU-ART-03 · Modificación de artículo y sus códigos

**Rama:** `feature/HU-ART-03-modificar-articulo` → **`develop`** (sale de `0580a16`)
**Título sugerido:** `feat: modificacion de articulo, codigos de proveedor, baja y reactivacion con permisos por rol (HU-ART-03)`
**Issues:** Refs #17. **No lo cierra:** CA4 (modificar los márgenes de venta) no está implementado, porque falta una regla de negocio (ver "Qué NO hace").

## Qué hace

### Permisos desde la sesión
- `IContextoUsuario.EsAdministrador`, leído del claim de rol en `ContextoUsuario` (`IsInRole("ADMINISTRADOR")`, el mismo claim que firma `GeneradorToken` y que lee `[Authorize(Roles)]`). Decisión de Javi.
- El caso de uso devuelve `TipoError.Prohibido` si un OPERADOR cambia el código interno o retira un código, y `MapeoResultado` lo traduce a **403**. Decisión de Javi. Los demás `NoAutorizado` (login inválido, sesión sin usuario) siguen en 401. El rol nunca sale del cuerpo: un `"rol": "ADMINISTRADOR"` en el JSON no cambia nada (test HTTP).
- El `[Authorize(Roles = "ADMINISTRADOR,OPERADOR")]` del controlador se mantiene.

### `PUT /api/articulos/{id}` · `ModificarArticuloUseCase` (204)
- **CA1:** nombre, ubicación y código interno, **por Id**, en la unidad de trabajo. La condición `activo` va en el WHERE del UPDATE: si otra sesión da de baja el artículo entre la lectura y la escritura, devuelve 400 y no escribe nada.
- **CA2:** `codigosProveedor` son los que se asocian. Los nuevos se insertan normalizados; los que el artículo ya tiene (mismo proveedor y misma clave) no se tocan ni se revalidan; **uno omitido NO se retira**.
- **CA3:** un código de otro artículo → 409 informando cuál (código interno y nombre). El mismo código del propio artículo no es conflicto.
- **D2:** un segundo código del mismo proveedor → 409. Cambiar el código de un proveedor es retirar (admin) y agregar.
- **CA5:** dado de baja → 400, "reactivalo antes de modificarlo".
- **Código interno (solo ADMINISTRADOR):** 1-40 caracteres, normalizado (trim + mayúsculas), sin el prefijo `LC-` y sin coincidir con ningún código interno ni de proveedor, incluidos los del propio artículo y los nuevos de la misma petición (B1 del auditor). Mandar el mismo código con otro formato no es un cambio. Si no cambia, el UPDATE no toca la columna, así que no se pisa un cambio que otra sesión hizo entre la lectura y la escritura (B2).
- Las validaciones de nombre, ubicación y códigos son las de `ArticuloValidaciones`, compartidas con el alta. El DTO HTTP agrega `Required`/`MaxLength` (400 con errores por campo).

### `DELETE /api/articulos/{id}/codigos/{proveedorId}` · `RetirarCodigoProveedorUseCase` (204)
- Operación explícita, **solo ADMINISTRADOR**. D2: el proveedor identifica el código.
- Si el artículo está dado de baja → 400. Si no tiene código de ese proveedor (antes o al borrar) → 404.

### `POST /{id}/baja` y `POST /{id}/reactivacion` · baja lógica con el historial de la Fase 2 (D10)
- Baja: motivo obligatorio (con trim) y usuario de la sesión. La fila y `articulo_historial_baja` se escriben en la misma unidad de trabajo, con la misma fecha. `articulo` no tiene `usuario_baja_id`: el usuario queda solo en el historial, como en proveedor.
- Reactivación: cierra la baja abierta con el usuario de la sesión. La fila conserva motivo y fecha de la última baja (D10).
- Las dos son para ADMINISTRADOR y OPERADOR.

### Ficha y frontend
- `GET /api/articulos/{id}` suma `ubicacionDeposito` y `motivoBaja`. El cambio es aditivo.
- **Entregable visible:** en la ficha de `frontend/src/app/articulos.ts` se pueden:
  - modificar nombre, ubicación y código interno;
  - agregar códigos;
  - retirar un código;
  - dar de baja con motivo y reactivar.
- El código interno y "Retirar" solo se habilitan para ADMINISTRADOR. Es solo visual: el permiso lo decide la API. Ver `frontend/README.md`.

## Qué NO hace
- **CA4 · modificar los márgenes de venta → recalcular solo ese artículo. No implementado: falta una regla de negocio y choca con D1.**
  - D1 fija bases y porcentajes: Lista +50 % sobre costo, Costo 0 %, Efectivo −10 % sobre Lista.
  - Tarjeta3 y Tarjeta6 tienen tasas "configurables, sin definir".
  - CasoEspecial es precio manual, de otra HU, con OB1/OB2 pendientes.
  - Falta decidir:
    - qué márgenes se editan por artículo;
    - si cambiar el +50 % de UN artículo contradice D1, o si D1 es solo el valor inicial;
    - si las tasas de tarjeta son por artículo o globales;
    - quién puede editarlos.
  - No se inventó ninguna. El recálculo de un solo artículo ya existe (`CalcularPreciosArticuloUseCase`, Fase 6).
- No guarda historial de códigos retirados: se borra la fila. Nada referencia `articulo_codigo_alternativo`.
- No cambia el esquema ni `Data/Models/`.

## Decisiones (confirmadas por Javi, 2026-10-17)
1. Un código interno puesto a mano no puede empezar con `LC-`. Sin esto, uno como `LC-000999` haría fallar con 409 el alta del artículo de Id 999.
2. Retirar un código de un artículo dado de baja → 400, por la misma regla que CA5.
3. Baja y reactivación de artículo: ADMINISTRADOR y OPERADOR.
4. Un rol sin permiso recibe **403**: `TipoError.Prohibido`, solo en los dos chequeos de rol. Login inválido y sesión sin usuario siguen en 401 (`NoAutorizado`).

Queda para el equipo: la ficha suma `ubicacionDeposito` y `motivoBaja` (aditivo).

## Cómo se verificó
- **Suite (en `aa2e795`, corrida del auditor):**
  - **431 total · 431 passed · 0 failed · 0 skipped**, 0 warnings con `--no-incremental`;
  - **145 `[PostgreSqlFact]`** contra PostgreSQL real, con el rol `lacentral_javi`;
  - antes: 353 y 119. Tests nuevos: **78**, 26 de ellos contra PostgreSQL.
- **Persistencia:**
  - todo con casos de uso, repositorios y unidad de trabajo reales sobre el mismo contexto, y relectura con un contexto nuevo. Solo la sesión es un doble;
  - sin Ids sembrados.
- **Atomicidad con fallas reales del motor:**
  - un código que choca en `codigo_unico_global` después de actualizar los datos: carrera simulada por otra sesión;
  - `ux_articulo_hist_abierta` en la baja;
  - `articulo_hist_fechas_coherentes` en la reactivación.
  - En los tres casos no queda nada escrito.
- **HTTP:** `WebApplicationFactory` contra PostgreSQL real, con tokens de OPERADOR y ADMINISTRADOR.
- **Prueba de humo por el proxy contra `lacentral_test`, sin escrituras.** El rol de test solo tiene SELECT en `public`, que es la copia de los datos reales.
  - Las 4 rutas nuevas sin token → 401: existen, porque una ruta inexistente da 404.
  - Login malo → 401. Pantalla → 200.
  - **Las escrituras por la pantalla no se probaron en el navegador.**

### Criterios de aceptación → tests
| CA | Estado | Tests |
|---|---|---|
| CA1 | verificado | `ModificarArticuloPostgreSqlTests.Modificar_ActualizaPorIdYNoTocaOtroArticulo`, `...Api...Put_OperadorModificaDatosYAgregaCodigo_204YSeGuarda` |
| CA2 | verificado | `Modificar_AgregaElCodigoDeOtroProveedorYConservaLosExistentes` (la fila existente conserva su Id), `Modificar_SinCodigos_ConservaLosExistentes` |
| CA3 | verificado | `Modificar_CodigoDeOtroArticulo_ConflictoInformandoCualYNoEscribeNada`, `Put_CodigoDeOtroArticulo_409InformandoCual` |
| CA4 | **NO implementado** | — (falta la regla de negocio) |
| CA5 | verificado | `Modificar_DadoDeBaja_InvalidoYNoEscribeNada`, `Modificar_OtraSesionLoDaDeBajaEntreLecturaYEscritura_...`, `Baja_Modificar_Reactivar_Modificar` (HTTP) |
| Permisos | verificado | `Put_OperadorCambiaElCodigoInterno_403YNoEscribe`, `Put_AdministradorCambiaElCodigoInterno_204YSeGuarda`, `Delete_OperadorRetiraUnCodigo_403YElCodigoQueda`, `Delete_Administrador..._204`, `Login_ContrasenaIncorrecta_DevuelveStatus401` |

## TDD
| Unidad | estructura → test (rojo) → producción |
|---|---|
| Estructura | `901d55f`: el puerto, las firmas, esqueletos que devuelven Success y stubs que no escriben |
| Modificación (Moq) | `02c2d53`: 27 rojos, 21 por Assert y 6 por Verify → `a9dbd7f` |
| Retiro, baja y reactivación (Moq) | `2c16031`: 20 rojos → `2983318` |
| Repositorio (PG) | `9ff4ac2`: 14 tests, 11 rojos por aserción → `e72d569` |
| HTTP (PG) | `b9e42b1`: 10 tests, 5 rojos por aserción → `4f9a5f5` |
| Frontend | `7bc47c4` |
| Borde del código interno (no bloqueante del verificador) | `a1f4143`: 40 caracteres se guardan. Pasa a la primera, declarado; con `>` → `>=` da rojo |
| Ronda 1 del auditor (B1, B2, N1) | `e060dbe`: 7 rojos (4 Verify, 2 Expected Conflicto / Actual Ninguno, 1 Expected "ADM-9" / Actual "LC-000001") → `7858c26` |
| Guarda de B1 por los dos lados (no bloqueante del verificador) | `acc8f9d`: caracterización; `Any` → `All` y `==` → `!=` dan rojo |
| Rol sin permiso → 403 (decisión de Javi) | `0abcd8c` (`TipoError.Prohibido`, sin mapear) → `70af035`: 4 rojos (2 Moq Expected Prohibido / Actual NoAutorizado, 2 HTTP Expected Forbidden / Actual Unauthorized) → `1a93f7e` |
| Login inválido sigue en 401 | `aa2e795`: caracterización, pasa a la primera; con `NoAutorizado` → 403 en el mapeo da rojo |

**Excepciones declaradas.** Estos tests pasaron a la primera. Cada uno se verificó con una mutación sin commitear, y todas dieron rojo:
- **PG.** Pasan a la primera porque los resuelve el caso de uso, o porque el stub no escribe.
  - `Modificar_CodigoDeOtroArticulo_...` y `Modificar_CodigoInternoIgualAlCodigoDeProveedorDeOtroArticulo_Conflicto`: con M2, `ObtenerConflictosCodigoAsync` sin la rama de códigos de proveedor.
  - `Modificar_OtraSesionLoDaDeBaja...`: con M1, el UPDATE sin `activo`.
- **HTTP.** Pasan a la primera porque los cubren los casos de uso ya hechos.
  - `Put_OperadorModifica...`: con H2, el controlador ignora los códigos.
  - `Put_OperadorCambia...` y `Delete_Operador...` (hoy `_403`, entonces `_401`): con H1, `EsAdministrador => true`.
  - `Put_CodigoDeOtroArticulo_409...`: con H3, sin el chequeo del caso de uso. El 409 sale del índice y no dice cuál artículo.
  - `Put_Inexistente_404`: con H4, inexistente → Invalido.
- **Otras mutaciones, todas en rojo:**
  - M3: UPDATE sin Id. Lo frena el índice único `articulo_codigo_interno_key` (23505), no la aserción "no toca otro artículo": un UPDATE sin Id sobre varias filas activas siempre choca con ese índice;
  - M4: retiro sin filtro de artículo;
  - M5: la reactivación borra motivo y fecha.

## Compuerta (subagentes de a uno, cada uno en su worktree)
| Pasada | Alcance | Veredicto | Qué |
|---|---|---|---|
| verificador | `0580a16..7bc47c4` | **APTO** | Orden test → producción limpio en las 4 unidades. Los 4 rojos se reproducen por aserción con los números declarados. 25 mutaciones (las 9 declaradas y 13 propias): ninguna sobrevive. Suite 424/424, 143 PG. No bloqueantes: el borde de 40 sin test (→ `a1f4143`); M3 lo frena el índice único, no la aserción (corregido el texto); el conflicto en carrera sale como excepción (409 por el manejador); 401 frente a 403 (deuda) |
| auditor | `0580a16..a1f4143` | **NO APTO** | **B1:** el código interno nuevo igual a un código de proveedor nuevo de la misma petición se guardaba en silencio. **B2:** el UPDATE reescribía el código interno leído y pisaba un cambio de otra sesión. Lo reprodujo contra PostgreSQL real. Suite 425/425. CHECK, índices, FK, identidad, relectura y seguridad: sin hallazgos |
| ronda 1 | `e060dbe` → `7858c26` | — | B1: Conflicto. B2: el código interno es `string?` en el puerto, y con null la columna queda fuera del SET. N1: el test de reactivación afirma su restricción |
| verificador | `a1f4143..7858c26` | **APTO** | 7 rojos reproducidos. 11 mutaciones muertas, entre ellas la de B2 y las dos ramas del repositorio. Suite 428/428, 145 PG. No bloqueantes N-a/N-b (la guarda B1 por un solo lado) → `acc8f9d` |
| auditor | `a1f4143..acc8f9d` | **APTO** | B1 y B2 cerrados contra el motor, con sus mutaciones en rojo. El SQL de la rama null no lleva `codigo_interno`, y `activo` sigue en los dos WHERE. Suite 430/430, 145 PG |
| verificador | `b4a1ff0..aa2e795` (403) | **APTO** | 4 rojos reproducidos por aserción. 6 mutaciones muertas: cada caso de uso de vuelta a `NoAutorizado`, `Prohibido` → 401, la línea del mapeo borrada (→ 400), `NoAutorizado` → 403, y un `NoAutorizado` de sesión vuelto `Prohibido`. Los 11 `NoAutorizado` restantes son de login o de sesión |
| auditor | `b4a1ff0..aa2e795` (403) | **APTO** | Los dos 403 releen con un contexto nuevo y no escriben. El chequeo de rol va antes de escribir y fuera de la unidad de trabajo: con la escritura antes del chequeo, los dos tests dan rojo. **Suite 431/431, 0 omitidos, 145 PG** |

## Deuda nueva
- La unicidad entre `codigo_interno` y los códigos de proveedor la sigue controlando solo la aplicación, ahora también en la modificación. Una carrera entre dos sesiones no la cierra el motor.
- Retirar un código borra la fila: no queda rastro de qué código tuvo el artículo.
- **Auditor N2:** `ReactivarArticuloUseCase` ignora el `bool` de `CerrarAsync`. Un inactivo sin una baja abierta en el historial, como un dato importado, se reactiva sin dejar rastro. Es el mismo patrón que cliente y proveedor.
- **Auditor N3:** los códigos de proveedor pueden empezar con `LC-` (`ValidarCodigos`), aunque el código interno no. Ya venía del alta.
- **Auditor N4:** la carrera entre dos sesiones por el par código interno / código de proveedor no la cierra el motor. Ahora que el código interno se puede poner a mano, es alcanzable con cualquier código. Cerrarla pide esquema (un trigger o una tabla única de códigos) o un lock: lo decide el equipo.
- **Auditor N5:** no hay test HTTP del 409 que sale de un 23505 en una carrera al modificar. El manejador tiene las dos restricciones en la lista blanca.
- El conflicto por carrera en `AgregarCodigosAsync` llega al caso de uso como `DbUpdateException`, no como `Result`. Por HTTP sale 409 con el mensaje genérico del índice (verificador).
- Esquema huérfano en `lacentral_test`: `pr110_test_5bef5661630743e4b6d09c409f11fafa`. Ya figuraba en la deuda de la Fase 6. Lo borra el equipo.
