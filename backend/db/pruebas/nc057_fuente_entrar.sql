-- nc057_fuente_entrar.sql · va ANTES del 11: lo que sigue en la sesión corre en nc057_fuente.
-- Si la plantilla no existe (o no es lacentral_test), se corta y el 11 no llega a correr en public.
SET search_path TO nc057_fuente;
DO $guarda$
BEGIN
    IF current_database() <> 'lacentral_test' OR current_schema() IS DISTINCT FROM 'nc057_fuente' THEN
        RAISE EXCEPTION 'El 11 va en lacentral_test.nc057_fuente (base %, esquema %): no se aplica.',
            current_database(), coalesce(current_schema(), 'ninguno');
    END IF;
END $guarda$;
