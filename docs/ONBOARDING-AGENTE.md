# La Central · Guía para trabajar el proyecto con Claude Code

Para **Javi** y **Lauti**. Leela entera una vez antes de tocar nada. Después usala de
referencia.

Este archivo describe cómo se trabaja este proyecto. **No describe qué está hecho**:
eso vive en `docs/sprint03/ESTADO.md`, que es la fuente de verdad del estado y se
actualiza al cerrar cada fase.

---

# 1 · El proyecto en diez líneas

Sistema de gestión para una casa de repuestos. Tesis de la carrera.

- **Backend:** C# · .NET 10 · ASP.NET Core · EF Core 10 · xUnit · Moq · JWT/BCrypt.
- **Base:** PostgreSQL 18, en un contenedor accesible por Tailscale.
- **Frontend:** Angular, en `frontend/`.
- **Arquitectura:** Core-Driven, tres proyectos. Ver la sección 5.
- **Método:** Scrum, 8 sprints de 2 semanas. Git Flow. TDD estricto.
- **Datos reales:** la base principal tiene 124 proveedores y 2108 clientes importados
  de Multisoft. Leé la sección 12 antes de tocar una base.

El trabajo lo ejecuta Claude Code siguiendo el plan de `docs/sprint03/PLAN_SPRINT3.md`,
fase por fase, y se verifica solo con dos subagentes. Vos sos quien decide, revisa y
desbloquea.

---

# 2 · Setup de tu máquina, paso a paso

## 2.1 · El repo

```powershell
git clone https://github.com/AAAActis/la-central-gestion.git
cd la-central-gestion
git switch develop
```

**Siempre trabajás desde la raíz del repo**, no desde `backend/`. La configuración de
Claude Code (`.claude/`) y `CLAUDE.md` viven en la raíz, y se leen del directorio donde
arrancás la sesión. Si arrancás desde `backend/`, no cargan los agentes ni las reglas.

## 2.2 · .NET

Necesitás el SDK de .NET 10. Verificá:

```powershell
dotnet --version
```

Y `dotnet-ef` en la misma versión que EF Core del proyecto (hoy 10.0.11). Solo hace
falta si vas a re-scaffoldear; ver `backend/db/rescaffold/README.md`.

## 2.3 · La conexión a la base de pruebas — OBLIGATORIO

**Sin esta variable, más de 70 tests te dan en rojo.** No es tu entorno: desde el
Sprint 3, los tests marcados `[PostgreSqlFact]` **fallan** en vez de saltearse cuando
no hay conexión. Antes se omitían, la suite mostraba "todo verde" sin haber probado la
persistencia, y así se escaparon dos bugs críticos.

Pedile a Santi la cadena **por privado**. No está en el repo y no va a estar.

```powershell
[Environment]::SetEnvironmentVariable("LACENTRAL_TEST_PG", 'LA-CADENA-QUE-TE-PASEN', "User")
```

**Comilla simple**, no doble: si la contraseña tiene un `$`, PowerShell se come un
pedazo dentro de comillas dobles y después pasás una hora con un error `28P01` que
parece del servidor.

Cerrá la terminal, abrí una nueva, y verificá:

```powershell
$env:LACENTRAL_TEST_PG.Length
```

Más detalle en `docs/entorno-local.md`.

## 2.4 · Claude Code

Instalalo según la documentación oficial de Anthropic. Después:

```powershell
cd <la-raiz-del-repo>
claude
```

Al iniciar, chequeá tres cosas:

1. `pwd` termina en la raíz del repo, **sin** `backend`.
2. Los subagentes `verificador-tdd` y `auditor-persistencia` aparecen como tipo
   disponible. Si da `Agent type 'verificador-tdd' not found`, arrancaste del
   directorio equivocado.
3. `CLAUDE.md` se cargó.

**Los agentes y las reglas se leen AL INICIAR la sesión.** Si cambiás algo de
`.claude/`, tenés que salir con `exit` y volver a entrar. Esto ya nos costó una sesión
entera.

## 2.5 · graphify — opcional, recomendado

Arma un grafo del código y lo consulta sin leer archivos enteros. Baja bastante el
consumo de tokens en sesiones largas. **No es obligatorio**: el proyecto funciona sin
él.

Si lo querés, está documentado en `docs/entorno-local.md`, sección 2. Tres cosas
importantes:

- `.claude/settings.json` **no está versionado**, porque necesita la ruta absoluta del
  ejecutable y es distinta en cada máquina. Lo armás vos con el ejemplo del doc.
