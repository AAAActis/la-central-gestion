# ESTADO · Sprint 4

> Única fuente de verdad del estado del sprint. Se actualiza **antes** de cada push, en el
> mismo PR que el código que describe. Quedó desactualizado tres veces en el Sprint 3: la regla
> existe por eso.

**Sprint 4 · 20/10/2026 → 31/10/2026.** Base: `main` con el Sprint 3 mergeado y etiquetado.
**Velocidad medida:** 34 SP (Sprint 3). **Comprometido:** 48 SP en tres pistas paralelas.

---

## El objetivo

**Que un vendedor escriba el nombre de un repuesto y vea, en una pantalla, lo que COSTÓ y lo que
CUESTA en cada proveedor, y cuánto hay en cada sucursal — aunque cada proveedor lo llame
distinto.**

Los dos precios, no uno. **Lo que costó** determina el margen de lo que ya está en el depósito:
un repuesto comprado a 100 que hoy cuesta 150 se vende distinto que uno comprado a 150. **Lo que
cuesta** determina a quién comprarle hoy: el proveedor más barato del mes pasado puede no serlo
hoy. Con uno solo, la mitad de la decisión queda sin respaldo.

El sistema actual no puede hacer esto de ninguna forma. No es una versión mejor de algo: es
algo que no existe. Es el problema número uno del DIAGNÓSTICO y el único de los diez objetivos
del Mandato v3.0 sin una línea construida.

**Lo nuevo frente al plan anterior:** se incorpora la capa de **vinculación entre el catálogo
del proveedor y el catálogo de La Central**, con búsqueda por aproximación. Sin esa capa, los
datos de los proveedores no se pueden cruzar con los 2644 artículos propios, y el comparador
muestra una lista vacía. Diseño completo en `docs/sprint04/METODO-VINCULACION.md`.

---

## Compuertas · se cierran antes de que arranque cualquier pista

| # | Qué | Quién | Estado |
|---|---|---|---|
| C1 | Script 05 · agregar modalidad **L2 (+40 %)** y retirar **Tarjeta3 / Tarjeta6**. Cambio de esquema: el CHECK `articulo_precio_modalidad_valida` lista los seis nombres actuales y el alta inserta seis filas por artículo. Script del equipo, aplicado a mano en `lacentral_test` y `lacentral`, re-scaffold después, comparando antes la huella md5 del esquema `public` de las dos bases. Arrastra: borrar o reapuntar `RecalcularPreciosTests.TarjetasConTasasDePrueba_AplicanLaFormula` y `CalcularPreciosArticuloUseCaseTests.PrecioJustoEnElLimiteDeSuColumna_InvalidoYNoEscribe`. | Santi | **Pendiente** |
| C2 | Script 06 · tabla **`proveedor_articulo`** con clave única `(proveedor_id, codigo_proveedor)`, columna `nombre_normalizado` indexada y `articulo_id` NULLABLE. Más la tabla de vinculaciones aprendidas. | Santi | **Pendiente** |
| C3 | Base **`lacentral_demo`** con datos sembrados inventados: 5 proveedores, unos 30 artículos con varios códigos por artículo y compras a distinto precio. Bloqueante para que Sergio pruebe algo. Hoy `lacentral_test.public` tiene 124 proveedores y 2108 clientes que parecen copia de datos reales, y `ValidarBase` solo mira el nombre. | Santi | **Pendiente** |
| C4 | **Decisión de negocio · el código interno.** En la Fase 3 se decidió que lo genera el sistema (`LC-{id:D6}`), y por eso el CA2 de HU-ART-01 quedó NO verificado. Pero la convención del backlog dice que el código interno es la clave única de artículos, y el objetivo 4 del mandato unifica los códigos de proveedor **bajo esa clave**. Pregunta a Sergio: *"cuando cargan un repuesto nuevo, ¿le ponen un código propio de ustedes o les sirve que el sistema le asigne uno?"* Define si el alta lleva campo, si la pantalla de artículos lo lleva, y si N4 se cierra solo. | Santi → Sergio | **Respondida por Sergio (19/10/2026), pendiente de decisión del equipo.** Ver «Respuestas de Sergio · 19/10/2026». |
| C5 | **Escribir HU-CMP-06 y HU-CMP-07** (IDs 053 y 054) en el formato de la proforma, y revisarlas antes de implementar. Son reglas de negocio, no tecnicismos. | Santi | **Pendiente** |
| C6 | **Medición previa, 1 hora.** Tomar 50 artículos reales con sus códigos en dos o tres proveedores, normalizarlos y contar cuántos coinciden exacto. Define si hace falta el nivel 3 de coincidencia o alcanza con los dos primeros. | Santi | **Pendiente** |

---

## Respuestas de Sergio · 19/10/2026 (vía Javi)

Registradas textuales. **No son decisiones**: las toma el equipo. No se numeran como D-nn.

**a. Código del artículo (responde la compuerta C4).** Todos los artículos tienen CÓDIGO
ORIGINAL (del fabricante) + código del proveedor. El original es la clave normalizada; los de
proveedor son alternativos. Si dos marcas venden el mismo código original, son DOS artículos: se
distinguen por marca. El código original se puede corregir después de cargado, y lo puede
corregir CUALQUIER empleado.

Datos existentes: el `codigo_interno` importado de Multisoft mezcla códigos de proveedor y
originales; no se puede migrar automáticamente, hay que cargar los originales.

Consecuencias (compuerta del equipo, **NO se implementan en la pista B**):
- contradice la Fase 3 (`LC-{id}` generado) y la regla de la Fase 7 (cambiar el código solo
  ADMINISTRADOR);
- cambio de esquema: marca + código original, columna que no puede nacer NOT NULL por los datos
  existentes (obligatoria en la aplicación para altas y modificaciones), índice único parcial
  (código original normalizado + marca), listado de artículos sin código original;
- cierra N4 en el motor para los que lo tengan;
- hace falta una HU de carga de códigos originales (y marca) de los existentes;
- reescribir CA2 de HU-ART-01 con este modelo.

Preguntas abiertas: cuántos artículos hay que completar y de dónde sale la marca de los
existentes.

