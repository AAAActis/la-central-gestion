o# Instrucciones para Claude Code · 04/11/2026

> Pegá este archivo completo en Claude Code al arrancar la sesión.
> Reemplaza lo que diga la sección `PRÓXIMA SESIÓN` del `ESTADO.md` del Sprint 4,
> que quedó escrita el 28/10 y ya no aplica.

---

## 0 · Dónde estamos

- **Hoy es 04/11/2026.** El Sprint 4 cerró el 30/10. Estamos en el **Sprint 5 (2/11 al 13/11)**.
- **La demo a Sergio es el viernes 6/11.** Esa fecha ordena todo lo de abajo. No es el cierre
  del sprint: es a propósito, con una semana de margen, para que sus correcciones tengan dónde
  entrar.
- La máquina de desarrollo se reinstaló (CachyOS, shell **fish**, paquetes de Node con **pnpm**).
  El entorno ya está funcionando: **435 tests en verde, 2m24s**. No toques la configuración de
  conexión ni las variables de entorno: están resueltas.

---

## 0.1 · Lo primero: comprobar el estado REAL, no el declarado

**Antes de tocar una rama, verificá en qué quedó el Sprint 4 de verdad.** Este archivo y el
`ESTADO.md` describen lo que estaba escrito el 28/10, y ese archivo avisa de sí mismo en su
primera línea: *"Quedó desactualizado tres veces en el Sprint 3: la regla existe por eso"*.

Si arrancás asumiendo que un PR sigue abierto y en realidad entró, rebaseás sobre un árbol que
no es el que creés.

```bash
git fetch --all --prune
git branch -a --sort=-committerdate
git log --oneline develop -20
gh pr list --state all --limit 20
dotnet test 2>&1 | tail -3        # una sola vez, para fijar la línea de base
```

Y comparás eso contra lo que dice la tabla de la §3. **Donde no coincidan, manda Git.**

### Después, el cierre del Sprint 4

El Sprint 4 cerró por calendario el **30/10** y no está declarado. Escribilo en
`docs/sprint04/ESTADO.md` antes de empezar con los merges:

- qué entró y qué quedó sin mergear, con el motivo
- qué compuertas siguen abiertas (C1 a C6)
- los SP comprometidos contra los entregados, y el motivo de la diferencia
- los cinco merges pendientes declarados como **arrastre del Sprint 4 al Sprint 5**, no como
  trabajo nuevo del 5

**No inventes números.** Los SP entregados y las fechas salen del historial de Git y de los PR.
Si un dato no está en el repo, dejalo marcado como pendiente y avisá.

Esto no es trámite: sin el cierre escrito, el GR-01 queda con un sprint sin declarar, y la
velocidad medida —34 SP, 3,4 SP/día— deja de servir para planificar los que vienen.

---

## 1 · Política de tests · esto es lo que más cambia

**La regla: no corras las 435 pruebas en el bucle de trabajo.** Correrlas cada vez cuesta dos
minutos y medio y le pega a una netbook Celeron que además sirve a otros siete contenedores.

### Mientras trabajás en algo

```fish
dotnet test --filter "FullyQualifiedName~<NombreDeLaClase>" --no-build
```

El `~` es "contiene", así que sirve con el nombre de la clase o del namespace. `--no-build`
ahorra la compilación cuando ya compilaste.

### Al cerrar cada fase

Los tests de esa fase, y **una sola corrida completa al final de la fase**. Si la fase quedó en
verde, seguís con la siguiente sin volver a correr todo.

### Frontend

```fish
npx ng test --watch=false --browsers=ChromeHeadless
```

Solo si tocaste `frontend/`. Si tocaste solo backend, no la corras.

### Tarea concreta, y es la que más rinde

**Separar las pruebas que pegan contra PostgreSQL de las que no.** Hoy son 287 unitarias puras
y 148 contra base, y están mezcladas en la misma corrida.

1. Poné `[Trait("Base", "PostgreSQL")]` en todas las clases que usan `PostgreSqlPruebas`.
2. A partir de ahí quedan dos comandos:

```fish
dotnet test --filter "Base!=PostgreSQL"    # bucle rápido: segundos, cero carga a la Arch
dotnet test                                # completa, antes de abrir el PR
```

Eso hay que **versionarlo**, no dejarlo en la máquina de cada uno: Lauti trabaja en Windows
contra la misma Arch, y si los dos corren la suite completa al mismo tiempo el problema se
duplica.

### Paralelismo · cerrar la deuda D-27b en el mismo movimiento

El `ESTADO.md` ya diagnosticó esto y hay que implementarlo:

> *"El harness arma una cadena de conexión aislada por esquema de test, así que Npgsql crea
> **un pool por esquema**. La demanda real de conexiones es colecciones en paralelo × tamaño
> del pool."*