- Un hook de `PreToolUse` que falla **bloquea la llamada a la herramienta**. Si copiás
  el `settings.json` de otro, el primer `Read` de tu sesión se cuelga sin explicarte
  nada.
- La carpeta del grafo se crea en la **raíz del repo**, no en `backend/`. graphify sube
  hasta la raíz de git. Si te aparece una adentro de `backend/`, borrala.

## 2.6 · Lo que NO hay que instalar

Esto no es una lista de opiniones: cada una se probó y se descartó con un motivo.

| Herramienta | Por qué no |
|---|---|
| **WSL2 / Ubuntu** | Se migró todo a WSL y rompió el build: `obj/project.assets.json` quedó con rutas absolutas de Linux y el proyecto no compilaba desde Windows. Se volvió a Windows. **Trabajá en Windows nativo.** |
| **OmniRoute** | Pide 8 GB de heap para un agente de código y 10-16 GB para dos. Y enrutar los verificadores a modelos más flojos destruye lo único que aportan: encontrar lo que el implementador no vio. |
| **engram** | Resuelve "el agente olvida entre sesiones". Eso ya lo resuelve `ESTADO.md`, versionado y legible por el equipo. Agregar un servidor MCP cuesta tokens en **cada** request. |
| **BMad** | Capa de planificación. El plan del sprint ya existe y está más ajustado al proyecto. |
| **Threads** | Duplica los subagentes nativos, que ya funcionan. |
| **`everything-claude-code` completo** | Nueve agentes más seis reglas siempre activas. Los esquemas de herramientas y las reglas permanentes se pagan en cada request. |

El stack es: **Claude Code + los dos subagentes del repo + graphify (opcional).** Nada
más. Cada cosa que agregás a la sesión se paga en tokens en cada pedido.

## 2.6b · Opcionales que el equipo usa

Estas las usa Santi y le funcionan. No son obligatorias y el proyecto anda sin
ellas, pero bajan el consumo de tokens en sesiones largas:

- **graphify** — ver la sección 2.5.
- **ponytail** (perfil `lite`) — como paso de revisión.
- **headroom** — gestión de contexto.

La diferencia con la lista de la sección 2.6: estas se invocan cuando hacen falta.
Las de la 2.6 están siempre activas o traen un costo fijo por request, y por eso se
descartaron.

## 2.7 · Verificación final del setup

Desde la raíz del repo:

```powershell
cd backend
dotnet build --no-incremental
dotnet test
```

Tiene que dar **0 warnings y 0 omitidos**. Al 15/10/2026 son **234 tests, 234
correctos**, de los cuales más de 70 corren contra PostgreSQL real. Si ves omitidos, te
falta la variable del punto 2.3. Si ves warnings, avisá: la suite se dejó en cero a
propósito y un warning nuevo es deuda que entró sin que nadie la viera.

---

# 3 · Las reglas que no se negocian

Están en `CLAUDE.md`, que Claude Code lee solo. Las repito acá para que las sepas vos.

**La regla de oro.** `LaCentral.UseCases` **no referencia ningún otro proyecto**. Su
`.csproj` no tiene un solo `PackageReference` ni `ProjectReference`. Si para resolver
algo hace falta agregarle una referencia, la solución está mal: lo que falta es un
puerto.

**Database First.** El script SQL es la fuente de verdad del esquema.
**PROHIBIDO `dotnet ef migrations`.** Un cambio de esquema es un script SQL nuevo en
`backend/db/rescaffold/`, aplicado a mano, seguido de re-scaffold.

**PROHIBIDO editar archivos de `backend/LaCentral.Data/Models/`**: son generados. El
código manual va en `LaCentralDbContext.Personalizaciones.cs`.

**Nada de excepciones para reglas de negocio.** Se usa `Result<T>` con
`TipoError { Ninguno, NoEncontrado, Conflicto, Invalido, NoAutorizado }`.
`MapeoResultado.AResultadoHttp()` traduce a HTTP.

**La API nunca devuelve una entidad de dominio ni un modelo de persistencia.** Solo
DTOs.

**Sin MediatR. Sin interfaces para casos de uso**, solo para puertos de
infraestructura. Los casos de uso se registran a mano en `Program.cs`, que es el único
lugar donde existe el acoplamiento `Api → Data`.

