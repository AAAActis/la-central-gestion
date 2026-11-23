-- 04 · Cerrar una corrida · contrato de n8n con el script 12 (HU-CMP-08, 055; D-S7-01)
-- $1 corrida_id · $2 'SUCCESS' o 'FAILED' · $3 mensaje de error (NULL si no hubo)
-- Cuenta los ítems de la corrida en su historial (CA-002). Si cierra en SUCCESS y leyó al menos un ítem,
-- marca vencido (activo = FALSE) lo del proveedor que esta corrida no leyó, sin borrarlo (CA-003). Una
-- corrida FAILED, o una SUCCESS sin ítems (login caído, sitio vacío), no vence nada: no hay lectura
-- completa contra la cual comparar. Solo sobre una RUNNING: sin filas = ya estaba cerrada.
-- Devuelve id, cantidad_items y cuántos renglones quedaron vencidos.
WITH corrida AS (
    UPDATE scrape_corrida SET estado = $2::text, fin = now(), mensaje_error = $3::text,
           cantidad_items = (SELECT count(*) FROM proveedor_articulo_historial h WHERE h.corrida_id = $1::integer)
    WHERE  id = $1::integer AND estado = 'RUNNING'
    RETURNING id, proveedor_id, estado, cantidad_items
), vencidos AS (
    UPDATE proveedor_articulo pa SET activo = FALSE
    FROM   corrida c
    WHERE  c.estado = 'SUCCESS' AND c.cantidad_items > 0
      AND  pa.proveedor_id = c.proveedor_id AND pa.ultima_corrida_id <> c.id AND pa.activo
    RETURNING pa.id
)
SELECT c.id, c.cantidad_items, (SELECT count(*) FROM vencidos) AS vencidos FROM corrida c;
