import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { rutasDe } from './app.routes';
import { MODULOS_DEL_SISTEMA } from './modulos';
import { MODULOS } from './nucleo/acceso';
import { agregarToken } from './nucleo/sesion';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideHttpClient(withInterceptors([agregarToken])),
    provideRouter(rutasDe(MODULOS_DEL_SISTEMA)),
    { provide: MODULOS, useValue: MODULOS_DEL_SISTEMA },
  ]
};
