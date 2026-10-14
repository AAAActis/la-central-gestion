-- =====================================================================================
--  03_Limpieza_Columnas_Fantasma.sql
--  La Central · Sprint 3 · Deriva de esquema detectada al re-scaffoldear
--
--  QUÉ HACE
--    Elimina tres columnas de public.articulo que existen en el motor pero no en
--    ningún script versionado: codigo, descripcion y cantidad.
--
--  POR QUÉ
--    Aparecieron al comparar la salida del scaffold contra LaCentral.Data/Models/.
--    No están en base_completa.sql, ni en 01_Ajustes_Sprint3.sql, ni en el 02.
--    Sus nombres coinciden con los de la tabla de importación del esquema staging
--    (base_completa.sql:397): son restos de la importación de Multisoft.
--
--    En Database First el script es la fuente de verdad. Una columna que solo
--    existe en el motor no es esquema, es deriva: una base creada limpia desde los
--    scripts no la tiene, y EF la pediría igual. Aceptarlas en el modelo sería
--    legitimarlas porque están ahí.
--
--    Y hay un riesgo concreto: articulo.codigo nullable, al lado del código interno
--    real que HU-ART-01 genera automáticamente, es una trampa esperando que alguien
--    escriba en la columna equivocada. Lo mismo descripcion junto al nombre real.
--
--  ALCANCE VERIFICADO
--    En lacentral_test: 0 de 2644 filas tienen dato en las tres. Sin comentarios y
--    sin restricciones. Verificado contra el catálogo el 14/10/2026.
--    Este script NO borra si encuentra una sola fila con dato: aborta.
--
--  CÓMO SE EJECUTA
--    DBeaver: "Execute script" (Alt+X), NO "Execute statement".
--    Primero contra lacentral_test. Verificá. Después contra lacentral.
--    Si en alguna de las dos bases hay filas con dato, PARÁ: la decisión cambia.
-- =====================================================================================


-- Limpia una transacción abortada de un intento anterior en la misma sesión.
ROLLBACK;


BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';


-- =====================================================================================
--  PARTE 1 · VERIFICACIÓN PREVIA
-- =====================================================================================

DO $verificacion$
DECLARE
    v_cols       int;
    v_total      bigint;
    v_con_dato   bigint;
    v_detalle    text;
BEGIN
    RAISE NOTICE 'Base: %. Esquema: %.', current_database(), current_schema();

    -- 1.1 Las tres columnas tienen que existir con el tipo observado
    SELECT count(*) INTO v_cols
    FROM information_schema.columns
    WHERE table_schema = current_schema()
      AND table_name = 'articulo'
      AND (
            (column_name = 'codigo'      AND data_type = 'character varying'
             AND character_maximum_length = 50 AND is_nullable = 'YES')
         OR (column_name = 'descripcion' AND data_type = 'character varying'
             AND character_maximum_length = 50 AND is_nullable = 'YES')
         OR (column_name = 'cantidad'    AND data_type = 'integer'
             AND is_nullable = 'YES')
      );

    IF v_cols <> 3 THEN
        RAISE EXCEPTION
            'Se esperaban las tres columnas fantasma (codigo varchar(50) NULL, descripcion varchar(50) NULL, cantidad integer NULL) y se encontraron %. Si ya se aplicó este script en esta base, no hay nada que hacer. Si los tipos difieren de los observados, PARÁ y revisá antes de borrar.',
            v_cols;
    END IF;

    -- 1.2 COMPUERTA DE DATOS. Ni una fila con dato.
    EXECUTE format(
        'SELECT count(*), count(*) FILTER (WHERE codigo IS NOT NULL OR descripcion IS NOT NULL OR cantidad IS NOT NULL) FROM %I.articulo',
        current_schema())
    INTO v_total, v_con_dato;

    RAISE NOTICE 'articulo: % fila(s) en total, % con dato en las columnas fantasma.',
        v_total, v_con_dato;

    IF v_con_dato > 0 THEN
        RAISE EXCEPTION
            '% fila(s) de articulo tienen dato en codigo, descripcion o cantidad. Este script NO borra datos. Hay que decidir qué significan esas filas antes de tocar nada: si son de la importación de Multisoft, puede que haya que migrarlas a la columna correcta.',
            v_con_dato;
    END IF;

    -- 1.3 No pueden tener restricciones ni formar parte de un índice. Si las
    --     tienen, el DROP sin CASCADE va a fallar igual, pero mejor avisar acá.
    FOR v_detalle IN
        SELECT format('  %s (%s)', con.conname, con.contype)
        FROM pg_constraint con
        JOIN pg_attribute  a ON a.attrelid = con.conrelid
                            AND a.attnum = ANY (con.conkey)
        WHERE con.conrelid = format('%I.articulo', current_schema())::regclass
          AND a.attname IN ('codigo', 'descripcion', 'cantidad')
    LOOP
        RAISE NOTICE 'ATENCIÓN · restricción sobre una columna fantasma: %', v_detalle;
    END LOOP;

    FOR v_detalle IN
        SELECT format('  %s', i.relname)
        FROM pg_index     ix
        JOIN pg_class     i ON i.oid = ix.indexrelid
        JOIN pg_attribute a ON a.attrelid = ix.indrelid
                           AND a.attnum = ANY (ix.indkey)
        WHERE ix.indrelid = format('%I.articulo', current_schema())::regclass
          AND a.attname IN ('codigo', 'descripcion', 'cantidad')
    LOOP
        RAISE NOTICE 'ATENCIÓN · índice sobre una columna fantasma: %', v_detalle;
    END LOOP;

    RAISE NOTICE 'Verificación previa superada.';
