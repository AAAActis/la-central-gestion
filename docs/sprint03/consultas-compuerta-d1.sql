-- Compuerta de esquema D1 · consultas de SOLO LECTURA sobre el catálogo.
-- Correr contra la base de TEST (lacentral_test), esquema plantilla public:
--   psql "<LACENTRAL_TEST_PG>" -f docs/sprint03/consultas-compuerta-d1.sql
-- No escriben nada: la sesión se pone en solo lectura antes de la primera consulta.
SET default_transaction_read_only = on;

SELECT current_database(), current_setting('server_version'),
       current_setting('transaction_read_only') AS solo_lectura;

-- 1. articulo_margen: columnas tal como las tiene el motor
SELECT a.attnum, a.attname, format_type(a.atttypid, a.atttypmod) AS tipo,
       a.attnotnull AS not_null, NULLIF(a.attidentity, '') AS identidad,
       pg_get_expr(d.adbin, d.adrelid) AS defecto
FROM pg_attribute a
LEFT JOIN pg_attrdef d ON d.adrelid = a.attrelid AND d.adnum = a.attnum
WHERE a.attrelid = 'public.articulo_margen'::regclass AND a.attnum > 0 AND NOT a.attisdropped
ORDER BY a.attnum;

-- 2. articulo_margen: PK, UNIQUE, FK y CHECK, con la definición que devuelve el motor
SELECT conname, contype, pg_get_constraintdef(oid)
FROM pg_constraint WHERE conrelid = 'public.articulo_margen'::regclass
ORDER BY contype, conname;

-- 3. articulo_margen: índices, FKs entrantes, triggers y comentario
SELECT indexname, indexdef FROM pg_indexes
WHERE schemaname = 'public' AND tablename = 'articulo_margen';
SELECT conrelid::regclass, conname, pg_get_constraintdef(oid)
FROM pg_constraint WHERE confrelid = 'public.articulo_margen'::regclass;
SELECT tgname, pg_get_triggerdef(oid) FROM pg_trigger
WHERE tgrelid = 'public.articulo_margen'::regclass AND NOT tgisinternal;
SELECT obj_description('public.articulo_margen'::regclass, 'pg_class');

-- 4. Toda columna numeric/money/real/double del esquema, con tipo y escala
SELECT c.table_name, c.column_name, format_type(a.atttypid, a.atttypmod) AS tipo,
       c.numeric_precision, c.numeric_scale, c.is_nullable, c.column_default
FROM information_schema.columns c
JOIN pg_attribute a
  ON a.attrelid = (quote_ident(c.table_schema) || '.' || quote_ident(c.table_name))::regclass
 AND a.attname = c.column_name
WHERE c.table_schema = 'public'
  AND a.atttypid IN ('numeric'::regtype, 'money'::regtype, 'real'::regtype, 'double precision'::regtype)
ORDER BY 1, 2;

-- 5. ¿Algo que ya pueda guardar un precio por modalidad CON NOMBRE?
--    Columnas o tablas con nombre de modalidad/precio/tarifa/config, y tipos ENUM.
SELECT table_name, column_name, data_type FROM information_schema.columns
WHERE table_schema = 'public'
  AND column_name ~* '(modalidad|tarjeta|efectivo|transferencia|lista|especial|tasa|recargo|tarifa)'
ORDER BY 1, 2;
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name ~* '(modalidad|precio|margen|tarifa|lista|tasa|config|parametro)'
ORDER BY 1;
SELECT t.typname, string_agg(e.enumlabel, ', ' ORDER BY e.enumsortorder)
FROM pg_type t JOIN pg_enum e ON e.enumtypid = t.oid
JOIN pg_namespace n ON n.oid = t.typnamespace
WHERE n.nspname = 'public' GROUP BY 1;

-- 6. CHECKs de precios en todo el esquema
SELECT conrelid::regclass, conname, pg_get_constraintdef(c.oid)
FROM pg_constraint c JOIN pg_namespace n ON n.oid = c.connamespace
WHERE n.nspname = 'public' AND c.contype = 'c'
  AND pg_get_constraintdef(c.oid) ~* '(precio|costo|margen|porcentaje|modalidad|numero)'
ORDER BY 1, 2;

-- 7. Datos de articulo_margen (solo conteos, para dimensionar la migración)
SELECT numero, count(*), min(porcentaje), max(porcentaje)
FROM public.articulo_margen GROUP BY 1 ORDER BY 1;
