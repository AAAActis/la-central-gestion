## 043 · HU-UI-05 · Componente de formulario reutilizable

Rama `feature/hu-ui-05-formulario-reutilizable`. Sale de `feature/hu-ui-fase1-fundamentos-visuales`,
que todavía no está en develop, porque necesita sus tokens y el botón en carga. **Cuando la Fase 1
entre en `develop`, esta rama se rebasea sobre `develop` y el diff se revisa ahí.**

Los CA salen de `docs/historias/043-HU-UI-05.md` (v1.1). **HU-UI-04 (el listado, HU 042) sigue en
curso**: Javi la está haciendo en paralelo. Su precondición se quitó de la proforma en la v1.1
porque era un resto del plan viejo: el formulario no usa el listado.

### Qué se hizo

Todo vive en `frontend/src/app/compartido/formulario/`. No se tocó nada fuera de ese directorio:
ni `styles.css`, ni el README de `app/`, ni pantallas.

- **`Formulario`** (`app-formulario`) se arma a partir de una lista de campos
  (`CampoFormulario[]`). El campo sigue la anatomía de la §8 de `SISTEMA-VISUAL.md` y el botón,
  la §7. Recibe los errores del servidor por dos entradas separadas, `erroresPorCampo` y
  `errorGeneral`, y no traduce ningún texto.
- **`erroresDelServidor()`** reparte una respuesta de error de la API:
  - los `errors` de un 400 de validación van a su campo, según la propiedad raíz y sin
    distinguir mayúsculas. Las claves son las del contrato que fija `ArticulosApiPostgreSqlTests`;
  - el `detail` de un ProblemDetails va al mensaje general.

### Criterios de aceptación

| CA | Estado | Prueba |
|---|---|---|
| CA-001 · completo y válido → envía y confirma | Verificado | `CA-001 · completo y válido…` |
| CA-002 · obligatorio vacío o formato inválido → bloquea, indica el campo, no llama a la API | Verificado | dos tests `CA-002 · …` |
| CA-003 · agregar o quitar un multivaluado sin afectar el resto | Verificado | `CA-003 · agregar y quitar un teléfono…` |
| CA-004 · motivo del servidor junto al campo | **Parcialmente verificado** | ver abajo |

**CA-004, parcialmente verificado.** Para los errores de **validación de modelo** (los `errors`
por propiedad de un 400) está verificado. Para los de **regla de negocio** (409 y 400 de un caso
de uso) **no**: hoy la API devuelve el motivo como `detail` de texto, sin decir a qué campo
corresponde, así que va al mensaje general del formulario.

Queda pendiente una decisión de backend, registrada en `ESTADO.md`: agregar el campo como
miembro de extensión del ProblemDetails en `MapeoResultado.Falla`, para `Conflicto` e `Invalido`.
Cuando esté, el componente no cambia: cambia lo que la pantalla le pasa en `erroresPorCampo`.

Además, con una prueba cada uno:
- etiqueta asociada por for/id;
- ayuda debajo y vinculada con `aria-describedby`;
- error junto al campo, vinculado y con `role="alert"`;
- envío con el botón en carga, sin doble envío;
- error general con `role="alert"`;
- valores iniciales para modificar.

### Ciclo

`chore` (estructura) → `test` (13 en rojo, todos por aserción: los helpers fallan con `fail()` y
un motivo, no con `TypeError`) → `feat`. Mutaciones sin commitear, cada una en rojo:
- sin la guarda de envío en curso;
- enviar con el formulario inválido;
- error sin `aria-describedby`;
- error de campo sin `role=alert`;
- `label` sin `for`;
- sin `markAllAsTouched`;
- números enviados como texto;
- quitar que vacía toda la lista;
- ignorar los errores del servidor;
- mapeo sensible a mayúsculas;
- sin validar el formato.

La señal `intentado` sobrevivió a su mutación, porque `markAllAsTouched` ya la cubre, y se borró.

### Verificación

- `npx ng build`: sin errores ni warnings.
- `npx ng test --watch=false --browsers=ChromeHeadless`: **48/48** (35 previos + 13 nuevos).
  Corre solo en Chromium.
- Sin hexadecimales ni medidas a mano en el componente: todo es `var(--token)`.

### Qué no se hizo y por qué

- **Ninguna pantalla usa el formulario todavía.** Lo van a usar las HU 045 y siguientes. Por eso
  no hay capturas ni medición de desplazamiento horizontal: no hay pantalla que lo monte, y no se
  crean pantallas nuevas.
- No hay campos de selección (`select`) ni de texto largo. Ningún CA los pide; se agregan cuando
  una pantalla los necesite.
- El README de `frontend/src/app/` no menciona el formulario: está fuera de este directorio y
  Javi lo puede tocar por la HU 042.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
