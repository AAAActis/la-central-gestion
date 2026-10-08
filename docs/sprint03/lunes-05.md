# Lunes 05/10/2026 — Javi

## Tareas y evidencia

1. **CRÍTICO 1: conservar historial al reactivar cliente.** El PR #104 ya
   incorporó el mapeo de MotivoBaja/FechaBaja a develop. Esta rama refuerza la
   regresión: comprueba la lectura de ambos campos, ejecuta el caso de uso real
   y relee con otro contexto. El ciclo HTTP comprueba baja, fecha e historial.
2. **ALTO 7: retirar /api/test-db.** La eliminación también llegó en #104.
   Esta rama verifica por HTTP que la ruta devuelve 404 sin consultar la base.

Las pruebas de repositorio actuales usan InMemory y la prueba del ciclo HTTP
usa mocks. No acreditan índices, transacciones ni persistencia PostgreSQL.
La verificación en base exclusiva por Tailscale sigue pendiente de acceso.
El Id estable del cliente (D5, 14/10) y mostrar historial en el DTO de ficha
no se consideran resueltos por estas regresiones.

## Registro compartido

Fuente del alcance: Sprint_03_La_Central.md, versión 07/10/2026.
Acuerdos D1–D6: modalidades persistidas; un código por proveedor/artículo;
FCO-01 antes de ART-05; Sprint 3 del 05 al 18/10; Id estable de cliente;
PostgreSQL de test aislado por Tailscale, sin librerías nuevas.

Pendientes del equipo: alinear #32 con Sprint 3 y #22, reestimar las cinco HU,
actualizar CU-007/proformas/GR-01 y acordar capacidad. No hay nuevas estimaciones
aprobadas ni acceso a esos documentos en esta entrega.

## PR

Título: test(clientes): verificar historial de reactivación y retiro de test-db

Descripción: Sobre las correcciones integradas en #104, agrega regresiones de
las tareas de Javi del lunes. Comprueba motivo/fecha al leer y reactivar con el
repositorio real InMemory, restablece asserts del ciclo HTTP y verifica 404 en
/api/test-db. No cierra HU completas ni presenta InMemory como PostgreSQL.
