import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { Sesion } from './sesion';
import type { Pagina } from '../compartido/listado';

export interface CodigoProveedorResumen {
  proveedorId: number;
  razonSocial: string;
  codigo: string;
}

export interface ArticuloResultado {
  id: number;
  codigoInterno: string;
  nombre: string;
  activo: boolean;
  codigosProveedor: CodigoProveedorResumen[];
}

export interface PaginaArticulos {
  articulos: ArticuloResultado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
  conflictos: { claveNormalizada: string; articuloIds: number[] }[];
}

export type Modalidad =
  | 'Lista' | 'Tarjeta3' | 'Tarjeta6' | 'EfectivoTransferencia' | 'Costo' | 'CasoEspecial';
export type RazonAusencia = 'SinConfiguracion' | 'SinCosto' | 'CasoEspecialNoFijado' | 'TasaSinDefinir';

export interface PrecioFicha {
  modalidad: Modalidad;
  base: 'CostoArticulo' | 'PrecioLista' | null;
  porcentaje: number | null;
  precio: number | null;
  calculoManual: boolean;
  fechaCalculo: string | null;
  usuarioId: number | null;
  razonAusencia: RazonAusencia | null;
}

export interface FichaArticulo {
  id: number;
  codigoInterno: string;
  nombre: string;
  activo: boolean;
  costoVigente: number | null;
  ultimoProveedorId: number | null;
  codigos: { proveedorId: number; razonSocial: string; codigoOriginal: string; claveNormalizada: string }[];
  existencias: { sucursalId: number; sucursal: string; cantidad: number; ubicacionDeposito: string | null }[];
  totalExistencias: number;
  precios: PrecioFicha[];
  motivoBaja: string | null;
}

/** 016 · un registro del historial de compras (solo lectura: lo escribe la factura de compra). */
export interface RegistroHistorialCompra {
  fecha: string;
  proveedorId: number;
  proveedor: string;
  precioCosto: number;
  facturaCompraId: number;
  numeroFactura: string;
  anulado: boolean;
}

/** La ubicación no va en el alta: se carga desde la ficha, por sucursal (N-9). */
export interface AltaArticulo {
  nombre: string;
  codigosProveedor: { proveedorId: number; codigo: string }[];
}

/** PUT /api/articulos/{id}. codigoInterno null = no se cambia; los códigos omitidos NO se retiran. */
export interface ModificacionArticulo {
  nombre: string;
  codigoInterno: string | null;
  codigosProveedor: { proveedorId: number; codigo: string }[];
}

export interface ProveedorResumen {
  id: number;
  codigo: string;
  razonSocial: string;
  cuit: string | null;
  activo: boolean;
  /** HU-FCO-06: lo que el proveedor suele emitir. Solo un valor por defecto. */
  tipoComprobanteHabitual?: 'A' | 'B' | null;
}

/** 045 · clientes y proveedores comparten pantalla: lo que difiere lo pone la configuración. */
export type BaseTerceros = 'clientes' | 'proveedores';

export interface TerceroResumen {
  id: number;
  codigo: string;
  razonSocial: string;
  cuit: string | null;
  activo: boolean;
}

/** La ficha de `GET /api/{clientes|proveedores}/{id}`. Cada base trae sus campos propios. */
export interface FichaTercero {
  codigo: string;
  razonSocial: string;
  cuit: string | null;
  activo: boolean;
  telefonos: string[];
  direcciones: string[];
  condicionFiscal?: string | null;
  condicionPago?: string | null;
  urlReferencia?: string | null;
  tipoComprobanteHabitual?: 'A' | 'B' | null;
  motivoBaja?: string | null;
}

export interface LineaFacturaCompra {
  articuloId: number;
  sucursalId: number;
  cantidad: number;
  precioUnitario: number;
  descuentoPorcentaje: number;
  descuento2Porcentaje: number;
  descuento3Porcentaje: number;
  /** D-S6-15: el externo por marca o por artículo. */
  descuento4Porcentaje: number;
}

export interface FacturaCompraNueva {
  proveedorId: number;
  numero: string;
  tipoComprobante: string;
  fechaEmision: string;
  fechaVencimiento: string | null;
  fechaImputacion: string | null;
  percepcionIva: number;
  percepcionIibb: number;
  percepcionMunicipal: number;
  percepcionGanancias: number;
  lineas: LineaFacturaCompra[];
  /** HU-FCO-05 (029) CA-006: se cargó desde el PDF del proveedor. */
  desdePdf?: boolean;
}

/** HU-FCO-05 (029): una línea leída del PDF; sin `articuloId`, el código del proveedor no tiene artículo. */
export interface LineaPdfInterpretada {
  codigoProveedor: string;
  descripcion: string;
  articuloId: number | null;
  cantidad: number;
  precioUnitario: number;
  descuentoPorcentaje: number;
  descuento2Porcentaje: number;
  descuento3Porcentaje: number;
  /** D-S6-15: el externo por marca o por artículo. */
  descuento4Porcentaje: number;
}

