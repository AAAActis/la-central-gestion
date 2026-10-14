-- =====================================================================================
--  02_Modalidades_Precios.sql
--  La Central · Sprint 3 · Compuerta de esquema D1
--
--  QUÉ HACE
--    1. Crea articulo_precio_modalidad: precios de venta por artículo y modalidad,
--       con el porcentaje del que salieron y la fecha en que se estamparon.
--    2. Elimina articulo_margen, que indexaba por numero (las listas 1/2/3 que D1
--       vino a eliminar) y no tenía dónde guardar un precio.
--    3. Elimina articulo.precio_venta_estimado: con los precios por modalidad en la
--       tabla nueva, esa columna era una segunda fuente de verdad del mismo dato.
--
--  DECISIÓN DE DISEÑO (equipo, 14/10/2026)
--    Se persiste el porcentaje Y el precio calculado. El precio es un hecho que
--    pasó en una fecha, no solo una función del costo. Razones:
--      · D1 dice "precios por modalidad, persistidos por artículo";
--      · la Fase 6 recalcula y PERSISTE al confirmar una factura de compra;
--      · CasoEspecial (precio amigo) no sale de ninguna fórmula: sin una columna
--        de precio no tiene dónde vivir;
--      · una modalidad sin costo base o sin tasa configurada guarda precio NULL,
--        y la aplicación deriva la razón de la ausencia.
--
--  ALCANCE VERIFICADO
--    articulo_margen tiene 0 filas en lacentral y en lacentral_test (contado el
--    14/10/2026). Este script NO migra datos: si encuentra filas, ABORTA. Migrarlas
--    exige decidir a qué modalidad corresponde cada numero, y esa es una decisión
--    comercial que nadie tomó. Si alguna vez aparecen filas, va en otro script.
--
--  BASE DE DATOS FIRST
--    Este script es la fuente de verdad del esquema. Se aplica a mano y después se
--    re-scaffoldea. PROHIBIDO dotnet ef migrations.
--
--  CÓMO SE EJECUTA
--    DBeaver: "Execute script" (Alt+X), NO "Execute statement" (Ctrl+Enter).
--    Los bloques DO $$ ... $$ tienen que viajar enteros.
--    Primero contra lacentral_test. Verificá. Después contra lacentral.
--
--  TODO EN UNA TRANSACCIÓN. Si algo falla, no queda nada a medias.
-- =====================================================================================


-- Limpia una transacción abortada de un intento anterior en la misma sesión.
-- Si no hay ninguna abierta, PostgreSQL avisa "no transaction in progress" y sigue.
ROLLBACK;


BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';


-- =====================================================================================
--  PARTE 1 · VERIFICACIÓN PREVIA
--  El informe de la compuerta D1 salió del script SQL y del scaffold. Esta parte
--  comprueba contra el motor que la realidad coincide, y aborta si no.
-- =====================================================================================

DO $verificacion$
DECLARE
    v_cols_ok     int;
    v_cols_total  int;
    v_filas       bigint;
    v_detalle     text;
