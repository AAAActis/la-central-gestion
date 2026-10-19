# El método de vinculación por asterisco · diseño

Tu idea es buena y es barata. La escribo completa, con el modelo de datos, los tres niveles de
coincidencia, el bucle de aprendizaje y los límites honestos. Al final, dónde entra el scraping
y cómo levantar n8n.

---

## El problema, en una línea

El mismo repuesto se llama distinto en cada proveedor:

| Proveedor | Cómo lo nombra |
|---|---|
| A | `GN/100` |
| B | `Mouse Genius GN/100` |
| C | `mau gen gn 100` |

Y en La Central ese artículo tiene su propio código interno y su propio nombre. Son cuatro
nombres para una cosa.

## Lo primero, y es gratis: normalizar

Antes de pensar en similitud, en IA o en cualquier cosa: **pasá todo por una normalización y
mirá cuánto se resuelve solo.**

```
normalizar(texto) = mayúsculas
                  + quitar acentos
                  + quitar todo lo que no sea letra o número
                  + colapsar espacios
```

Aplicada a tus tres ejemplos:

| Original | Normalizado | ¿Contiene `GN100`? |
|---|---|---|
| `GN/100` | `GN100` | sí, exacto |
| `Mouse Genius GN/100` | `MOUSEGENIUSGN100` | sí |
| `mau gen gn 100` | `MAUGENGN100` | sí |

**Los tres comparten el token `GN100`.** No hace falta IA, no hace falta similitud difusa: hace
falta sacarle las barras, los espacios y las minúsculas. Ese es el 80 % del problema y cuesta
una función de veinte líneas.

Y de acá sale el primer experimento del sprint, que es de una hora: **tomá 50 artículos reales
con sus códigos en dos o tres proveedores, normalizalos, y contá cuántos coinciden exacto.** Si
da 40 de 50, los niveles 2 y 3 son un lujo y no la prioridad. Si da 15, el diseño cambia.
Medilo antes de construir.

---

## El modelo de datos

Tres piezas, y dos ya existen:

| Tabla | Estado | Qué guarda |
|---|---|---|
| `articulo` | **existe** | El catálogo maestro de La Central. 2644 filas. |
| `articulo_codigo_alternativo` | **existe** (D2) | Código + proveedor + artículo. Un código vigente por proveedor y artículo, con unicidad global. |
| `proveedor_articulo` | **nueva** | El catálogo crudo del proveedor, tal como lo manda. |

La tabla nueva, en lo esencial:

| Columna | Para qué |
|---|---|
| `proveedor_id` | De quién es esta fila |
| `codigo_proveedor` | El código con que ese proveedor lo identifica |
| `nombre`, `descripcion`, `marca` | Lo que el proveedor dice, sin tocar |
| `nombre_normalizado` | El resultado de la función de arriba. **Indexado.** |
| `costo`, `stock`, `fecha_dato` | El dato comercial y cuándo se obtuvo |
| **`articulo_id` NULLABLE** | **La vinculación. `NULL` = sin vincular.** |

Clave única: `(proveedor_id, codigo_proveedor)`. Eso permite reingestar el catálogo del
proveedor cuantas veces quieras actualizando precio y stock, sin duplicar filas y **sin perder
las vinculaciones ya hechas**. Es el `ON CONFLICT DO UPDATE` de siempre, y es la pieza que hace
que esto sea sostenible: la vinculación es trabajo humano y no se puede estar rehaciendo.

Ese `articulo_id` nullable es exactamente el "Sin vincular / vinculado" del sistema de Eduardo.
Es el modelo correcto y ya está probado en producción en el otro proyecto.

---

## La búsqueda: tres niveles, del más barato al más caro

El asterisco dispara los tres y muestra los resultados **agrupados por nivel**, no mezclados:
el operador tiene que ver de un vistazo qué es seguro y qué es un candidato.

### Nivel 1 · Coincidencia exacta sobre el normalizado

```
WHERE nombre_normalizado = normalizar($busqueda)
   OR codigo_proveedor_normalizado = normalizar($busqueda)
```

Instantáneo, con índice. **Esto no es un candidato: es una certeza.** Se muestra arriba,
separado, y se puede vincular sin pensar.

