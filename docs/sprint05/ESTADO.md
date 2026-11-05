# ESTADO · Sprint 5

**Entregable:** al cerrar este sprint, una persona puede entrar al sistema con cualquiera de los dos roles y buscar artículos, ver en su ficha la existencia y la ubicación por sucursal y el historial de compras, darlos de alta, de baja con confirmación y reactivarlos, gestionar clientes y proveedores, y registrar una factura de compra, con montos y cantidades en formato argentino, sobre `lacentral_demo`.

> Única fuente de verdad del estado del sprint. Se actualiza **antes** de cada push, en el
> mismo PR que el código que describe.

**Sprint 5 · 02/11/2026 → 13/11/2026.** Demo a Sergio: **viernes 06/11/2026**.
Instrucciones de arranque: `docs/sprint05/21-INSTRUCCIONES-CLAUDE-CODE-04-11.md`.
Cierre del Sprint 4 y arrastre: `docs/sprint04/ESTADO.md` (PR #139).

---

## Entregable del Sprint 5

Es la línea del principio de este archivo (forma de la §1 de `CLAUDE.md`).

- **Fuera del Sprint 5:** el comparador (Sprint 6 y 7, ver `docs/sprint06/ESTADO.md`) y la
  factura por PDF (sin sprint).
- **El sprint no se cierra por fecha** (13/11 es una estimación) sino cuando se cumplen las dos:
  1. El entregable se recorrió a mano en el navegador contra `lacentral_demo`, y quedó anotado
     acá qué se probó y con qué datos (punto 10 de la Definición de Terminado). **Pendiente.**
  2. El dueño lo usó, o está agendado que lo use, y lo que dijo quedó anotado acá. **Pendiente:
     ni Santi ni el dueño probaron nunca el sistema.**
- **La demo del 06/11 corre sobre `develop` con el #141**, con el 07 y el 08 aplicados a
  `lacentral_demo` el 05/11 (S5-06 levantada, ver Decisiones). Guion: `GUION-DEMO.md`. **No
  muestra el historial de compras**: no hay pantalla ni endpoint que lo lea (HU-ART-06 pendiente).


## Tablero · Sprint 5 (milestone «Sprint 5», cierra el 13/11/2026)

El tablero y esta tabla son la misma verdad. Si uno cambia, el otro cambia en el mismo push.
Capacidad: ~18 SP, con tres personas (Santi y Lauti comparten la sesión y se turnan; Javi tiene
la suya).

| Issue | HU | SP | Asignado | Estado | Nota |
|---|---|---|---|---|---|
| #43 | 042 · HU-UI-04 · listado reutilizable | 5 | Javi | **Cerrado** (#145, 05/11) | Arrastre del Sprint 4 construido en el 5 |
| #148 | Formato es-AR de montos y cantidades | 1 | Javi | Abierto | Lo vio el ensayo de la demo |
| #147 | Paginación de clientes y proveedores | 2 | Santi | Abierto | 200 con página vacía; CA-003 v1.1 de HU-CLI-02 y de la HU de proveedores |
| #46 | 045 · HU-UI-07 · clientes y proveedores | 3 | Javi | Abierto | Lleva la baja con la confirmación de la 044 |
| #23 | 016 · HU-ART-06 · historial de compras | 5 | Santi | Abierto | CA-003 y CA-004 **PARCIAL**: dependen de la anulación (028, #9, backlog) |
| #21 | 014 · HU-ART-04 · baja lógica de artículo | 3 | Javi | Abierto | CA-001 hecho en HU-ART-03 (#122); faltan del CA-002 al CA-005 |

Abiertos: 14 SP. Orden de trabajo de esta sesión: #148, #147, #46, #23 y #21.

**Fuera del sprint y sin milestone (backlog), decisión del 05/11/2026:** #9 (028), #27 (020),
#33 (026), #53 (051), #54 (052), #26, #28, #29, #31 y #41. **Deuda, sin milestone:** #137.

---

## Decisiones (04/11/2026)

| # | Decisión | Motivo |
|---|---|---|
| S5-01 | **`lacentral_demo` se arma con el esquema de `develop`** (Parte A de `base_completa.sql` + 01 a 04). El 07 se le aplica **después**, junto con el código que lo usa. | Que la demo del viernes no dependa del re-scaffold. |
| S5-02 | **`stock.ubicacion_deposito` pasa de `VARCHAR(30)` a `VARCHAR(60)`** en las dos bases (Santi, con el superusuario). Versionado en `08_Ubicacion_Deposito_60.sql`. La validación de la API no cambia. | El 30 del 07 fue un número sin fundamento; la validación ya estaba en 60 y la columna de origen, `articulo.ubicacion_deposito`, era de 60. |
| S5-03 | **La ubicación sale del alta y de la modificación del artículo.** Se carga desde la ficha, **por sucursal**, igual que se ve la cantidad. | El estante se asigna cuando la mercadería llega; en el alta no hay existencia en ninguna sucursal. Copiarla a todas afirmaría que la pieza está en el mismo estante en las dos: la redundancia que N-9 eliminó. |
| S5-04 | **Numeración de scripts:** la normalización llegó como `05_…` y queda **07**. El 05 es `05_rol_tests.sql`; el **06 sigue reservado para C2**. | Primer número libre. |
| S5-06 | ~~**El 07 y el 08 NO se aplican a `lacentral_demo` antes de la demo.** Van el lunes 09/11, o antes si el #141 está mergeado y verificado — **nunca jueves ni viernes**.~~ **Levantada el 05/11/2026 (Santi)**: el 07 y el 08 se aplicaron ese jueves. | Su motivo era que la demo no dependiera de que el #141 entrara a tiempo; el #141 entró y la regla perdió su motivo. Sin el 07, `develop` daba 500 en la ficha contra la demo (`codigo_barras` no existe). |
| S5-07 | ~~**Hasta que Sergio pruebe el sistema no se construye nada nuevo** (05/11/2026, Santi): ni la paginación de clientes y proveedores ni la 045.~~ **Levantada el mismo 05/11** (Santi): con la demo andando, se arrancan las cinco HU del tablero. | La demo era lo único con fecha, y quedó lista. |
| S5-08 | **Tablero rehecho por entregable** (05/11/2026, Santi): al Sprint 5 entra lo que suma al entregable; el comparador va al 6 y al 7, sin dejarlo a medias; lo que depende de una compuerta abierta no entra; la deuda no lleva milestone. Sprint 6: 055, 053, 054 y 050 (opción a). Sprint 7: 030 y 032. La CMP-02 queda afuera (etapa 2, D16). | El tablero no se había actualizado desde el replan del 27/10. |
| S5-05 | **La comparación de esquemas `lacentral_test` ↔ `lacentral` la hace Santi** con el superusuario: el rol de desarrollo no tiene `CONNECT` sobre `lacentral` y no se le va a dar. 04/11: md5 de columnas idéntico. | Seguridad de los datos reales. |

---

## Fases

| Fase | Qué | Rama | PR | Estado |
|---|---|---|---|---|
| a · C3 | `lacentral_demo` creada (owner `lacentral_santi`), con Parte A + 01 a 04, y **sembrada** con `backend/db/demo/sembrar_demo.cs` contra la API de `develop` | `feature/c3-lacentral-demo` | ver abajo | **Hecha 04/11.** Esquema igual al de `develop` (huella comparada). 2 usuarios, 5 proveedores, 30 artículos, 90 códigos, 15 facturas, 180 líneas, 15 fechas. Login de `operador.demo` y búsqueda verificados por HTTP |
| b · re-scaffold | 07 versionado, re-scaffold, arreglos del modelo, ubicación por sucursal (backend y ficha) | `feature/normalizacion-3fn-rescaffold` | ver abajo | **Hecha 04/11.** `verificador-tdd`: ronda 1 NO APTO (arreglado), **ronda 2 APTO**. `auditor-persistencia`: **APTO**. Las dos sobre `0d41569` |
| c · tests | Los 7 del esquema y la corrida completa | idem | idem | **Hecha.** Ver «Tests» |
| d · 07 en demo | Aplicar 07 y 08 a `lacentral_demo` | — | — | **Hecha 05/11** (S5-06 levantada). 07: compuerta OK, cierre «TODO OK», PK de `factura_compra_detalle` de tres columnas, 24 provincias. 08: «VARCHAR(60). TODO OK» |

**C3 (compuerta del Sprint 4): cerrada el 04/11/2026.** La demo del viernes corre sobre
`lacentral_demo` con el esquema de `develop`. **No se toca hasta después de la demo.** La clave
de `admin.demo` y `operador.demo` está en `~/.config/lacentral/clave-demo.txt` (600), en la
máquina de Santi; no está en el repo.

### Script 08 · dónde está aplicado

| Base | 08 (`ubicacion_deposito` a 60) |
|---|---|
| `lacentral_test` | **Aplicado** a mano por Santi, 04/11 |
| `lacentral` | **Aplicado** a mano por Santi, 04/11 |
| `lacentral_demo` | **Aplicado** 05/11, después del 07 |

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

- **Backend: 458/458**, 0 omitidos, 0 warnings, 2 min 46 s, en una corrida completa sobre
  **`4f4e17e`**, el último commit con código del PR (436 − 5 eliminados + 27 nuevos). Este es el
  único número que vale; los anteriores quedan solo como rastro de dónde salió cada test.
- **Conciliación del conteo (04/11):**

  | Corrida | Backend | Frontend | Qué agregó el commit |
  |---|---|---|---|
  | mediodía, `a4179f4` (y `44df50a`, ronda 1 del verificador) | 456 | 94 | — |
  | `79606e8` (ronda 2 del verificador sobre `0d41569`, solo docs encima) | 457 | 95 | `b5e0ca5`: `HistorialBajaRepositorioTests`, dos bajas (M18). `79606e8`: un spec de `articulos.spec.ts`, error de la API (F2) |
  | **`4f4e17e`** | **458** | **95** | `5cb63b1`: `TraitBasePostgreSqlTests` (toda clase con `[PostgreSqlFact]` lleva el Trait) |

  Frontend: `frontend/` es idéntico entre `79606e8` y `4f4e17e` (mismo hash de árbol,
  `4fa0404`), así que no se volvió a correr (política de pruebas: solo si se tocó `frontend/`).
- **Separación por tipo (`[Trait("Base", "PostgreSQL")]`, `59c0327`):** 294 unitarias en
  0,7 s con `--filter "Base!=PostgreSQL"`, 164 contra PostgreSQL. Las 20 clases con
  `[PostgreSqlFact]` lo llevan. Una de ellas, `ArticuloRepositorioTests`, tiene además un
  `[Fact]` puro que queda del lado de PostgreSQL: se corre en la completa, no en el bucle
  rápido. Los dos comandos y la cuenta de D-27b, en `backend/LaCentral.Tests/README.md`.
  (El 287/148 de las instrucciones del 04/11 era sobre la línea de base de 435.)
- **Eliminados (5), con su motivo:**

  | Test | Motivo |
  |---|---|
  | `CrearArticuloUseCaseTests.UbicacionDeMasDe60_Invalido` | El alta ya no recibe ubicación (S5-03). El máximo de 60 lo prueba `AsignarUbicacionArticuloUseCaseTests.UbicacionDeMasDe60_Invalido` / `UbicacionDe60_Entra`. |
  | `CrearArticuloUseCaseTests.Alta_SinUbicacion_GuardaNull` | Sin el campo en el alta no hay caso. Que un artículo sin fila de stock dé ubicación null lo prueba `AsignarUbicacionPostgreSqlTests.Ficha_TraeLaUbicacionDeCadaSucursal`. |
  | `ModificarArticuloUseCaseTests.UbicacionDeMasDe60_Invalido` | La modificación ya no recibe ubicación (S5-03). Mismo reemplazo que el primero. |
  | `ModificarArticuloUseCaseTests.UbicacionEnBlanco_SeGuardaComoNull` | Idem. Lo reemplaza `AsignarUbicacionArticuloUseCaseTests.UbicacionEnBlanco_LaBorra` (null y blancos) y `AsignarUbicacionPostgreSqlTests.Null_BorraLaUbicacion`. |
  | `ModificarArticuloApiPostgreSqlTests.Get_Ficha_TraeLaUbicacion` | La ficha ya no tiene ubicación del artículo, sino por sucursal. Lo reemplaza `Put_Ubicacion_204YLaFichaLaMuestraEnEsaSucursal`. |

- Nuevos (27 = los 25 de abajo + `b5e0ca5` + `TraitBasePostgreSqlTests`): 9 del caso de uso de ubicación (Moq), 5 de su persistencia (PostgreSQL, releídos
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
- **Frontend: 95/95** (88 + 7, primer spec de la pantalla de artículos), sobre `79606e8`; árbol idéntico en `4f4e17e`. Solo Chromium.
- **`verificador-tdd` ronda 2: APTO** sobre `0d41569`. M18 y F2 mueren por aserción; 10
  mutaciones re-muestreadas, ninguna sobrevive; 457/457 y 95/95 reproducidos; los rojos
  declarados coinciden con los que reprodujo.
- **`auditor-persistencia`: APTO** sobre `0d41569`. Catálogo de `lacentral_test` leído en solo
  lectura (CHECK de baja intactos después del `DROP COLUMN` de N-11, `stock_pkey` respalda el
  `ON CONFLICT`, clave de tres columnas del detalle, 08 aplicado); sin Ids sembrados; todo
  relee con contexto nuevo; 46/46 filtrados contra PostgreSQL; 3 mutaciones detectadas.

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
- **D-S5-07 · restricciones del 07 sin test que las haga fallar** (`auditor-persistencia`):
  `articulo_codigo_barras_formato`, `factura_venta_medio_cobro_dominio`,
  `factura_venta_autorizacion_distinta`, `provincia_nombre_*` y el NOT NULL de `medio_cobro`.
  Hoy ningún código de producción escribe esas columnas: no hay camino real. Cuando aparezca el
  caso de uso que las escriba, test con SQLSTATE y nombre de restricción.
- **D-S5-08 · no se unifica la lectura de quién dio de baja:** el usuario lee la baja ABIERTA
  (reactivado → null) y el cliente la ÚLTIMA (rastro, D10). Es deliberado y lo fijan los tests.
- **D-S5-09 · sin test de concurrencia del upsert de ubicación** (la condición `activo` va
  dentro de la sentencia, pero no hay prueba con dos sesiones), ni test de API que afirme que
  OPERADOR recibe 204 en `PUT .../ubicaciones/{sucursalId}` (el rol es de toda la clase).
- Sigue de antes: D-27a, D-27c, el campo en el `ProblemDetails`, el largo del motivo (200/255),
  `RestablecerContrasenaUseCase.cs:43`.

---

## Orden de merge (04/11, para Javi)

1. **#141 primero.** Desbloquea `develop`: hasta que entre, toda rama nueva que toque backend
   hereda los 148 tests de PostgreSQL en rojo.
2. **Después #139, #140 y #142**, con `git merge origin/develop` en cada uno a medida que entren
   (no rebase: las cuatro ramas ya están pusheadas, regla 3 de `CLAUDE.md`).
   **#139 y #141 tocan los dos `frontend/`:** puede haber conflicto en `styles.css`.
3. **Con los cuatro adentro, UNA corrida completa sobre `develop`** (backend y frontend), y el
   número acá. Ese es el estado que hereda todo el mundo.

---

## PRÓXIMA SESIÓN

**05/11 · las cinco HU del Sprint 5, en este orden, una rama y un PR cada una:** #148 (es-AR),
#147 (paginación), #46 (045, con la baja y su confirmación), #23 (016) y #21 (014). El estado de
cada una está en la tabla «Tablero». Demo del 06/11 con `GUION-DEMO.md`.

0. **Lo primero después del merge de Javi: `docs/sprint05/COMO-PROBARLO.md` y el ensayo de la
   demo** de punta a punta por el frontend contra `lacentral_demo`. Ni Santi ni el dueño
   probaron nunca el sistema. Es la base de la condición 1 del entregable.
1. **Fase d (S5-06):** el lunes 09/11 (o con el #141 mergeado y verificado, nunca jueves ni
   viernes), 07 y después 08 a `lacentral_demo`. Desde ahí la demo corre solo con el código del
   #141: `develop` lee `articulo.ubicacion_deposito`, que el 07 borra.
2. **La 042 está hecha (#145); la 045 sigue en el tablero de arriba**, una HU por PR (regla 2 de `CLAUDE.md`).
3. Pendientes del 04/11, en este orden: `git merge develop` en la 044 (resolver
   `formulario.ts` sin perder `password` ni `autocompletar`); aviso flotante con foco devuelto y
   Escape.
