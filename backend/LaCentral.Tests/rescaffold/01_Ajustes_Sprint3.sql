-- =============================================================================
-- La Central · 01_Ajustes_Sprint3.sql
-- Parche de esquema con las decisiones D8, D9 y D10 del 07/10/2026.
--
-- Se aplica sobre una base creada con base_completa.sql.
-- Todo el script corre dentro de UNA transacción: si algo falla, no queda
-- nada a medio aplicar. El DDL de PostgreSQL es transaccional.
--
-- NO es idempotente: los RENAME COLUMN y los DROP COLUMN fallan en una
-- segunda corrida. Eso es seguro — el error aborta la transacción y la base
-- queda como estaba — pero significa que se corre una sola vez por base.
--
-- Orden de aplicación:
--   1. lacentral_test  → verificar → correr la suite
--   2. base de desarrollo
--   3. re-scaffold de LaCentral.Data/Models (CRÍTICO 4)
--
-- Fuente de verdad: base_completa.sql + este script. Ver la nota final sobre
-- qué hacer con los dos archivos.
-- =============================================================================


-- -----------------------------------------------------------------------------
-- VERIFICACIÓN PREVIA · correr estas tres consultas ANTES del script
-- -----------------------------------------------------------------------------
-- 1 · Nombres reales de los CHECK de baja lógica. Si alguno no coincide con
--     los que este script usa, ajustar el DROP CONSTRAINT correspondiente.
--
--     SELECT  rel.relname        AS tabla,
--             con.conname        AS constraint_name,
--             pg_get_constraintdef(con.oid) AS definicion
--     FROM    pg_constraint con
--     JOIN    pg_class rel ON rel.oid = con.conrelid
--     WHERE   con.contype = 'c'
--       AND   con.conname LIKE '%baja%'
--     ORDER BY tabla;
--
-- 2 · Tipo exacto de las columnas de fecha de baja. Si son TIMESTAMP sin zona,
--     reemplazar TIMESTAMPTZ por TIMESTAMP en las tablas de historial.
--
--     SELECT  table_name, column_name, data_type
--     FROM    information_schema.columns
--     WHERE   column_name IN ('fecha_baja', 'fecha_alta')
--     ORDER BY table_name, column_name;
--
-- 3 · Cuántas filas de factura_compra hay, para saber si el backfill de D9
--     tiene algo que mover.
--
--     SELECT count(*) AS facturas, count(*) FILTER (WHERE estado = 'ACTIVA') AS activas
--     FROM   factura_compra;
-- -----------------------------------------------------------------------------


BEGIN;

SET LOCAL statement_timeout = '60s';


-- =============================================================================
-- D8 · Percepciones, no retenciones
--
-- El GR-01 lo dice en tres lugares coincidentes: el bloque OTROS CONCEPTOS de
-- APS-005, la observación del proceso y el manual de puesto de Matías Bossio
-- («transcripción e ingreso de percepciones impositivas (IVA, IIBB,
-- Municipalidad, Ganancias)», 1,5 hs/día).
--
-- En una factura DE COMPRA la percepción es la que el proveedor le adiciona a
-- La Central y suma a lo que hay que pagar. La retención sería la que La
-- Central le descuenta al proveedor al pagarle, y eso está fuera de alcance
-- junto con Cuentas a Pagar.
--
-- El RENAME conserva tipo, precisión, default y constraints. No hace falta
-- nada más.
-- =============================================================================

ALTER TABLE factura_compra RENAME COLUMN retencion_iva        TO percepcion_iva;
ALTER TABLE factura_compra RENAME COLUMN retencion_iibb       TO percepcion_iibb;
ALTER TABLE factura_compra RENAME COLUMN retencion_municipal  TO percepcion_municipal;
ALTER TABLE factura_compra RENAME COLUMN retencion_ganancias  TO percepcion_ganancias;

COMMENT ON COLUMN factura_compra.percepcion_iva IS
    'Percepción de IVA que el proveedor adiciona al comprobante. Dato transcripto '
    'del PDF: el sistema no calcula alícuotas ni liquida impuestos.';
