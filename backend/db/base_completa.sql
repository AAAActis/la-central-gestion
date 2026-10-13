-- ════════════════════════════════════════════════════════════════
--  LA CENTRAL — Base de datos completa
--  PostgreSQL 18 · Versión 3.0 · 26/08/2026
-- ════════════════════════════════════════════════════════════════
--  Script único. Reemplaza a los archivos 01 a 06 anteriores.
--  Borra todo y reconstruye el esquema definitivo, ya con todos
--  los ajustes surgidos del relevamiento y de los datos reales.
--
--  SE EJECUTA EN TRES TANDAS:
--    PARTE A → esquema y tablas de staging
--    ►►► acá se importan los tres CSV con DBeaver ◄◄◄
--    PARTE B → control de la importación
--    PARTE C → transformación y conciliación
-- ════════════════════════════════════════════════════════════════


-- ████████████████████████████████████████████████████████████████
-- PARTE A — ESQUEMA COMPLETO
-- Seleccionar desde acá hasta el cartel de FIN DE LA PARTE A
-- ████████████████████████████████████████████████████████████████

DROP SCHEMA IF EXISTS staging CASCADE;
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;

CREATE EXTENSION IF NOT EXISTS pg_trgm;


-- ─────────────────────────────────────────────────
-- 1. ORGANIZACIÓN Y ACCESO   (Épica 8)
-- ─────────────────────────────────────────────────
CREATE TABLE sucursal (
    id      SMALLINT    PRIMARY KEY,
    codigo  CHAR(2)     NOT NULL UNIQUE,
    nombre  VARCHAR(60) NOT NULL COLLATE "es-AR-x-icu"
);
INSERT INTO sucursal VALUES
    (1,'FR','Fragueiro (Casa Central)'),
    (2,'SV','San Vicente (Sucursal)');

CREATE TABLE rol (
    id     SMALLINT    PRIMARY KEY,
    nombre VARCHAR(20) NOT NULL UNIQUE
);
INSERT INTO rol VALUES (1,'OPERADOR'), (2,'ADMINISTRADOR');

CREATE TABLE usuario (
    id              INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre_usuario  VARCHAR(30)  NOT NULL UNIQUE,
    hash_contrasena VARCHAR(255) NOT NULL,
    rol_id          SMALLINT     NOT NULL REFERENCES rol(id),
    sucursal_id     SMALLINT     NOT NULL REFERENCES sucursal(id),
    activo          BOOLEAN      NOT NULL DEFAULT TRUE,
    motivo_baja     TEXT,
    fecha_baja      TIMESTAMPTZ,
    fecha_alta      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    CONSTRAINT usuario_baja_coherente CHECK (
        (activo AND motivo_baja IS NULL AND fecha_baja IS NULL) OR
        (NOT activo AND motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL))
);
COMMENT ON TABLE usuario IS
  'Un usuario representa un puesto de trabajo, no una persona. La trazabilidad alcanza a la terminal desde la que se operó.';


-- ─────────────────────────────────────────────────
-- 2. CLIENTES   (Épica 1)
-- ─────────────────────────────────────────────────
CREATE TABLE cliente (
    id                     INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo                 VARCHAR(20)  NOT NULL UNIQUE,
    razon_social           VARCHAR(120) NOT NULL COLLATE "es-AR-x-icu",
    cuit_cuil              VARCHAR(13)  UNIQUE,
    cuit_simulado          BOOLEAN      NOT NULL DEFAULT FALSE,
    condicion_fiscal       VARCHAR(30),
    email                  VARCHAR(120),
    condicion_pago         VARCHAR(60),
    tiene_cuenta_corriente BOOLEAN      NOT NULL DEFAULT FALSE,
    activo                 BOOLEAN      NOT NULL DEFAULT TRUE,
    motivo_baja            TEXT,
    fecha_baja             TIMESTAMPTZ,
    fecha_alta             TIMESTAMPTZ  NOT NULL DEFAULT now(),
    CONSTRAINT cliente_baja_coherente CHECK (
        (activo AND motivo_baja IS NULL AND fecha_baja IS NULL) OR
        (NOT activo AND motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL)),
    CONSTRAINT cliente_cuit_formato CHECK (
        cuit_cuil IS NULL OR cuit_cuil ~ '^[0-9]{2}-[0-9]{8}-[0-9]$'),
    CONSTRAINT cliente_condicion_fiscal_valida CHECK (
        condicion_fiscal IS NULL OR condicion_fiscal IN
        ('RESPONSABLE_INSCRIPTO','MONOTRIBUTO','EXENTO','CONSUMIDOR_FINAL','NO_CATEGORIZADO'))
);
COMMENT ON COLUMN cliente.codigo IS
  'Clave de negocio, heredada de Multisoft. Reemplaza al CUIT como identificador único porque el sistema de origen no lo tiene cargado en la mayoría de los registros.';
