-- =====================================================================================
-- 12_Catalogo_Proveedores.sql
-- La Central · 23/11/2026 · Compuerta C2 · HU-CMP-08 (055, issue #128) · D-S7-01
--
-- QUÉ HACE
-- 1. scrape_corrida: cada lectura del catálogo de un proveedor que hace n8n (estado
--    RUNNING, SUCCESS o FAILED; total que informó el sitio; cantidad de ítems leídos; id
--    de la ejecución de n8n; inicio y fin). A lo sumo una RUNNING por proveedor.
-- 2. proveedor_articulo: el catálogo ACTUAL de cada proveedor, una fila por código del
--    proveedor (UNIQUE (proveedor_id, codigo)): se reingesta con
--    INSERT ... ON CONFLICT (proveedor_id, codigo) DO UPDATE, sin duplicar. Lo que no vino
--    en la última corrida completa queda vencido (vencido = true), no se borra.
-- 3. proveedor_articulo_historial: precios y stock de cada fila en cada corrida (RF-016).
--    Una fila por (renglón, corrida).
--
-- DATOS REALES (Expoyer, relevado el 23/11/2026; docs/sprint07/ESTADO.md)
-- - ~100.000 renglones por proveedor, bajados por rubro de a 500 (~400 pedidos por
--   corrida), con repetidos que el upsert absorbe. Todo lo que corre por renglón usa la
--   clave (proveedor_id, codigo) o el índice de la corrida: nada recorre el catálogo entero
--   salvo el vencimiento, una vez por corrida y por proveedor_id.
-- - Stock: un ESTADO por depósito del proveedor (se guarda el que le sirve a La Central),
--   no una cantidad. La cantidad queda NULL para quien no la informa.
-- - Precios: lista, neto con el descuento del cliente y, opcional, pago anticipado. El
--   precio de venta que muestra la web del proveedor NO se guarda.
-- - Código: el comercial (el que viene en las facturas y se cruza con
--   articulo_codigo_alternativo); el interno del portal es id_externo.
-- 4. El rol lacentral_n8n, sin clave (la pone Santi, nunca en el repo): SELECT, INSERT y
--    UPDATE en las tres tablas de arriba, SELECT en proveedor, y nada más. Sin DELETE: lo
--    que no vino en una corrida se marca activo = false, no se borra.
--
-- QUÉ SE REUSA
-- - proveedor: tal cual. n8n lo lee para saber a quién scrapea y si está activo.
-- - articulo_codigo_alternativo ES la vinculación: (articulo_id, proveedor_id, codigo).
--   Un renglón del catálogo está vinculado cuando su (proveedor_id, codigo) aparece ahí.
--   No hay una tabla de vinculación nueva, y la vinculación NO apunta al id del renglón:
--   reingestar (o rehacer el catálogo entero) no la pierde. n8n no tiene permiso sobre
--   esa tabla. OJO: su codigo es único en TODO el sistema (D2, N-8): si dos proveedores
--   usan el mismo código comercial, solo uno de los dos renglones se puede vincular.
-- - precio_proveedor queda como está (último precio por ARTÍCULO propio, para la consulta
--   en vivo de la 031, la factura y la carga manual). El catálogo es por renglón del
--   proveedor, vinculado o no: no entra en una tabla con clave articulo_id.
--
-- POR QUÉ NO HAY TRIGGERS
-- PostgreSqlPruebas clona las tablas con LIKE ... INCLUDING ALL, que no copia triggers:
-- una regla en un trigger no se podría probar. Lo que la base no garantiza con
-- restricciones y permisos lo hace n8n con el SQL de docs/sprint07/CONTRATO-N8N.md.
--
-- REQUISITO
-- El script 11 aplicado (nota_credito_compra existe). Si no, se corta sin tocar nada.
--
-- CÓMO SE APLICA
-- Una transacción. Es idempotente: cada paso se salta si ya está, y la verificación del
-- final corre siempre. OJO: una tabla que ya existe se salta entera; si se aplicó una
-- versión anterior de este script, la verificación lo detecta y corta. Con el superusuario lacentral (crea un rol). Primero en
-- lacentral_test, después en lacentral_demo y en lacentral. Después: re-scaffold.
-- El rol es de todo el servidor: se crea la primera vez; los permisos van por base.
-- =====================================================================================
ROLLBACK;
BEGIN;

DO $aplicar$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema()
                   AND table_name = 'nota_credito_compra') THEN
        RAISE EXCEPTION '12: falta el script 11 (nota_credito_compra). Aplicá el 11 primero.';
    END IF;

    -- 1 · Corridas
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema()
                   AND table_name = 'scrape_corrida') THEN
        CREATE TABLE scrape_corrida (
            id               INTEGER     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            proveedor_id     INTEGER     NOT NULL REFERENCES proveedor(id),
            estado           VARCHAR(10) NOT NULL DEFAULT 'RUNNING',
            inicio           TIMESTAMPTZ NOT NULL DEFAULT now(),
            fin              TIMESTAMPTZ,
            total_informado  INTEGER,
            cantidad_items   INTEGER,
            ejecucion_n8n    VARCHAR(100),
            mensaje_error    TEXT,
            CONSTRAINT scrape_corrida_estado_valido CHECK (estado IN ('RUNNING', 'SUCCESS', 'FAILED')),
            CONSTRAINT scrape_corrida_cierre_coherente CHECK (
                (estado = 'RUNNING' AND fin IS NULL AND cantidad_items IS NULL) OR
                (estado <> 'RUNNING' AND fin IS NOT NULL AND cantidad_items IS NOT NULL)),
            CONSTRAINT scrape_corrida_fin_posterior CHECK (fin IS NULL OR fin >= inicio),
            CONSTRAINT scrape_corrida_cantidades_no_negativas CHECK (
                COALESCE(total_informado, 0) >= 0 AND COALESCE(cantidad_items, 0) >= 0),
            CONSTRAINT scrape_corrida_id_proveedor_unico UNIQUE (id, proveedor_id)
        );
        -- El orquestador nocturno y un disparo manual no pueden pisarse. Con estado en la clave
        -- (constante dentro del índice parcial) la unicidad es la misma, y el scaffold no lee la
        -- relación con proveedor como 1:1 (la trampa del índice parcial de D10).
        CREATE UNIQUE INDEX ux_scrape_corrida_en_curso ON scrape_corrida (proveedor_id, estado)
            WHERE estado = 'RUNNING';
        COMMENT ON TABLE scrape_corrida IS
            'Cada lectura del catálogo de un proveedor que hace n8n (D-S7-01, 23/11/2026). Se abre en RUNNING y se cierra en SUCCESS o FAILED con la cantidad de ítems leídos (la cuenta del historial de la corrida). A lo sumo una RUNNING por proveedor. HU-CMP-08 (055).';
        COMMENT ON COLUMN scrape_corrida.total_informado IS
            'Total de artículos que informó el sitio en el sondeo inicial (página de 1 artículo). Se compara con cantidad_items al cerrar.';
        COMMENT ON COLUMN scrape_corrida.ejecucion_n8n IS
            'Id de la ejecución de n8n ($execution.id), para encontrarla en su historial.';
        RAISE NOTICE '12: scrape_corrida creada.';
    END IF;

    -- 2 · Catálogo actual
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema()
                   AND table_name = 'proveedor_articulo') THEN
        CREATE TABLE proveedor_articulo (
            id                 INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            proveedor_id       INTEGER       NOT NULL REFERENCES proveedor(id),
            codigo             VARCHAR(60)   NOT NULL,
            id_externo         VARCHAR(100),
            codigo_barras      VARCHAR(40),
            numero_original    VARCHAR(300),
            nombre             VARCHAR(300)  NOT NULL COLLATE "es-AR-x-icu",
            descripcion        TEXT,
            detalle_tecnico    TEXT,
            marca              VARCHAR(120),
            rubro              VARCHAR(120),
            precio_neto        NUMERIC(14,2),
            precio_lista       NUMERIC(14,2),
            precio_pago_anticipado NUMERIC(14,2),
            estado_stock       VARCHAR(12),
            stock              NUMERIC(14,2),
            alicuota_iva       NUMERIC(5,2),
            url_imagen         VARCHAR(1000),
            ultima_lectura     TIMESTAMPTZ   NOT NULL DEFAULT now(),
            fecha_origen       TIMESTAMPTZ,
            activo             BOOLEAN       NOT NULL DEFAULT TRUE,
            vencido            BOOLEAN       NOT NULL DEFAULT FALSE,
            ultima_corrida_id  INTEGER       NOT NULL,
            CONSTRAINT proveedor_articulo_codigo_unico UNIQUE (proveedor_id, codigo),
            CONSTRAINT proveedor_articulo_id_proveedor_unico UNIQUE (id, proveedor_id),
            CONSTRAINT proveedor_articulo_corrida_mismo_proveedor FOREIGN KEY (ultima_corrida_id, proveedor_id)
                REFERENCES scrape_corrida (id, proveedor_id),
            CONSTRAINT proveedor_articulo_textos_no_vacios CHECK (btrim(codigo) <> '' AND btrim(nombre) <> ''),
            CONSTRAINT proveedor_articulo_precios_no_negativos CHECK (
                COALESCE(precio_neto, 0) >= 0 AND COALESCE(precio_lista, 0) >= 0
                AND COALESCE(precio_pago_anticipado, 0) >= 0),
            CONSTRAINT proveedor_articulo_estado_stock_valido CHECK (
                estado_stock IS NULL OR estado_stock IN ('HAY', 'NO_HAY', 'A_CONFIRMAR')),
            CONSTRAINT proveedor_articulo_alicuota_valida CHECK (alicuota_iva IS NULL OR alicuota_iva BETWEEN 0 AND 100)
        );
        -- Búsqueda por aproximación de la 053 y del comparador (030), como articulo.nombre.
        CREATE INDEX ix_proveedor_articulo_nombre_trgm ON proveedor_articulo USING gin (nombre gin_trgm_ops);
        CREATE INDEX ix_proveedor_articulo_codigo_barras ON proveedor_articulo (codigo_barras)
            WHERE codigo_barras IS NOT NULL;
        COMMENT ON TABLE proveedor_articulo IS
            'Catálogo actual de cada proveedor, una fila por código del proveedor, que n8n reingesta en cada corrida con INSERT ... ON CONFLICT (proveedor_id, codigo) DO UPDATE (D-S7-01, 23/11/2026). La vinculación con un artículo propio es articulo_codigo_alternativo (mismo proveedor_id y codigo): no apunta al id de esta fila, así que reingestar no la pierde. HU-CMP-08 (055).';
        COMMENT ON COLUMN proveedor_articulo.codigo IS
            'Código comercial del artículo en el proveedor, el que viene en sus facturas (Expoyer: commercialCode, «C 000100 14 E»). Clave de la reingesta junto con proveedor_id, y el que se cruza con articulo_codigo_alternativo.codigo.';
        COMMENT ON COLUMN proveedor_articulo.id_externo IS
            'Identificador del renglón en el portal del proveedor (Expoyer: internalCode, «C00010014E»).';
        COMMENT ON COLUMN proveedor_articulo.numero_original IS
            'Número original de fabricante; puede traer varios separados por « / » (Expoyer: familyCode).';
        COMMENT ON COLUMN proveedor_articulo.marca IS
            'Marca del PRODUCTO (Expoyer: supplier.name). No la del vehículo (brandName).';
        COMMENT ON COLUMN proveedor_articulo.rubro IS
            'Rubro del proveedor, en sus palabras (Expoyer: category.name). No es un rubro de La Central.';
        COMMENT ON COLUMN proveedor_articulo.precio_lista IS
            'Precio de lista del proveedor, sin IVA (Expoyer: basePrice).';
        COMMENT ON COLUMN proveedor_articulo.precio_pago_anticipado IS
            'Neto con pago anticipado, si el proveedor lo informa (Expoyer: earlyPaymentPrice). El precio de venta que muestra el portal (salePrice, neto + margen de la vista cliente) NO se guarda.';
        COMMENT ON COLUMN proveedor_articulo.estado_stock IS
            'Disponibilidad que informa el proveedor en el depósito que le sirve a La Central (Expoyer: CORDOBA; 1 = HAY, 3 = NO_HAY, 2 = A_CONFIRMAR hasta que Sergio diga qué es). NULL = no informa.';
        COMMENT ON COLUMN proveedor_articulo.stock IS
            'Cantidad, solo si el proveedor la informa (Expoyer no). NULL = no informa, nunca cero.';
        COMMENT ON COLUMN proveedor_articulo.alicuota_iva IS
            'Alícuota que informa el proveedor (21 = 21 %). NULL = no la informa (Expoyer: vatRate null): el comparador usa la del artículo propio o, sin ella, 21 % (D-S7-02).';
        COMMENT ON COLUMN proveedor_articulo.detalle_tecnico IS
            'Ficha técnica aplanada: «CLAVE: valor | CLAVE: valor».';
        COMMENT ON COLUMN proveedor_articulo.precio_neto IS
            'Costo neto, sin IVA, con el descuento del cliente (Expoyer: discountedPrice = lista × (1 − 61,08 %) × (1 − 15 %), validado contra su factura). Desconocido es NULL, nunca cero.';
        COMMENT ON COLUMN proveedor_articulo.url_imagen IS
            'URL de la imagen en el sitio del proveedor. Las imágenes no se descargan.';
        COMMENT ON COLUMN proveedor_articulo.fecha_origen IS
            'Fecha que el sitio del proveedor da para el dato (actualización del precio), si la da.';
        COMMENT ON COLUMN proveedor_articulo.activo IS
            'Lo que dice el proveedor: FALSE si lo publica como discontinuado. Si no informa nada, TRUE.';
        COMMENT ON COLUMN proveedor_articulo.vencido IS
            'TRUE = no vino en la última corrida SUCCESS completa del proveedor. No se borra: conserva su vinculación y su historial, y vuelve a FALSE si reaparece (055 CA-003).';
        COMMENT ON COLUMN proveedor_articulo.ultima_corrida_id IS
            'Última corrida que leyó este renglón. Al cerrar en SUCCESS, lo que no tenga esta corrida se marca vencido.';
        RAISE NOTICE '12: proveedor_articulo creada.';
    END IF;

    -- 3 · Historial por corrida (RF-016)
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema()
                   AND table_name = 'proveedor_articulo_historial') THEN
        CREATE TABLE proveedor_articulo_historial (
            proveedor_articulo_id INTEGER       NOT NULL,
            corrida_id            INTEGER       NOT NULL,
            proveedor_id          INTEGER       NOT NULL,
            precio_neto           NUMERIC(14,2),
            precio_lista          NUMERIC(14,2),
            precio_pago_anticipado NUMERIC(14,2),
            estado_stock          VARCHAR(12),
            stock                 NUMERIC(14,2),
            fecha_lectura         TIMESTAMPTZ   NOT NULL DEFAULT now(),
            PRIMARY KEY (proveedor_articulo_id, corrida_id),
            CONSTRAINT pah_articulo_mismo_proveedor FOREIGN KEY (proveedor_articulo_id, proveedor_id)
                REFERENCES proveedor_articulo (id, proveedor_id),
            CONSTRAINT pah_corrida_mismo_proveedor FOREIGN KEY (corrida_id, proveedor_id)
                REFERENCES scrape_corrida (id, proveedor_id),
            CONSTRAINT pah_precios_no_negativos CHECK (
                COALESCE(precio_neto, 0) >= 0 AND COALESCE(precio_lista, 0) >= 0
                AND COALESCE(precio_pago_anticipado, 0) >= 0),
            CONSTRAINT pah_estado_stock_valido CHECK (
                estado_stock IS NULL OR estado_stock IN ('HAY', 'NO_HAY', 'A_CONFIRMAR'))
        );
        -- Para contar los ítems de una corrida al cerrarla.
        CREATE INDEX ix_pah_corrida ON proveedor_articulo_historial (corrida_id);
        COMMENT ON TABLE proveedor_articulo_historial IS
            'Precios y stock de cada renglón del catálogo en cada corrida (RF-016). Una fila por (renglón, corrida): un renglón repetido dentro de la misma corrida (un rubro bajado en los dos sentidos) no duplica. Crece ~100.000 filas por corrida y proveedor. Las dos FK compuestas con proveedor_id impiden mezclar proveedores. HU-CMP-08 (055).';
        RAISE NOTICE '12: proveedor_articulo_historial creada.';
    END IF;
