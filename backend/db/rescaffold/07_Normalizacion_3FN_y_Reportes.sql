-- =====================================================================================
--  07_Normalizacion_3FN_y_Reportes.sql  (llegó como 05; renumerado el 04/11/2026: 05 es 05_rol_tests.sql y el 06 está reservado para C2)
--  La Central · 02/11/2026 · Actis · Billarroel · Bossio
--
--  QUÉ HACE
--    Dos cosas, en una sola corrida y una sola transacción:
--
--    PARTE A · CINCO de los seis cambios del Camino A (§11 del capítulo de
--              Normalización): N-3, N-5, N-7, N-9 y N-11.
--              N-6 salió del script el 04/11/2026 al medir los valores reales de
--              `condicion_pago`: ver la §A.4. Falta una decisión de negocio.
--
--    PARTE B · las columnas que pidió la Dirección el 02/11/2026 y sin las cuales
--              seis de los reportes no se pueden construir.
--
--    Van juntas a propósito. N-9 mueve `ubicacion_deposito` a `stock` y la Parte B
--    le agrega `minimo` a la misma tabla: partirlo en dos scripts serían dos
--    re-scaffold y dos ventanas de esquema inconsistente, sin ninguna ventaja.
--
--  POR QUÉ 3FN Y NO 5FN
--    El esquema construido está hoy en 2FN. Lo que lo frena de 3FN son estos seis
--    hallazgos, y nada más. BCNF, 4FN y 5FN se cumplen solas una vez aplicados: no
--    queda ningún determinante que no sea clave candidata, ninguna dependencia
--    multivaluada no trivial y ninguna dependencia de reunión.
--
--    N-1 y N-2 (`articulo.precio_costo` y `articulo.ultimo_proveedor_id`, derivados
--    del último `articulo_historial_compra`) NO se corrigen: son denormalizaciones
--    deliberadas, están declaradas y se conservan por rendimiento del comparador.
--    Por eso el capítulo dice «3FN con dos denormalizaciones declaradas», no «3FN».
--
--  LO QUE ESTE SCRIPT NO DECIDE
--    1. El dominio de `cliente.condicion_pago` (N-6). SALIÓ de este script. Los
--       valores reales medidos en la base no son los que inferí del export, y la
--       lista que tenía escrita habría borrado 384 clientes de flotas de taxi.
--       Detalle completo en la §A.4. Va en su propio script.
--    2. `localidad` NO se convierte en tabla, y es una corrección a lo que yo mismo
--       escribí en la §11. Medí 6 violaciones de `Localidad -> Provincia` en los
--       2.108 clientes y son homónimos legítimos: hay más de una «Villa María» en
--       más de una provincia. Una tabla `localidad` con clave ajena a `provincia`
--       afirmaría una dependencia funcional que el dominio no tiene. Queda como
--       texto libre. `provincia` sí: 21 valores distintos, 0 violaciones.
--    3. N-8 (`codigo_unico_global`) no entra. Es un cambio de clave natural en
--       `articulo_codigo_alternativo` y toca el caso de uso de vinculación: va con
--       su propia historia de usuario, no de prepo acá.
--    4. N-4 y N-10 no son correcciones de esquema. N-4 hay que verificarlo contra
--       el caso de uso; N-10 es una propiedad de la instancia, no del esquema.
--
--  CÓMO SE EJECUTA
--    DBeaver: "Execute script" (Alt+X), NO "Execute statement".
--    Primero contra lacentral_test. Verificar. Después contra lacentral.
--
--    El script ABORTA SOLO si algo no está como espera. La compuerta de la §0 corre
--    ANTES de tocar nada: si un nombre de columna no coincide, falla ahí y la base
--    queda intacta. No hay estado intermedio posible.
--
--    NO ES RE-EJECUTABLE, y es a propósito. Es una migración de una sola vez: si lo
--    corrés sobre una base ya migrada, la compuerta lo rechaza porque las columnas
--    que espera encontrar ya no están. Eso es la protección funcionando, no un error
--    del script.
--
--    SI FALLA, LEÉ EL PRIMER ERROR Y NADA MÁS. Después del primero, DBeaver sigue
--    mandando sentencias contra una transacción ya abortada y escupe veinte líneas
--    de «current transaction is aborted». Son ruido: la causa real está en la
--    primera. La base no cambió.
--
--  PROBADO
--    Corrido contra PostgreSQL 16 sobre un esquema mínimo que reproduce el estado
--    previo, con los casos borde del origen cargados a mano: «CUENTADO PENDIENTE»,
--    «Córdoba » con acento y espacio, el centinela «< Prov.NO Registrada», dos
--    localidades homónimas en provincias distintas, y un artículo sin ubicación.
--    Las ocho afirmaciones que el script hace se verificaron una por una contra la
--    base migrada (la lista está al final, en «Tests que hay que agregar»).
-- =====================================================================================


-- Limpia una transacción abortada de un intento anterior en la misma sesión.
ROLLBACK;

BEGIN;


-- =====================================================================================
--  0 · COMPUERTA PREVIA
--      Verifica que el esquema sea el que este script espera, antes de modificar nada.
-- =====================================================================================

