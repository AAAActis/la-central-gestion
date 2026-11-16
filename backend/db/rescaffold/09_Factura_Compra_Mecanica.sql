-- =====================================================================================
--  09_Factura_Compra_Mecanica.sql
--  La Central · 13/11/2026 · HU-FCO-06 (056, issue #163)
--
--  QUÉ HACE
--    1. proveedor.tipo_comprobante_habitual  CHAR(1) NULL, 'A' o 'B'. Solo el valor por
--       defecto que la cabecera propone (CA-004). NULL = sin dato: no se propone nada.
--    2. factura_compra.tipo_comprobante      CHAR(1) NULL, 'A' o 'B'. El de ESA factura:
--       de él salen el 99 del número (CA-005) y el 21 % de IVA (CA-014). NULL solo en las
--       facturas cargadas antes de este script, que no lo registraban; el caso de uso lo
--       exige en las nuevas.
--    3. factura_compra.fecha_imputacion      DATE NOT NULL (CA-006). Las filas existentes
--       toman su fecha de emisión, el único dato que tienen.
--    4. factura_compra_detalle.descuento_2_porcentaje y descuento_3_porcentaje, NUMERIC(6,2)
--       NOT NULL DEFAULT 0, entre 0 y 100, como descuento_porcentaje (CA-012).
--
--  POR QUÉ
--    Respuestas de Sergio del 13/11/2026 (D-S6-10 y D-S6-11 en docs/sprint06/ESTADO.md):
--    el tipo de comprobante depende de la factura, no del proveedor, y un proveedor puede
--    emitir A y B indistintamente.
--
--  CÓMO SE APLICA
--    Una transacción. Es idempotente: cada ALTER se salta si la columna ya existe, y la
--    verificación del final corre siempre. Primero en lacentral_test (con el dueño de las
--    tablas), después en lacentral_demo y en lacentral.
-- =====================================================================================

ROLLBACK;

BEGIN;

DO $alter$
DECLARE
    falta boolean;
BEGIN
    SELECT NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
        AND table_name = 'proveedor' AND column_name = 'tipo_comprobante_habitual') INTO falta;
    IF falta THEN
        ALTER TABLE proveedor ADD COLUMN tipo_comprobante_habitual CHAR(1);
        ALTER TABLE proveedor ADD CONSTRAINT proveedor_tipo_comprobante_habitual_valido
            CHECK (tipo_comprobante_habitual IS NULL OR tipo_comprobante_habitual IN ('A', 'B'));
        COMMENT ON COLUMN proveedor.tipo_comprobante_habitual IS
            'Tipo de comprobante que el proveedor suele emitir (A o B). Solo el valor por defecto de la cabecera de una factura de compra: un proveedor puede emitir A y B indistintamente (Sergio, 13/11/2026). NULL = sin dato.';
        RAISE NOTICE '09: proveedor.tipo_comprobante_habitual agregada.';
    END IF;

    SELECT NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
        AND table_name = 'factura_compra' AND column_name = 'tipo_comprobante') INTO falta;
    IF falta THEN
        ALTER TABLE factura_compra ADD COLUMN tipo_comprobante CHAR(1);
        ALTER TABLE factura_compra ADD CONSTRAINT fc_tipo_comprobante_valido
            CHECK (tipo_comprobante IS NULL OR tipo_comprobante IN ('A', 'B'));
        COMMENT ON COLUMN factura_compra.tipo_comprobante IS
            'Tipo de ESTA factura (A o B). El A suma 21 % de IVA y se copia su número tal cual; el B no suma IVA y lleva 99 en el primer tramo del número (Sergio, 13/11/2026). NULL solo en facturas anteriores al script 09.';
        RAISE NOTICE '09: factura_compra.tipo_comprobante agregada.';
    END IF;

    SELECT NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
        AND table_name = 'factura_compra' AND column_name = 'fecha_imputacion') INTO falta;
    IF falta THEN
        ALTER TABLE factura_compra ADD COLUMN fecha_imputacion DATE;
        UPDATE factura_compra SET fecha_imputacion = fecha_emision;
        ALTER TABLE factura_compra ALTER COLUMN fecha_imputacion SET NOT NULL;
        COMMENT ON COLUMN factura_compra.fecha_imputacion IS
            'Fecha a la que se imputa la compra, aparte de la emisión y el vencimiento (HU-FCO-06). Dato del comprobante, sin lógica financiera.';
        RAISE NOTICE '09: factura_compra.fecha_imputacion agregada (las existentes, con su emisión).';
    END IF;

    SELECT NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
        AND table_name = 'factura_compra_detalle' AND column_name = 'descuento_2_porcentaje') INTO falta;
    IF falta THEN
        ALTER TABLE factura_compra_detalle
            ADD COLUMN descuento_2_porcentaje NUMERIC(6,2) NOT NULL DEFAULT 0,
            ADD COLUMN descuento_3_porcentaje NUMERIC(6,2) NOT NULL DEFAULT 0;
        ALTER TABLE factura_compra_detalle ADD CONSTRAINT fcd_descuento_2_valido
            CHECK (descuento_2_porcentaje >= 0 AND descuento_2_porcentaje <= 100);
        ALTER TABLE factura_compra_detalle ADD CONSTRAINT fcd_descuento_3_valido
            CHECK (descuento_3_porcentaje >= 0 AND descuento_3_porcentaje <= 100);
        COMMENT ON COLUMN factura_compra_detalle.descuento_2_porcentaje IS
            'Segundo descuento de la línea, sucesivo: se aplica sobre el neto del primero (HU-FCO-06; supuesto a confirmar con Sergio).';
        COMMENT ON COLUMN factura_compra_detalle.descuento_3_porcentaje IS
            'Tercer descuento de la línea, sucesivo: se aplica sobre el neto del segundo.';
        RAISE NOTICE '09: factura_compra_detalle.descuento_2_porcentaje y descuento_3_porcentaje agregadas.';
    END IF;
END $alter$;

DO $verif$
BEGIN
    IF (SELECT count(*) FROM information_schema.columns WHERE table_schema = current_schema() AND (
            (table_name = 'proveedor' AND column_name = 'tipo_comprobante_habitual' AND is_nullable = 'YES') OR
            (table_name = 'factura_compra' AND column_name = 'tipo_comprobante' AND is_nullable = 'YES') OR
            (table_name = 'factura_compra' AND column_name = 'fecha_imputacion' AND is_nullable = 'NO') OR
            (table_name = 'factura_compra_detalle' AND column_name IN ('descuento_2_porcentaje', 'descuento_3_porcentaje')
                AND is_nullable = 'NO'))) <> 5 THEN
        RAISE EXCEPTION '09: las cinco columnas no quedaron como se esperaba.';
    END IF;
    IF (SELECT count(*) FROM pg_constraint WHERE connamespace = current_schema()::regnamespace AND conname IN (
            'proveedor_tipo_comprobante_habitual_valido', 'fc_tipo_comprobante_valido',
            'fcd_descuento_2_valido', 'fcd_descuento_3_valido')) <> 4 THEN
        RAISE EXCEPTION '09: faltan restricciones CHECK.';
    END IF;
    RAISE NOTICE '09: TODO OK.';
END $verif$;

COMMIT;
