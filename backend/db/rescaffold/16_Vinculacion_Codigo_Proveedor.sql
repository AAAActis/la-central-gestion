-- =====================================================================================
--  16_Vinculacion_Codigo_Proveedor.sql
--  La Central · 25/11/2026 · D2 revisada (D-S7-06) y vinculación de la 054 (D-S7-07; #127)
--
--  ESQUELETO: todavía no hace nada (rojo de Script16PostgreSqlTests).
--
--  QUÉ HACE
--    1. articulo_codigo_alternativo: la unicidad pasa a ser UNIQUE (proveedor_id, codigo)
--       (índice codigo_alt_proveedor_codigo_unico). Se borran:
--         codigo_unico_global      UNIQUE (codigo): dos proveedores pueden usar el mismo código;
--         un_codigo_por_proveedor  UNIQUE (articulo_id, proveedor_id): un artículo puede tener
--                                  varios códigos del mismo proveedor (CA3 de la 054);
--         ix_codigo_alt_proveedor  (proveedor_id): lo cubre el índice nuevo, que empieza igual.
--       Cada código de un proveedor sigue apuntando a un solo artículo: la factura de compra
--       identifica el artículo sin ambigüedad.
--    2. articulo_codigo_alternativo.usuario_id (FK a usuario) y fecha_vinculacion
--       (TIMESTAMPTZ): quién vinculó y cuándo (054 CA1). Los dos o ninguno
--       (aca_vinculacion_coherente). Las filas de antes del 16 y los códigos cargados desde
--       la ficha del artículo quedan con los dos en NULL: no se inventa quién ni cuándo.
--    La vinculación ES esta fila: proveedor_articulo se cruza con ella por
--    (proveedor_id, codigo). n8n no toca esta tabla (no tiene permisos sobre ella, script 12).
--
--  REQUISITO
--    El script 12 aplicado (proveedor_articulo existe). Si no, se corta sin tocar nada.
--
--  CÓMO SE APLICA
--    Una transacción. Es idempotente: cada paso se salta si ya está, y la verificación del
--    final corre siempre. Lo aplica Santi, primero en lacentral_test (public), después en
--    lacentral_demo y en lacentral (con backup). Después: re-scaffold.
-- =====================================================================================

ROLLBACK;

BEGIN;

COMMIT;