COMMENT ON COLUMN factura_compra.percepcion_iibb IS
    'Percepción de Ingresos Brutos. Dato transcripto, sin cálculo por jurisdicción.';
COMMENT ON COLUMN factura_compra.percepcion_municipal IS
    'Percepción municipal. Dato transcripto.';
COMMENT ON COLUMN factura_compra.percepcion_ganancias IS
    'Percepción de Ganancias. Dato transcripto.';


-- =============================================================================
-- D9 · El destino de stock se carga por línea, no por comprobante
--
-- APS-005 tiene una columna «Dep.» de tres dígitos en la grilla de ítems y la
-- regla relevada dice que al confirmar «ingresa stock al depósito DE LA LÍNEA».
-- La observación del proceso lo confirma: «cargados todos los productos,
-- coloca SV o FR según si fue a sucursal o depósito».
--
-- Secuencia: agregar nullable → rellenar desde la cabecera → NOT NULL →
-- clave foránea → recién entonces eliminar la columna de la cabecera.
-- Hacerlo en otro orden pierde el dato de las facturas ya cargadas.
-- =============================================================================

ALTER TABLE factura_compra_detalle
    ADD COLUMN IF NOT EXISTS sucursal_id SMALLINT;

-- Backfill: cada línea hereda la sucursal que tenía su comprobante.
UPDATE factura_compra_detalle d
SET    sucursal_id = f.sucursal_id
FROM   factura_compra f
WHERE  f.id = d.factura_compra_id
  AND  d.sucursal_id IS NULL;

ALTER TABLE factura_compra_detalle
    ALTER COLUMN sucursal_id SET NOT NULL;

ALTER TABLE factura_compra_detalle
    ADD CONSTRAINT factura_compra_detalle_sucursal_id_fkey
    FOREIGN KEY (sucursal_id) REFERENCES sucursal(id);

CREATE INDEX IF NOT EXISTS ix_fc_detalle_sucursal
    ON factura_compra_detalle (sucursal_id);

COMMENT ON COLUMN factura_compra_detalle.sucursal_id IS
    'Destino de la mercadería de ESTA línea. Una factura puede repartirse entre '
    'Fragueiro y San Vicente, y el sistema anterior lo carga por línea (APS-005, '
    'columna «Dep.»). El stock se incrementa contra esta sucursal, no contra una '
    'del comprobante.';

-- La sucursal de la cabecera se elimina. Dónde se REGISTRÓ el comprobante sigue
-- siendo recuperable por factura_compra.usuario_id → usuario.sucursal_id, porque
-- un usuario representa un puesto de trabajo en una sucursal.
ALTER TABLE factura_compra
    DROP CONSTRAINT IF EXISTS factura_compra_sucursal_id_fkey;

ALTER TABLE factura_compra
    DROP COLUMN sucursal_id;


-- =============================================================================
-- D10 · Historial de baja en su propia tabla
--
-- El CHECK original exigía
--     (activo AND motivo_baja IS NULL AND fecha_baja IS NULL)
--   OR (NOT activo AND motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL)
--
-- y HU-CLI-05 CA-003 exige que el motivo de la baja anterior sobreviva a la
-- reactivación. Las dos cosas no pueden ser verdad en la misma fila: por eso
-- el test en verde afirmaba un estado que PostgreSQL rechaza con 23514.
--
-- Solución en dos partes:
--   (a) el CHECK pasa a restringir solo el lado inactivo;
--   (b) el historial va a una tabla 1:N, que además admite varias bajas y
--       reactivaciones de la misma entidad. La columna única solo podía
--       guardar la última: era un grupo repetitivo mal resuelto.
--
-- Las columnas motivo_baja y fecha_baja NO se eliminan: siguen describiendo
-- la baja vigente, que es lo que la ficha muestra. La tabla guarda el pasado.
-- =============================================================================

-- --- (a) Relajar los CHECK de coherencia -------------------------------------
-- Si algún nombre no coincide, usar la consulta 1 de la verificación previa.

