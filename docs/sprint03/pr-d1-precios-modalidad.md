# PR · Sprint 3 — Compuerta D1: precios por modalidad en la aplicación

**Rama:** `feature/d1-precios-modalidad` → **`feature/d10-historial-baja`** (base provisoria)
**Título sugerido:** `feat: re-scaffold D1 y conversión texto→Modalidad (compuerta de esquema)`

> ⚠️ **Base provisoria.** Esta rama sale de `feature/d10-historial-baja` (Fase 2, `dabcde4`), que
> todavía está en PR sin mergear (y esa, a su vez, sobre la Fase 1). **Cuando la Fase 2 entre a
> `develop`, hay que cambiar la base de este PR a `develop` y revisar el diff ahí.** Tienen que
> quedar solo los commits de esta rama: `74f47ed`..`1351393` más el del cuerpo del PR.

## Qué cambió en la base (scripts del equipo, aplicados a mano)

Están en `backend/db/rescaffold/`. El agente no los escribió:

| Script | Dónde | Cambio |
|---|---|---|
| `02_Modalidades_Precios.sql` | `lacentral_test` y `lacentral` | Crea **`articulo_precio_modalidad`** `(articulo_id, modalidad, porcentaje, precio, calculo_manual, fecha_calculo, usuario_id)`: PK `(articulo_id, modalidad)`, FK a `articulo` (CASCADE) y a `usuario`, 7 CHECK. Elimina **`articulo_margen`** (0 filas en las dos bases, sin migración) y **`articulo.precio_venta_estimado`**. |
| `03_Limpieza_Columnas_Fantasma.sql` | solo `lacentral_test` | Elimina `articulo.codigo`, `descripcion` y `cantidad`: estaban solo en la base de test (0 de 2644 filas con dato) y en ningún script. Producción no las tenía. |

## Qué hace

| Commit | Cambio |
|---|---|
| `09f4bc1` | Versiona el script 03, que estaba aplicado pero no en el repo. |
| `7801bbd` | **Re-scaffold** de `LaCentral.Data/Models/` contra `lacentral_test`. Antes se verificó en el catálogo que `public.articulo` tiene las 10 columnas de los scripts. Cambios: se genera `ArticuloPrecioModalidad`, `Articulo` pierde `PrecioVentaEstimado` y no trae `codigo`, `descripcion` ni `cantidad`, y aparece la colección inversa en `Usuario`. **`ArticuloMargen.cs` se borró a mano**, porque `--force` no borra el archivo de una tabla eliminada. Las personalizaciones 1:N de los historiales no se tocaron. El resto de los archivos solo cambian en los fines de línea, que git normaliza. |
| `a996e21` | Refactor en verde: `PrecioModalidadConsultaDto.Modalidad` pasa de `string` a `Modalidad`. |
| `91a705e` | **Test en rojo**, contra PostgreSQL. |
| `d8b6814` | **Producción:** `ModalidadPersistida.Leer` y su uso en `ArticuloRepositorio.ObtenerDetallePorIdAsync`. |
| `2144ef5` | `backend/db/rescaffold/README.md` con el comando de scaffold, la advertencia test/producción y las dos trampas. El README de tests aclara que el fixture compara el esquema en un solo sentido. |
| `1351393` | `ESTADO.md`: compuerta D1 cerrada y deuda nueva. |

### Dónde va la conversión texto ↔ `Modalidad`, y por qué

**En el mapeo del repositorio** (`backend/LaCentral.Data/Repositorios/ModalidadPersistida.cs`), no como
value converter en `LaCentralDbContext.Personalizaciones.cs`:

1. **Un value converter no tiene dónde engancharse sin tocar lo generado.** El scaffold declara
   `public string Modalidad`. `HasConversion` convierte entre el tipo de la propiedad CLR y el de la
   columna. Para que EF entregue un enum, la propiedad tendría que ser `Modalidad`, y eso es editar
   `Models/`. La otra salida es ignorar la propiedad generada y declarar una nueva en un `partial`,
   que es otro archivo manual en `Models/`: la deuda que ya tiene `HistorialesBaja.Personalizaciones.cs`.
   Un converter `string → string` que solo valide sería una segunda capa sin enum.
