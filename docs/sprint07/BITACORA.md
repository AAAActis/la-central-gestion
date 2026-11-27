# BITÁCORA · Sprint 7

Se agrega, nunca se reescribe. Formato y reglas: `CLAUDE.md`, «Bitácora de tarea». Lo del
Sprint 7 anterior al 25/11/2026 (C2, el 12, el 14 y sus re-scaffolds) está en
`docs/sprint06/BITACORA.md`.

## 25/11/2026 · Arranque del Sprint 7: script 13, 053/054, comparador, compras

**Instrucción recibida (verbatim):**
> Arrancamos el Sprint 7. Leé primero docs/sprint07/ESTADO.md y el último bloque «en curso» de la bitácora, y hacé el fetch y la comparación con develop como dice CLAUDE.md. Fijate si el #201 (re-scaffold del 14) ya está mergeado; si no, avisame y no toques Proveedor hasta que entre.
>
> CONTEXTO NUEVO (no está en el repo):
> - Scrape de Expoyer corrido en lacentral_demo con el workflow de n8n que usa las 7 sentencias del CONTRATO-N8N: corrida 2 en SUCCESS, 103.478 filas en proveedor_articulo (proveedor_id 7 en la demo), 46.538 con stock, 0 sin costo, 0 vencidos. Precios validados contra factura: precio_neto = lista × (1−61,08%) × (1−15%). Autocor (proveedor_id 10 en la demo, CUIT 30-70832509-7) lo estoy terminando yo.
> - Los workflows de n8n, el scrape y sus credenciales los manejo yo por fuera: no los toques ni los documentes como hechos en el repo.
> - Javi tiene el Fix E2 (proveedor sin CUIT: exacta primero, posible uso de proveedor.cuit_simulado) y el Fix F F.3 a F.5 (vencimiento por proveedor). No toques esas áreas.
>
> ORDEN DE TRABAJO (un PR por HU, desde develop, como siempre):
> 1. Script 13 (anulación con costo previo y un solo administrador, D-S7-03 y D-S7-04), que está en borrador en feature/s7-script-13 sin ejecutar ni probar. Probalo en un esquema de ensayo de lacentral_test, escribí las pruebas PostgreSQL de sus CHECK, y abrí PR. Pasame el bloque fish para aplicarlo en las tres bases (test, demo, real con backup), y después de que lo aplique hacé el re-scaffold en PR aparte.
> 2. HU 053 (candidatos de vinculación por aproximación, pg_trgm) y HU 054 (vincular un artículo de proveedor al catálogo propio). Primero la vinculación automática por código: si proveedor_articulo.codigo coincide con un articulo_codigo_alternativo del mismo proveedor, se vincula solo. Lo que no coincida pasa a candidatos por parecido de nombre y número original, y lo confirma el operador. Usá los datos reales de Expoyer en la demo para la prueba manual del entregable.
> 3. Comparador: HU 030, 032, 033, 034 y la pantalla HU 050. La HU 031 (consulta a los sitios) queda cubierta por los scrapes de n8n: documentá en su PR qué parte hace n8n y qué parte el sistema, sin inventar lo que no está.
> 4. Compras: HU 026, 027, 028 (la 028 necesita el 13 aplicado) y HU 016.
>
> REGLAS:
> - Antes de cada HU, si un criterio de aceptación no se puede cumplir con lo decidido, o falta una regla de negocio, PARÁ y preguntame con una pregunta corta. No inventes reglas.
> - PRs chicos. Si una HU pasa de unas 300 líneas, partila en dos PR con sentido propio.
> - Verificadores: una vez por PR, al cerrar la HU, con el alcance acotado a sus commits y con el hash, la línea y el conteo de la suite si ya la corriste. auditor-persistencia solo si toca backend o el esquema.
> - No mergees nada vos: cuando un PR esté listo, avisame con su número, el resumen de CA y la suite, y seguí con la siguiente HU desde develop.
> - Bitácora y ESTADO como dice CLAUDE.md, antes de cada push.

**Plan:**
1. Arranque: leer ESTADO y el último bloque «en curso»; fetch y comparación con `develop`; estado del #201.
2. Script 13 en `feature/s7-script-13` (con `develop` mergeado): corregir el borrador, ensayarlo en un esquema descartable de `lacentral_test` (copia de `public` con el método de `backend/db/pruebas/`), pruebas PostgreSQL de sus CHECK contra esa copia, guarda de datos previos (anuladas sin usuario), PR. Bloque fish para Santi (test, demo y real con backup).
3. Al aplicarse el 13: re-scaffold en PR aparte.
4. 053/054, comparador y compras, en ese orden. Antes de cada HU, revisar sus CA contra lo decidido; si falta una regla, parar y preguntar.

**Estado:** en curso
- 1) Arranque. `origin/develop` = `07fd8e7` (#200). **El #201 sigue abierto**: no se toca `Proveedor` hasta que entre. El último bloque «en curso» de `docs/sprint06/BITACORA.md` (23/11, «C2 con los datos reales del primer scrape») ya está cumplido: C2 entró con el #199 (`0633b3b`). No se retoma. Worktrees: solo el principal.
- 2) `feature/s7-script-13` con `origin/develop` mergeado (`0cb0de7`, merge sin conflicto; la rama solo traía el SQL del 13). Error en el borrador: el paso 2 hace `pg_get_constraintdef((SELECT FROM pg_constraint …))`, sin `oid`. No hay `psql` en esta máquina: el ensayo corre los SQL con un programa descartable de .NET (Npgsql) en el scratchpad.
- Corrección: el borrador SÍ tiene `SELECT oid` en el paso 2. Lo de la línea anterior salió de una lectura comprimida del archivo, no del archivo; no hay tal error. Ensayo en `s13_ensayo_246121ce` (`lacentral_test`, `lacentral_santi`; copia con `backend/db/pruebas/nc057_fuente_{copia,entrar}.sql` y el nombre reemplazado): `copia creada (33 tablas)`; el 13: `13: TODO OK.`; el 13 otra vez: solo `13: TODO OK.` (idempotente). Datos (solo lectura): en `lacentral_test`, `public` no tiene facturas de compra; en `lacentral_demo` hay 18, todas ACTIVA. La real no se lee desde acá: se agrega al 13 una guarda que corta si hay facturas ANULADA sin `usuario_anulacion_id` (sin ella, `fc_anulacion_con_usuario` fallaría con un 23514 sin explicación).
- Pruebas: `Script13PostgreSqlTests` (11, `[PostgreSqlFact]`; cada una corre el archivo del 13 tal cual sobre su esquema aislado). Rojo `2b3e9d5`, contra el borrador: `Con error: 1, Superado: 10, Total: 11`; la que falla es `ConFacturaAnuladaSinUsuario_ElScriptCortaSinAplicarNada`, por aserción (`Expected: "P0001"`, `Actual: "23514"`: el ADD CONSTRAINT revienta sin explicar). Las otras 10 pasan contra el borrador porque esos CHECK ya estaban escritos (caracterización). Producción `0d4ec50` (guarda + encabezado): `Correctas! - Con error: 0, Superado: 11, Total: 11`.
- Esquema de ensayo `s13_ensayo_246121ce` borrado (`borrado.sql` con el nombre reemplazado: «drop cascades to 34 other objects», `public: 33 tablas`). No toqué `demo_ensayo_c3` ni los tres `pr110_test_*` que hay en `lacentral_test`, porque no son míos.

### PARA SANTI · 25/11/2026 · script 13 en las tres bases

Desde la raíz del repo, en fish. Cada paso tiene que terminar con `NOTICE:  13: TODO OK.`; si uno falla, no sigas. El 13 es una sola transacción: si corta, no deja nada a medias.

1 · `lacentral_test` (`public`):

```fish
cd ~/dev/TESIS/la-central-gestion
git fetch origin
git show origin/feature/s7-script-13:backend/db/rescaffold/13_Anulacion_Costo_Previo_Un_Administrador.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_test -v ON_ERROR_STOP=1
```

2 · `lacentral_demo`:

```fish
git show origin/feature/s7-script-13:backend/db/rescaffold/13_Anulacion_Costo_Previo_Un_Administrador.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_demo -v ON_ERROR_STOP=1
```

3 · `lacentral` (la real), con backup antes. Tiene que decir `backup OK` antes de aplicar:

```fish
mkdir -p ~/dev/TESIS/backups
ssh atiz@arch docker exec lacentral_db pg_dump -U lacentral -Fc lacentral > ~/dev/TESIS/backups/lacentral-antes-13.dump
and test -s ~/dev/TESIS/backups/lacentral-antes-13.dump
and echo "backup OK"
and git show origin/feature/s7-script-13:backend/db/rescaffold/13_Anulacion_Costo_Previo_Un_Administrador.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral -v ON_ERROR_STOP=1
```

