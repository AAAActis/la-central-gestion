-- =====================================================================================
--  nc057_fuente_borrado.sql · La Central · 18/11/2026 · E11
--  Borra la plantilla nc057_fuente de lacentral_test, cuando el 11 ya está en public (con
--  la pila y el PR de la carga manual de la 057 mergeados). Guardas: solo en lacentral_test,
--  y nada fuera de nc057_fuente depende de ella. public no se toca.
-- =====================================================================================

DO $borrado$
BEGIN
    IF current_database() <> 'lacentral_test' THEN
        RAISE EXCEPTION 'nc057_fuente se borra SOLO de lacentral_test (estás en %).', current_database();
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'nc057_fuente') THEN
        RAISE NOTICE 'nc057_fuente no existe: nada que borrar.';
        RETURN;
    END IF;
    IF EXISTS (SELECT 1 FROM pg_depend d JOIN pg_class o ON o.oid = d.objid
               JOIN pg_class r ON r.oid = d.refobjid
               WHERE r.relnamespace = 'nc057_fuente'::regnamespace
               AND o.relnamespace NOT IN ('nc057_fuente'::regnamespace, 'pg_toast'::regnamespace)) THEN
        RAISE EXCEPTION 'Hay objetos fuera de nc057_fuente que dependen de ella: no se borra.';
    END IF;
    DROP SCHEMA nc057_fuente CASCADE;
    RAISE NOTICE 'nc057_fuente borrado; public: % tablas.', (SELECT count(*) FROM pg_tables WHERE schemaname = 'public');
END $borrado$;
