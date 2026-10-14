-- =====================================================================================
--  04_Modalidades_Base_Calculo.sql
--  La Central · Sprint 3 · D1 · base de cálculo por modalidad
--
--  QUÉ HACE
--    Agrega articulo_precio_modalidad.base: sobre QUÉ se aplica el porcentaje.
--    Y corrige un CHECK del script 02 que era incompatible con D1.
--
--  POR QUÉ
--    D1 define las modalidades así:
--        Lista                  = costo × 1,50
--        Costo                  = costo × 1,00
--        EfectivoTransferencia  = LISTA × 0,90      <-- sobre Lista, no sobre el costo
--        Tarjeta3 / Tarjeta6    = tasa sin definir
--        CasoEspecial           = sin fórmula
--
--    El script 02 dejó una sola columna `porcentaje`, que asume que todas las
--    modalidades son un porcentaje sobre el costo. Efectivo no lo es. Guardar su
--    equivalente sobre el costo (35 %) funcionaría hasta el día en que cambie el
--    margen de Lista: Efectivo no se movería, porque la dependencia entre las dos
--    viviría solo en una fórmula del código y no en el dato.
--
--    Con `base`, la cadena queda en el esquema y el recálculo de la Fase 6 la puede
--    seguir sin que nadie se acuerde de que existe.
--
--  LA FÓRMULA, A PARTIR DE ACÁ
--        precio = valor_de_la_base × (1 + porcentaje / 100)
--
--    | Modalidad             | base          | porcentaje |
--    |-----------------------|---------------|------------|
--    | Lista                 | CostoArticulo | +50        |
--    | Costo                 | CostoArticulo | 0          |
--    | EfectivoTransferencia | PrecioLista   | -10        |
--    | Tarjeta3 / Tarjeta6   | NULL          | NULL       |
--    | CasoEspecial          | NULL          | NULL       |
--
--  DOS DECISIONES TÉCNICAS, PARA QUE NO SE RE-DISCUTAN
--    1. Los valores de `base` son 'CostoArticulo' y 'PrecioLista', no 'Costo' y
--       'Lista'. `Costo` y `Lista` son además nombres de MODALIDAD, y significan
--       otra cosa: la base CostoArticulo es el campo costo del artículo, no el
--       precio de la modalidad Costo. Que hoy coincidan (Costo = costo × 1,00) es
--       un accidente, no una identidad.
--    2. `porcentaje` ahora admite negativos, hasta -100. Efectivo es un DESCUENTO
--       sobre Lista: -10. El CHECK `porcentaje_no_negativo` del script 02 lo
--       prohibía: era un CHECK incompatible con D1 que nadie vio. Se reemplaza.
--       El piso -100 es el real: por debajo el precio sería negativo.
--
--  LO QUE ESTE SCRIPT NO DECIDE
--    La base de Tarjeta3 y Tarjeta6. Un recargo de tarjeta suele ir sobre el precio
--    de lista, pero D1 dice que las tasas están sin definir y que no se inventa un
--    número. Esas dos filas van con `base` y `porcentaje` en NULL hasta que la tasa
--    exista. El esquema lo permite y los CHECK lo respetan.
--
--  CÓMO SE EJECUTA
--    DBeaver: "Execute script" (Alt+X), NO "Execute statement".
--    Primero contra lacentral_test. Verificá. Después contra lacentral.
-- =====================================================================================


-- Limpia una transacción abortada de un intento anterior en la misma sesión.
ROLLBACK;


BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';


-- =====================================================================================
--  PARTE 1 · VERIFICACIÓN PREVIA
-- =====================================================================================

DO $verificacion$
DECLARE
    v_filas   bigint;
    v_checks  int;
BEGIN
    RAISE NOTICE 'Motor: PostgreSQL %. Base: %. Esquema: %.',
        current_setting('server_version'), current_database(), current_schema();

    -- 1.1 La tabla del script 02 tiene que existir
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = current_schema()
          AND table_name = 'articulo_precio_modalidad'
    ) THEN
        RAISE EXCEPTION
            'No se encontró articulo_precio_modalidad. Aplicá primero 02_Modalidades_Precios.sql.';
    END IF;

    -- 1.2 La columna no puede existir ya
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'articulo_precio_modalidad'
          AND column_name = 'base'
    ) THEN
        RAISE EXCEPTION
            'articulo_precio_modalidad.base ya existe. Este script ya se aplicó en el esquema %.',
            current_schema();
    END IF;

    -- 1.3 El CHECK que vamos a reemplazar tiene que estar
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = format('%I.articulo_precio_modalidad', current_schema())::regclass
          AND conname = 'articulo_precio_modalidad_porcentaje_no_negativo'
    ) THEN
        RAISE EXCEPTION
            'No se encontró el CHECK articulo_precio_modalidad_porcentaje_no_negativo. El esquema no es el que dejó el script 02: revisá antes de seguir.';
    END IF;

    SELECT count(*) INTO v_checks
    FROM pg_constraint
    WHERE conrelid = format('%I.articulo_precio_modalidad', current_schema())::regclass
      AND contype = 'c';

    IF v_checks <> 7 THEN
        RAISE EXCEPTION
            'Se esperaban los 7 CHECK del script 02 y hay %. Revisá antes de seguir.',
            v_checks;
    END IF;

    -- 1.4 COMPUERTA DE DATOS. Rellenar `base` en filas existentes exige decidir
    --     modalidad por modalidad, y eso no lo hace un script.
    EXECUTE format('SELECT count(*) FROM %I.articulo_precio_modalidad', current_schema())
        INTO v_filas;

    IF v_filas > 0 THEN
        RAISE EXCEPTION
            'articulo_precio_modalidad tiene % fila(s). Este script agrega una columna cuyo valor NO se puede deducir de lo que ya está guardado: hay que decidir la base de cada modalidad. Al 2026-10-14 la tabla estaba vacía y ningún código escribía en ella.',
            v_filas;
    END IF;
    RAISE NOTICE 'articulo_precio_modalidad está vacía: la columna entra sin backfill.';

    RAISE NOTICE 'Verificación previa superada.';
