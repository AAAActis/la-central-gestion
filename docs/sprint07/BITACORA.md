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
- Paso 2a (053/054, esquema): rama `feature/127-vinculacion-esquema` desde `origin/develop` (`07fd8e7`).
  - Script 16 (`16_Vinculacion_Codigo_Proveedor.sql`): crea UNIQUE (`proveedor_id`, `codigo`) `codigo_alt_proveedor_codigo_unico` y borra `codigo_unico_global`, `un_codigo_por_proveedor` y `ix_codigo_alt_proveedor` (los índices son índices, no restricciones). Agrega `usuario_id` y `fecha_vinculacion`, con el CHECK `aca_vinculacion_coherente` (los dos o ninguno). Idempotente; requiere el 12.
  - Decisiones técnicas: la ficha del artículo (alta y modificación) sigue con un código por proveedor, como hoy, y los códigos extra del mismo proveedor entran por la vinculación de la 054. Los mensajes del manejador y `Modificar_ElCodigoChocaEnElMotor…` (que dependen de `codigo_unico_global`) se cambian en el PR del re-scaffold, porque `RestriccionesUnicasPostgreSqlTests` exige que cada nombre del mapa exista en la base. Entre la aplicación del 16 y ese PR, esas dos pruebas quedan en rojo.
  - Pruebas `Script16PostgreSqlTests` (8), con el 16 en esqueleto (BEGIN/COMMIT vacíos) para el rojo.