DO $compuerta$
DECLARE
    v_esperado  TEXT[] := ARRAY[
        'articulo.id',
        'articulo.ubicacion_deposito',
        'articulo_historial_compra.factura_compra_id',
        'articulo_historial_compra.proveedor_id',
        'cliente.condicion_pago',   -- sólo se verifica que exista; N-6 salió del script
        'cliente_direccion.provincia',
        'cliente_direccion.localidad',
        'proveedor_direccion.provincia',
        'factura_compra.id',
        'factura_compra.proveedor_id',
        'factura_compra_detalle.factura_compra_id',
        'factura_compra_detalle.articulo_id',
        'factura_compra_detalle.sucursal_id',
        'factura_venta.id',
        'stock.articulo_id',
        'stock.sucursal_id',
        'stock.cantidad',
        'sucursal.id',
        'usuario.id'
    ];
    v_item      TEXT;
    v_falta     TEXT := '';
    v_dup       BIGINT;
    v_inco      BIGINT;
BEGIN
    -- 0.1 · Todas las columnas que el script va a leer o modificar tienen que existir.
    FOREACH v_item IN ARRAY v_esperado LOOP
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE  table_schema = current_schema()
              AND  table_name   = split_part(v_item, '.', 1)
              AND  column_name  = split_part(v_item, '.', 2)
        ) THEN
            v_falta := v_falta || E'\n  falta  ' || v_item;
        END IF;
    END LOOP;

    IF v_falta <> '' THEN
        RAISE EXCEPTION
            'El esquema no es el que este script espera. No se modificó nada.%', v_falta;
    END IF;

    -- 0.2 · N-7 · ¿hay ya filas que la clave nueva rechazaría?
    --       No puede haber dos filas con (factura, artículo, sucursal) repetido.
    SELECT count(*) INTO v_dup FROM (
        SELECT 1
        FROM   factura_compra_detalle
        GROUP  BY factura_compra_id, articulo_id, sucursal_id
        HAVING count(*) > 1
    ) d;

    IF v_dup > 0 THEN
        RAISE EXCEPTION
            'N-7: hay % combinaciones (factura, articulo, sucursal) repetidas. '
            'La clave nueva las rechazaría. Resolvelas antes de correr el script.', v_dup;
    END IF;

    -- 0.3 · N-3 · ¿hay historiales cuyo proveedor no sea el de su propia factura?
    --       Es exactamente lo que la clave ajena compuesta va a impedir de acá en más.
    SELECT count(*) INTO v_inco
    FROM   articulo_historial_compra h
    JOIN   factura_compra f ON f.id = h.factura_compra_id
    WHERE  h.proveedor_id IS DISTINCT FROM f.proveedor_id;

    IF v_inco > 0 THEN
        RAISE EXCEPTION
            'N-3: hay % filas de articulo_historial_compra cuyo proveedor_id no coincide '
            'con el de su factura. La clave ajena compuesta las rechazaría. '
            'Corregilas antes (o decidí cuál de los dos valores vale).', v_inco;
    END IF;

    RAISE NOTICE 'Compuerta previa: OK. El esquema es el esperado y los datos admiten los cambios.';
END $compuerta$;


-- =====================================================================================
--  PARTE A · LOS SEIS CAMBIOS DE NORMALIZACIÓN
-- =====================================================================================


-- -------------------------------------------------------------------------------------
--  A.1 · N-7 · La clave primaria de factura_compra_detalle
--
--  EL MÁS SERIO, y el único que arregla un defecto funcional y no sólo una forma
--  normal. D9 agregó `sucursal_id` por línea con este fundamento textual:
--  «Una factura puede repartirse entre Fragueiro y San Vicente». Pero la clave
--  siguió siendo (factura_compra_id, articulo_id), que PROHÍBE justamente eso.
--
--  El caso que hoy falla: se piden 20 bujías del mismo código, 10 para cada
--  sucursal. Son dos líneas con el mismo artículo y la misma factura, y el insert
--  de la segunda viola la clave. La regla de negocio y la clave se contradicen.
--
--  El determinante real es (factura, artículo, sucursal).
-- -------------------------------------------------------------------------------------

ALTER TABLE factura_compra_detalle
    DROP CONSTRAINT factura_compra_detalle_pkey;

ALTER TABLE factura_compra_detalle
    ADD CONSTRAINT factura_compra_detalle_pkey
    PRIMARY KEY (factura_compra_id, articulo_id, sucursal_id);

COMMENT ON CONSTRAINT factura_compra_detalle_pkey ON factura_compra_detalle IS
    'Incluye sucursal_id porque una factura se reparte entre sucursales (D9): el '
    'mismo artículo puede venir en dos líneas con destino distinto. La clave '
    'anterior, (factura_compra_id, articulo_id), lo prohibía y contradecía la regla '
    'relevada. Hallazgo N-7 del capítulo de Normalización.';


-- -------------------------------------------------------------------------------------
--  A.2 · N-3 · La clave ajena compuesta de articulo_historial_compra
--
--  La fila ya tiene `factura_compra_id`, y `factura_compra.proveedor_id` determina
--  el proveedor: la cadena es  id -> factura -> proveedor. Pero no había ninguna
--  restricción que atara las dos columnas, así que se podía insertar un historial
--  cuyo proveedor no fuera el de su propia factura. Nada lo impedía.
--
--  La clave ajena compuesta necesita un UNIQUE en el lado referenciado. El UNIQUE
--  (id, proveedor_id) sobre la cabecera es redundante en términos lógicos —`id` ya
--  es clave— pero es el requisito de PostgreSQL para que una FK compuesta lo
--  referencie, y no afirma nada falso.
-- -------------------------------------------------------------------------------------

ALTER TABLE factura_compra
    ADD CONSTRAINT factura_compra_id_proveedor_unico UNIQUE (id, proveedor_id);

COMMENT ON CONSTRAINT factura_compra_id_proveedor_unico ON factura_compra IS
    'Redundante en lo lógico (id ya es clave primaria). Existe porque PostgreSQL '
    'exige un UNIQUE en el lado referenciado para admitir la clave ajena compuesta '
    'de articulo_historial_compra. Hallazgo N-3.';

