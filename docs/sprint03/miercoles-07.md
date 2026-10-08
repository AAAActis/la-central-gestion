# Miércoles 07/10/2026 — Javi — repositorio ART-02

## Entrega

1. ArticuloRepositorio con lectura real por EF, puerto y DTOs de consulta.
2. Ranking código interno exacto → código proveedor exacto → palabras del
   nombre. Alternativas OR, palabras AND, deduplicación por Id en SQL.
3. Activos por defecto, opción de incluir inactivos, orden ranking/nombre/Id,
   total y paginación después de unir todos los resultados.
4. Conflictos de claves normalizadas con todos los Ids, incluyendo inactivos
   y resultados fuera de la página. Sin fusión ni vinculación automática.
5. Detalle preparado con Id, nombre, código, estado, costo/último proveedor
   nullable, originales de códigos y existencias decimales por sucursal.
6. Registro de la dependencia en API, sin adelantar endpoints del jueves.

## Contrato compartido y dependencias

Esta rama se basa en la del martes (normalización). IConsultaArticulosRepositorio
se limita a la lectura y no colisiona con IArticuloRepositorio/Articulo de #103,
todavía pendientes de revisión comercial. ArticuloRepositorio queda como punto
de extensión de persistencia para Santi; no se implementan altas/modificaciones
ni se prometen operaciones de escritura que lanzarían NotImplementedException.
La coordinación externa con Santi queda pendiente: usar NormalizacionArticulo
al guardar, mantener código original + clave e índices de claves normalizadas.

Hasta que llegue el esquema D1, las claves se calculan al consultar sobre las
columnas existentes. No se modifican archivos scaffoldeados ni esquemas remotos.
No se supone que un índice del valor original acelere upper(btrim(columna)).
SQL de comparación y fixture están en docs/datos-prueba/articulos-busqueda.sql.
La equivalencia de mayúsculas para códigos no latinos requiere validación del
collation de PostgreSQL con los códigos reales del export antes de incorporarlos.

PreciosPorModalidad es null: el esquema legado no identifica las modalidades
de D1. No se deriva una modalidad del precio estimado antiguo, no se presenta
cero ni se calcula la fórmula retirada. La ficha final y razones de ausencia
se completan el viernes sobre el contrato de precios acordado.

## Verificación y límites

Tests InMemory: ranking, alternativas, activos, paginación, colisiones, costo
nullable y stock decimal. ToQueryString verifica traducción PostgreSQL sin
conectarse: no acredita ejecución, índices ni tiempos.
Pruebas en PostgreSQL exclusivo por Tailscale y medición de planes pendientes
de acceso/configuración. No se cierra #15 ni FCO-01/ART-05 por esta entrega.

El test PostgreSQL queda preparado y se omite explícitamente sin la variable
LA_CENTRAL_TEST_POSTGRES. Configurar esa conexión fuera del repositorio; la
base debe llamarse test_* o *_test. El test crea y elimina únicamente un
esquema art02_test_<guid>, con tablas mínimas de lectura y restricción D2.
No usa EnsureDeleted/EnsureCreated sobre la base compartida ni agrega librerías.
No valida la migración completa D1 ni la escritura de ART-01.

## PR

Título: feat(articulos): implementar repositorio de búsqueda paginada ART-02

Descripción: Agrega el contrato de lectura y ArticuloRepositorio con búsqueda
por códigos normalizados y palabras del nombre, alternativas deduplicadas,
activos, ranking estable, total y paginación. Informa conflictos sin fusionar
artículos y prepara detalle nullable con códigos originales y stock por
sucursal. Se apoya en la preparación del martes. No incluye endpoints,
escritura ni precios comerciales; la validación PostgreSQL real sigue pendiente.
Relacionado con #15; no cierra la historia.