**Los Id los genera la base**, con `GENERATED ALWAYS AS IDENTITY`. Si un test siembra
un Id a mano, PostgreSQL devuelve `428C9`. Nunca siembres Ids.

**Todo en español**: clases, métodos, variables, mensajes y commits.

**Permisos desde la sesión autenticada (`IContextoUsuario`), NUNCA del cuerpo del
request.** Alta y modificación de datos comunes: ADMINISTRADOR y OPERADOR. Cambiar el
código interno de un artículo o retirar un código de proveedor: SOLO ADMINISTRADOR.

---

# 4 · TDD: el método, no una recomendación

Para cada unidad de trabajo, en este orden y sin saltear:

1. **Escribí el test. Ejecutalo. Verificá que FALLE por aserción**, con número de
   línea. No por error de compilación, no por un `NullReferenceException`, no por una
   excepción de un stub: eso es un test que no llegó a probar nada.
2. Escribí el **mínimo** código de producción que lo hace pasar.
3. Corré la **suite completa**. Si rompiste algo, arreglalo antes de seguir.
4. Refactorizá con la suite en verde.

**Commits separados.** El commit del test en rojo va aparte del commit de producción.
Si hace falta andamiaje para que compile, va en un tercer commit de `chore` sin
comportamiento, antes del test. Si test y producción vienen juntos, el orden **no se
puede verificar desde git**, y eso hay que declararlo en el cuerpo del PR.

**Qué se prueba con qué:**

| Qué | Con qué |
|---|---|
| Lógica de casos de uso, guardas, decisiones | Moq |
| Mapeo de repositorio, índices, transacciones, CHECK, atomicidad, búsquedas con `pg_trgm` | **PostgreSQL REAL**, con `[PostgreSqlFact]` y la infraestructura de `LaCentral.Tests/Infraestructura/PostgreSqlPruebas.cs` |
| Recorridos HTTP de punta a punta | `WebApplicationFactory` |

**La regla que más importa:** un mock prueba la lógica del caso de uso, **no prueba que
la fila se escriba**. InMemory no implementa CHECK constraints, ni índices únicos
parciales, ni columnas de identidad. Este equipo ya perdió una semana por eso: dos bugs
críticos pasaron con la suite en verde. **Todo lo que toque persistencia se verifica
releyendo la fila con un contexto nuevo contra PostgreSQL real.**

**Si un test pasa a la primera, no se para: se declara y se verifica con una mutación
no commiteada** que lo ponga en rojo. Un test de caracterización —uno que documenta
comportamiento que ya existe— pasa a la primera por definición, y no está mal. Lo que
importa es que **discrimine**.

---

# 5 · La arquitectura, en tres cajas

```
LaCentral.Api        controladores, DTOs HTTP, seguridad, composition root
LaCentral.UseCases   entidades de dominio, casos de uso, puertos, Result<T>
LaCentral.Data       DbContext, modelos scaffoldeados, repositorios
```

`UseCases` es el centro y no conoce a nadie. `Api` y `Data` dependen de él. Ningún
controlador importa `LaCentral.Data`.

Diagramas en `docs/`: el de arquitectura, el de clases de dominio y el de casos de uso.

---

# 6 · Las decisiones de negocio ya tomadas

**No se re-discuten.** Si una te parece mal, se habla con el equipo; no se cambia en el
código.

| | Decisión |
|---|---|
| **D1** | Precios por modalidad, persistidos por artículo. Seis modalidades: Lista, Tarjeta3, Tarjeta6, EfectivoTransferencia, Costo, CasoEspecial. Fórmula: `precio = valor_de_la_base × (1 + porcentaje/100)`. Lista = +50 sobre CostoArticulo. Costo = 0 sobre CostoArticulo. Efectivo = **−10 sobre PrecioLista**, no sobre el costo. Tarjeta3 y Tarjeta6: tasas **sin definir**, base y porcentaje en NULL — **no inventes un número**. CasoEspecial: precio manual, sin fórmula. **Precio NULL significa "no se pudo calcular", NUNCA cero.** |
| **D2** | Un solo código vigente por proveedor y artículo, más la unicidad global de códigos. |
| **D3** | HU-FCO-01 va **antes** que HU-ART-05: el recálculo necesita una compra que lo dispare. |
| **D7** | Todo en pesos. No hay moneda ni cotización. |
| **D8** | Son **percepciones**, no retenciones: `factura_compra.percepcion_{iva, iibb, municipal, ganancias}`. |
| **D9** | El destino de stock se carga **por línea**: `factura_compra_detalle.sucursal_id`. |
| **D10** | Historial de baja en cuatro tablas 1:N, con `fecha_reactivacion`, `usuario_baja_id` y `usuario_reactivacion_id`. Índice único parcial: **una sola baja abierta** por entidad. La baja y su historial ocurren en **una transacción**. Una fila activa puede conservar motivo y fecha: es el rastro de su última baja, y HU-CLI-05 CA-003 exige no perderlo. |

