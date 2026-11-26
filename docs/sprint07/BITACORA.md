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

## 25/11/2026 · Fix E2 · plan con `proveedor.cuit_simulado` (D-S7-12)

**Instrucción recibida (verbatim):**
> Decisión de Javi, 25/11/2026 (D-S7-12): el Fix E2 va por proveedor.cuit_simulado. Regla: si el PDF no trae CUIT, entre los proveedores ACTIVOS que coinciden por razón social, se asigna el único con cuit_simulado = true, siempre con «Revisar». Con 0 o con 2 o más, no se asigna nada. Con CUIT en el PDF, nada cambia. Esto REEMPLAZA la regla «razón social exacta primero» en el camino sin CUIT. Antes de seguir: git fetch --all --prune y un solo git merge origin/develop (entraron #203 = 0e9a531 y #201 = bda2692; el reparto está en docs/sprint07/ESTADO.md, sección «Reparto · 25/11/2026»). Leela. Lo de Santi (053, 054, comparador, compras, scripts 13 y 16, n8n) NO se toca. AHORA SOLO UN PLAN, SIN TOCAR CÓDIGO: 1. Verificá en lacentral_test (SOLO esa base; nunca lacentral ni lacentral_demo) que cuit_simulado esté bien cargado: cuántos proveedores lo tienen en true, cuáles son los pares blanco/negro, y si hay algún proveedor sin CUIT real en false que pudiera quedar mal clasificado. Solo lecturas. 2. Decime qué commits y tests del E2 actuales se reemplazan o se reusan (BuscarActivosPorRazonSocialExactaAsync, ProveedorRazonSocialExactaTests, ProveedorRazonSocialExactaPostgreSqlTests, el cambio en InterpretarFacturaPdfUseCase) y cómo sería la consulta nueva. 3. Listá los casos de prueba de la regla (0, 1 y 2+ coincidencias con cuit_simulado, activo/inactivo, PDF con CUIT sin cambios) y qué va en Moq y qué en PostgreSQL real. 4. Preguntas que la regla no deje cerradas. Sin código, sin commits de producción, sin push. Registralo en la bitácora.

**Plan:** 1. Merge de `origin/develop` y lectura del reparto. 2. Lecturas en `lacentral_test`. 3. Reuso, consulta y casos. 4. Preguntas. Sin código.

**Estado:** terminada
- Merge de `origin/develop` (`bda2692`) en `ffe1f4e`; conflicto solo en la bitácora del sprint 6, conservados los dos lados (HEAD primero), 52 `**Estado:**` y 52 `##`. El reparto deja a Javi el Fix E2, el F.3 a F.5 y la 058; nada de Santi se toca.
- `lacentral_test.public`, solo lectura (`transaction_read_only = on`, rol `lacentral_javi`): 124 proveedores; `cuit_simulado = true`: 48 (47 activos); `false` activos: 76; ninguno sin CUIT. Pares por nombre contenido (6), cada uno con exactamente uno en `true`: AUTORECAMBI X/AUTORECAMBI, CROMOSOL/CROMOSOL SAICF, EXPOYER/EXPOYER S.A., FAZIO/NESTOR FAZIO S.A., FIATCAR/FIATCAR DE LEONARDO…, FUSION/FUSION SRL. «ETMAN» solo coincide con CACHO ETMAN (22, `true`). Sin sospechosos por las heurísticas usadas (sin CUIT en `false`; «X» suelta en `false`; forma societaria en `true`). No se consultaron `lacentral` ni `lacentral_demo`.
- Hallazgos: la app no escribe `cuit_simulado` (un proveedor dado de alta por el sistema queda en `false`); la siembra de la demo pone `false` a ETMAN y Fazio; el lector junta todos los CUIT del texto, incluido el de la empresa.
- Plan entregado a Javi.
**BLOQUEADO EN:** respuestas de Javi (qué es «PDF sin CUIT», CUIT no registrado, demo, alta de proveedores en negro) y OK del plan.
- Plan aprobado por Javi con ajustes (bloque siguiente).

## 26/11/2026 · Fix E2 · implementación con `proveedor.cuit_simulado`

**Instrucción recibida (verbatim):**
> Plan aprobado, con ajustes. Respuestas: 1. «Sin CUIT» = ningún CUIT del texto pertenece a un proveedor cargado (lo de hoy). 2. La regla se aplica SOLO si el comprobante leído no es A. Verificá antes que el tipo esté disponible en el punto donde se decide; si no lo está, pará y avisame, sin agregar lectura de PDF. 3. Demo: leé lacentral_demo (solo lectura) para ver ETMAN, Fazio y Expoyer. Prepará un script idempotente que pase a true lo que corresponda. NO lo apliques: lo aplica Javi o Santi. 4. Alta de un proveedor en negro desde la app: fuera de este fix. Anotalo como deuda en el ESTADO y en el cuerpo del PR (hoy se corrige por SQL). Cambio al plan: renombrá el método a BuscarActivosEnNegroPorRazonSocialAsync (puerto, repositorio, llamador y mocks), para que el nombre diga el filtro. Autorizado: TDD con el rojo por aserción en un commit propio y la producción en otro, commits nuevos sin rebase, bitácora en docs/sprint07/BITACORA.md (se agrega, no se reescribe). Un bloque nuevo en el ESTADO que diga que la spec del Fix E2 («exacta primero») queda superada por D-S7-12, sin tocar los bloques viejos. Tests filtrados en el bucle. Al cerrar, suite completa UNA vez sobre el último commit, desde backend/, con línea literal, conteo y hash. Sin push, sin PR y sin verificadores todavía (los worktrees extra siguen vivos). Declará que los datos de cuit_simulado valen para lacentral_test y que la real está sin verificar.

**Plan:** 1. Verificar el tipo. 2. Commit de renombre (sin cambio de conducta). 3. Pruebas en rojo. 4. Producción. 5. Demo (solo lectura) y script sin aplicar. 6. ESTADO y bitácora. 7. Suite completa una vez.

**Estado:** en curso
- El tipo está disponible donde se decide: `ComprobanteLeido.TipoComprobante` (`"A"`, `"B"` o `null` si el código de ARCA no se reconoce); no hace falta leer nada nuevo del PDF. `origin/develop` sin commits nuevos.
- Commits: renombre `ea6b734` (sin cambio de conducta, compila); pruebas `8632734`, en rojo sobre ese commit: `Con error: 4, Superado: 52, Total: 56`, las 4 por aserción (2 Moq de factura A, 2 PostgreSQL). Una primera corrida mostró `Total: 54` sin las 2 de PostgreSQL nuevas (causa no determinada; se recompiló y se repitió) y no cuenta. Se borraron las dos clases de «exacta primero» y las pruebas de CUIT y de cada layout pasaron a comprobantes B.
- Producción `00710c0`: filtrado `Correctas! - Con error: 0, Superado: 56, Total: 56`. Graphify actualizado.
- `lacentral_demo`, solo lectura (`ro=on`): 9 proveedores; ETMAN (8), Fazio (9) y Expoyer (7) activos, sin CUIT y en `false`; hay además AUTOCOR SRL (10, con CUIT, `false`). Script `backend/db/demo/marcar_en_negro_demo.sql` escrito y **sin ejecutar** en ninguna base; sin commitear.
- Los datos de `cuit_simulado` valen para `lacentral_test`; `lacentral` está sin verificar.
- ESTADO del sprint 7: bloque nuevo (spec superada por la D-S7-12, regla vigente, deuda del alta en negro).