COMMENT ON COLUMN cliente.cuit_simulado IS
  'Verdadero cuando el CUIT fue generado por el equipo. Los simulados usan un bloque de documento que arranca en 90.000.000, inexistente entre los documentos reales.';
COMMENT ON COLUMN cliente.tiene_cuenta_corriente IS
  'Dato informativo. El sistema NO lleva saldo ni deuda: Cuentas a Cobrar está fuera del alcance, en coherencia con la exclusión de Cuentas a Pagar.';

CREATE INDEX ix_cliente_razon_social      ON cliente (razon_social);
CREATE INDEX ix_cliente_razon_social_trgm ON cliente USING gin (razon_social gin_trgm_ops);

CREATE TABLE cliente_telefono (
    id          INTEGER     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cliente_id  INTEGER     NOT NULL REFERENCES cliente(id) ON DELETE CASCADE,
    numero      VARCHAR(60) NOT NULL,
    descripcion VARCHAR(60),
    UNIQUE (cliente_id, numero)
);

CREATE TABLE cliente_direccion (
    id            INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cliente_id    INTEGER      NOT NULL REFERENCES cliente(id) ON DELETE CASCADE,
    calle         VARCHAR(120) COLLATE "es-AR-x-icu",
    barrio        VARCHAR(80)  COLLATE "es-AR-x-icu",
    localidad     VARCHAR(80)  COLLATE "es-AR-x-icu",
    provincia     VARCHAR(60)  COLLATE "es-AR-x-icu",
    codigo_postal VARCHAR(10),
    descripcion   VARCHAR(40)
);


-- ─────────────────────────────────────────────────
-- 3. PROVEEDORES   (Épica 2)
-- ─────────────────────────────────────────────────
CREATE TABLE proveedor (
    id               INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo           VARCHAR(20)  NOT NULL UNIQUE,
    razon_social     VARCHAR(120) NOT NULL COLLATE "es-AR-x-icu",
    cuit             VARCHAR(13)  UNIQUE,
    cuit_simulado    BOOLEAN      NOT NULL DEFAULT FALSE,
    condicion_fiscal VARCHAR(30),
    email            VARCHAR(120),
    url_referencia   VARCHAR(300),
    activo           BOOLEAN      NOT NULL DEFAULT TRUE,
    motivo_baja      TEXT,
    fecha_baja       TIMESTAMPTZ,
    fecha_alta       TIMESTAMPTZ  NOT NULL DEFAULT now(),
    CONSTRAINT proveedor_baja_coherente CHECK (
        (activo AND motivo_baja IS NULL AND fecha_baja IS NULL) OR
        (NOT activo AND motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL)),
    CONSTRAINT proveedor_cuit_formato CHECK (
        cuit IS NULL OR cuit ~ '^[0-9]{2}-[0-9]{8}-[0-9]$'),
    CONSTRAINT proveedor_condicion_fiscal_valida CHECK (
        condicion_fiscal IS NULL OR condicion_fiscal IN
        ('RESPONSABLE_INSCRIPTO','MONOTRIBUTO','EXENTO','CONSUMIDOR_FINAL','NO_CATEGORIZADO'))
);
COMMENT ON COLUMN proveedor.url_referencia IS
  'Página de precios del proveedor, usada por el comparador (HU-CMP-05). Ningún proveedor la tiene cargada en el sistema de origen: hay que relevarla.';

