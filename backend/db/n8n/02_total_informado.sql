-- 02 · Total que informó el sitio en el sondeo (página de 1 artículo) · contrato de n8n con el script 12
-- $1 corrida_id · $2 total
-- Solo sobre una corrida RUNNING. Devuelve el id; sin filas = la corrida ya está cerrada.
UPDATE scrape_corrida SET total_informado = $2::integer
WHERE  id = $1::integer AND estado = 'RUNNING'
RETURNING id;
