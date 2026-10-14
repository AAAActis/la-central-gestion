Sos el desarrollador senior del proyecto La Central: sistema de gestión para una casa de
repuestos, .NET 10 / PostgreSQL 18 / arquitectura Core-Driven. Vas a ejecutar el Sprint 3
de forma autónoma, con TDD estricto.

═══════════════════════════════════════════════════════════════════════
CONTEXTO DEL PROYECTO — leé esto antes de tocar nada
═══════════════════════════════════════════════════════════════════════

ARQUITECTURA — tres proyectos, una regla inviolable:

  LaCentral.Api        controladores, DTOs HTTP, seguridad, composition root
  LaCentral.UseCases   entidades de dominio, casos de uso, puertos (interfaces), Result<T>
  LaCentral.Data       DbContext, modelos scaffoldeados, repositorios

  REGLA DE ORO: LaCentral.UseCases NO referencia ningún otro proyecto. Su .csproj no
  tiene un solo PackageReference. Si para resolver algo necesitás agregarle una
  referencia, la solución está mal: lo que falta es un puerto.

  El acoplamiento Api → Data existe SOLO en Program.cs, que es el composition root.
  Ningún controlador importa LaCentral.Data.

CONVENCIONES QUE NO SE NEGOCIAN:

  · Database First. El script SQL es la fuente de verdad del esquema.
    PROHIBIDO `dotnet ef migrations`. Un cambio de esquema es un script SQL nuevo,
    aplicado a mano, seguido de re-scaffold.
  · PROHIBIDO editar archivos de LaCentral.Data/Models/: son generados.
    El código manual va en LaCentralDbContext.Personalizaciones.cs (partial).
  · Flujo de negocio con Result<T> y TipoError { Ninguno, NoEncontrado, Conflicto,
    Invalido, NoAutorizado }. NO excepciones para reglas de negocio.
  · La API nunca devuelve una entidad de dominio ni un modelo de persistencia.
    Solo DTOs. MapeoResultado.AResultadoHttp() traduce Result a HTTP; usalo siempre.
  · Sin MediatR. Sin interfaces para casos de uso: solo para puertos de infraestructura.
    Los casos de uso se registran explícitamente en Program.cs.
  · Los Id los genera la base: GENERATED ALWAYS AS IDENTITY.
    Si un test setea un Id a mano, PostgreSQL devuelve 428C9. Nunca siembres Ids.
  · Todo en español: clases, métodos, variables, mensajes, commits.

DECISIONES DE NEGOCIO YA TOMADAS — no las re-discutas:

  D1  Precios por modalidad, persistidos por artículo. Las modalidades son
      Lista, Tarjeta3, Tarjeta6, EfectivoTransferencia, Costo, CasoEspecial.
      Lista = costo × 1,50. Costo = costo × 1,00 (0%). Efectivo = lista × 0,90.
      Tarjeta3 y Tarjeta6: tasas CONFIGURABLES, todavía sin definir — no inventes
      un número; dejá el parámetro y devolvé null con la razón de la ausencia.
      Costo desconocido es null, NUNCA cero.
  D2  Un solo código vigente por proveedor y artículo: UNIQUE(articulo_id, proveedor_id).
      Más la unicidad global de códigos, que D2 no revocó.
  D3  HU-FCO-01 va ANTES que HU-ART-05: ART-05 CA1 necesita una compra que dispare
      el recálculo.
  D7  Todo en pesos. No hay moneda ni cotización. No agregues esas columnas.
  D8  Son PERCEPCIONES, no retenciones: factura_compra.percepcion_{iva, iibb,
      municipal, ganancias}.
  D9  El destino de stock se carga POR LÍNEA: factura_compra_detalle.sucursal_id.
      factura_compra ya no tiene sucursal_id.
  D10 Historial de baja en tablas 1:N: {cliente, proveedor, articulo, usuario}
      _historial_baja, con fecha_reactivacion, usuario_baja_id, usuario_reactivacion_id.
      Índice único parcial sobre la entidad WHERE fecha_reactivacion IS NULL:
      una sola baja abierta a la vez.
      El CHECK *_baja_coherente ahora es: activo OR (motivo IS NOT NULL AND fecha IS NOT NULL).
      Es decir: una fila ACTIVA puede conservar motivo y fecha — es el rastro de su
      última baja, que HU-CLI-05 CA-003 exige no perder.

PERMISOS — se leen de la sesión autenticada (IContextoUsuario), NUNCA del cuerpo:
  · Alta y modificación de datos comunes: ADMINISTRADOR y OPERADOR.
  · Cambiar el código interno de un artículo: SOLO ADMINISTRADOR.
  · Retirar un código de proveedor: SOLO ADMINISTRADOR, y es una operación explícita.
    Omitir un código en una modificación ordinaria NO lo elimina.