**b. Listas de precios.** No hay Excel/CSV; los proveedores publican precios actualizados en su
web, salvo cuando no hay stock. HU-CMP-08 (055, pista C) no se resuelve con archivo como está
planteada: **avisar a Lauti y Santi**. Para B3: distinguir precio vigente, precio vencido
(RNF-014) y "sin precio: el proveedor no tiene stock".

---

## Las tres pistas

### PISTA A · Santi · la capa de vinculación — 21 SP

| # | HU | Historia | SP | Depende de |
|---|---|---|---|---|
| A1 | 016 · HU-ART-06 | Historial de compras del artículo | 5 | C1 |
| A2 | 053 · HU-CMP-06 | Búsqueda de candidatos por aproximación entre catálogos | 8 | C2 · C5 · C6 |
| A3 | 054 · HU-CMP-07 | Vinculación manual con aprendizaje | 8 | A2 |

**A3 es el corazón del sprint.** Al vincular, el sistema hace tres cosas en una sola operación:
escribe `articulo_id`, **agrega el código del proveedor a `articulo_codigo_alternativo`** —y
desde ahí es coincidencia exacta para siempre—, y registra la decisión. Tiene que poder
deshacerse, y la desvinculación saca también el código alternativo que agregó.

**Deuda que entra, porque es del mismo código:**
- `RestablecerContrasenaUseCase.cs:43` ignora si el UPDATE escribió. Un restablecimiento de
  contraseña que informa éxito sin haber escrito nada. **Va primero, antes de A1:** es el único
  ítem de la deuda con consecuencia de seguridad.
- `ReactivarUsuarioUseCase.cs:58` y los tres `Reactivar*UseCase` ignoran `CerrarAsync`. Antes
  hay que decidir qué responde una reactivación de un inactivo sin baja abierta —un dato
  importado—: ¿error, o reactivar y abrir un rastro? Decisión del equipo, una línea.

### PISTA B · Javi · el frontend, con la habilidad de diseño — 16 SP

| # | HU | Historia | SP | Depende de |
|---|---|---|---|---|
| B1 | 039 · HU-UI-01 | Estructura base y navegación | 3 | — |
| B2 | 042 · HU-UI-04 | Componente de listado reutilizable | 5 | B1 |
| B3 | 050 · HU-UI-12 | Pantalla del comparador y de vinculación | 8 | B2 · A2 |

**Leé la habilidad de diseño de frontend antes de escribir una línea de HTML.** El frontend hoy
son siete archivos planos sin rutas, sin componentes compartidos y sin tests. B1 y B2 existen
para que la pantalla del comparador no sea el octavo archivo plano.

**El criterio, que es requisito y no preferencia.** El sistema actual es *"una cosa llena de
botones por todos lados"*. Esto tiene que poder usarlo alguien sin que nadie le explique:

- Al entrar, **una sola cosa que hacer**: un campo de búsqueda.
- **Lo más barato primero**, con la diferencia en porcentaje contra el más barato, para no
  restar de cabeza.
- **Cada dato con su fecha.** Un costo de hace ocho meses y uno de ayer no se comparan igual.
- **La existencia por sucursal en la misma pantalla:** si hay stock, no hace falta comparar
  nada, y esa es la respuesta más rápida posible.
- **Los resultados agrupados por certeza:** coincidencia exacta arriba y separada, candidatos
  por aproximación abajo. El operador tiene que ver de un vistazo qué es seguro.
- **Vincular en un clic**, con el candidato más probable preseleccionado y deshacer disponible.
  Si vincular cuesta cinco clics, nadie vincula y el sistema no aprende nunca.

Esta pantalla es **el entregable del sprint**: es la que se le muestra a Sergio.

#### Avance de la pista B

