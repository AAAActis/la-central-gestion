-- =====================================================================================
--  16_Vinculacion_Codigo_Proveedor.sql
--  La Central · 25/11/2026 · D2 revisada (D-S7-06) y vinculación de la 054 (D-S7-07; #127)
--
--  QUÉ HACE
--    1. articulo_codigo_alternativo: la unicidad pasa a ser UNIQUE (proveedor_id, codigo)
--       (restricción codigo_alt_proveedor_codigo_unico). Se borran:
--         codigo_unico_global      restricción UNIQUE (codigo): dos proveedores pueden usar el mismo código;
--         un_codigo_por_proveedor  restricción UNIQUE (articulo_id, proveedor_id): un artículo puede tener
--                                  varios códigos del mismo proveedor (CA3 de la 054);
--         ix_codigo_alt_proveedor  índice (proveedor_id): lo cubre el único nuevo, que empieza igual.
--       Cada código de un proveedor sigue apuntando a un solo artículo: la factura de compra
--       identifica el artículo sin ambigüedad.
--    2. articulo_codigo_alternativo.usuario_id (FK a usuario) y fecha_vinculacion
--       (TIMESTAMPTZ): quién vinculó y cuándo (054 CA1). Los dos o ninguno
--       (aca_vinculacion_coherente). Las filas de antes del 16 y los códigos cargados desde
--       la ficha del artículo quedan con los dos en NULL: no se inventa quién ni cuándo.
--    La vinculación ES esta fila: proveedor_articulo se cruza con ella por
--    (proveedor_id, codigo). n8n no toca esta tabla (no tiene permisos sobre ella, script 12).
--
--  REQUISITO
--    El script 12 aplicado (proveedor_articulo existe). Si no, se corta sin tocar nada.
--
--  CÓMO SE APLICA
--    Una transacción. Es idempotente: cada paso se salta si ya está, y la verificación del
--    final corre siempre. Lo aplica Santi, primero en lacentral_test (public), después en
--    lacentral_demo y en lacentral (con backup). Después: re-scaffold.
-- =====================================================================================

ROLLBACK;

BEGIN;

DO $aplicar$
DECLARE
    v_esquema regnamespace := current_schema()::regnamespace;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema()
                   AND table_name = 'proveedor_articulo') THEN
        RAISE EXCEPTION '16: falta el script 12 (proveedor_articulo). Aplicá el 12 primero.';
    END IF;

    -- 1 · Unicidad por proveedor y código (D-S7-06). Primero el índice nuevo: si hubiera
    -- repetidos, revienta acá, antes de sacar los viejos.
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = v_esquema AND conname = 'codigo_alt_proveedor_codigo_unico') THEN
        ALTER TABLE articulo_codigo_alternativo
            ADD CONSTRAINT codigo_alt_proveedor_codigo_unico UNIQUE (proveedor_id, codigo);
        RAISE NOTICE '16: codigo_alt_proveedor_codigo_unico creado.';
    END IF;
    -- Es una restricción UNIQUE (con su índice); por si alguna base la tuviera como índice suelto, también.
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = v_esquema AND conname = 'codigo_unico_global') THEN
        ALTER TABLE articulo_codigo_alternativo DROP CONSTRAINT codigo_unico_global;
        RAISE NOTICE '16: codigo_unico_global borrado.';
    ELSIF EXISTS (SELECT 1 FROM pg_class WHERE relnamespace = v_esquema AND relname = 'codigo_unico_global') THEN
        DROP INDEX codigo_unico_global;
        RAISE NOTICE '16: codigo_unico_global borrado.';
    END IF;
    -- Es una restricción UNIQUE (con su índice); por si alguna base la tuviera como índice suelto, también.
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = v_esquema AND conname = 'un_codigo_por_proveedor') THEN
        ALTER TABLE articulo_codigo_alternativo DROP CONSTRAINT un_codigo_por_proveedor;
        RAISE NOTICE '16: un_codigo_por_proveedor borrado.';
    ELSIF EXISTS (SELECT 1 FROM pg_class WHERE relnamespace = v_esquema AND relname = 'un_codigo_por_proveedor') THEN
        DROP INDEX un_codigo_por_proveedor;
        RAISE NOTICE '16: un_codigo_por_proveedor borrado.';
    END IF;
    IF EXISTS (SELECT 1 FROM pg_class WHERE relnamespace = v_esquema AND relname = 'ix_codigo_alt_proveedor') THEN
        DROP INDEX ix_codigo_alt_proveedor;
        RAISE NOTICE '16: ix_codigo_alt_proveedor borrado (lo cubre el único nuevo).';
    END IF;

    -- 2 · Quién vinculó y cuándo (D-S7-07)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
                   AND table_name = 'articulo_codigo_alternativo' AND column_name = 'usuario_id') THEN
        ALTER TABLE articulo_codigo_alternativo
            ADD COLUMN usuario_id INTEGER REFERENCES usuario(id),
            ADD COLUMN fecha_vinculacion TIMESTAMPTZ,
            ADD CONSTRAINT aca_vinculacion_coherente CHECK ((usuario_id IS NULL) = (fecha_vinculacion IS NULL));
        COMMENT ON COLUMN articulo_codigo_alternativo.usuario_id IS
            'Quién vinculó este código del proveedor al artículo (054 CA1). NULL en los códigos de antes del 16 y en los cargados desde la ficha del artículo (D-S7-07, 25/11/2026).';
        COMMENT ON COLUMN articulo_codigo_alternativo.fecha_vinculacion IS
            'Cuándo se vinculó. NULL si y solo si usuario_id es NULL (aca_vinculacion_coherente).';
        COMMENT ON TABLE articulo_codigo_alternativo IS
            'Códigos de los proveedores para cada artículo propio. Es la vinculación con el catálogo: proveedor_articulo se cruza por (proveedor_id, codigo). Un artículo puede tener varios códigos del mismo proveedor; cada código de un proveedor apunta a un solo artículo (D2 revisada, D-S7-06, 25/11/2026).';
        RAISE NOTICE '16: usuario_id y fecha_vinculacion agregados.';
    END IF;
END $aplicar$;

DO $verif$
DECLARE
    v_esquema regnamespace := current_schema()::regnamespace;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = v_esquema AND conname = 'codigo_alt_proveedor_codigo_unico'
                   AND contype = 'u' AND pg_get_constraintdef(oid) = 'UNIQUE (proveedor_id, codigo)') THEN
        RAISE EXCEPTION '16: falta el único (proveedor_id, codigo).';
    END IF;
    IF EXISTS (SELECT 1 FROM pg_class WHERE relnamespace = v_esquema
               AND relname IN ('codigo_unico_global', 'un_codigo_por_proveedor', 'ix_codigo_alt_proveedor')) THEN
        RAISE EXCEPTION '16: quedó un índice viejo de articulo_codigo_alternativo.';
    END IF;
    IF (SELECT count(*) FROM information_schema.columns WHERE table_schema = current_schema()
        AND table_name = 'articulo_codigo_alternativo' AND column_name IN ('usuario_id', 'fecha_vinculacion')) <> 2
       OR NOT EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = v_esquema AND conname = 'aca_vinculacion_coherente') THEN
        RAISE EXCEPTION '16: faltan usuario_id, fecha_vinculacion o aca_vinculacion_coherente.';
    END IF;
    RAISE NOTICE '16: TODO OK.';
END $verif$;

COMMIT;
