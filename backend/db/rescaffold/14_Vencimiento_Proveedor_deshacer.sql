-- =====================================================================================
--  14_Vencimiento_Proveedor_deshacer.sql
--  La Central · 23/11/2026 · Fix F · inverso del 14
--
--  QUÉ HACE
--    Quita proveedor.vencimiento_modo y proveedor.vencimiento_dias con sus dos CHECK. Idempotente:
--    cada paso se salta si ya no está. Una transacción.
--
--  ADVERTENCIA
--    Borra la configuración de vencimiento de TODOS los proveedores (modo y días). Los datos de
--    la tabla proveedor no cambian. Preferible a esto: restaurar el backup del paso 1 de la
--    aplicación del 14 (pg_restore). Si el código del Fix F ya está desplegado, hay que
--    revertir primero ese código: el modelo de EF espera las columnas.
--
--  CÓMO SE APLICA
--    Igual que el 14: primero en lacentral_test, después en lacentral_demo y lacentral.
-- =====================================================================================

ROLLBACK;

BEGIN;

DO $deshacer$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = current_schema()::regnamespace
            AND conname = 'proveedor_vencimiento_dias_coherente') THEN
        ALTER TABLE proveedor DROP CONSTRAINT proveedor_vencimiento_dias_coherente;
    END IF;
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = current_schema()::regnamespace
            AND conname = 'proveedor_vencimiento_modo_valido') THEN
        ALTER TABLE proveedor DROP CONSTRAINT proveedor_vencimiento_modo_valido;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'proveedor' AND column_name = 'vencimiento_dias') THEN
        ALTER TABLE proveedor DROP COLUMN vencimiento_dias;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'proveedor' AND column_name = 'vencimiento_modo') THEN
        ALTER TABLE proveedor DROP COLUMN vencimiento_modo;
    END IF;
END $deshacer$;

DO $verif$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'proveedor' AND column_name IN ('vencimiento_modo', 'vencimiento_dias')) THEN
        RAISE EXCEPTION '14 deshacer: quedó alguna columna de vencimiento en proveedor.';
    END IF;
    RAISE NOTICE '14 deshacer: TODO OK.';
END $verif$;

COMMIT;