ALTER TABLE articulo_historial_compra
    ADD CONSTRAINT articulo_historial_compra_factura_proveedor_fkey
    FOREIGN KEY (factura_compra_id, proveedor_id)
    REFERENCES  factura_compra (id, proveedor_id)
    ON UPDATE CASCADE;

COMMENT ON CONSTRAINT articulo_historial_compra_factura_proveedor_fkey
    ON articulo_historial_compra IS
    'El proveedor del historial tiene que ser el de su propia factura. Sin esta '
    'restricción la dependencia transitiva id -> factura -> proveedor quedaba sin '
    'garantizar y se podían insertar filas incoherentes. Hallazgo N-3.';


-- -------------------------------------------------------------------------------------
--  A.3 · N-5 · Tabla provincia, y clave ajena desde las dos tablas de dirección
--
--  `cliente_direccion.provincia` y `proveedor_direccion.provincia` eran VARCHAR(60)
--  libre: el nombre se repetía por fila y nada impedía que convivieran CORDOBA,
--  Córdoba y 'CORDOBA ' con espacio al final. Es dependencia transitiva: el nombre
--  de la provincia no depende de la dirección, depende de la provincia.
--
--  Medí 21 valores distintos en el export, con 0 violaciones de Provincia[5] ->
--  Provincia[6] y de Prov. -> Provincia. Igual cargo las 24 jurisdicciones y no
--  sólo las 21 en uso: así ninguna dirección nueva falla al resolver, y una tabla
--  de provincias incompleta es un pie para el próximo que cargue un cliente.
--
--  La fila `(0, '< Prov.NO Registrada')` NO se carga: es el centinela del sistema
--  viejo. Acá eso se expresa con la columna en NULL, que es lo que significa.
-- -------------------------------------------------------------------------------------

CREATE TABLE provincia (
    id      INTEGER     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre  VARCHAR(60) NOT NULL,

    CONSTRAINT provincia_nombre_unico       UNIQUE (nombre),
    CONSTRAINT provincia_nombre_no_vacio    CHECK  (btrim(nombre) <> ''),
    CONSTRAINT provincia_nombre_sin_bordes  CHECK  (nombre = btrim(nombre))
);

COMMENT ON TABLE provincia IS
    'Provincias argentinas en uso. Elimina la dependencia transitiva de las dos '
    'tablas de dirección, donde el nombre viajaba como texto libre por fila. '
    'Hallazgo N-5. El centinela «< Prov.NO Registrada» del sistema anterior no se '
    'carga: su equivalente acá es la clave ajena en NULL.';

INSERT INTO provincia (nombre) VALUES
    ('BUENOS AIRES'),
    ('CATAMARCA'),
    ('CHACO'),
    ('CHUBUT'),
    ('CIUDAD AUTONOMA DE BUENOS AIRES'),
    ('CORDOBA'),
    ('CORRIENTES'),
    ('ENTRE RIOS'),
    ('FORMOSA'),
    ('JUJUY'),
    ('LA PAMPA'),
    ('LA RIOJA'),
    ('MENDOZA'),
    ('MISIONES'),
    ('NEUQUEN'),
    ('RIO NEGRO'),
    ('SALTA'),
    ('SAN JUAN'),
    ('SAN LUIS'),
    ('SANTA CRUZ'),
    ('SANTA FE'),
    ('SANTIAGO DEL ESTERO'),
    ('TIERRA DEL FUEGO'),
    ('TUCUMAN');

-- Las dos direcciones pasan de texto a clave ajena, en tres pasos por tabla:
-- agregar la columna, resolver el nombre contra la tabla nueva, y recién después
-- eliminar el texto. Si algún nombre no resuelve, el paso de control de abajo
-- aborta y el texto todavía está ahí.

ALTER TABLE cliente_direccion   ADD COLUMN provincia_id INTEGER REFERENCES provincia(id);
ALTER TABLE proveedor_direccion ADD COLUMN provincia_id INTEGER REFERENCES provincia(id);

-- La resolución normaliza mayúsculas, espacios de borde Y ACENTOS. Lo de los
-- acentos no es decorativo: en el export medí los 21 valores en mayúsculas sin
-- acento, pero en cuanto alguien cargue «Córdoba» a mano desde la pantalla, un
-- igual simple no lo encuentra. `translate` y no la extensión `unaccent` para no
-- depender de una extensión que puede no estar instalada en los dos entornos.

UPDATE cliente_direccion d
SET    provincia_id = p.id
FROM   provincia p
WHERE  upper(btrim(translate(d.provincia,
        'áéíóúÁÉÍÓÚàèìòùÀÈÌÒÙäëïöüÄËÏÖÜâêîôûÂÊÎÔÛñÑ',
        'aeiouAEIOUaeiouAEIOUaeiouAEIOUaeiouAEIOUnN'))) = p.nombre;

UPDATE proveedor_direccion d
SET    provincia_id = p.id
FROM   provincia p
WHERE  upper(btrim(translate(d.provincia,
        'áéíóúÁÉÍÓÚàèìòùÀÈÌÒÙäëïöüÄËÏÖÜâêîôûÂÊÎÔÛñÑ',
        'aeiouAEIOUaeiouAEIOUaeiouAEIOUaeiouAEIOUnN'))) = p.nombre;