### Nivel 2 · Similitud por trigramas — y acá está el regalo

Esto **ya lo tenés funcionando**. `pg_trgm` está instalado, el índice GIN está creado y
HU-ART-02 lo usa para la búsqueda de artículos desde el Sprint 3.

```
WHERE nombre_normalizado % normalizar($busqueda)
ORDER BY similarity(nombre_normalizado, normalizar($busqueda)) DESC
```

Cero infraestructura nueva. Lo único a calibrar es el umbral (`pg_trgm.similarity_threshold`),
y eso se calibra con los 50 artículos del experimento, no a ojo.

### Nivel 3 · Coincidencia por tokens raros

El nivel 2 falla justo en tu tercer ejemplo: `MAUGENGN100` contra `MOUSEGENIUSGN100` comparten
pocos trigramas porque `MAU` y `MOUSE` no se parecen tanto. Lo que sí comparten es el token
`GN100`.

Entonces: partí el texto en tokens y **pesá cada token por lo raro que es en todo el
catálogo**. `MOUSE` aparece en 400 filas, pesa poco. `GN100` aparece en 3, pesa muchísimo. El
que comparte el token raro gana.

Eso es TF-IDF y Postgres lo hace con el buscador de texto completo (`ts_rank`), o incluso con
un conteo propio sobre una tabla de tokens. **No lo construyas en el Sprint 4.** Dejalo
diseñado y medí primero si el nivel 1 y el 2 alcanzan. Si alcanzan, te ahorrás la parte más
complicada.

---

## El bucle de aprendizaje · esto es lo que lo hace valer

Cuando el operador vincula una fila de `proveedor_articulo` a un artículo, el sistema hace
**tres cosas en una sola operación**, no una:

1. Escribe `proveedor_articulo.articulo_id`.
2. **Inserta el código del proveedor en `articulo_codigo_alternativo`.** Desde ese momento ese
   código es una coincidencia de nivel 1, para siempre. Nunca más hay que pensarlo.
3. Guarda la decisión en un registro de vinculaciones: texto normalizado de origen, artículo,
   usuario y fecha.

El punto 3 es el que aprende, y no es un modelo: **es una tabla de decisiones humanas**. La
próxima vez que cualquier proveedor manda un texto cuyo normalizado ya figura ahí, se propone
primero y con certeza.

### Y acá está el efecto que multiplica

Una vez que `GN/100` del proveedor A y `mau gen gn 100` del proveedor C están los dos
vinculados al artículo Y, **el sistema sabe que son equivalentes entre sí**. Transitivamente.
Así que cuando llega el proveedor D con `MOUSE GEN. GN100 NEGRO`, la similitud no se calcula
solo contra el nombre de La Central: se calcula **contra todos los textos ya vinculados a ese
artículo**.

El conjunto de candidatos crece con cada vinculación. Cada decisión humana mejora las
siguientes.

**Por eso esto le gana a llamar a una IA en cada búsqueda:** la IA razonaría de nuevo cada vez
y cobraría cada vez. Esto se acuerda. Y tu instinto de hacerlo perezoso —vincular a medida que
alguien busca, no de entrada— es el correcto: nadie va a sentarse a vincular 2644 artículos por
cinco proveedores, pero vincular los diez que se buscan por día sí se sostiene, y en tres meses
está hecho el catálogo que realmente se usa.

### Las dos pantallas que esto necesita

| Pantalla | Para quién | Qué hace |
|---|---|---|
| **Buscar y comparar** | Vendedor | Escribe, ve los proveedores que lo tienen con su costo y fecha, ordenados de más barato a más caro, y la existencia por sucursal. |
| **Vincular** | Operador | Ve las filas sin vincular que coincidieron, elige el artículo de La Central y confirma. Es la que alimenta todo lo demás. |

La de vincular es la que no se puede improvisar: si vincular cuesta cinco clics, nadie vincula
y el sistema no aprende nunca. Un clic por vinculación, con el candidato más probable
preseleccionado, y deshacer disponible.

---

## Los límites, dichos de frente

- **La primera vinculación de cada artículo en cada proveedor la hace una persona.** No hay
  magia. Lo que el método logra es que sea una vez y no todas las veces.