END $verificacion$;


-- =====================================================================================
--  PARTE 2 · LA COLUMNA
-- =====================================================================================

ALTER TABLE articulo_precio_modalidad
    ADD COLUMN base varchar(16) NULL;

ALTER TABLE articulo_precio_modalidad
    ADD CONSTRAINT articulo_precio_modalidad_base_valida
        CHECK (base IS NULL OR base IN ('CostoArticulo', 'PrecioLista'));

-- No hay porcentaje sin base, ni base sin porcentaje. Un porcentaje sin base es un
-- número sin significado, y una base sin porcentaje no calcula nada.
ALTER TABLE articulo_precio_modalidad
    ADD CONSTRAINT articulo_precio_modalidad_base_coherente
        CHECK ((porcentaje IS NULL) = (base IS NULL));

-- Lista no puede calcularse sobre sí misma.
ALTER TABLE articulo_precio_modalidad
    ADD CONSTRAINT articulo_precio_modalidad_base_sin_autorreferencia
        CHECK (NOT (modalidad = 'Lista' AND base = 'PrecioLista'));


-- =====================================================================================
--  PARTE 3 · EL CHECK INCOMPATIBLE CON D1
--  Efectivo = Lista × 0,90 es un porcentaje de -10. El CHECK del script 02 lo
--  prohibía. El piso pasa a -100, que es donde el precio se haría negativo.
--  El techo lo pone el tipo: numeric(6,2) llega hasta 9999,99.
-- =====================================================================================

ALTER TABLE articulo_precio_modalidad
    DROP CONSTRAINT articulo_precio_modalidad_porcentaje_no_negativo;

ALTER TABLE articulo_precio_modalidad
    ADD CONSTRAINT articulo_precio_modalidad_porcentaje_rango
        CHECK (porcentaje IS NULL OR porcentaje >= -100);


-- =====================================================================================
--  PARTE 4 · COMENTARIOS
--  La fórmula vive acá además de en el código. Un esquema que no explica su propia
--  regla obliga a leer el código para entender el dato.
-- =====================================================================================

COMMENT ON TABLE articulo_precio_modalidad IS
    'D1: precios de venta por articulo y modalidad. Formula: precio = valor_de_la_base * (1 + porcentaje/100). Lista = +50% sobre CostoArticulo. Costo = 0% sobre CostoArticulo. EfectivoTransferencia = -10% sobre PrecioLista. Tarjeta3/Tarjeta6: base y porcentaje NULL hasta que la tasa este definida. CasoEspecial: sin formula, precio manual. La unica regla de calculo del sistema vive en CalcularPreciosArticuloUseCase.';

COMMENT ON COLUMN articulo_precio_modalidad.base IS
    'Sobre que se aplica el porcentaje: CostoArticulo (el campo costo del articulo) o PrecioLista (el precio de la modalidad Lista del mismo articulo). NULL cuando no hay formula. OJO: CostoArticulo NO es la modalidad Costo.';

COMMENT ON COLUMN articulo_precio_modalidad.porcentaje IS
    'Porcentaje sobre el valor de `base`. Admite negativos hasta -100: EfectivoTransferencia es un descuento (-10) sobre PrecioLista. NULL si no hay formula (CasoEspecial) o la tasa no esta definida (Tarjeta3/Tarjeta6).';


-- =====================================================================================
--  PARTE 5 · VERIFICACIÓN POSTERIOR
-- =====================================================================================

DO $cierre$
DECLARE
    v_checks  int;
    v_detalle text;
BEGIN
    SELECT count(*) INTO v_checks
    FROM pg_constraint
    WHERE conrelid = format('%I.articulo_precio_modalidad', current_schema())::regclass
      AND contype = 'c';

    IF v_checks <> 10 THEN
        RAISE EXCEPTION 'Se esperaban 10 CHECK y hay %.', v_checks;
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = format('%I.articulo_precio_modalidad', current_schema())::regclass
          AND conname = 'articulo_precio_modalidad_porcentaje_no_negativo'
    ) THEN
        RAISE EXCEPTION 'El CHECK porcentaje_no_negativo sigue existiendo.';
    END IF;

    RAISE NOTICE 'articulo_precio_modalidad: 10 CHECK. La columna base entró y el CHECK de porcentaje se reemplazó.';

    FOR v_detalle IN
        SELECT format('  %s : %s', conname, pg_get_constraintdef(oid))
        FROM pg_constraint
        WHERE conrelid = format('%I.articulo_precio_modalidad', current_schema())::regclass
        ORDER BY contype, conname
    LOOP
        RAISE NOTICE 'Restriccion: %', v_detalle;
    END LOOP;
END $cierre$;

COMMIT;


-- =====================================================================================
--  DESPUÉS DE APLICAR
--
--  1. Correrlo en las DOS bases y confirmar que las dos quedan con 10 CHECK y la
--     columna `base`.
--  2. Re-scaffold, según backend/db/rescaffold/README.md. Comparar antes el esquema
--     de test contra el de producción, como dice ese README.
--  3. El modelo generado va a declarar `base` como string. La conversión al enum de
--     dominio va junto a la de Modalidad, en ModalidadPersistida, con el mismo
--     criterio: nombre exacto, y falla ante un valor fuera de los dos. Sin defaults.
-- =====================================================================================
