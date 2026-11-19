-- =====================================================================================
--  11_Nota_Credito_Compra.sql
--  La Central · 18/11/2026 · HU-FCO-07 (057, issue #182) · D-S6-18, D-S6-20 y D-S6-23
--
--  QUÉ HACE
--    1. sucursal.es_reclamos BOOLEAN NOT NULL DEFAULT false, con índice único parcial (a lo
--       sumo un depósito Reclamos), la fila «RC · Reclamos (depósito)» y una fila de stock
--       en 0 para cada artículo en Reclamos (todo artículo tiene una fila por ubicación).
--    2. Nada en factura_compra: la FK compuesta (mismo proveedor) usa el UNIQUE (id, proveedor_id)
--       que ya puso el 07 (factura_compra_id_proveedor_unico). Si falta, se corta.
--    3. nota_credito_compra: la cabecera (proveedor, número, emisión, clase, importe neto,
--       percepciones, origen, usuario, fecha de registro) y su anulación (estado, motivo,
--       fecha, quién la pidió y quién la autorizó, D-S6-23); el número es único por
--       proveedor solo entre las vigentes.
--    4. nota_credito_compra_factura: muchos a muchos con factura_compra, del mismo proveedor.
--    5. nota_credito_compra_detalle: las líneas de una devolución (artículo, sucursal de
--       origen, cantidad).
--
--  POR QUÉ
--    Reglas de Sergio del 18/11/2026 (docs/sprint06/ESTADO.md, «Decisiones · 18/11/2026»):
--    la nota de crédito no cambia el costo (por eso no hay precio en las líneas); referencia
--    al menos una factura del mismo proveedor, muchos a muchos; es siempre tipo A (por eso
--    no hay columna de tipo, y el IVA es el 21 % del neto, no se guarda); por devolución, la
--    mercadería sale de una sucursal o del depósito Reclamos, elegido por línea (D9).
--    Reclamos es una ubicación más (D-S6-20): entra mercadería por transferencia (HU-STK-04).
--
--  REQUISITO
--    El script 10 aplicado (factura_compra_detalle.descuento_4_porcentaje existe). Si no, el
--    script se corta sin tocar nada.
--
--  CÓMO SE APLICA
--    Una transacción. Es idempotente: cada paso se salta si ya está, y la verificación del
--    final corre siempre. Primero en lacentral_test (con el dueño de las tablas), después en
--    lacentral_demo y en lacentral. Después: re-scaffold.
-- =====================================================================================

ROLLBACK;

BEGIN;

DO $aplicar$
DECLARE
    id_reclamos SMALLINT;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'factura_compra_detalle' AND column_name = 'descuento_4_porcentaje') THEN
        RAISE EXCEPTION '11: falta el script 10 (factura_compra_detalle.descuento_4_porcentaje). Aplicá el 10 primero.';
    END IF;

    -- 1 · Depósito Reclamos (D-S6-20)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'sucursal' AND column_name = 'es_reclamos') THEN
        ALTER TABLE sucursal ADD COLUMN es_reclamos BOOLEAN NOT NULL DEFAULT false;
        COMMENT ON COLUMN sucursal.es_reclamos IS
            'TRUE solo para el depósito Reclamos, donde el negocio sigue la mercadería en reclamo al proveedor. Es una ubicación más (stock, transferencias, origen de una devolución), pero no es destino de una factura de compra (HU-FCO-07, D-S6-20, 18/11/2026).';
        RAISE NOTICE '11: sucursal.es_reclamos agregada.';
    END IF;
    CREATE UNIQUE INDEX IF NOT EXISTS ux_sucursal_reclamos ON sucursal (es_reclamos) WHERE es_reclamos;

    SELECT id INTO id_reclamos FROM sucursal WHERE es_reclamos;
    IF id_reclamos IS NULL THEN
        SELECT COALESCE(max(id), 0) + 1 INTO id_reclamos FROM sucursal;
        INSERT INTO sucursal (id, codigo, nombre, es_reclamos) VALUES (id_reclamos, 'RC', 'Reclamos (depósito)', true);
        RAISE NOTICE '11: sucursal % «Reclamos (depósito)» creada.', id_reclamos;
    END IF;
    INSERT INTO stock (articulo_id, sucursal_id)
        SELECT a.id, id_reclamos FROM articulo a
        ON CONFLICT (articulo_id, sucursal_id) DO NOTHING;

    -- 2 · La FK compuesta (mismo proveedor) usa el UNIQUE (id, proveedor_id) del 07. Un segundo
    --     UNIQUE igual sería un índice duplicado (y PostgreSqlPruebas no podría aparearlo).
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE connamespace = current_schema()::regnamespace
            AND conname = 'factura_compra_id_proveedor_unico') THEN
        RAISE EXCEPTION '11: falta factura_compra_id_proveedor_unico (script 07).';
    END IF;

    -- 3 · Cabecera
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema()
            AND table_name = 'nota_credito_compra') THEN
        CREATE TABLE nota_credito_compra (
            id                   INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            proveedor_id         INTEGER       NOT NULL REFERENCES proveedor(id),
            numero               VARCHAR(30)   NOT NULL,
            fecha_emision        DATE          NOT NULL,
            clase                VARCHAR(12)   NOT NULL,
            importe_neto         NUMERIC(14,2) NOT NULL,
            percepcion_iva       NUMERIC(14,2),
            percepcion_iibb      NUMERIC(14,2),
            percepcion_municipal NUMERIC(14,2),
            percepcion_ganancias NUMERIC(14,2),
            origen               VARCHAR(10)   NOT NULL DEFAULT 'MANUAL',
            usuario_id           INTEGER       NOT NULL REFERENCES usuario(id),
            fecha_registro       TIMESTAMPTZ   NOT NULL DEFAULT now(),
            -- Anulación (D-S6-23, 18/11/2026), como factura_compra: motivo y doble certificación.
            estado                  VARCHAR(10)   NOT NULL DEFAULT 'ACTIVA',
            motivo_anulacion        TEXT,
            fecha_anulacion         TIMESTAMPTZ,
            usuario_anulacion_id    INTEGER       REFERENCES usuario(id),
            usuario_autorizacion_id INTEGER       REFERENCES usuario(id),
            CONSTRAINT ncc_estado_valido CHECK (estado IN ('ACTIVA', 'ANULADA')),
            CONSTRAINT ncc_anulacion_coherente CHECK (
                (estado = 'ACTIVA'  AND motivo_anulacion IS NULL AND fecha_anulacion IS NULL
                    AND usuario_anulacion_id IS NULL AND usuario_autorizacion_id IS NULL) OR
                (estado = 'ANULADA' AND motivo_anulacion IS NOT NULL AND fecha_anulacion IS NOT NULL
                    AND usuario_anulacion_id IS NOT NULL AND usuario_autorizacion_id IS NOT NULL)),
            CONSTRAINT ncc_autorizacion_distinta CHECK (usuario_autorizacion_id <> usuario_anulacion_id),
            CONSTRAINT ncc_id_proveedor_unico UNIQUE (id, proveedor_id),
            CONSTRAINT ncc_clase_valida CHECK (clase IN ('BONIFICACION', 'DEVOLUCION')),
            CONSTRAINT ncc_origen_valido CHECK (origen IN ('MANUAL', 'PDF')),
            CONSTRAINT ncc_importe_positivo CHECK (importe_neto > 0),
            CONSTRAINT ncc_percepciones_no_negativas CHECK (
                COALESCE(percepcion_iva, 0) >= 0 AND COALESCE(percepcion_iibb, 0) >= 0 AND
                COALESCE(percepcion_municipal, 0) >= 0 AND COALESCE(percepcion_ganancias, 0) >= 0)
        );
        -- El número se libera al anularse: la unicidad rige solo entre las vigentes (como la factura).
        CREATE UNIQUE INDEX ux_nota_credito_compra_numero_activa
            ON nota_credito_compra (proveedor_id, numero) WHERE estado = 'ACTIVA';
        COMMENT ON TABLE nota_credito_compra IS
            'Nota de crédito de un proveedor (HU-FCO-07). Siempre tipo A: el número se copia exacto y el IVA es el 21 % del neto, que no se guarda. No cambia el costo de ningún artículo: es plata a favor en la cuenta corriente con el proveedor, que el sistema no lleva (Sergio, 18/11/2026).';
        COMMENT ON COLUMN nota_credito_compra.clase IS
            'BONIFICACION: bonificación o diferencia de precio, solo importes, no mueve stock. DEVOLUCION: devolución de mercadería, mueve stock (nota_credito_compra_detalle).';
        COMMENT ON COLUMN nota_credito_compra.importe_neto IS
            'Importe neto del comprobante, sin IVA ni percepciones. En una devolución también se transcribe: las líneas mueven stock, no plata.';
        RAISE NOTICE '11: nota_credito_compra creada.';
    END IF;

    -- 4 · Muchos a muchos con factura_compra, del mismo proveedor
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema()
            AND table_name = 'nota_credito_compra_factura') THEN
        CREATE TABLE nota_credito_compra_factura (
            nota_credito_compra_id INTEGER NOT NULL,
            factura_compra_id      INTEGER NOT NULL,
            proveedor_id           INTEGER NOT NULL,
            PRIMARY KEY (nota_credito_compra_id, factura_compra_id),
            CONSTRAINT nccf_nota_mismo_proveedor FOREIGN KEY (nota_credito_compra_id, proveedor_id)
                REFERENCES nota_credito_compra (id, proveedor_id) ON DELETE CASCADE,
            CONSTRAINT nccf_factura_mismo_proveedor FOREIGN KEY (factura_compra_id, proveedor_id)
                REFERENCES factura_compra (id, proveedor_id)
        );
        COMMENT ON TABLE nota_credito_compra_factura IS
            'Facturas de compra a las que corresponde una nota de crédito: muchos a muchos. Las dos FK compuestas con proveedor_id hacen que la base rechace una factura de otro proveedor (Sergio, 18/11/2026).';
        RAISE NOTICE '11: nota_credito_compra_factura creada.';
    END IF;

    -- 5 · Líneas de una devolución
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema()
            AND table_name = 'nota_credito_compra_detalle') THEN
        CREATE TABLE nota_credito_compra_detalle (
            nota_credito_compra_id INTEGER       NOT NULL REFERENCES nota_credito_compra(id) ON DELETE CASCADE,
            articulo_id            INTEGER       NOT NULL REFERENCES articulo(id),
            sucursal_id            SMALLINT      NOT NULL REFERENCES sucursal(id),
            cantidad               NUMERIC(12,3) NOT NULL,
            PRIMARY KEY (nota_credito_compra_id, articulo_id, sucursal_id),
            CONSTRAINT nccd_cantidad_positiva CHECK (cantidad > 0)
        );
        COMMENT ON TABLE nota_credito_compra_detalle IS
            'Mercadería devuelta en una nota de crédito por devolución. Sin precio: la nota no cambia costos.';
        COMMENT ON COLUMN nota_credito_compra_detalle.sucursal_id IS
            'Origen de la mercadería de ESTA línea: la sucursal que la devuelve o el depósito Reclamos, elegido por línea como el destino de la factura (D9). El stock baja contra esta ubicación.';
        RAISE NOTICE '11: nota_credito_compra_detalle creada.';
    END IF;