- Fijá `maxParallelThreads` en un `xunit.runner.json` **versionado**, con su entrada de
  `CopyToOutputDirectory` en el `.csproj`.
- Documentá en `LaCentral.Tests/README.md` la cuenta **hilos × pool ≤ CONNECTION LIMIT del
  rol**, para que el número deje de ser adivinado.
- **No subas el tamaño del pool para tapar un `53300`.** Es un parche y el propio ESTADO lo
  dice. El techo se pone en los hilos.

### End to end

**Uno solo, al cerrar un conjunto de fases, no uno por fase.** Un E2E por fase es el mismo
error que correr las 435: paga el costo completo para verificar un cambio chico.

---

## 2 · Lo que no se toca

- La configuración de conexión de la máquina nueva. Ya está resuelta.
- `docker compose down` **sin `-v`**. El volumen tiene los 124 proveedores y 2.108 clientes
  reales importados de Multisoft.
- Nada se escribe contra `lacentral`. Lecturas contra `lacentral_test`; escrituras de prueba
  contra `lacentral_demo`, cuando exista.
- `PostgreSqlSeguridadTests` no se desactiva ni se esquiva.
- Sin `dotnet ef migrations`. El esquema es Database First: SQL en `backend/db/`, y re-scaffold.

---

## 3 · Orden de trabajo hasta el viernes 6/11

### Primero · desatascar lo que está hecho y sin mergear

**Esto es arrastre del Sprint 4, no trabajo nuevo del 5**, y así se declara.

Según el `ESTADO.md` del 28/10 hay cinco cosas terminadas que no entraron, y tres están en
ramas encadenadas. **Verificá la tabla contra Git antes de usarla** (§0.1): puede haber
cambiado.

| Qué | Rama | Estado |
|---|---|---|
| B1 · HU-UI-01 estructura base | `feature/HU-UI-01-estructura-base` | PR #130, aprobada con correcciones ya aplicadas |
| HU-STK-01 existencias | `feature/HU-STK-01-existencias-ubicacion` | PR #131, compuerta APTO |
| Fase 1 · fundamentos visuales | `feature/hu-ui-fase1-fundamentos-visuales` | sin push |
| HU-UI-05 · 043 formulario | `feature/hu-ui-05-formulario-reutilizable` | sale de la Fase 1, sin push |
| HU-UI-06 · 044 confirmación | `feature/hu-ui-06-confirmacion-con-motivo` | sale de la 043, sin push |

Las tres últimas están encadenadas: **rebaseá en ese orden** sobre `develop` (Fase 1 primero,
después 043, después 044). Si rebaseás 044 antes que la Fase 1, arrastrás los commits de las
otras dos.

Dos Issues faltan y van con el milestone correspondiente: el de la **043** y el de la **044**.

### Segundo · la compuerta C3, que es el prerrequisito de la demo

**`lacentral_demo` con datos sembrados inventados:** 5 proveedores, unos 30 artículos con
varios códigos por artículo, y compras al mismo artículo a distinto precio y en distinta fecha
(sin eso el comparador no muestra nada interesante).

Está pendiente desde el Sprint 4 y el replan la fechó para la primera semana del Sprint 5,
antes del 7/11. **Sin esta base, la demo del viernes no tiene dónde correr**: no se demuestra
sobre `lacentral_test` —los tests crean y destruyen esquemas ahí— ni nunca sobre `lacentral`.

### Tercero · recién después, B2 y B3

B2 (042 · componente de listado) y B3 (050 · pantalla del comparador y vinculación). B3
además depende del endpoint de A2, que no existe.

---

## 4 · Los cambios de esquema van todos en una sola ventana

Hay **cuatro** cambios de esquema pendientes. Si entran de a uno, son cuatro re-scaffold y
cuatro rondas de conflictos en las cinco ramas abiertas de arriba.

| # | Qué | Estado |
|---|---|---|
| C1 | Modalidad **L2 (+40 %)** y retiro de **Tarjeta3 / Tarjeta6**. Toca el CHECK `articulo_precio_modalidad_valida` y el alta que inserta seis filas por artículo | Pendiente |
| C2 | Tabla **`proveedor_articulo`** con clave única `(proveedor_id, codigo_proveedor)`, `nombre_normalizado` indexado y `articulo_id` NULLABLE, más la tabla de vinculaciones aprendidas | Pendiente |
| C4 | **`marca` + `codigo_original`** con índice único parcial sobre (código original normalizado, marca). La columna no puede nacer NOT NULL por los datos existentes | Respondida por Sergio, **decisión del equipo pendiente** |
| — | **Normalización a 3FN** (N-3, N-5, N-6, N-7, N-9, N-11) + las columnas de reportes | Script escrito y probado, **pendiente de correr** |