-- SEGUNDA PASADA · nombres históricos que no son el nombre oficial de la provincia.
-- «CAPITAL FEDERAL» son 3 filas medidas en la base real (2 de cliente, 1 de proveedor): es
-- el nombre anterior de la Ciudad Autónoma. NO va como fila nueva en `provincia` —serían dos
-- filas para una misma provincia, que es justo la redundancia que N-5 elimina— sino como
-- sinónimo que resuelve a la fila oficial. Los otros tres se agregan por si aparecen al
-- cargar a mano; cuestan nada y evitan que el script aborte por un nombre esperable.

CREATE TEMP TABLE provincia_sinonimo (
    desde  TEXT PRIMARY KEY,
    hasta  TEXT NOT NULL
) ON COMMIT DROP;

INSERT INTO provincia_sinonimo (desde, hasta) VALUES
    ('CAPITAL FEDERAL',        'CIUDAD AUTONOMA DE BUENOS AIRES'),
    ('CABA',                   'CIUDAD AUTONOMA DE BUENOS AIRES'),
    ('C.A.B.A.',               'CIUDAD AUTONOMA DE BUENOS AIRES'),
    ('CIUDAD DE BUENOS AIRES', 'CIUDAD AUTONOMA DE BUENOS AIRES');

UPDATE cliente_direccion d
SET    provincia_id = p.id
FROM   provincia p, provincia_sinonimo s
WHERE  d.provincia_id IS NULL
  AND  s.desde  = upper(btrim(translate(d.provincia,
        'áéíóúÁÉÍÓÚàèìòùÀÈÌÒÙäëïöüÄËÏÖÜâêîôûÂÊÎÔÛñÑ',
        'aeiouAEIOUaeiouAEIOUaeiouAEIOUaeiouAEIOUnN')))
  AND  p.nombre = s.hasta;

UPDATE proveedor_direccion d
SET    provincia_id = p.id
FROM   provincia p, provincia_sinonimo s
WHERE  d.provincia_id IS NULL
  AND  s.desde  = upper(btrim(translate(d.provincia,
        'áéíóúÁÉÍÓÚàèìòùÀÈÌÒÙäëïöüÄËÏÖÜâêîôûÂÊÎÔÛñÑ',
        'aeiouAEIOUaeiouAEIOUaeiouAEIOUaeiouAEIOUnN')))
  AND  p.nombre = s.hasta;


DO $resolucion$
DECLARE
    v_cli    BIGINT;
    v_pro    BIGINT;
    v_nombre TEXT;
BEGIN
    SELECT count(*) INTO v_cli
    FROM   cliente_direccion
    WHERE  provincia_id IS NULL
      AND  btrim(coalesce(provincia, '')) <> ''
      AND  btrim(provincia) NOT ILIKE '%NO Registrada%';

    SELECT count(*) INTO v_pro
    FROM   proveedor_direccion
    WHERE  provincia_id IS NULL
      AND  btrim(coalesce(provincia, '')) <> ''
      AND  btrim(provincia) NOT ILIKE '%NO Registrada%';

    IF v_cli + v_pro > 0 THEN
        RAISE NOTICE 'Nombres de provincia que no resolvieron:';
        FOR v_nombre IN
            SELECT DISTINCT btrim(provincia)
            FROM (
                SELECT provincia FROM cliente_direccion   WHERE provincia_id IS NULL
                UNION ALL
                SELECT provincia FROM proveedor_direccion WHERE provincia_id IS NULL
            ) t
            WHERE btrim(coalesce(provincia, '')) <> ''
              AND btrim(provincia) NOT ILIKE '%NO Registrada%'
            ORDER BY 1
        LOOP
            RAISE NOTICE '  >> %', v_nombre;
        END LOOP;

        RAISE EXCEPTION
            'N-5: % direcciones de cliente y % de proveedor tienen una provincia que '
            'no está en la tabla. Agregá esos nombres al INSERT de arriba y volvé a '
            'correr el script. No se eliminó la columna de texto.', v_cli, v_pro;
    END IF;

    RAISE NOTICE 'N-5: todas las provincias resolvieron contra la tabla.';
END $resolucion$;

ALTER TABLE cliente_direccion   DROP COLUMN provincia;
ALTER TABLE proveedor_direccion DROP COLUMN provincia;

CREATE INDEX cliente_direccion_provincia_id_idx   ON cliente_direccion   (provincia_id);
CREATE INDEX proveedor_direccion_provincia_id_idx ON proveedor_direccion (provincia_id);

COMMENT ON COLUMN cliente_direccion.provincia_id IS
    'Reemplaza la columna de texto libre `provincia`. NULL significa provincia no '
    'registrada, que es lo que el origen expresaba con el centinela de código 0. '
    'Hallazgo N-5.';

COMMENT ON COLUMN proveedor_direccion.provincia_id IS
    'Reemplaza la columna de texto libre `provincia`. NULL significa provincia no '
    'registrada. Hallazgo N-5.';

-- La localidad queda como texto a propósito. Medí 6 violaciones de
-- Localidad -> Provincia en los 2.108 clientes y son homónimos legítimos: hay más
-- de una «Villa María» en más de una provincia. Una tabla `localidad` con clave
-- ajena a `provincia` afirmaría una dependencia funcional que el dominio no tiene.
COMMENT ON COLUMN cliente_direccion.localidad IS
    'Texto libre, y es una decisión. El nombre de localidad NO determina la '
    'provincia: hay homónimos entre provincias (6 casos medidos en el origen), así '
    'que no hay dependencia transitiva que eliminar. Normalizarla exigiría el '
    'par (nombre, provincia) como clave, que es dato que el origen no trae.';


