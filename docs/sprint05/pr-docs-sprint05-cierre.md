<!-- Cuerpo del PR de `docs/sprint05-cierre` → `develop`. `gh` no está instalado en la máquina del
agente y la regla de la sesión prohíbe usar la credencial de git para la API: lo abre Javi.
Título sugerido: «docs: cierre del Sprint 5 · ESTADO e informe (sprint no cerrado: faltan las dos condiciones)» -->

**Depende de #154 y de #157.** Sale de `origin/docs/preguntas-sergio` (#157), porque ese PR ya toca
el ESTADO y la bitácora. Solo documentación.

## Qué trae
- `docs/sprint05/ESTADO.md`: sección **«CIERRE DEL SPRINT 5»**, con la línea de base de las
  suites, el entregable parte por parte, las dos condiciones de cierre, el tablero final, el
  arrastre al Sprint 6, la deuda abierta y la PRÓXIMA SESIÓN.
- `docs/sprint05/informe-cierre-sprint5.md`: entregable, qué entró, métricas, arrastre,
  decisiones y lecciones.
- `docs/sprint05/BITACORA.md`: el bloque «09/11/2026 08:32», cerrado.

## El sprint NO queda cerrado
- **Condición 1 (recorrido contra `lacentral_demo`): pendiente.** No se pudo correr porque en la
  máquina no están definidas `LACENTRAL_DEMO_PG` ni `LACENTRAL_DEMO_CLAVE`.
- **Condición 2 (el dueño lo usó): pendiente.** No hay registro en el repo.

## Línea de base: `prueba/cierre-s5` (local, no pusheada)
Es `develop` (`144c325`) con #154, #155, #156 y #158 mergeados, sin conflictos. HEAD `0bd5b67`.
- Backend: `Correctas! - Con error:     0, Superado:   497, Omitido:     0, Total:   497, Duración: 11 m 12 s - LaCentral.Tests.dll (net10.0)`
- Frontend: `TOTAL: 167 SUCCESS`

## Verificadores
- `auditor-persistencia`: no aplica (no toca `backend/` ni el esquema).
- `verificador-tdd`: ver el comentario o la bitácora.

## De dónde salen los números
De `git log --first-parent origin/develop`, de la API pública de GitHub (09/11, sin autenticar) y
de las corridas de arriba. Los SP salen del tablero del 05/11, porque GitHub no los tiene.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