**`CostoArticulo` es el campo costo del artículo, NO la modalidad Costo.** Que hoy
coincidan es un accidente, no una identidad.

---

# 7 · El ciclo de una fase, de punta a punta

## 7.1 · Antes de empezar

Abrí Claude Code desde la raíz del repo y pasale esto:

```
Leé CLAUDE.md, docs/sprint03/PLAN_SPRINT3.md y docs/sprint03/ESTADO.md, y arrancá por
la sección "PRÓXIMA SESIÓN" de ESTADO.md.
```

`ESTADO.md` tiene al final una sección **"PRÓXIMA SESIÓN — arrancar acá"** que dice qué
toca, con qué bloqueos y de qué rama sale. Es el único lugar donde hay que mirar.

Confirmá al arrancar: los dos subagentes cargan como tipo, y `dotnet test` da el número
que dice `ESTADO.md`. Si no coincide, pará y averiguá por qué antes de escribir una
línea.

## 7.2 · La rama

```powershell
git switch develop
git pull
git switch -c feature/<rama-de-la-fase>
```

Si la fase anterior **todavía no está mergeada** en develop y esta fase la necesita, la
rama sale de la rama anterior, y en el cuerpo del PR se deja dicho que hay que
**cambiar la base a develop cuando la anterior entre, y revisar el diff ahí**. Así se
hicieron las Fases 1, 2 y D1, y las tres entraron limpias en orden.

## 7.3 · Mientras trabaja

Commits atómicos, convencionales, en español:

```
test(articulos): alta rechaza dos codigos del mismo proveedor
feat(articulos): alta de articulo con codigos y stock inicial
fix(clientes): actualizar por Id en lugar de codigo mutable
```

**PROHIBIDO:** push directo a `develop` o `main`. Mergear tus propios PR. `--force`.
Cerrar un Issue con `Closes #N` si la HU quedó incompleta — se usa `Refs #N`.

## 7.4 · Al cerrar: los dos subagentes

Antes de abrir el PR corren los dos, **de a uno, nunca en paralelo**, sobre el diff de
la rama:

- **`verificador-tdd`** — comprueba que el ciclo TDD se respetó: que cada test se vio
  en rojo por la razón correcta, y que los tests **discriminan** (una mutación en
  producción los pone en rojo). Una mutación que **sobrevive** con la suite en verde es
  un agujero de cobertura y es bloqueante.
- **`auditor-persistencia`** — comprueba que todo lo que toca la base se verificó
  contra PostgreSQL real, releyendo con un contexto nuevo, y no contra mocks o
  InMemory.

Ninguno de los dos arregla nada: leen y reportan. Un verificador que puede arreglar lo
que encuentra deja de ser verificador.

**Ante un NO APTO:** no se para, se arregla y se vuelve a verificar, **hasta 3 rondas**.
Lo único que se modifica es **código de producción y tests**.

PROHIBIDO, para llegar a APTO:
- cambiar la definición de un subagente o aflojar su criterio;
- reescribir el cuerpo del PR para que el hallazgo deje de aplicar;
- declarar como excepción algo que se puede arreglar;
- escribir un test que alcanza una rama con un doble absurdo en lugar de probar
  producción.

Después de 3 rondas sin APTO: **PARÁ y reportá** qué probaste en cada una.

**Una guarda inalcanzable no se arregla con un test, se borra.** Si una mutación
sobrevive porque la rama es imposible de alcanzar en producción, el arreglo correcto es
eliminar la guarda. Un test que llega a una rama muerta inyectando un doble imposible
no prueba producción: prueba el doble.

**Dos cosas que aprendimos a la fuerza con los subagentes:**

1. **Nunca más de uno a la vez.** El 14/10 corrieron dos, copiando los mismos archivos
   de producción del mismo repo, y uno **revirtió en silencio el árbol principal**. El
   veredicto que salió de ahí no valía nada. La definición nueva se niega a correr si
   detecta otro worktree.
