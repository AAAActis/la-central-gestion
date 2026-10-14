# PR · Fase 3 Sprint 3 — HU-ART-01 · Alta de artículo

**Rama:** `feature/HU-ART-01-alta-articulo` → **`develop`**
**Título sugerido:** `feat: alta de articulo con precios por modalidad y base de calculo (HU-ART-01)`
**Issue:** Refs #14 (ver "Qué NO hace": falta el entregable en frontend)

## Qué hace

### Esquema (script del equipo, no lo escribió el agente)
- `04_Modalidades_Base_Calculo.sql`, aplicado en `lacentral_test` y `lacentral`. Agrega `articulo_precio_modalidad.base` (`CostoArticulo` | `PrecioLista`) y deja **10 CHECK**: `porcentaje_rango` (>= -100) reemplaza a `porcentaje_no_negativo`, y entran `base_valida`, `base_coherente` y `base_sin_autorreferencia`.
- Antes del re-scaffold se comparó la huella md5 del esquema `public` de test contra la de producción: `14665b6c…` en las dos. El re-scaffold (`d9ff67d`) solo trae `Base` y los comentarios nuevos.

### Decisiones A a D del equipo
| | Cambio |
|---|---|
| A | `MargenModalidad` desaparece. **`PrecioModalidad`** (`UseCases/Entidades/`) espeja la fila: Modalidad, Base, Porcentaje, Precio, CalculoManual, FechaCalculo, UsuarioId. Enum nuevo **`BasePrecio { CostoArticulo, PrecioLista }`**. |
| B | `Articulo.CalcularPrecioEstimado` desaparece. **`ReglasPrecioModalidad.Calcular`** es la única regla: `valor_de_la_base × (1 + porcentaje/100)`, con redondeo AwayFromZero. Devuelve null si el precio es manual, si no hay porcentaje o si falta el valor de la base, y nunca reemplaza una base por otra. |
| C | **`ReglasPrecioModalidad.ConfiguracionInicial`**: las seis filas con que nace un artículo. Lista CostoArticulo +50, Costo CostoArticulo 0, Efectivo PrecioLista -10, Tarjeta3/6 sin base ni porcentaje, CasoEspecial manual. Todas con precio null. |
| D | `ModalidadPersistida` convierte también texto ↔ `BasePrecio`, con el mismo criterio: nombre exacto, excepción si el valor no es uno de los dos, sin defaults. Se agregan `Escribir`/`EscribirBase`: un valor del enum sin nombre lanza. |

### HU-ART-01
- **`ArticuloValidaciones`** (estática, para el alta y la modificación):
  - nombre de 1 a 200 caracteres, ubicación de hasta 60;
  - código normalizado de 1 a 60 caracteres, con proveedor indicado;
  - la misma clave normalizada dos veces en la petición → Conflicto.
- **`CrearArticuloUseCase`:**
  - proveedor inexistente o inactivo → Invalido;
  - dos códigos del mismo proveedor → Conflicto (D2);
  - código ya usado → Conflicto, informando **cuál** (código interno y nombre del artículo dueño), tanto si choca con un código alternativo como con un código interno;
  - los códigos se guardan con `NormalizacionArticulo.Codigo`, la misma función que usa la búsqueda;
  - stock cero en cada sucursal, las seis filas de precio, costo null.
- **Código interno = `LC-{id:D6}`, derivado de la identidad.** No hay MAX+1 ni cambio de esquema:
  - el alta inserta con un `TMP-<guid>` provisional y, en la misma unidad de trabajo, `AsignarCodigoInternoAsync` lo reemplaza por el definitivo;
  - ningún código importado empieza con `LC-`;
  - si dos altas simultáneas pasan la validación de códigos, la carrera la cierran `codigo_unico_global` y `un_codigo_por_proveedor`, que el manejador ya traduce a 409.
- **`IArticuloRepositorio`** se redefinió para el alta: `AgregarAsync` (devuelve el Id generado), `AsignarCodigoInternoAsync` y `ObtenerSucursalesAsync`. El borrador anterior no tenía implementación ni usos.
- **`POST /api/articulos`** con `[Authorize(Roles = "ADMINISTRADOR,OPERADOR")]`. Responde **201** con `{id, codigoInterno}` y `Location: /api/articulos/{id}`. Se agrega `MapeoResultado.ACreadoHttp`. Los casos de uso se registran en `Program.cs`.

### Hallazgos del auditor que bloqueaban esta fase
- **H1:** un test contra el motor por cada restricción. Son los 9 CHECK que no tenían test (`modalidad_valida` ya lo tenía), la PK (23505), las 2 FK (23503) y el ON DELETE CASCADE. Cada fila viola **una sola** restricción, y el test afirma el SQLSTATE y el nombre.
- **H2:** resuelto con A y B.
- **H3:** el DTO de detalle expone base, porcentaje, `calculo_manual`, fecha y usuario. El test compara el DTO completo releído con un contexto nuevo (fecha con microsegundos, porcentaje -10).

## Qué NO hace
- **No tiene entregable visible en `frontend/`.** La carpeta no existe en el repo. El requisito permanente ("toda fase que agregue endpoints cierra con un entregable en frontend/") se agregó al plan en esta sesión, al cerrar la fase. **Queda para que decida el equipo.**
- No hay `GET /api/articulos/{id}`: es de la Fase 4. El `Location` ya apunta a esa ruta.
- No hay forma de fijar el precio de CasoEspecial ni las tasas de Tarjeta3/6.
- **"Sucursal habilitada":** la tabla `sucursal` no tiene esa marca, así que el stock cero va a todas las sucursales (hoy FR y SV).
- No mueve `Models/HistorialesBaja.Personalizaciones.cs`, que sigue como deuda.
- `CalcularPreciosArticuloUseCase` (el recálculo) es de la Fase 6. El alta no calcula precios porque nace sin costo.

