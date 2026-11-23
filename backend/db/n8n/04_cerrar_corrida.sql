-- 04 · Cerrar una corrida · contrato de n8n con el script 12 (HU-CMP-08, 055; D-S7-01)
-- $1 corrida_id · $2 'SUCCESS' o 'FAILED' · $3 mensaje de error (NULL si no hubo)
-- Cuenta los ítems de la corrida en su historial (CA-002). Si cierra en SUCCESS y leyó al menos un ítem,
-- marca vencido (activo = FALSE) lo del proveedor que esta corrida no leyó, sin borrarlo (CA-003). Una
-- corrida FAILED, o una SUCCESS sin ítems (login caído, sitio vacío), no vence nada: no hay lectura
-- completa contra la cual comparar. Solo sobre una RUNNING: sin filas = ya estaba cerrada.
-- Devuelve id, cantidad_items y cuántos renglones quedaron vencidos.
-- shortcut: esqueleto, sin implementar todavía.
SELECT NULL::integer AS id, NULL::integer AS cantidad_items, NULL::bigint AS vencidos WHERE $1::integer IS NULL AND $2::text IS NULL AND $3::text IS NULL;