CREATE INDEX ix_proveedor_razon_social      ON proveedor (razon_social);
CREATE INDEX ix_proveedor_razon_social_trgm ON proveedor USING gin (razon_social gin_trgm_ops);

CREATE TABLE proveedor_telefono (
    id           INTEGER     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    proveedor_id INTEGER     NOT NULL REFERENCES proveedor(id) ON DELETE CASCADE,
    numero       VARCHAR(60) NOT NULL,
    descripcion  VARCHAR(60),
    UNIQUE (proveedor_id, numero)
);

CREATE TABLE proveedor_direccion (
    id            INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    proveedor_id  INTEGER      NOT NULL REFERENCES proveedor(id) ON DELETE CASCADE,
    calle         VARCHAR(120) COLLATE "es-AR-x-icu",
    barrio        VARCHAR(80)  COLLATE "es-AR-x-icu",
    localidad     VARCHAR(80)  COLLATE "es-AR-x-icu",
    provincia     VARCHAR(60)  COLLATE "es-AR-x-icu",
    codigo_postal VARCHAR(10),
    descripcion   VARCHAR(40)
);

CREATE TABLE proveedor_credencial (
    proveedor_id    INTEGER      PRIMARY KEY REFERENCES proveedor(id) ON DELETE CASCADE,
    usuario         VARCHAR(120) NOT NULL,
    secreto_cifrado BYTEA        NOT NULL,
    vigente         BOOLEAN      NOT NULL DEFAULT TRUE,
    fecha_alta      TIMESTAMPTZ  NOT NULL DEFAULT now()
);
COMMENT ON COLUMN proveedor_credencial.secreto_cifrado IS
  'Contraseña cifrada por la aplicación. La base nunca almacena el valor en claro.';


-- ─────────────────────────────────────────────────
-- 4. ARTÍCULOS   (Épica 3)
-- ─────────────────────────────────────────────────
CREATE TABLE articulo (
    id                    INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo_interno        VARCHAR(40)   NOT NULL UNIQUE,
    nombre                VARCHAR(200)  NOT NULL COLLATE "es-AR-x-icu",
    precio_costo          NUMERIC(14,2),
    precio_venta_estimado NUMERIC(14,2),
    ultimo_proveedor_id   INTEGER       REFERENCES proveedor(id),
    ubicacion_deposito    VARCHAR(60),
    activo                BOOLEAN       NOT NULL DEFAULT TRUE,
    motivo_baja           TEXT,
    fecha_baja            TIMESTAMPTZ,
    fecha_alta            TIMESTAMPTZ   NOT NULL DEFAULT now(),
    CONSTRAINT articulo_baja_coherente CHECK (
        (activo AND motivo_baja IS NULL AND fecha_baja IS NULL) OR
        (NOT activo AND motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL)),
    CONSTRAINT articulo_precios_no_negativos CHECK (
        (precio_costo IS NULL OR precio_costo >= 0) AND
        (precio_venta_estimado IS NULL OR precio_venta_estimado >= 0))
);
COMMENT ON COLUMN articulo.precio_costo IS
  'DENORMALIZACIÓN DELIBERADA: valor derivado del último registro válido de articulo_historial_compra. Se mantiene acá para no recalcularlo en cada consulta del comparador.';
COMMENT ON COLUMN articulo.ubicacion_deposito IS
  'Posición física de referencia en el depósito, en texto libre. Dato informativo: el sistema no gestiona el depósito.';

CREATE INDEX ix_articulo_nombre      ON articulo (nombre);
CREATE INDEX ix_articulo_nombre_trgm ON articulo USING gin (nombre gin_trgm_ops);