| # | Estado | Rama | PR | Tests | PG |
|---|---|---|---|---|---|
| B1 | **Mergeada en develop (#130, 20/10/2026).** Aprobada con correcciones por Lauti; correcciones aplicadas antes del merge. Actualizada con develop por merge (trae #131). verificador-tdd: ronda 1 NO APTO (3 mutaciones sobrevivientes), ronda 2 **APTO**; correcciones (merge y título, ronda 3 acotada): **APTO**. auditor-persistencia: no aplica (no toca backend). | `feature/HU-UI-01-estructura-base` | #130 · Refs #40; cuerpo en `docs/sprint04/pr-b1-hu-ui-01.md` | frontend 21/21 (antes no había) · backend 435/435 después del merge | 0 nuevos · 148 |
| B2 | Pendiente. Desbloqueada: B1 está en develop desde el 20/10. | — | — | — | — |
| B3 | Pendiente. Depende de B2 y del endpoint de A2. | — | — | — | — |

**CA de HU-UI-01:** CA1 verificado · CA2 · NO verificado como está escrito. Motivo: no existe
pantalla de gestión de usuarios en frontend/, así que no hay opción real que ocultar. Lo que SÍ
está verificado es el mecanismo de filtrado por rol: un módulo exclusivo de ADMINISTRADOR,
definido solo en el test, aparece para el administrador, no aparece para el operador, y si el
operador escribe la ruta queda en su primer módulo (acceso.spec.ts). El CA queda pendiente de
verificación hasta que exista esa pantalla. · CA3 verificado · CA4 verificado (test a 1024, 768 y 480 px más prueba manual en
Chrome a 1024×768 y 1366×768, sin backend). DoD 9: `docs/manual/navegacion.md`. DoD 10: **NO
verificada**, porque C3 (`lacentral_demo`) está pendiente.

**Decisiones de B1 (19/10/2026, agente; las revisa Javi en el PR):**
- `ng test` con Karma + Jasmine, el stack que instala `ng new` en Angular 20. Sin otras
  librerías.
- El rol del menú sale de `SesionActual.rol`, que el login devuelve junto con el token. No se
  decodifica el JWT en el navegador.
- Los módulos se definen en `frontend/src/app/modulos.ts`; de ahí salen el menú y las rutas. Sin
  sesión, cualquier ruta muestra el login.

**Deuda nueva de B1:**
- F5 pierde la sesión (`frontend/src/app/nucleo/sesion.ts`, solo en memoria). Decidir dónde
  guardar el token es una decisión de seguridad del equipo.
- El `flex-wrap` del `nav` (`frontend/src/styles.css`, regla `nav`) no tiene test que lo
  discrimine mientras haya solo dos módulos.
- **npm bloquea install scripts** (20/10/2026, revisión de #130, N4). `npm ci` en `frontend/`
  bloquea los install scripts de @parcel/watcher, esbuild, lmdb y msgpackr-extract. No hay
  `.npmrc` en el repo ni en el usuario; `ignore-scripts` es `false`. Es el comportamiento por
  defecto de npm 12.0.2 (`allow-scripts = [""]`, `dangerously-allow-all-scripts = false`).
  **Ya pasaba antes de B1:** `npm ci` sobre c0c6b86 bloquea los mismos 4. El build y los tests
  andan igual. No se cambió nada; si hace falta, se aprueban con
  `npm install-scripts approve <paquete>` (decisión del equipo).
- La tabla de stock de HU-STK-01 (fila Total, negativos en rojo) no tiene test de frontend:
  ningún spec monta la ficha de artículo.

### PISTA C · Lauti · de dónde sale «lo que cuesta» — 11 SP

| # | HU | Historia | SP | Depende de |
|---|---|---|---|---|
| C1 | 017 · HU-STK-01 | Consulta de existencias por ubicación | 3 | — |
| C2 | **055 · HU-CMP-08** | **Incorporación de la lista de precios de un proveedor desde archivo** | 5 | C2 (esquema) |
| C3 | 032 · HU-CMP-03 | Vista Compra del comparador: costo pagado contra precio vigente | 3 | A1 · C2 |

**C1 arranca sin depender de nadie**, y es la historia correcta para entrar al proyecto: dos
criterios de aceptación, uno de ellos es *"un artículo sin movimientos muestra cantidad cero,
no vacío"*, y se prueba contra PostgreSQL real en veinte minutos.

**Las tres HU del comparador hay que reescribirlas antes de implementarlas**, porque están
redactadas sobre la consulta en vivo a los sitios y este sprint trabaja sobre datos propios:

- **HU-CMP-01:** el CA2 pasa de "su precio" a **el último costo registrado con su fecha**,
  tomado del historial de compras. Los CA4, CA5 y CA6 se mantienen tal cual.
- **HU-CMP-03:** en vez de precio de lista y bonificado publicados por el proveedor, **el último
  costo de cada proveedor con su fecha** y la diferencia porcentual contra el más barato. El CA4
  se reemplaza por *"proveedor sin compras registradas: se muestra igual, indicando que no hay
  costo registrado"*, que es el caso real y el más frecuente.

Van como **v1.1 de cada proforma**, con el motivo en el control de cambios. No se sobreescribe
la v1.0.

### PISTA D · documentación · se reparte — sin SP, pero es ruta crítica de la tesis

| Qué | Quién |
|---|---|
| El listado de los **14 Requerimientos Funcionales**. No existe en ningún artefacto y las 52 HU y los 8 CU lo referencian. Media página. | Santi |
| Corregir el **RF de los 7 CU** que colisionan. Manda la numeración del archivo de HU. | Santi |
| Reescribir **CA2 y CA6 de HU-ART-01** y **CA4 de HU-ART-03**: quedaron desactualizados por el código interno generado y los márgenes globales. **Cierra #17.** | Javi con Sergio |
| Pasar las 5 HU construidas de **E1 a E7** en las proformas, con fecha, responsable y evidencia, y sus CA de "Pendiente" a "Pasa" con el nombre del test. Las 52 proformas están congeladas en E1 con tres sprints cerrados; el informe de cierre ya tiene los 82 nombres de test: es copiar. | Lauti |
| Diagramas de secuencia de **CU-001, CU-003 y CU-007**. Faltan los tres en DISEÑO. | Lauti |

---

## Definición de Terminado · los tres agregados de este sprint

Los siete de siempre, más:

**8 · Todos los criterios de aceptación, verificados o declarados.** Ninguno queda sin mención.
Un CA que no se puede verificar va como **NO verificado con su motivo**. Un CA que no aplica como
está escrito **se reescribe en la proforma en la misma fase**, con su versión y su motivo.

**9 · El uso, documentado.** Toda HU con pantalla cierra con una nota de cinco a diez líneas en
`docs/manual/<pantalla>.md`: qué se hace, en qué orden, y qué pasa cuando algo sale mal. Es la
materia prima del objetivo 3 del Mandato (manuales de software por rol). Al cierre de cada HU
cuesta diez minutos; al final del proyecto, una semana.

**10 · El entregable se prueba a mano en el navegador contra `lacentral_demo`**, y queda
registrado qué se probó y con qué datos.

**Regla nueva, de la retro del Sprint 3:** de los 13 NO APTO de los subagentes, nueve fueron
mutaciones sobrevivientes en bordes numéricos. **Toda guarda con un límite numérico lleva dos
tests: el último valor que entra y el primero que no.** Se escriben juntos, cuestan lo mismo, y
ahorran una ronda de verificación por fase.

---

## Orden y acoples

```
C1 · C2 · C3 ──┬── A1 historial ────────┬── C2(Lauti) CMP-01 ── C3(Lauti) Vista Compra
C4 (Sergio)    │                        │
C5 · C6        └── A2 candidatos ── A3 vinculación
                        │
               B1 ── B2 ┴─── B3 pantalla comparador + vinculación
```

**Dos puntos de encuentro:** Javi no cierra B3 sin el endpoint de A2; Lauti no cierra C2 sin A1.
Los dos se avisan **en cuanto A1 y A2 estén mergeados**, no al cierre.

**48 SP contra una velocidad medida de 34.** Lo digo de frente: sostiene si las tres pistas
corren de verdad en paralelo. Si no, **lo que tiene que entrar es A1 + A2 + A3 + B1 + B2 + B3**:
la vinculación funcionando con su pantalla. Todo lo demás es negociable; eso no.

---

## Lo que NO entra

| Qué | Por qué |
|---|---|
| **HU-CMP-02 · consulta en vivo a los sitios** (13 SP, riesgo Alto) | Credenciales de La Central guardadas, un integrador por proveedor, dependencia de cinco sitios ajenos. Se declara como **etapa 2 del objetivo 4**, Sprint 7 u 8, con **un** proveedor como prueba de concepto. Antes, la medición de cuánto tarda un login más una búsqueda. |
| HU-STK-04 · transferencias (8 SP) | Sale por las dos HU nuevas de vinculación. Sprint 5. |
| HU-CMP-04 · Vista Venta (3 SP) | Sprint 5. |
| HU-UI-08 artículos | Espera C4: dibujarla antes es rehacerla. (HU-UI-05, el formulario, **sí entra**: se adelantó por el replan del 27/10 y está en curso desde el 28/10.) |
| HU-019 · descuento de stock por venta | Depende de la factura de venta, Sprint 6. Hacerlo sin la venta es código sin usuario. |
| HU nueva de precio manual / CasoEspecial | La escriben Javi y Sergio; arrastra tres problemas de concurrencia. Sprint 5. |
| **N4** · carrera código interno / código de proveedor | Depende de C4: si el código interno deja de ser editable a mano, la carrera se cierra sola. **Se decide acá, se implementa en el siguiente.** |
| Historial de códigos retirados | Esquema. El equipo decide primero si hace falta. |

---

## Decisiones confirmadas que entran a este sprint

| # | Decisión | Fecha | Fuente |
|---|---|---|---|
| D12 | Sin modalidades de venta en tres y seis pagos: venden en cuotas pero el dueño prefiere que no estén en el sistema. | 16/10/2026 | Sergio, vía Javi |
| D13 | Tres listas globales, iguales para todos los artículos: **L1 +50 %**, **L2 +40 %** (nueva), **L3 0 % al costo** (consumo propio de los dueños, reemplaza al 0,01 %). | 16/10/2026 | Sergio, vía Javi |
| D14 | Efectivo = **L1 −10 %**, también cuando la fila de Lista es manual. | 17/10/2026 | Sergio, vía Javi |
| D15 | Baja y reactivación de artículo: **todos los roles**, Administrador y Operador. | 17/10/2026 | Sergio, vía Javi |
| D16 | El comparador, **etapa 1 sobre datos propios**. La ingesta automatizada es etapa 2, con un proveedor como prueba de concepto. | 18/10/2026 | Equipo |
| D17 | **Corrección del protocolo:** "las compuertas se cierran antes de que arranque cualquier pista" fue un error de redacción. Cada compuerta bloquea historias concretas: **C1** (script L2 / retiro de tarjetas) bloquea HU-CMP-03 y los tests de tarjetas; **C2** (`proveedor_articulo`) bloquea HU-CMP-06, HU-CMP-07 y HU-CMP-08; **C3** (`lacentral_demo`) bloquea solo el punto 10 de la DoD. | 19/10/2026 | Equipo |
| D18 | **Arrancar HU-STK-01 (Pista C · C1) antes del re-scaffold global fue autorizado por el equipo**, con el riesgo aceptado: cuando entren los scripts de C1 y C2, el re-scaffold puede conflictuar con esta rama en el contexto; se rebasea. HU-STK-01 lee `stock`, que existe desde el Sprint 3, y no toca `articulo_precio_modalidad` ni `proveedor_articulo`. | 19/10/2026 | Equipo |
| D19 | HU-STK-01: la existencia se muestra en la **ficha del artículo** (`GET /api/articulos/{id}`), sin endpoint nuevo: los CA dicen "cuando el vendedor abre su detalle". La ficha trae **todas** las sucursales (cero sin fila en `stock`) y `totalExistencias`. | 19/10/2026 | Agente, dentro del alcance de los CA |

---

## Issues de GitHub

| Acción | Detalle |
|---|---|
| Milestone Sprint 4 | Fecha techo 31/10/2026 |
| Ya movidos | #15 (tests de ART-02 CA2 y CA5) · #17 (reescritura de CA4) · #22 (L2, retiro de tarjetas, CasoEspecial) |
| Nuevos, uno por HU | 016, 017, 030, 032, 039, 042, 050, **053**, **054** — con ID numérico y código en el título |
| Nuevos, deuda | `RestablecerContrasena` sin verificar escritura · `CerrarAsync` ignorado en los cuatro `Reactivar*` · C2 esquema `proveedor_articulo` · C3 `lacentral_demo` |
| Limpieza | Borrar las 17 ramas locales viejas ya mergeadas y el esquema huérfano `pr110_test_5bef5661630743e4b6d09c409f11fafa` de `lacentral_test` |

---

## Fases del sprint

| Fase | HU | Estado | Rama | PR | Tests (PG) |
|---|---|---|---|---|---|
| Pista C · C1 | 017 · HU-STK-01 | **Mergeada en develop (#131, 20/10/2026).** Compuerta APTO (verificador y auditor). Cuerpo en `docs/sprint04/pr-hu-stk-01.md` | `feature/HU-STK-01-existencias-ubicacion` | #131 · Refs #24 | 435 (148) |
| Pista B · B1 | 039 · HU-UI-01 | **Mergeada en develop (#130, 20/10/2026).** Aprobada con correcciones (Lauti), aplicadas antes del merge. CA2 NO verificado como está escrito | `feature/HU-UI-01-estructura-base` | #130 · Refs #40 | backend 435 (148) · frontend 21 |
| Pista C · HU 041 + Fase 2 | 041 · HU-UI-03 | **PR abierto contra develop (29/10/2026).** CA-001 a CA-004 pasan. Acceso NO verificado contra 01-acceso.png (no está en el repo) | `feature/hu-ui-03-control-de-sesion` (de `develop`) | #138 · cuerpo en `docs/sprint04/pr-hu-ui-03.md` | frontend 83 (48 + 35) · backend 436, 148 sin enlace |

**Deuda nueva, de entorno (19/10/2026):** con un rol personal de PostgreSQL con tope de
conexiones (`lacentral_lauti`), `dotnet test` en paralelo da **140 fallos falsos**
(`53300: too many connections for role`). En serie, `develop` da 431/431. Mientras el equipo
no suba el tope o no use `lacentral_tests`, en esa máquina la suite se corre con
`dotnet test -- xUnit.ParallelizeTestCollections=false` (≈36 min).

**Deuda nueva registrada el 27/10** (fuente: `docs/sprint04/REPLAN-SPRINT4-v2.md`; registrada,
no implementada). Las dos salieron de la noche de infraestructura y las dos son de la misma
familia: el harness confía en lo que está escrito en la configuración en vez de medir lo que
pasa de verdad.

**D-27a · El harness no verifica la identidad del motor.**
`PostgreSqlPruebas` valida que el *nombre* de la base sea `test_*` o `*_test`, pero
no comprueba qué motor atendió la conexión. Esta noche un reenvío de red mandó las
pruebas a la base de otro proyecto, y Postgres respondió `28P01 password
authentication failed` en lugar de `28000 role does not exist` —por diseño, para no
revelar qué usuarios existen—, con lo que el síntoma pareció una credencial vencida
durante cuarenta minutos. Falta una verificación al inicio de la corrida:
`current_database()`, versión del servidor, y fallar si no coinciden con lo esperado.

**D-27b · `Maximum Pool Size` no es un techo total.**
El harness arma una cadena de conexión aislada por esquema de test, así que Npgsql
crea **un pool por esquema**. La demanda real de conexiones es *colecciones en
paralelo × tamaño del pool*, y por eso revienta el `CONNECTION LIMIT` del rol con
`53300`. Subir el límite del rol es un parche. Corresponde acotar `maxParallelThreads`
en la configuración de xUnit y documentar en el README la cuenta
*hilos × pool ≤ límite del rol*, para que el número deje de ser adivinado.

**Decisión de backend pendiente (28/10/2026) · el campo en el ProblemDetails.**
`MapeoResultado.Falla` tiene que agregar a qué campo corresponde el error, como miembro de
extensión del ProblemDetails, para `Conflicto` e `Invalido`. Solo el backend sabe qué regla
se disparó.

Mientras no esté, el CA-004 de HU-UI-05 se declara en dos partes: **verificado** para los
errores de validación de modelo (`errors` por propiedad) y **NO verificado**, con motivo, para
los de regla de negocio, que hoy llegan como mensaje general del formulario.

Cuando se agregue, el componente de formulario no cambia: cambia lo que cada pantalla le pasa
en el mapa campo → mensaje. Descartados:
- traducir el texto del error a un campo en el frontend, porque deduce reglas de negocio de una
  prosa (§1.7);
- reescribir el CA, porque debilita el criterio para acomodarlo a una limitación de hoy.
**D-27b · CERRADA y confirmada (28/10/2026).** Rama `fix/d27-entorno-de-pruebas`.
`xunit.runner.json` fija `maxParallelThreads = 5` y `parallelAlgorithm = conservative`, y el
README de Tests documenta la cuenta **T × (1 + P) + m ≤ L**. Para L = 25, P = 3 y m = 5, da T = 5.
La validó la medición directa del pico en `pg_stat_activity`:

| T | P | Techo | Pico | L | Resultado |
|---|---|---|---|---|---|
| 16 | 8 | 144 | 17 | 60 | 435/435 |
| 5 | 3 | 20 | 8 | 60 | 435/435, 3 min |
| 5 | 3 | 20 | 9 | 25 | abortada, 0 `53300` |
| 5 | 3 | 20 | 4 | 25 | 135 fallos de red, 0 `53300` |
| 5 | 3 | 20 | 8 | 25 | **436/436, 2 min 57 s · 0 `53300`** |

**Confirmación con L = 25 (28/10/2026):** con el enlace sano y el rol en 25, `dotnet test`
completo dio **436/436, 0 `53300`**, pico de 8 conexiones. La cota T × (1 + P) + m ≤ L deja de
ser una recomendación: está demostrada. El rol de Santi queda en 25.

**Deuda · falso verde con P = 1 (28/10/2026), resuelta en la misma rama.** Con
`Maximum Pool Size=1`, `DosAltasSimultaneas_CodigosInternosDistintos` y
`DosBajasAbiertasSimultaneas_...` pasaban en serie: no probaban la concurrencia que declaran.
Ahora el fixture exige P ≥ 2 antes de conectarse (`PostgreSqlPoolTests`, con mutación verificada).

**Decisión para el Sprint 5 · PostgreSQL local por desarrollador (propuesta, 28/10/2026).**
Hoy la suite depende de un enlace de red compartido. Cuando ese enlace se degrada, 148 tests
fallan por causas que no están en el código, y esos fallos se confunden con regresiones. Además,
los tres roles comparten el `max_connections = 100` de un solo motor, lo que obliga a racionar el
`CONNECTION LIMIT`. Con un Postgres local, cada desarrollador tiene su motor: no hay red de por
medio, la cuenta de conexiones es solo suya, y la base con datos reales queda fuera del camino de
los tests. Costo: mantener la plantilla local al día con los scripts de `backend/db/`. D-27a
(verificar la identidad del motor) se vuelve más necesaria con varios motores.

**D-27c · menor.** El prefijo de esquema de test es `pr110_test_`, con el número de
un PR viejo hardcodeado. Debería ser `lacentral_test_`, y la limpieza debería correr
en el `DisposeAsync` aunque el test falle — hay un esquema huérfano de agosto que lo
prueba.

---

## Rediseño visual · Fase 1 · fundamentos (28/10/2026)

Rama `feature/hu-ui-fase1-fundamentos-visuales`, PR #135. Cuerpo del PR en `docs/sprint04/pr-fase1-ui.md`.
Material: `docs/sprint04/ADENDA-REDISENO-UI.md`, que manda sobre el prompt.

**Hecho:**
- Tokens de `SISTEMA-VISUAL.md` §2 a §4 y §9 en `frontend/src/styles.css`.
- Tres tokens nuevos: `--peligro`, `--peligro-osc` y `--error-texto`. La §2, la §7 y la §8 del documento ya los nombran.
- Fuentes OFL autoalojadas en `public/fonts/`.
- Marca provisoria en `diseno/marca.ts`. Todavía no está montada: la monta la Fase 2.
- Clases globales de base.
- Botones con los seis requisitos de la §7.
- `prefers-reduced-motion`.
- Botón en carga, modal y aviso flotante en `compartido/`.

**Tests:** frontend 35/35 (21 previos + 14 nuevos). El backend no se tocó y no se corrió: el
enlace a la base está degradado (ver D-27b).

**Evidencia:**
- 9 combinaciones medidas por CDP: 1024, 768 y 480 px × acceso, artículos y factura. En ninguna hay desplazamiento horizontal.
- Capturas y estados de botón en `docs/sprint04/evidencia/fase1/`.
- La capa del botón tarda 0,3 s, y 0 s con movimiento reducido.

**Decisiones (28/10/2026):**
- `--dur-giro` 800 ms.
- `--dur-aviso` 5 s para éxito, información y alerta. **El error no se va solo**: queda hasta
  que el usuario lo cierra.
- Detrás del modal va el velo, no el panal. La ambigüedad de la §5 queda marcada en el documento.
- Cinco tokens sin nombre entran a la §2: `--borde-oscuro`, `--velo`, `--fondo-hex-respaldo`,
  `--grosor-foco` y `--dur-giro`.
- **Un `button` sin clase es secundario**; el primario se declara con `.primario`. Hasta las
  Fases 3 y 4, artículos, factura y acceso no tienen primario.
- Regla nueva en `CLAUDE.md`: un spec no afirma valores de color. Se reescribió CA1 en ese sentido.

**Fuentes, medidas** (`evidencia/fase1/mediciones-cdp.txt`):
- Para las 3 familias y sus 8 pesos y estilos, `document.fonts.load()` da `loaded` y
  `document.fonts.check()` da `true`.
- Un chip de código de ejemplo resuelve a JetBrains Mono.
- Control negativo: una cara que apunta a un archivo inexistente da `error` y `check() = false`.
- Los archivos llegan a `dist/`.

**La suite de frontend corre solo en Chromium.** Lo que depende del motor no está cubierto en
Firefox ni en Safari: por ejemplo, que `<dialog>` devuelva el foco al cerrarse, que hoy es una
caracterización.

**Esquemas huérfanos en `lacentral_test`: borrados el 28/10/2026, con aprobación.**
- `pr110_test_5bef5661630743e4b6d09c409f11fafa` (26 tablas, de agosto).
- `pr110_test_7d2e734c4b0345a686683e52b70f6c44` (26 tablas, de una corrida de D-27b abortada
  durante la caída del enlace).

Cómo se borraron: un `DROP SCHEMA … CASCADE` por nombre exacto, sin comodín ni bucle. Justo
antes de cada uno se volvió a consultar el esquema y se confirmó que no tenía sesión activa
(`application_name`). Después, la consulta no devuelve ninguno.

**Lo que demuestra el de agosto:** sobrevivió dos meses. No es un residuo: es la evidencia de
**D-27c**, la limpieza no corre cuando el test falla. El DROP manual lo sacó; mientras no se
arregle el `DisposeAsync`, vuelve a pasar.

**Aviso flotante (28/10/2026): un aviso nuevo reemplaza al anterior, no se suma.** Está en la §8
y en el componente, con dos tests de caracterización verificados por mutación. **Avisar a
Lauti** antes de que lo use en el acceso:
- el error no se va solo;
- reintentar no apila carteles;
- el componente tiene un solo lugar, así que un aviso de otro tipo también reemplaza al que
  estaba.

**NO verificado / pendiente:**
- Comparación con el prototipo y las 13 referencias, incluida la forma de la marca: Javi
  todavía no las subió.
- `public/fondo-hex.jpg`: la imagen no está. Hoy queda el color de respaldo.

## HU-UI-05 · 043 · formulario reutilizable (28/10/2026)

Rama `feature/hu-ui-05-formulario-reutilizable`, PR #134, con base en la rama de la Fase 1
(#135): no se rebasea, GitHub reapunta el PR cuando la Fase 1 entre. Cuerpo del PR en
`docs/sprint04/pr-hu-ui-05.md`.

- Componente en `compartido/formulario/`.
- Frontend 48/48.
- CA-001, CA-002 y CA-003: verificados.
- CA-004, en dos partes (28/10, revisión de Javi: «parcialmente verificado» no es un estado de
  la DoD): **verificado** para la validación de modelo; **NO verificado** para las reglas de
  negocio, con motivo: el ProblemDetails no dice el campo. Espera la decisión de backend del
  campo en el ProblemDetails. Igual en la proforma (v1.2) y en el cuerpo del PR.
- HU-UI-04 sigue en curso (Javi).
- Ninguna pantalla lo usa todavía.
- **Issue de la 043: pendiente de creación.** No existe: los Issues creados uno por HU son 016,
  017, 030, 032, 039, 042, 050, 053 y 054, y la 043 estaba fuera del sprint hasta el replan
  del 27/10. Al crearlo, va con el milestone Sprint 4.
- **Viene un `select`.** El formulario no tiene campos de selección porque ningún CA los pide.
  Cuando una pantalla lo necesite (probablemente la 045, clientes y proveedores), tiene que llegar
  con su criterio de aceptación. «Qué pasa si la lista está vacía» y «qué pasa si el valor
  guardado ya no está en la lista» son preguntas de negocio, no de interfaz.
**Ramas y bases de los PR (28/10/2026, revisión de Javi).** Los commits 792d183 (proformas de
las 52 HU y replan) y 7f92eb2 (sistema visual, prompt y adenda) se commitearon en el `develop`
local y las tres ramas salieron de ahí: cada PR los revisaba de nuevo. Pasan a su propio PR,
`docs/proformas-y-sistema-visual`, y las bases quedan:

| Rama | PR | Base |
|---|---|---|
| `docs/proformas-y-sistema-visual` | nuevo | `develop` |
| `fix/d27-entorno-de-pruebas` | #133 | `docs/proformas-y-sistema-visual` |
| `feature/hu-ui-fase1-fundamentos-visuales` | #135 | `docs/proformas-y-sistema-visual` |
| `feature/hu-ui-05-formulario-reutilizable` | #134 | `feature/hu-ui-fase1-fundamentos-visuales` |

No hizo falta reescribir historia: las tres ramas ya estaban encima de 7f92eb2, cuyo padre es
`origin/develop`. Orden de merge: docs → #133 y #135 (cualquiera primero) → #134. GitHub reapunta
cada PR cuando su base se mergea. **El `develop` local se realineó con `origin/develop`:** nada se
commitea en `develop`, todo entra por PR.

## HU-UI-03 · 041 + Fase 2 · control de sesión, marco y acceso (29/10/2026)

Rama `feature/hu-ui-03-control-de-sesion`, sale de `develop` (6a4a942): la Fase 1 (#135) y la
HU-UI-05 (#134) ya están ahí. Push aprobado el 29/10/2026; **PR #138** contra `develop`, con el cuerpo en
`docs/sprint04/pr-hu-ui-03.md`.

**Hecho:**
- **Sesión en `sessionStorage`** (decisión de la adenda §5): sobrevive a F5 en la misma pestaña
  y muere al cerrarla. Salir la borra. Un valor ilegible cuenta como sin sesión. El token no se
  decodifica en el navegador: el rol es el que devuelve el login (B1).
- **CA-002:** un 401 con el token vigente cierra la sesión y el acceso dice «Tu sesión venció.
  Volvé a entrar.». Un 403 no la cierra (la sesión vale, el rol no alcanza). Un 401 tardío de un
  token anterior no cierra la sesión nueva.
- **Marco:** `diseno/marca.ts` montada por primera vez (compacta en el encabezado, completa en el
  acceso), filete rojo bajo el encabezado grafito, usuario y rol a la derecha con Salir (`oscuro`),
  pie en `--grafito-2`. El activo conserva la raya blanca y `aria-current="page"`.
- **Acceso:** tarjeta partida (formulario | panel de marca) sobre `.fondo-hex`. Usa
  `compartido/formulario/`: es su primer consumidor. Ingresar es el único `.primario`, con estado
  de carga. Los errores van al aviso flotante de error, un objeto
  `{ texto }` por intento. Un 401 dice siempre «Usuario o contraseña incorrectos.», aunque el
  servidor dijera de más. Al abrirse, el foco queda en Usuario.
- **Extensión de `compartido/formulario/` (componente ajeno, recién mergeado).** Decisión del
  29/10/2026: va en este PR y no en uno aparte, por calendario (el sprint cierra el 30/10). Se
  agregó `'password'` a `CampoFormulario.tipo` y `autocompletar` (el atributo `autocomplete`).
  Motivo: el componente no soportó su primer uso real. Sin `password`, la contraseña se ve en
  claro. Sin `username` y `current-password`, los gestores de contraseñas no completan el acceso
  y el operador escribe a mano todos los días. Lo revisa Javi sabiendo que está.
- **`button.primario` en `styles.css`: hueco de la Fase 1 que este PR cierra.** La clase se
  declaraba (formulario, acceso) pero ninguna regla la pintaba, y no la hubo en ninguna rama. Sale
  de la §7: relleno `--rojo`, texto `--blanco`, `--rojo-osc` al apoyar el cursor. La capa, el foco,
  la carga y el deshabilitado son los de la base. Se levantó, con motivo, la restricción de no
  tocar `styles.css`: es un defecto en código mergeado, no trabajo en paralelo.
- Manual: `docs/manual/acceso.md` (DoD 9) y `navegacion.md` corregido (F5 y la raya blanca).
- Proforma 041 en E4 con los CA marcados.

**CA (proforma `docs/historias/041-HU-UI-03.md`, leídos de ahí):**
- **CA-001** pasa. Es caracterización: ya se cumplía desde B1.
- **CA-002** pasa. La expiración se detecta por el 401 de la API.
- **CA-003** pasa, también caracterización. El ingreso se muestra en la misma dirección; al
  entrar, el usuario queda en la pantalla que pidió.
- **CA-004** pasa. En Chrome real, `history.back()` después de Salir muestra el acceso.

**Tests:** frontend **83/83** (48 previos + 35 nuevos). Backend: **436 en total; 288 verdes y 148
en rojo por `Timeout during connection attempt` contra 100.125.169.91:5432** (enlace de
Tailscale). No hubo `53300`. Son exactamente los 148 de PostgreSQL, y esta rama no toca backend.

**Mutaciones:** 35 distintas sobre producción: `sesion.ts`, `login.ts`, `app.ts`, `formulario.ts`
y la regla `button.primario`. Todas dejan al menos un test en rojo. Dos sobrevivieron
después de migrar el acceso al formulario (la contraseña como texto, sin `autocompletar`): el
formulario lo probaba en general, pero no que la pantalla lo pidiera. Se agregó ese test y las dos
mueren. El bug de `#usuario` (abajo), reintroducido tal cual, deja en rojo solo el test del cuerpo
del login.

**Bug encontrado por la evidencia, no por los tests:** la referencia `#usuario` tapaba a la
propiedad `usuario` del `ngModel`, y el campo mostraba `[object HTMLInputElement]`. Ningún test
afirmaba qué credenciales se envían. Ciclo rojo → arreglo en `d14a0bd`.

**Evidencia** (`docs/sprint04/evidencia/hu-ui-03/`, script `capturas-cdp.mjs`, salida en
`mediciones-cdp.txt`):
- 12 capturas a 1024, 768 y 480 px (acceso, acceso con error, artículos, factura) y la sesión vencida.
- Sin desplazamiento horizontal en ninguna.
- F5 en Chrome real conserva la ruta y la sesión; `localStorage` queda con 0 claves.
- Teclado: Tab en el marco recorre Artículos → Factura de compra → Salir. Enter en Salir lleva al
  acceso con el foco en Usuario. Enter con la contraseña mala deja el foco en Contraseña.
- Movimiento reducido: la capa de Ingresar pasa de 0,3 s a 0 s y el aviso de `subir` a `none`.
- `grep` de hexadecimales fuera de `styles.css`: ninguno.

**NO verificado:**
- **Acceso y marco contra su referencia.** `docs/diseno/referencias/` y `docs/diseno/prototipo/`
  no están en el repo. Sin la referencia no se sabe si el filete va arriba o abajo, ni el texto del
  pie, ni el título del formulario. Quedaron: filete abajo, pie «La Central Repuestos», título
  «Acceso al sistema».
- **Fondo hexagonal.** `public/fondo-hex.jpg` no está. `.fondo-hex` apunta al archivo y se ve el
  respaldo `--fondo-hex-respaldo` (#2b2b2e).
- **DoD 10** (contra `lacentral_demo`): C3 sigue pendiente. El recorrido en el navegador fue con
  la API interceptada (401/200 en el login, 503 o 401 en el resto), sin backend.
- **Solo Chromium.**

**Referencias y fondo:** los sube Javi. Se le pidieron el 29/10/2026, porque bloquean tres
declaraciones de esta HU: acceso contra 01-acceso.png, marco contra su referencia y fondo
hexagonal. Se le ofreció hacer la recompresión de las cuatro capturas pesadas que tiene
pendiente el PR #132.

**HU-UI-06 · pendiente (29/10/2026).** No está en develop: `confirmacion-motivo.ts` vive solo en
`origin/feature/hu-ui-06-confirmacion-con-motivo`, y no se tocó. Esa rama modifica `formulario.ts`
(`maximo`, `coincideCon`, `varianteEnviar`, slot de acciones) y este PR también (`password`,
`autocompletar`). Cuando entre, necesita un merge con lo de acá.

**Deuda · `compartido/aviso-flotante.ts`, foco y Escape (29/10/2026).** Medido en Chrome:
- al cerrar el aviso con teclado, el foco cae en `body`;
- Escape no lo cierra.

No viola la especificación: la §8 exige Escape para el modal y no dice nada del aviso. El hueco
está en el documento antes que en el código. Un aviso de error no se va solo y no se cierra con
teclado, así que el usuario de teclado queda con un cartel del que solo sale con el mouse.
- **Deuda del componente:** al cerrarse, devolver el foco al elemento que lo originó, y cerrar con
  Escape.
- **Propuesta de agregado a la §8** (fila «Aviso flotante»; la aprueba Javi antes de implementarla):
  «Escape cierra el aviso que tiene el foco o, si ninguno lo tiene, el de error a la vista. Al
  cerrarse, el foco vuelve al elemento que lo originó; si ya no existe, al primer control de la
  pantalla, nunca a `body`.»

**Observaciones sin decidir:**
1. **Grosor del filete y del panel de marca.** Se usó `--grosor-foco` (3 px), como ya hace la raya
   del activo en `styles.css`. Si el filete tiene otro grosor, hace falta un token.
2. **`button.peligro` no pone el texto en blanco.** La §7 dice «`--peligro`, texto blanco», y la
   regla solo pone el relleno, así que el texto queda en `--tinta` sobre rojo oscuro. Es otro hueco
   de la Fase 1 y está fuera del alcance de este PR: no se tocó.
3. **`node_modules` sin Karma.** `ng test` fallaba con `Cannot find module 'karma-jasmine'`. Se
   arregló con `npm ci` (el lock no cambió). `npm` sigue bloqueando los 4 install scripts de N4.

**Deuda registrada · insumo del RNF de seguridad (29/10/2026).**
- **Decisión:** token en `sessionStorage`.
- **Alternativa rigurosa:** cookie `httpOnly` + `Secure` + `SameSite`, con un endpoint de refresco
  y protección CSRF (token de doble envío o encabezado propio verificado por la API).
- **Por qué es más segura:** un token en `sessionStorage` lo lee cualquier script de la página, así
  que una XSS lo roba. En una cookie `httpOnly`, el script no lo ve.
- **Por qué se postergó:** exige tocar el backend (emitir la cookie, el endpoint de refresco, la
  validación CSRF y la configuración de CORS). Esta semana el objetivo es la demo, y el sistema es
  de uso interno.
- **Riesgo aceptado mientras tanto:** una pestaña abierta en un puesto sin vigilar sigue operando
  hasta que el token vence (12 h, `GeneradorToken.cs`). El manual pide salir al dejar el puesto.

## PRÓXIMA SESIÓN

**Arrancá leyendo esto y `docs/PROMPT-FASE.md`.** Después:

1. Las compuertas bloquean historias concretas, no pistas enteras (D17). Confirmá que la de
   **tu** HU está cerrada; si no lo está, **PARÁ**.
2. Corré `dotnet build --no-incremental` y `dotnet test` desde `backend/`. Con HU-STK-01
   mergeada tiene que dar **435 tests, 148 contra PostgreSQL real, 0 omitidos, 0 warnings**
   (436 con D-27 mergeada: suma `PostgreSqlPoolTests`, que no se conecta)
   (sin ella, 431 y 145). Si da cientos de fallos `53300`, es el tope de conexiones del rol:
   corré en serie (ver la deuda de entorno arriba). Si no coincide por otra causa, **PARÁ y
   averiguá por qué** antes de tocar nada.
3. Tu primera unidad de trabajo es **`RestablecerContrasenaUseCase.cs:43`**, no una HU: el
   UPDATE no verifica que escribió y el caso de uso informa éxito igual. Un test en rojo por el
   camino real, y después el arreglo.
4. Recién entonces, la HU que te toque según tu pista.

**Pista B (Javi), aparte de los puntos 1 a 3, que son de la pista A:** B1 está mergeada en
develop (#130, 20/10/2026). Con B1
y HU-STK-01 en develop, los números son **435 tests y 148 contra PostgreSQL**, más la suite de
frontend (`ng test`, 21 tests). La siguiente es **B2 · 042 · HU-UI-04 · componente
de listado reutilizable**. Sale de develop, que ya tiene B1.
Va en `frontend/src/app/compartido/` y se prueba con
`ng test` (ver `frontend/src/app/README.md`). Antes de B2, leé la habilidad de diseño y los
criterios de la PISTA B. Avisar a Lauti y a Santi de la respuesta (b) de Sergio: HU-CMP-08 no se
resuelve con archivo.

**HU-UI-03 (041):** PR #138 abierto contra `develop` (`docs/sprint04/pr-hu-ui-03.md`), en revisión.
Faltan las referencias y el fondo de Javi para cerrar las tres declaraciones NO verificadas. Si
entra HU-UI-06, va a necesitar un merge sobre `formulario.ts`.

**Dónde parás de verdad:** si hace falta tocar el esquema, si un CA contradice a otro o al
esquema, si falta una regla de negocio que nadie definió, si PostgreSQL no está disponible —una
prueba no ejecutada NO es una prueba aprobada—, o si llevás más de tres intentos en el mismo
problema. En todos esos casos explicás qué encontraste, qué opciones ves y qué recomendás. **No
decidís vos.**