/** HU-FCO-05 (029): lo que devuelve `POST /api/facturas-compra/pdf`. Nada quedó guardado. */
export interface FacturaPdfInterpretada {
  proveedorId: number | null;
  tipoComprobante: 'A' | 'B' | null;
  numero: string | null;
  fechaEmision: string | null;
  fechaVencimiento: string | null;
  percepcionIva: number | null;
  percepcionIibb: number | null;
  percepcionMunicipal: number | null;
  percepcionGanancias: number | null;
  lineas: LineaPdfInterpretada[];
  /** Campos que el sistema no pudo afirmar: `proveedorId`, `tipoComprobante`, `numero`, `fechaEmision`, `lineas[i]`. */
  paraRevisar: string[];
}

/** HU-FCO-06 CA-015: lo que devuelve POST /api/facturas-compra/calculo. El IVA lo decide la API. */
export interface TotalesFacturaCompra {
  neto: number;
  iva: number;
  percepciones: number;
  total: number;
}

@Injectable({ providedIn: 'root' })
export class ApiLaCentral {
  private readonly http = inject(HttpClient);

  login(nombreUsuario: string, contrasena: string): Observable<Sesion> {
    return this.http.post<Sesion>('/api/acceso/login', { nombreUsuario, contrasena });
  }

  buscar(texto: string, incluirInactivos: boolean, pagina: number, tamanoPagina: number, aproximada = false): Observable<PaginaArticulos> {
    let params = new HttpParams()
      .set('texto', texto)
      .set('incluirInactivos', incluirInactivos)
      .set('pagina', pagina)
      .set('tamanoPagina', tamanoPagina);
    // HU-FCO-06 CA-008: solo la factura de compra pide coincidencias aproximadas.
    if (aproximada) params = params.set('aproximada', true);
    return this.http.get<PaginaArticulos>('/api/articulos', { params });
  }

  ficha(id: number): Observable<FichaArticulo> {
    return this.http.get<FichaArticulo>(`/api/articulos/${id}`);
  }

  historialCompras(id: number): Observable<RegistroHistorialCompra[]> {
    return this.http.get<RegistroHistorialCompra[]>(`/api/articulos/${id}/historial-compras`);
  }

  crear(alta: AltaArticulo): Observable<{ id: number; codigoInterno: string }> {
    return this.http.post<{ id: number; codigoInterno: string }>('/api/articulos', alta);
  }

  modificar(id: number, cambios: ModificacionArticulo): Observable<void> {
    return this.http.put<void>(`/api/articulos/${id}`, cambios);
  }

  /** N-9 · ubicación en depósito de ESA sucursal. null la borra. */
  asignarUbicacion(id: number, sucursalId: number, ubicacionDeposito: string | null): Observable<void> {
    return this.http.put<void>(`/api/articulos/${id}/ubicaciones/${sucursalId}`, { ubicacionDeposito });
  }

  /** Solo ADMINISTRADOR: lo decide la API con el rol de la sesión. */
  retirarCodigo(id: number, proveedorId: number): Observable<void> {
    return this.http.delete<void>(`/api/articulos/${id}/codigos/${proveedorId}`);
  }

  /** 014 · `confirmacion` es el código interno reescrito (CA-004): lo compara la API. */
  darDeBaja(id: number, motivo: string, confirmacion: string): Observable<void> {
    return this.http.post<void>(`/api/articulos/${id}/baja`, { motivo, confirmacion });
  }

  reactivar(id: number): Observable<void> {
    return this.http.post<void>(`/api/articulos/${id}/reactivacion`, null);
  }

  /**
   * Solo activos: a un proveedor inactivo no se le registran compras. La primera página (#147).
   * HU-FCO-06 CA-001: por código o nombre, sin CUIT.
   */
  proveedores(texto: string): Observable<ProveedorResumen[]> {
    const params = new HttpParams().set('texto', texto).set('incluirInactivos', false).set('porCuit', false);
    return this.http.get<{ items: ProveedorResumen[] }>('/api/proveedores', { params }).pipe(map(p => p.items));
  }

  terceros(base: BaseTerceros, texto: string, incluirInactivos: boolean, pagina: number, tamanoPagina: number) {
    const params = new HttpParams().set('texto', texto).set('incluirInactivos', incluirInactivos)
      .set('pagina', pagina).set('tamanoPagina', tamanoPagina);
    return this.http.get<Pagina<TerceroResumen>>(`/api/${base}`, { params });
  }

  fichaTercero(base: BaseTerceros, id: number): Observable<FichaTercero> {
    return this.http.get<FichaTercero>(`/api/${base}/${id}`);
  }

  crearTercero(base: BaseTerceros, alta: Record<string, unknown>): Observable<unknown> {
    return this.http.post(`/api/${base}`, alta);
  }

  modificarTercero(base: BaseTerceros, id: number, cambios: Record<string, unknown>): Observable<void> {
    return this.http.put<void>(`/api/${base}/${id}`, cambios);
  }

  darDeBajaTercero(base: BaseTerceros, id: number, baja: Record<string, string>): Observable<void> {
    return this.http.post<void>(`/api/${base}/${id}/baja`, baja);
  }

  reactivarTercero(base: BaseTerceros, id: number): Observable<void> {
    return this.http.post<void>(`/api/${base}/${id}/reactivacion`, null);
  }