CREATE TABLE articulo_margen (
    articulo_id INTEGER      NOT NULL REFERENCES articulo(id) ON DELETE CASCADE,
    numero      SMALLINT     NOT NULL,
    porcentaje  NUMERIC(6,2) NOT NULL,
    PRIMARY KEY (articulo_id, numero),
    CONSTRAINT margen_porcentaje_valido CHECK (porcentaje >= 0)
);
COMMENT ON TABLE articulo_margen IS
  'En La Central se usan tres márgenes: 50 %, 40 % y 0,01 %. El último representa precio de venta igual al costo, porque el sistema anterior no admite cargar cero.';

CREATE TABLE articulo_codigo_alternativo (
    id           INTEGER     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    articulo_id  INTEGER     NOT NULL REFERENCES articulo(id) ON DELETE CASCADE,
    proveedor_id INTEGER     NOT NULL REFERENCES proveedor(id),
    codigo       VARCHAR(60) NOT NULL,
    CONSTRAINT codigo_unico_global    UNIQUE (codigo),
    CONSTRAINT un_codigo_por_proveedor UNIQUE (articulo_id, proveedor_id)
);
CREATE INDEX ix_codigo_alt_articulo  ON articulo_codigo_alternativo (articulo_id);
CREATE INDEX ix_codigo_alt_proveedor ON articulo_codigo_alternativo (proveedor_id);

CREATE TABLE articulo_historial_compra (
    id                INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    articulo_id       INTEGER       NOT NULL REFERENCES articulo(id),
    proveedor_id      INTEGER       NOT NULL REFERENCES proveedor(id),
    factura_compra_id INTEGER       NOT NULL,
    precio_costo      NUMERIC(14,2) NOT NULL,
    fecha             TIMESTAMPTZ   NOT NULL,
    anulado           BOOLEAN       NOT NULL DEFAULT FALSE,
    CONSTRAINT historial_precio_no_negativo CHECK (precio_costo >= 0)
);
CREATE INDEX ix_historial_articulo ON articulo_historial_compra (articulo_id, fecha DESC)
    WHERE NOT anulado;

CREATE TABLE precio_proveedor (
    articulo_id       INTEGER       NOT NULL REFERENCES articulo(id) ON DELETE CASCADE,
    proveedor_id      INTEGER       NOT NULL REFERENCES proveedor(id) ON DELETE CASCADE,
    precio_lista      NUMERIC(14,2),
    precio_bonificado NUMERIC(14,2),
    fecha_obtencion   TIMESTAMPTZ   NOT NULL,
    origen            VARCHAR(20)   NOT NULL,
    PRIMARY KEY (articulo_id, proveedor_id),
    CONSTRAINT pp_origen_valido CHECK (
        origen IN ('CONSULTA_WEB','FACTURA_COMPRA','CARGA_MANUAL'))
);


-- ─────────────────────────────────────────────────
-- 5. STOCK   (Épica 4)
-- ─────────────────────────────────────────────────
CREATE TABLE stock (
    articulo_id INTEGER       NOT NULL REFERENCES articulo(id) ON DELETE CASCADE,
    sucursal_id SMALLINT      NOT NULL REFERENCES sucursal(id),
    cantidad    NUMERIC(12,3) NOT NULL DEFAULT 0,
    PRIMARY KEY (articulo_id, sucursal_id)
);
COMMENT ON TABLE stock IS
  'Existencia por artículo y ubicación. Admite valores negativos: la venta sin stock no se bloquea (HU-STK-03).';

CREATE TABLE transferencia (
    id                  INTEGER     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    sucursal_origen_id  SMALLINT    NOT NULL REFERENCES sucursal(id),
    sucursal_destino_id SMALLINT    NOT NULL REFERENCES sucursal(id),
    usuario_id          INTEGER     NOT NULL REFERENCES usuario(id),
    fecha               TIMESTAMPTZ NOT NULL DEFAULT now(),
    observaciones       TEXT,
    CONSTRAINT transferencia_origen_distinto_destino
        CHECK (sucursal_origen_id <> sucursal_destino_id)
);

CREATE TABLE transferencia_detalle (
    transferencia_id INTEGER       NOT NULL REFERENCES transferencia(id) ON DELETE CASCADE,
    articulo_id      INTEGER       NOT NULL REFERENCES articulo(id),
    cantidad         NUMERIC(12,3) NOT NULL,
    PRIMARY KEY (transferencia_id, articulo_id),
    CONSTRAINT transferencia_cantidad_positiva CHECK (cantidad > 0)
);