ALTER TABLE cliente   DROP CONSTRAINT IF EXISTS cliente_baja_coherente;
ALTER TABLE proveedor DROP CONSTRAINT IF EXISTS proveedor_baja_coherente;
ALTER TABLE articulo  DROP CONSTRAINT IF EXISTS articulo_baja_coherente;
ALTER TABLE usuario   DROP CONSTRAINT IF EXISTS usuario_baja_coherente;

ALTER TABLE cliente ADD CONSTRAINT cliente_baja_coherente
    CHECK (activo OR (motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL));

ALTER TABLE proveedor ADD CONSTRAINT proveedor_baja_coherente
    CHECK (activo OR (motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL));

ALTER TABLE articulo ADD CONSTRAINT articulo_baja_coherente
    CHECK (activo OR (motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL));

ALTER TABLE usuario ADD CONSTRAINT usuario_baja_coherente
    CHECK (activo OR (motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL));

COMMENT ON CONSTRAINT cliente_baja_coherente ON cliente IS
    'Una fila inactiva exige motivo y fecha. Una fila activa PUEDE conservarlos: '
    'es el rastro de su última baja, que HU-CLI-05 CA-003 pide no perder al '
    'reactivar. El historial completo vive en cliente_historial_baja.';


-- --- (b) Tablas de historial -------------------------------------------------
-- Mismo patrón las cuatro. Nótese el índice único parcial: solo puede haber
-- UNA baja abierta (sin reactivación) por entidad a la vez — el mismo recurso
-- que ux_factura_compra_numero_activa.

CREATE TABLE cliente_historial_baja (
    id                      INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cliente_id              INTEGER     NOT NULL REFERENCES cliente(id) ON DELETE CASCADE,
    motivo                  TEXT        NOT NULL,
    fecha_baja              TIMESTAMPTZ NOT NULL,
    fecha_reactivacion      TIMESTAMPTZ,
    usuario_baja_id         INTEGER     REFERENCES usuario(id),
    usuario_reactivacion_id INTEGER     REFERENCES usuario(id),
    CONSTRAINT cliente_hist_fechas_coherentes
        CHECK (fecha_reactivacion IS NULL OR fecha_reactivacion >= fecha_baja),
    CONSTRAINT cliente_hist_motivo_no_vacio
        CHECK (length(btrim(motivo)) > 0)
);

CREATE INDEX ix_cliente_hist_baja
    ON cliente_historial_baja (cliente_id, fecha_baja DESC);

CREATE UNIQUE INDEX ux_cliente_hist_abierta
    ON cliente_historial_baja (cliente_id)
    WHERE fecha_reactivacion IS NULL;

COMMENT ON TABLE cliente_historial_baja IS
    'Una fila por cada baja. fecha_reactivacion NULL = baja vigente. El índice '
    'único parcial impide dos bajas abiertas simultáneas del mismo cliente.';


CREATE TABLE proveedor_historial_baja (
    id                      INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    proveedor_id            INTEGER     NOT NULL REFERENCES proveedor(id) ON DELETE CASCADE,
    motivo                  TEXT        NOT NULL,
    fecha_baja              TIMESTAMPTZ NOT NULL,
    fecha_reactivacion      TIMESTAMPTZ,
    usuario_baja_id         INTEGER     REFERENCES usuario(id),
    usuario_reactivacion_id INTEGER     REFERENCES usuario(id),
    CONSTRAINT proveedor_hist_fechas_coherentes
        CHECK (fecha_reactivacion IS NULL OR fecha_reactivacion >= fecha_baja),
    CONSTRAINT proveedor_hist_motivo_no_vacio
        CHECK (length(btrim(motivo)) > 0)
);

CREATE INDEX ix_proveedor_hist_baja
    ON proveedor_historial_baja (proveedor_id, fecha_baja DESC);

CREATE UNIQUE INDEX ux_proveedor_hist_abierta
    ON proveedor_historial_baja (proveedor_id)
    WHERE fecha_reactivacion IS NULL;