  /** HU-FCO-02: solo viajan los criterios que tienen valor. */
  facturasCompra(filtro: FiltroFacturasCompra, pagina: number, tamanoPagina: number): Observable<Pagina<FacturaCompraResumen>> {
    let params = new HttpParams().set('pagina', pagina).set('tamanoPagina', tamanoPagina);
    for (const [clave, valor] of Object.entries(filtro))
      if (valor !== null && valor !== undefined && String(valor).trim() !== '') params = params.set(clave, String(valor).trim());
    return this.http.get<Pagina<FacturaCompraResumen>>('/api/facturas-compra', { params });
  }

  /** HU-FCO-04 (028): quien anula sale de la sesión; autoriza otro ADMINISTRADOR con su usuario y contraseña. */
  anularFacturaCompra(id: number, anulacion: AnulacionFacturaCompra): Observable<void> {
    return this.http.post<void>(`/api/facturas-compra/${id}/anulacion`, anulacion);
  }

  registrarFacturaCompra(factura: FacturaCompraNueva): Observable<{ id: number }> {
    return this.http.post<{ id: number }>('/api/facturas-compra', factura);
  }

  /** HU-FCO-05 (029): el PDF viaja como formulario, en el campo «archivo». */
  /** Con proveedorId, los códigos de las líneas se buscan en ese proveedor (uno elegido a mano). */
  interpretarFacturaPdf(archivo: File, proveedorId?: number): Observable<FacturaPdfInterpretada> {
    const formulario = new FormData();
    formulario.append('archivo', archivo);
    if (proveedorId !== undefined) formulario.append('proveedorId', String(proveedorId));
    return this.http.post<FacturaPdfInterpretada>('/api/facturas-compra/pdf', formulario);
  }

  calcularFacturaCompra(factura: FacturaCompraNueva): Observable<TotalesFacturaCompra> {
    return this.http.post<TotalesFacturaCompra>('/api/facturas-compra/calculo', factura);
  }

  /** HU-FCO-07 (057): el usuario sale de la sesión; es siempre A, así que no viaja el tipo. */
  registrarNotaCreditoCompra(nota: NotaCreditoCompraNueva): Observable<{ id: number }> {
    return this.http.post<{ id: number }>('/api/notas-credito-compra', nota);
  }

  /** 057 CA-007: neto, 21 % de IVA y percepciones, sin guardar nada. */
  calcularNotaCreditoCompra(nota: NotaCreditoCompraNueva): Observable<TotalesNotaCreditoCompra> {
    return this.http.post<TotalesNotaCreditoCompra>('/api/notas-credito-compra/calculo', nota);
  }
}

/** HU-FCO-07 (057): un artículo devuelto; sale del stock de sucursalId (una sucursal o Reclamos). */
export interface LineaNotaCreditoCompra {
  articuloId: number;
  sucursalId: number;
  cantidad: number;
}

/** 057: el cuerpo de `POST /api/notas-credito-compra`. La bonificación va sin líneas. */
export interface NotaCreditoCompraNueva {
  proveedorId: number;
  numero: string;
  fechaEmision: string;
  clase: 'BONIFICACION' | 'DEVOLUCION';
  importeNeto: number;
  percepcionIva: number | null;
  percepcionIibb: number | null;
  percepcionMunicipal: number | null;
  percepcionGanancias: number | null;
  facturaIds: number[];
  lineas: LineaNotaCreditoCompra[];
}

export interface TotalesNotaCreditoCompra {
  neto: number;
  iva: number;
  percepciones: number;
  total: number;
}

/** HU-FCO-04 (028): el cuerpo de `POST /api/facturas-compra/{id}/anulacion`. */
export interface AnulacionFacturaCompra {
  motivo: string;
  usuarioAutoriza: string;
  contrasenaAutoriza: string;
}

/** HU-FCO-02 (026): criterios de `GET /api/facturas-compra`; las fechas en ISO (aaaa-mm-dd). */
export interface FiltroFacturasCompra {
  proveedorId?: number | null;
  numero?: string;
  emisionDesde?: string;
  emisionHasta?: string;
  vencimientoDesde?: string;
  vencimientoHasta?: string;
}

export interface FacturaCompraResumen {
  id: number;
  proveedorId: number;
  proveedor: string;
  numero: string;
  tipoComprobante: string;
  fechaEmision: string;
  fechaVencimiento: string | null;
  anulada: boolean;
}

/** El detalle de un ProblemDetails, o un texto genérico según el código. */
export function mensajeDeError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'Error inesperado.';
  const cuerpo = error.error;
  if (cuerpo?.detail) return cuerpo.detail;
  if (cuerpo?.errors) return Object.values(cuerpo.errors as Record<string, string[]>).flat().join(' ');
  switch (error.status) {
    case 0: return 'No hay conexión con la API. ¿Está levantada en el puerto 5181?';
    case 401: return 'La sesión no es válida o venció. Volvé a entrar.';
    case 403: return 'Tu rol no tiene permiso para esta operación.';
    case 404: return 'No existe.';
    default: return `Error ${error.status}.`;
  }
}