BEGIN
    -- 1.1 Versión del motor
    IF current_setting('server_version_num')::int < 180000 THEN
        RAISE EXCEPTION
            'Se esperaba PostgreSQL 18 o superior. Encontrado: %.',
            current_setting('server_version');
    END IF;
    RAISE NOTICE 'Motor: PostgreSQL %. Base: %. Esquema: %.',
        current_setting('server_version'), current_database(), current_schema();

    -- 1.2 Las tablas que referenciamos tienen que existir con la clave esperada
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'articulo' AND column_name = 'id'
    ) THEN
        RAISE EXCEPTION 'No se encontró articulo.id en el esquema %.', current_schema();
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'usuario' AND column_name = 'id'
    ) THEN
        RAISE EXCEPTION 'No se encontró usuario.id en el esquema %.', current_schema();
    END IF;

    -- 1.3 La tabla destino no puede existir ya: el script no es idempotente
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = current_schema()
          AND table_name = 'articulo_precio_modalidad'
    ) THEN
        RAISE EXCEPTION
            'articulo_precio_modalidad ya existe en el esquema %. Este script ya se aplicó acá.',
            current_schema();
    END IF;

    -- 1.4 articulo_margen tiene que tener EXACTAMENTE la forma del informe D1
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = current_schema() AND table_name = 'articulo_margen'
    ) THEN
        RAISE EXCEPTION 'No se encontró articulo_margen en el esquema %.', current_schema();
    END IF;

    SELECT count(*) INTO v_cols_total
    FROM information_schema.columns
    WHERE table_schema = current_schema() AND table_name = 'articulo_margen';

    SELECT count(*) INTO v_cols_ok
    FROM information_schema.columns
    WHERE table_schema = current_schema()
      AND table_name = 'articulo_margen'
      AND (
            (column_name = 'articulo_id' AND data_type = 'integer')
         OR (column_name = 'numero'      AND data_type = 'smallint')
         OR (column_name = 'porcentaje'  AND data_type = 'numeric'
             AND numeric_precision = 6 AND numeric_scale = 2)
      );

    IF v_cols_total <> 3 OR v_cols_ok <> 3 THEN
        RAISE EXCEPTION
            'articulo_margen no tiene la forma esperada (articulo_id integer, numero smallint, porcentaje numeric(6,2)). Columnas totales: %, coincidentes: %. Revisá el informe de la compuerta D1 antes de seguir.',
            v_cols_total, v_cols_ok;
    END IF;

    -- 1.5 COMPUERTA DE DATOS. Este script no migra: si hay filas, para.
    EXECUTE format('SELECT count(*) FROM %I.articulo_margen', current_schema())
        INTO v_filas;

    IF v_filas > 0 THEN
        RAISE EXCEPTION
            'articulo_margen tiene % fila(s) y este script NO migra datos. Se verificó que estaba vacía en lacentral y lacentral_test el 14/10/2026. Si aparecieron filas, hay que decidir a qué modalidad corresponde cada numero y escribir un script de migración aparte. No se descarta ninguna fila en silencio.',
            v_filas;
    END IF;
    RAISE NOTICE 'articulo_margen está vacía: nada que migrar.';

    -- 1.6 articulo.precio_venta_estimado tiene que existir, porque la PARTE 5 la borra
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'articulo' AND column_name = 'precio_venta_estimado'
    ) THEN
        RAISE EXCEPTION
            'No se encontró articulo.precio_venta_estimado. Si ya se eliminó, comentá su DROP en la PARTE 5 antes de volver a correr.';
    END IF;

    -- 1.7 Para comparar: tipos de las columnas de precio, costo y fecha que ya
    --     existen. Si tu convención difiere de numeric(12,2) y timestamptz,
    --     cambiá la PARTE 2 ANTES de aplicar, no después.
    FOR v_detalle IN
        SELECT format('  %s.%s -> %s',
                      c.relname, a.attname,
                      format_type(a.atttypid, a.atttypmod))
        FROM pg_class      c
        JOIN pg_namespace  n ON n.oid = c.relnamespace
        JOIN pg_attribute  a ON a.attrelid = c.oid
        WHERE n.nspname = current_schema()
          AND c.relkind = 'r'
          AND a.attnum > 0
          AND NOT a.attisdropped
          AND (a.attname LIKE '%precio%'
            OR a.attname LIKE '%costo%'
            OR a.attname IN ('fecha_baja', 'fecha_reactivacion'))
        ORDER BY c.relname, a.attname
    LOOP
        RAISE NOTICE 'Tipo existente: %', v_detalle;
    END LOOP;

    RAISE NOTICE 'Verificación previa superada.';
END $verificacion$;


-- =====================================================================================
--  PARTE 2 · LA TABLA
--
--  La modalidad es TEXTO con CHECK, no un id numérico ni una tabla de lookup.
--  Un id numérico reintroduce exactamente las listas 1/2/3 que D1 eliminó, y una
--  tabla de lookup obliga al enum de C# a espejar ids que pueden desincronizarse.
--  Los seis valores son los nombres EXACTOS de los miembros del enum
--  LaCentral.UseCases.Entidades.Modalidad: la conversión es nombre a nombre.
-- =====================================================================================

