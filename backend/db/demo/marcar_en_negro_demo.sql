-- =====================================================================================
--  marcar_en_negro_demo.sql  ·  SOLO lacentral_demo (nunca lacentral ni lacentral_test)
--  La Central · 26/11/2026 · Fix E2 (D-S7-12)
--
--  QUÉ HACE
--    Marca cuit_simulado = true (proveedor «en negro») en los tres proveedores sin CUIT de la
--    demo que se usan para recorrer el reconocimiento por razón social del PDF:
--      DEMO-06  «ETMAN Repuestos (demo)»
--      DEMO-07  «Fazio (demo)»
--      00035    «Expoyer»
--    Sin la marca, desde la D-S7-12 ninguno se reconoce. No toca ninguna otra fila.
--    Ojo: por razón social nunca se asigna una factura A; solo presupuestos y comprobantes B.
--
--  CÓMO SE APLICA
--    Lo aplica Javi o Santi, DESPUÉS de sembrar_proveedores_pdf.sql. Una transacción,
--    idempotente (una segunda corrida no cambia nada). Se corta si la base no es
--    lacentral_demo o si falta alguno de los tres.
-- =====================================================================================

ROLLBACK;

BEGIN;

DO $guarda$
BEGIN
    IF current_database() <> 'lacentral_demo' THEN
        RAISE EXCEPTION 'marcar_en_negro_demo: solo corre en lacentral_demo (esta es %).', current_database();
    END IF;
END $guarda$;

-- Código y razón social juntos: un código reutilizado por otro proveedor no se marca.
UPDATE proveedor
   SET cuit_simulado = true
 WHERE (codigo, razon_social) IN (('DEMO-06', 'ETMAN Repuestos (demo)'),
                                  ('DEMO-07', 'Fazio (demo)'),
                                  ('00035',   'Expoyer'))
   AND cuit IS NULL
   AND NOT cuit_simulado;

DO $verif$
DECLARE
    termino text;
BEGIN
    FOREACH termino IN ARRAY ARRAY['etman', 'fazio', 'expoyer'] LOOP
        IF (SELECT count(*) FROM proveedor
             WHERE activo AND cuit_simulado AND razon_social ILIKE '%' || termino || '%') <> 1 THEN
            RAISE EXCEPTION 'marcar_en_negro_demo: tiene que haber exactamente un proveedor activo en negro de «%».', termino;
        END IF;
    END LOOP;
    RAISE NOTICE 'marcar_en_negro_demo: TODO OK.';
END $verif$;

COMMIT;
