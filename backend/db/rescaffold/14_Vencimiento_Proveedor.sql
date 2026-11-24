-- =====================================================================================
--  14_Vencimiento_Proveedor.sql
--  La Central · 23/11/2026 · Fix F · vencimiento automático según el proveedor
--
--  QUÉ HACE
--    1. proveedor.vencimiento_modo  VARCHAR(30) NOT NULL DEFAULT 'MANUAL', con CHECK en
--       ('MANUAL', 'CONTADO', 'DIAS_CORRIDOS', 'DIA_FIJO_MES_SIGUIENTE').
--    2. proveedor.vencimiento_dias  SMALLINT NULL, con CHECK de coherencia con el modo:
--         MANUAL, CONTADO            -> NULL
--         DIAS_CORRIDOS              -> 7, 10, 20, 21, 25 o 30 (NULL no pasa)
--         DIA_FIJO_MES_SIGUIENTE     -> 1 a 28 (NULL no pasa)
--    Los proveedores existentes quedan en MANUAL: nada cambia hasta que alguien configure uno.
--    No hay proveedores hardcodeados: a Fric-Rot se le configura DIA_FIJO_MES_SIGUIENTE con
--    25 desde la pantalla de proveedores o por datos, no en este script.
--
--  POR QUÉ
--    Decisiones de Sergio (vía Javi, 23/11/2026): el vencimiento se calcula desde la fecha de
--    emisión de la factura; la condición se configura por proveedor; Fric-Rot vence el día 25
--    del mes siguiente a la emisión; Etman queda aparte (su PDF ya trae el vencimiento).
--    Sin definir, siguen manuales: «Quincena y 15/25/30 días», «Cuenta corriente 30-60» y
--    «30-60-90» (varios vencimientos en un campo) y el «-5 %» de «CTA CTE 15 DIAS -5%».
--    Supuesto a confirmar con Sergio: CONTADO vence el mismo día de la emisión.
--
--  REQUISITO
--    Ninguno más allá de la tabla proveedor.
--
--  CÓMO SE APLICA
--    Una transacción. Es idempotente: cada ALTER se salta si ya está hecho, y la verificación
--    del final corre siempre. Lo aplica Santi, primero en lacentral_test (public, con el dueño
--    de las tablas), después en lacentral_demo y en lacentral. Después: re-scaffold.
-- =====================================================================================

ROLLBACK;

BEGIN;

DO $alter$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'proveedor' AND column_name = 'vencimiento_modo') THEN
        ALTER TABLE proveedor ADD COLUMN vencimiento_modo VARCHAR(30) NOT NULL DEFAULT 'MANUAL';
        RAISE NOTICE '14: proveedor.vencimiento_modo agregada.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'proveedor' AND column_name = 'vencimiento_dias') THEN
        ALTER TABLE proveedor ADD COLUMN vencimiento_dias SMALLINT;
        RAISE NOTICE '14: proveedor.vencimiento_dias agregada.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = current_schema()::regnamespace
            AND conname = 'proveedor_vencimiento_modo_valido') THEN
        ALTER TABLE proveedor ADD CONSTRAINT proveedor_vencimiento_modo_valido
            CHECK (vencimiento_modo IN ('MANUAL', 'CONTADO', 'DIAS_CORRIDOS', 'DIA_FIJO_MES_SIGUIENTE'));
    END IF;

    -- Un CHECK deja pasar el NULL (resultado «desconocido»): sin IS NOT NULL, DIAS_CORRIDOS o
    -- DIA_FIJO_MES_SIGUIENTE con días NULL pasaban. Una base donde ya se aplicó la primera
    -- versión del 14 (sin IS NOT NULL) se corrige acá: se reemplaza la restricción.
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = current_schema()::regnamespace
            AND conname = 'proveedor_vencimiento_dias_coherente'
            AND pg_get_constraintdef(oid) NOT LIKE '%IS NOT NULL%') THEN
        ALTER TABLE proveedor DROP CONSTRAINT proveedor_vencimiento_dias_coherente;
        RAISE NOTICE '14: proveedor_vencimiento_dias_coherente reemplazada (la versión vieja dejaba pasar días NULL).';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = current_schema()::regnamespace
            AND conname = 'proveedor_vencimiento_dias_coherente') THEN
        ALTER TABLE proveedor ADD CONSTRAINT proveedor_vencimiento_dias_coherente
            CHECK (
                (vencimiento_modo IN ('MANUAL', 'CONTADO') AND vencimiento_dias IS NULL)
                OR (vencimiento_modo = 'DIAS_CORRIDOS' AND vencimiento_dias IS NOT NULL
                    AND vencimiento_dias IN (7, 10, 20, 21, 25, 30))
                OR (vencimiento_modo = 'DIA_FIJO_MES_SIGUIENTE' AND vencimiento_dias IS NOT NULL
                    AND vencimiento_dias BETWEEN 1 AND 28)
            );
    END IF;

    COMMENT ON COLUMN proveedor.vencimiento_modo IS
        'Cómo vence una factura de este proveedor, desde su fecha de emisión: MANUAL (no se calcula; se carga a mano, por defecto), CONTADO (el mismo día), DIAS_CORRIDOS (emisión + vencimiento_dias) o DIA_FIJO_MES_SIGUIENTE (el día vencimiento_dias del mes siguiente a la emisión). Sergio vía Javi, 23/11/2026.';
    COMMENT ON COLUMN proveedor.vencimiento_dias IS
        'Los días de vencimiento_modo: NULL en MANUAL y CONTADO; 7, 10, 20, 21, 25 o 30 en DIAS_CORRIDOS; 1 a 28 (el día del mes) en DIA_FIJO_MES_SIGUIENTE. Sergio vía Javi, 23/11/2026.';
END $alter$;

DO $verif$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'proveedor' AND column_name = 'vencimiento_modo'
            AND is_nullable = 'NO' AND character_maximum_length = 30
            AND column_default LIKE '%MANUAL%') THEN
        RAISE EXCEPTION '14: vencimiento_modo no quedó VARCHAR(30) NOT NULL DEFAULT ''MANUAL''.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'proveedor' AND column_name = 'vencimiento_dias'
            AND is_nullable = 'YES' AND data_type = 'smallint') THEN
        RAISE EXCEPTION '14: vencimiento_dias no quedó SMALLINT NULL.';
    END IF;
    IF EXISTS (SELECT 1 FROM proveedor WHERE vencimiento_modo <> 'MANUAL' OR vencimiento_dias IS NOT NULL) THEN
        RAISE NOTICE '14: hay proveedores ya configurados (normal si es una segunda corrida).';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = current_schema()::regnamespace
            AND conname = 'proveedor_vencimiento_modo_valido') THEN
        RAISE EXCEPTION '14: falta la restricción CHECK proveedor_vencimiento_modo_valido.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = current_schema()::regnamespace
            AND conname = 'proveedor_vencimiento_dias_coherente'
            AND pg_get_constraintdef(oid) LIKE '%IS NOT NULL%') THEN
        RAISE EXCEPTION '14: falta la restricción CHECK proveedor_vencimiento_dias_coherente (con IS NOT NULL en los modos con días).';
    END IF;
    RAISE NOTICE '14: TODO OK.';
END $verif$;

COMMIT;