- **Un código mal vinculado se propaga**, porque pasa a ser nivel 1. Hace falta poder
  desvincular, y que la desvinculación saque también el código alternativo que se había
  agregado. Eso va en la HU desde el principio, no después.
- **Dos artículos distintos de La Central pueden compartir código de proveedor** si el padrón
  importado tiene duplicados — y ya sabemos que los tiene, por CUIT en clientes y proveedores.
  El nivel 1 tiene que informar el conflicto, no elegir uno. Esa regla ya existe y está
  probada: es el `CodigoDeOtroArticulo_ConflictoInformandoCual` del Sprint 3.
- **Nada de esto necesita scraping para funcionar.** Funciona hoy, con el historial de compras
  que ya se escribe en cada factura desde la Fase 5.

---

## Dónde entra el scraping, y por qué no en el Sprint 4

Lo del taller funciona: tres de siete proveedores en producción. La factibilidad no está en
discusión. Lo que está en discusión es la secuencia y el alcance de **esta** tesis.

**Tres razones para que no entre ahora:**

1. **Sin la capa de vinculación, los datos scrapeados no sirven.** Disdma tiene 8208 artículos;
   La Central tiene 2644. Traerlos sin vincular es tener 8208 filas que nadie puede cruzar. La
   capa es el prerequisito, no el complemento.
2. **El mandato lo excluye hoy, y es la exclusión más cara de levantar.** Son credenciales de La
   Central guardadas en el sistema, un integrador por proveedor, y dependencia de que cinco
   sitios ajenos no cambien. El bloque de riesgo del propio documento de HU lo dice mejor que yo:
   *"es frágil por naturaleza… si un proveedor rediseña su web, la integración deja de funcionar
   sin aviso"*. Y además *"puede chocar con los términos de uso de los sitios de los
   proveedores"*. Eso en una tesis con fecha de entrega es el ítem de mayor riesgo del proyecto.
3. **El n8n está en la escuela de tu cuñado.** Para el otro proyecto está perfecto. Para un
   entregable de tesis, depender de un contenedor en infraestructura de un tercero que no es ni
   de ustedes ni de La Central es una dependencia que en la defensa te la van a preguntar.

**La propuesta, que es la que sí te lo deja adentro:** el objetivo 4 del mandato se declara en
dos etapas, igual que el objetivo 5 con la factura de compra.

- **Etapa 1 — Sprint 4.** Comparador sobre datos propios, con la capa de vinculación y la
  búsqueda por asterisco. Es lo que Multisoft no puede hacer de ninguna forma.
- **Etapa 2 — Sprint 7 u 8, con UN proveedor como prueba de concepto.** Ingesta automatizada del
  catálogo de un solo proveedor, el de API REST más limpia —por lo que contás, Disdma o
  Fratello—, cargando `proveedor_articulo`. Los otros seis quedan declarados como trabajo
  futuro, con el relevamiento hecho.

Un proveedor andando demuestra la arquitectura completa y la defendés sin riesgo. Siete
proveedores te comen el sprint 8 y si uno cambia la web en febrero te quedás sin entregable.

**Y el experimento que decide, que sigue sin hacerse y cuesta media hora:** cronometrá un login
más una búsqueda en el sitio de un proveedor real. Si la consulta en vivo tarda 3 segundos, la
etapa 2 puede ser en vivo. Si tarda 15, tiene que ser ingesta programada a la madrugada y el
comparador lee lo ingerido. Esa medición cambia la arquitectura entera y no se puede adivinar.

---

## n8n · cómo levantarlo

**Sobre la licencia, para que no haya sorpresa.** n8n no es software libre clásico: usa una
licencia de uso sostenible. Autoalojado y para uso interno del negocio no se paga; lo que la
licencia restringe es ofrecerlo como servicio a terceros. Para lo de ustedes, autoalojado, no
hay que pagar nada. Y como lo que estás leyendo es lo que yo sé hasta mayo de 2026, antes de
comprometerlo en el mandato leé los términos vigentes en el sitio de n8n: estas licencias
cambian.

