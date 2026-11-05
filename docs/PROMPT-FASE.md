# Protocolo de ejecución de una fase

Este archivo es el **protocolo de trabajo del agente**. Lo lee Claude Code al arrancar
una fase y lo sigue de punta a punta.

Cómo se invoca, y es lo único que hace falta pegar:

```
Leé CLAUDE.md, docs/ONBOARDING-AGENTE.md y docs/PROMPT-FASE.md.
Ejecutá la FASE <N> completa según ese protocolo.
```

---

## Lo primero: cómo se trabaja acá

**Ejecutás la fase COMPLETA de corrido.** No pedís confirmación entre commits, no
preguntás si seguir, no mostrás cada diff para que alguien lo apruebe. El humano te
revisa **al final**, sobre el PR y tu reporte.

Pedir aprobación commit por commit convierte al humano en un revisor de líneas, que es
justo lo que esta forma de trabajo evita: para eso están los dos subagentes
verificadores. El humano decide lo que un agente no puede decidir, y eso está en la
lista de compuertas más abajo.

**Hablás poco mientras trabajás.** Nada de narrar cada paso. Un aviso al empezar, los
reportes de las compuertas, y el reporte final.

---

## 1 · Antes de escribir una línea

1. **Arrancá desde la raíz del repo.** Si `pwd` termina en `backend/`, pará: no
   cargaron ni los subagentes ni las reglas.
2. Leé `docs/sprint04/ESTADO.md`, en especial la sección **"PRÓXIMA SESIÓN"**, y la fase
   que corresponde en `docs/sprint04/REPLAN-SPRINT4-v2.md`.
3. Confirmá que `verificador-tdd` y `auditor-persistencia` cargan como `subagent_type`
   real. Si da `Agent type not found`, **PARÁ**.
4. Corré `dotnet build --no-incremental` y `dotnet test` desde `backend/`. El total tiene
   que coincidir con lo que dice `ESTADO.md`, con **0 omitidos y 0 warnings**. Si no
   coincide, **PARÁ y averiguá por qué** antes de tocar nada: puede haber entrado una
   fase que no sabés, o un `ESTADO.md` desactualizado.
5. Si la fase anterior no está mergeada y esta la necesita, la rama sale de la rama
   anterior, y lo declarás en el cuerpo del PR: **cambiar la base a `develop` cuando la
   anterior entre, y revisar el diff ahí.**

```powershell
git switch develop
git pull
git switch -c feature/<rama-de-la-fase>
```

---

## 2 · El ciclo, por cada unidad de trabajo

**Tres commits por unidad**, en este orden:

| | Commit | Qué lleva |
|---|---|---|
| 1 | `chore(...)`: estructura | Firmas que compilan y devuelven vacío o null. **Sin comportamiento.** Existe para que el rojo del paso 2 sea por aserción y no por error de compilación. |
| 2 | `test(...)`: rojo | El test. Lo corrés, **lo ves fallar por aserción con número de línea**, y lo commiteás en rojo. |
| 3 | `feat(...)` / `fix(...)` | El mínimo código de producción que lo hace pasar. |

Después de cada unidad: **la suite completa**. Si rompiste algo, lo arreglás antes de
seguir.

**Un rojo que no es rojo:** si el test falla por error de compilación, por un
`NullReferenceException` o por la excepción de un stub `NotImplementedException`, **no
llegó a probar nada**. Volvé al paso 1 y armá la estructura.

**Si un test pasa a la primera**, no pares: **declaralo y verificalo con una mutación no
commiteada** que lo ponga en rojo. Un test de caracterización —uno que documenta
comportamiento que ya existe— pasa a la primera por definición. Lo que importa es que
**discrimine**. La mutación va descrita en el cuerpo del PR.

**Qué se prueba con qué:**

| Qué | Con qué |
|---|---|
| Lógica de casos de uso, guardas, decisiones | Moq |
| Mapeo de repositorio, índices, transacciones, CHECK, atomicidad, `pg_trgm` | **PostgreSQL REAL**, con `[PostgreSqlFact]` |
| Recorridos HTTP de punta a punta | `WebApplicationFactory` |

Un mock prueba la lógica del caso de uso, **no prueba que la fila se escriba**. Todo lo
que toque persistencia se verifica **releyendo la fila con un contexto nuevo** contra
PostgreSQL real.

---

## 3 · La compuerta: los dos subagentes

Al terminar la fase, con la suite en verde, corrés los dos sobre el diff de la rama.

**De a uno. Nunca en paralelo.** Esto no es una preferencia: el 14/10/2026 corrieron dos
a la vez copiando los mismos archivos de producción del mismo repo, y uno **revirtió en
silencio el árbol principal**. El veredicto que salió de ahí no valía nada.

Antes de lanzar cada uno, `git worktree list`: si hay un worktree que no sea el
principal, hay otra instancia viva o un residuo. Limpialo o pará.

### Ante un NO APTO: no se para, se arregla

Arreglás y volvés a verificar, **hasta 3 rondas**. Lo único que modificás es **código de
producción y tests**.

**PROHIBIDO, para llegar a APTO:**

- cambiar la definición de un subagente o aflojar su criterio;
- reescribir el cuerpo del PR para que el hallazgo deje de aplicar;
- declarar como excepción algo que se puede arreglar;
- escribir un test que alcanza una rama con un doble absurdo en lugar de probar
  producción.

**Después de 3 rondas sin APTO: PARÁS** y reportás qué probaste en cada una.