-- ─────────────────────────────────────────────────
-- 6. FACTURAS DE COMPRA   (Épica 6)
-- ─────────────────────────────────────────────────
CREATE TABLE factura_compra (
    id                   INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    proveedor_id         INTEGER       NOT NULL REFERENCES proveedor(id),
    numero               VARCHAR(30)   NOT NULL,
    fecha_emision        DATE          NOT NULL,
    fecha_vencimiento    DATE,
    sucursal_id          SMALLINT      NOT NULL REFERENCES sucursal(id),
    retencion_iva        NUMERIC(14,2) DEFAULT 0,
    retencion_iibb       NUMERIC(14,2) DEFAULT 0,
    retencion_municipal  NUMERIC(14,2) DEFAULT 0,
    retencion_ganancias  NUMERIC(14,2) DEFAULT 0,
    estado               VARCHAR(10)   NOT NULL DEFAULT 'ACTIVA',
    origen               VARCHAR(10)   NOT NULL DEFAULT 'MANUAL',
    usuario_id           INTEGER       NOT NULL REFERENCES usuario(id),
    fecha_registro       TIMESTAMPTZ   NOT NULL DEFAULT now(),
    motivo_anulacion     TEXT,
    fecha_anulacion      TIMESTAMPTZ,
    usuario_anulacion_id INTEGER       REFERENCES usuario(id),
    CONSTRAINT fc_estado_valido CHECK (estado IN ('ACTIVA','ANULADA')),
    CONSTRAINT fc_origen_valido CHECK (origen IN ('MANUAL','PDF')),
    CONSTRAINT fc_anulacion_coherente CHECK (
        (estado='ACTIVA'  AND motivo_anulacion IS NULL AND fecha_anulacion IS NULL) OR
        (estado='ANULADA' AND motivo_anulacion IS NOT NULL AND fecha_anulacion IS NOT NULL)),
    CONSTRAINT fc_vencimiento_posterior CHECK (
        fecha_vencimiento IS NULL OR fecha_vencimiento >= fecha_emision)
);
-- El número se libera al anularse: la unicidad rige solo entre facturas activas
CREATE UNIQUE INDEX ux_factura_compra_numero_activa
    ON factura_compra (proveedor_id, numero) WHERE estado = 'ACTIVA';

CREATE TABLE factura_compra_detalle (
    factura_compra_id    INTEGER       NOT NULL REFERENCES factura_compra(id) ON DELETE CASCADE,
    articulo_id          INTEGER       NOT NULL REFERENCES articulo(id),
    cantidad             NUMERIC(12,3) NOT NULL,
    precio_unitario      NUMERIC(14,2) NOT NULL,
    descuento_porcentaje NUMERIC(6,2)  NOT NULL DEFAULT 0,
    PRIMARY KEY (factura_compra_id, articulo_id),
    CONSTRAINT fcd_cantidad_positiva  CHECK (cantidad > 0),
    CONSTRAINT fcd_precio_no_negativo CHECK (precio_unitario >= 0),
    CONSTRAINT fcd_descuento_valido   CHECK (descuento_porcentaje BETWEEN 0 AND 100)
);

ALTER TABLE articulo_historial_compra
    ADD CONSTRAINT fk_historial_factura
    FOREIGN KEY (factura_compra_id) REFERENCES factura_compra(id);


