# PR #110 — corrección local de tests PostgreSQL

Base revisada: `594df1f`, rama remota `fix/saneamiento-bd-d10`.
Rama de corrección: `fix/pr110-tests-postgresql`. Sin merge ni publicación.

## Cambios y alcance

- ClienteRepositorioTests y ProveedorRepositorioTests ejecutan sus casos de
  uso/repositorios con Npgsql, nunca mediante fallback InMemory. Guardan sin
  Id asignado, toman los valores generados y releen con otros contextos.
- Verifican baja, motivo/fecha, cierre al reactivar, modificación ordinaria,
  segunda baja y conservación del primer registro. Se mantiene el primer
  historial tracked al registrar la segunda baja, para detectar la inferencia
  1:1 incorrecta del scaffold. Dos bajas abiertas deben fallar por unicidad.
- ArticuloRepositorioTests genera Id de artículo, proveedor y códigos al
  guardar; carga relaciones por navegaciones. Sucursal se toma del catálogo
  existente de la plantilla: el mapeo la declara ValueGeneratedNever.
- ArticuloBusquedaPostgreSqlTests conserva los 40 artículos y las aserciones
  de búsqueda, pero elimina el esquema mínimo artificial y los INSERT con Id.
  Usa exactamente la misma infraestructura y claves generadas que los otros.
- Las cuatro relaciones de historial se corrigen como 1:N en archivos partial,
  ignorando la navegación singular inferida por scaffold y agregando colecciones.
  El DbContext y modelos generados permanecen intactos. Los cuatro tests de
  modelo verifican FK no única, navegación de colección, ausencia de claves
  shadow y conservación del índice único parcial de baja abierta.
- No se modifican los IDs de mocks/casos de uso ni el Id=999 de la prueba de
  usuario inexistente: no son entidades insertadas en columnas identity.

## Esquema e aislamiento

La fixture exige una conexión explícita a base exclusiva `test_*` o `*_test`.
Rechaza nombres principal/producción/prod y comprueba `current_database()`.
No utiliza la conexión normal de la API ni crea/modifica la base principal.

Con acceso disponible, lee el catálogo de la plantilla del test y usa
`GetTableName`/`GetColumnName` del modelo EF, no nombres de DbSet. Verifica
columnas, tipos/nulabilidad, identity ALWAYS, FK y el índice parcial D10.
Copia tablas con `LIKE INCLUDING ALL` a `pr110_test_<guid>`: conserva tipos,
default, CHECK, identity con secuencias propias e índices. Agrega explícitamente
las FK reales redirigidas a esa copia; no elimina ni relaja restricciones.
Solo copia los datos de catálogos Rol/Sucursal, cuyas claves no son identity.

Las tablas/fuentes reales no se alteran. Cada fixture limpia únicamente su
esquema generado. Si hay triggers de usuario, defaults serial que compartan
secuencias, FK fuera del conjunto, o una estructura incompatible, falla antes
de cargar datos: requiere revisar el SQL real, no deshabilitar restricciones.

## Configuración

- `LA_CENTRAL_TEST_POSTGRES`: conexión exclusiva de test, fuera del repositorio.
- `LACENTRAL_TEST_PG`: alias compatible con el PR. Si ambas existen con valores
  distintos, la fixture rechaza la ambigüedad.
- `LA_CENTRAL_TEST_SCHEMA`: esquema plantilla; default `public`, siempre dentro
  de la base exclusiva de test. Debe contener el esquema aplicado de D8–D10.
- `LA_CENTRAL_REQUIRE_POSTGRES=1`: en validación obligatoria/CI, no permite
  omitir los tests por falta de conexión; esa ausencia se convierte en fallo.

El usuario de test necesita leer el esquema fuente y crear/eliminar su propio
esquema. No hacen falta librerías ni extensiones nuevas desde esta entrega.

## Comandos ejecutados y resultado real

Se usó .NET SDK 10.0.401 y DOTNET_CLI_HOME en una carpeta temporal para no tocar
estado interno de Codex. Se ejecutó primero la selección y luego la suite.

```powershell
dotnet test backend/LaCentral.Tests/LaCentral.Tests.csproj --filter "FullyQualifiedName~ClienteRepositorioTests|FullyQualifiedName~ProveedorRepositorioTests|FullyQualifiedName~ArticuloRepositorioTests|FullyQualifiedName~ArticuloBusquedaPostgreSqlTests|FullyQualifiedName~HistorialBajaModeloTests|FullyQualifiedName~PostgreSqlSeguridadTests" --verbosity minimal --logger "trx;LogFileName=pr110-seleccion.trx"
dotnet test backend/LaCentral.Tests/LaCentral.Tests.csproj --no-restore --verbosity minimal --logger "trx;LogFileName=pr110-completa.trx"
```

Tras los últimos ajustes se repitió la selección, suprimiendo el warning
MSB3277 preexistente, y la suite completa con `--no-build --no-restore`.

- Selección final: **19 aprobadas, 8 omitidas, 0 fallos** (27 casos).
- Suite final: **89 aprobadas, 8 omitidas, 0 fallos** (97 casos).
- `git diff --check`: sin errores de whitespace.
- Búsqueda final en fixtures afectados: sin Id manual, InMemory, OVERRIDING,
  DROP CONSTRAINT ni cambios de identity para ocultar 428C9.
- Informes: `backend/LaCentral.Tests/TestResults/pr110-seleccion.trx` y
  `pr110-completa.trx` (no versionados).

## Lo que NO se pudo comprobar

Las ocho omitidas son cliente (1), proveedor (1), artículos (5) y búsqueda
PostgreSQL (1). **No se ejecutó ninguna contra PostgreSQL real** porque no hay
conexión de test configurada en este entorno. Docker tampoco tiene motor
activo. Las aprobadas verifican modelo, traducción SQL, guardas y lógica/mocks;
no se presentan como evidencia de persistencia PostgreSQL.

No está versionado ni disponible `01_Ajustes_Sprint3.sql`; el único SQL de la
rama es el fixture sintético de búsqueda anterior. No se inventó un reemplazo
ni se aplicaron DDL remotos. La validación/clonado de catálogo descrita arriba
también está pendiente de ejecución con la plantilla real de D8–D10.

## Antes de integrar

1. Facilitar/configurar conexión de test y SQL D8–D10 de Santi; confirmar la
   plantilla y permisos. Ejecutar los ocho casos con REQUIRE_POSTGRES=1.
2. Resolver cualquier diferencia detectada entre el mapeo y el esquema real,
   conservando sus restricciones. Revisar especialmente tipos, FK e índices.
3. Ejecutar la selección y suite completas sobre la versión integrada con
   develop: esta rama parte del head del PR #110, no incorpora otros merges.
4. Permanecen warnings previos de versiones EF Relational y nulabilidad;
   no se cambiaron paquetes para ocultarlos.
5. Esta entrega no completa la lógica de historial de usuarios/artículos ni
   la propagación de responsables de auditoría: solo corrige su cardinalidad.

## Aprendizaje del proyecto

428C9 se evita dejando identity ALWAYS generar el Id y usando el valor devuelto,
no eligiendo un número mayor ni OVERRIDING SYSTEM VALUE. Los índices únicos
parciales de historial pueden inducir una cardinalidad 1:1 incorrecta en el
scaffold: la personalización debe preservar 1:N y la unicidad solo de abiertas.
LIKE INCLUDING ALL no copia las FK; una fixture fiel debe reconstruirlas dentro
del esquema aislado y no compartir secuencias externas con la plantilla.
La bóveda central configurada no está disponible; se conserva esta nota aquí
sin crear otra bóveda.