-- -------------------------------------------------------------------------------------
--  A.4 · N-6 · SACADO DE ESTE SCRIPT · va en el siguiente
--
--  Lo saqué después de medir los valores REALES de `cliente.condicion_pago` en la base,
--  el 04/11/2026. No se parecen a lo que yo había inferido del export, y la lista que
--  tenía escrita acá habría destruido datos del negocio:
--
--      CUENTA CORRIENTE A 30 DIAS  404      MERCADO PAGO            46
--      VOUCHER RAPITAXI            233      -CONTADO- / CONTADO     10
--      VALES TELETAXIS             148      ORDEN DE TRANSMITAXI     3
--      CONTADO EFECTIVO            145      CUENTADO PENDIENTE       1
--      CONTADO PENDIENTE           136      (NULL)                 923
--      CUENTA CORRIENTE 20 DIAS     59
--
--  DOS HALLAZGOS, y los dos son de negocio, no técnicos:
--
--  1. RAPITAXI, TELETAXIS y TRANSMITAXI son 384 clientes. No son condiciones de pago
--     genéricas: son instrumentos de las flotas de taxis, que son una parte grande de la
--     clientela. Un CHECK con una lista genérica los habría borrado a todos.
--
--  2. `CUENTA CORRIENTE A 30 DIAS` guarda DOS datos en una columna: la condición
--     (cuenta corriente) y el plazo (30 días). Eso es un problema de normalización por
--     derecho propio, y exige decidir si el plazo va en su propia columna. 463 clientes
--     entre los dos valores de cuenta corriente.
--
--  Hasta que la Dirección cierre la lista y decida qué hacer con el plazo, la columna
--  queda como está: VARCHAR(60) sin CHECK. Es la situación actual y no empeora nada.
--  Frenar los otros cinco cambios por esto sería cambiar lo seguro por lo incierto.
-- -------------------------------------------------------------------------------------


-- -------------------------------------------------------------------------------------
--  A.5 · N-9 · ubicacion_deposito pasa de articulo a stock
--
--  Estaba en `articulo`, con clave `articulo.id` sola. Pero el estante es de cada
--  local: es IDÉNTICO al problema que `stock` ya resolvió para la cantidad, sin
--  resolver. Con la tercera sucursal abriendo, un artículo va a tener tres
--  posiciones y un solo campo donde escribirlas.
--
--  La ubicación no depende del artículo: depende del par (artículo, sucursal), que
--  es exactamente la clave de `stock`.
-- -------------------------------------------------------------------------------------

ALTER TABLE stock
    ADD COLUMN ubicacion_deposito VARCHAR(30);

-- El valor que había en `articulo` se replica a todas las filas de stock de ese
-- artículo. No es una suposición: en el origen el dato era uno solo para todas las
-- sucursales, así que replicarlo conserva exactamente la información que existía.
-- Separarlo por sucursal es trabajo de depósito, no de este script.
UPDATE stock s
SET    ubicacion_deposito = a.ubicacion_deposito
FROM   articulo a
WHERE  a.id = s.articulo_id
  AND  btrim(coalesce(a.ubicacion_deposito, '')) <> '';

ALTER TABLE articulo DROP COLUMN ubicacion_deposito;

COMMENT ON COLUMN stock.ubicacion_deposito IS
    'Posición física del artículo en el depósito de ESA sucursal. Estaba en '
    '`articulo`, donde un solo campo tenía que servir para todos los locales. '
    'Migró con el valor replicado desde articulo: en el origen el dato era único '
    'para todas las sucursales, así que la réplica conserva la información que '
    'había. Hallazgo N-9.';


-- -------------------------------------------------------------------------------------
--  A.6 · N-11 · Las cuatro columnas de auditoría duplicadas
--
--  `cliente.usuario_alta_id` y `cliente.usuario_baja_id` duplicaban
--  `cliente_historial_baja.usuario_baja_id`, sin ninguna restricción que las atara.
--  Lo mismo en `usuario`. Y se habían agregado sólo a esas dos tablas, no a
--  `proveedor` ni a `articulo`, que tienen las mismas tablas de historial: el
--  esquema quedaba asimétrico sin ningún motivo.
--
--  El dato se conserva donde corresponde: en las cuatro tablas *_historial_baja,
--  que además guardan el motivo y la fecha. Lo que se elimina es la copia.
--
--  Se hace condicional porque estas columnas entraron fuera de la estructura del
--  script 01 y puede que no estén en todos los entornos.
-- -------------------------------------------------------------------------------------

DO $n11$
DECLARE
    v_par   TEXT;
    v_tabla TEXT;
    v_col   TEXT;
    v_hechos INTEGER := 0;
BEGIN
    FOREACH v_par IN ARRAY ARRAY[
        'cliente.usuario_alta_id',
        'cliente.usuario_baja_id',
        'usuario.usuario_alta_id',
        'usuario.usuario_baja_id'
    ] LOOP
        v_tabla := split_part(v_par, '.', 1);
        v_col   := split_part(v_par, '.', 2);

        IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE  table_schema = current_schema()
              AND  table_name   = v_tabla
              AND  column_name  = v_col
        ) THEN
            EXECUTE format('ALTER TABLE %I DROP COLUMN %I', v_tabla, v_col);
            v_hechos := v_hechos + 1;
            RAISE NOTICE 'N-11: eliminada %.%', v_tabla, v_col;
        ELSE
            RAISE NOTICE 'N-11: %.% no existía, nada que hacer.', v_tabla, v_col;
        END IF;
    END LOOP;

    RAISE NOTICE 'N-11: % columnas eliminadas de 4 posibles.', v_hechos;
END $n11$;


-- =====================================================================================
--  PARTE B · LAS COLUMNAS QUE PIDIÓ LA DIRECCIÓN EL 02/11/2026
--
--  Cada una habilita un reporte concreto. Ninguna existe hoy, y sin ellas el
--  reporte no se puede construir: no es cuestión de consultar mejor.
-- =====================================================================================


