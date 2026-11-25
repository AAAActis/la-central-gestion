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
