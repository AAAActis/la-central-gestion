# Entregas de Javi — 05 al 07 de octubre de 2026

Una rama y un PR por día. Las ramas se crearon localmente; no se publicaron
PR ni se cambiaron issues, puntos, responsables o milestones.

| Día | Rama | Base del PR | Validación |
| --- | --- | --- | --- |
| Lunes | fix/hu-cli-05-historial-reactivacion | develop | 66 pasan; regresión falla al retirar el mapeo |
| Martes | feature/hu-art-02-preparacion-busqueda | develop | 71 pasan |
| Miércoles | feature/hu-art-02-repositorio-busqueda | feature/hu-art-02-preparacion-busqueda | 83 pasan; 1 PostgreSQL omitida por falta de conexión |

Si el PR del martes se integra primero, actualizar la base del miércoles a
develop y revisar su diff. No mezclar los commits del lunes en el PR del martes.
Para revisar una entrega local: git switch <rama>.

## PR lunes

**Título:** test(clientes): verificar historial de reactivación y retiro de test-db

El PR #104 ya corrigió el mapeo y retiró /api/test-db. Esta entrega completa
la verificación de las tareas del lunes: comprueba motivo y fecha con el
repositorio real InMemory y el caso de uso de reactivación, relee con otro
contexto, refuerza las aserciones del ciclo HTTP y verifica 404 en la ruta
retirada. No duplica las correcciones ni cierra las historias completas.

Validación: 66 tests pasan. Al retirar temporalmente el mapeo, la regresión
falla; el archivo fue restaurado. Persistencia PostgreSQL aún pendiente.

## PR martes

**Título:** test(articulos): preparar normalización y consultas de búsqueda ART-02

Prepara la búsqueda con reglas compartidas de normalización, preservación de
guiones/ceros y alternativas separadas por asteriscos. Agrega tests, un fixture
de 40 artículos sintéticos y consultas SQL con EXPLAIN ANALYZE reproducibles
sin tocar el catálogo real. Registra los pendientes de export y coordinación.

Validación: 71 tests pasan. No se ejecutaron las mediciones PostgreSQL ni se
recibió el export de la empresa. Relacionado con #15; no cierra la historia.

## PR miércoles

**Título:** feat(articulos): implementar repositorio de búsqueda paginada ART-02

Implementa ArticuloRepositorio y un contrato de lectura independiente del
agregado de escritura todavía en revisión en #103. Prioriza código interno,
código de proveedor y palabras del nombre; une alternativas por Id, filtra
activos y pagina después de ordenar. Informa colisiones sin fusionar artículos.
Prepara detalle nullable con códigos originales y stock decimal por sucursal.
Registra la dependencia en API; no adelanta endpoints ni reglas comerciales.

Validación: 83 tests pasan y 1 PostgreSQL queda omitido por falta de conexión.
Se verifica también la traducción SQL con el proveedor Npgsql. El test real
queda listo mediante LA_CENTRAL_TEST_POSTGRES en una base exclusiva test_* o
*_test, creando y limpiando únicamente un esquema propio. Sin librerías nuevas.
Relacionado con #15; no cierra ART-02.

## Estado pendiente

- Obtener acceso/configuración de PostgreSQL de test por Tailscale y ejecutar
  las pruebas/planes allí; no usar la base de demostración.
- Solicitar/recibir export real y confirmar equivalencias de códigos con cada
  proveedor. Los datos del fixture son sintéticos.
- Coordinar con Santi persistencia de originales/claves e índices y normalización
  al guardar. La implementación de escritura no está en esta entrega.
- Contrato comercial corregido de #103 y esquema D1; precios por modalidad
  quedan null hasta que haya datos y mapeo definidos.
- Actualizaciones compartidas de GitHub, CU-007, proformas y GR-01, y capacidad
  reestimada por el equipo. No se inventaron estimaciones.

## Aprendizajes del proyecto

La traducción correcta a SQL no acredita ejecución ni uso de índices. Los
tests InMemory no validan D2 en PostgreSQL. El orden de operaciones de búsqueda
es unir alternativas, agrupar por Id, mantener mejor ranking, ordenar y luego
paginar; hacerlo por alternativa primero ocultaría resultados.
Una colisión normalizada requiere conservar los Ids y revisión humana.
Las notas se conservan aquí porque la bóveda central indicada no está
disponible en la ruta configurada; no se creó otra bóveda.