2. **El límite Core-Driven está donde debe.** El modelo de persistencia habla de texto, porque así
   es la columna. Lo que sale del repositorio hacia UseCases ya es `Modalidad`, y la conversión
   ocurre una sola vez, en el borde. `LaCentral.UseCases` sigue sin referencias.
3. **La conversión es explícita y verificable.** Compara por nombre exacto (`StringComparer.Ordinal`)
   contra los seis miembros del enum y lanza `InvalidOperationException` con el valor leído. **No usa
   `Enum.Parse`**, que acepta `"3"`, espacios alrededor y (con `ignoreCase`) `"lista"`: ninguno de esos
   es un valor del esquema.

**Falla, no devuelve un default.** Un valor fuera de los seis es un dato corrupto: el CHECK
`articulo_precio_modalidad_valida` lo impide, así que solo aparece si alguien quita el CHECK o escribe
por fuera. No es un caso de negocio, y por eso es una excepción y no un `Result<T>`.

El sentido inverso (`Modalidad` → texto) **no entra en este PR**: no hay ninguna escritura en
`articulo_precio_modalidad` todavía. Entra con la primera escritura (Fase 3 o 6), con su propio test.

### `Articulo.CalcularPrecioEstimado`

**Sobrevive y no cambia.** Calcula en memoria, sobre `CostoVigente` y `Margenes`. **Ningún código
esperaba persistir su resultado.** No hay repositorio, controlador ni caso de uso que escriba
`PrecioVentaEstimado`, ni en esta rama ni en ninguna otra local o remota: se buscó en todas las refs.
Hasta `20e7c22`, `PrecioVentaEstimado` existió como propiedad calculada del dominio, pero nunca se
mapeó a la columna. Por eso la solución compiló sin cambios después del re-scaffold.

Lo que **sí** queda pendiente para la Fase 3: el modelo "porcentaje sobre costo" de `MargenModalidad`
no expresa Efectivo (sobre Lista) ni CasoEspecial (precio fijo). La tabla nueva tiene dónde guardarlos.

## Los 42 tests en rojo

Tenían **una sola causa**: el modelo pedía `articulo.precio_venta_estimado`, que ya no existe (42703).
Con el re-scaffold, la suite dio **176/176** sin ningún otro cambio. No apareció ninguna falla por otra
causa. Ningún test, repositorio, DTO ni el fixture referenciaba `articulo_margen` o
`precio_venta_estimado`. Los README tampoco.

## TDD

- `91a705e` (solo test) → `d8b6814` (solo producción):
  - `Detalle_LeeLasSeisModalidadesPersistidasPorNombre`: rojo en L53 (`Assert.NotNull`: el repositorio
    devolvía `PreciosPorModalidad = null`). Siembra las seis, en un orden distinto del enum, y relee
    con un contexto nuevo. Comprueba el orden del enum y que Tarjeta3 y Tarjeta6 vuelvan con precio
    `null`, no cero.
  - `Detalle_ModalidadFueraDeLasSeis_FallaAlLeerEnVezDeConvertirseEnUnaValida`: rojo en L94 (no se lanzó
    ninguna excepción). Quita el CHECK **solo en el esquema aislado**: antes afirma que
    `current_schema()` es `pr110_test_<32 hex>`. Después siembra `"3"`, `"lista"`, `" Lista"`,
    `"Lista "`, `"Mayorista"` y `""`. Cada uno tiene que fallar al leer y nombrar el valor en el mensaje.
  - **Mutación** (no commiteada): con `Enum.TryParse(valor, ignoreCase: true)` en lugar de la búsqueda
    exacta, el segundo test vuelve a rojo en L94.
- **Excepción declarada:** `Esquema_RechazaModalidadFueraDeLasSeis` es de caracterización del CHECK
  (23514, `articulo_precio_modalidad_valida`, la fila no queda). Pasa ya en el commit de test: prueba
  el esquema, no código de esta rama.
