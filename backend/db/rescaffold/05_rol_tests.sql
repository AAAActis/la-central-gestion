-- 05_rol_tests.sql
-- Rol con el que corre la suite contra PostgreSQL (LACENTRAL_TEST_PG).
--
-- Lo APLICÓ EL EQUIPO antes del 2026-10-15; acá queda versionado. Se reconstruyó
-- leyendo el catálogo real (pg_roles, datacl, nspacl, role_table_grants,
-- pg_default_acl) con el propio lacentral_tests. La contraseña NO va en el repo.
--
-- Qué puede y qué no:
--   · sin superusuario, sin CREATEDB, sin CREATEROLE, sin BYPASSRLS;
--   · CONNECT + CREATE sobre lacentral_test: CREATE es lo que le permite a
--     PostgreSqlPruebas crear (y borrar, como dueño) su esquema aislado pr110_test_*;
--   · en public de lacentral_test, SOLO SELECT: el fixture copia la estructura con
--     CREATE TABLE ... (LIKE public.x INCLUDING ALL) y los catálogos rol/sucursal
--     con INSERT ... SELECT. No escribe en public;
--   · sin acceso a la base lacentral (datos reales de la empresa).

-- En el servidor, como dueño de las bases (lacentral):
CREATE ROLE lacentral_tests LOGIN
    NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT
    PASSWORD '<definida por el equipo, fuera del repo>';

GRANT CONNECT, CREATE ON DATABASE lacentral_test TO lacentral_tests;
-- lacentral ya tiene revocado CONNECT para PUBLIC: lacentral_tests no entra.

-- Conectado a lacentral_test:
GRANT USAGE ON SCHEMA public TO lacentral_tests;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO lacentral_tests;
-- Las tablas que agregue un re-scaffold futuro también se leen sin otro GRANT.
ALTER DEFAULT PRIVILEGES FOR ROLE lacentral IN SCHEMA public
    GRANT SELECT ON TABLES TO lacentral_tests;
