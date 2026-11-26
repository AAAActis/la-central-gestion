import { Modulo } from './nucleo/acceso';
import { Articulos } from './pantallas/articulos/articulos';
import { Comparador } from './pantallas/comparador/comparador';
import { ComprasRegistradas } from './pantallas/compras-registradas/compras-registradas';
import { FacturaCompra } from './pantallas/factura-compra/factura-compra';
import { NotaCreditoCompra } from './pantallas/nota-credito-compra/nota-credito-compra';
import { NotasCreditoRegistradas } from './pantallas/notas-credito-registradas/notas-credito-registradas';
import { Clientes, Proveedores } from './pantallas/terceros/terceros';

/**
 * Los módulos del sistema. Un módulo nuevo se agrega acá y aparece en el menú y en las rutas;
 * los roles son los mismos que exige el [Authorize] de su controlador.
 */
export const MODULOS_DEL_SISTEMA: readonly Modulo[] = [
  {
    ruta: 'articulos', titulo: 'Artículos', bajada: 'Buscá repuestos por nombre o código y consultá su ficha.',
    roles: ['ADMINISTRADOR', 'OPERADOR'], componente: Articulos,
  },
  // [Authorize] sin roles en ClientesController y ProveedoresController: cualquier puesto autenticado.
  { ruta: 'clientes', titulo: 'Clientes', roles: ['ADMINISTRADOR', 'OPERADOR'], componente: Clientes },
  { ruta: 'proveedores', titulo: 'Proveedores', roles: ['ADMINISTRADOR', 'OPERADOR'], componente: Proveedores },
  {
    ruta: 'factura-compra', titulo: 'Factura de compra', bajada: 'Cargá el comprobante y asigná una sucursal de destino a cada artículo.',
    roles: ['ADMINISTRADOR', 'OPERADOR'], componente: FacturaCompra,
  },
  {
    ruta: 'compras-registradas', titulo: 'Compras registradas', bajada: 'Consultá las facturas de compra por proveedor, número o fechas.',
    roles: ['ADMINISTRADOR', 'OPERADOR'], componente: ComprasRegistradas,
  },
  {
    ruta: 'nota-credito-compra', titulo: 'Nota de crédito de proveedor', bajada: 'Registrá una bonificación o una devolución y las facturas a las que corresponde.',
    roles: ['ADMINISTRADOR', 'OPERADOR'], componente: NotaCreditoCompra,
  },
  {
    ruta: 'notas-credito-registradas', titulo: 'Notas de crédito registradas', bajada: 'Consultá las notas de crédito por proveedor o fecha y anulá una con autorización.',
    roles: ['ADMINISTRADOR', 'OPERADOR'], componente: NotasCreditoRegistradas,
  },
  {
    ruta: 'comparador', titulo: 'Comparador de precios', bajada: 'Mirá lo que cuesta un artículo en cada proveedor, del más barato al más caro.',
    roles: ['ADMINISTRADOR', 'OPERADOR'], componente: Comparador,
  },
];
