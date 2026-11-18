-- =====================================================================================
--  nc057_fuente_copia.sql · La Central · 18/11/2026 · E11 (057, script 11)
--
--  Crea en lacentral_test el esquema plantilla nc057_fuente: copia de public con la
--  estructura que valida PostgreSqlPruebas (tablas, CHECK, defaults, identity, índices y
--  restricciones con los MISMOS nombres, FK redirigidas a la copia) y solo los datos que la
--  suite lee de la plantilla: rol y sucursal. No copia datos de la empresa.
--  similarity() se resuelve con un envoltorio que llama a la de public (pg_trgm vive ahí).
--  Sobre esta copia se aplica el 11; public no se toca. Mismo método que fco06_fuente (13/11).
--
--  Se corre con el superusuario lacentral, conectado a lacentral_test. Si nc057_fuente ya
--  existe, se corta sin tocar nada (para rehacerla: nc057_fuente_borrado.sql).
-- =====================================================================================

BEGIN;

DO $copia$
DECLARE
    v_tabla  TEXT;
    v_rol    TEXT;
    r        RECORD;
    v_def    TEXT;
BEGIN
    IF current_database() <> 'lacentral_test' THEN
        RAISE EXCEPTION 'nc057_fuente: esto va SOLO en lacentral_test (estás en %).', current_database();
    END IF;
    IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'nc057_fuente') THEN
        RAISE EXCEPTION 'nc057_fuente ya existe: no se toca. Para rehacerla, primero nc057_fuente_borrado.sql.';
    END IF;
    -- Con pg_catalog solo, las definiciones salen calificadas (public.x): así se redirigen sin ambigüedad.
    SET LOCAL search_path = pg_catalog;

    CREATE SCHEMA nc057_fuente;

    FOR v_tabla IN SELECT c.relname FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                   WHERE n.nspname = 'public' AND c.relkind IN ('r', 'p') AND NOT c.relispartition ORDER BY 1 LOOP
        EXECUTE format('CREATE TABLE nc057_fuente.%I (LIKE public.%I INCLUDING ALL)', v_tabla, v_tabla);
    END LOOP;

    -- LIKE pone nombres nuevos a los índices (y a las PK/UNIQUE que respaldan); el manejador
    -- global reconoce las violaciones por nombre. Se aparean por tabla, unicidad y definición.
    FOR r IN
        WITH ix AS (
            SELECT n.nspname AS esquema, t.relname AS tabla, x.relname AS nombre,
                   i.indisunique::text || ' ' || regexp_replace(pg_get_indexdef(i.indexrelid), '^.* USING ', '') AS firma
            FROM pg_index i JOIN pg_class x ON x.oid = i.indexrelid
            JOIN pg_class t ON t.oid = i.indrelid JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname IN ('public', 'nc057_fuente'))
        SELECT o.tabla, o.nombre AS original, c.nombre AS copia,
               count(*) OVER (PARTITION BY o.tabla, o.firma) AS iguales
        FROM ix o LEFT JOIN ix c ON c.esquema = 'nc057_fuente' AND c.tabla = o.tabla AND c.firma = o.firma
        WHERE o.esquema = 'public'
    LOOP
        IF r.copia IS NULL OR r.iguales <> 1 THEN
            RAISE EXCEPTION 'nc057_fuente: no se pudo aparear sin ambigüedad el índice %.%.', r.tabla, r.original;
        END IF;
        IF r.copia <> r.original THEN
            EXECUTE format('ALTER INDEX nc057_fuente.%I RENAME TO %I', r.copia, r.original);
        END IF;
    END LOOP;

    -- LIKE no copia las FK: se agregan apuntando a la copia.
    FOR r IN SELECT t.relname AS tabla, c.conname, pg_get_constraintdef(c.oid) AS def
             FROM pg_constraint c JOIN pg_class t ON t.oid = c.conrelid JOIN pg_namespace n ON n.oid = t.relnamespace
             WHERE c.contype = 'f' AND n.nspname = 'public' ORDER BY 1, 2 LOOP
        IF r.def !~ 'REFERENCES public\.' THEN
            RAISE EXCEPTION 'nc057_fuente: la FK % no apunta a public: %', r.conname, r.def;
        END IF;
        v_def := regexp_replace(r.def, 'REFERENCES public\.', 'REFERENCES nc057_fuente.');
        EXECUTE format('ALTER TABLE nc057_fuente.%I ADD CONSTRAINT %I %s', r.tabla, r.conname, v_def);
    END LOOP;

    -- Lo único que la suite lee de la plantilla: los catálogos sin identity.
    INSERT INTO nc057_fuente.rol SELECT * FROM public.rol;
    INSERT INTO nc057_fuente.sucursal SELECT * FROM public.sucursal;

    -- EF traduce TrigramsSimilarity a similarity() sin esquema; la suite busca en «aislado, plantilla».
    CREATE FUNCTION nc057_fuente.similarity(text, text) RETURNS real
        LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE AS 'SELECT public.similarity($1, $2)';

    -- Los roles de la suite leen la plantilla como leen public (05_rol_tests.sql).
    FOR v_rol IN SELECT rolname FROM pg_roles WHERE rolname IN ('lacentral_tests', 'lacentral_santi') LOOP
        EXECUTE format('GRANT USAGE ON SCHEMA nc057_fuente TO %I', v_rol);
        EXECUTE format('GRANT SELECT ON ALL TABLES IN SCHEMA nc057_fuente TO %I', v_rol);
        EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA nc057_fuente GRANT SELECT ON TABLES TO %I', v_rol);
    END LOOP;

    RAISE NOTICE 'nc057_fuente: copia creada (% tablas).',
        (SELECT count(*) FROM pg_tables WHERE schemaname = 'nc057_fuente');
END $copia$;

COMMIT;
