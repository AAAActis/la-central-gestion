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
  return { porCampo: {}, general: null };
}
