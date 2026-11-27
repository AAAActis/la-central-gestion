/** Qué resultado elige el Enter de un buscador (HU-FCO-06 CA-002, CA-003 y CA-009). */

/** CA-002 y CA-003: el proveedor cuyo código es el texto, sin ceros a la izquierda. Con más de uno
 *  (035 y 00035) no hay elegido: elegir uno solo sería adivinar. */
export function exactoProveedor<T extends { codigo: string }>(proveedores: T[], texto: string): T | null {
  const codigo = sinCeros(texto);
  const exactos = proveedores.filter(p => sinCeros(p.codigo) === codigo);
  return exactos.length === 1 ? exactos[0] : null;
}

/** CA-009: el artículo cuyo código interno es el texto. */
export function exactoArticulo<T extends { codigoInterno: string }>(articulos: T[], texto: string): T | null {
  const codigo = texto.trim().toUpperCase();
  return articulos.find(a => a.codigoInterno.trim().toUpperCase() === codigo) ?? null;
}

/** CA-003: 35 y 00035 son el mismo código. */
function sinCeros(codigo: string) {
  return codigo.trim().toUpperCase().replace(/^0+(?=.)/, '');
}