CREATE TABLE articulo_precio_modalidad (
    articulo_id     integer        NOT NULL,
    modalidad       varchar(24)    NOT NULL,

    -- El porcentaje del que salió el precio. NULL cuando no hay fórmula
    -- (CasoEspecial) o cuando la tasa todavía no está definida (Tarjeta3/Tarjeta6).
    porcentaje      numeric(6,2)       NULL,

    -- El precio estampado. NULL = no se pudo calcular. NUNCA cero: un cero es un
    -- precio válido y nadie lo nota hasta que alguien vende.
    precio          numeric(12,2)      NULL,

    -- true = lo fijó una persona y el recálculo automático NO lo pisa.
    -- La Fase 6 filtra por calculo_manual = false. No es una excepción en el
    -- código, es una condición en el WHERE.
    calculo_manual  boolean        NOT NULL DEFAULT false,

    -- Cuándo se estampó el precio. Es lo que lo convierte en un hecho con fecha
    -- en lugar de un valor derivado.
    fecha_calculo   timestamptz        NULL,

    -- Quién fijó un precio manual. Mismo criterio que D10: el rastro dice QUIÉN.
    usuario_id      integer            NULL,

    CONSTRAINT articulo_precio_modalidad_pkey
        PRIMARY KEY (articulo_id, modalidad),

    CONSTRAINT articulo_precio_modalidad_articulo_fkey
        FOREIGN KEY (articulo_id) REFERENCES articulo (id) ON DELETE CASCADE,

    CONSTRAINT articulo_precio_modalidad_usuario_fkey
        FOREIGN KEY (usuario_id) REFERENCES usuario (id),

    -- Los seis valores de D1. Nada más entra.
    CONSTRAINT articulo_precio_modalidad_valida
        CHECK (modalidad IN ('Lista', 'Tarjeta3', 'Tarjeta6',
                             'EfectivoTransferencia', 'Costo', 'CasoEspecial')),

    -- Un precio fijado a mano no salió de una fórmula.
    CONSTRAINT articulo_precio_modalidad_manual_sin_porcentaje
        CHECK (NOT calculo_manual OR porcentaje IS NULL),

    -- Un precio calculado tiene que decir de qué porcentaje salió. Sin esto el
    -- precio es un número sin origen y la auditoría no se puede reconstruir.
    CONSTRAINT articulo_precio_modalidad_calculado_con_porcentaje
        CHECK (calculo_manual OR precio IS NULL OR porcentaje IS NOT NULL),

    -- Si es un hecho, pasó en un momento.
    CONSTRAINT articulo_precio_modalidad_precio_con_fecha
        CHECK (precio IS NULL OR fecha_calculo IS NOT NULL),

    -- Un precio amigo sin quién lo autorizó no sirve como rastro.
    CONSTRAINT articulo_precio_modalidad_manual_con_usuario
        CHECK (NOT calculo_manual OR precio IS NULL OR usuario_id IS NOT NULL),

    CONSTRAINT articulo_precio_modalidad_porcentaje_no_negativo
        CHECK (porcentaje IS NULL OR porcentaje >= 0),

    CONSTRAINT articulo_precio_modalidad_precio_no_negativo
        CHECK (precio IS NULL OR precio >= 0)
);


-- =====================================================================================
--  PARTE 3 · COMENTARIOS
--  El comentario de articulo_margen todavía hablaba del 0,01 %, una regla que ya
--  no existe. Un comentario desactualizado miente con la autoridad del esquema.
-- =====================================================================================

COMMENT ON TABLE articulo_precio_modalidad IS
    'D1: precios de venta por articulo y modalidad, con el porcentaje del que salieron y la fecha en que se estamparon. Reemplaza a articulo_margen. La unica regla de calculo del sistema vive en CalcularPreciosArticuloUseCase.';

COMMENT ON COLUMN articulo_precio_modalidad.modalidad IS
    'Nombre exacto del miembro del enum Modalidad. Seis valores, sin id numerico.';
COMMENT ON COLUMN articulo_precio_modalidad.porcentaje IS
    'Porcentaje sobre el costo. NULL si no hay formula (CasoEspecial) o la tasa no esta definida (Tarjeta3/Tarjeta6).';
COMMENT ON COLUMN articulo_precio_modalidad.precio IS
    'Precio estampado. NULL cuando no se pudo calcular. Nunca cero por defecto.';
