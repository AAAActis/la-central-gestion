# Comparador de precios · lo que cuesta un artículo en cada proveedor

1. Entrá a **Comparador de precios**, escribí el nombre o el código del artículo y apretá **Buscar**.
   Elegí el artículo de la lista.
2. Aparece una fila por cada código de proveedor vinculado al artículo, de la más barata a la más
   cara. Se compara el **precio neto** (bonificado, sin IVA); el de lista se muestra, pero no ordena.
   Una fila sin precio neto va al final, con «No informa precio bonificado».
3. **Vista Compra:** precio de lista, neto, la diferencia con el más barato, el estado de stock y la
   fecha en que se leyó el precio. «Último comprado» marca al proveedor de la última compra. Abajo, el
   stock de cada proveedor: si tiene varios códigos del mismo artículo, se informa el mayor, nunca la suma.
4. **Vista Venta:** el precio de venta estimado propio (lista) y la existencia por sucursal, junto a
   los precios de los proveedores. Si el artículo no tiene márgenes cargados, dice que no hay precio
   estimado.
5. **Abrir sitio** abre la página del proveedor en otra pestaña: el comparador queda como estaba. Si el
   proveedor no tiene la dirección cargada, no aparece.
6. No aparecen los proveedores dados de baja ni los productos vencidos. Los precios son los del último
   scrape de cada proveedor (los carga n8n); no se consulta al sitio en el momento.