**Una guarda inalcanzable no se arregla con un test: se borra.** Si una mutación
sobrevive porque la rama es imposible de alcanzar en producción, el arreglo correcto es
eliminar la guarda. Un test que llega a una rama muerta inyectando un doble imposible no
prueba producción: prueba el doble. Justificá la elección en el cuerpo del PR.

### Acotá el alcance

Si un subagente ya aprobó un diff y ese código **no cambió**, no lo vuelva a revisar.
Decíselo textual en la consigna: *"Alcance: solo los commits X..Y. El resto del diff ya
se verificó y ese código no cambió."* Re-verificar lo que no se tocó es lo que colgó esta
herramienta una vez.

---

## 4 · Dónde PARÁS de verdad

Inmediatamente, sin intentar workarounds:

- para resolver algo necesitás **agregar una referencia de proyecto a `UseCases`**;
- para resolver algo necesitás **editar un archivo de `Data/Models/`**;
- para resolver algo necesitás **cambiar el esquema de la base**;
- un **criterio de aceptación contradice otro**, o contradice el esquema;
- falta una **regla de negocio** que nadie definió;
- la **conexión a PostgreSQL no está disponible** — una prueba no ejecutada NO es una
  prueba aprobada: la registrás como no ejecutada y parás;
- llevás **más de tres intentos** en el mismo problema.

En todos esos casos: explicás qué encontraste, qué opciones ves y qué recomendás. **No
decidís vos.**

La diferencia con un NO APTO: un NO APTO es un tecnicismo de código y lo arreglás. Esto
es una decisión, y la toma el equipo.

---

## 5 · El cuerpo del PR

En `docs/sprint04/pr-<lo-que-sea>.md`, commiteado. Obligatorio:

- **Qué hace** y **qué NO hace**.
- **Cómo se verificó:** conteo de tests, cuántos contra PostgreSQL real, los dos
  veredictos.
- **La tabla de TDD:** por unidad, los hashes de estructura → test (rojo, con línea) →
  producción.
- **Las excepciones declaradas:** qué pasó a la primera y con qué mutación se verificó.
- **Qué queda pendiente y por qué.**

**Si algo quedó a medias, decilo.** Un PR que oculta deuda es peor que uno que no existe.
El cuerpo del PR **no puede afirmar nada que los subagentes no hayan podido verificar**:
ya pasó que afirmara "cada test se vio fallar antes" cuando git no lo podía respaldar, y
el verificador lo marcó como bloqueante.

No hay `gh` en las máquinas del equipo: el PR se abre a mano desde el link de compare,
pegando el cuerpo del archivo. Si no podés abrirlo, dejá el link armado en el reporte.

---

## 6 · `ESTADO.md` va ANTES del push

Es la única fuente de verdad del estado, y **quedó desactualizado tres veces**. Una
sesión que lo lea mal rehace trabajo ya hecho.

Tiene que quedar:

- la fila de la fase en la tabla: estado, rama, PR, conteo de tests y cuántos PG;
- las **decisiones** tomadas, con fecha;
- la **deuda nueva** detectada, con archivo y línea;
- los **veredictos** de los dos subagentes, con qué verificó cada uno;
- la sección **"PRÓXIMA SESIÓN"** reescrita para la fase siguiente, con sus bloqueos
  reales.

El número del PR se conoce recién al abrirlo: ese va en un commit chico aparte.

---

## 7 · El entregable visible

**Toda fase que agregue endpoints cierra con un entregable en `frontend/`**, no solo con
tests. Angular, sin librería de estado y sin framework de UI: lo mínimo que funcione y
se vea decente.

Los endpoints son `[Authorize]`: hace falta el login contra el endpoint de acceso que ya
existe, con el token en memoria.

---

## 8 · Lo que NO hacés nunca

- **No mergeás tus propios PR.** Ni ningún PR.
- **No pusheás a `develop` ni a `main`.** Nunca `--force`.
- **No cerrás un Issue** si la HU quedó incompleta: `Refs #N`, no `Closes #N`.
- **No tocás el esquema de la base.** Los scripts SQL los escribe el equipo.
- **No desactivás ni eludís `PostgreSqlSeguridadTests`.** Ningún `TRUNCATE` ni `DROP`
  fuera del esquema exclusivo de pruebas.
- **No pegás una cadena de conexión** en un archivo versionado, en un comentario ni en un
  reporte. Van como marcador de posición.

---

## 9 · El reporte final

Cuando terminás, parás y reportás. En este orden, y sin adornos:

1. **El estado:** fase cerrada, o parada en tal compuerta.
2. **Los dos veredictos**, textuales. Si hubo rondas, qué se arregló en cada una.
3. **Los números:** build (warnings), suite (total, pasados, fallados, omitidos), cuántos
   contra PostgreSQL real. Antes y después.
4. **Las decisiones** que tomaste y por qué, en una línea cada una.
5. **La deuda nueva**, con archivo y línea.
6. **Qué NO pudiste verificar**, y por qué. Esto es obligatorio: un reporte sin esta
   sección es un reporte que oculta sus límites.
7. **Qué necesitás del humano**, si necesitás algo. Una cosa por línea, concreta.
8. **El link del PR**, o el link de compare armado.

Después de eso, **PARÁS**. No empezás la fase siguiente.

---

## 10 · Por qué todo esto

Lo que hace defendible este proyecto en la tesis no es que los tests estén en verde: es
que se puede demostrar **cómo** se llegó a ese verde. Por eso los tres commits por
unidad, por eso las mutaciones, por eso el `ESTADO.md`, por eso un cuerpo de PR que
declara lo que no pudo verificar.

Un atajo que te lleve a un APTO sin esa evidencia no te ahorra trabajo: te lo mueve a la
defensa, cuando ya no se puede arreglar.
