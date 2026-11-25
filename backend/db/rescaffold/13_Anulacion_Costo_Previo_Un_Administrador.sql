-- =====================================================================================
-- 13_Anulacion_Costo_Previo_Un_Administrador.sql
-- La Central · 23/11/2026 · D-S7-03 (028 CA-002 v1.2, 016 CA-004 v1.1; #9, #23) y
--                            D-S7-04 (028 CA-006 y CA-007 v1.2, 057 CA-012 v1.3; #195)
--
-- 25/11/2026: ensayado en un esquema descartable de lacentral_test, con pruebas PostgreSQL
-- de sus CHECK (Script13PostgreSqlTests). Sin re-scaffold todavía.
--
-- QUÉ HACE
-- 1. D-S7-03 · factura_compra_detalle.costo_previo (NUMERIC, NULL = el artículo no tenía
--    costo) y costo_previo_guardado (BOOLEAN NOT NULL DEFAULT false). Al registrar una
--    línea, el caso de uso guarda el costo que el artículo tenía justo antes y pone
--    costo_previo_guardado = true. Al anular, si el costo actual sigue siendo el que puso
--    esa factura (su fila de articulo_historial_compra), vuelve a costo_previo; si no (se
--    cambió a mano, o lo cambió otra compra), queda el actual.
--    Las líneas registradas antes del 13 quedan con costo_previo_guardado = false: para
--    ellas la anulación sigue como hasta la v1.1 (último registro válido del historial).
--    El default es false, no true, para que una rama sin este cambio que inserte líneas
--    no las marque como guardadas sin guardar nada.
-- 2. D-S7-04 · nota_credito_compra: ncc_anulacion_coherente deja de exigir
--    usuario_autorizacion_id. La columna queda (conserva quién autorizó las anulaciones
--    hechas con doble certificación) y ncc_autorizacion_distinta también.
-- 3. D-S7-04 · factura_compra y nota_credito_compra: una anulada exige quién la anuló
--    (fc_anulacion_con_usuario; en la nota ya lo exigía ncc_anulacion_coherente).
--
-- REQUISITO
-- El script 12 aplicado (scrape_corrida existe). Si no, se corta sin tocar nada.
-- Ninguna factura de compra ANULADA sin usuario_anulacion_id. Si hay, se corta sin tocar nada.
--
-- CÓMO SE APLICA
-- Una transacción. Es idempotente: cada paso se salta si ya está, y la verificación del
-- final corre siempre. Primero en lacentral_test, después en lacentral_demo y en
-- lacentral. Después: re-scaffold.
-- =====================================================================================
ROLLBACK;
BEGIN;

DO $aplicar$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema()
                   AND table_name = 'scrape_corrida') THEN
        RAISE EXCEPTION '13: falta el script 12 (scrape_corrida). Aplicá el 12 primero.';
    END IF;

    -- 1 · Costo previo por línea (D-S7-03)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
                   AND table_name = 'factura_compra_detalle' AND column_name = 'costo_previo') THEN
        ALTER TABLE factura_compra_detalle
            ADD COLUMN costo_previo NUMERIC(14,2),
            ADD COLUMN costo_previo_guardado BOOLEAN NOT NULL DEFAULT false,
            ADD CONSTRAINT fcd_costo_previo_no_negativo CHECK (costo_previo IS NULL OR costo_previo >= 0),
            ADD CONSTRAINT fcd_costo_previo_coherente CHECK (costo_previo_guardado OR costo_previo IS NULL);
        COMMENT ON COLUMN factura_compra_detalle.costo_previo IS
            'Costo del artículo justo antes de registrar esta línea. NULL = no tenía costo (desconocido, nunca cero). Al anular la factura, el costo vuelve a este valor si nadie lo cambió después (D-S7-03, 23/11/2026).';
        COMMENT ON COLUMN factura_compra_detalle.costo_previo_guardado IS
            'TRUE si la línea se registró guardando costo_previo (desde el script 13). FALSE en las anteriores: su anulación usa el último registro válido de articulo_historial_compra.';
        RAISE NOTICE '13: factura_compra_detalle.costo_previo agregado.';
    END IF;

    -- 2 · La nota de crédito se anula sin autorización de otra persona (D-S7-04)
    IF pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE connamespace = current_schema()::regnamespace
                             AND conname = 'ncc_anulacion_coherente')) LIKE '%usuario_autorizacion_id IS NOT NULL%' THEN
        ALTER TABLE nota_credito_compra DROP CONSTRAINT ncc_anulacion_coherente;
        ALTER TABLE nota_credito_compra ADD CONSTRAINT ncc_anulacion_coherente CHECK (
            (estado = 'ACTIVA' AND motivo_anulacion IS NULL AND fecha_anulacion IS NULL
                AND usuario_anulacion_id IS NULL AND usuario_autorizacion_id IS NULL) OR
            (estado = 'ANULADA' AND motivo_anulacion IS NOT NULL AND fecha_anulacion IS NOT NULL
                AND usuario_anulacion_id IS NOT NULL));
        COMMENT ON COLUMN nota_credito_compra.usuario_autorizacion_id IS
            'Quién autorizó la anulación cuando se pedía doble certificación (hasta el 23/11/2026). Desde la D-S7-04 anula un ADMINISTRADOR solo y queda NULL.';
        RAISE NOTICE '13: ncc_anulacion_coherente sin autorización.';
    END IF;

    -- 3 · Una factura anulada dice quién la anuló (D-S7-04)
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = current_schema()::regnamespace
                   AND conname = 'fc_anulacion_con_usuario') THEN
        IF EXISTS (SELECT 1 FROM factura_compra WHERE estado = 'ANULADA' AND usuario_anulacion_id IS NULL) THEN
            RAISE EXCEPTION '13: hay % facturas de compra anuladas sin usuario_anulacion_id: no se aplicó nada.',
                (SELECT count(*) FROM factura_compra WHERE estado = 'ANULADA' AND usuario_anulacion_id IS NULL);
        END IF;
        ALTER TABLE factura_compra ADD CONSTRAINT fc_anulacion_con_usuario
            CHECK (estado <> 'ANULADA' OR usuario_anulacion_id IS NOT NULL);
        COMMENT ON COLUMN factura_compra.usuario_autorizacion_id IS
            'Quién autorizó la anulación cuando se pedía doble certificación (hasta el 23/11/2026). Desde la D-S7-04 anula un ADMINISTRADOR solo y queda NULL. Alimenta R-08.';
        RAISE NOTICE '13: fc_anulacion_con_usuario puesta.';
    END IF;
