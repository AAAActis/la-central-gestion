# Re-scaffold de `LaCentral.Data/Models/`

El esquema es **Database First**: los scripts SQL son la fuente de verdad y el modelo
de EF se regenera desde la base. **Prohibido `dotnet ef migrations`.**

## Scripts, en orden

| Script | Qué hace |
|---|---|
| `../base_completa.sql` | Esquema inicial. |
| `01_Ajustes_Sprint3.sql` | D8 (percepciones), D9 (sucursal por línea), D10 (historial de baja 1:N). |
| `02_Modalidades_Precios.sql` | D1: crea `articulo_precio_modalidad`; elimina `articulo_margen` y `articulo.precio_venta_estimado`. |
| `03_Limpieza_Columnas_Fantasma.sql` | Elimina `articulo.codigo`, `descripcion` y `cantidad`, que solo existían en `lacentral_test` (deriva, no esquema). |

Cada script corre en una transacción y no es idempotente. Se aplica a mano: primero
en `lacentral_test`, se verifica, después en `lacentral`.

## ⚠ Antes de scaffoldear: la base de pruebas tiene que coincidir con producción

El scaffold se hace contra la base de **pruebas** (`lacentral_test`), no contra la
principal. Si el esquema de test difiere del de producción, el modelo generado
describe una base que no existe, y el error es de la misma clase que un test sobre
InMemory: todo compila y todo pasa, pero contra una base que no es la real.

Ya pasó una vez: `lacentral_test` tenía tres columnas en `articulo` que
producción no tenía, por una carga de Multisoft contra test. Lo derivado era test, no
producción. Ver el script 03.

Antes de cada re-scaffold, comparar en las **dos** bases, como mínimo, las columnas
de las tablas que cambian, y en lo posible el esquema `public` completo:

```sql
SELECT c.relname AS tabla, a.attname AS columna,
       format_type(a.atttypid, a.atttypmod) AS tipo, a.attnotnull AS no_nula
FROM   pg_attribute a
JOIN   pg_class c     ON c.oid = a.attrelid
JOIN   pg_namespace n ON n.oid = c.relnamespace
WHERE  n.nspname = 'public' AND c.relkind = 'r'
  AND  a.attnum > 0 AND NOT a.attisdropped
ORDER  BY tabla, columna;
```

Si las salidas difieren, **no se scaffoldea**. Primero hay que decidir cuál base está
mal y corregirla con un script versionado.

El fixture de tests no sirve como red para esto: solo verifica que lo que EF mapea
exista en la base, no que la base no tenga columnas de más.

## Comando

Desde `backend/LaCentral.Data/`:

```powershell
dotnet ef dbcontext scaffold "<CADENA-DE-CONEXION-DE-TEST>" Npgsql.EntityFrameworkCore.PostgreSQL `
  --context LaCentralDbContext --context-dir . `
  --output-dir Models `
  --namespace LaCentral.Data.Models --context-namespace LaCentral.Data.Models `
  --schema public --no-pluralize --no-onconfiguring --force
```

- La cadena es la de `LACENTRAL_TEST_PG`: cómo se configura está en
  [`docs/entorno-local.md`](../../../docs/entorno-local.md). **Nunca** pegues la cadena
  real en un archivo versionado. En PowerShell: `"$env:LACENTRAL_TEST_PG"`.
- `--schema public` es obligatorio: la base de test tiene además el esquema `staging`,
  y en una corrida de tests abortada pueden quedar esquemas `pr110_test_*` huérfanos.
  Sin el filtro, el scaffold los incluye.
- Requiere `dotnet-ef` en la misma versión que EF Core del proyecto (hoy 10.0.11).

## Dos trampas

1. **`--force` no borra el archivo de una tabla eliminada.** Sobrescribe lo que
   regenera, pero el `.cs` de una tabla que ya no existe queda en `Models/` y sigue
   compilando. Después de un script que borra una tabla, hay que borrar a mano ese
   archivo. Por ejemplo, `ArticuloMargen.cs` después del 02. Eso no es editar un modelo:
   es completar lo que `--force` no hace.
2. **`Models/HistorialesBaja.Personalizaciones.cs` es código manual dentro de la
   carpeta generada.** El scaffold no lo toca: no lo sobrescribe ni lo borra. Agrega
   las colecciones 1:N de los historiales de baja, que el scaffold interpreta como 1:1
   por el índice único parcial (D10). La configuración que las usa está en
   `LaCentralDbContext.Personalizaciones.cs`. Es deuda: debería vivir fuera de
   `Models/`.

## Después del scaffold

1. Revisá el diff con `git diff --ignore-cr-at-eol`: el scaffold reescribe todos los
   archivos con otros fines de línea, y git los normaliza al hacer `add`
   (`core.autocrlf`). El diff real tiene que contener solo lo que cambió el script.
2. `dotnet build --no-incremental` y `dotnet test` desde `backend/`: 0 warnings,
   0 omitidos.
