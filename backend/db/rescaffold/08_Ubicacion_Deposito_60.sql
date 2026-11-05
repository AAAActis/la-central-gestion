-- =====================================================================================
--  08_Ubicacion_Deposito_60.sql
--  La Central · 04/11/2026
--
--  QUÉ HACE
--    stock.ubicacion_deposito pasa de VARCHAR(30) a VARCHAR(60).
--
--  POR QUÉ
--    El 07 la creó en 30, un número sin fundamento. La validación de la API ya estaba
--    en 60 (ArticuloValidaciones.LargoUbicacion) y articulo.ubicacion_deposito, de
--    donde se migró el dato, era VARCHAR(60). Decisión del 04/11/2026.
--
--  ESTADO (04/11/2026)
--    YA APLICADO a mano por Santi, con el superusuario, en lacentral_test y lacentral.
--    PENDIENTE solo en lacentral_demo, después del 07 (no antes del lunes 09/11).
--    Es idempotente: si la columna ya es de 60, no hace el ALTER y solo verifica.
--    Ensanchar un VARCHAR no reescribe la tabla.
-- =====================================================================================

ROLLBACK;

BEGIN;

DO $alter$
BEGIN
    IF (SELECT character_maximum_length FROM information_schema.columns
        WHERE table_schema = current_schema() AND table_name = 'stock'
          AND column_name = 'ubicacion_deposito') IS DISTINCT FROM 60 THEN
        ALTER TABLE stock ALTER COLUMN ubicacion_deposito TYPE VARCHAR(60);
        RAISE NOTICE '08: stock.ubicacion_deposito ensanchada a VARCHAR(60).';
    ELSE
        RAISE NOTICE '08: stock.ubicacion_deposito ya era VARCHAR(60); sin cambios.';
    END IF;
END $alter$;

DO $verif$
BEGIN
    IF (SELECT character_maximum_length FROM information_schema.columns
        WHERE table_schema = current_schema() AND table_name = 'stock'
          AND column_name = 'ubicacion_deposito') IS DISTINCT FROM 60 THEN
        RAISE EXCEPTION '08: stock.ubicacion_deposito no quedó en VARCHAR(60).';
    END IF;
    RAISE NOTICE '08: stock.ubicacion_deposito en VARCHAR(60). TODO OK.';
END $verif$;

COMMIT;