2. **La suite completa se corre una vez al final, no una por commit.** Tarda ~110
   segundos porque más de 70 tests van contra PostgreSQL por red. El rojo de cada
   commit se comprueba con el test puntual, con `dotnet test --filter`.

## 7.5 · El cuerpo del PR

Va en `docs/sprint03/pr-<lo-que-sea>.md`, commiteado. Obligatorio:

- **Qué hace** y **qué NO hace**.
- **Cómo se verificó:** conteo de tests, cuántos contra PostgreSQL real, los veredictos
  de los dos subagentes.
- **Qué queda pendiente y por qué.**
- **Las excepciones a TDD, declaradas**: qué pasó a la primera y con qué mutación se
  verificó.

**Si algo quedó a medias, decilo. Un PR que oculta deuda es peor que uno que no
existe.** Esto no es retórica: ya pasó que el cuerpo de un PR afirmara "cada test se
vio fallar antes" cuando git no lo podía respaldar, y el verificador lo marcó como
bloqueante. El cuerpo del PR no puede afirmar nada que los subagentes no hayan podido
verificar.

No hay `gh` en las máquinas del equipo, así que el PR se abre a mano desde el link de
compare de GitHub, pegando el cuerpo del archivo.

## 7.6 · `ESTADO.md`

Se actualiza **al cerrar cada fase**, y es lo último que se toca antes de parar.
Tiene que quedar:

- la fila de la fase en la tabla: estado, rama, PR, conteo de tests;
- las **decisiones** que se tomaron, con fecha;
- la **deuda nueva** detectada, con archivo y línea;
- los **veredictos** de los dos subagentes, con qué verificó cada uno;
- la sección **"PRÓXIMA SESIÓN"** reescrita para la fase siguiente.

Si se pierde el contexto de la sesión, **este archivo es la única fuente de verdad**.
Un `ESTADO.md` desactualizado hace que la sesión siguiente rehaga trabajo ya hecho —
también pasó.

## 7.7 · El entregable visible

**Requisito permanente: toda fase que agregue endpoints cierra con un entregable en
`frontend/`**, no solo con tests. Angular, sin librería de estado y sin framework de
UI: lo mínimo que funcione y se vea decente.

Ojo con esto: los endpoints son `[Authorize]`. Hace falta un login mínimo contra el
endpoint de acceso que ya existe, con el token en memoria. Sin eso la pantalla no
muestra nada.

En `frontend/README.md` van los comandos para levantarla y el puerto.

---

# 8 · Dónde PARAR y preguntar

Esto no es opcional. El agente para **inmediatamente**, sin intentar workarounds, si:

- para resolver algo necesita agregar una **referencia de proyecto a `UseCases`**;
- para resolver algo necesita **editar un archivo de `Data/Models/`**;
- para resolver algo necesita **cambiar el esquema de la base**;
- un **criterio de aceptación contradice otro**, o contradice el esquema;
- la **conexión a PostgreSQL no está disponible** — una prueba no ejecutada NO es una
  prueba aprobada: se registra como no ejecutada y se para;
- lleva **más de tres intentos** en el mismo problema.

En todos esos casos: explica qué encontró, qué opciones ve y qué recomienda. **No
decide.**

Esas compuertas funcionan. En la Fase 3 el agente paró al descubrir que la base de
pruebas tenía tres columnas que producción no tenía, en lugar de scaffoldear sobre un
esquema equivocado.

---

# 9 · Lo que falta construir

Detalle en `docs/sprint03/PLAN_SPRINT3.md`. Estado al día en `ESTADO.md`.

