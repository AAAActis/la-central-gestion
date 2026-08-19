# La Central — Sistema de Gestión

Sistema de gestión para **La Central Casa de Repuestos**: clientes, proveedores, artículos, facturación y stock por sucursal, más comparación de precios entre proveedores por códigos alternativos y carga automatizada de facturas desde PDF.

Proyecto final — Analista de Sistemas Informáticos, UNC.

---

## Qué resuelve

El sistema actual de la empresa no da soporte a dos procesos críticos, que hoy se resuelven por fuera:

- **Comparación de precios entre proveedores.** Cada artículo es identificado con un código distinto por cada proveedor, y esos códigos no están vinculados entre sí. Decidir a quién comprarle depende del conocimiento personal del vendedor y de revisar los sitios de cada proveedor a mano.
- **Carga de facturas de compra.** El registro se hace campo por campo, lo que insume tiempo y genera errores de digitación que se propagan al precio de costo, al precio de venta estimado y a las existencias.

## Funcionalidades

**Base (réplica del sistema actual)**

- CRUD de clientes, proveedores, artículos, facturas de venta y facturas de compra, con baja lógica y motivo obligatorio
- Códigos alternativos por artículo, cada uno asociado a un proveedor
- Márgenes de venta por artículo, con recálculo del precio estimado al actualizarse el costo
- Control de stock por ubicación — Fragueiro (`FR`) y San Vicente (`SV`) — con transferencias entre ambas
- Historial de compras por artículo (proveedor, precio de costo y fecha)

**Nuevas**

- **Comparador de precios**: busca un artículo por nombre y muestra las opciones de cada proveedor con su código y su precio, en vista de compra y de venta
- **Carga automatizada de facturas**: extrae los datos del PDF del proveedor, coteja los códigos contra la base y presenta el resultado para revisión humana antes de confirmar

## Stack

| Capa | Tecnología |
|---|---|
| Backend | C# · .NET · Entity Framework Core · LINQ |
| Frontend | TypeScript · Angular |
| Base de datos | PostgreSQL |
| Entorno | Docker · Tailscale |
| Testing | xUnit |
| Automatización | n8n |

## Estructura

```
.
├── backend/     API REST en .NET
├── frontend/    Cliente web en Angular
└── docs/        Documentación del proyecto
```

## Puesta en marcha

**Requisitos:** .NET SDK, Node.js, Angular CLI, Docker y Docker Compose.

```bash
# Base de datos
docker compose up -d

# Backend
cd backend
dotnet restore
dotnet ef database update
dotnet run

# Frontend
cd frontend
npm install
ng serve
```

La cadena de conexión y las variables sensibles se cargan desde un archivo `.env` que **no se versiona**. Copiar `.env.example` y completarlo antes de levantar el proyecto.

## Metodología

Scrum con sprints de dos semanas. El backlog se gestiona con GitHub Projects e Issues:

- Cada **Issue** es una Historia de Usuario
- La **épica** es un campo del Project
- Cada **sprint** es un Milestone
- Los story points siguen escala Fibonacci y la prioridad, criterio MoSCoW

### Definition of Done

Una HU está terminada cuando cumple las cinco condiciones:

1. El código funciona en el entorno compartido
2. Fue revisado por otro integrante mediante pull request aprobado
3. Tiene tests automatizados que pasan
4. Cada criterio de aceptación fue verificado
5. El Issue queda documentado y cerrado desde el commit

## Convenciones

**Ramas**

```
main          versión estable
develop       integración
feature/HU-XX-descripcion-corta
fix/descripcion-corta
```

**Commits** — formato convencional, referenciando el Issue:

```
feat(articulos): alta con códigos alternativos por proveedor

Closes #12
```

## Documentación

En `docs/`:

- Mandato del Proyecto
- Historias de Usuario (38 en 8 épicas)
- Casos de Uso (CU-001 a CU-008)
- Product Backlog con estimación y plan de sprints

## Equipo

Actis Santiago · Billarroel Lautaro · Bossio Javier

Docente tutor: Villagra Fernando

---

Trabajo de carácter académico. El software no se desarrolla para ser entregado ni puesto en producción en la empresa.