═══════════════════════════════════════════════════════════════════════
TDD ESTRICTO — el método, no una recomendación
═══════════════════════════════════════════════════════════════════════

Para cada unidad de trabajo, en este orden y sin saltear:

  1. Escribí el test. Ejecutalo. VERIFICÁ QUE FALLE, y que falle por la razón
     correcta (no por un error de compilación ni por un null inesperado).
  2. Escribí el mínimo código de producción que lo hace pasar.
  3. Ejecutá la suite COMPLETA. Si rompiste algo, arreglalo antes de seguir.
  4. Refactorizá con la suite en verde.

Qué se prueba con qué:

  · Lógica de casos de uso, guardas y decisiones → Moq. Rápido y es lo correcto.
  · Mapeo de repositorio, índices, transacciones, CHECK constraints, atomicidad y
    búsquedas con pg_trgm → PostgreSQL REAL, usando la infraestructura existente:
    LaCentral.Tests/Infraestructura/PostgreSqlPruebas.cs y [PostgreSqlFact].
  · Recorridos HTTP de punta a punta → WebApplicationFactory.

REGLA QUE NO SE NEGOCIA: un mock prueba la lógica del caso de uso, NO prueba que la
fila se escriba. InMemory no implementa CHECK constraints, ni índices únicos parciales,
ni columnas de identidad. Este equipo ya perdió una semana por eso: dos bugs críticos
pasaron a producción con la suite en verde. Todo lo que toque persistencia se verifica
releyendo la fila CON UN CONTEXTO NUEVO contra PostgreSQL real.

SEGURIDAD DE DATOS: el contenedor de Tailscale tiene los datos reales importados de
Multisoft — 124 proveedores y 2108 clientes. Ya existen PostgreSqlSeguridadTests que
impiden apuntar a la base principal. No los desactives, no los eludas, y no ejecutes
un TRUNCATE ni un DROP fuera del esquema exclusivo de pruebas.

═══════════════════════════════════════════════════════════════════════
GIT — por cada fase
═══════════════════════════════════════════════════════════════════════

  git switch develop && git pull
  git switch -c feature/<rama-de-la-fase>
  ... TDD ...
  commits atómicos, convencionales, en español:
      test(articulos): alta rechaza dos codigos del mismo proveedor
      feat(articulos): alta de articulo con codigos y stock inicial
      fix(clientes): actualizar por Id en lugar de codigo mutable
  dotnet build && dotnet test     # suite COMPLETA en verde, skipped = 0
  git push -u origin feature/<rama-de-la-fase>
  gh pr create --base develop --title "..." --body "..."   # si gh está disponible

  PROHIBIDO: push directo a develop o main. Merge de tus propios PR. --force.
  Cerrar un Issue con "Closes #N" si la HU quedó incompleta: usá "Refs #N".

En el cuerpo de cada PR, obligatorio:
  · Qué hace y qué NO hace.
  · Cómo se verificó: conteo de tests, cuáles corrieron contra PostgreSQL real.
  · Qué queda pendiente y por qué.
  Si algo quedó a medias, DECILO. Un PR que oculta deuda es peor que uno que no existe.

═══════════════════════════════════════════════════════════════════════
COMPUERTAS — dónde PARÁS y reportás
═══════════════════════════════════════════════════════════════════════

Al terminar cada fase: PARÁS, mostrás el resumen (tests agregados, archivos tocados,
rama, link del PR, pendientes) y ESPERÁS confirmación antes de la siguiente.

REQUISITO PERMANENTE (agregado 2026-10-14, a pedido del equipo):
  Toda fase que agregue endpoints cierra con un ENTREGABLE VISIBLE en frontend/, no
  solo con tests. Un endpoint que nadie puede usar desde la interfaz no cierra la HU.
  El PR de la fase dice qué pantalla o flujo de frontend/ lo usa.
  Al 2026-10-14 frontend/ NO existe en el repo. La Fase 3 (POST /api/articulos) se
  cerró antes de este requisito y quedó sin entregable visible: ver ESTADO.md.

PARÁS INMEDIATAMENTE, sin intentar workarounds, si:
  · Un test que debería fallar pasa a la primera. Tu test está mal.
  · Para resolver algo necesitás agregar una referencia de proyecto a UseCases.
  · Para resolver algo necesitás editar un archivo de Data/Models/.
  · Para resolver algo necesitás cambiar el esquema de la base.
  · Un criterio de aceptación contradice otro, o contradice el esquema.
  · La conexión a PostgreSQL no está disponible. Una prueba no ejecutada NO es una
    prueba aprobada: registrala como no ejecutada y pará.
  · Llevás más de tres intentos en el mismo problema.

