import { HttpErrorResponse } from '@angular/common/http';
import { mensajeDeError } from '../../nucleo/api';
import { CampoFormulario } from './formulario';

export interface ErroresServidor {
  porCampo: Record<string, string>;
  general: string | null;
}

/**
 * Reparte una respuesta de error de la API entre los campos y el mensaje general: los `errors`
 * de un 400 de validación van a su campo por el nombre de la propiedad; el `detail` de un
 * ProblemDetails, al general. No interpreta el texto de ningún mensaje.
 */
export function erroresDelServidor(error: unknown, campos: readonly CampoFormulario[]): ErroresServidor {
  const errores = error instanceof HttpErrorResponse ? (error.error?.errors as Record<string, string[]> | undefined) : undefined;
  if (!errores) return { porCampo: {}, general: mensajeDeError(error) };

  const porCampo: Record<string, string> = {};
  const sueltos: string[] = [];
  for (const [clave, mensajes] of Object.entries(errores)) {
    // "CodigosProveedor[0].Codigo" o "$.nombre": cuenta la propiedad raíz, sin distinguir mayúsculas.
    const raiz = clave.replace(/^\$\./, '').split(/[.[]/)[0].toLowerCase();
    const campo = campos.find(c => c.nombre.toLowerCase() === raiz);
    if (campo) porCampo[campo.nombre] = [porCampo[campo.nombre], ...mensajes].filter(Boolean).join(' ');
    else sueltos.push(...mensajes);
  }
  return { porCampo, general: sueltos.length ? sueltos.join(' ') : null };
}
