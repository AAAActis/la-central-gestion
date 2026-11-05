# La Central · reglas del proyecto

Las rutas de este archivo son relativas a la raíz del repo (raíz de git y del grafo).
La solución .NET está en `backend/LaCentral.slnx`: `dotnet build` y `dotnet test` se
corren desde `backend/`.

## Graphify
Hay un grafo de conocimiento en `graphify-out/`. Para preguntas sobre el código,
`graphify query "<pregunta>"` ANTES de leer archivos de a uno o grepear.
`graphify path "A" "B"` para relaciones, `graphify explain "X"` para un concepto.
`graphify-out/GRAPH_REPORT.md` solo para revisión amplia de arquitectura.
Después de modificar código: `graphify update .` desde la raíz del repo.

## Arquitectura Core-Driven
- `LaCentral.Api` controladores, DTOs HTTP, seguridad, composition root
- `LaCentral.UseCases` entidades, casos de uso, puertos, Result<T>
- `LaCentral.Data` DbContext, modelos scaffoldeados, repositorios

REGLA DE ORO: `LaCentral.UseCases` NO referencia ningún otro proyecto. Su `.csproj`
(`backend/LaCentral.UseCases/LaCentral.UseCases.csproj`) no tiene un solo
`PackageReference`. Si para resolver algo hace falta agregarle una referencia, lo que
falta es un puerto.

El acoplamiento `Api → Data` existe SOLO en `backend/LaCentral.Api/Program.cs`. Ningún
controlador importa `LaCentral.Data`.

## Prohibiciones
- `dotnet ef migrations`. El esquema es Database First: script SQL (`backend/db/`) +
  re-scaffold.
- Editar archivos de `backend/LaCentral.Data/Models/` — son generados. El código manual
  va en `backend/LaCentral.Data/LaCentralDbContext.Personalizaciones.cs` (partial).
- Excepciones para flujo de negocio. Se usa `Result<T>` con
  `TipoError { Ninguno, NoEncontrado, Conflicto, Invalido, NoAutorizado, Prohibido }`.
  `NoAutorizado` (401): sin credenciales válidas o sin usuario en la sesión. `Prohibido`
  (403): la sesión es válida, pero su rol no permite la operación.
- Devolver entidades o modelos de persistencia desde la API. Solo DTOs, vía
  `MapeoResultado.AResultadoHttp()`.
- Sembrar Ids en los tests. Los genera la base: `GENERATED ALWAYS AS IDENTITY`.
  Un Id a mano da error 428C9 contra PostgreSQL.
- MediatR. Interfaces para casos de uso — solo para puertos de infraestructura.

## Pruebas
- Lógica de casos de uso y guardas → Moq.
- Mapeo de repositorio, índices, transacciones, CHECK constraints, atomicidad,
  búsqueda con pg_trgm → PostgreSQL REAL, vía
  `backend/LaCentral.Tests/Infraestructura/PostgreSqlPruebas.cs` y `[PostgreSqlFact]`.
- Recorridos HTTP → `WebApplicationFactory`.

Un mock prueba la lógica del caso de uso, NO prueba que la fila se escriba. InMemory no
implementa CHECK constraints, ni índices únicos parciales, ni columnas de identidad.
Este equipo perdió una semana por eso: dos bugs críticos pasaron con la suite en verde.
Todo lo que toque persistencia se verifica releyendo la fila CON UN CONTEXTO NUEVO.

## NO APTO de un subagente
Ante un NO APTO de un subagente, NO se para: se arregla y se vuelve a verificar, hasta 3
rondas. Lo único que se modifica es código de producción y tests.
PROHIBIDO, para llegar a APTO:
- cambiar la definición de un subagente o aflojar su criterio;
- reescribir el cuerpo del PR para que el hallazgo deje de aplicar;
- declarar como excepción algo que se puede arreglar;
- escribir un test que alcanza una rama con un doble absurdo en lugar de probar producción.

Después de 3 rondas sin APTO: PARÁS y reportás qué probaste en cada una.
SIGUEN PARANDO, como siempre: un NO APTO que exija cambiar el esquema de la base, o que
venga de criterios de aceptación contradictorios o de una regla de negocio que falta. Eso
lo decide el equipo.

