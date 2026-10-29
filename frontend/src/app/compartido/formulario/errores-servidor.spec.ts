import { HttpErrorResponse } from '@angular/common/http';
import { erroresDelServidor } from './errores-servidor';
import { CampoFormulario } from './formulario';

const CAMPOS: CampoFormulario[] = [
  { nombre: 'nombre', etiqueta: 'Nombre' },
  { nombre: 'codigosProveedor', etiqueta: 'Códigos de proveedor', multivaluado: true },
];

describe('erroresDelServidor', () => {
  // Las claves son las que fija el contrato del 400 en ArticulosApiPostgreSqlTests.
  it('los errors de un 400 de validación van a su campo por el nombre de la propiedad', () => {
    const respuesta = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        errors: {
          Nombre: ['El nombre es obligatorio.'],
          'CodigosProveedor[0].Codigo': ['El código de proveedor no puede superar los 60 caracteres.'],
          'CodigosProveedor[1].Codigo': ['El código de proveedor es obligatorio.'],
          UbicacionDeposito: ['La ubicación en depósito no puede superar los 60 caracteres.'],
        },
      },
    });

    expect(erroresDelServidor(respuesta, CAMPOS)).toEqual({
      porCampo: {
        nombre: 'El nombre es obligatorio.',
        codigosProveedor: 'El código de proveedor no puede superar los 60 caracteres. El código de proveedor es obligatorio.',
      },
      general: 'La ubicación en depósito no puede superar los 60 caracteres.',
    });
  });

  it('el detail de un ProblemDetails (regla de negocio) va al mensaje general: hoy no dice el campo', () => {
    const respuesta = new HttpErrorResponse({ status: 409, error: { title: 'Conflict', detail: 'Ya existe un artículo con ese código.' } });

    expect(erroresDelServidor(respuesta, CAMPOS)).toEqual({ porCampo: {}, general: 'Ya existe un artículo con ese código.' });
  });

  it('sin cuerpo, el general es el mensaje habitual según el estado', () => {
    expect(erroresDelServidor(new HttpErrorResponse({ status: 403 }), CAMPOS))
      .toEqual({ porCampo: {}, general: 'Tu rol no tiene permiso para esta operación.' });
  });
});
