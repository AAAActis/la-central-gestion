# Controladores

Uno por recurso, en plural: UsuariosController, ArticulosController.

Responsabilidad: recibir la petición HTTP, mapear el DTO de entrada al
caso de uso, y traducir el Result que devuelve a una respuesta HTTP.

No va acá: reglas de negocio, consultas a la base, using de EF Core.