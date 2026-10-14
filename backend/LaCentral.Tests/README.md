# LaCentral.Tests

## Los tests de PostgreSQL son obligatorios

Los tests marcados con `[PostgreSqlFact]` corren contra un PostgreSQL **real**.
**Nunca se omiten**: si no hay conexión configurada, **fallan** con un mensaje que
remite a este archivo.

Por qué: InMemory no implementa CHECK constraints, índices únicos parciales ni columnas
de identidad. Con los tests de PostgreSQL omitidos, la suite se veía verde sin haber
tocado el motor. Así pasaron dos bugs críticos.

Si ves `Superado: N` con algún test de PostgreSQL en rojo por "Falta la conexión
PostgreSQL de test", **la suite no está verde**: configurá la conexión.

## Configurar la conexión

Una variable de entorno con la cadena de conexión a una base **exclusiva de pruebas**:

| Variable | Uso |
|---|---|
| `LACENTRAL_TEST_PG` | Nombre habitual. |
| `LA_CENTRAL_TEST_POSTGRES` | Alias. Si están las dos, tienen que tener el mismo valor. |
| `LA_CENTRAL_TEST_SCHEMA` | Opcional. Esquema plantilla del que se copia la estructura. Por defecto `public`. |

El nombre de la base **tiene** que ser `test_*` o `*_test` (por ejemplo `la_central_test`).
`PostgreSqlSeguridadTests` y el fixture rechazan cualquier otro nombre, y cualquiera que
contenga `principal`, `prod` o `produccion`. **Nunca** apuntes a la base principal: tiene
los datos reales de la empresa.

Windows (PowerShell, persistente para tu usuario; reabrí la terminal o el IDE después):

```powershell
[Environment]::SetEnvironmentVariable("LACENTRAL_TEST_PG",
  "Host=<host>;Port=5432;Database=la_central_test;Username=<usuario>;Password=<clave>", "User")
```

Bash (sesión actual):

```bash
export LACENTRAL_TEST_PG="Host=<host>;Port=5432;Database=la_central_test;Username=<usuario>;Password=<clave>"
```

Rider o Visual Studio heredan las variables del usuario: reiniciá el IDE después de
definirla.

`LA_CENTRAL_REQUIRE_POSTGRES` ya no se usa: exigir PostgreSQL es el único comportamiento.

## Qué necesita la base de test

La base de test tiene que tener aplicado el esquema real (`db/base_completa.sql` y los
scripts de `db/rescaffold/`). El fixture (`Infraestructura/PostgreSqlPruebas.cs`):

1. Verifica que cada tabla y columna mapeada por EF exista en la plantilla con el mismo
   tipo, nulabilidad e identidad. Si no coinciden, falla **antes** de crear nada.
2. Crea un esquema aislado `pr110_test_<guid>` por test, copia la estructura (`LIKE ...
   INCLUDING ALL` más las FK) y los catálogos `rol` y `sucursal`.
3. Al terminar, borra **solo** ese esquema aislado.

No toca datos ni tablas de la plantilla.

## Qué se prueba con qué

- Lógica de casos de uso y guardas: Moq.
- Mapeo de repositorio, índices, transacciones, CHECK, atomicidad y pg_trgm: PostgreSQL
  real con `[PostgreSqlFact]`. Toda afirmación relee la fila **con un contexto nuevo**.
- Recorridos HTTP: `WebApplicationFactory`.

## Verificar

```bash
dotnet test LaCentral.slnx
```

Resultado esperado: `Con error: 0` y `Omitido: 0`.
