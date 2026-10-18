# Entorno local

Configuración que **no se versiona** porque depende de cada máquina, pero que sin
ella el proyecto no se trabaja igual. Cada uno la arma una vez en su equipo.

> **Nunca pongas una cadena de conexión real, una contraseña ni una ruta con tu
> usuario en un archivo versionado.** En este documento todo eso va como
> marcador de posición.

---

## 1. Lo que ya viene en el clon

`.claude/agents/` tiene los dos subagentes verificadores y **sí está versionado**:

| Subagente | Qué hace |
|---|---|
| `verificador-tdd` | Revisa que el ciclo TDD se haya respetado de verdad: hace checkout de cada commit de test y confirma que estaba en rojo por la razón correcta. |
| `auditor-persistencia` | Revisa que todo lo que toca la base esté verificado contra PostgreSQL real y no contra InMemory o mocks. |

Ninguno de los dos tiene `Edit` ni `Write`. Solo leen y reportan: un verificador que
puede arreglar lo que encuentra deja de ser verificador.

No hay nada que configurar. Aparecen solos si abrís Claude Code **desde la raíz del
repo**, no desde `backend/`: la configuración se lee del directorio donde arrancás.

---

## 2. Hooks de graphify — opcional, recomendado

### Qué hacen

Graphify arma un grafo del código (clases, métodos, quién llama a quién) y lo
consulta sin leer archivos enteros. Los hooks interceptan `Read`, `Glob`, `Grep` y
`Bash` y le recuerdan al agente que consulte el grafo antes de abrir archivos. Eso
baja bastante el consumo de tokens en sesiones largas.

### Por qué no está versionado

El `settings.json` necesita la ruta absoluta del ejecutable, que es distinta en cada
máquina. Y hay algo peor: un hook de `PreToolUse` que falla **bloquea la llamada a
la herramienta**. Si versionáramos el archivo con la ruta de otro, el primer `Read`
de tu sesión se cuelga sin explicarte por qué.

Por eso `.claude/settings.json` está en el `.gitignore` y lo armás vos.

### Cómo armarlo

1. Instalá graphify según su README.

2. Averiguá dónde quedó el ejecutable:

```powershell
   Get-Command graphify | Select-Object -ExpandProperty Source
```

3. Creá `.claude/settings.json` en la **raíz del repo** con este contenido,
   reemplazando `<RUTA-A-GRAPHIFY>` por lo que devolvió el comando anterior.
   En Windows usá barras normales (`/`), no invertidas:

```json
   {
     "hooks": {
       "PreToolUse": [
         {
           "matcher": "Bash|Grep",
           "hooks": [
             { "type": "command", "command": "<RUTA-A-GRAPHIFY> hook-guard search", "timeout": 10 }
           ]
         },
         {
           "matcher": "Read|Glob",
           "hooks": [
             { "type": "command", "command": "<RUTA-A-GRAPHIFY> hook-guard read", "timeout": 10 }
           ]
         }
       ]
     }
   }
```

4. Generá el grafo, desde la raíz del repo:

```powershell
   graphify update .
```

   **La carpeta del grafo se crea en la raíz del repo, no en `backend/`.** Graphify
   sube hasta la raíz de git. Si te aparece una adentro de `backend/`, borrala: la de
   la raíz es la buena, porque además ve `docs/` y el `README.md`.

5. Verificá que el hook se dispare: abrí Claude Code y pedile que lea cualquier
   archivo. Tiene que correr el hook antes del `Read`.

El grafo se desactualiza con cada commit. Corré `graphify update .` en una terminal
aparte, no en la de Claude Code.

---

## 3. `LACENTRAL_TEST_PG` — obligatorio

**Sin esta variable, 17 tests te dan en rojo.** No es un error de tu entorno: desde
la Fase 1 del Sprint 3 los tests marcados `[PostgreSqlFact]` **fallan** en vez de
saltearse cuando no hay conexión. Antes se omitían y la suite mostraba "todo verde"
sin haber probado la persistencia: así se nos escaparon dos bugs críticos.

La cadena de conexión apunta al contenedor de pruebas por Tailscale. **Pedísela a
Santi por un canal privado.** No está en el repo y no va a estar.

Para dejarla persistente en Windows, una sola vez:

```powershell
[Environment]::SetEnvironmentVariable("LACENTRAL_TEST_PG", "<CADENA-QUE-TE-PASEN>", "User")
```

Cerrá la terminal y abrí una nueva: `SetEnvironmentVariable` no afecta a la sesión
en curso. Después confirmá:

```powershell
$env:LACENTRAL_TEST_PG
```

### Reglas de la base de pruebas

El contenedor tiene **datos reales importados de Multisoft**: 124 proveedores y
2108 clientes. Hay tests (`PostgreSqlSeguridadTests`) que impiden apuntar a la base
principal.

- No los desactives ni los eludas.
- Ningún `TRUNCATE` ni `DROP` fuera del esquema exclusivo de pruebas.
- La base de test se llama `test_*` o `*_test`. Si tu cadena apunta a otra cosa,
  está mal.

---

## 4. Verificación final

Desde la raíz del repo:

```powershell
dotnet build --no-incremental
dotnet test
```

Tiene que dar **0 warnings** y **0 skipped**. Si ves tests omitidos, te falta la
variable del punto 3. Si ves warnings, avisá: la suite se dejó en cero a propósito
y un warning nuevo es deuda que entró sin que nadie la viera.