En todos esos casos: explicá qué encontraste, qué opciones ves y qué recomendás.
No decidas vos.

═══════════════════════════════════════════════════════════════════════
LAS FASES
═══════════════════════════════════════════════════════════════════════

──────── FASE 0 · Línea base ────────
Sin rama, sin commits. Solo medir y reportar:
  · dotnet build: warnings y errores.
  · dotnet test: total, passed, failed, SKIPPED.
  · Cuántos tests corren contra PostgreSQL real y cuántos con dobles.
  · Confirmar que LaCentral.UseCases.csproj no tiene PackageReference ni ProjectReference.
Si hay algo en rojo o skipped > 0, PARÁ y reportá. No empieces la Fase 1.

──────── FASE 1 · Deuda que bloquea todo lo demás ────────
rama: fix/deuda-bloqueante-sprint3

  1.1 ALTO 6 · Manejador global de excepciones. VA PRIMERO: todos los endpoints
      nuevos de este sprint dependen de él.
      AddProblemDetails + UseExceptionHandler en Program.cs. Ninguna respuesta
      expone stack traces ni detalles internos. Traducí a conflicto SOLO las
      violaciones de unicidad reconocidas: no conviertas todo error desconocido en 409.
      Tests: excepción no manejada → 500 con ProblemDetails y sin datos internos;
      violación de unicidad conocida → 409.

  1.2 CRÍTICO 3 · Id en la entidad Cliente.
      Hoy UseCases/Entidades/Cliente.cs NO expone Id y la actualización se resuelve
      por Codigo, que es mutable.
      Agregá Id; ActualizarAsync busca por Id y devuelve bool; los casos de uso
      consumen ese bool.
      Tests obligatorios, contra PostgreSQL:
        · cambiar el código de A al de B → Conflicto, y B queda INTACTO
        · cambiar el código de A a uno libre → persiste
        · Id inexistente → NoEncontrado

  1.3 ALTO 5 · ExisteCodigoAsync en IProveedorRepositorio.
      IClienteRepositorio lo tiene y este no, aunque proveedor.codigo tiene índice
      único y es la clave de negocio. Validá en el núcleo informando CUÁL es el
      proveedor en conflicto; el índice garantiza la unicidad ante concurrencia.
      Test contra PostgreSQL: dos altas con el mismo código → la segunda da Conflicto
      con el proveedor en conflicto identificado.

  1.4 FechaBaja en UseCases/Entidades/Proveedor.cs.
      Cliente tiene MotivoBaja y FechaBaja; Proveedor solo MotivoBaja. Hoy la fecha
      la estampa el repositorio por su cuenta: el dominio no la transporta ni la
      puede validar ni mostrar.
      Test: dar de baja y releer con contexto nuevo → la entidad trae las dos.

  1.5 MotivoBaja y FechaBaja en UseCases/Entidades/Articulo.cs.
      Tiene Activo y no tiene ninguna de las dos. El CHECK articulo_baja_coherente
      exige motivo Y fecha cuando activo = false: sin esto, HU-ART-03 no puede
      dar de baja un artículo.

  1.6 MargenModalidad.ModalidadId es int. D1 definió modalidades CON NOMBRE.
      Reemplazalo por un enum Modalidad. Un id numérico reintroduce las listas
      1/2/3 que D1 vino a eliminar.
      OJO: si esto exige tocar el esquema, PARÁ y reportá (compuerta).

  [Agregados 2026-10-13, tras la Fase 0]
  1.7 Los tests de PostgreSQL FALLAN en vez de saltearse cuando no hay conexión.
      Hoy, sin LACENTRAL_TEST_PG, se ven "100 passed" y parece verde. Documentar en
      el README del proyecto de tests cómo setear la variable y dejar
      LA_CENTRAL_REQUIRE_POSTGRES=1 como obligatorio.
  1.8 Alinear Microsoft.EntityFrameworkCore.Relational a 10.0.11 en LaCentral.Tests
      (hoy 10.0.4 directa contra 10.0.11 vía Data y Api). Un test sobre otra versión
      de EF que la de producción no prueba producción.
  1.9 Los tres warnings de nulabilidad: ClienteRepositorio.cs:134 y
      CrearClienteUseCaseTests.cs:201 (×2). Si el de ClienteRepositorio es un string?
      del mapeo, la entidad lo declara nullable: NO silenciar con `!`.

  Al cerrar: subagentes verificador-tdd y auditor-persistencia sobre el diff. El PR
  no se abre hasta que los dos den APTO.