-- -------------------------------------------------------------------------------------
--  B.1 · articulo.codigo_barras · habilita R-10
--
--  Pedido de Sergio: el EAN del fabricante es el mismo en todos los proveedores que
--  venden esa pieza, así que sirve de identificador común de equivalencia SIN que
--  nadie lo declare a mano.
--
--  NO reemplaza `articulo_codigo_alternativo`: hay repuestos sin EAN, y dos marcas
--  distintas tienen EAN distinto aunque la pieza sirva igual. Resuelve sola una
--  parte grande del catálogo, no todo.
--
--  VARCHAR(20) y no numérico: EAN-13, EAN-8, UPC-A y los códigos internos de
--  proveedor conviven, y los ceros a la izquierda son significativos.
--  El UNIQUE es PARCIAL: la mayoría de los artículos va a tener NULL durante mucho
--  tiempo, y un UNIQUE común los dejaría pasar igual pero indexaría 2.644 nulos.
-- -------------------------------------------------------------------------------------

ALTER TABLE articulo
    ADD COLUMN codigo_barras VARCHAR(20);

ALTER TABLE articulo
    ADD CONSTRAINT articulo_codigo_barras_formato
    CHECK (codigo_barras IS NULL OR codigo_barras ~ '^[0-9A-Za-z-]{6,20}$');

CREATE UNIQUE INDEX articulo_codigo_barras_unico
    ON articulo (codigo_barras)
    WHERE codigo_barras IS NOT NULL;

COMMENT ON COLUMN articulo.codigo_barras IS
    'EAN/UPC del fabricante. Identificador común de equivalencia entre proveedores: '
    'dos proveedores que venden la misma pieza traen el mismo código de barras, así '
    'que la equivalencia se deduce sin declararla. No reemplaza a '
    'articulo_codigo_alternativo (hay repuestos sin EAN, y marcas distintas tienen '
    'EAN distinto). VARCHAR y no numérico: los ceros a la izquierda cuentan. '
    'Pedido de la Dirección, 02/11/2026. Alimenta el reporte R-10.';


-- -------------------------------------------------------------------------------------
--  B.2 · stock.minimo · habilita R-11, el reporte más pedido
--
--  Textual de Sergio: «que tengan mínimos de cantidades, para que esos reportes
--  tengan un reporte de alerta: che, de esto tenés poco stock. Y vos podés poner
--  los pisos».
--
--  Va en `stock` y no en `articulo` por la misma razón que la cantidad: EL MÍNIMO
--  ES POR SUCURSAL. Fragueiro puede necesitar 20 bujías de piso y San Vicente 5.
--  En `articulo` eso no se podría expresar.
--
--  DEFAULT 0 significa «sin piso definido», y es lo correcto como arranque: un piso
--  inventado dispararía 2.644 alertas falsas el primer día.
-- -------------------------------------------------------------------------------------

ALTER TABLE stock
    ADD COLUMN minimo NUMERIC(12,3) NOT NULL DEFAULT 0;

ALTER TABLE stock
    ADD CONSTRAINT stock_minimo_no_negativo CHECK (minimo >= 0);

COMMENT ON COLUMN stock.minimo IS
    'Punto de reposición de ese artículo en ESA sucursal. 0 = sin piso definido, y '
    'por eso no genera alerta: un piso inventado produciría alertas falsas para todo '
    'el catálogo. Va en stock y no en articulo porque el mínimo es por sucursal. '
    'Pedido de la Dirección, 02/11/2026. Alimenta el reporte R-11.';


-- -------------------------------------------------------------------------------------
--  B.3 · factura_venta.medio_cobro · habilita R-14
--
--  Hoy no existe, y sin esto no hay reporte de composición de ingresos: no se puede
--  saber cuánto entró en efectivo, cuánto por transferencia y cuánto quedó en
--  cuenta corriente.
--
--  Ojo con la diferencia, que importa: `cliente.condicion_pago` es la condición
--  ACORDADA con el cliente; `factura_venta.medio_cobro` es cómo se cobró ESA venta.
--  Un cliente de cuenta corriente puede pagar una venta en efectivo. Son dos datos
--  distintos y ninguno se deduce del otro.
--
--  Entra como NULL y recién se vuelve NOT NULL si la tabla está vacía. Poner un
--  DEFAULT 'EFECTIVO' sobre ventas ya cargadas afirmaría algo que nadie midió.
-- -------------------------------------------------------------------------------------

ALTER TABLE factura_venta
    ADD COLUMN medio_cobro VARCHAR(30);

ALTER TABLE factura_venta
    ADD CONSTRAINT factura_venta_medio_cobro_dominio
    CHECK (medio_cobro IS NULL OR medio_cobro IN (
        'EFECTIVO',
        'TRANSFERENCIA',
        'TARJETA DE DEBITO',
        'TARJETA DE CREDITO',
        'CUENTA CORRIENTE'
    ));

DO $cobro$
DECLARE
    v_filas BIGINT;
BEGIN
    SELECT count(*) INTO v_filas FROM factura_venta;

    IF v_filas = 0 THEN
        ALTER TABLE factura_venta ALTER COLUMN medio_cobro SET NOT NULL;
        RAISE NOTICE 'B.3: factura_venta está vacía, medio_cobro queda NOT NULL.';
    ELSE
        RAISE NOTICE
            'B.3: factura_venta tiene % filas, así que medio_cobro queda NULLABLE. '
            'Cargá el medio de cobro de esas ventas y después corré: '
            'ALTER TABLE factura_venta ALTER COLUMN medio_cobro SET NOT NULL;', v_filas;
    END IF;
