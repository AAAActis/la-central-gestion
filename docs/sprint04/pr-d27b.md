**Base: `docs/proformas-y-sistema-visual`**, que trae las proformas y el sistema visual. Este PR
muestra solo el trabajo de D-27; cuando la base entre en `develop`, GitHub lo reapunta solo.

## Qué resuelve

El consumo de conexiones de la suite de pruebas dependía de números elegidos a ojo. Esta
rama lo vuelve derivable y agrega una guarda contra una configuración que vaciaba de
sentido a dos pruebas sin que nada avisara.

## Qué se hizo

**Deuda D-27b · el techo de conexiones ahora se calcula**

El harness arma una cadena de conexión aislada por esquema de test, así que Npgsql crea
**un pool por esquema**: `Maximum Pool Size` es un techo por esquema, no total. La demanda
real es *hilos en paralelo × (1 + pool) + conexiones de administración*.

- `xunit.runner.json`: `maxParallelThreads: 5` y `parallelAlgorithm: conservative`. Sin
  `conservative`, un test asíncrono que espera a la base libera el hilo, xUnit arranca otro
  y el número de hilos deja de ser un techo.
- `LaCentral.Tests/README.md`: la cuenta `T × (1 + P) + m ≤ L`, de dónde sale cada término
  y cómo despejar `T` para otro `L`. El `1` es la conexión de administración que el fixture
  mantiene abierta durante todo el test.
- Con `L = 25`, `m = 5` y `P = 3` da `T = 5`.

**Deuda del falso verde · guarda de `Maximum Pool Size ≥ 2`**

Con el pool en 1, las dos pruebas de altas simultáneas **dejan de ejecutarse en paralelo**
—no pueden, con una sola conexión— y pasan a correr en serie. Siguen en verde y dejan de
probar la concurrencia que su nombre declara.

Una prueba que pasa sin verificar lo que dice verificar es peor que una que falla: la que
falla avisa. El fixture ahora verifica al arrancar que el pool sea de al menos 2 y corta
con un mensaje que lo explica. Hecho con el ciclo de tres commits, con mutación que
confirma que el test falla sin la guarda.

**Documentación**

- `ESTADO.md`: corregidas las filas que declaraban a #130 y #131 como pendientes de merge
  (están en `develop` desde el 20/10). Registradas D-27a, D-27b y D-27c, y la propuesta de
  una instancia local de PostgreSQL por desarrollador como decisión del Sprint 5.
- `PROMPT-FASE.md`: las referencias a `docs/sprint03/` pasan a `docs/sprint04/`.

## Evidencia

Pico de conexiones medido leyendo `pg_stat_activity` cada 0,5 s:

| T | P | Techo calculado | Pico medido | L | Resultado |
|---|---|---|---|---|---|
| 16 (antes) | 8 | 144 | 17 | 60 | 435/435 |
| 5 | 3 | 20 | 8 | 60 | 435/435, 3 min |
| 5 | 3 | 20 | 9 | 25 | corrida abortada — ver abajo |
| 5 | 3 | 20 | 4 | 25 | corrida abortada — ver abajo |

En las dos corridas con `L = 25`: **cero errores `53300`** y pico de 9 sobre 25. Los fallos
fueron de red (`Failed to connect`, `Timeout during reading`, sesiones en
`idle in transaction` durante el DDL), con el enlace privado degradado a relay en São Paulo
—65 % de pérdida, latencia promedio de 2.389 ms y picos de 6.157 ms—.

**La fórmula queda validada por la medición directa del pico.** Lo que falta es una corrida
limpia de confirmación de punta a punta.

## Qué NO se hizo

- **Corrida de confirmación con `L = 25`:** pendiente por la degradación del enlace, no por
  la cuenta. El rol queda en 60 mientras tanto.
- **D-27a** (verificar la identidad del motor al inicio de la corrida) y **D-27c** (prefijo
  de esquema con el número de un PR viejo y limpieza en `DisposeAsync`): registradas, no
  implementadas.

## Definición de Terminado

| Punto | Estado |
|---|---|
| 9 · Revisado por otro integrante | Pendiente de esta revisión |
| 10 · Verificado contra la base real | **NO verificado**, y el motivo es el mismo que esta rama documenta: el enlace al servidor está degradado. Las 148 pruebas de integración no se pudieron correr de forma concluyente |

El punto 10 se declara no verificado en lugar de omitirse. No existe un estado intermedio.

## Nota para quien revise

El cambio de `maxParallelThreads` hace que la suite tarde más y consuma menos. Es
deliberado: el objetivo es que el consumo sea **predecible**, no mínimo. Subir el límite
del rol hasta que la suite pase mueve el problema de lugar; acotar la demanda y demostrar
que entra en el límite lo resuelve.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
