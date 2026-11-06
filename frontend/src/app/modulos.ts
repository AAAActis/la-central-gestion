import { Modulo } from './nucleo/acceso';
import { Articulos } from './pantallas/articulos/articulos';
import { FacturaCompra } from './pantallas/factura-compra/factura-compra';
import { Clientes, Proveedores } from './pantallas/terceros/terceros';

/**
 * Los módulos del sistema. Un módulo nuevo se agrega acá y aparece en el menú y en las rutas;
 * los roles son los mismos que exige el [Authorize] de su controlador.
 */
export const MODULOS_DEL_SISTEMA: readonly Modulo[] = [
  { ruta: 'articulos', titulo: 'Artículos', roles: ['ADMINISTRADOR', 'OPERADOR'], componente: Articulos },
  // [Authorize] sin roles en ClientesController y ProveedoresController: cualquier puesto autenticado.
  { ruta: 'clientes', titulo: 'Clientes', roles: ['ADMINISTRADOR', 'OPERADOR'], componente: Clientes },
  { ruta: 'proveedores', titulo: 'Proveedores', roles: ['ADMINISTRADOR', 'OPERADOR'], componente: Proveedores },
  { ruta: 'factura-compra', titulo: 'Factura de compra', roles: ['ADMINISTRADOR', 'OPERADOR'], componente: FacturaCompra },
];
