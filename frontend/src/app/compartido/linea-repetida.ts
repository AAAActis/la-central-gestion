/** #169 (D9): repetido es el mismo artículo en el mismo destino u origen; en otra sucursal es otra
 *  línea. `salvo` es la línea que se está reemplazando. */
export function lineaRepetida(
  lineas: { articulo: { id: number }; sucursalId: number | null }[],
  articuloId: number, sucursalId: number | null, salvo: number | null = null,
) {
  return lineas.some((l, i) => l.articulo.id === articuloId && l.sucursalId === sucursalId && i !== salvo);
}
