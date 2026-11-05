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
  "Host=<host>;Port=5432;Database=la_central_test;Username=<usuario>;Password=<clave>;Maximum Pool Size=3", "User")
```

Bash (sesión actual):

```bash
export LACENTRAL_TEST_PG="Host=<host>;Port=5432;Database=la_central_test;Username=<usuario>;Password=<clave>;Maximum Pool Size=3"
```

Rider o Visual Studio heredan las variables del usuario: reiniciá el IDE después de
definirla.

`LA_CENTRAL_REQUIRE_POSTGRES` ya no se usa: exigir PostgreSQL es el único comportamiento.

## Cuántas conexiones usa la suite

Tres valores, y una cuenta que los ata. Si el rol de PostgreSQL tiene `CONNECTION LIMIT`,
la suite tiene que entrar en él, o da fallos `53300: too many connections for role`.

| | Valor | Dónde está |
|---|---|---|
| **T** | Tests de PostgreSQL corriendo a la vez | `maxParallelThreads` en `xunit.runner.json` (hoy **5**) |
| **P** | Tope de conexiones de **cada** pool | `Maximum Pool Size` en tu `LACENTRAL_TEST_PG` (recomendado **3**) |
| **L** | Conexiones permitidas al rol | `CONNECTION LIMIT` del rol (equipo: **25** por desarrollador) |

```
T × (1 + P) + m ≤ L
```

De dónde sale cada término:

- **Un pool por test, no uno total.** Cada test arma su esquema aislado con su propia cadena
  de conexión (`search_path` distinto), y Npgsql crea un pool por cadena. `Maximum Pool Size`
  es un techo **por pool**: con T tests a la vez hay T pools, y el techo real es T × P.
- **El 1** es la conexión de administración que el fixture abre al crear el esquema y tiene
  abierta hasta borrarlo, al final del test.
- **m** es tu reserva: todo lo que abras con el mismo rol mientras corre la suite (psql,
  DBeaver, la API levantada a mano). Reservá **5**.
- **T acota de verdad** porque `parallelAlgorithm` es `conservative`: xUnit no arranca un test
  nuevo hasta que termina otro. Con `aggressive`, un test que espera la base libera el hilo,
  arranca otro, y T deja de ser un techo. No lo cambies.

Despejado, para un L dado:

```
T = ⌊(L − m) / (1 + P)⌋        con L = 25, m = 5, P = 3:  T = ⌊20 / 4⌋ = 5
```

Reglas:

- **P ≥ 2.** `DosAltasSimultaneas_CodigosInternosDistintos` y
  `DosBajasAbiertasSimultaneas_...` abren dos conexiones del mismo esquema a la vez. Con
  P = 1 **pasan igual, y eso es lo peligroso**: el segundo contexto espera a que el primero
  devuelva la conexión, las dos operaciones corren en serie y el test deja de probar la
  concurrencia (medido: 2/2 en verde con P = 1). Por eso el fixture **rechaza P < 2** antes
  de conectarse (`PostgreSqlPoolTests`). P = 3 deja una de margen. Un test que
  necesite más de P conexiones a la vez espera el `Timeout` del pool (10 s): si no se libera
  ninguna, falla con timeout; si se libera, se serializa sin avisar.
- **`Maximum Pool Size` va siempre explícito.** Sin él, P = 100 y la cuenta no garantiza nada.
- **Si tu L es otro**, no toques `xunit.runner.json`: pasá tu T por línea de comandos,
  `dotnet test -- xUnit.MaxParallelThreads=<T>`.
- **El límite del equipo.** El motor tiene `max_connections = 100`. Tres roles de
  desarrollo × 25 = 75, y el resto queda para los roles de la aplicación y las conexiones
  reservadas de superusuario. Subir el L de un rol se lo come a los otros dos.

Medido el 28/10/2026 contra `lacentral_test`, muestreando `pg_stat_activity` cada 0,5 s
(el muestreador es 1 de esas conexiones):

| T | P | Techo T × (1 + P) | Pico observado | L | Resultado |
|---|---|---|---|---|---|
| 16 (antes: un hilo por núcleo) | 8 | 144 | 17 | 60 | 435/435 |
| 5 | 3 | 20 | 8 | 60 | 435/435, 3 min |
| 5 | 3 | 20 | 9 | 25 | colgada a los 10 min, abortada · 0 `53300` |
| 5 | 3 | 20 | 4 | 25 | 135 fallos de red, abortada · 0 `53300` |
| 5 | 3 | 20 | 8 | 25 | **436/436, 2 min 57 s · 0 `53300`** |

Las dos corridas con L = 25 no fallaron por el límite: ningún `53300`, y el pico nunca pasó
de 9. Fallaron porque el enlace de Tailscale cayó a un relay en São Paulo (65 % de pérdida,
RTT promedio de 2.389 ms): `Failed to connect`, `Timeout during reading` y sesiones
`idle in transaction` cortadas a mitad del DDL. **Lo demostrado:** la demanda medida (≤ 9)
queda dentro del techo calculado (20) y del límite (25). **Confirmado (28/10/2026):** con el
enlace sano, la corrida completa con L = 25 dio 436/436 y ningún `53300`, con un pico de 8
(en esa corrida el muestreador no se cuenta).

El pico real está lejos del techo porque casi todos los tests usan una conexión por vez.
El techo es lo que se garantiza; el pico depende de qué tests coincidan y de la máquina.

## Qué necesita la base de test

La base de test tiene que tener aplicado el esquema real (`db/base_completa.sql` y los
scripts de `db/rescaffold/`). El fixture (`Infraestructura/PostgreSqlPruebas.cs`):

1. Verifica que cada tabla y columna mapeada por EF exista en la plantilla con el mismo
   tipo, nulabilidad e identidad. Si no coinciden, falla **antes** de crear nada.
   La comparación va en un solo sentido: una columna que existe en la base y que el
   modelo no mapea **no** se detecta. Así pasaron inadvertidas `articulo.codigo`,
   `descripcion` y `cantidad`, que solo estaban en `lacentral_test`
   (ver `db/rescaffold/03_Limpieza_Columnas_Fantasma.sql`).
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