-- ─────────────────────────────────────────────────
-- 7. FACTURAS DE VENTA   (Épica 5)
-- ─────────────────────────────────────────────────
CREATE TABLE factura_venta (
    id                   INTEGER     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cliente_id           INTEGER     NOT NULL REFERENCES cliente(id),
    numero               VARCHAR(30) NOT NULL,
    fecha                DATE        NOT NULL,
    sucursal_id          SMALLINT    NOT NULL REFERENCES sucursal(id),
    orden_compra         VARCHAR(40),
    movil                VARCHAR(40),
    estado               VARCHAR(10) NOT NULL DEFAULT 'ACTIVA',
    usuario_id           INTEGER     NOT NULL REFERENCES usuario(id),
    fecha_registro       TIMESTAMPTZ NOT NULL DEFAULT now(),
    motivo_anulacion     TEXT,
    fecha_anulacion      TIMESTAMPTZ,
    usuario_anulacion_id INTEGER     REFERENCES usuario(id),
    CONSTRAINT fv_estado_valido CHECK (estado IN ('ACTIVA','ANULADA')),
    CONSTRAINT fv_anulacion_coherente CHECK (
        (estado='ACTIVA'  AND motivo_anulacion IS NULL AND fecha_anulacion IS NULL) OR
        (estado='ANULADA' AND motivo_anulacion IS NOT NULL AND fecha_anulacion IS NOT NULL))
);
CREATE UNIQUE INDEX ux_factura_venta_numero_activa
    ON factura_venta (numero) WHERE estado = 'ACTIVA';

CREATE TABLE factura_venta_detalle (
    factura_venta_id INTEGER       NOT NULL REFERENCES factura_venta(id) ON DELETE CASCADE,
    articulo_id      INTEGER       NOT NULL REFERENCES articulo(id),
    cantidad         NUMERIC(12,3) NOT NULL,
    precio_unitario  NUMERIC(14,2) NOT NULL,
    PRIMARY KEY (factura_venta_id, articulo_id),
    CONSTRAINT fvd_cantidad_positiva  CHECK (cantidad > 0),
    CONSTRAINT fvd_precio_no_negativo CHECK (precio_unitario >= 0)
);


-- ─────────────────────────────────────────────────
-- 8. STAGING — espejo exacto de los tres CSV
-- ─────────────────────────────────────────────────
CREATE SCHEMA staging;

CREATE TABLE staging.proveedor_raw (
    codigo TEXT, razon_social TEXT, cuit TEXT, condicion_fiscal TEXT,
    direccion TEXT, localidad TEXT, provincia TEXT, codigo_postal TEXT,
    tel_fax TEXT, email TEXT, url_referencia TEXT, inactivo TEXT,
    cuit_simulado TEXT
);

CREATE TABLE staging.cliente_raw (
    codigo TEXT, razon_social TEXT, cuit_cuil TEXT, condicion_fiscal TEXT,
    barrio TEXT, direccion TEXT, localidad TEXT, provincia TEXT,
    codigo_postal TEXT, telefono TEXT, email TEXT, condicion_pago TEXT,
    cuenta_corriente TEXT, inactivo TEXT, cuit_simulado TEXT
);

CREATE TABLE staging.articulo_raw (
    codigo TEXT, descripcion TEXT, cantidad TEXT, ubicacion_deposito TEXT
);

-- Convierte cadena vacía en nulo, que es lo semánticamente correcto
CREATE FUNCTION staging.v(t TEXT) RETURNS TEXT AS $$
    SELECT NULLIF(btrim(t), '');
$$ LANGUAGE sql IMMUTABLE;


-- ████████████████████████████████████████████████████████████████
-- FIN DE LA PARTE A
-- ►►► AHORA IMPORTAR LOS TRES CSV CON DBEAVER ◄◄◄
-- ████████████████████████████████████████████████████████████████



-- ████████████████████████████████████████████████████████████████
-- PARTE B — CONTROL DE LA IMPORTACIÓN
-- Ejecutar esta única consulta con Ctrl+Enter
-- ████████████████████████████████████████████████████████████████

SELECT 'proveedor_raw' AS tabla, count(*) AS filas, 124 AS esperado FROM staging.proveedor_raw
UNION ALL SELECT 'cliente_raw',  count(*), 2108 FROM staging.cliente_raw
UNION ALL SELECT 'articulo_raw', count(*), 2644 FROM staging.articulo_raw;

-- Si algún número no coincide con el esperado, NO seguir:
-- vaciar la tabla con TRUNCATE y volver a importar ese CSV.