- `7801bbd` (re-scaffold) y `a996e21` (refactor) no tienen commit de test propio. El primero es código
  generado y lo cubre la suite existente, que pasó de 42 rojos a verde. El segundo es un cambio de tipo
  sin productores de valores.

## Qué NO hace

- No toca el esquema. No edita a mano ningún modelo de `Models/`: borrar `ArticuloMargen.cs` completa
  lo que `--force` no hace.
- No mueve `Models/HistorialesBaja.Personalizaciones.cs` fuera de `Models/`. Queda como deuda y es un
  cambio aparte.
- No define qué se muestra cuando falta una modalidad: sin filas, `PreciosPorModalidad` sigue en
  `null`. Eso es HU-ART-02 (Fase 4), igual que serializar el enum por nombre en HTTP.
- No alinea el dominio `Articulo` con la tabla nueva: eso es la Fase 3.

## Deuda nueva (detalle en `ESTADO.md`)

- **Proceso:** `lacentral_test` se fue del esquema de los scripts y nadie lo notó. Producción **no**
  estaba derivada. El scaffold se hace contra test, así que antes de cada re-scaffold hay que comparar
  test contra producción (consulta en el README de `rescaffold/`). El fixture no detecta columnas de
  más en la base.
- `Models/HistorialesBaja.Personalizaciones.cs`: código manual dentro de la carpeta generada.

## Cómo se verificó (2026-10-14)

- `dotnet build --no-incremental`: **0 advertencias, 0 errores**.
- `dotnet test`: **179 total · 179 passed · 0 failed · 0 omitidos**. La base de la rama tenía 176, y
  42 en rojo después de aplicar el 02.
- **Contra PostgreSQL real: 46 tests** (antes 43). Son 45 con esquema aislado, confirmados en el StdOut
  del `.trx`, y 1 de catálogo.
- Subagentes sobre `git diff dabcde4..1351393`:
  - **verificador-tdd: APTO.** En su propio worktree reprodujo el rojo de `91a705e` (L53 y L94, por
    aserción) y el verde de `d8b6814`. Confirmó la mutación declarada y agregó una suya: `default` en
    lugar de lanzar también vuelve a rojo en L94. Suite: 179/179, 0 warnings, 46 `[PostgreSqlFact]`.
    No reprodujo los 42 rojos previos al re-scaffold.
  - **auditor-persistencia: APTO.** Contra el catálogo de `lacentral_test`, el modelo coincide con
    `articulo_precio_modalidad` (7 columnas, PK, 2 FK, 7 CHECK) y con `articulo` (10 columnas, comparadas
    en las dos direcciones). Relectura con contexto nuevo, sin Ids sembrados, y el `DROP CONSTRAINT` no
    puede alcanzar `public`. Suite: 179/179. **Hallazgos no bloqueantes**, que pasan a bloquear la fase
    que agregue la escritura en la tabla:
    - **H1:** sin test contra el motor de los otros 6 CHECK, la PK (23505), las FK (23503) ni el CASCADE.
    - **H2:** `MargenModalidad` no transporta `Precio`, `CalculoManual`, `FechaCalculo` ni `UsuarioId`,
      así que no puede cumplir `precio_con_fecha` ni `manual_con_usuario`. Además, `CalcularPrecioEstimado`
      es una segunda regla de cálculo.
    - **H3:** porcentaje, manual, fecha y usuario se insertan, pero no se releen por valor, porque el DTO no
      los expone.
    - **H4:** el fixture compara en un solo sentido. Ya está documentado.
    - Sin verificar: `lacentral`, porque está fuera del alcance de los tests. Si el 02 no estuviera aplicado
      ahí, el `Include` de `ArticuloRepositorio.cs:93` daría 42P01. Según el equipo, está aplicado.
    - Mejora menor: calificar con el esquema el `ALTER TABLE` del test, en lugar de depender del `search_path`.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