**Aplicalos seguidos, un solo re-scaffold, y recién entonces compilás.** Antes del re-scaffold,
comparar la huella del esquema `public` de `lacentral_test` contra `lacentral`, como dice
`backend/db/rescaffold/README.md`.

**Conflicto de numeración a resolver primero:** el script de normalización llegó nombrado
`05_Normalizacion_3FN_y_Reportes.sql`, pero el `ESTADO.md` reserva el **05** para C1 y el **06**
para C2 (el replan del 27/10 los numera 06 y 07, así que los dos documentos tampoco coinciden
entre sí). Mirá qué hay en `backend/db/` y renumerá el de normalización al primer número libre.

**Arrastres del C1, que ya están identificados:** borrar o reapuntar
`RecalcularPreciosTests.TarjetasConTasasDePrueba_AplicanLaFormula` y
`CalcularPreciosArticuloUseCaseTests.PrecioJustoEnElLimiteDeSuColumna_InvalidoYNoEscribe`.

**Lo que rompe la normalización en el código, para saberlo antes de compilar:**

- `articulo.ubicacion_deposito` ya no existe → lo que la lea pasa a `stock`
- `cliente_direccion.provincia` es ahora `provincia_id` → JOIN para proyectar el nombre, y la
  pantalla pasa de campo de texto a combo
- `factura_compra_detalle` tiene clave de **tres** columnas → revisar los Find y los Update del
  repositorio que la buscaban por dos
- `cliente.usuario_alta_id` / `usuario_baja_id` no existen → el dato sale de
  `cliente_historial_baja`

---

## 5 · Una colisión de diseño que hay que resolver antes de HU-CMP-06 y 07

`articulo.codigo_barras` (del script de normalización) y la decisión **C4** (código original
del fabricante + marca) atacan **el mismo problema por dos caminos**: los dos sirven para
reconocer que dos proveedores venden la misma pieza.

- El **código original** es el número de parte del fabricante. Es el que Sergio nombró y el que
  el mandato declara como clave normalizada.
- El **código de barras** es el EAN. También viene del fabricante, pero no todos los repuestos
  lo traen.

No se contradicen y pueden convivir, pero hay que decidir **cuál es la clave de equivalencia y
cuál es auxiliar** antes de implementar la capa de vinculación, porque las dos alimentan el
nivel 1 de coincidencia del `METODO-VINCULACION.md`.

**Es decisión de negocio, no técnica. No la tomes vos:** quedó para que Santi la lleve a Sergio.

Y antes de construir los niveles 2 y 3 de coincidencia, está pendiente la **compuerta C6**: la
medición de una hora sobre 50 artículos reales normalizados, que define si hace falta el nivel 3
o alcanza con los dos primeros. Medir antes de construir.

---

## 6 · Lo que sigue abierto y no se decide solo

| Qué | Quién decide |
|---|---|
| La lista cerrada de `cliente.condicion_pago` | Sergio |
| Clave de equivalencia: código original o código de barras (§5) | Equipo, con Sergio |
| C4 completo: si el alta lleva campo de código original, y de dónde sale la marca de los existentes | Equipo |
| El largo del motivo: 200 en `DarDeBajaRequest` contra 255 en `BajaClienteRequest` y `BajaProveedorRequest`. Nadie decidió si son dos límites a propósito o uno mal replicado | Equipo |
| El campo en el `ProblemDetails` de `MapeoResultado.Falla`. Mientras no esté, el CA-004 de HU-UI-05 queda parcialmente verificado | Backend |
| Qué responde una reactivación de un inactivo sin baja abierta (dato importado): ¿error, o reactivar y abrir rastro? | Equipo, una línea |
| D-27a: el harness no verifica `current_database()` ni la versión del servidor | Deuda registrada |
| D-27c: el prefijo de esquema es `pr110_test_` con un número de PR viejo, y la limpieza no corre si el test falla | Deuda registrada |

**Dónde parás:** si hace falta tocar el esquema fuera de la ventana de la §4, si un CA
contradice a otro o al esquema, si falta una regla de negocio que nadie definió, si PostgreSQL
no está disponible —una prueba no ejecutada no es una prueba aprobada—, o si llevás más de tres
intentos en el mismo problema. En todos esos casos explicás qué encontraste, qué opciones ves y
qué recomendás. **No decidís vos.**

---

## 7 · La deuda que va primero, antes de cualquier HU

`RestablecerContrasenaUseCase.cs:43` ignora si el UPDATE escribió: un restablecimiento de
contraseña que informa éxito sin haber escrito nada. Es el único ítem de la deuda con
consecuencia de seguridad. Un test en rojo por el camino real, y después el arreglo.