**Lo que necesita:** Docker, una base para su propia configuración —Postgres sirve y ya tienen
uno—, y una clave de cifrado que **no se pierde nunca**: si se pierde, todas las credenciales
guardadas en n8n quedan ilegibles.

Un `docker-compose.yml` mínimo, con n8n y su propia base separada de la del sistema:

```yaml
services:
  n8n-db:
    image: postgres:18
    environment:
      POSTGRES_DB: n8n
      POSTGRES_USER: n8n
      POSTGRES_PASSWORD: ${N8N_DB_PASSWORD}
    volumes:
      - n8n-db-data:/var/lib/postgresql/data
    restart: unless-stopped

  n8n:
    image: docker.n8n.io/n8nio/n8n
    depends_on: [n8n-db]
    environment:
      DB_TYPE: postgresdb
      DB_POSTGRESDB_HOST: n8n-db
      DB_POSTGRESDB_DATABASE: n8n
      DB_POSTGRESDB_USER: n8n
      DB_POSTGRESDB_PASSWORD: ${N8N_DB_PASSWORD}
      N8N_ENCRYPTION_KEY: ${N8N_ENCRYPTION_KEY}
      N8N_HOST: ${N8N_HOST}
      GENERIC_TIMEZONE: America/Argentina/Cordoba
      N8N_SECURE_COOKIE: "false"
    ports:
      - "5678:5678"
    volumes:
      - n8n-data:/home/node/.n8n
    restart: unless-stopped

volumes:
  n8n-db-data:
  n8n-data:
```

Las cuatro variables van en un `.env` **al lado del compose y fuera del repositorio**. La clave
de cifrado se genera con `openssl rand -base64 32` y se guarda donde guarden las contraseñas,
no en el chat ni en un archivo versionado.

**Tres cosas que te van a morder, y dos son de tu propio documento:**

1. **Las credenciales de los proveedores van en el almacén de credenciales de n8n, nunca en un
   nodo Code ni en una URL.** Es lo que el CA8 de HU-CMP-02 ya exige: *"se almacenan de forma
   cifrada y no son visibles desde ninguna pantalla"*.
2. **`N8N_SECURE_COOKIE: "false"` solo vale en red interna.** Si algún día eso sale a internet,
   va con HTTPS y la variable se saca. Un n8n expuesto sin HTTPS es la credencial de todos los
   proveedores en texto claro por la red.
3. **El contenedor de n8n necesita llegar a tu Postgres por Tailscale.** Resolvelo con la IP de
   Tailscale, no con `localhost`: desde dentro del contenedor, `localhost` es el contenedor.

**Y un aviso que vale para el otro proyecto también:** el documento de Gemini tiene dos
contraseñas de proveedor en texto claro, un CUIT y un domicilio. **Rotá esa contraseña y sacá
los datos personales del documento.** Ese archivo ya pasó por dos chats y una IA; tratalo como
comprometido. Para esta tesis vale la regla que ya tenemos: ninguna credencial en el
repositorio, en el documento ni en un chat.

---

## Qué pasa con el Sprint 4

Sí, el Sprint 4 es el comparador, y con esto queda mejor de lo que lo había planteado: en vez
de solo leer el historial de compras, suma **la capa de vinculación y la búsqueda por
asterisco**, que es lo que lo vuelve útil de verdad y lo que después le da sentido a la
ingesta automatizada.

Eso implica **dos historias nuevas** que el backlog no tiene, y hay que escribirlas:

| Propuesta | Historia | SP estimados |
|---|---|---|
| HU-CMP-06 | Búsqueda de candidatos por aproximación entre los catálogos de proveedores | 8 |
| HU-CMP-07 | Vinculación manual de un artículo de proveedor al catálogo de La Central, con aprendizaje | 8 |

Las escribís vos con el formato de la proforma —ID 053 y 054, que siguen la numeración— y te
las reviso antes de que las implemente nadie. Esa es la parte que no delego: son reglas de
negocio, no tecnicismos.

Con esas dos, el Sprint 4 pasa de 46 a 62 SP y **no entra**. Lo que sale es HU-STK-04
transferencias (8 SP) y HU-CMP-04 Vista Venta (3), que se van al Sprint 5. El plan corregido va
en el `ESTADO.md`.