## Tests reapuntados (decisión B), ninguno borrado
`ArticuloPrecioEstimadoTests` conserva sus 8 casos y los nombres, apuntando a `ReglasPrecioModalidad.Calcular`. Dos de ellos ya no tienen sentido literal con la fórmula con base:
- `CalcularPrecioEstimado_SinMargenes_RetornaNull`: ya no existe la lista de "márgenes cargados". Ahora prueba Tarjeta3 sin tasa (base y porcentaje NULL).
- `CalcularPrecioEstimado_ModalidadSinMargenCargado_RetornaNull`: ahora prueba CasoEspecial manual.

Se agregan 3 de la base: CostoArticulo ignora PrecioLista; Efectivo -10 sobre PrecioLista; y PrecioLista sin precio de Lista da null aunque haya costo.

## TDD
Cada unidad tiene tres commits:
1. **Estructura** (`chore`, sin comportamiento): firmas que compilan y devuelven vacío o null, para que el rojo sea por aserción y no por compilación.
2. **Test en rojo**, verificado por aserción.
3. **Producción**, en un commit aparte.

| Unidad | estructura → test (rojo) → producción |
|---|---|
| H3 + D | `e2bc35f` → `413308a` (L58, L101) → `362b870` |
| B + A | `1d96eb0` → `fce502c` (9/13) → `7677fb8` |
| C | (`1d96eb0`) → `eb22ab0` (L17, L33, L47) → `d8a1064` |
| Caso de uso, Moq | `568a29b` → `db51be9` (17) → `442aeff` |
| Repositorio, PG | `c0ff00d` → `c133b8f` (4) → `2bb8266` |
| Controlador, HTTP + PG | `127ab20` → `ef3b6a8` (2) → `d9ae80f` |

**Excepciones declaradas** (pasaron a la primera). Cada una se verificó con una mutación no commiteada que la pone en rojo:
- Los 4 tests de la fórmula que esperan null pasan contra el esqueleto, que devuelve null. Tres mutaciones los ponen en rojo:
  - costo nulo → 0;
  - `PrecioLista ?? costo`;
  - `porcentaje ?? 0` con base nula → costo.
- **H1** (`9d43f88`) es caracterización del esquema. La mutación quita la restricción bajo prueba en el esquema aislado antes de insertar, y quita la FK antes del DELETE: los 13 dan rojo.
- Hay 3 tests PG (D2, código de otro artículo y proveedor inactivo) que resuelve el caso de uso ya commiteado. Mutaciones:
  - sin chequeo D2 → 23505 de `un_codigo_por_proveedor`;
  - sin búsqueda de conflictos → 23505 de `codigo_unico_global`;
  - sin chequeo de activo → Values differ.
- El **403** pasa a la primera porque el `[Authorize(Roles)]` entró con la estructura. Mutación: `[Authorize]` sin roles → rojo.

La regla del plan dice "un test que pasa a la primera → parar". Para esta fase, el equipo la reemplazó por "declarar y verificar con mutación".

## Cómo se verificó (2026-10-14, HEAD `c6dc60b`)
- `dotnet build --no-incremental`: **0 advertencias, 0 errores**.
- `dotnet test`: **227 total · 227 passed · 0 failed · 0 omitidos**. develop tenía 179, y al aplicar el 04 tenía 1 rojo por el CHECK `base_coherente`: la siembra de un test no lo cumplía, y se corrigió en `413308a`.
- **Contra PostgreSQL real: 71** (antes 46). Son 70 con esquema aislado, confirmados en el StdOut del `.trx`, y 1 de catálogo. El test HTTP levanta la API con `WebApplicationFactory` y el `DbContext` en el esquema aislado, sin mocks.
- `LaCentral.UseCases.csproj` sigue sin referencias. Ningún controlador importa `LaCentral.Data`.
- Subagentes sobre `git diff f3c6705..c6dc60b`, corridos de a uno (detalle completo en `ESTADO.md`):
  - **verificador-tdd: NO APTO.** Los rojos, el orden test → producción y las mutaciones declaradas se reproducen. Bloquean 4 mutaciones que sobreviven con la suite en verde:
    - `ArticulosController` sin test de alta sin códigos;
    - la rama `creado is null` de `CrearArticuloUseCase`;
    - el guard `Enum.IsDefined` de `ModalidadPersistida`;
    - las DataAnnotations del DTO HTTP sin test de 400.
  - **auditor-persistencia: APTO.** H1, H2 y H3 están cerrados, ningún `TMP-` sobrevive y el alta es atómica en las cuatro tablas. Los hallazgos no bloquean.

> ⚠️ **Este PR NO se puede abrir hasta que el verificador dé APTO.**

## Pendiente (detalle en `ESTADO.md`)
- El entregable en `frontend/`: lo decide el equipo.
- La Fase 4 tiene que serializar `Modalidad` y `BasePrecio` por nombre en HTTP.
- La comparación del esquema test/producción depende del equipo: el agente no tiene acceso a `lacentral`.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
