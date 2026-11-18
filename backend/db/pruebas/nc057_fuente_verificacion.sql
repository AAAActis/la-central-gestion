-- =====================================================================================
--  nc057_fuente_verificacion.sql · La Central · 18/11/2026 · E11
--  Solo lectura. Después de la copia y del 11: nc057_fuente es public más el 11, y public
--  no cambió. Termina con «nc057_fuente: TODO OK.» o con la excepción de lo que falta.
-- =====================================================================================

DO $verif$
DECLARE
    v_faltan TEXT;
BEGIN
    IF current_database() <> 'lacentral_test' THEN
        RAISE EXCEPTION 'Esto se verifica en lacentral_test (estás en %).', current_database();
    END IF;
    -- public sigue sin el 11.
    IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'public' AND tablename LIKE 'nota_credito_compra%')
       OR EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public'
                  AND table_name = 'sucursal' AND column_name = 'es_reclamos') THEN
        RAISE EXCEPTION 'public tiene el 11: no era para public todavía.';
    END IF;
    -- Toda tabla de public está en la copia.
    SELECT string_agg(tablename, ', ') INTO v_faltan FROM pg_tables p WHERE schemaname = 'public'
        AND NOT EXISTS (SELECT 1 FROM pg_tables c WHERE c.schemaname = 'nc057_fuente' AND c.tablename = p.tablename);
    IF v_faltan IS NOT NULL THEN RAISE EXCEPTION 'Faltan tablas en nc057_fuente: %', v_faltan; END IF;
    -- Las tres del 11, y nada más que sobre.
    SELECT string_agg(tablename, ', ') INTO v_faltan FROM pg_tables c WHERE schemaname = 'nc057_fuente'
        AND NOT EXISTS (SELECT 1 FROM pg_tables p WHERE p.schemaname = 'public' AND p.tablename = c.tablename)
        AND tablename NOT IN ('nota_credito_compra', 'nota_credito_compra_factura', 'nota_credito_compra_detalle');
    IF v_faltan IS NOT NULL THEN RAISE EXCEPTION 'Sobran tablas en nc057_fuente: %', v_faltan; END IF;
    IF (SELECT count(*) FROM pg_tables WHERE schemaname = 'nc057_fuente' AND tablename LIKE 'nota_credito_compra%') <> 3 THEN
        RAISE EXCEPTION 'nc057_fuente no tiene las tres tablas del 11.';
    END IF;
    -- Índices y restricciones de public, con el mismo nombre en la copia.
    SELECT string_agg(indexname, ', ') INTO v_faltan FROM pg_indexes p WHERE schemaname = 'public'
        AND NOT EXISTS (SELECT 1 FROM pg_indexes c WHERE c.schemaname = 'nc057_fuente' AND c.indexname = p.indexname);
    IF v_faltan IS NOT NULL THEN RAISE EXCEPTION 'Índices de public que faltan en nc057_fuente: %', v_faltan; END IF;
    SELECT string_agg(conname, ', ') INTO v_faltan FROM pg_constraint p WHERE connamespace = 'public'::regnamespace
        AND contype IN ('p', 'u', 'f', 'c')
        AND NOT EXISTS (SELECT 1 FROM pg_constraint c WHERE c.connamespace = 'nc057_fuente'::regnamespace AND c.conname = p.conname);
    IF v_faltan IS NOT NULL THEN RAISE EXCEPTION 'Restricciones de public que faltan en nc057_fuente: %', v_faltan; END IF;
    -- Ninguna FK de la copia apunta fuera de ella.
    IF EXISTS (SELECT 1 FROM pg_constraint c JOIN pg_class r ON r.oid = c.confrelid
               WHERE c.contype = 'f' AND c.connamespace = 'nc057_fuente'::regnamespace
               AND r.relnamespace <> 'nc057_fuente'::regnamespace) THEN
        RAISE EXCEPTION 'Hay FK de nc057_fuente que apuntan fuera de la copia.';
    END IF;
    -- Catálogos: los de public, más Reclamos.
    IF (SELECT count(*) FROM nc057_fuente.rol) <> (SELECT count(*) FROM public.rol) THEN
        RAISE EXCEPTION 'nc057_fuente.rol no tiene las filas de public.rol.';
    END IF;
    IF (SELECT count(*) FROM nc057_fuente.sucursal) <> (SELECT count(*) FROM public.sucursal) + 1
       OR (SELECT count(*) FROM nc057_fuente.sucursal WHERE es_reclamos) <> 1 THEN
        RAISE EXCEPTION 'nc057_fuente.sucursal no son las de public más Reclamos.';
    END IF;
    IF nc057_fuente.similarity('bomba de agua', 'bomba agua') <> public.similarity('bomba de agua', 'bomba agua') THEN
        RAISE EXCEPTION 'nc057_fuente.similarity no da lo mismo que la de public.';
    END IF;
    RAISE NOTICE 'nc057_fuente: TODO OK (% tablas; public: %).',
        (SELECT count(*) FROM pg_tables WHERE schemaname = 'nc057_fuente'),
        (SELECT count(*) FROM pg_tables WHERE schemaname = 'public');
END $verif$;