END $cobro$;

COMMENT ON COLUMN factura_venta.medio_cobro IS
    'Cómo se cobró ESTA venta. Distinto de cliente.condicion_pago, que es la '
    'condición acordada con el cliente: uno de cuenta corriente puede pagar una '
    'venta en efectivo, y ninguno de los dos datos se deduce del otro. Pedido de la '
    'Dirección, 02/11/2026. Alimenta el reporte R-14.';


-- -------------------------------------------------------------------------------------
--  B.4 · La doble certificación para anular · habilita R-08 y modifica el RF-010
--
--  La Dirección exige que anular un comprobante lleve dos personas: quien lo pide y
--  quien lo autoriza, y que el que autoriza tenga rol ADMINISTRADOR.
--
--  La columna sola no alcanza para garantizarlo: que los dos usuarios sean distintos
--  se puede expresar en un CHECK, pero que el autorizante sea ADMINISTRADOR depende
--  de otra tabla y eso un CHECK no lo puede mirar. Esa mitad va en el caso de uso,
--  y lo digo acá para que no se suponga que la base lo cubre.
--
--  El CHECK de «distintos» se arma por código porque el nombre de la columna que
--  guarda quién anuló no es el mismo en las dos tablas en todos los entornos.
-- -------------------------------------------------------------------------------------

ALTER TABLE factura_venta
    ADD COLUMN usuario_autorizacion_id INTEGER REFERENCES usuario(id);

ALTER TABLE factura_compra
    ADD COLUMN usuario_autorizacion_id INTEGER REFERENCES usuario(id);

DO $doble$
DECLARE
    v_tabla      TEXT;
    v_anulador   TEXT;
    -- Sólo columnas que de verdad guardan QUIÉN ANULÓ. `usuario_id` quedó afuera a
    -- propósito: es quien EMITIÓ el comprobante, y usarla haría que el CHECK
    -- afirmara otra regla («el autorizante no puede ser el emisor») sin que nadie
    -- se enterara. Si ninguna de las dos está, el script avisa y no pone el CHECK.
    v_candidatas TEXT[] := ARRAY['usuario_anulacion_id', 'usuario_baja_id'];
    v_c          TEXT;
BEGIN
    FOREACH v_tabla IN ARRAY ARRAY['factura_venta', 'factura_compra'] LOOP
        v_anulador := NULL;

        FOREACH v_c IN ARRAY v_candidatas LOOP
            IF v_anulador IS NULL AND EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE  table_schema = current_schema()
                  AND  table_name   = v_tabla
                  AND  column_name  = v_c
            ) THEN
                v_anulador := v_c;
            END IF;
        END LOOP;

        IF v_anulador IS NULL THEN
            RAISE NOTICE
                'B.4: en % no encontré la columna del usuario que anula. La columna '
                'usuario_autorizacion_id quedó creada, pero el CHECK de «dos personas '
                'distintas» NO se pudo poner. Hay que agregarlo a mano.', v_tabla;
        ELSE
            EXECUTE format(
                'ALTER TABLE %I ADD CONSTRAINT %I CHECK '
                '(usuario_autorizacion_id IS NULL OR usuario_autorizacion_id <> %I)',
                v_tabla, v_tabla || '_autorizacion_distinta', v_anulador);

            RAISE NOTICE 'B.4: % · CHECK contra % puesto.', v_tabla, v_anulador;
        END IF;
    END LOOP;
END $doble$;

COMMENT ON COLUMN factura_venta.usuario_autorizacion_id IS
    'Quién AUTORIZÓ la anulación, que tiene que ser una persona distinta de quien la '
    'pidió (doble certificación exigida por la Dirección, 02/11/2026). Que además '
    'tenga rol ADMINISTRADOR lo valida el caso de uso: depende de otra tabla y un '
    'CHECK no puede mirarla. Alimenta R-08 y modifica el RF-010.';

COMMENT ON COLUMN factura_compra.usuario_autorizacion_id IS
    'Quién AUTORIZÓ la anulación. Mismo criterio que en factura_venta: la condición '
    'de rol ADMINISTRADOR se valida en el caso de uso, no acá. Alimenta R-08.';


-- -------------------------------------------------------------------------------------
--  B.5 · con_comprobante_fiscal · R-15
--
--  Nombre neutro a propósito. El campo y el reporte existen en el sistema; en el
--  GR-01 no se documentan.
--
--  DEFAULT TRUE: todo lo cargado hasta hoy se asume con comprobante, que es lo que
--  el esquema venía afirmando implícitamente al no tener la columna.
-- -------------------------------------------------------------------------------------

ALTER TABLE factura_venta
    ADD COLUMN con_comprobante_fiscal BOOLEAN NOT NULL DEFAULT TRUE;

ALTER TABLE factura_compra
    ADD COLUMN con_comprobante_fiscal BOOLEAN NOT NULL DEFAULT TRUE;

COMMENT ON COLUMN factura_venta.con_comprobante_fiscal IS
    'Indica si la operación se respaldó con comprobante fiscal. TRUE por defecto: es '
    'lo que el esquema afirmaba implícitamente al no tener la columna.';

COMMENT ON COLUMN factura_compra.con_comprobante_fiscal IS
    'Indica si la operación se respaldó con comprobante fiscal. TRUE por defecto.';


-- =====================================================================================
--  COMPUERTA DE CIERRE
--     Si algo de lo de arriba no quedó como se esperaba, acá revienta y el ROLLBACK
--     deja la base como estaba.
-- =====================================================================================

