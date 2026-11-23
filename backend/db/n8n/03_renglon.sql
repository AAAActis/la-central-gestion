-- 03 · Incorporar un renglón del catálogo · contrato de n8n con el script 12 (HU-CMP-08, 055; D-S7-01)
-- $1 corrida_id · $2 código del proveedor · $3 id externo · $4 código de barras · $5 nombre
-- $6 descripción · $7 detalle técnico («CLAVE: valor | CLAVE: valor») · $8 marca
-- $9 precio neto sin IVA · $10 precio de lista · $11 stock · $12 alícuota de IVA (21 = 21 %)
-- $13 URL de la imagen · $14 fecha de origen del dato
-- Upsert por (proveedor_id, código): no duplica, y la vinculación (articulo_codigo_alternativo, por
-- proveedor y código) no se toca (CA-001). Deja la fila activa (si estaba vencida, vuelve) y escribe su
-- precio y stock en el historial de la corrida (RF-016), una fila por renglón y corrida, en la misma
-- sentencia. Solo sobre una corrida RUNNING: sin filas = la corrida ya está cerrada. Desconocido es
-- NULL, nunca cero.
WITH corrida AS (
    SELECT id, proveedor_id FROM scrape_corrida WHERE id = $1::integer AND estado = 'RUNNING'
), renglon AS (
    INSERT INTO proveedor_articulo AS pa (proveedor_id, codigo, id_externo, codigo_barras, nombre, descripcion,
        detalle_tecnico, marca, precio_neto, precio_lista, stock, alicuota_iva, url_imagen, fecha_origen,
        ultima_corrida_id, ultima_lectura, activo)
    SELECT c.proveedor_id, btrim($2::text), $3::text, $4::text, btrim($5::text), $6::text, $7::text, $8::text,
           $9::numeric, $10::numeric, $11::numeric, $12::numeric, $13::text, $14::timestamptz,
           c.id, now(), TRUE
    FROM   corrida c
    ON CONFLICT (proveedor_id, codigo) DO UPDATE SET
        id_externo = EXCLUDED.id_externo, codigo_barras = EXCLUDED.codigo_barras, nombre = EXCLUDED.nombre,
        descripcion = EXCLUDED.descripcion, detalle_tecnico = EXCLUDED.detalle_tecnico, marca = EXCLUDED.marca,
        precio_neto = EXCLUDED.precio_neto, precio_lista = EXCLUDED.precio_lista, stock = EXCLUDED.stock,
        alicuota_iva = EXCLUDED.alicuota_iva, url_imagen = EXCLUDED.url_imagen, fecha_origen = EXCLUDED.fecha_origen,
        ultima_corrida_id = EXCLUDED.ultima_corrida_id, ultima_lectura = EXCLUDED.ultima_lectura, activo = TRUE
    RETURNING pa.id, pa.proveedor_id, pa.ultima_corrida_id, pa.precio_neto, pa.precio_lista, pa.stock
)
INSERT INTO proveedor_articulo_historial (proveedor_articulo_id, corrida_id, proveedor_id, precio_neto, precio_lista, stock)
SELECT id, ultima_corrida_id, proveedor_id, precio_neto, precio_lista, stock FROM renglon
ON CONFLICT (proveedor_articulo_id, corrida_id) DO UPDATE SET
    precio_neto = EXCLUDED.precio_neto, precio_lista = EXCLUDED.precio_lista, stock = EXCLUDED.stock,
    fecha_lectura = now()
RETURNING proveedor_articulo_id;