Si en alguna base corta con «13: hay N facturas de compra anuladas sin usuario_anulacion_id», no se aplicó nada: hay que decidir quién figura como el que las anuló. Esa decisión es del equipo; no la invento.
- Suite completa sobre `68745d6`, contra `public` de `lacentral_test` (`dotnet build --no-incremental`: 0 errores, 0 advertencias; `dotnet test --no-build`): `Correctas! - Con error:     0, Superado:   869, Omitido:     0, Total:   869, Duración: 7 m 44 s - LaCentral.Tests.dll (net10.0)`. Son 858 + las 11 del 13, con cero `[FAIL]`. Push y PR a continuación; este commit es solo docs y queda fuera de la corrida.
- PR **#202** abierto. `verificador-tdd` en curso, con alcance `f944258`, `2b3e9d5` y `0d4ec50` y la suite de `68745d6` (869). El `auditor-persistencia` va después: no corren dos a la vez.
- Revisión previa de la 053 y la 054:
  - `proveedor_articulo` no tiene `articulo_id`, y `articulo_codigo_alternativo` (`articulo_id`, `proveedor_id`, `codigo`) no guarda usuario ni fecha. El CA1 de la 054 pide asentarlos, así que la vinculación necesita un script nuevo.
  - El CA3 de la 054 (dos códigos del mismo proveedor para el mismo artículo) choca con la D2 (un código vigente por proveedor y artículo). El ESTADO ya lo marcaba como decisión del equipo.
  - La 053 no tiene umbral de parecido: la compuerta C6 (medición sobre 50 artículos) no está hecha.

**BLOQUEADO EN:** la 054 y el umbral de la 053, que dependen de Santi o del equipo: el CA3 contra la D2, dónde se guarda la vinculación y el umbral de pg_trgm. Mientras tanto, los verificadores del #202.
- Respuestas de Santi (25/11/2026), verbatim, a las tres preguntas:
  > (CA3 vs D2) Vale el CA3. Decisión de Santi, 25/11/2026: la D2 se afloja así. Un artículo propio puede tener varios códigos del mismo proveedor (por ejemplo, la misma bomba en dos marcas, cada una con su código). Lo que se mantiene es la unicidad de (proveedor_id, codigo): cada código de un proveedor apunta a un solo artículo, así la factura de compra sigue identificando el artículo sin ambigüedad. En el comparador cada código aparece como su propia fila, con su marca, precio y stock; el CA4 informa el máximo de stock entre ellas. Registralo como decisión con fecha en el ESTADO y actualizá la D2 donde esté escrita.
  >
  > Esquema: para vincular no hace falta articulo_id en proveedor_articulo. El vínculo es articulo_codigo_alternativo (proveedor_id, codigo, articulo_id), y proveedor_articulo se cruza con él por (proveedor_id, codigo). Vincular un producto del scrape es crear ese código alternativo, así la vinculación automática por código sale gratis y no se duplica el dato. Si la D2 hoy está implementada como un índice único por (proveedor_id, articulo_id) vigente, se reemplaza por el de (proveedor_id, codigo). Si los CA de la 054 piden saber quién vinculó y cuándo, agregá usuario_id y fecha a articulo_codigo_alternativo. Todo cambio de esquema va en un script nuevo, el 15, con su PR, y me pasás el bloque fish para aplicarlo en las tres bases.
  >
  > (Vinculación) Código alternativo (Recomendado).
  >
  > (Umbral 053) Mido y propongo.
- Choque de numeración: la D-S7-05 reserva el 15 para las alícuotas (D-S7-02, #194). Se lo señalo a Santi. Hasta que conteste, el script de la vinculación queda sin número.
- La D2 en el esquema: `articulo_codigo_alternativo` tiene `un_codigo_por_proveedor` UNIQUE (`articulo_id`, `proveedor_id`), que es lo que se reemplaza por (`proveedor_id`, `codigo`), y `codigo_unico_global` UNIQUE (`codigo`) solo. Este último impide que dos proveedores compartan un código, y Santi nombró únicamente la unicidad (`proveedor_id`, `codigo`). No está decidido si el global queda.

**BLOQUEADO EN:** dos respuestas de Santi: el número del script de la vinculación (el 15 es de alícuotas según la D-S7-05) y si `codigo_unico_global` queda o se reemplaza por (`proveedor_id`, `codigo`). Mientras tanto, los verificadores del #202 y la medición C6 (solo lectura sobre la demo).
- Respuestas de Santi (25/11/2026): el script de la vinculación es el **16** (el 15 sigue siendo de alícuotas, como dice la D-S7-05), y `codigo_unico_global` se **reemplaza por UNIQUE (`proveedor_id`, `codigo`)**: el mismo texto de código puede estar en dos proveedores.
- C6, datos disponibles (solo lectura, 25/11):
  - `lacentral_demo`: 31 artículos activos y códigos alternativos solo de los proveedores 1 a 5, ninguno de Expoyer (7). `lacentral_santi` no tiene SELECT sobre `proveedor_articulo` (42501).
  - `lacentral_test` (`public`): 2.644 artículos activos, sin códigos alternativos y sin filas en `proveedor_articulo`. pg_trgm 1.6 en `public`.
  - Así no hay 50 artículos propios para cruzar contra el scrape de Expoyer.

**BLOQUEADO EN:** C6, a la espera de que Santi elija cómo se accede a los datos. Sigo con el script 16, que ya está decidido.
- Respuesta de Santi sobre la C6 y reglas del día (25/11/2026), verbatim:
  > Postergá la C6: construí la 053 con el umbral configurable en 0,3 y dejá la medición pendiente en el ESTADO; se mide cuando el scrape corra contra la base real (articulos reales + catálogo juntos). No pidas GRANT ni toques permisos.
  >
  > De acá en adelante, para no frenar: las decisiones TÉCNICAS las tomás vos (esquema, índices, tipos, nombres, qué script, cómo se arma una pantalla, valores por defecto configurables), las registrás con fecha en el ESTADO y seguís. Solo parás por: (a) una regla de NEGOCIO que no esté escrita ni se deduzca de las D vigentes, (b) un cambio que exija tocar datos reales de lacentral, o (c) criterios de aceptación que se contradigan entre sí. Si tenés varias preguntas de negocio, juntalas y me las hacés todas juntas al final de la HU, no de a una.
  >
  > Reglas ya decididas hoy (25/11/2026), registralas:
  > - D2 revisada: un artículo puede tener varios códigos del mismo proveedor; la unicidad es UNIQUE(proveedor_id, codigo), no global.
  > - La vinculación es la fila de articulo_codigo_alternativo, con usuario_id y fecha (script 16). n8n no toca esa tabla.
  > - Vinculación automática: si proveedor_articulo (proveedor_id, codigo) ya existe en articulo_codigo_alternativo, está vinculado.
  > - Comparador: una fila por código vinculado, con marca, precio neto, precio de lista, estado de stock e imagen; ordenado por precio neto. CA4: estado de stock máximo entre las filas del mismo proveedor.
  > - Precio que se compara: precio_neto (costo sin IVA). El precio de lista se muestra pero no se compara.
  > - Productos vencidos (vencido = true) no aparecen en el comparador ni como candidatos.
  > - Para la prueba manual del entregable usá la demo: si necesitás vincular artículos de la demo con códigos de Expoyer o Autocor, cargalos por la pantalla o con un script de siembra de la demo, nunca en lacentral.
- Con eso, la C6 queda postergada: umbral configurable en 0,3. Desde ahora tomo yo las decisiones técnicas y las registro en el ESTADO (memoria `decisiones-tecnicas-sin-preguntar`). Las reglas del día van al ESTADO como D-S7-06 a D-S7-11, en el PR del 16.
- 25/11 · Suite completa sobre `e71b546` (rama del 16): 64 con error / 802 / 866, 2 h; los 64 son de conexión (43 «Exception while reading from stream», 9 transitorios, 5 timeout, 4 «Failed to connect», 1 transacción desechada), ninguna aserción. La base estaba saturada; la corrida no vale y el 16 todavía cambia (normalizar_busqueda).
- 25/11 · verificador-tdd #202 ronda 1: **NO APTO**. Sobreviven M7/M8 (nota ACTIVA con motivo/fecha/anulador), M14 (ANULADA sin motivo), M11 (guarda que corta por cualquier ANULADA) y M15 (requisito del 12 desactivado). Se arregla con pruebas.
- 25/11 · `a23c8fb` test(db): 4 pruebas nuevas en Script13PostgreSqlTests (NotaActivaConRastroDeAnulacion, NotaAnuladaSinMotivoOSinFecha, ConFacturaAnuladaConUsuario_ElScriptSeAplica, SinScript12_ElScriptCortaSinAplicarNada). Filtrado: 15/15 (7 min). Sigue: suite completa y ronda 2 del verificador solo sobre `a23c8fb`.
- 25/11 · Suite completa sobre `2331ee1`: `Con error! - Con error:     3, Superado:   870, Omitido:     0, Total:   873, Duración: 58 m 16 s`. Los 3 fallos son timeouts de Npgsql («The operation has timed out» y «Exception while reading from stream»), sin ninguna aserción, en clases que no tocan el 13: FacturasCompraApi `Post_SinFechaDeEmision_400YNoEscribe`, ProveedorDetalleApi `Modificar_SinTipoHabitualEnElCuerpo…` y ClienteRepositorio `DosCiclos…`. Corridos filtrados dan `Correctas! - Con error: 0, Superado: 4, Total: 4` (un nombre matchea dos pruebas). 873 = 869 + 4 nuevas.
- PR #202 actualizado (push de `a23c8fb` y de este commit de docs). `verificador-tdd` ronda 2, acotado a `a23c8fb`.
- 25/11 · `verificador-tdd` #202 ronda 2 (solo `a23c8fb`): **APTO**. M7, M8a, M8b, M14, M14b, M11 y M15 quedan muertas, cada una con su prueba en rojo. El verificador no tomó mi corrida (hash `2331ee1` ≠ HEAD, y con 3 timeouts) y corrió la suite una vez sobre `78bb15a`: `Correctas! - Con error:     0, Superado:   873, Omitido:     0, Total:   873, Duración: 55 m 2 s - LaCentral.Tests.dll (net10.0)`. El cambio de rama del árbol principal que notó fue mío (pasé a la del 16). Sigue el `auditor-persistencia`.

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

## 26/11/2026 · #202 sin conflicto y auditor; base_completa.sql con el CHECK de la D10; consulta de esquemas

**Instrucción recibida (verbatim):**
> 1) #202: quedó con conflicto tras el merge del #203 (bitácora del Sprint 7, add/add). Mergeá origin/develop en feature/s7-script-13 conservando los dos lados y terminá el auditor-persistencia. Avisame cuando esté sin conflicto y con los dos verificadores en APTO.
> 2) Javi avisó que base_completa.sql tiene el CHECK de baja estricto, y las bases vivas tienen el de la D10 (activo OR (motivo IS NOT NULL AND fecha IS NOT NULL)). Arreglalo en un PR aparte desde develop: que base_completa.sql quede igual a lo que corre en las bases, con una prueba que arme el esquema desde ese archivo y compare las restricciones contra public de lacentral_test.
> 3) Pasame una consulta de solo lectura para listar los esquemas de lacentral_test con su dueño y su tamaño, para decidir cuáles borrar. No borres nada vos.