DO $cierre$
DECLARE
    v_falta  TEXT := '';
    v_item   TEXT;
    v_sobra  TEXT := '';
    v_pk     TEXT;
    v_prov   BIGINT;
BEGIN
    -- Columnas que TIENEN que existir ahora
    FOREACH v_item IN ARRAY ARRAY[
        'provincia.nombre',
        'cliente_direccion.provincia_id',
        'proveedor_direccion.provincia_id',
        'stock.ubicacion_deposito',
        'stock.minimo',
        'articulo.codigo_barras',
        'factura_venta.medio_cobro',
        'factura_venta.usuario_autorizacion_id',
        'factura_compra.usuario_autorizacion_id',
        'factura_venta.con_comprobante_fiscal',
        'factura_compra.con_comprobante_fiscal'
    ] LOOP
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE  table_schema = current_schema()
              AND  table_name   = split_part(v_item, '.', 1)
              AND  column_name  = split_part(v_item, '.', 2)
        ) THEN
            v_falta := v_falta || E'\n  no se creó  ' || v_item;
        END IF;
    END LOOP;

    -- Columnas que TIENEN que haber desaparecido
    FOREACH v_item IN ARRAY ARRAY[
        'articulo.ubicacion_deposito',
        'cliente_direccion.provincia',
        'proveedor_direccion.provincia',
        'cliente.usuario_alta_id',
        'cliente.usuario_baja_id',
        'usuario.usuario_alta_id',
        'usuario.usuario_baja_id'
    ] LOOP
        IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE  table_schema = current_schema()
              AND  table_name   = split_part(v_item, '.', 1)
              AND  column_name  = split_part(v_item, '.', 2)
        ) THEN
            v_sobra := v_sobra || E'\n  sigue existiendo  ' || v_item;
        END IF;
    END LOOP;

    IF v_falta <> '' OR v_sobra <> '' THEN
        RAISE EXCEPTION 'El cierre no coincide con lo esperado.% %', v_falta, v_sobra;
    END IF;

    -- N-7 · la clave primaria tiene que tener las tres columnas
    SELECT pg_get_constraintdef(oid) INTO v_pk
    FROM   pg_constraint
    WHERE  conrelid = format('%I.factura_compra_detalle', current_schema())::regclass
      AND  contype  = 'p';

    IF v_pk NOT LIKE '%sucursal_id%' THEN
        RAISE EXCEPTION 'N-7: la clave primaria quedó sin sucursal_id: %', v_pk;
    END IF;

    -- N-5 · la tabla de provincias tiene que tener filas y las direcciones resolver
    SELECT count(*) INTO v_prov FROM provincia;
    IF v_prov < 20 THEN
        RAISE EXCEPTION 'N-5: la tabla provincia quedó con % filas.', v_prov;
    END IF;

    RAISE NOTICE '====================================================================';
    RAISE NOTICE 'TODO OK.';
    RAISE NOTICE 'Parte A · N-3, N-5, N-7, N-9 y N-11 aplicados. Falta N-6 (condicion_pago).';
    RAISE NOTICE '          con N-1 y N-2 conservadas como denormalizaciones declaradas.';
    RAISE NOTICE 'Parte B · codigo_barras, minimo, medio_cobro, usuario_autorizacion_id';
    RAISE NOTICE '          y con_comprobante_fiscal creadas.';
    RAISE NOTICE 'Clave primaria de factura_compra_detalle: %', v_pk;
    RAISE NOTICE 'Provincias cargadas: %', v_prov;
    RAISE NOTICE '====================================================================';
END $cierre$;

COMMIT;


-- =====================================================================================
--  DESPUÉS DE APLICAR
--
--  1. Correrlo en las DOS bases y confirmar que las dos imprimen el mismo cierre.
--
--  2. Re-scaffold, según backend/db/rescaffold/README.md. Comparar antes el esquema
--     de test contra el de producción, como dice ese README.
--
--  3. Lo que se rompe en el código, y conviene saberlo antes de compilar:
--     · `articulo.ubicacion_deposito` ya no existe  -> todo lo que la lea pasa a stock
--     · `cliente_direccion.provincia` es ahora `provincia_id` -> hay que proyectar el
--       nombre con un JOIN, y la pantalla pasa de campo de texto a combo
--     · `factura_compra_detalle` tiene clave de tres columnas -> revisar los Find y
--       los Update del repositorio que la buscaban por dos
--     · `cliente.usuario_alta_id` / `usuario_baja_id` no existen -> el dato sale de
--       cliente_historial_baja
--
--  4. Lo que queda abierto a propósito:
--     · El dominio de condicion_pago, hasta que Sergio confirme la lista
--     · El NOT NULL de factura_venta.medio_cobro, si la tabla tenía filas
--     · La mitad del CHECK de autorización que depende del rol: va en el caso de uso
--     · N-8, con su propia historia de usuario
--
--  5. Tests que hay que agregar, porque cada cambio es una afirmación verificable:
--     · Insertar dos líneas del mismo artículo con sucursal distinta -> tiene que
--       ENTRAR (hoy falla, y es el defecto que N-7 arregla)
--     · Insertar un historial con proveedor distinto al de su factura -> tiene que
--       FALLAR
--     · Insertar una condicion_pago fuera de la lista -> tiene que FALLAR
--     · Dos artículos con el mismo codigo_barras -> tiene que FALLAR
--     · Dos artículos con codigo_barras NULL -> tiene que ENTRAR (el UNIQUE es parcial)
--     · Autorizar una anulación con el mismo usuario que la pidió -> tiene que FALLAR
-- =====================================================================================
