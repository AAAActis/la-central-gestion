# 031 · HU-CMP-02 · qué hace el sistema y qué queda fuera (26/11/2026)

La 031 no se construye como consulta en vivo. Los precios los trae el scrape de cada proveedor, que
corre en n8n y escribe `proveedor_articulo` con las sentencias de `CONTRATO-N8N.md` (D-S7-01). Los
workflows, el programa y las credenciales de n8n los maneja Santi fuera del repo. Este documento no
afirma nada sobre lo que hacen.

| CA | En el sistema (comparador, #030/#050) | Fuera del sistema |
|----|---------------------------------------|-------------------|
| CA-001 consulta en paralelo | No consulta sitios: lee la base. | La forma de consultar es de n8n. |
| CA-002 mostrar lo que ya llegó | No aplica: una sola lectura local, sin esperas por proveedor. | — |
| CA-003 falla o demora de un proveedor | Muestra el último precio conocido con su fecha (`ultima_lectura`) y, si la última corrida **terminada** del proveedor quedó en `FAILED`, lo informa en la fila (050 CA-003, 27/11/2026). Una corrida en curso no cuenta. | El reintento y el aviso de falla son de n8n. |
| CA-004 proveedor sin acceso automatizado | Si tiene filas cargadas, las muestra con su fecha. Si no tiene filas, no aparece. | — |
| CA-005 reusar un resultado reciente | No aplica: no hay consulta que repetir. | — |
| CA-006 credenciales vencidas | El sistema no usa las credenciales: `proveedor_credencial` solo las guarda. | Lo hace n8n, si lo hace. |
