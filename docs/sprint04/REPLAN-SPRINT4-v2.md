# Replan Sprint 4 · versión 2 · 27/10/2026

Reemplaza al replan del 26/10. Va en `docs/sprint04/REPLAN-27-10.md`, y sus decisiones
se aplican sobre `docs/sprint04/ESTADO.md`, que sigue siendo la única fuente de verdad
del sprint.

---

## Por qué hay una v2

El replan del 26/10 contaba con 5 días hábiles y 17 SP. El martes 27 se consumió
entero en recuperar el entorno de desarrollo: la base de la tesis estaba caída desde
el 20/10 y una regla de reenvío de red desviaba las pruebas a la base de otro
proyecto. Quedó resuelto y los 435 tests están en verde, pero quedan **3 días
hábiles**, no 5.

El Sprint 4 cierra el **viernes 30/10** (el 31 es sábado y no se trabaja).

**Velocidad medida:** Sprint 3 entregó 34 SP en 10 días hábiles → 3,4 SP/día de
equipo. Tres días dan una capacidad realista de **~10 SP de equipo**, contra los 17
planificados. El plan de abajo mantiene los 17 pero los ordena de modo que **lo que
entre sea demostrable**, y declara de antemano qué se cae si algo se atrasa.

---

## Objetivo (sin cambios)

> Que las cinco áreas construidas en los Sprints 1 a 3 tengan pantalla, y que un
> empleado pueda operar el sistema de punta a punta sin que nadie le explique.

---

## Paso 0 · `docs/diseno.md` · antes de la primera pantalla

**No estaba en el replan anterior y es lo que más rinde de toda la semana.**

En el proyecto de nutrición el frontend consumió muchas iteraciones y muchos tokens
porque cada corrección era una adivinanza nueva: no había nada escrito contra lo que
converger. La causa no fue la herramienta, fue que el diseño no tenía criterios de
aceptación.

Antes de que el agente escriba una línea de HTML, queda escrito y acordado:

- Paleta: fondo, superficie, texto primario y secundario, acento, error, éxito.
- Escala tipográfica: cuántos tamaños y para qué sirve cada uno.
- Escala de espaciado: cuántos valores y cuándo se usa cada uno.
- Densidad de tabla: alto de fila, separación, alineación de números.
- Anatomía de un formulario: dónde va la etiqueta, dónde el error, qué pasa al enviar.
- Anatomía de un estado vacío, un estado de carga y un error de red.
- Anatomía de una confirmación destructiva.

Una hora de Santi con revisión. A partir de ahí, cada historia no dice *"diseñá la
pantalla de clientes"* sino *"aplicá el sistema de `docs/diseno.md` a la pantalla de
clientes"*. Lo primero se discute cinco veces; lo segundo, ninguna.

**Regla para las correcciones de diseño:** se nombra la regla, no la sensación. *"El
alto de fila pasa de 8 a 12"* cuesta una corrección. *"Se ve apretado"* cuesta tres.

**Regla para las revisiones de diseño:** se juzga con la captura, nunca con la
descripción. Las capturas van a `docs/sprint04/evidencia/`, como ya hizo Javi en B1.

---

## Secuencia de los tres días

El orden cambió respecto del replan anterior por una dependencia que estaba sin
declarar: **045 (clientes y proveedores) necesita que 042 y 043 ya existan**, porque
son los componentes que reutiliza. Si Lauti arranca 045 el miércoles, se bloquea.

### Miércoles 28

| Quién | Tarea | SP |
|---|---|---|
| Santi | `docs/diseno.md` acordado y commiteado | — |
| Santi | Merge de #130 y #131, y corrección de la tabla de fases del `ESTADO.md` | — |
| Lauti | **041** control de sesión (login, logout, guarda de ruta) | 3 |
| Javi | **042** componente de listado reutilizable | 5 |

041 no depende de nada y sin él no hay demo posible. 042 se merged el miércoles
mismo, aunque sea al final del día, porque desbloquea a Lauti.

### Jueves 29

| Quién | Tarea | SP |
|---|---|---|
| Javi | **043** componente de formulario reutilizable | 5 |
| Lauti | **045** pantallas de clientes y proveedores (parte de listado) | 3 |

