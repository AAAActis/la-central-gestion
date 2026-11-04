# ESTADO · Sprint 5

> Única fuente de verdad del estado del sprint. Se actualiza **antes** de cada push, en el
> mismo PR que el código que describe.

**Sprint 5 · 02/11/2026 → 13/11/2026.** Demo a Sergio: **viernes 06/11/2026**.
Instrucciones de arranque: `docs/sprint05/21-INSTRUCCIONES-CLAUDE-CODE-04-11.md`.
Cierre del Sprint 4 y arrastre: `docs/sprint04/ESTADO.md` (PR #139).

---

## Decisiones (04/11/2026)

| # | Decisión | Motivo |
|---|---|---|
| S5-01 | **`lacentral_demo` se arma con el esquema de `develop`** (Parte A de `base_completa.sql` + 01 a 04). El 07 se le aplica **después**, junto con el código que lo usa. | Que la demo del viernes no dependa del re-scaffold. |
| S5-02 | **`stock.ubicacion_deposito` pasa de `VARCHAR(30)` a `VARCHAR(60)`** en las dos bases (Santi, con el superusuario). Versionado en `08_Ubicacion_Deposito_60.sql`. La validación de la API no cambia. | El 30 del 07 fue un número sin fundamento; la validación ya estaba en 60 y la columna de origen, `articulo.ubicacion_deposito`, era de 60. |
| S5-03 | **La ubicación sale del alta y de la modificación del artículo.** Se carga desde la ficha, **por sucursal**, igual que se ve la cantidad. | El estante se asigna cuando la mercadería llega; en el alta no hay existencia en ninguna sucursal. Copiarla a todas afirmaría que la pieza está en el mismo estante en las dos: la redundancia que N-9 eliminó. |
| S5-04 | **Numeración de scripts:** la normalización llegó como `05_…` y queda **07**. El 05 es `05_rol_tests.sql`; el **06 sigue reservado para C2**. | Primer número libre. |
| S5-06 | **El 07 y el 08 NO se aplican a `lacentral_demo` antes de la demo.** Van el lunes 09/11, o antes si el #141 está mergeado y verificado — **nunca jueves ni viernes**. | La demo no puede depender de que el #141 entre a tiempo. |
| S5-05 | **La comparación de esquemas `lacentral_test` ↔ `lacentral` la hace Santi** con el superusuario: el rol de desarrollo no tiene `CONNECT` sobre `lacentral` y no se le va a dar. 04/11: md5 de columnas idéntico. | Seguridad de los datos reales. |

---

## Fases

| Fase | Qué | Rama | PR | Estado |
|---|---|---|---|---|
| a · C3 | `lacentral_demo` creada (owner `lacentral_santi`), con Parte A + 01 a 04, y **sembrada** con `backend/db/demo/sembrar_demo.cs` contra la API de `develop` | `feature/c3-lacentral-demo` | ver abajo | **Hecha 04/11.** Esquema igual al de `develop` (huella comparada). 2 usuarios, 5 proveedores, 30 artículos, 90 códigos, 15 facturas, 180 líneas, 15 fechas. Login de `operador.demo` y búsqueda verificados por HTTP |
| b · re-scaffold | 07 versionado, re-scaffold, arreglos del modelo, ubicación por sucursal (backend y ficha) | `feature/normalizacion-3fn-rescaffold` | ver abajo | **Hecha 04/11.** `verificador-tdd` ronda 1 NO APTO, arreglado; ronda 2 y `auditor-persistencia` pendientes |
| c · tests | Los 7 del esquema y la corrida completa | idem | idem | **Hecha.** Ver «Tests» |
| d · 07 en demo | Aplicar 07 y 08 a `lacentral_demo` | — | — | **Postergada (S5-06):** lunes 09/11 o con el #141 mergeado y verificado; nunca jueves ni viernes |

**C3 (compuerta del Sprint 4): cerrada el 04/11/2026.** La demo del viernes corre sobre
`lacentral_demo` con el esquema de `develop`. **No se toca hasta después de la demo.** La clave
de `admin.demo` y `operador.demo` está en `~/.config/lacentral/clave-demo.txt` (600), en la
máquina de Santi; no está en el repo.

### Script 08 · dónde está aplicado

| Base | 08 (`ubicacion_deposito` a 60) |
|---|---|
| `lacentral_test` | **Aplicado** a mano por Santi, 04/11 |
| `lacentral` | **Aplicado** a mano por Santi, 04/11 |
| `lacentral_demo` | **Pendiente**, después del 07 (S5-06) |

Es idempotente: si la columna ya es de 60 no hace el `ALTER`. Probado en `lacentral_test` en
una sesión de solo lectura: «ya era VARCHAR(60); sin cambios».

### Qué cambió con el modelo nuevo (fase b)

- **`usuario_baja_id` de `cliente` y `usuario` (N-11):** sale de `*_historial_baja`. El cliente
  lee su **última** baja (reactivado la conserva, como motivo y fecha, D10); el usuario, la
  **abierta** (su reactivación la limpia). Sin cambio de comportamiento. `usuario_alta_id`
  salió de las entidades: nadie lo leía.
- **`provincia_id` (N-5):** hoy ningún código la usa (las direcciones se mapean solo con
  `Calle`), así que no hubo JOIN que agregar. **Las pantallas de clientes y proveedores (HU 045)
  lo van a necesitar:** un JOIN a `provincia` para proyectar el nombre, y un combo en lugar de
  un campo de texto. Ver D-S5-02.
- **`factura_compra_detalle` con clave de tres columnas:** el repositorio solo inserta, no
  busca por clave. La guarda del caso de uso decía «una línea por artículo: es la clave
  primaria»; pasa a **(artículo, sucursal)**. El test `ArticuloRepetidoEnDosLineas_Invalido`
  afirmaba la regla vieja con dos sucursales distintas: pasó a la misma sucursal. La regla
  nueva la cubre `VeinteDelMismoArticulo_DiezParaCadaSucursal_EntraYElStockQuedaRepartido`:
  el caso de uso real contra PostgreSQL, 20 bujías del mismo código, 10 a cada sucursal; entra y
  el stock queda en 13 y 10. Con la guarda vieja (mutación), ese test y el de Moq dan rojo.
- **Ubicación (S5-03):** `PUT /api/articulos/{id}/ubicaciones/{sucursalId}` (ADMINISTRADOR y
  OPERADOR, máximo 60, vacío borra; sin fila de stock la crea en cero; dado de baja → 400). La
  ficha trae la ubicación en cada existencia; en la pantalla, la tabla de stock tiene un campo y
  un Guardar por sucursal.
- **`factura_venta.medio_cobro` NOT NULL:** rompía el dato de un test de restricciones únicas.
  Producción no escribe facturas de venta todavía: **la HU de ventas tiene que pedir el medio de
  cobro.**

### Tests

- **Backend: 457/457**, 0 omitidos, 0 warnings, 2 min 59 s, en una corrida completa sobre
  `79606e8` (436 − 5 eliminados + 26 nuevos). Antes: 456/456 sobre `a4179f4` (y sobre
  `44df50a`, en la corrida del `verificador-tdd`); una corrida anterior dio 455/456 por el
  `medio_cobro` (arreglado en `7acfe72`).
- **Eliminados (5), con su motivo:**

  | Test | Motivo |
  |---|---|
  | `CrearArticuloUseCaseTests.UbicacionDeMasDe60_Invalido` | El alta ya no recibe ubicación (S5-03). El máximo de 60 lo prueba `AsignarUbicacionArticuloUseCaseTests.UbicacionDeMasDe60_Invalido` / `UbicacionDe60_Entra`. |
  | `CrearArticuloUseCaseTests.Alta_SinUbicacion_GuardaNull` | Sin el campo en el alta no hay caso. Que un artículo sin fila de stock dé ubicación null lo prueba `AsignarUbicacionPostgreSqlTests.Ficha_TraeLaUbicacionDeCadaSucursal`. |
  | `ModificarArticuloUseCaseTests.UbicacionDeMasDe60_Invalido` | La modificación ya no recibe ubicación (S5-03). Mismo reemplazo que el primero. |
  | `ModificarArticuloUseCaseTests.UbicacionEnBlanco_SeGuardaComoNull` | Idem. Lo reemplaza `AsignarUbicacionArticuloUseCaseTests.UbicacionEnBlanco_LaBorra` (null y blancos) y `AsignarUbicacionPostgreSqlTests.Null_BorraLaUbicacion`. |
  | `ModificarArticuloApiPostgreSqlTests.Get_Ficha_TraeLaUbicacion` | La ficha ya no tiene ubicación del artículo, sino por sucursal. Lo reemplaza `Put_Ubicacion_204YLaFichaLaMuestraEnEsaSucursal`. |

- Nuevos (25): 9 del caso de uso de ubicación (Moq), 5 de su persistencia (PostgreSQL, releídos
  con contexto nuevo), 2 HTTP, 2 de la compra con el mismo artículo en dos sucursales (Moq y el
  caso de negocio de las 20 bujías, contra PostgreSQL), y **los 7 del esquema 07** en `EsquemaNormalizacionPostgreSqlTests`: cada FALLA
  verifica el código SQL y el nombre de la restricción.
- **Rojo antes de producción, en commits separados (`f2bb5a9`, `694f53c`), pero NO todo por
  aserción** (`verificador-tdd`, 04/11):
  - de los 17 rojos de `f2bb5a9`, **13 fallaban por `NotImplementedException`** de los stubs de
    `0307800` (los 9 de `AsignarUbicacionArticuloUseCaseTests` y 4 de
    `AsignarUbicacionPostgreSqlTests`); solo 4 por aserción. El mensaje del commit dice «por la
    razón correcta» y para esos 13 no lo es;
  - de los 6 de `694f53c`, 3 fallaban por aserción y 3 por `TypeError` (el input no existía);
  - lo compensan las mutaciones: todas las de producción del caso de uso, el repositorio, la
    ficha y la pantalla ponen en rojo al menos un test (M1–M17, F1, F3, F4).
- **N-11 entró en `0307800` junto con la adaptación de los tests**: el orden test → producción
  no se puede ver en git. Lo cubren las mutaciones M15–M17 y, desde `b5e0ca5`, M18.
- **Ronda 1 de `verificador-tdd`: NO APTO** por dos mutaciones que sobrevivían. Arreglado con
  tests, sin tocar producción: `b5e0ca5` (dos bajas del cliente: el detalle dice quién dio la
  última; M18 lo pone en rojo) y `79606e8` (la ficha muestra el error de la API al guardar la
  ubicación; F2 lo pone en rojo).
- Los 7 del esquema nacieron en verde: el 07 ya estaba aplicado. No se mutaron (exigiría
  cambiar el esquema de la base de pruebas).
- **Frontend: 95/95** (88 + 7, primer spec de la pantalla de artículos). Solo Chromium.
- `verificador-tdd`: ronda 1 NO APTO (arriba); ronda 2 pendiente. `auditor-persistencia`:
  pendiente, después del verificador.

---

## Deuda

- **D-S5-01 · `medio_cobro`:** la HU de ventas tiene que pedirlo; la columna es NOT NULL.
- **D-S5-02 · `provincia_id`:** sin JOIN ni combo. La HU 045 (clientes y proveedores) los
  necesita: JOIN para proyectar el nombre y combo en vez de texto.
- **D-S5-03 · costo con dos líneas del mismo artículo:** si llegan a precios distintos, el
  costo vigente queda en el de la última línea. No hay regla de negocio que diga otra cosa.
- **D-S5-04 · entorno:** `dotnet-ef` necesita `DOTNET_ROOT=$HOME/.dotnet` y `~/.dotnet/tools`
  en el PATH. No hay `psql` en la máquina. `CHROME_BIN=/usr/bin/chromium`.
- **D-S5-05 · el grafo de `graphify-out/` describe la rama de la 044, no el esquema nuevo.**
  Se armó el 04/11 desde el árbol principal, que está en
  `feature/hu-ui-06-confirmacion-con-motivo`: no ve el re-scaffold, la ubicación por sucursal
  ni el 08. Una pregunta al grafo sobre el esquema nuevo contesta mal. **Reindexar desde
  `develop` cuando el #141 y la 044 estén mergeados**, ya con `graphifyy[sql]` para que entren
  los scripts de `backend/db/` (Database First: son la fuente de verdad del esquema).
- **D-S5-06 · siete documentos «leídos a una línea del final» en la extracción del 04/11.**
  Los subagentes de graphify reportaron N−1 líneas en `auditor-persistencia.md`, `README.md`,
  los README de `Controllers/` y `Dtos/`, `entorno-local.md`, `PLAN_SPRINT3.md` y
  `pr-fase1-deuda-bloqueante.md`. Si es un error de conteo sistemático, se pierde la última
  línea de cada documento, que en un `.md` puede ser una conclusión. **Verificar en la
  reindexación desde `develop`** (D-S5-05): comparar el conteo real de líneas de uno de esos
  archivos con lo que el grafo dice que leyó. No antes. Ese grafo de la 044 no se grabó: queda
  el `graph.json` anterior (decisión del 04/11).
- Sigue de antes: D-27a, D-27c, el campo en el `ProblemDetails`, el largo del motivo (200/255),
  `RestablecerContrasenaUseCase.cs:43`.

---

## PRÓXIMA SESIÓN

1. **Fase d (S5-06):** el lunes 09/11 (o con el #141 mergeado y verificado, nunca jueves ni
   viernes), 07 y después 08 a `lacentral_demo`. Desde ahí la demo corre solo con el código del
   #141: `develop` lee `articulo.ubicacion_deposito`, que el 07 borra.
2. Veredicto de `verificador-tdd` y `auditor-persistencia` sobre la fase b.
3. Pendientes del 04/11, en este orden: `git merge develop` en la 044 (resolver
   `formulario.ts` sin perder `password` ni `autocompletar`); aviso flotante con foco devuelto y
   Escape.