Guarda sin test: si es ALCANZABLE en producción, test en rojo por un camino real, en un
commit separado del de producción. Si NO es alcanzable, se BORRA y queda el test que
demuestra que el caso no puede ocurrir por otra vía.

## Seguridad de datos
El contenedor de Tailscale tiene datos reales de la empresa: 124 proveedores y 2108
clientes. `PostgreSqlSeguridadTests` impide apuntar a la base principal: no lo
desactives ni lo eludas. Ningún `TRUNCATE` ni `DROP` fuera del esquema de pruebas.

## Decisiones de negocio vigentes
- D1 Precios por modalidad persistidos: Lista, Tarjeta3, Tarjeta6,
  EfectivoTransferencia, Costo, CasoEspecial. Lista = costo × 1,50. Costo = × 1,00 (0%).
  Efectivo = lista × 0,90. Tarjeta3/6: tasas configurables SIN DEFINIR — no inventar un
  número. Costo desconocido es null, nunca cero.
- D2 Un código vigente por proveedor y artículo. Más la unicidad global de códigos.
- D3 HU-FCO-01 antes que HU-ART-05.
- D7 Todo en pesos. Sin moneda ni cotización.
- D8 Son PERCEPCIONES: `factura_compra.percepcion_{iva, iibb, municipal, ganancias}`.
- D9 Destino de stock POR LÍNEA: `factura_compra_detalle.sucursal_id`.
- D10 Historial de baja en tablas 1:N con índice único parcial sobre la entidad
  `WHERE fecha_reactivacion IS NULL`. El CHECK es
  `activo OR (motivo IS NOT NULL AND fecha IS NOT NULL)`: una fila ACTIVA puede
  conservar motivo y fecha — es el rastro de su última baja (HU-CLI-05 CA-003).

## Permisos
Se leen de la sesión autenticada (`IContextoUsuario`), NUNCA del cuerpo de la petición.
- Alta y modificación de datos comunes: ADMINISTRADOR y OPERADOR.
- Cambiar el código interno de un artículo: SOLO ADMINISTRADOR.
- Retirar un código de proveedor: SOLO ADMINISTRADOR, operación explícita. Omitir un
  código en una modificación ordinaria NO lo elimina.

## Frontend
**UI:** toda pantalla o componente de `frontend/` sigue `docs/diseno/SISTEMA-VISUAL.md`.
No se inventan colores, tamaños ni animaciones fuera de sus tokens.

Un spec NO afirma un valor de color (`toBe('#2d6cdf')`, `rgb(...)`): se rompe con cada cambio
de paleta y no verifica nada que importe. Afirma el comportamiento: que la clase esté aplicada,
que `aria-current` esté presente, que el activo se distinga del resto, que el botón quede
deshabilitado mientras carga.

La suite de frontend corre solo en Chromium (ChromeHeadless): verifica un motor. Lo que depende
del navegador —por ejemplo, que `<dialog>` devuelva el foco al cerrarse— no está probado en
Firefox ni en Safari.

## Idioma
Todo en español: clases, métodos, variables, mensajes, commits.

## Estado del sprint
`docs/sprint03/ESTADO.md` es la memoria entre fases. Leelo al empezar, actualizalo al
terminar cada fase. Si el contexto se pierde, ese archivo es la única fuente de verdad
sobre qué está hecho.

REGLA: ESTADO.md se actualiza ANTES del push de cada fase, nunca después, y va en el
mismo push: la tabla de fases, los PRs y merges, las decisiones con fecha, la deuda y
"PRÓXIMA SESIÓN". Ya quedó desactualizado tres veces; una sesión que lo lee mal rehace
trabajo hecho.

## Bitácora de tarea · obligatoria

Todo trabajo se registra en `docs/sprint<NN>/BITACORA.md` **mientras pasa**, no al final.
Es lo único que permite retomar una sesión cortada sin perder contexto.

**Al recibir una instrucción, antes de la primera acción**, agregá un bloque nuevo al final
del archivo:

```
## <fecha y hora> · <título corto de la tarea>

**Instrucción recibida (verbatim):**
> <la instrucción tal como llegó, sin resumir ni reordenar>

**Plan:** <los pasos, numerados>

**Estado:** en curso
```

