import { Component, signal } from '@angular/core';
import { ArticuloResultado, FacturaCompraResumen, NotaCreditoCompraNueva, ProveedorResumen, TotalesNotaCreditoCompra } from '../../nucleo/api';

interface LineaDevuelta { articulo: ArticuloResultado; sucursalId: number | null; cantidad: number | null; }
interface Origen { id: number; nombre: string; }

/** HU-FCO-07 (057) · carga manual de la nota de crédito de proveedor. Esqueleto: sin comportamiento. */
@Component({
  selector: 'app-nota-credito-compra',
  template: `<section class="tarjeta"><h2>Nota de crédito de proveedor</h2></section>`,
})
export class NotaCreditoCompra {
  textoProveedor = '';
  readonly proveedores = signal<ProveedorResumen[]>([]);
  readonly errorProveedor = signal<string | null>(null);
  readonly proveedor = signal<ProveedorResumen | null>(null);
  readonly facturas = signal<FacturaCompraResumen[]>([]);
  readonly errorFacturas = signal<string | null>(null);
  readonly marcadas = signal<ReadonlySet<number>>(new Set());
  numero = '';
  fechaEmision = '';
  clase: 'BONIFICACION' | 'DEVOLUCION' = 'BONIFICACION';
  importeNeto: number | null = null;
  percepciones: { iva: number | null; iibb: number | null; municipal: number | null; ganancias: number | null } =
    { iva: null, iibb: null, municipal: null, ganancias: null };
  textoArticulo = '';
  readonly articulos = signal<ArticuloResultado[]>([]);
  lineas: LineaDevuelta[] = [];
  readonly origenes = signal<Origen[]>([]);
  readonly totales = signal<TotalesNotaCreditoCompra | null>(null);
  readonly error = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly registrada = signal<string | null>(null);

  buscarProveedor() {}
  elegirProveedor(_p: ProveedorResumen) {}
  cambiarProveedor() {}
  alternarFactura(_id: number) {}
  buscarArticulo() {}
  agregarArticulo(_a: ArticuloResultado) {}
  quitarLinea(_i: number) {}
  nota(): NotaCreditoCompraNueva | null { return null; }
  calcular() {}
  invalidarTotales() {}
  registrar() {}
  nueva() {}
}
