import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Sesion } from './sesion';

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
}

export interface LineaFacturaCompra {
  articuloId: number;
  sucursalId: number;
  cantidad: number;
  precioUnitario: number;
  descuentoPorcentaje: number;
}

export interface FacturaCompraNueva {
  proveedorId: number;
  numero: string;
  fechaEmision: string;
  fechaVencimiento: string | null;
  percepcionIva: number;
  percepcionIibb: number;
  percepcionMunicipal: number;
  percepcionGanancias: number;
  lineas: LineaFacturaCompra[];
}

@Injectable({ providedIn: 'root' })
export class ApiLaCentral {
  private readonly http = inject(HttpClient);

  login(nombreUsuario: string, contrasena: string): Observable<Sesion> {
    return this.http.post<Sesion>('/api/acceso/login', { nombreUsuario, contrasena });
  }

  buscar(texto: string, incluirInactivos: boolean, pagina: number, tamanoPagina: number): Observable<PaginaArticulos> {
    const params = new HttpParams()
      .set('texto', texto)
      .set('incluirInactivos', incluirInactivos)
      .set('pagina', pagina)
      .set('tamanoPagina', tamanoPagina);
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

  darDeBaja(id: number, motivo: string): Observable<void> {
    return this.http.post<void>(`/api/articulos/${id}/baja`, { motivo });
  }

  reactivar(id: number): Observable<void> {
    return this.http.post<void>(`/api/articulos/${id}/reactivacion`, null);
  }

  /** Solo activos: a un proveedor inactivo no se le registran compras. 404 si no hay ninguno. */
  proveedores(texto: string): Observable<ProveedorResumen[]> {
    const params = new HttpParams().set('texto', texto).set('incluirInactivos', false);
    return this.http.get<ProveedorResumen[]>('/api/proveedores', { params });
  }

  registrarFacturaCompra(factura: FacturaCompraNueva): Observable<{ id: number }> {
    return this.http.post<{ id: number }>('/api/facturas-compra', factura);
  }
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