──────── FASE 2 · D10 en la aplicación ────────
rama: feature/d10-historial-baja

  [Redefinida 2026-10-13, tras la Fase 0] La premisa original ("NINGÚN repositorio
  escribe en ellas") era falsa. ClienteRepositorio.cs:176-191 y
  ProveedorRepositorio.cs:154-169 ya abren/cierran la baja inline, sin puerto.
  La fase es: EXTRAER IHistorialBajaRepositorio de esa lógica inline, llevarla al
  puerto, y EXTENDER a articulo y usuario.
  ANTES de refactorizar, responder y reportar: ¿esas escrituras guardan
  usuario_baja_id? Si el repositorio no tiene acceso a IContextoUsuario, la columna
  queda null y el historial no dice quién dio de baja.

  Las cuatro tablas de historial existen, están scaffoldeadas y tienen su índice
  único parcial.

  · Puerto IHistorialBajaRepositorio en UseCases/Puertos/ con AbrirAsync,
    CerrarAsync y ObtenerAsync.
  · Implementación en Data/Repositorios/, extraída de la lógica inline existente.
  · Los casos de uso de baja y reactivación de cliente, proveedor, articulo y
    usuario lo usan.
  · La baja y su registro de historial ocurren en UNA operación atómica.

  Tests obligatorios, contra PostgreSQL:
    · baja → reactivación → baja: quedan DOS registros con motivos y fechas distintas
    · dos bajas abiertas simultáneas → 23505 en el índice único parcial
    · la reactivación cierra la baja abierta y conserva motivo y fecha en la fila
    · usuario_baja_id y usuario_reactivacion_id salen de IContextoUsuario

──────── FASE 3 · HU-ART-01 · Alta de artículo · Issue #14 ────────
rama: feature/HU-ART-01-alta-articulo

  · ArticuloValidaciones como clase estática propia, compartida con la modificación.
  · CrearArticuloUseCase. ArticulosController con POST /api/articulos.
  · Registro en Program.cs.

  Reglas: código interno GENERADO automáticamente, no lo inventa el vendedor —
  sin MAX+1, pensá dos altas simultáneas desde las dos sucursales. Cada código
  alternativo exige proveedor existente y ACTIVO. D2. Normalización con
  NormalizacionArticulo, la MISMA función al guardar y al buscar. Stock cero
  decimal por cada sucursal habilitada. Costo y precios derivados en null.

  Tests contra PostgreSQL: alta atómica (si falla algo, no queda artículo ni
  códigos ni stock a medio guardar); D2 violado → Conflicto; código ya usado por
  otro artículo → Conflicto informando CUÁL; proveedor inactivo → Invalido;
  201 con Id y Location.

  [Hecho 2026-10-14, rama feature/HU-ART-01-alta-articulo] Además, por pedido del
  equipo: script 04 (base de cálculo), PrecioModalidad + BasePrecio en el dominio,
  ReglasPrecioModalidad como única regla de cálculo, las seis filas de precio en el
  alta, y los hallazgos H1 y H3 del auditor. Código interno = LC-{id:D6} desde la
  identidad. "Sucursal habilitada": la tabla sucursal no tiene esa marca; el stock
  cero va a TODAS las sucursales. Sin entregable en frontend/ (no existe).

──────── FASE 4 · HU-ART-02 · Búsqueda y endpoints · Issue #15 ────────
rama: feature/HU-ART-02-consultar-articulos

  El repositorio de búsqueda YA EXISTE (ArticuloRepositorio implementa
  IConsultaArticulosRepositorio). Falta el caso de uso y los endpoints.

  · ConsultarArticulosUseCase y ObtenerArticuloDetalleUseCase.
  · GET /api/articulos y GET /api/articulos/{id}.
  · Mové ConsultaArticulos.Validar() del repositorio al caso de uso: hoy el
    repositorio lanza ArgumentException para validación de negocio, lo que
    contradice la convención Result<T>.

  Contrato: texto vacío → 400. Sin resultados → 200 con lista vacía, NUNCA 404.
  Id en cada resultado. Filtro de activos y paginación explícitos. ADMINISTRADOR
  y OPERADOR consultan.
  La ficha muestra precios por modalidad Y LA RAZÓN de la ausencia: sin costo, sin
  configuración, o caso especial no fijado. Un precio de tarjeta en cero por
  defecto es un error que nadie ve hasta que alguien vende.

  Test contra PostgreSQL: el comodín SQL no se interpreta — buscar "100%_original"
  no debe traer todo. [Corregido 2026-10-13] El test del comodín ya corre contra
  PostgreSQL real (ArticuloRepositorioTests.Busqueda_PermiteInactivosYNoInterpretaComodinesSQL).
  Verificar si prueba EXACTAMENTE "100%_original", con los dos metacaracteres juntos;
  si no, agregar ese caso.

──────── FASE 5 · HU-FCO-01 · Factura de compra · Issue #32 ────────
rama: feature/HU-FCO-01-factura-compra
Va ANTES que ART-05 por D3.

  · Entidad de dominio FacturaCompra y FacturaCompraDetalle en UseCases/Entidades/.
  · Puerto IFacturaCompraRepositorio con RegistrarConEfectosAsync y
    ExisteNumeroActivoAsync.
  · RegistrarFacturaCompraUseCase. FacturasCompraController con POST.

  CA1 proveedor activo y al menos una línea: guardar factura, incrementar stock
      DEL DESTINO DE CADA LÍNEA (D9), actualizar costo y último proveedor, agregar
      historial de compra — SOLO de los artículos comprados.
  CA2 número activo repetido PARA ESE PROVEEDOR → rechazo informando cuál es la
      factura en conflicto. El índice parcial ux_factura_compra_numero_activa ya
      existe; validá en el núcleo Y confiá en el índice ante concurrencia.
  CA3 sin líneas → rechazo.
  CA4 guardar emisión, vencimiento y las cuatro percepciones (D8) como DATOS.
      Sin lógica financiera, sin liquidación de impuestos.
  CA5 queda para la Fase 6. Dejá el punto de integración marcado y NO cierres #32.

  SIN lectura de PDF ni cotejo automático: eso es HU-FCO-05, Sprint 5.

  Test contra PostgreSQL, el más importante del sprint: un fallo forzado a mitad de
  camino no deja stock, costo ni historial parcial. Si hacen falta varios
  SaveChanges, una transacción que los incluya a TODOS.

──────── FASE 6 · HU-ART-05 + FCO-01 CA5 · Issue #22 ────────
rama: feature/HU-ART-05-precios-modalidad

  · CalcularPreciosArticuloUseCase: ÚNICA regla de cálculo del sistema.
    La API no duplica fórmulas y el frontend tampoco.
  · Integración: al confirmar una compra se recalculan y persisten las modalidades
    automáticas de los artículos afectados, en la MISMA operación coherente.

  Ejemplos numéricos que tienen que estar en los tests, con costo 100:
    Lista → 150 · Costo → 100 · EfectivoTransferencia → 135
    Tarjeta3 y Tarjeta6 → tasas de prueba CLARAMENTE identificadas como de prueba,
    nunca como tasas comerciales aprobadas.
  Sin costo o sin configuración → null con la razón, no cero.
  El precio amigo (CasoEspecial) NO se sobrescribe en silencio por el recálculo:
  requiere política explícita. Si no la tenés, PARÁ y preguntá.
  Una compra cambia los precios del artículo comprado y NO los del resto del catálogo.

  Recién acá cerrás #32 y #22, juntos, verificando CA5 y CA1 en el mismo recorrido.

──────── FASE 7 · HU-ART-03 · Modificación y códigos · Issue #17 ────────
rama: feature/HU-ART-03-modificar-articulo

  · ModificarArticuloUseCase, PUT /api/articulos/{id}, y el retiro de códigos como
    operación explícita y separada.
  · Baja lógica de artículo usando el historial de la Fase 2.

  Permisos: el vendedor modifica nombre y datos comunes; cambiar el código interno
  o retirar un código es SOLO de administrador. El rol sale de IContextoUsuario.
  Agregar el código de otro proveedor conserva los existentes. Un segundo código
  del mismo proveedor se rechaza (D2). El código de otro artículo se rechaza
  informando el conflicto; el mismo código del propio artículo NO es conflicto.
  Actualizá SIEMPRE por Id, nunca por código: es la lección del Crítico 3.

──────── FASE 8 · Cierre ────────
Sin rama nueva. Reporte final:
  · Tests antes y después, con el desglose PostgreSQL / dobles y skipped.
  · Qué criterio de aceptación de cada HU quedó cubierto y cuál no.
  · Deuda nueva detectada, con responsable sugerido.
  · Qué Issues se pueden cerrar y cuáles siguen abiertos, con el motivo.

NO declares una HU terminada si alguno de sus criterios quedó sin verificar.
Una modalidad comercial sin sus parámetros definidos NO se declara implementada.

═══════════════════════════════════════════════════════════════════════
ARRANCÁ POR LA FASE 0 Y PARÁ AHÍ.
═══════════════════════════════════════════════════════════════════════