- Rojo `0e28343` (el 16 en esqueleto): `Con error: 8, Superado: 0, Total: 8`, cada una por su razón (23505 `codigo_unico_global` y `un_codigo_por_proveedor`, 42703 `usuario_id`, nombre del índice, aviso ausente). En `f8aef6d` (el 16 real) las 8 fallaron con 2BP01: `codigo_unico_global` y `un_codigo_por_proveedor` son **restricciones** UNIQUE, no índices sueltos. Lo de «son índices» de más arriba salió de una consulta a `pg_constraint` que había fallado (42725), y yo lo leí como que no había restricciones. Corregido: el 16 borra la restricción (o el índice, si alguna base lo tuviera suelto) y el único nuevo también es una restricción. `Correctas! - Con error: 0, Superado: 8, Total: 8`.
- Decisiones del 25/11 en el ESTADO como D-S7-06 a D-S7-12. Además, la tabla del tablero (#127) y el «Pendiente del equipo» del CA3 quedan cerrados por la D-S7-06. La D2 se actualizó en `CLAUDE.md`, `docs/ONBOARDING-AGENTE.md` y `docs/sprint04/METODO-VINCULACION.md`.

### PARA SANTI · 25/11/2026 · script 16 en las tres bases

Va después del 13, aunque no depende de él (los dos requieren solo el 12). Desde la raíz del repo, en fish. Cada paso tiene que terminar con `NOTICE:  16: TODO OK.`; si uno falla, no sigas. Es una sola transacción.

1 · `lacentral_test` (`public`):

```fish
cd ~/dev/TESIS/la-central-gestion
git fetch origin
git show origin/feature/127-vinculacion-esquema:backend/db/rescaffold/16_Vinculacion_Codigo_Proveedor.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_test -v ON_ERROR_STOP=1
```

2 · `lacentral_demo`:

```fish
git show origin/feature/127-vinculacion-esquema:backend/db/rescaffold/16_Vinculacion_Codigo_Proveedor.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral_demo -v ON_ERROR_STOP=1
```

3 · `lacentral` (la real), con backup antes. Tiene que decir `backup OK` antes de aplicar:

```fish
mkdir -p ~/dev/TESIS/backups
ssh atiz@arch docker exec lacentral_db pg_dump -U lacentral -Fc lacentral > ~/dev/TESIS/backups/lacentral-antes-16.dump
and test -s ~/dev/TESIS/backups/lacentral-antes-16.dump
and echo "backup OK"
and git show origin/feature/127-vinculacion-esquema:backend/db/rescaffold/16_Vinculacion_Codigo_Proveedor.sql | ssh atiz@arch docker exec -i lacentral_db psql -U lacentral -d lacentral -v ON_ERROR_STOP=1
```

Al aplicarlo en `lacentral_test`, dos pruebas dan rojo hasta el PR del re-scaffold del 16: `RestriccionesUnicasPostgreSqlTests` (el mapa del manejador nombra `codigo_unico_global` y `un_codigo_por_proveedor`) y `Modificar_ElCodigoChocaEnElMotorDespuesDeActualizarLosDatos_NoQuedaNada` (choca por la unicidad global). Conviene aplicarlo cuando yo pueda abrir ese PR enseguida.
- 25/11 · Renumeración: el #203 (reparto, desde `develop`) usa la D-S7-12 para la decisión de Javi (factura sin CUIT). En esta rama, la «decisiones técnicas del asistente», que la línea de arriba contaba como D-S7-12, pasa a **D-S7-13**. Nueva **D-S7-14**: el 16 queda sin `normalizar_busqueda`, porque la vinculación automática es igualdad exacta por `(proveedor_id, codigo)`. La suite sobre `e71b546` no vale: 64 errores de conexión, base saturada, ver el bloque del #202. Esta rama corre la suite cuando termine la ronda 2 del verificador del #202, para no cargar la base dos veces a la vez.
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

## 26/11/2026 · Script 16 a PR (punto 3 de la cola autónoma del 26/11)

La instrucción verbatim está en el bloque «26/11/2026 · Cola autónoma…» de la bitácora de `fix/base-completa-check-baja`. Este es el rastro del punto 3, en `feature/127-vinculacion-esquema`.

**Estado:** en curso
- Merge de `origin/develop` (`bda2692`): conflictos solo en docs (bitácora add/add, y en ESTADO las decisiones y PRÓXIMA SESIÓN). Se conservan los dos lados; la D-S7-12 de Javi queda entre la D-S7-11 y la D-S7-13. El código que entró (re-scaffold del 14, `Proveedor`) no se solapa con el 16.
- Mientras corre la suite del auditor del #202 (sin nada contra la base), escrito y commiteado, con su rojo en esqueleto:
  - 053 en `feature/126-candidatos-vinculacion` (sobre esta rama): rojo `725aee7` (unitarias 10/10 en rojo), rojo de pantalla `7b3fc58` (4 en rojo; «con texto vacío no consulta» pasa contra el esqueleto), verde de pantalla `0c79ee8` (frontend 387/387), producción `848714a` (unitarias 10/10). **Falta correr** `CandidatosApiPostgreSqlTests` (9), en rojo sobre `725aee7` y en verde.
  - Comparador en `feature/030-comparador` (sobre esta rama): rojo `02b91f3` (unitarias 8/8 en rojo), producción `2613307` (8/8), rojo de pantalla `cc1a4b6` (8 en rojo), verde `430a603` (frontend 390/390), 031 documentada `6b3d522`. **Falta correr** `ComparadorApiPostgreSqlTests` (3).
  - 054: no se escribe todavía. Necesita el 16 aplicado y su re-scaffold (`UsuarioId` y `FechaVinculacion` en el modelo, y `codigo_alt_proveedor_codigo_unico` en el mapa del manejador). D-S7-17: se parte en 054a y 054b.
- ESTADO: D-S7-15 a D-S7-17 y la sección «PREGUNTAS PARA SANTI» (PS-01 a PS-05).

## 26/11/2026 17:25 · Retomar tras el reinicio: auditor del #202 y la cola

**Instrucción recibida (verbatim):**
> Se reinició la máquina en medio del trabajo: el auditor-persistencia del #202 estaba corriendo su suite completa sobre fe5ed3e y se cortó. Retomá según CLAUDE.md:
> 1) Leé el último bloque «en curso» de la bitácora y hacé el fetch y la comparación con develop.
> 2) Revisá los worktrees: git worktree list. Si quedó el del auditor u otro tuyo, sacalo con git worktree remove (sin --force; si dice que está sucio, avisame qué tiene) y después git worktree prune. Fijate también que no hayan quedado esquemas de ensayo a medias en lacentral_test (solo listalos, no borres nada que no sea tuyo).
> 3) Volvé a invocar el auditor-persistencia del #202 con el alcance acotado a sus commits. La suite de la ronda anterior no terminó, así que esta invocación la corre de nuevo (una sola vez).
> 4) Seguí con la cola que te dejé (base_completa, script 16, 053/054, comparador, compras) con las mismas reglas: no parar por lo técnico, PREGUNTAS PARA SANTI y PARA SANTI en el ESTADO y la bitácora, y seguir.