-- ████████████████████████████████████████████████████████████████
-- PARTE C — TRANSFORMACIÓN
-- Seleccionar desde el BEGIN hasta el COMMIT y ejecutar con Alt+X
-- ████████████████████████████████████████████████████████████████

BEGIN;

-- ─── PROVEEDORES ─────────────────────────────────
INSERT INTO proveedor (codigo, razon_social, cuit, cuit_simulado, condicion_fiscal,
                       email, url_referencia, activo, motivo_baja, fecha_baja)
SELECT staging.v(r.codigo), staging.v(r.razon_social), staging.v(r.cuit),
       COALESCE(staging.v(r.cuit_simulado) = 'S', FALSE),
       staging.v(r.condicion_fiscal), staging.v(r.email), staging.v(r.url_referencia),
       staging.v(r.inactivo) IS NULL,
       CASE WHEN staging.v(r.inactivo) IS NOT NULL
            THEN 'Marcado como inactivo en el sistema de origen (Multisoft).' END,
       CASE WHEN staging.v(r.inactivo) IS NOT NULL THEN now() END
  FROM staging.proveedor_raw r
 WHERE staging.v(r.codigo) IS NOT NULL AND staging.v(r.razon_social) IS NOT NULL;

INSERT INTO proveedor_direccion (proveedor_id, calle, localidad, provincia, codigo_postal, descripcion)
SELECT p.id, staging.v(r.direccion), staging.v(r.localidad),
       staging.v(r.provincia), staging.v(r.codigo_postal), 'Principal'
  FROM staging.proveedor_raw r JOIN proveedor p ON p.codigo = staging.v(r.codigo)
 WHERE staging.v(r.direccion) IS NOT NULL OR staging.v(r.localidad) IS NOT NULL
    OR staging.v(r.provincia) IS NOT NULL;

-- El origen mezcla teléfono y fax en un solo campo. No se trocea:
-- partirlo automáticamente inventaría una distinción que el origen no hace.
INSERT INTO proveedor_telefono (proveedor_id, numero, descripcion)
SELECT p.id, staging.v(r.tel_fax), 'Teléfono / fax (sin separar en el origen)'
  FROM staging.proveedor_raw r JOIN proveedor p ON p.codigo = staging.v(r.codigo)
 WHERE staging.v(r.tel_fax) IS NOT NULL
ON CONFLICT (proveedor_id, numero) DO NOTHING;

-- ─── CLIENTES ────────────────────────────────────
INSERT INTO cliente (codigo, razon_social, cuit_cuil, cuit_simulado, condicion_fiscal,
                     email, condicion_pago, tiene_cuenta_corriente,
                     activo, motivo_baja, fecha_baja)
SELECT staging.v(r.codigo), staging.v(r.razon_social), staging.v(r.cuit_cuil),
       COALESCE(staging.v(r.cuit_simulado) = 'S', FALSE),
       staging.v(r.condicion_fiscal), staging.v(r.email), staging.v(r.condicion_pago),
       COALESCE(staging.v(r.cuenta_corriente) = 'S', FALSE),
       staging.v(r.inactivo) IS NULL,
       CASE WHEN staging.v(r.inactivo) IS NOT NULL
            THEN 'Marcado como inactivo en el sistema de origen (Multisoft).' END,
       CASE WHEN staging.v(r.inactivo) IS NOT NULL THEN now() END
  FROM staging.cliente_raw r
 WHERE staging.v(r.codigo) IS NOT NULL AND staging.v(r.razon_social) IS NOT NULL;

INSERT INTO cliente_direccion (cliente_id, calle, barrio, localidad, provincia, codigo_postal, descripcion)
SELECT c.id, staging.v(r.direccion), staging.v(r.barrio), staging.v(r.localidad),
       staging.v(r.provincia), staging.v(r.codigo_postal), 'Principal'
  FROM staging.cliente_raw r JOIN cliente c ON c.codigo = staging.v(r.codigo)
 WHERE staging.v(r.direccion) IS NOT NULL OR staging.v(r.barrio) IS NOT NULL
    OR staging.v(r.localidad) IS NOT NULL OR staging.v(r.provincia) IS NOT NULL;

