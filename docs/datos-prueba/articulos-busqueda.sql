-- Fixture sintético para PostgreSQL. Ejecutar con psql -v ON_ERROR_STOP=1 -f ...
-- Crea SOLO tablas temporales de esta sesión. No carga la base de demostración.
BEGIN;
CREATE TEMP TABLE articulo_busqueda_fixture (
    id integer PRIMARY KEY, codigo_interno text UNIQUE NOT NULL,
    nombre text NOT NULL, activo boolean NOT NULL, precio_costo numeric NULL);
CREATE TEMP TABLE codigo_busqueda_fixture (
    articulo_id integer REFERENCES articulo_busqueda_fixture(id),
    proveedor_id integer NOT NULL, codigo text UNIQUE NOT NULL,
    UNIQUE (articulo_id, proveedor_id));

INSERT INTO articulo_busqueda_fixture
SELECT n, 'ART-' || lpad(n::text, 6, '0'),
    CASE WHEN n = 1 THEN 'Mouse GENIUS 01'
         WHEN n = 2 THEN 'Mouse GENIUS 02'
         WHEN n = 3 THEN 'Bujía pequeña modelo 03'
         ELSE (ARRAY['Filtro Bosch', 'Bujía NGK', 'Correa Gates', 'Pastilla Brembo'])[(n % 4) + 1]
              || ' modelo ' || lpad(n::text, 2, '0') END,
    n <> 40, CASE WHEN n % 3 = 0 THEN NULL ELSE n * 10.25 END
FROM generate_series(1, 40) n;

-- Dos escrituras originales que colisionan al normalizar: revisión humana,
-- nunca fusión. Son proveedores distintos y cumplen D2 por artículo.
INSERT INTO codigo_busqueda_fixture VALUES
    (1, 1, 'gn-01'), (1, 2, 'genius-01'), (2, 1, ' GN-01 ');
INSERT INTO codigo_busqueda_fixture
SELECT n, 2, 'PRV-' || lpad(n::text, 3, '0') FROM generate_series(2, 40) n;

CREATE INDEX ON articulo_busqueda_fixture (upper(btrim(codigo_interno)));
CREATE INDEX ON codigo_busqueda_fixture (upper(btrim(codigo)));
ANALYZE articulo_busqueda_fixture;
ANALYZE codigo_busqueda_fixture;

-- Comparar planes con el mismo fixture. 40 filas pueden favorecer Seq Scan;
-- no forzar índices ni declarar tiempos sin ejecutar EXPLAIN ANALYZE.
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM articulo_busqueda_fixture
WHERE activo AND upper(btrim(codigo_interno)) = 'ART-000001';

EXPLAIN (ANALYZE, BUFFERS)
SELECT a.* FROM articulo_busqueda_fixture a
WHERE a.activo AND EXISTS (
    SELECT 1 FROM codigo_busqueda_fixture c
    WHERE c.articulo_id = a.id AND upper(btrim(c.codigo)) = 'GN-01');

EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM articulo_busqueda_fixture
WHERE activo AND translate(upper(nombre), 'ÁÉÍÓÚÜ', 'AEIOUU') LIKE '%MOUSE%'
             AND translate(upper(nombre), 'ÁÉÍÓÚÜ', 'AEIOUU') LIKE '%GENIUS%';

-- Alternativas por separado, ranking interno/proveedor/nombre y unión por Id.
WITH alternativas AS (
    SELECT DISTINCT upper(btrim(fragmento)) texto
    FROM unnest(string_to_array('mouse genius 01*gn-01*genius-01*', '*')) fragmento
    WHERE btrim(fragmento) <> ''
), coincidencias AS (
    SELECT a.id, 0 prioridad FROM articulo_busqueda_fixture a, alternativas q
    WHERE a.activo AND upper(btrim(a.codigo_interno)) = q.texto
    UNION ALL
    SELECT a.id, 1 FROM articulo_busqueda_fixture a, alternativas q
    WHERE a.activo AND EXISTS (SELECT 1 FROM codigo_busqueda_fixture c
        WHERE c.articulo_id = a.id AND upper(btrim(c.codigo)) = q.texto)
    UNION ALL
    SELECT a.id, 2 FROM articulo_busqueda_fixture a, alternativas q
    WHERE a.activo AND NOT EXISTS (
        SELECT 1 FROM regexp_split_to_table(translate(q.texto, 'ÁÉÍÓÚÜ', 'AEIOUU'), '\s+') palabra
        WHERE strpos(translate(upper(a.nombre), 'ÁÉÍÓÚÜ', 'AEIOUU'), palabra) = 0)
)
SELECT a.id, a.codigo_interno, a.nombre, min(c.prioridad) prioridad
FROM coincidencias c JOIN articulo_busqueda_fixture a ON a.id = c.id
GROUP BY a.id ORDER BY prioridad, a.nombre, a.id;

DO $$ BEGIN
    IF (SELECT count(*) FROM articulo_busqueda_fixture) <> 40 THEN
        RAISE EXCEPTION 'El fixture debe tener 40 artículos';
    END IF;
    IF (SELECT count(DISTINCT articulo_id) FROM codigo_busqueda_fixture
        WHERE upper(btrim(codigo)) = 'GN-01') <> 2 THEN
        RAISE EXCEPTION 'La colisión normalizada debe conservar ambos artículos';
    END IF;
END $$;
ROLLBACK;