END $verificacion$;


-- =====================================================================================
--  PARTE 2 · LA LIMPIEZA
--  Sin CASCADE a propósito: si una vista o una restricción dependiera de alguna, el
--  DROP tiene que fallar y avisar, no arrastrarse lo que encuentre.
-- =====================================================================================

ALTER TABLE articulo DROP COLUMN codigo;
ALTER TABLE articulo DROP COLUMN descripcion;
ALTER TABLE articulo DROP COLUMN cantidad;


-- =====================================================================================
--  PARTE 3 · VERIFICACIÓN POSTERIOR
-- =====================================================================================

DO $cierre$
DECLARE
    v_cols    int;
    v_detalle text;
BEGIN
    SELECT count(*) INTO v_cols
    FROM information_schema.columns
    WHERE table_schema = current_schema()
      AND table_name = 'articulo'
      AND column_name IN ('codigo', 'descripcion', 'cantidad');

    IF v_cols > 0 THEN
        RAISE EXCEPTION 'Quedaron % columna(s) fantasma en articulo.', v_cols;
    END IF;

    RAISE NOTICE 'Las tres columnas fantasma fueron eliminadas.';
    RAISE NOTICE 'Columnas de articulo que quedan:';

    FOR v_detalle IN
        SELECT format('  %s %s %s',
                      a.attname,
                      format_type(a.atttypid, a.atttypmod),
                      CASE WHEN a.attnotnull THEN 'NOT NULL' ELSE 'NULL' END)
        FROM pg_attribute a
        WHERE a.attrelid = format('%I.articulo', current_schema())::regclass
          AND a.attnum > 0
          AND NOT a.attisdropped
        ORDER BY a.attnum
    LOOP
        RAISE NOTICE '%', v_detalle;
    END LOOP;
END $cierre$;

COMMIT;


-- =====================================================================================
--  DESPUÉS DE APLICAR
--
--  1. Corré el script en las DOS bases y confirmá que la lista de columnas de
--     articulo queda idéntica. El scaffold se hace contra lacentral_test: si las dos
--     bases difieren, el modelo generado no representa a producción.
--
--  2. Recién entonces, el re-scaffold real.
--
--  3. Esta deriva tiene una causa: el esquema se modificó alguna vez sin pasar por un
--     script versionado. Vale anotarlo en ESTADO.md como deuda de proceso, no solo
--     como un arreglo puntual. La próxima vez que el scaffold difiera de los scripts,
--     este es el primer lugar donde mirar.
-- =====================================================================================
