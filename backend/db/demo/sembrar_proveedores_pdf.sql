-- =====================================================================================
--  sembrar_proveedores_pdf.sql  ·  SOLO lacentral_demo (nunca lacentral ni lacentral_test)
--  La Central · 23/11/2026 · Fix E (#196)
--
--  QUÉ HACE
--    Agrega a la demo dos proveedores inventados para poder recorrer a mano el reconocimiento
--    por razón social del PDF: ETMAN y Fazio. Expoyer ya está (id 7, «Expoyer»).
--      DEMO-06  «ETMAN Repuestos (demo)»   activo, sin CUIT
--      DEMO-07  «Fazio (demo)»             activo, sin CUIT
--    Sin CUIT a propósito (cuit NULL, cuit_simulado false): ningún CUIT del PDF los ubica, así
--    que el camino que se prueba es el de la razón social, que los deja marcados «Revisar».
--    Los códigos continúan DEMO-01 a DEMO-05 de sembrar_demo.cs.
--
--  CÓMO SE APLICA
--    Una transacción, idempotente por código (una segunda corrida no cambia nada ni duplica).
--    Se corta si la base no es lacentral_demo. No toca ninguna otra fila.
-- =====================================================================================

ROLLBACK;

BEGIN;

DO $guarda$
BEGIN
    IF current_database() <> 'lacentral_demo' THEN
        RAISE EXCEPTION 'sembrar_proveedores_pdf: solo corre en lacentral_demo (esta es %).', current_database();
    END IF;
END $guarda$;

INSERT INTO proveedor (codigo, razon_social, cuit, cuit_simulado, activo)
VALUES ('DEMO-06', 'ETMAN Repuestos (demo)', NULL, false, true),
       ('DEMO-07', 'Fazio (demo)',           NULL, false, true)
ON CONFLICT (codigo) DO NOTHING;

DO $verif$
BEGIN
    IF (SELECT count(*) FROM proveedor WHERE codigo IN ('DEMO-06', 'DEMO-07') AND activo AND cuit IS NULL) <> 2 THEN
        RAISE EXCEPTION 'sembrar_proveedores_pdf: DEMO-06 y DEMO-07 tienen que existir, activos y sin CUIT.';
    END IF;
    IF (SELECT count(*) FROM proveedor WHERE activo AND razon_social ILIKE '%etman%') <> 1
       OR (SELECT count(*) FROM proveedor WHERE activo AND razon_social ILIKE '%fazio%') <> 1 THEN
        RAISE EXCEPTION 'sembrar_proveedores_pdf: tiene que haber exactamente un proveedor activo de ETMAN y uno de Fazio.';
    END IF;
    RAISE NOTICE 'sembrar_proveedores_pdf: TODO OK.';
END $verif$;

COMMIT;