**Plan:**
1. Bitácora leída. Fetch: `origin/develop` no trae commits nuevos (`HEAD..origin/develop` vacío).
2. Worktrees y esquemas sobrantes de `lacentral_test`.
3. `auditor-persistencia` del #202 sobre `fe5ed3e`, acotado a `f944258`, `2b3e9d5`, `0d4ec50` y `a23c8fb`. Corre la suite él.
4. Cola: base_completa → 16 → 053/054 → comparador → compras (si el 13 está aplicado).

**Estado:** en curso
- Worktree del auditor (`…/3472cf08…/scratchpad/auditor-wt-5646dca3`, `fe5ed3e`): figuraba `prunable` porque el reinicio borró el scratchpad. `git worktree remove` y `git worktree prune -v` lo sacaron; queda solo el principal.
- `lacentral_test` a las 17:22 (reinicio de esta máquina 16:57:31). Esquemas fuera de `public`: `demo_ensayo_c3` (26 tablas), `staging` (3) y diez `pr110_test_*` (33 tablas c/u). De esos, `2264c6cc…` y `691bc549…` son de la corrida cortada del auditor: tienen sesión desde 100.88.21.29 (esta máquina), abierta antes del reinicio. Los otros ocho no tienen sesión que los identifique: `046097d2…`, `3856de7c…`, `3e568313…`, `7c31ee4a…`, `959576f5…`, `9f995e95…`, `e6221f80…`, `ef4770ee…`. De esos, `959576f5…`, `9f995e95…` y `ef4770ee…` tienen sesión desde 100.89.21.64 (otro equipo), con 17 sesiones `idle in transaction` desde las 15:08 y un `DROP SCHEMA "pr110_test_959576f5…"` trabado desde las 16:04 por la pid 163340, que es de esa misma máquina. No borré nada: el clasificador de permisos rechazó el `pg_terminate_backend` de mis sesiones fantasma y el `DROP` de mis dos esquemas. Las sesiones fantasma (12) se caen solas cuando venza el keepalive de TCP.
- `auditor-persistencia` del #202 relanzado sobre `fe5ed3e`, acotado a `f944258`, `2b3e9d5`, `0d4ec50` y `a23c8fb`; corre él la suite (una vez). Mientras tanto, solo código sin base.
- 054a (vincular) escrita mientras corre el auditor del #202, en `feature/127-vincular` (sobre `feature/126-candidatos-vinculacion`, con merge de la rama del 16 para los docs). No corre nada contra la base: el 16 no está aplicado en ninguna base (consultado a las 17:30: `codigo_unico_global` sigue y no hay `usuario_id`) y la base está ocupada por el auditor.
  - Caso de uso: rojo `c215055` (9/9 en rojo, `NotImplementedException`), producción `b9255b0` (`VincularFilaCatalogoUseCaseTests` 9/9).
  - HTTP contra PostgreSQL: prueba `7f9fa36` (`VinculacionApiPostgreSqlTests`, 5), producción `ea01c8a` (endpoint, repositorio en SQL, DI). **Falta correrla**: el rojo sobre `7f9fa36` (tiene que dar 404 por la ruta que falta) y el verde, con el 16 aplicado en `public` de `lacentral_test`.
  - Pantalla: rojo `c5dc79f` (3 en rojo, 5 de la 053 en verde), producción `62f21dd` (8/8; frontend completo 390/390).
  - Falta de la 054a: el CA5 (alta y vínculo en una operación). D-S7-18 en el ESTADO.
- Compras (016, 028, #195): se saltean, el 13 no está aplicado en ninguna base (no hay `costo_previo` en `factura_compra_detalle` de `lacentral_test`).
- 054a CA5 (alta y vínculo en una operación): rojo `998dc4b`→ caso de uso rojo en `…` (ver log: `test(catalogo): 054a CA5 · alta y vínculo…`, 3/3 en rojo), producción `def614a` (`LaCentral.Tests.Catalogo` 22/22), prueba HTTP `998dc4b` (2 más en `VinculacionApiPostgreSqlTests`, ahora 7) y endpoint `ad41244`; pantalla rojo y verde en los dos últimos commits de frontend (391/391). La composición reusa `CrearArticuloUseCase` dentro de una unidad de trabajo exterior: si la vinculación falla, el alta se descarta. **Falta correr** `VinculacionApiPostgreSqlTests` (7), en rojo sobre `7f9fa36`/`998dc4b` y en verde, con el 16 aplicado.