END $aplicar$;

DO $verif$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
            AND table_name = 'sucursal' AND column_name = 'es_reclamos' AND is_nullable = 'NO') THEN
        RAISE EXCEPTION '11: sucursal.es_reclamos no quedó BOOLEAN NOT NULL.';
    END IF;
    IF (SELECT count(*) FROM sucursal WHERE es_reclamos) <> 1 THEN
        RAISE EXCEPTION '11: tiene que haber exactamente un depósito Reclamos.';
    END IF;
    IF EXISTS (SELECT 1 FROM articulo a WHERE NOT EXISTS (SELECT 1 FROM stock s JOIN sucursal u ON u.id = s.sucursal_id
            WHERE s.articulo_id = a.id AND u.es_reclamos)) THEN
        RAISE EXCEPTION '11: hay artículos sin fila de stock en Reclamos.';
    END IF;
    IF (SELECT count(*) FROM pg_constraint WHERE connamespace = current_schema()::regnamespace AND conname IN (
            'ncc_id_proveedor_unico', 'ncc_clase_valida',
            'ncc_estado_valido', 'ncc_anulacion_coherente', 'ncc_autorizacion_distinta',
            'ncc_origen_valido', 'ncc_importe_positivo', 'ncc_percepciones_no_negativas',
            'nccf_nota_mismo_proveedor', 'nccf_factura_mismo_proveedor', 'nccd_cantidad_positiva')) <> 11 THEN
        RAISE EXCEPTION '11: faltan restricciones (se esperaban 11).';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = current_schema()
            AND indexname = 'ux_nota_credito_compra_numero_activa') THEN
        RAISE EXCEPTION '11: falta el índice ux_nota_credito_compra_numero_activa.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = current_schema() AND indexname = 'ux_sucursal_reclamos') THEN
        RAISE EXCEPTION '11: falta el índice ux_sucursal_reclamos.';
    END IF;
    RAISE NOTICE '11: TODO OK.';
END $verif$;

COMMIT;
