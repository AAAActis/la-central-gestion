## D-27b · El paralelismo de los tests sale de una cuenta

`Maximum Pool Size` es un techo **por pool**, y el harness crea un pool por esquema aislado. Por eso la demanda real de conexiones es T × P, no P.

- `xunit.runner.json`: `maxParallelThreads = 5` y `parallelAlgorithm = conservative`. Sin `conservative`, un test async que espera a la base libera el hilo y T deja de ser un techo.
- `LaCentral.Tests/README.md`: la cuenta **T × (1 + P) + m ≤ L**, de dónde sale cada término y cómo despejar T para otro L.
- El fixture rechaza `Maximum Pool Size < 2` antes de conectarse. Con P = 1, los dos tests de concurrencia pasaban en serie sin probar nada (verificado: 2/2 en verde). Ciclo: `da89ebe` (rojo: el fixture intentaba conectarse) → `5749f77`. Mutación: con el umbral en `< 1`, el test vuelve a rojo.
- Docs: #130 y #131 figuran mergeados en `ESTADO.md`, `PROMPT-FASE.md` apunta a `sprint04/`, y se registran la deuda de P = 1 y la propuesta de Postgres local para el Sprint 5.

### Evidencia: pico medido en `pg_stat_activity`, cada 0,5 s

| T | P | Techo T × (1 + P) | Pico | L | Resultado |
|---|---|---|---|---|---|
| 16 (antes) | 8 | 144 | 17 | 60 | 435/435 |
| 5 | 3 | 20 | 8 | 60 | 435/435, 3 min |
| 5 | 3 | 20 | 9 | 25 | abortada, 0 `53300` |
| 5 | 3 | 20 | 4 | 25 | 135 fallos de red, 0 `53300` |

### Pendiente: confirmación con L = 25

Queda pendiente porque el enlace se degradó: Tailscale cayó a un relay en São Paulo (65 % de pérdida, RTT promedio de 2.389 ms). En las dos corridas con L = 25 no hubo ningún `53300`, y el pico (9) quedó por debajo del techo calculado (20) y del límite (25). Los fallos fueron `Failed to connect` y `Timeout during reading`. Cuando el enlace vuelva a ser directo, hay que correr `dotnet test` completo con L = 25 y esperar 435/435 (436 con el test nuevo).

La suite completa con el test nuevo (436) no se corrió por el estado del enlace. Sí corrieron en verde los 11 tests de harness que no dependen de la red.

Refs D-27b.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
