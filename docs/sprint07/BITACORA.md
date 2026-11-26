# BITÁCORA · Sprint 7

Se agrega, nunca se reescribe. Formato y reglas: `CLAUDE.md`, «Bitácora de tarea». Lo del
Sprint 7 anterior al 25/11/2026 (C2, el 12, el 14 y sus re-scaffolds) está en
`docs/sprint06/BITACORA.md`.

## 25/11/2026 · Reparto del Sprint 7 en develop (PR solo de docs)

**Instrucción recibida (verbatim):**
> Necesito el reparto del Sprint 7 en develop YA, para que el Claude de Javi lo lea. Desde develop, rama docs/sprint07-reparto, un PR solo de docs (sin suite ni verificadores, declaralo):
> - En docs/sprint07/ESTADO.md, arriba del tablero, una sección «Reparto · 25/11/2026» con dos listas:
>   · Santi (su Claude): script 13 (#202), script 16 y vinculación (053 #126, 054 #127), comparador (030 #16, 031 #18 en lo que toca al sistema, 032 #19, 033 #20, 034 #35, 050 #52), compras (026, 027 #34, 028 #9, 016 #23, #195). Scrapes y workflows de n8n (fuera del repo).
>   · Javi (su Claude): Fix E2 (proveedor sin CUIT) y Fix F F.3–F.5 (vencimiento por proveedor, usa las columnas del 14 / #201). Y la 058 (#194, alícuotas, script 15) cuando Sergio conteste la P7-02.
>   · Regla: nadie toca lo del otro. ESTADO y bitácora: cada uno agrega sus bloques, nunca reescribe los del otro; conflictos con merge conservando los dos lados. Scripts: 13 y 16 son de Santi, 14 y 15 de Javi; si uno necesita tocar una tabla del script del otro, avisa antes.
> - Decisión de negocio de Javi, 25/11/2026 (registrala con fecha, D-S7-12): «una factura sin CUIT es remito o presupuesto: compra en negro». Para el Fix E2 queda sugerido usar proveedor.cuit_simulado (la única coincidencia por nombre con cuit_simulado = true, con «Revisar»); lo decide e implementa Javi.
> - En docs/sprint07/BITACORA.md, un bloque nuevo con la instrucción verbatim y «Estado: terminada».
> Abrí el PR, pasame el número y seguí con lo tuyo.

**Plan:**
1. Rama `docs/sprint07-reparto` desde `origin/develop` (`07fd8e7`), en un worktree aparte: el árbol principal corre la suite del #202.
2. En el `ESTADO`: la sección «Reparto · 25/11/2026» arriba del tablero y la fila D-S7-12.
3. Abrir este bloque, hacer el push y abrir el PR, sin suite ni verificadores.

- `docs/sprint07/BITACORA.md` no existía en `develop`; se crea con la misma cabecera que en `feature/s7-script-13`. Al mergear esa rama habrá un conflicto add/add, que se resuelve conservando los dos lados.
- En `feature/127-vinculacion-esquema`, la fila D-S7-12 era otra decisión técnica de Santi. Pasa a D-S7-13 en esa rama: el número D-S7-12 queda para la decisión de Javi.

**Estado:** terminada. Queda abierto que Santi mergee el PR.

## 26/11/2026 · base_completa.sql con el CHECK de la D10 (punto 2 del bloque del 26/11 en `feature/s7-script-13`)

La instrucción verbatim y el plan están en el bloque «26/11/2026 · #202 sin conflicto y auditor; base_completa.sql…» de la bitácora de `feature/s7-script-13` (#202). Este bloque es el rastro del punto 2, en `fix/base-completa-check-baja` desde `origin/develop` (`bda2692`).

**Estado:** en curso
- Prueba `BaseCompletaPostgreSqlTests` (sin commit todavía): corre solo la parte de tablas de la PARTE A (entre `CREATE EXTENSION … pg_trgm;` y `CREATE SCHEMA staging;`; corta si ese tramo tiene DROP, TRUNCATE, SCHEMA, `public.` o search_path) en un esquema `bc_prueba_*` dentro de una transacción que se revierte, y compara `pg_constraint` contra `public` de `lacentral_test`. Rojo: `Con error: 1, Superado: 0, Total: 1`, **112 restricciones distintas**. Solo 4 son las del aviso de Javi (`usuario`, `cliente`, `proveedor`, `articulo` `*_baja_coherente`, estrictos en el archivo, D10 en la base). Las otras 108 son todo lo que agregaron los scripts 01 a 14 y el archivo (v3.0, 26/08/2026) no tiene: tablas `nota_credito_compra*`, `articulo_precio_modalidad`, `proveedor_articulo*`, `scrape_corrida`, los cuatro `*_historial_baja`, `provincia` y direcciones, la PK de `factura_compra_detalle` con `sucursal_id`; y sobra `articulo_margen`.

**BLOQUEADO EN:** Santi: «igual a lo que corre en las bases» es reescribir la PARTE A entera (los 12 scripts adentro, PR grande) y deja la PARTE B/C de importación apuntando a columnas que ya no existen; o se arreglan los 4 CHECK de baja y la prueba compara solo esos.
- Respuesta de Santi (26/11/2026), verbatim:
  > Solo los 4 CHECK de baja, en un PR chico, con la prueba acotada a esos 4 y a la PK. Además, en el encabezado de base_completa.sql poné un aviso visible: «Esquema v3.0 del 26/08/2026, histórico. El esquema vigente es este archivo más los scripts 01 a 14 de backend/db/rescaffold/, en orden. No armar una base solo con este archivo.» El desfase completo va como deuda en el ESTADO, con la propuesta de regenerar la PARTE A desde pg_dump --schema-only de public al cerrar el Sprint 7 (no a mano) y marcar la PARTE C como histórica. Esa regeneración no se hace ahora.
- Lectura técnica: «la PK» = la PK de cada una de esas cuatro tablas (`usuario_pkey`, `cliente_pkey`, `proveedor_pkey`, `articulo_pkey`). La de `factura_compra_detalle` (que también difiere) queda en la deuda con las otras 107.
- Rojo `0564466` (prueba acotada): `Con error: 1, Superado: 0, Total: 1`, «4 restricciones distintas», las 4 `*_baja_coherente`; las 4 PK coinciden.
- Producción `6f36ac9`: los 4 CHECK a la D10 y el aviso del encabezado. Filtrado: `Correctas! - Con error: 0, Superado: 1, Total: 1`. Deuda del desfase completo en el ESTADO (C7-06) y PRÓXIMA SESIÓN. Sigue: suite completa cuando termine la del auditor del #202 (no se carga la base dos veces a la vez), PR y verificadores.
- 3) Consulta de esquemas de `lacentral_test` (dueño, tablas, tamaño; `BEGIN READ ONLY … ROLLBACK`) entregada a Santi en la respuesta. No la corrí ni borré nada.

## 26/11/2026 · Cola autónoma: #202, base_completa, script 16, 053/054, comparador, compras

**Instrucción recibida (verbatim):**
> Seguí trabajando solo con esta cola, en este orden, sin esperarme entre una tarea y otra:
>
> 1) #202: cuando el auditor-persistencia dé su resultado, si es NO APTO arreglalo (hasta 3 rondas, como dice CLAUDE.md). Con los dos APTO, dejá el PR listo y anotalo en el ESTADO. No lo mergeo yo hasta que vuelva.
> 2) base_completa.sql: suite completa (cuando no haya otra corriendo), push, PR y verificadores con el alcance acotado a sus commits.
> 3) Script 16 (vinculación: UNIQUE(proveedor_id, codigo), usuario_id y fecha_vinculacion en articulo_codigo_alternativo) con sus pruebas PostgreSQL y PR. Dejá el bloque fish para aplicarlo en las tres bases en la bitácora, en una sección «PARA SANTI», y seguí.
> 4) 053 y 054 sobre la rama del 16 (PR apilado, con la base declarada en el cuerpo; cuando el 16 entre a develop, retargeteás). Vinculación automática por código primero, después candidatos con pg_trgm (umbral configurable en 0,3, C6 pendiente), con su pantalla.
> 5) Comparador: 030, 032, 033, 034 y la pantalla 050, con las reglas D-S7-06 a D-S7-11. La 031 solo documentada.
> 6) Compras: 016 y 028 (D-S7-03) y #195 (D-S7-04) recién cuando yo haya aplicado el 13; si todavía no, saltealas y seguí. La 027 sigue bloqueada por la P7-01: no la toques.
>
> Reglas para no frenar:
> - Nunca dos suites completas a la vez ni mientras corra un verificador: mientras una corre, avanzá escribiendo código o pruebas de la tarea siguiente, sin correr nada contra la base.
> - Decisiones técnicas: las tomás vos y las registrás con fecha en el ESTADO.
> - Solo parás por (a) una regla de negocio que no esté escrita, (b) algo que toque datos reales de lacentral, o (c) CA contradictorios. Si te pasa, NO te quedes esperando: anotá la pregunta en una sección «PREGUNTAS PARA SANTI» del ESTADO y seguí con la tarea siguiente de la cola.
> - Todo lo que necesite mis manos (aplicar scripts, merges) va a «PARA SANTI» en la bitácora, con el bloque fish listo, y seguís.
> - Si un verificador da NO APTO, lo arreglás vos (hasta 3 rondas), como dice CLAUDE.md.
> - Al final de cada tarea: bitácora y ESTADO actualizados, y una línea con el número de PR.
>
> Cuando vuelva, quiero un resumen de: PRs abiertos con su estado de verificadores, bloques PARA SANTI pendientes y PREGUNTAS PARA SANTI.

**Plan:**
1. #202: esperar el resultado del auditor (en curso sobre `fe5ed3e`); NO APTO → arreglo, hasta 3 rondas. APTO → ESTADO.
2. `fix/base-completa-check-baja`: suite completa cuando termine la del auditor; push, PR, `verificador-tdd` y `auditor-persistencia` de a uno, acotados a `0564466..6f36ac9`.
3. Script 16: ya está en `feature/127-vinculacion-esquema` (rojo `0e28343`, verde `f8aef6d`/`bd33193`, 8/8 filtradas, bloque fish en la bitácora de esa rama). Falta: merge de `develop`, suite, PR, verificadores.
4. 053/054 apilados sobre la rama del 16. 5. Comparador. 6. Compras, solo si el 13 está aplicado.
Mientras corre una suite o un verificador: solo escribir código o pruebas, nada contra la base.

**Estado:** en curso
