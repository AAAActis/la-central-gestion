import { Component } from '@angular/core';

/** HU-FCO-02 (026) · Consulta de facturas de compra registradas. */
@Component({
  selector: 'app-compras-registradas',
  template: ``,
})
export class ComprasRegistradas {
  textoProveedor = '';
  numero = '';
  emisionDesde = '';
  emisionHasta = '';
  vencimientoDesde = '';
  vencimientoHasta = '';

  buscarProveedor() {}
}
