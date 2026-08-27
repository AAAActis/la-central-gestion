# DTOs

Contratos de entrada y salida de la API, agrupados por recurso.

  CrearUsuarioRequest   - cuerpo de la petición
  UsuarioResponse       - cuerpo de la respuesta

Las entidades de LaCentral.UseCases.Models NUNCA se devuelven
directamente. Tres razones:

1. Usuario expone hash_contrasena.
2. Las propiedades de navegación son bidireccionales: el serializador
   entra en recursión infinita.
3. Si el contrato de la API es la entidad, cualquier cambio del esquema
   rompe al frontend.