import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { catchError, throwError } from 'rxjs';

/** Lo que devuelve POST /api/acceso/login (SesionDto). */
export interface Sesion {
  token: string;
  nombreUsuario: string;
  rol: string;
  sucursalId: number;
}

/** Dónde se guarda la sesión en sessionStorage. */
export const CLAVE_SESION = 'lacentral.sesion';
export const AVISO_SESION_VENCIDA = 'Tu sesión venció. Volvé a entrar.';

function leerGuardada(): Sesion | null {
  try {
    return JSON.parse(sessionStorage.getItem(CLAVE_SESION) ?? 'null');
  } catch {
    return null;
  }
}

/**
 * La sesión vive en sessionStorage (HU-UI-03): sobrevive a F5 en la misma pestaña y muere al
 * cerrarla. El token no se decodifica en el navegador: el rol es el que devolvió el login.
 * shortcut: el token es legible por JavaScript (expuesto a XSS); lo riguroso es una cookie
 * httpOnly con refresco y protección CSRF, deuda registrada en ESTADO.md para el RNF de seguridad.
 */
@Injectable({ providedIn: 'root' })
export class SesionActual {
  readonly sesion = signal<Sesion | null>(leerGuardada());
  /** Por qué terminó la última sesión, si no fue por Salir: lo informa la pantalla de acceso. */
  readonly avisoAlCerrar = signal<string | null>(null);

  iniciar(sesion: Sesion) {
    // Si el navegador no deja guardar, la sesión sigue en memoria: F5 vuelve a pedir el ingreso.
    try { sessionStorage.setItem(CLAVE_SESION, JSON.stringify(sesion)); } catch { /* sin almacenamiento */ }
    this.avisoAlCerrar.set(null);
    this.sesion.set(sesion);
  }

  cerrar(aviso: string | null = null) {
    try { sessionStorage.removeItem(CLAVE_SESION); } catch { /* sin almacenamiento */ }
    this.avisoAlCerrar.set(aviso);
    this.sesion.set(null);
  }
}

/**
 * Agrega el Bearer a toda llamada a /api, salvo al login. Un 401 con ese token es una sesión
 * vencida: se cierra y el acceso lo informa (CA-002). Un 403 no: la sesión vale, el rol no alcanza.
 */
export const agregarToken: HttpInterceptorFn = (peticion, siguiente) => {
  const sesion = inject(SesionActual);
  const token = sesion.sesion()?.token;
  if (!token || peticion.url.endsWith('/api/acceso/login')) return siguiente(peticion);
  return siguiente(peticion.clone({ setHeaders: { Authorization: `Bearer ${token}` } })).pipe(
    catchError(error => {
      // Solo si sigue siendo la sesión que mandó el pedido: una respuesta tardía no cierra la nueva.
      if (error instanceof HttpErrorResponse && error.status === 401 && sesion.sesion()?.token === token)
        sesion.cerrar(AVISO_SESION_VENCIDA);
      return throwError(() => error);
    }),
  );
};
