-- 02 · Total que informó el sitio en el sondeo (página de 1 artículo) · contrato de n8n con el script 12
-- $1 corrida_id · $2 total
-- Solo sobre una corrida RUNNING. Devuelve el id; sin filas = la corrida ya está cerrada.
-- shortcut: esqueleto, sin implementar todavía.
SELECT NULL::integer AS id WHERE $1::integer IS NULL AND $2::integer IS NULL;