INSERT INTO cliente_telefono (cliente_id, numero, descripcion)
SELECT c.id, staging.v(r.telefono), 'Teléfono'
  FROM staging.cliente_raw r JOIN cliente c ON c.codigo = staging.v(r.codigo)
 WHERE staging.v(r.telefono) IS NOT NULL
ON CONFLICT (cliente_id, numero) DO NOTHING;

-- ─── ARTÍCULOS ───────────────────────────────────
INSERT INTO articulo (codigo_interno, nombre, ubicacion_deposito)
SELECT staging.v(r.codigo), staging.v(r.descripcion), staging.v(r.ubicacion_deposito)
  FROM staging.articulo_raw r
 WHERE staging.v(r.codigo) IS NOT NULL AND staging.v(r.descripcion) IS NOT NULL;

-- Stock: la cantidad del origen es el total y no discrimina sucursal.
-- Se asigna a Fragueiro y se inicializa San Vicente en cero.
-- ADVERTENCIA: el equipo declaró que este dato NO está actualizado.
INSERT INTO stock (articulo_id, sucursal_id, cantidad)
SELECT a.id, 1, COALESCE(staging.v(r.cantidad)::NUMERIC, 0)
  FROM staging.articulo_raw r JOIN articulo a ON a.codigo_interno = staging.v(r.codigo)
 WHERE staging.v(r.cantidad) ~ '^-?[0-9]+$'
ON CONFLICT DO NOTHING;

INSERT INTO stock (articulo_id, sucursal_id, cantidad)
SELECT a.id, s.id, 0 FROM articulo a CROSS JOIN sucursal s
ON CONFLICT DO NOTHING;

COMMIT;


-- ─────────────────────────────────────────────────
-- CONCILIACIÓN — ejecutar con Ctrl+Enter
-- ─────────────────────────────────────────────────
SELECT 'proveedores' AS entidad, count(*) AS total,
       count(*) FILTER (WHERE NOT cuit_simulado) AS cuit_real,
       count(*) FILTER (WHERE cuit_simulado)     AS cuit_simulado,
       count(*) FILTER (WHERE NOT activo)        AS inactivos FROM proveedor
UNION ALL
SELECT 'clientes', count(*),
       count(*) FILTER (WHERE NOT cuit_simulado),
       count(*) FILTER (WHERE cuit_simulado),
       count(*) FILTER (WHERE NOT activo) FROM cliente
UNION ALL
SELECT 'artículos', count(*), count(ubicacion_deposito), NULL,
       count(*) FILTER (WHERE NOT activo) FROM articulo
UNION ALL
SELECT 'registros de stock', count(*), NULL, NULL, NULL FROM stock;

-- ESPERADO
--   proveedores          124  ·  76 real  ·   48 simulado  ·   1 inactivo
--   clientes            2108  · 528 real  · 1580 simulado  · 279 inactivos
--   artículos           2644  · 105 con ubicación de depósito
--   registros de stock  5288  (2644 artículos × 2 sucursales)


-- ════════════════════════════════════════════════════════════════
-- LO QUE NO VIENE DEL SISTEMA DE ORIGEN
-- ════════════════════════════════════════════════════════════════
--  · articulo_codigo_alternativo → qué código le asigna cada proveedor
--    a cada artículo. Sostiene el comparador y el cotejo de facturas.
--  · precio_proveedor  → precio de cada proveedor.
--  · articulo_margen   → márgenes de venta (50 %, 40 % y 0,01 %).
--  · proveedor.url_referencia → ninguno de los 124 la tiene cargada.
-- Sin estos datos, las dos innovaciones solo se prueban con carga manual.
--
-- HALLAZGO DE CALIDAD DE DATOS
-- 19 CUIT del origen tienen dígito verificador incorrecto, entre ellos
-- 22-22222222-2 y varios con prefijo 00, que no existe. Se importan tal
-- como vienen: su estado es evidencia de que el sistema actual no valida
-- el CUIT al cargarlo. Registrarlo en la Matriz de Diagnóstico.

-- Una vez validada la importación:  DROP SCHEMA staging CASCADE;
