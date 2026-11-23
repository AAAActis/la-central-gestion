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
--    INSERT ... ON CONFLICT (proveedor_id, codigo) DO UPDATE, sin duplicar.
-- 3. proveedor_articulo_historial: precio y stock de cada fila en cada corrida (RF-016).
--    Una fila por (renglón, corrida).
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
--   esa tabla.
-- - precio_proveedor queda como está (último precio por ARTÍCULO propio, para la consulta
--   en vivo de la 031, la factura y la carga manual). El catálogo es por renglón del
--   proveedor, vinculado o no: no entra en una tabla con clave articulo_id.
--
-- POR QUÉ NO HAY TRIGGERS
-- PostgreSqlPruebas clona las tablas con LIKE ... INCLUDING ALL, que no copia triggers:
-- una regla en un trigger no se podría probar. Lo que la base no garantiza con
-- restricciones y permisos lo hace n8n con el SQL de la proforma de la 055 (§ Notas).
--
-- REQUISITO
-- El script 11 aplicado (nota_credito_compra existe). Si no, se corta sin tocar nada.
--
-- CÓMO SE APLICA
-- Una transacción. Es idempotente: cada paso se salta si ya está, y la verificación del
-- final corre siempre. Con el superusuario lacentral (crea un rol). Primero en
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
            nombre             VARCHAR(300)  NOT NULL COLLATE "es-AR-x-icu",
            descripcion        TEXT,
            detalle_tecnico    TEXT,
            marca              VARCHAR(120),
            precio_neto        NUMERIC(14,2),
            precio_lista       NUMERIC(14,2),
            stock              NUMERIC(14,2),
            alicuota_iva       NUMERIC(5,2),
            url_imagen         VARCHAR(1000),
            ultima_lectura     TIMESTAMPTZ   NOT NULL DEFAULT now(),
            fecha_origen       TIMESTAMPTZ,
            activo             BOOLEAN       NOT NULL DEFAULT TRUE,
            ultima_corrida_id  INTEGER       NOT NULL,
            CONSTRAINT proveedor_articulo_codigo_unico UNIQUE (proveedor_id, codigo),
            CONSTRAINT proveedor_articulo_id_proveedor_unico UNIQUE (id, proveedor_id),
            CONSTRAINT proveedor_articulo_corrida_mismo_proveedor FOREIGN KEY (ultima_corrida_id, proveedor_id)
                REFERENCES scrape_corrida (id, proveedor_id),
            CONSTRAINT proveedor_articulo_textos_no_vacios CHECK (btrim(codigo) <> '' AND btrim(nombre) <> ''),
            CONSTRAINT proveedor_articulo_precios_no_negativos CHECK (
                COALESCE(precio_neto, 0) >= 0 AND COALESCE(precio_lista, 0) >= 0),
            CONSTRAINT proveedor_articulo_alicuota_valida CHECK (alicuota_iva IS NULL OR alicuota_iva BETWEEN 0 AND 100)
        );
        -- Búsqueda por aproximación de la 053 y del comparador (030), como articulo.nombre.
        CREATE INDEX ix_proveedor_articulo_nombre_trgm ON proveedor_articulo USING gin (nombre gin_trgm_ops);
        CREATE INDEX ix_proveedor_articulo_codigo_barras ON proveedor_articulo (codigo_barras)
            WHERE codigo_barras IS NOT NULL;
        COMMENT ON TABLE proveedor_articulo IS
            'Catálogo actual de cada proveedor, una fila por código del proveedor, que n8n reingesta en cada corrida con INSERT ... ON CONFLICT (proveedor_id, codigo) DO UPDATE (D-S7-01, 23/11/2026). La vinculación con un artículo propio es articulo_codigo_alternativo (mismo proveedor_id y codigo): no apunta al id de esta fila, así que reingestar no la pierde. HU-CMP-08 (055).';
        COMMENT ON COLUMN proveedor_articulo.codigo IS
            'Código del artículo en el proveedor. Clave de la reingesta junto con proveedor_id.';
        COMMENT ON COLUMN proveedor_articulo.detalle_tecnico IS
            'Ficha técnica aplanada: «CLAVE: valor | CLAVE: valor».';
        COMMENT ON COLUMN proveedor_articulo.precio_neto IS
            'Precio neto, sin IVA. Desconocido es NULL, nunca cero.';
        COMMENT ON COLUMN proveedor_articulo.url_imagen IS
            'URL de la imagen en el sitio del proveedor. Las imágenes no se descargan.';
        COMMENT ON COLUMN proveedor_articulo.fecha_origen IS
            'Fecha que el sitio del proveedor da para el dato (actualización del precio), si la da.';
        COMMENT ON COLUMN proveedor_articulo.activo IS
            'FALSE = vencido: no vino en la última corrida SUCCESS del proveedor. No se borra: conserva su vinculación y su historial, y vuelve a TRUE si reaparece.';
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
            stock                 NUMERIC(14,2),
            fecha_lectura         TIMESTAMPTZ   NOT NULL DEFAULT now(),
            PRIMARY KEY (proveedor_articulo_id, corrida_id),
            CONSTRAINT pah_articulo_mismo_proveedor FOREIGN KEY (proveedor_articulo_id, proveedor_id)
                REFERENCES proveedor_articulo (id, proveedor_id),
            CONSTRAINT pah_corrida_mismo_proveedor FOREIGN KEY (corrida_id, proveedor_id)
                REFERENCES scrape_corrida (id, proveedor_id),
            CONSTRAINT pah_precios_no_negativos CHECK (
                COALESCE(precio_neto, 0) >= 0 AND COALESCE(precio_lista, 0) >= 0)
        );
        -- Para contar los ítems de una corrida al cerrarla.
        CREATE INDEX ix_pah_corrida ON proveedor_articulo_historial (corrida_id);
        COMMENT ON TABLE proveedor_articulo_historial IS
            'Precio y stock de cada renglón del catálogo en cada corrida (RF-016). Una fila por (renglón, corrida): una página reenviada dentro de la misma corrida no duplica. Las dos FK compuestas con proveedor_id impiden mezclar proveedores. HU-CMP-08 (055).';
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
            'pah_articulo_mismo_proveedor', 'pah_corrida_mismo_proveedor', 'pah_precios_no_negativos')) <> 14 THEN
        RAISE EXCEPTION '12: faltan restricciones (se esperaban 14).';
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