**Plan:**
1. `feature/s7-script-13`: merge de `origin/develop` (`bda2692`) conservando los dos lados en BITACORA y ESTADO; push; `auditor-persistencia` acotado a los commits del 13, con la suite de `78bb15a` (873) — el merge solo trae docs y el re-scaffold del 14, que ya tiene su suite en el #201.
2. Rama desde `develop` para `base_completa.sql`: prueba PostgreSQL que arma un esquema desde el archivo y compara las restricciones CHECK contra `public` (rojo), corregir el archivo (verde), suite, PR, verificadores.
3. Consulta de solo lectura de esquemas con dueño y tamaño, en la respuesta.

**Estado:** en curso
- 1) Merge de `origin/develop` en `feature/s7-script-13`: conflictos add/add en `docs/sprint07/BITACORA.md` (bloque del 13 + bloque del reparto de Javi) y de contenido en `ESTADO.md` (PRÓXIMA SESIÓN): se conservan los dos lados, sin marcadores. Lo demás que entró (`Proveedor.cs`, `LaCentralDbContext.cs`, bitácora del Sprint 6) no se solapa con el 13.
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

**Estado:** terminada
- El tipo está disponible donde se decide: `ComprobanteLeido.TipoComprobante` (`"A"`, `"B"` o `null` si el código de ARCA no se reconoce); no hace falta leer nada nuevo del PDF. `origin/develop` sin commits nuevos.
- Commits: renombre `ea6b734` (sin cambio de conducta, compila); pruebas `8632734`, en rojo sobre ese commit: `Con error: 4, Superado: 52, Total: 56`, las 4 por aserción (2 Moq de factura A, 2 PostgreSQL). Una primera corrida mostró `Total: 54` sin las 2 de PostgreSQL nuevas (causa no determinada; se recompiló y se repitió) y no cuenta. Se borraron las dos clases de «exacta primero» y las pruebas de CUIT y de cada layout pasaron a comprobantes B.
- Producción `00710c0`: filtrado `Correctas! - Con error: 0, Superado: 56, Total: 56`. Graphify actualizado.
- `lacentral_demo`, solo lectura (`ro=on`): 9 proveedores; ETMAN (8), Fazio (9) y Expoyer (7) activos, sin CUIT y en `false`; hay además AUTOCOR SRL (10, con CUIT, `false`). Script `backend/db/demo/marcar_en_negro_demo.sql` escrito y **sin ejecutar** en ninguna base; sin commitear.
- Los datos de `cuit_simulado` valen para `lacentral_test`; `lacentral` está sin verificar.
- ESTADO del sprint 7: bloque nuevo (spec superada por la D-S7-12, regla vigente, deuda del alta en negro).
- Orden de Javi (26/11/2026): no repetir la suite; reintento filtrado (#190); `dotnet build --no-incremental` sin tests; mutaciones del tipo solo en clases Moq y revertir; script aprobado en commit propio sin ejecutarlo; deuda de Expoyer en la demo (decisión de Javi, no refleja la real).
- Suite completa sobre `187ba6a`: `Con error! - Con error:    42, Superado:   821, Omitido:     0, Total:   863, Duración: 1 h 36 m - LaCentral.Tests.dll (net10.0)`. 863 = 871 − 13 de «exacta primero» + 5 nuevos. Los 42: 29 `NpgsqlException` y 13 `InvalidOperationException` por fallo transitorio; ninguno de las clases del fix. Antes de la suite la compilación fue incremental.
- Reintento filtrado por nombre de los 42: `Con error: 8, Superado: 34, Total: 42` (7 de conexión y `NotasCreditoCompraApiPostgreSqlTests.Post_Devolucion_201ConIdYLocation_YQuedaEscritaConElUsuarioDelToken`, que esperaba 201 y recibió 500; la salida no muestra la causa). Segundo reintento de esos 8: `Correctas! - Con error: 0, Superado: 8, Total: 8`.
- `dotnet build --no-incremental` en `bin` falló con 4 errores de copia: un `LaCentral.Api` (PID 51212, ajeno a esta tarea, no se tocó) tiene las DLL bloqueadas. Repetido con `--artifacts-path` en una carpeta nueva: `Compilación correcta. 0 Advertencia(s) 0 Errores`.
- Mutaciones sobre la condición del tipo (52 pruebas Moq; base 52/52): `== "B"` mata 1 (`UnComprobanteDeTipoNoReconocido_…`); `== null` mata 5 (los tres `CadaLayout…`, `RazonSocialUnica_…`, `LaBusquedaPorRazonSocial_PideAlMenosDos…`). Revertidas sin commitear.
- Script y nota del README en `bb87162`; no ejecutado. Sin push, PR ni verificadores.

**Estado de la tarea:** implementación y verificación local hechas; quedan push, PR y verificadores.

## 26/11/2026 · Fix E2 · confirmación del tipo no reconocido

**Instrucción recibida (verbatim):**
> 1. Confirmado: un comprobante de tipo no reconocido (null) se asigna. La regla es «no es A». Declaralo como riesgo en el PR: un código de ARCA que el lector no traduce, en una factura A real de un proveedor cuyo CUIT no está cargado, caería a la cuenta en negro, con «Revisar». 2. Mostrame la lista de códigos de comprobante que el lector traduce a "A" y a "B", para el cuerpo del PR. 3. Sin push, sin PR, sin verificadores todavía.

**Plan:** 1. Leer los códigos en `LayoutsPdf`. 2. Anotar la confirmación y el riesgo en el ESTADO. Sin código.

**Estado:** terminada
- `LayoutsPdf.cs`: ETMAN `Nro.: (\d{2})` con `01` → A y `06` → B; Expoyer «Código N… (\d{1,2})» con `1` → A y `6` → B; Fazio fija `"B"`. Resto o sin coincidencia → `null`.
- ESTADO: confirmación, riesgo para el PR y lista de códigos. Sin commit. Quedan push, PR y verificadores.

## 26/11/2026 · Fix E2 · verificadores

**Instrucción recibida (verbatim):**
> Ya hay un solo worktree. Antes de nada: git fetch --all --prune y git log --oneline HEAD..origin/develop; si develop trajo algo, avisame antes de seguir. 1) verificador-tdd, UNA vez, acotado a los commits ea6b734, 8632734 y 00710c0 (lo que se revisa es el diff neto contra develop). Pasale: Hash 187ba6a. Línea literal: Con error! - Con error: 42, Superado: 821, Omitido: 0, Total: 863, Duración: 1 h 36 m - LaCentral.Tests.dll (net10.0). Los 42 fallos son de infraestructura (#190: 29 Npgsql y 13 de «transient failure»); ninguno de las clases del fix. Reintento filtrado: 42 → 8 con error → 8/8 verdes. build --no-incremental: 0 advertencias, 0 errores (en carpeta aparte). Mutaciones sobre la condición != "A": falla 1 con == "B" y 5 con == null. Si decide correr la suite de nuevo, que la corra. 2) auditor-persistencia, UNA vez, con el mismo alcance MÁS bb87162 (el script de demo: idempotencia, guarda de base, filas que toca). Mismos datos de la suite. Ante un NO APTO: se arregla (solo producción y tests), y se verifica solo lo arreglado, hasta 3 rondas, sin tocar la definición del verificador ni el cuerpo del PR. Sin push ni PR hasta que ambos den APTO y yo lo autorice. Mostrame cada veredicto completo.

**Plan:** 1. Fetch y comparar. 2. `verificador-tdd`. 3. `auditor-persistencia`. 4. Veredictos.

**Estado:** terminada
- `git fetch --all --prune`: `HEAD..origin/develop` vacío (`origin/develop` = `bda2692`). Un solo worktree.
- `verificador-tdd`: **APTO** sobre `ea6b734`, `8632734`, `00710c0` (HEAD `9fb0f70`). Rojo reproducido en `8632734`: 4 con error, 52 superados, 56 en total. Mutaciones: ninguna sobreviviente (U1–U7, R1–R4). Suite completa corrida por él sobre `9fb0f70`: `Con error! - Con error:     2, Superado:   861, Omitido:     0, Total:   863, Duración: 1 h 14 m` (2 de infraestructura, #190); reintento de los 2: 2/2. `build --no-incremental`: 0 advertencias, 0 errores. Worktree borrado; `git worktree list` muestra solo el principal. Hallazgos no bloqueantes: caracterización del tipo no reconocido (declarar en el PR); `proveedorId is null` y `razonSocial` no vacía parecen cláusulas no alcanzables (decide el equipo).
- `git fetch` antes del auditor: `HEAD..origin/develop` vacío.
- `auditor-persistencia`: **APTO** sobre los mismos commits más `bb87162`, sin bloqueantes. No corrió pruebas propias (usó la suite de `9fb0f70`). No bloqueantes, los dos en `marcar_en_negro_demo.sql` y hoy no alcanzables: la verificación final cuenta uno por término y no que sean DEMO-06, DEMO-07 y `00035`; `ILIKE` no normaliza acentos y la app sí. Nota: el README de demo (sección 4) y `sembrar_proveedores_pdf.sql` describen la regla anterior. Ensayó el script en un esquema propio de `lacentral_test` (guarda reescrita solo en su copia), borrado al terminar; `lacentral_demo` solo lectura.
- Encontró 5 esquemas `pr110_test_*` en `lacentral_test` que no son suyos (4 de `lacentral_javi`, 1 de `lacentral_santi`), origen desconocido. No se borraron.
- Limpieza: `git worktree list` muestra solo el principal, HEAD `9fb0f70`, `HEAD..origin/develop` vacío.
- **Abierto:** push y PR; ESTADO y esta bitácora sin commit; decidir los no bloqueantes.
- **BLOQUEADO EN:** autorización de Javi para push y PR.
- Corrección: el ESTADO **no** está sin commit (entró en `9fb0f70`); sin commit está solo esta bitácora. Falta una actualización **nueva** del ESTADO (los dos veredictos, la suite de `9fb0f70` con su reintento, el PR) antes del push y en el mismo push. La instrucción de este bloque (correr los verificadores y mostrar los veredictos) terminó; push y PR van en un bloque nuevo.
- Revisadas las cláusulas del hallazgo 3: sin layout reconocido el caso de uso corta en L33-34, y los tres layouts pasan un literal en `RazonSocial`; `proveedorId is null` está implicado por `!cuitConocido`. Ninguna de las dos es alcanzable en producción. Sin cambios: decide Javi.

## 26/11/2026 14:42 · Fix E2 · ronda posterior al APTO

**Instrucción recibida (verbatim):**
> Antes de decidir, pegame el veredicto COMPLETO de verificador-tdd, no solo las
> dos cláusulas.
> Autorizado, en este orden, commits nuevos, sin rebase, sin push:
> 1. Borrá las dos cláusulas inalcanzables de InterpretarFacturaPdfUseCase.cs:62
>    (reescribiendo el patrón de RazonSocial, que hoy descarta el nulo del
>    tipo). Dejá el test que demuestra que ese caso no puede ocurrir. Un test
>    de PostgreSQL que fije que ActualizarAsync no toca cuit_simulado (primero
>    en rojo si es posible; si ya pasa, caracterización, declaralo).
> 2. Tests filtrados en el bucle; suite completa UNA vez sobre el último commit,
>    con la base libre. Línea literal, conteo, hash. Reintento filtrado solo de
>    lo que falle por #190.
> 3. verificador-tdd acotado SOLO a los commits de esta ronda, con la suite.
> 4. Después: arreglá la verificación del script (los tres pares con activo
>    AND cuit_simulado, y la misma normalización de acentos que producción) y
>    la nota del README. Reproducí los escenarios 4, 5 y 7 del auditor en un
>    esquema temporal propio antes de commitear. auditor-persistencia acotado a
>    ESE commit.
> 5. Esquemas huérfanos: borrá los cuatro pr110_test_* del rol lacentral_javi,
>    por nombre exacto, solo si no hay conexiones activas. NO toques el de
>    lacentral_santi ni demo_ensayo_c3.
> Mostrame cada resultado. Sin push ni PR hasta mi OK.

**Plan:** 1. Veredicto completo en el chat. 2. Cláusulas y pruebas. 3. Filtradas y suite. 4. `verificador-tdd`. 5. Script y README de demo, ensayo y `auditor-persistencia`. 6. Esquemas huérfanos.

**Estado:** terminada
- `git fetch --all --prune`: `HEAD..origin/develop` vacío. HEAD `9fb0f70`, un solo worktree. Veredicto del `verificador-tdd` pegado completo en el chat.
- Paso 2 (pruebas), commit `f71533e`: `Actualizar_NoTocaCuitSimulado_NiEnTrueNiEnFalse` en PostgreSQL, **caracterización** (pasa sobre el código actual, 1/1). Discrimina: `ActualizarAsync` escribiendo `false` → 1/1 con error; escribiendo `true` → 1/1 con error; revertidas.
- Paso 2 (producción), commit `2d008a0`: sin `proveedorId is null`; `ComprobanteLeido.RazonSocial` pasa a `string` sin valor por defecto. `build --no-incremental` 0/0; Moq y PDF 92/92. Mutaciones: sin `!cuitConocido` → 2 en rojo (`ElCuitManda_...`, `UnCuitQueEsDeUnProveedorInactivo_...`); layout con razón social vacía: ETMAN 2, Fazio 3, Expoyer 1 en rojo; revertidas.
- Filtrado PostgreSQL sobre `2d008a0`: 19 pruebas, 10 verdes (las 6 de `ProveedorPostgreSqlTests` y las 4 de `ProveedorSucursalPdfPostgreSqlTests`) y 9 de `CatalogoProveedorPostgreSqlTests` (entraron por el nombre) con `FileNotFoundException` de `docs/sprint07/CONTRATO-N8N.md`: compilé con `--artifacts-path` fuera del repo. En `bin` la prueba sin base de esas 9 pasa 1/1; las 8 restantes las cubre la suite. La API ajena ya no corre.
- Base libre antes de la suite: 0 sesiones ajenas en `lacentral_test`; esquemas `pr110_test_*`: los mismos 5. Suite completa sobre `2d008a0` lanzada.


## 26/11/2026 16:02 · Fix E2 · cierre de la ronda y cuerpo del PR

**Instrucción recibida (verbatim):**
> Cuando termine la suite sobre 2d008a0:
> 1. Mostrame dónde se usa ComprobanteLeido fuera de UseCases (Api, DTOs,
>    frontend). Si RazonSocial llega a un contrato expuesto, pará y avisame.
> 2. verificador-tdd acotado SOLO a f71533e y 2d008a0; pasale la línea de esa
>    suite y aclarale que las 9 de CatalogoProveedor del filtrado fallaron por
>    compilar fuera del repo.
> 3. Después, el arreglo del script y el auditor sobre ese commit, y los 4
>    esquemas pr110_test_* de lacentral_javi.
> 4. Cuerpo del PR: declará (a) el test de caracterización del tipo no
>    reconocido y el de ActualizarAsync, (b) la limpieza de las cláusulas
>    preexistentes como cambio aparte, con su motivo, (c) que 00710c0 toca dos
>    archivos de prueba para compilar, (d) las dos corridas de la suite con sus
>    fallos de #190 y reintentos, (e) la deuda del alta en negro.
> Sin push ni PR hasta mi OK.

**Plan:** 1. Usos de `ComprobanteLeido` (solo lectura, no toca la base ni la suite). 2. Esperar la suite. 3. `verificador-tdd`. 4. Script y README de demo, ensayo, commit y `auditor-persistencia`. 5. Cuatro esquemas. 6. Cuerpo del PR (borrador, sin abrirlo). Continúa el bloque anterior, que se cierra junto con este.

**Estado:** terminada
- Paso 1: `ComprobanteLeido` fuera de `UseCases` solo en `backend/LaCentral.Tests/FacturasCompra/LecturaPdfTests.cs:21`. `Api` y `frontend/src`: sin referencias. `POST /api/facturas-compra/pdf` devuelve `FacturaPdfInterpretada`, sin razón social (igual que la interfaz de `api.ts`). No llega a un contrato expuesto: se sigue. Las dos cláusulas retiradas ya estaban en `origin/develop`.
- **BLOQUEADO EN:** el fin de la suite completa sobre `2d008a0` (en segundo plano).

## 26/11/2026 16:20 · Fix E2 · el script de demo queda solo con Fazio

**Instrucción recibida (verbatim):**
> Cambio de Javi sobre marcar_en_negro_demo.sql: queda SOLO Fazio (DEMO-07).
> Sacá Expoyer (trabaja solo en negro; el blanco se resuelve por CUIT y no se
> marca) y sacá ETMAN (se decide aparte). Actualizá el encabezado, el README, el
> término de la verificación final y la línea del ESTADO que decía «Expoyer se
> marca en negro por decisión de Javi»: bloque NUEVO en la bitácora que lo
> revierta, sin reescribir el viejo. La verificación final con los pares
> (codigo, razon_social) y la normalización de acentos se arregla igual.
> Anotá como deuda, para un script de demo aparte: los gemelos en blanco con CUIT
> real (Fazio, Expoyer) y ETMAN, según los PDFs de Javi. No va en este PR.

**Plan:** Entra en el paso del script, después del `verificador-tdd`: 1. Script, README y ESTADO solo con Fazio. 2. Ensayo en esquema propio (escenarios 4, 5 y 7, este con un nombre acentuado de Fazio). 3. Deuda en el ESTADO y en el cuerpo del PR.

**Estado:** terminada
- **Revierte** la decisión anterior de que Expoyer se marca en negro en `lacentral_demo`: desde hoy el script marca solo a Fazio (`DEMO-07`). Expoyer y ETMAN salen. Los bloques anteriores no se tocan.
- (bloque «cierre de la ronda») Suite completa sobre `2d008a0` (14:56–16:14): `Con error! - Con error:    95, Superado:   769, Omitido:     0, Total:   864, Duración: 1 h 17 m`. Los 95 son de infraestructura (#190): 89 `NpgsqlException`/timeout y 6 «transient failure»; ninguna aserción.
- **Corrección:** el «0 sesiones ajenas» anterior a la suite no vale. La consulta filtraba por `backend_type`, que PostgreSQL oculta para sesiones de otros roles. Sin ese filtro (16:2x) hay 9 sesiones de `lacentral_santi`, 2 con nombre de esquema de prueba y un esquema suyo con 20 candados. No se sabe si estaba corriendo pruebas durante la suite.
- Esquemas nuevos de `lacentral_javi` que no estaban antes de la suite, sin candados: `pr110_test_959576f5a2e94cd8b7672947a4edb9ea`, `pr110_test_9f995e9519264abc8e01c6037ea15eb3`, `pr110_test_ef4770ee9a5b40d8a365e102c526810f`. No están entre los cuatro autorizados: no se borran.
- **BLOQUEADO EN:** una ventana con la base libre (Santi la está usando). El reintento de los 95 métodos fallidos queda en segundo plano y arranca solo tras dos chequeos seguidos sin pruebas ajenas (tope: 60 chequeos).
- Reintento filtrado de los 95 métodos sobre `2d008a0` (19:14–19:38, tras dos chequeos libres a las 19:11 y 19:14): `Correctas! - Con error:     0, Superado:    95, Omitido:     0, Total:    95, Duración: 23 m 54 s`. Al terminar, 0 sesiones y 0 candados ajenos visibles; sin esquemas nuevos.
- **Corrección de horas:** la lectura de los fallos y el chequeo de las sesiones de Santi fueron hacia las 18:41, no «16:2x»; el bloque «el script de demo queda solo con Fazio» tampoco se abrió a las 16:20 sino hacia las 18:40.
- Antes del verificador: `git fetch --all --prune`, `HEAD..origin/develop` vacío; un solo worktree; HEAD `2d008a0`. `verificador-tdd` lanzado sobre `f71533e` y `2d008a0`.
- `verificador-tdd` sobre `f71533e` y `2d008a0`: **APTO**. Caracterización 1/1 sobre `f71533e`; 8 mutaciones del diff, ninguna sobrevive; reprodujo las declaradas; `build --no-incremental` 0/0; no repitió la suite (usó la de `2d008a0` y su reintento). No bloqueante: `string` no impide una razón social vacía; hoy no es alcanzable, y un cuarto layout sin su caso de prueba no tendría esa protección. Worktree borrado.
- (bloque «solo Fazio») `782a871`: script y README de demo, solo con Fazio. Ensayo en un esquema propio `e2_ensayo_1543243c` de `lacentral_test` (guarda reescrita solo en memoria), releyendo con conexión nueva; borrado al terminar. Script anterior: los escenarios 5 («Nafazio» en negro con el objetivo renombrado) y 7 («Fázio Sur») terminaban en OK; el 4 ya cortaba. Script nuevo: los 10 casos (1 a 9 y 5b) como se esperaba; cambia solo `DEMO-07` en la primera corrida y nada en la segunda. El 7 se adaptó de Expoyer a Fazio. No se aplicó a `lacentral_demo`.
- ESTADO (sin commit): script solo con Fazio y la deuda de los gemelos en blanco y ETMAN, en lugar de la línea de Expoyer.
- `sembrar_proveedores_pdf.sql` (líneas 10-11) todavía describe la regla anterior a la D-S7-12: no se tocó, no estaba en el pedido.
- `auditor-persistencia` lanzado sobre `782a871` (HEAD), con la suite de `2d008a0` y su reintento; declarado que el commit del script queda fuera de esa corrida.
- **BLOQUEADO EN:** el veredicto del `auditor-persistencia` (en segundo plano).
- `auditor-persistencia` sobre `782a871`: **persistencia verificada contra el motor real, sin hallazgos bloqueantes**. Ensayo propio de 16 casos en `aud_e2_cb082bf1` (borrado), `fallos=0`; paridad con `BuscarActivosEnNegroPorRazonSocialAsync` sobre 12 nombres; `lacentral_demo` solo SELECT (10 proveedores, 0 activos en negro: ninguna versión del script está aplicada). No corrió la suite sobre `782a871`. No bloqueantes: (1) en la demo existe `00037 «NESTOR FAZIO S.A.»`, con CUIT y sin marcar; (2) deducido, sin ejecutar: por esa fila, volver a correr `sembrar_proveedores_pdf.sql` cortaría; (3) el README no dice la consecuencia para un PDF de Expoyer. Worktree borrado.
- Paso de esquemas: los cuatro autorizados, por nombre exacto, con chequeo de conexiones.
- Esquemas: borrados los cuatro autorizados por nombre exacto, en una transacción (antes: 4/4 propios, 0 sesiones con esos nombres, 0 candados, 0 otras sesiones de mi rol; 536 relaciones adentro; 8885 objetos de afuera controlados, 0 faltantes). Releído con conexión nueva: quedan `demo_ensayo_c3`, tres `pr110_test_*` de `lacentral_santi`, `staging` y los tres nuevos de `lacentral_javi` (no autorizados).
- `graphify update .`: 4521 nodos, 13663 aristas. ESTADO al día (sin commit). Cuerpo del PR redactado en el scratchpad de la sesión (`pr/cuerpo-pr.md`), sin abrir el PR.
- Cierre de los tres bloques de hoy (14:42, 16:02 y el del script de demo). **Abierto:** commit de ESTADO y bitácora, push y PR (sin merge); la suite completa no corrió sobre `782a871`; tres esquemas huérfanos nuevos de `lacentral_javi`.
- **BLOQUEADO EN:** el OK de Javi para commitear los documentos, pushear y abrir el PR.
- Revisión final: `git diff --stat 2d008a0 HEAD` toca solo `backend/db/demo/` (2 archivos, sin `.cs` ni `.csproj`). `git blame` en `origin/develop`: las líneas de producción que cambian son de los commits del Fix E (`c3e1d93`, `6ce13f3`). ESTADO: PRÓXIMA SESIÓN del 26/11 y `bb87162` marcado como reemplazado.

## 27/11/2026 09:16 · estado del reintento y actividad de la base

**Instrucción recibida (verbatim):**
> Probemos ahora. Antes de lanzar nada, mostrame el estado: qué quedó del
> reintento que esperaba en segundo plano (si corrió, su línea literal), y
> si hay procesos tuyos vivos.
> Después revisá la actividad de otros roles en lacentral_test SIN el filtro que
> ocultaba sesiones ajenas: cuántas conexiones hay y de qué rol. Si hay sesiones
> de lacentral_santi con consultas activas, no lances nada y decímelo; si solo
> hay conexiones inactivas o ninguna, lanzá el reintento filtrado de los 95
> fallos de la suite sobre 2d008a0.
> Pasame la línea literal y, si algo falla, solo los nombres de lo que falló
> (no las trazas).
> Sin push, sin PR, sin verificadores todavía.

**Plan:** 1. Log del reintento anterior y procesos vivos. 2. Actividad sin filtrar. 3. Reintento según corresponda. Solo lecturas hasta decidir.

**Estado:** terminada
- Estado: el reintento anterior corrió el 26/11 (19:14–19:38) sobre `2d008a0`: `Correctas! - Con error:     0, Superado:    95, Omitido:     0, Total:    95, Duración: 23 m 54 s`. Sin procesos `dotnet`, `testhost` ni `bash` vivos. Actividad sin filtrar (09:16:54–09:17:55, cuatro muestras): 0 conexiones de cualquier rol.
- Reintento relanzado por pedido: los mismos 95 métodos, `--no-build` sobre los binarios del build de `2d008a0` (26/11 14:55); `2d008a0..HEAD` solo toca `backend/db/demo/`. Chequeo previo: `lacentral_santi` sin conexiones. Log nuevo `reintento2.log`.
- **BLOQUEADO EN:** el fin del reintento en segundo plano.
- Reintento relanzado (27/11, 09:19–09:27), mismos 95 métodos sobre los binarios de `2d008a0`: `Correctas! - Con error:     0, Superado:    95, Omitido:     0, Total:    95, Duración: 7 m 42 s`. Antes y después: `lacentral_santi` sin conexiones.
- **Abierto:** sin cambios; falta el OK de Javi para commitear los documentos, pushear y abrir el PR, y decidir si la suite se declara o se repite.

## 27/11/2026 09:29 · verificaciones, commit docs y cuerpo del PR

**Instrucción recibida (verbatim):**
> No hay OK de push todavía. Faltan dos verificaciones, en este orden:
> 1. Mostrame git log --oneline -10 y git diff --stat 2d008a0..HEAD, y el
>    contenido final del script de demo, con el resultado del ensayo de los
>    escenarios 4, 5 y 7 en esquema temporal propio.
> 2. Invocá verificador-tdd UNA vez, acotado SOLO a f71533e y 2d008a0. Pasale:
>    - Suite sobre 2d008a0: Con error! - Con error: 95, Superado: 769, Omitido:
>      0, Total: 864, Duración: 1 h 17 m (los 95 de #190, base ocupada).
>    - Reintento de esos 95 sobre los mismos binarios, con la base libre (0
>      conexiones de otros roles antes y después): Correctas! - Con error: 0,
>      Superado: 95, Total: 95, Duración: 7 m 42 s.
>    - 2d008a0..HEAD solo toca backend/db/demo/.
>    - Mutaciones y build limpio de 2d008a0.
> 3. Después, auditor-persistencia acotado SOLO al commit del script (782a871 y
>    los del arreglo).
> 4. Cuando ambos den APTO: ESTADO y bitácora en un commit docs, y redactame el
>    cuerpo del PR para que yo lo lea ANTES de pushear.
> Si alguno da NO APTO: se arregla y se verifica solo lo arreglado, hasta 3 rondas.
> Sin push ni PR.

**Plan:** 1. Fetch y comparar. 2. Commits, diff, script y ensayo. 3. Verificadores: comprobar primero qué ya corrió sobre estos commits. 4. Commit docs. 5. Cuerpo del PR.

**Estado:** terminada
- `git fetch --all --prune`: `HEAD..origin/develop` vacío. HEAD `782a871`, un solo worktree, `backend/` limpio. Mostrados `git log --oneline -10`, `git diff --stat 2d008a0..HEAD` (solo `backend/db/demo/`), el script del commit y el ensayo del 26/11 (blob `a028eb89`, el mismo que se ensayó).
- Verificadores: no se relanzan sin confirmación. Ya corrieron el 26/11 sobre el alcance pedido: `verificador-tdd` APTO sobre `f71533e` y `2d008a0` (HEAD `2d008a0`); `auditor-persistencia` «verificada contra el motor real», sin bloqueantes, sobre `782a871`, que sigue siendo HEAD. No hay commits de arreglo. La regla del repo es no invocarlos dos veces sobre el mismo HEAD.
- **BLOQUEADO EN:** que Javi decida si valen esos veredictos (y sigue el commit de docs y el cuerpo del PR) o se relanzan los dos.

## 27/11/2026 10:13 · tres PDF reales por el lector, antes del cuerpo del PR

**Instrucción recibida (verbatim):**
> Autorizado: dar por válidos los verificadores del 26/11 (tdd sobre f71533e y 2d008a0; auditor sobre 782a871). No se relanzan.
>
> Antes del cuerpo del PR, un paso nuevo, solo lectura y sin base: pasá por el reader (el mismo que usa InterpretarFacturaPdfUseCase) estos tres PDFs reales y pegame, por cada uno, RazonSocial, Cuit y TipoComprobante tal como los extrae:
> 1. Presupuesto_49108.pdf (Fazio, negro). Imprime solo el CUIT del cliente BOSAC S.A.S. ([CUIT del cliente (BOSAC)]) y ningún nombre de proveedor; el encabezado es "***".
> 2. Factura_A000100099753.pdf (Nestor Fazio SA, A). Imprime CUIT [CUIT de la SA] arriba y, al pie, "por cuenta y orden de el titular (CUIT [CUIT del titular])".
> 3. 10713-VT-FA0137-26150.pdf (ETMAN / MULTIORIGINAL PARTS S.A., A). CUIT [CUIT de ETMAN]; el cliente es el mismo BOSAC.
> Los PDFs están en la carpeta de Javi; si no los ves, avisá y los pasa él.
>
> Qué quiero saber:
> a) Si en el presupuesto el reader toma el CUIT de BOSAC como CUIT del proveedor, y qué RazonSocial saca (si saca la del cliente, la regla busca "BOSAC").
> b) Qué CUIT toma en la factura de Fazio (el primero o el de cuenta y orden).
> c) Si los tres dan el TipoComprobante esperado (B, A, A).
> No cambies código por esto. Si el resultado muestra un problema, reportalo y paramos ahí.
>
> Después: commit de docs (ESTADO.md antes del push, bitácora append-only) y el cuerpo del PR para que Javi lo lea ANTES de pushear. En "sin verificar" declará lo que salga de este paso, y que el recorrido con PDFs reales sobre lacentral_demo (DoD 10) está pendiente. Sin push ni PR hasta el OK de Javi.

**Plan:** 1. Fetch y comparar. 2. Lector real sobre los tres PDF, sin base, desde un script en el scratchpad (los PDF no se copian al repo). 3. Reportar; si hay un problema, parar. 4. Si no, commit de docs y cuerpo del PR.

**Estado:** terminada
- Decisión de Javi: valen los verificadores del 26/11; no se relanzan. Se levanta el bloqueo del bloque anterior.
- Lector real (`LectorTextoPdf` + `LayoutsPdf.Leer`, sin base, script en el scratchpad; los PDF no se copiaron): `Presupuesto_49108.pdf` → razón social «Fazio» (fija del layout), un solo CUIT (el del cliente), tipo B, 2 líneas. `Factura_A000100099753.pdf` → **ningún layout lo reconoce** (`null`): sin razón social, CUIT ni tipo. `10713-VT-FA0137-26150.pdf` → «ETMAN», dos CUIT (ETMAN y el cliente, en ese orden), tipo A, 1 línea.
- `LayoutsPdf` y `LectorTextoPdf` no cambian en este PR: la falta del layout de la factura A de Fazio es previa.
- **BLOQUEADO EN:** problema en el punto (c); la instrucción manda parar. Falta que Javi decida si sigue el commit de docs y el cuerpo del PR declarándolo, y qué se hace con los CUIT reales del bloque (el repo es público).

## 27/11/2026 10:22 · enmascarar CUIT, commit docs, cuerpo del PR y consulta en la demo

**Instrucción recibida (verbatim):**
> Respuestas:
> 1. Los CUIT se enmascaran en la bitácora (antes del primer commit) y en el cuerpo del PR: referirse a ellos como "CUIT de la SA", "CUIT del cliente (BOSAC)", "CUIT del titular", con una nota en la bitácora "(CUIT omitidos: repo público)". Ningún CUIT, ni de empresa ni de persona, entra al repo.
> 2. La factura A de Fazio sin layout NO se arregla en este PR. Declaralo en el PR como hallazgo fuera de alcance: no hay layout para la factura A de Fazio (solo el del presupuesto); el texto sí se extrae; al escribirlo, cuidar que el CUIT del proveedor es el primero y el de "cuenta y orden" es el último. Proponé el texto de un issue aparte; no lo abras, lo decide Javi.
> 3. Seguí: commit de docs (ESTADO.md antes del push, bitácora append-only), y el cuerpo del PR para que Javi lo lea antes de pushear. En el cuerpo, declará lo que salió de los PDFs: presupuesto de Fazio OK (razón social fija del layout, tipo B, el CUIT de BOSAC se toma pero no coincide con ningún proveedor); ETMAN OK (tipo A, descuento4 para revisar); Fazio A no reconocida. En "sin verificar": si algún proveedor tiene esos CUIT (condición: BOSAC no cargado como proveedor), importes y percepciones contra los PDF, y el recorrido de DoD 10 sobre lacentral_demo.
> 4. Chequeo de solo lectura en lacentral_demo, SELECT únicamente: ¿existe algún proveedor con el CUIT de BOSAC? Reportá cantidad, sin mostrar el CUIT. En lacentral (real) no consultes nada.
> Sin push ni PR hasta el OK de Javi.

**Plan:** 1. Enmascarar. 2. La consulta del punto 4 primero, porque el cuerpo del PR afirma algo que depende de ella. 3. ESTADO y bitácora. 4. Cuerpo del PR y texto del issue. 5. Commit de docs.

**Estado:** terminada
- (CUIT omitidos: repo público) Por decisión de Javi se reemplazaron los CUIT del bloque «tres PDF reales», antes del primer commit; es el único cambio sobre esas líneas.
- Punto 4: un `SELECT` en `lacentral_demo`, sesión de solo lectura verificada: 10 proveedores, 2 con CUIT, **0 con el CUIT del cliente (BOSAC)**. `lacentral` no se consultó.
- Escaneo de patrones de CUIT (con y sin guiones) en las líneas agregadas por la rama, los mensajes de commit y los cambios sin commitear: ninguno.
- La factura A de Fazio queda como hallazgo fuera de alcance; el issue se propone sin abrirlo. ESTADO actualizado.
- Cuerpo del PR actualizado y texto del issue de la factura A de Fazio, los dos en el scratchpad de la sesión (`pr/cuerpo-pr.md`, `pr/issue-factura-a-fazio.md`); el PR y el issue no se abrieron. Sin patrones de CUIT en ellos, en la bitácora ni en el ESTADO.
- La suite completa no se repitió sobre el HEAD: se declara en el cuerpo del PR la de `2d008a0` con sus reintentos.
- Commit de docs: ESTADO y esta bitácora. Cierra los bloques «verificaciones, commit docs y cuerpo del PR», «tres PDF reales por el lector» y este.
- **Abierto:** push y PR (sin merge); decidir el issue; recorrido de DoD 10 en `lacentral_demo`.
- **BLOQUEADO EN:** el OK de Javi para pushear y abrir el PR, después de leer el cuerpo.

## 27/11/2026 10:30 · ajustes al cuerpo del PR y nombre del titular, antes del push

**Instrucción recibida (verbatim):**
> Javi aprueba el cuerpo del PR con tres ajustes, antes de pushear:
> 1. En "Demo": cambiá "ETMAN y Expoyer se eligen a mano" por "ETMAN y Expoyer se resuelven por CUIT si están cargados, o se eligen a mano".
> 2. Grep en toda la rama (diff y mensajes de commit) y en el cuerpo del PR e issue por el nombre completo de la persona titular del "por cuenta y orden" (el que aparece en el pie de la factura de Fazio). Reportá solo la cantidad de coincidencias, sin pegar el nombre. Si hay alguna, reemplazala por "el titular" y recommiteá solo docs.
> 3. Antes de pushear: git fetch --all --prune y git log --oneline HEAD..origin/develop. Si trae commits, aplicá la regla de la sección 3 del CLAUDE.md.
> Suite: Javi va a intentar una ventana con la base libre. Hasta que él te diga "corré la suite", no la corras. Si te dice que no hay ventana, pusheás con lo declarado.
> Con el OK de Javi: push de la rama y PR con este cuerpo, que cierra con la línea de atribución de siempre. NO abras el issue de la factura A de Fazio; lo decide Javi.

**Plan:** 1. Fetch y comparar. 2. Frase de la demo. 3. Contar el nombre, reemplazar y recommitear docs. 4. Esperar el OK de Javi (suite o push).

**Estado:** terminada
- `git fetch --all --prune`: `HEAD..origin/develop` vacío (`bda2692`); la rama no está en el remoto.
- Cuerpo del PR: ajustada la frase de «Demo».
- (nombre del titular reemplazado por «el titular»: repo público) 1 coincidencia en la rama, en el bloque «tres PDF reales», dentro del commit de docs sin pushear; 0 en mensajes de commit, cuerpo del PR e issue. Se rehizo ese commit (`--amend`) para que no quede en la historia que se publica.
- **Abierto:** la suite (solo si Javi pide correrla), push y PR sin merge; el issue lo decide Javi.
- **BLOQUEADO EN:** Javi: ventana para la suite u OK para pushear con lo declarado.

## 27/11/2026 10:33 · suite completa sobre ec3d0ee en la ventana

**Instrucción recibida (verbatim):**
> Corré la suite

**Plan:** 1. Fetch y comparar. 2. Comprobar que la base esté libre, sin filtrar sesiones ajenas. 3. `build --no-incremental` y suite completa sobre `ec3d0ee`. 4. Reintento filtrado solo de lo que falle por #190. 5. Cuerpo del PR, ESTADO y esta bitácora. El push espera el OK de Javi.

**Estado:** terminada
- `git fetch --all --prune`: `HEAD..origin/develop` vacío; HEAD `ec3d0ee`, un solo worktree, sin procesos `dotnet` vivos.
- Base antes de lanzar (10:33:49–10:34:49, cuatro muestras sin filtrar): 0 conexiones de cualquier rol.
- `build --no-incremental` y suite completa lanzados sobre `ec3d0ee` en segundo plano, con una muestra de actividad de otros roles cada 10 minutos.
- **BLOQUEADO EN:** el fin de la suite.
- Suite completa sobre `ec3d0ee` (27/11, 10:35–10:53): `Correctas! - Con error:     0, Superado:   864, Omitido:     0, Total:   864, Duración: 17 m 38 s`. `build --no-incremental`: 0 advertencias, 0 errores. Sin reintentos.
- Otros roles durante la corrida (10:35, 10:45, 10:53): 0 conexiones, 0 en transacción. Sin esquemas de prueba nuevos al terminar. `HEAD..origin/develop` vacío.
- Cuerpo del PR y ESTADO actualizados; commit de docs aparte, posterior a la corrida.
- **Abierto:** push y PR sin merge; decidir el issue; recorrido de DoD 10.
- **BLOQUEADO EN:** el OK de Javi para pushear y abrir el PR.

## 27/11/2026 10:55 · push y PR del Fix E2

**Instrucción recibida (verbatim):**
> OK de Javi: repetí el fetch (git log --oneline HEAD..origin/develop). Si viene vacío, push de fix/proveedor-razon-social-exacta y abrí el PR contra develop con el cuerpo actual (cita ec3d0ee y 447e17c, cierra con la línea de atribución). Sin merge. NO abras el issue de la factura A de Fazio. Si develop trae commits, aplicá la sección 3 del CLAUDE.md y parás a reportar antes de pushear.
> Cuando esté abierto, pasame el número y el link del PR, y anotá en la bitácora el número de PR (el ESTADO ya está actualizado antes del push; si necesitás cambiarlo por el número, hacelo en un commit docs aparte antes del push o decime y lo dejamos como deuda).

**Plan:** 1. Fetch; si develop trae commits, parar. 2. Revisar lo que se publica y pushear `447e17c`. 3. PR contra `develop`. 4. Número del PR en esta bitácora. Sin merge y sin issue.

**Estado:** terminada
- `git fetch --all --prune` y `git log --oneline HEAD..origin/develop`: vacío (`bda2692`). Escaneo previo de lo publicado: sin CUIT, sin el nombre del titular y sin claves.
- Push de `447e17c` (22 commits sobre `develop`) y **PR #204** contra `develop`: https://github.com/AAAActis/la-central-gestion/pull/204 . Abierto, sin merge ni auto-merge, `MERGEABLE`, revisión requerida, sin checks de CI informados. El cuerpo publicado es el archivo aprobado. El issue de la factura A de Fazio no se abrió.
- Esta anotación queda sin commitear, posterior al push.
- **Deuda:** el ESTADO publicado todavía dice que el push y el PR esperan el OK de Javi y no nombra el #204; se corrige en un commit de docs cuando Javi lo autorice.
- **Abierto:** revisión y merge del #204 (no es de este asistente); commit de docs con el número; decidir el issue; recorrido de DoD 10 en `lacentral_demo`.

## 27/11/2026 10:57 · cierre del Fix E2: docs en el PR #204 y esquemas huérfanos

**Instrucción recibida (verbatim):**
> Autorizado, en este orden:
> 1. Commit de docs (ESTADO.md y BITACORA.md): el ESTADO publicado todavía dice que el push y el PR esperan OK y no nombra el #204; corregilo (PR #204 abierto el 27/11, esperando revisión de Santi; Fix E2 cerrado de nuestro lado; deuda: script de demo sin aplicar, DoD 10 pendiente, issue Fazio A sin abrir, alta en negro). Bitácora: el número del PR y el cierre de la tarea (Estado: terminada, con lo que queda abierto). Sin CUIT ni nombre del titular. Push de ese commit y sumalo al PR: es solo documentación, no relances verificadores ni suite; dejá una línea en el PR (comentario) diciendo qué commit docs se agregó.
> 2. Borrá los tres esquemas pr110_test_* huérfanos de lacentral_javi, SOLO en lacentral_test: primero listalos con su dueño, confirmá que el dueño es lacentral_javi y que no hay conexiones de otros roles, y borralos por nombre exacto (DROP SCHEMA ... CASCADE, uno por uno, sin comodines). Pegame el antes y el después.
> 3. Después parás. La rama de Enter la sigo yo.

**Plan:** 1. Fetch y comparar. 2. ESTADO y bitácora. 3. Commit, push y comentario en el #204. 4. Los tres esquemas. 5. Parar.

**Estado:** terminada
- `git fetch --all --prune`: `HEAD..origin/develop` vacío. #204 abierto en `447e17c`, sin revisiones ni comentarios.
- Fix E2: **PR #204** (https://github.com/AAAActis/la-central-gestion/pull/204), abierto el 27/11, esperando revisión de Santi; cerrado de nuestro lado. ESTADO corregido. Este commit es solo documentación: no se relanzan verificadores ni suite.
- **Abierto:** revisión y merge del #204; `marcar_en_negro_demo.sql` sin aplicar; DoD 10 pendiente; issue de la factura A de Fazio sin abrir; alta en negro; borrado de los tres esquemas huérfanos, autorizado, que se ejecuta después de este commit.
- Commit de docs `3763e15` pusheado al #204 y comentario dejado en el PR (https://github.com/AAAActis/la-central-gestion/pull/204#issuecomment-7220174143). Antes: sin CUIT ni nombre del titular, `HEAD..origin/develop` vacío.
- Esquemas en `lacentral_test`: borrados uno por uno y por nombre exacto `pr110_test_959576f5a2e94cd8b7672947a4edb9ea`, `pr110_test_9f995e9519264abc8e01c6037ea15eb3` y `pr110_test_ef4770ee9a5b40d8a365e102c526810f`. Antes: los tres de `lacentral_javi`, 0 conexiones de otros roles y 0 candados. Después, releído con conexión nueva: quedan `demo_ensayo_c3`, tres `pr110_test_*` de `lacentral_santi` y `staging`. Esta anotación es posterior al commit y queda sin commitear.

## 27/11/2026 11:00 · último commit de docs del Fix E2 y paso a la rama de Enter

**Instrucción recibida (verbatim):**
> Un último commit de docs en fix/proveedor-razon-social-exacta: corregí el ESTADO (los esquemas huérfanos de lacentral_javi YA se borraron, 3 de 3, el 27/11; quedan los de lacentral_santi: demo_ensayo_c3 y tres pr110_test_*, a resolver por Santi) y commiteá las anotaciones pendientes de la bitácora (push, comentario del PR, borrado). Sin CUIT ni nombre del titular. Push al PR (solo documentación, sin verificadores ni suite). Dejá el árbol limpio: git status debe mostrar solo .codex/ y AGENTS.md sin versionar. Pegame ese git status y el hash.
> Después, sin tocar nada más, seguí con la rama de Enter en esta misma carpeta:
> 1. git worktree list (debe haber solo el principal).
> 2. git fetch --all --prune; git log --oneline HEAD..origin/develop. Si develop trae commits, cruzalos con lo que toca la rama de Enter (git diff --name-only) y reportá antes de mergear (sección 3 del CLAUDE.md).
> 3. git checkout fix/enter-codigo-exacto; git branch --unset-upstream. Si el checkout se queja, parás y me pegás el error.
> 4. Leé el último bloque en curso de la bitácora de esa rama, actualizá su ESTADO.md ANTES del push, y pegame git log --oneline origin/develop..HEAD y git diff --stat contra develop.
> Sin push ni verificadores todavía: te digo cuándo.

**Plan:** 1. ESTADO y bitácora, commit y push al #204. 2. Los cuatro pasos de la rama de Enter, que se registran en la bitácora de esa rama.

**Estado:** terminada
- ESTADO corregido: los tres esquemas de `lacentral_javi` figuran como borrados y los de `lacentral_santi` como pendientes de Santi. Va en este commit junto con las anotaciones del push, el comentario y el borrado. Solo documentación: sin verificadores ni suite.
- **Abierto en esta rama:** revisión y merge del #204. La segunda parte de la instrucción sigue en la bitácora de `fix/enter-codigo-exacto`.

## 27/11/2026 · #202: merge de develop, suite y auditor

La instrucción verbatim está en el bloque «27/11/2026 18:40 · Retomar el Sprint 7…» de la bitácora de `feature/127-vincular`. Este es el rastro del punto 1 de esa cola en `feature/s7-script-13`.

**Estado:** en curso
- Merge de `origin/develop` (`80ede7a`: #204 y #205) → `a05158a`. Conflictos solo en `BITACORA.md` y `ESTADO.md` (PRÓXIMA SESIÓN); se conservan los dos lados. Lo que entró (Fix E2, Enter exacto) no toca el 13.
- Suite completa sobre `a05158a` (`dotnet build`: 0 errores; `dotnet test --no-build`, 18:42–19:10): `Correctas! - Con error:     0, Superado:   879, Omitido:     0, Total:   879, Duración: 28 m 14 s - LaCentral.Tests.dll (net10.0)`. 873 + las 6 que trajo `develop`. Muestras de `pg_stat_activity` durante la corrida, en el #190 (comentario 7229100324).
- Este commit es solo bitácora y queda fuera de la corrida.
- `auditor-persistencia` (ronda 1, acotado a `f944258`, `2b3e9d5`, `0d4ec50`, `a23c8fb`, con la suite de `a05158a`; no corrió nada): **NO APTO.** `ConsultarFacturasCompraPostgreSqlTests.cs:31-36` y `RegistrarFacturaCompraPostgreSqlTests.cs:648-654` siembran facturas ANULADA sin `UsuarioAnulacionId`. Pasan hoy porque `public` no tiene el 13; con el 13 aplicado, el fixture clona `fc_anulacion_con_usuario` y dan 23514. La misma forma de fila ya está probada contra el motor en `FacturaAnuladaSinQuienAnulo_LaRechazaLaBase`, que espera ese rechazo. Lo que está bien según el auditor: no hay Ids sembrados, todo se relee con una conexión nueva, los CHECK coinciden con lo que escribe producción, la atomicidad está probada y `PostgreSqlSeguridadTests` sigue intacto.
- Arreglo `7fd6f8a` (solo pruebas): los dos seeds ponen quién anuló (`UsuarioAnulacionId`), que ya hoy es válido. Filtrado `ConsultarFacturasCompraPostgreSqlTests|RegistrarFacturaCompraPostgreSqlTests`: `Correctas! - Con error:     0, Superado:    32, Omitido:     0, Total:    32, Duración: 11 m 37 s`. No hay rojo previo que mostrar en `public`: el rojo depende de que el 13 esté aplicado, y ya lo muestra la prueba citada.
- Suite completa sobre `7fd6f8a` (19:27–19:54): `Correctas! - Con error:     0, Superado:   879, Omitido:     0, Total:   879, Duración: 27 m 27 s - LaCentral.Tests.dll (net10.0)`. Reemplaza a la de `a05158a`. Auditor, ronda 2, acotada a `7fd6f8a`.