### Viernes 30

| Quién | Tarea | SP |
|---|---|---|
| Javi | **044** confirmación con motivo | 1 |
| Lauti | **045** pantallas de clientes y proveedores (parte de formulario) | — |
| Todos | Integración, capturas de evidencia, cierre del sprint | — |

**Total comprometido: 17 SP.** Por encima de la velocidad medida, y se declara así.

---

## Qué se cae primero si algo se atrasa

En este orden, y la decisión se registra el viernes en el `ESTADO.md` con el motivo:

1. **044** confirmación con motivo (1 SP) — pasa a Sprint 5.
2. **045** parte de formulario (de los 3 SP) — se entrega el listado de clientes y
   proveedores sin alta ni edición.
3. **043** formulario reutilizable (5 SP) — el sprint cierra con login y pantallas de
   solo lectura. Es poco, pero **es demostrable**: Sergio entra, navega y ve datos.

Lo que **no** se cae: 041 y 042. Sin login no hay demo, y sin el componente de
listado las pantallas de Sprint 5 vuelven a discutir el diseño desde cero.

---

## Qué NO entra en estos tres días

Para que quede explícito y nadie lo busque el viernes:

- **Compuerta C3 (`lacentral_demo`)** — no hace falta para desarrollar, sí para que
  Sergio pruebe. Va en la primera semana del Sprint 5, antes del 7/11.
- Scripts 06 (L2 y retiro de Tarjeta3/6) y 07 (`proveedor_articulo`).
- Pista D (los 14 RF, el RF de los 7 CU en colisión, las proformas de E1 a E7, los
  diagramas de secuencia de CU-001, CU-003 y CU-007).
- La deuda técnica abierta, incluidas las dos que salieron esta noche.

---

## Sprint 5 · 2/11 al 13/11

**Objetivo:** el frontend completo de lo construido, validado por el dueño.

**Y una fecha que no es negociable: demo a Sergio el viernes 6/11.**

No al cierre. Si Sergio ve el sistema por primera vez el 13/11, sus correcciones no
tienen dónde entrar y el sprint siguiente arranca rehaciendo. Media hora de demo con
una semana de margen es lo que convierte su opinión en trabajo posible.

Prerrequisito de esa demo: compuerta C3, `lacentral_demo` con datos sembrados. No se
demuestra sobre `lacentral_test` —los tests crean y destruyen esquemas ahí— ni jamás
sobre `lacentral`.

---

## Definición de Terminado · recordatorio de los tres puntos que más se saltan

- **Punto 8** · la proforma de la HU avanza de estado y sus CA quedan marcados. Los
  CA se leen de `docs/historias/`, no se inventan ni se copian del cuerpo del Issue.
- **Punto 9** · revisado por otro integrante. El estado de la revisión es una
  compuerta, no un sentimiento: si hay algo que corregir va "Request changes", no
  "Approve" con comentarios.
- **Punto 10** · verificado contra la base real. No existe un estado intermedio:
  o está verificado, o no lo está y se declara por qué.

---

## Deuda nueva registrada el 27/10

Las dos salieron de la noche de infraestructura y las dos son de la misma familia:
el harness confía en lo que está escrito en la configuración en vez de medir lo que
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

**D-27c · menor.** El prefijo de esquema de test es `pr110_test_`, con el número de
un PR viejo hardcodeado. Debería ser `lacentral_test_`, y la limpieza debería correr
en el `DisposeAsync` aunque el test falle — hay un esquema huérfano de agosto que lo
prueba.

---

## Dónde va cada archivo

```
docs/
  diseno.md                        ← nuevo, paso 0
  sprint04/
    ESTADO.md                      ← se edita: fases, compuertas, secuencia
    REPLAN-26-10.md                ← se conserva como registro
    REPLAN-27-10.md                ← este archivo
    evidencia/                     ← capturas de cada pantalla
  historias/                       ← las 52 proformas (todavía ausentes del repo)
```

El `ESTADO.md` se edita, no se duplica: es la verdad actual del sprint. Los replanes
quedan como rastro de auditoría fechado, que es la misma regla que ya decidieron para
el backlog.