CREATE TABLE articulo_historial_baja (
    id                      INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    articulo_id             INTEGER     NOT NULL REFERENCES articulo(id) ON DELETE CASCADE,
    motivo                  TEXT        NOT NULL,
    fecha_baja              TIMESTAMPTZ NOT NULL,
    fecha_reactivacion      TIMESTAMPTZ,
    usuario_baja_id         INTEGER     REFERENCES usuario(id),
    usuario_reactivacion_id INTEGER     REFERENCES usuario(id),
    CONSTRAINT articulo_hist_fechas_coherentes
        CHECK (fecha_reactivacion IS NULL OR fecha_reactivacion >= fecha_baja),
    CONSTRAINT articulo_hist_motivo_no_vacio
        CHECK (length(btrim(motivo)) > 0)
);

CREATE INDEX ix_articulo_hist_baja
    ON articulo_historial_baja (articulo_id, fecha_baja DESC);

CREATE UNIQUE INDEX ux_articulo_hist_abierta
    ON articulo_historial_baja (articulo_id)
    WHERE fecha_reactivacion IS NULL;


CREATE TABLE usuario_historial_baja (
    id                      INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    usuario_id              INTEGER     NOT NULL REFERENCES usuario(id) ON DELETE CASCADE,
    motivo                  TEXT        NOT NULL,
    fecha_baja              TIMESTAMPTZ NOT NULL,
    fecha_reactivacion      TIMESTAMPTZ,
    usuario_baja_id         INTEGER     REFERENCES usuario(id),
    usuario_reactivacion_id INTEGER     REFERENCES usuario(id),
    CONSTRAINT usuario_hist_fechas_coherentes
        CHECK (fecha_reactivacion IS NULL OR fecha_reactivacion >= fecha_baja),
    CONSTRAINT usuario_hist_motivo_no_vacio
        CHECK (length(btrim(motivo)) > 0)
);

CREATE INDEX ix_usuario_hist_baja
    ON usuario_historial_baja (usuario_id, fecha_baja DESC);

CREATE UNIQUE INDEX ux_usuario_hist_abierta
    ON usuario_historial_baja (usuario_id)
    WHERE fecha_reactivacion IS NULL;


-- --- (c) Backfill del historial existente ------------------------------------
-- Las filas que hoy están inactivas tienen su motivo y fecha en la fila.
-- Se copian al historial como baja abierta. Las que están activas con motivo
-- cargado — si hubiera alguna por el bug que se está corrigiendo — se copian
-- como baja ya reactivada, con la fecha de reactivación desconocida en NULL...
-- salvo que eso violaría el índice único parcial si además hay una baja
-- abierta. Por eso se filtra solo por NOT activo: lo que está activo no tiene
-- baja vigente y su historial anterior no es reconstruible desde la fila.

INSERT INTO cliente_historial_baja (cliente_id, motivo, fecha_baja)
SELECT id, motivo_baja, fecha_baja
FROM   cliente
WHERE  NOT activo AND motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL;

INSERT INTO proveedor_historial_baja (proveedor_id, motivo, fecha_baja)
SELECT id, motivo_baja, fecha_baja
FROM   proveedor
WHERE  NOT activo AND motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL;

INSERT INTO articulo_historial_baja (articulo_id, motivo, fecha_baja)
SELECT id, motivo_baja, fecha_baja
FROM   articulo
WHERE  NOT activo AND motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL;

INSERT INTO usuario_historial_baja (usuario_id, motivo, fecha_baja)
SELECT id, motivo_baja, fecha_baja
FROM   usuario
WHERE  NOT activo AND motivo_baja IS NOT NULL AND fecha_baja IS NOT NULL;


-- =============================================================================
-- D7 · Moneda y cotización: NO se agregan columnas
--
-- Decisión del 07/10/2026: La Central cobra y paga 100% en pesos.
--
-- Queda asentado acá porque el relevamiento muestra lo contrario del lado del
-- sistema actual: APS-005 tiene cabecera con combo de moneda y cotización de
-- cuatro decimales editable, APS-002 convierte precios «usando la cotización
-- vigente», y el relevamiento presume tablas COTIZACIONES y MONEDAS. Un lector
-- que compare el GR-01 con el esquema va a notar la ausencia.
--
-- Por eso esto NO es una omisión: es una exclusión de alcance, y tiene que
-- estar escrita en el Mandato v5, no solo en este comentario.
-- =============================================================================

