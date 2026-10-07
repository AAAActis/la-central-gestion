# Martes 06/10/2026 — Javi — preparación ART-02

## Entrega

1. Normalización reutilizable para alta/modificación y búsqueda: código con
   espacios exteriores U+0020 retirados y mayúsculas invariantes; guiones,
   ceros y escritura original conservados. PostgreSQL compara upper(btrim(codigo)).
2. Nombre buscable por todas las palabras de cada alternativa, sin distinguir
   mayúsculas ni vocales acentuadas españolas. Ñ se conserva; GENIOS no se
   convierte a GENIUS. Las palabras son literales, no comodines SQL.
3. `*` separa alternativas OR; los términos internos son AND. Se ignoran
   fragmentos vacíos y duplicados. Unión por Id estable; nunca por similitud.
4. Fixture de 40 artículos con marcas/modelos inventados para pruebas,
   códigos sintéticos, costo nullable y artículo inactivo. Ningún código se
   presenta como extraído del catálogo real de un proveedor.
5. Consultas SQL y EXPLAIN (ANALYZE, BUFFERS) reproducibles en tablas temporales.
   No modifican tablas del negocio ni requieren una nueva extensión.

## Ranking y colisiones

Código interno exacto (0), código de proveedor exacto (1), todas las palabras
del nombre (2). Los candidatos de todas las alternativas se unen por Id y
se conserva el mejor ranking. Orden por ranking/nombre/Id antes de paginar;
se informa total. Una clave normalizada que apunta a varios artículos se
informa como conflicto y conserva sus Ids, sin selección automática.

D2 mantiene un código vigente por proveedor/artículo. Encontrar un candidato
no autoriza una vinculación. El constructor/flujo de alta de Santi debe usar
la misma normalización y conservar el original; su implementación no está
incluida ni se afirma coordinada por mensajes externos.

## Ejecución y evidencia pendiente

`psql -v ON_ERROR_STOP=1 -f docs/datos-prueba/articulos-busqueda.sql`

El SQL queda preparado; no se presentan tiempos medidos ni planes reales
hasta ejecutarlo en PostgreSQL exclusivo de test por Tailscale. Una tabla
pequeña puede usar Seq Scan aunque exista un índice. El volumen real del
export será necesario para la evaluación de rendimiento.

Export real: pendiente de solicitud/recepción por el equipo. Solicitar copia
CSV con nombre, marca/modelo/medida y códigos originales por proveedor, origen
y fecha. No se envió ningún mensaje a la empresa ni se usaron datos reales.
El costo del fixture no sustituye la demostración compra → costo de FCO-01.

## PR

Título: test(articulos): preparar normalización y consultas de búsqueda ART-02

Descripción: Prepara ART-02 con normalización compartida y pruebas de
conservación de identidad, alternativas con asteriscos y nombres por palabras.
Agrega 40 artículos sintéticos y SQL de comparación de consultas/planes sin
tocar el catálogo real. Export y mediciones PostgreSQL quedan explícitamente
pendientes; no cierra #15.