**Después de cada paso terminado**, una línea bajo ese bloque: qué cambió y con qué
evidencia — commit, número de PR, conteo de tests. Nunca "listo" sin el dato que lo
respalda.

**Antes de parar, de preguntar algo o de quedarte bloqueado**, escribí la línea
`**BLOQUEADO EN:** <qué falta y de quién depende>` y recién entonces respondé.

**Al terminar la tarea**, cambiá `**Estado:** en curso` por `**Estado:** terminada` y
agregá qué quedó abierto.

### Reglas

- **Se agrega, nunca se reescribe.** La bitácora es rastro, no resumen. Un bloque viejo no
  se corrige: si algo cambió, va en un bloque nuevo.
- **Al arrancar o al retomar una sesión, lo PRIMERO que hacés es leer el último bloque con
  `**Estado:** en curso`.** Si hay uno, seguís desde ahí: no vuelvas a empezar ni rehagas
  lo que ya figura hecho.
- **La instrucción va verbatim** porque es lo que se pierde primero cuando se corta la
  sesión, y es lo único que no se puede reconstruir del repositorio.
- No reemplaza al `ESTADO.md`. El `ESTADO.md` es el estado del **sprint** y se actualiza
  antes de cada push; la bitácora es el estado de la **tarea en curso** y se actualiza
  mientras la tarea pasa.

## Política de pruebas · cuándo se corre qué

La suite completa tarda más de dos minutos, pega contra un PostgreSQL que corre en una
netbook compartida con otros siete contenedores, y cuando falla produce una traza por
fallo. Correrla en cada iteración es caro en tiempo, en carga y en contexto.

### En el bucle de trabajo · filtrado, siempre

```bash
dotnet test --filter "FullyQualifiedName~<NombreDeLaClase>" --no-build
```

El `~` es "contiene", así que sirve con el nombre de la clase o del namespace. `--no-build`
ahorra la compilación cuando ya compilaste.

**Qué entra en el filtro:** la clase que tocaste **y las que dependen de lo que tocaste** —
el caso de uso, su repositorio y el endpoint que lo expone. No se adivina: se mira qué
referencia lo que cambió (`graphify query` sirve para eso) y se filtra por el namespace que
los cubre a todos.

**Frontend:** `npx ng test --watch=false --browsers=ChromeHeadless`, y **solo si tocaste
`frontend/`**. Si el cambio es de backend, no se corre.

### La suite completa · solo en estos cuatro momentos

1. Antes de abrir un PR, o antes de actualizarlo con el último commit.
2. Después de un merge a `develop`, porque es el estado que hereda todo el mundo.
3. Cuando cambió el esquema de la base o un contrato compartido (un DTO, una interfaz de
   repositorio, un componente de `compartido/`).
4. Cuando la corrida filtrada dio verde pero el cambio toca algo transversal y no estás
   seguro de a quién más afecta.

Fuera de esos cuatro, no se corre entera.

### Dos reglas que ahorran la mayor parte del costo

- **Nunca dos veces por el mismo cambio.** El número que vale es el del último commit del
  PR: si corriste la suite y después commiteaste algo, la que cuenta es la de después, y la
  anterior no se reporta.
- **Si la suite completa falla, no la vuelvas a correr entera para ver si se arregló.**
  Corré filtrado sobre lo que falló hasta que esté en verde, y la completa **una sola vez**
  al final. Una corrida completa en rojo mete una traza por fallo en el contexto; dos
  corridas completas en rojo lo llenan.

### La separación por tipo de prueba

Las clases que usan `PostgreSqlPruebas` llevan `[Trait("Base", "PostgreSQL")]`. Con eso hay
dos comandos:

```bash
dotnet test --filter "Base!=PostgreSQL"    # unitarias puras: segundos, cero carga a la Arch
dotnet test                                # completa, en los cuatro momentos de arriba
```

El primero es el que se usa cuando el cambio no toca persistencia.

### Lo que no se negocia

Una prueba no ejecutada **no** es una prueba aprobada. Filtrar decide *cuándo* se corre
cada cosa, nunca *si* se corre: todo lo que el cambio afecta se prueba antes del PR, y el
conteo que queda escrito es el de una corrida real, no una estimación.
