-- =====================================================================================
--  marcar_en_negro_demo.sql  ·  SOLO lacentral_demo (nunca lacentral ni lacentral_test)
--  La Central · 26/11/2026 · Fix E2 (D-S7-12)
--
--  QUÉ HACE
--    Marca cuit_simulado = true (proveedor «en negro») SOLO en Fazio, el proveedor de la demo
--    que se usa para recorrer el reconocimiento por razón social del PDF:
--      DEMO-07  «Fazio (demo)»
--    Sin la marca, desde la D-S7-12 no se reconoce. No toca ninguna otra fila, y tampoco
--    desmarca lo que haya marcado una versión anterior de este script.
--    Ojo: por razón social nunca se asigna una factura A; solo presupuestos y comprobantes B.
--
--  CÓMO SE APLICA
--    Lo aplica Javi o Santi, DESPUÉS de sembrar_proveedores_pdf.sql. Una transacción,
--    idempotente (una segunda corrida no cambia nada). Se corta sin cambiar nada si la base no
--    es lacentral_demo, si DEMO-07 «Fazio (demo)» no queda activo y en negro, o si «Fazio» no
--    deja exactamente un proveedor activo en negro.
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
 WHERE (codigo, razon_social) = ('DEMO-07', 'Fazio (demo)')
   AND cuit IS NULL
   AND NOT cuit_simulado;

DO $verif$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM proveedor
                    WHERE (codigo, razon_social) = ('DEMO-07', 'Fazio (demo)') AND activo AND cuit_simulado) THEN
        RAISE EXCEPTION 'marcar_en_negro_demo: DEMO-07 «Fazio (demo)» tiene que quedar activo y en negro.';
    END IF;
    -- La misma normalización que la búsqueda de la aplicación: mayúsculas y sin acentos.
    IF (SELECT count(*) FROM proveedor
         WHERE activo AND cuit_simulado
           AND translate(upper(razon_social), 'ÁÉÍÓÚÜ', 'AEIOUU') LIKE '%FAZIO%') <> 1 THEN
        RAISE EXCEPTION 'marcar_en_negro_demo: tiene que haber exactamente un proveedor activo en negro de «Fazio».';
    END IF;
    RAISE NOTICE 'marcar_en_negro_demo: TODO OK.';
END $verif$;

COMMIT;
