-- =====================================================================================
--  10_Cuarto_Descuento.sql
--  La Central · 17/11/2026 · HU-FCO-06 (056, issue #163) CA-012 · D-S6-15
--
--  QUÉ HACE
--    factura_compra_detalle.descuento_4_porcentaje  NUMERIC(6,2) NOT NULL DEFAULT 0, entre
--    0 y 100, como los otros tres. Las líneas existentes quedan con 0: su costo no cambia.
--
--  POR QUÉ
--    Respuesta de Sergio del 17/11/2026 (D-S6-15 en docs/sprint06/ESTADO.md): la línea tiene
--    cuatro descuentos sucesivos. Los tres primeros son los que traen los proveedores; el
--    cuarto, los externos por marca o por artículo (por ejemplo, el 5 % de pago contado de
--    ETMAN, que no figura en la factura).
--
--  REQUISITO
--    El script 09 aplicado (descuento_3_porcentaje existe). Si no, el script se corta sin
--    tocar nada.
--
--  CÓMO SE APLICA
--    Una transacción. Es idempotente: el ALTER se salta si la columna ya existe, y la
--    verificación del final corre siempre. Primero en lacentral_test (con el dueño de las
--    tablas), después en lacentral_demo y en lacentral. Después: re-scaffold.
-- =====================================================================================

ROLLBACK;

BEGIN;

DO $alter$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'factura_compra_detalle' AND column_name = 'descuento_3_porcentaje') THEN
        RAISE EXCEPTION '10: falta el script 09 (factura_compra_detalle.descuento_3_porcentaje). Aplicá el 09 primero.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'factura_compra_detalle' AND column_name = 'descuento_4_porcentaje') THEN
        ALTER TABLE factura_compra_detalle ADD COLUMN descuento_4_porcentaje NUMERIC(6,2) NOT NULL DEFAULT 0;
        ALTER TABLE factura_compra_detalle ADD CONSTRAINT fcd_descuento_4_valido
            CHECK (descuento_4_porcentaje >= 0 AND descuento_4_porcentaje <= 100);
        COMMENT ON COLUMN factura_compra_detalle.descuento_4_porcentaje IS
            'Cuarto descuento de la línea, sucesivo: se aplica sobre el neto del tercero. Para los descuentos externos por marca o por artículo, como el 5 % de pago contado de ETMAN, que no figura en la factura (Sergio, 17/11/2026, D-S6-15).';
        RAISE NOTICE '10: factura_compra_detalle.descuento_4_porcentaje agregada.';
    END IF;
END $alter$;

DO $verif$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'factura_compra_detalle' AND column_name = 'descuento_4_porcentaje'
            AND is_nullable = 'NO' AND numeric_precision = 6 AND numeric_scale = 2) THEN
        RAISE EXCEPTION '10: descuento_4_porcentaje no quedó NUMERIC(6,2) NOT NULL.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = current_schema()::regnamespace
            AND conname = 'fcd_descuento_4_valido') THEN
        RAISE EXCEPTION '10: falta la restricción CHECK fcd_descuento_4_valido.';
    END IF;
    RAISE NOTICE '10: TODO OK.';
END $verif$;

COMMIT;
