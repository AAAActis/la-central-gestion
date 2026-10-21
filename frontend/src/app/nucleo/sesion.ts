import { HttpInterceptorFn } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';

/** Lo que devuelve POST /api/acceso/login (SesionDto). */
export interface Sesion {
  token: string;
  nombreUsuario: string;
  rol: string;
  sucursalId: number;
}

/**
 * La sesión vive SOLO en memoria: sin localStorage ni cookies. Recargar la página
 * obliga a volver a entrar.
 */
@Injectable({ providedIn: 'root' })
export class SesionActual {
  readonly sesion = signal<Sesion | null>(null);

  iniciar(sesion: Sesion) {
    this.sesion.set(sesion);
  }

  cerrar() {
    this.sesion.set(null);
  }
}

/** Agrega el Bearer a toda llamada a /api, salvo al login. */
export const agregarToken: HttpInterceptorFn = (peticion, siguiente) => {
  const token = inject(SesionActual).sesion()?.token;
  if (!token || peticion.url.endsWith('/api/acceso/login')) return siguiente(peticion);
  return siguiente(peticion.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