| Fase | Qué | Estado al 15/10/2026 |
|---|---|---|
| 0–2, D1 | Línea base, deuda bloqueante, historial de bajas D10, compuerta de esquema de precios | **hechas y mergeadas** |
| 3 | HU-ART-01 · Alta de artículo | hecha, PR en revisión |
| **4** | **HU-ART-02 · Búsqueda y endpoints (#15)** | **la que sigue** |
| 5 | HU-FCO-01 · Factura de compra (#32) | pendiente |
| 6 | HU-ART-05 · Precios por modalidad + FCO-01 CA5 (#22) | pendiente |
| 7 | HU-ART-03 · Modificación y códigos (#17) | pendiente |
| 8 | Cierre: informe final del sprint | pendiente |

Dos cosas que están en el esquema pero **no** en el plan del Sprint 3, y que alguien va
a tener que tomar: **ventas** (`factura_venta`) y **transferencias entre sucursales**
(`transferencia`). Y las **tasas de Tarjeta3 y Tarjeta6**, que no las define el código:
las define Alfredo.

---

# 10 · Cómo no quemar tokens

Lo que de verdad los consume, en orden:

1. **Hacer verificar de nuevo código que no cambió.** Si el verificador ya aprobó un
   diff y ese código no se tocó, **acotale el alcance** a lo nuevo. Decílselo textual
   en la consigna.
2. **Correr la suite completa muchas veces.** Una vez al final. Para un test puntual,
   `dotnet test --filter`.
3. **Pasarle diffs enormes a un subagente de una sola vez.** Un subagente que llena su
   propio contexto se cuelga sin avisar.
4. **Agregar herramientas a la sesión.** Los esquemas de las herramientas y las reglas
   siempre activas se pagan en **cada** request. Es la razón de la sección 2.6.

Y lo que sí ahorra: **que cada sesión arranque leyendo `ESTADO.md`** en lugar de
arrastrar la conversación entera. Para eso existe ese archivo.

---

# 11 · Si algo se cuelga

Pasa. Esto es lo que aprendimos:

**El agente dice "esperando agente" y no avanza.** En otra terminal:

```powershell
git worktree list
Get-Process dotnet,testhost,vstest.console -ErrorAction SilentlyContinue | Select-Object Name,CPU,StartTime
```

Si hay un worktree extra y `testhost` con CPU que sube, está corriendo la suite:
esperá. Si no hay ningún proceso y el worktree está quieto más de diez minutos, murió.

**Para limpiar:**

```powershell
Get-Process dotnet,testhost,vstest.console,MSBuild -ErrorAction SilentlyContinue | Stop-Process -Force
git worktree list
git worktree remove --force "<ruta-del-worktree>"
git worktree prune
```

**Y antes de cualquier otra cosa, averiguá si se perdió trabajo:**

```powershell
git status
git log --oneline origin/develop..HEAD
git stash list
```

Si los commits están, no se perdió nada: el árbol sucio es irrelevante. **No corras
`git reset --hard`, ni `git stash drop`, ni `git checkout .` hasta haber mirado eso.**

**Pusheá la rama aunque el PR no esté listo.** Pushear no es abrir el PR: es respaldo.
Treinta commits que existen solo en tu máquina son treinta commits a un worktree
corrupto de distancia.

---

# 12 · Seguridad de los datos — leé esto dos veces

El contenedor tiene **datos reales de la empresa**: 124 proveedores y 2108 clientes
importados de Multisoft.

- Existen `PostgreSqlSeguridadTests` que impiden apuntar a la base principal. **No los
  desactives, no los eludas.**
- **Ningún `TRUNCATE` ni `DROP` fuera del esquema exclusivo de pruebas.**
- La base de test se llama `test_*` o `*_test`. Si tu cadena apunta a otra cosa, está
  mal.
- **La cadena de conexión no va al repo, no va a un chat, no va a un archivo
  versionado.** Se pide por privado y se guarda en una variable de entorno. Cuando se
  lee de una variable, nunca queda escrita.
- Cuando elijas una contraseña para una base, evitá `;` `$` `` ` `` `"` `'` `=`. El `;`
  parte la cadena de conexión y el `$` lo interpreta PowerShell.

---

# 13 · El resumen de todo esto en una página

1. Arrancá Claude Code **desde la raíz del repo**.
2. Primer mensaje: que lea `CLAUDE.md`, `PLAN_SPRINT3.md` y `ESTADO.md`, y arranque por
   **"PRÓXIMA SESIÓN"**.
3. Rama nueva por fase. TDD con commits separados. Nada de `ef migrations`.
4. Todo lo que toca la base se prueba contra **PostgreSQL real**, releyendo con un
   contexto nuevo.
5. Al cerrar: los **dos subagentes**, de a uno. Ante NO APTO, arreglar y reverificar,
   hasta 3 rondas.
6. Cuerpo del PR honesto, `ESTADO.md` actualizado, entregable visible, y **PARAR**.
7. Ante una compuerta de la sección 8: **no decide el agente**, decide el equipo.

Lo que hace defendible este proyecto en la tesis no es que los tests estén en verde: es
que se puede demostrar **cómo** se llegó a ese verde. Por eso los commits separados, por
eso las mutaciones, por eso el `ESTADO.md`. Un PR que oculta deuda rompe eso, y es más
caro que la deuda.