COMMENT ON COLUMN articulo_precio_modalidad.calculo_manual IS
    'true = lo fijo una persona. El recalculo automatico de la Fase 6 filtra por false y no lo pisa.';
COMMENT ON COLUMN articulo_precio_modalidad.fecha_calculo IS
    'Cuando se estampo este precio.';
COMMENT ON COLUMN articulo_precio_modalidad.usuario_id IS
    'Quien fijo el precio manual. NULL en los calculados.';


-- =====================================================================================
--  PARTE 4 · LO QUE SE ELIMINA
--
--  Ningún código lee articulo_margen ni articulo.precio_venta_estimado (verificado
--  en el informe de la compuerta D1). Ese es el momento más barato que vamos a
--  tener para sacarlas.
--
--  Sin CASCADE a propósito: si una vista o una restricción dependiera de ellas, el
--  DROP tiene que fallar y avisar, no arrastrarse lo que encuentre.
-- =====================================================================================

DROP TABLE articulo_margen;

ALTER TABLE articulo DROP COLUMN precio_venta_estimado;


-- =====================================================================================
--  PARTE 5 · VERIFICACIÓN POSTERIOR
-- =====================================================================================

DO $cierre$
DECLARE
    v_checks  int;
    v_detalle text;
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = current_schema() AND table_name = 'articulo_margen'
    ) THEN
        RAISE EXCEPTION 'articulo_margen sigue existiendo.';
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'articulo' AND column_name = 'precio_venta_estimado'
    ) THEN
        RAISE EXCEPTION 'articulo.precio_venta_estimado sigue existiendo.';
    END IF;

    -- contype = 'c' son los CHECK. En PostgreSQL 18 los NOT NULL son contype = 'n'
    -- y no entran en este conteo, que es lo que queremos.
    SELECT count(*) INTO v_checks
    FROM pg_constraint
    WHERE conrelid = format('%I.articulo_precio_modalidad', current_schema())::regclass
      AND contype = 'c';

    IF v_checks < 7 THEN
        RAISE EXCEPTION 'Se esperaban al menos 7 CHECK y hay %.', v_checks;
    END IF;

    RAISE NOTICE 'articulo_precio_modalidad creada con % CHECK.', v_checks;
    RAISE NOTICE 'articulo_margen y articulo.precio_venta_estimado eliminadas.';

    FOR v_detalle IN
        SELECT format('  %s %s %s',
                      a.attname,
                      format_type(a.atttypid, a.atttypmod),
                      CASE WHEN a.attnotnull THEN 'NOT NULL' ELSE 'NULL' END)
        FROM pg_attribute a
        WHERE a.attrelid = format('%I.articulo_precio_modalidad', current_schema())::regclass
          AND a.attnum > 0
          AND NOT a.attisdropped
        ORDER BY a.attnum
    LOOP
        RAISE NOTICE 'Columna: %', v_detalle;
    END LOOP;
END $cierre$;

COMMIT;


-- =====================================================================================
--  DESPUÉS DE APLICAR
--
--  1. Verificá a mano, con una consulta de solo lectura:
--
--     SELECT conname, pg_get_constraintdef(oid)
--     FROM   pg_constraint
--     WHERE  conrelid = 'articulo_precio_modalidad'::regclass
--     ORDER  BY contype, conname;
--
--  2. Re-scaffold de LaCentral.Data/Models/. El scaffold va a generar
--     ArticuloPrecioModalidad con `string Modalidad` y va a borrar
--     ArticuloMargen.cs. Las dos cosas están bien.
--
--  3. La conversión texto ↔ enum Modalidad NO va en Data/Models/, que es generado.
--     Va en LaCentralDbContext.Personalizaciones.cs como value converter, o en el
--     mapeo del repositorio. Lo decide el equipo, pero la entidad de dominio
--     Articulo sigue hablando de Modalidad, nunca de string: ese es el límite del
--     Core-Driven y no se cruza.
--
--  4. Una conversión que acepte un valor fuera de los seis tiene que FALLAR, no
--     devolver un default. Un enum con default silencioso es el mismo bug que un
--     precio en cero.
-- =====================================================================================