END $aplicar$;

DO $verif$
BEGIN
    IF (SELECT count(*) FROM information_schema.columns WHERE table_schema = current_schema()
        AND table_name = 'factura_compra_detalle' AND column_name IN ('costo_previo', 'costo_previo_guardado')) <> 2 THEN
        RAISE EXCEPTION '13: faltan columnas en factura_compra_detalle.';
    END IF;
    IF (SELECT count(*) FROM pg_constraint WHERE connamespace = current_schema()::regnamespace AND conname IN (
            'fcd_costo_previo_no_negativo', 'fcd_costo_previo_coherente', 'fc_anulacion_con_usuario',
            'ncc_anulacion_coherente', 'ncc_autorizacion_distinta')) <> 5 THEN
        RAISE EXCEPTION '13: faltan restricciones (se esperaban 5).';
    END IF;
    IF pg_get_constraintdef((SELECT oid FROM pg_constraint WHERE connamespace = current_schema()::regnamespace
                             AND conname = 'ncc_anulacion_coherente')) LIKE '%usuario_autorizacion_id IS NOT NULL%' THEN
        RAISE EXCEPTION '13: ncc_anulacion_coherente todavía exige quien autoriza.';
    END IF;
    RAISE NOTICE '13: TODO OK.';
END $verif$;

COMMIT;