COMMENT ON TABLE factura_compra IS
    'Factura de compra de un proveedor. Importes en pesos argentinos: la empresa '
    'no opera en moneda extranjera (decisión D7, 07/10/2026). El sistema anterior '
    'admite moneda y cotización por comprobante y por línea (APS-005); acá queda '
    'excluido del alcance de forma deliberada. El destino de stock se carga por '
    'línea, no por comprobante (D9).';

ROLLBACK;

ALTER TABLE cliente ADD COLUMN IF NOT EXISTS usuario_alta_id INTEGER REFERENCES usuario(id);
ALTER TABLE cliente ADD COLUMN IF NOT EXISTS usuario_baja_id INTEGER REFERENCES usuario(id);

ALTER TABLE usuario ADD COLUMN IF NOT EXISTS usuario_alta_id INTEGER REFERENCES usuario(id);
ALTER TABLE usuario ADD COLUMN IF NOT EXISTS usuario_baja_id INTEGER REFERENCES usuario(id);


COMMIT;


-- =============================================================================
-- VERIFICACIÓN POSTERIOR · correr después del COMMIT
-- =============================================================================
-- 1 · Las cuatro columnas renombradas y la sucursal fuera de la cabecera:
--
    SELECT column_name FROM information_schema.columns
    WHERE  table_name = 'factura_compra' ORDER BY ordinal_position;
     -- se esperan percepcion_* y NINGUNA sucursal_id
--
-- 2 · El destino por línea, con su FK:
--
    SELECT column_name, is_nullable, data_type
    FROM   information_schema.columns
    WHERE  table_name = 'factura_compra_detalle' ORDER BY ordinal_position;
--
-- 3 · Que el estado que antes rompía ahora sea legal — ESTA es la prueba de D10:
--
     BEGIN;
     INSERT INTO cliente (codigo, razon_social, activo, motivo_baja, fecha_baja)
     VALUES ('ZZ-TEST-D10', 'Prueba D10', false, 'Cierre temporal', now());
--
     UPDATE cliente SET activo = true
     WHERE  codigo = 'ZZ-TEST-D10';
     -- Antes: 23514. Ahora tiene que pasar, conservando motivo y fecha.
--
     SELECT activo, motivo_baja, fecha_baja FROM cliente WHERE codigo = 'ZZ-TEST-D10';
     ROLLBACK;
--
-- 4 · Que el índice único parcial del historial haga su trabajo:
--
     BEGIN;
     INSERT INTO cliente (codigo, razon_social) VALUES ('ZZ-TEST-HIST', 'Prueba');
     INSERT INTO cliente_historial_baja (cliente_id, motivo, fecha_baja)
     SELECT id, 'primera', now() FROM cliente WHERE codigo = 'ZZ-TEST-HIST';
     INSERT INTO cliente_historial_baja (cliente_id, motivo, fecha_baja)
     SELECT id, 'segunda', now() FROM cliente WHERE codigo = 'ZZ-TEST-HIST';
     -- Se espera 23505 unique_violation en ux_cliente_hist_abierta
     ROLLBACK;
-- =============================================================================


-- =============================================================================
-- DOS COSAS QUE HAY QUE DECIDIR DESPUÉS DE CORRER ESTO
-- =============================================================================
-- 1 · ¿Cuál es la fuente de verdad del esquema?
--     Ahora hay dos archivos. O se regenera base_completa.sql incorporando
--     estos ajustes — y este script queda como registro histórico del cambio —
--     o se adopta una carpeta migraciones/ numerada y base_completa.sql pasa
--     a ser solo el punto de partida. Elegir una; tener dos fuentes sin
--     decidir cuál manda es cómo se rompe el Database First.
--
-- 2 · GENERATED ALWAYS AS IDENTITY se conserva, a propósito.
--     El error 428C9 de la prueba no es un problema del esquema: es la prueba
--     de que los tests seteaban Ids a mano y InMemory se lo permitía. Bajar a
--     GENERATED BY DEFAULT para que los tests pasen sería doblar el esquema
--     para acomodar los tests. Las tablas nuevas de historial también usan
--     ALWAYS por el mismo motivo.
-- =============================================================================