END $aplicar$;

-- >>> ROL n8n
DO $rol$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'lacentral_n8n') THEN
        -- Sin clave: la pone Santi a mano (\password lacentral_n8n). Nunca en el repo.
        CREATE ROLE lacentral_n8n LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
        RAISE NOTICE '12: rol lacentral_n8n creado (sin clave).';
    END IF;
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO lacentral_n8n', current_database());
    EXECUTE format('GRANT USAGE ON SCHEMA %I TO lacentral_n8n', current_schema());
    -- Nada heredado de antes: solo lo de abajo.
    EXECUTE format('REVOKE ALL ON ALL TABLES IN SCHEMA %I FROM lacentral_n8n', current_schema());
    EXECUTE format('REVOKE ALL ON ALL SEQUENCES IN SCHEMA %I FROM lacentral_n8n', current_schema());
    GRANT SELECT ON proveedor TO lacentral_n8n;
    GRANT SELECT, INSERT, UPDATE ON scrape_corrida, proveedor_articulo, proveedor_articulo_historial TO lacentral_n8n;
END $rol$;
-- <<< ROL n8n

DO $verif$
BEGIN
    IF (SELECT count(*) FROM information_schema.tables WHERE table_schema = current_schema()
        AND table_name IN ('scrape_corrida', 'proveedor_articulo', 'proveedor_articulo_historial')) <> 3 THEN
        RAISE EXCEPTION '12: faltan tablas (se esperaban 3).';
    END IF;
    IF (SELECT count(*) FROM pg_constraint WHERE connamespace = current_schema()::regnamespace AND conname IN (
            'scrape_corrida_estado_valido', 'scrape_corrida_cierre_coherente', 'scrape_corrida_fin_posterior',
            'scrape_corrida_cantidades_no_negativas', 'scrape_corrida_id_proveedor_unico',
            'proveedor_articulo_codigo_unico', 'proveedor_articulo_id_proveedor_unico',
            'proveedor_articulo_corrida_mismo_proveedor', 'proveedor_articulo_textos_no_vacios',
            'proveedor_articulo_precios_no_negativos', 'proveedor_articulo_alicuota_valida',
            'proveedor_articulo_estado_stock_valido', 'pah_estado_stock_valido',
            'pah_articulo_mismo_proveedor', 'pah_corrida_mismo_proveedor', 'pah_precios_no_negativos')) <> 16 THEN
        RAISE EXCEPTION '12: faltan restricciones (se esperaban 16).';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema()
                   AND table_name = 'proveedor_articulo' AND column_name = 'vencido') THEN
        RAISE EXCEPTION '12: proveedor_articulo es de una versión anterior del 12 (sin vencido). Borrá las tres tablas y volvé a aplicarlo.';
    END IF;
    IF (SELECT count(*) FROM pg_indexes WHERE schemaname = current_schema() AND indexname IN (
            'ux_scrape_corrida_en_curso', 'ix_proveedor_articulo_nombre_trgm',
            'ix_proveedor_articulo_codigo_barras', 'ix_pah_corrida')) <> 4 THEN
        RAISE EXCEPTION '12: faltan índices (se esperaban 4).';
    END IF;
-- >>> ROL n8n
    -- Exactamente estos permisos de tabla, ni uno más (DELETE, TRUNCATE, otras tablas).
    IF EXISTS (
        SELECT g.table_name, g.privilege_type FROM information_schema.role_table_grants g
        WHERE g.grantee = 'lacentral_n8n' AND g.table_schema = current_schema()
        EXCEPT
        SELECT t, p FROM unnest(ARRAY['scrape_corrida', 'proveedor_articulo', 'proveedor_articulo_historial']) t,
                         unnest(ARRAY['SELECT', 'INSERT', 'UPDATE']) p
        EXCEPT SELECT 'proveedor', 'SELECT') THEN
        RAISE EXCEPTION '12: lacentral_n8n tiene permisos de más.';
    END IF;
    IF (SELECT count(*) FROM information_schema.role_table_grants
        WHERE grantee = 'lacentral_n8n' AND table_schema = current_schema()) <> 10 THEN
        RAISE EXCEPTION '12: a lacentral_n8n le faltan permisos (se esperaban 10).';
    END IF;
-- <<< ROL n8n
    RAISE NOTICE '12: TODO OK.';
END $verif$;

COMMIT;
