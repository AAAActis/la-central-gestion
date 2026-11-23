-- 01 · Abrir una corrida · contrato de n8n con el script 12 (HU-CMP-08, 055; D-S7-01)
-- $1 proveedor_id · $2 id de la ejecución de n8n ($execution.id)
-- Devuelve el id de la corrida. SIN FILAS = el proveedor no existe o está dado de baja (CA-004): el
-- workflow se corta. Si ya hay una RUNNING para el proveedor, falla con 23505 en
-- ux_scrape_corrida_en_curso: el orquestador nocturno y un disparo manual no se pisan (CA-002).
-- shortcut: esqueleto, sin implementar todavía.
SELECT NULL::integer AS id WHERE $1::integer IS NULL AND $2::text IS NULL;
