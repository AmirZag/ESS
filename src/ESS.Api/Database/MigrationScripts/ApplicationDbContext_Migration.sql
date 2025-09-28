DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'ess') THEN
        CREATE SCHEMA ess;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS ess."__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250714120520_auto_14040423_153515') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'ess') THEN
            CREATE SCHEMA ess;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250714120520_auto_14040423_153515') THEN
    CREATE TABLE ess.app_settings (
        id character varying(500) NOT NULL,
        key character varying(100) NOT NULL,
        value character varying(2000) NOT NULL,
        type integer NOT NULL,
        description character varying(500),
        created_at timestamp with time zone NOT NULL,
        modified_at timestamp with time zone,
        CONSTRAINT pk_app_settings PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250714120520_auto_14040423_153515') THEN
    CREATE TABLE ess.users (
        id character varying(500) NOT NULL,
        name character varying(100) NOT NULL,
        national_code character varying(10) NOT NULL,
        phone_number character varying(11) NOT NULL,
        personal_code character varying(6) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        identity_id text NOT NULL,
        CONSTRAINT pk_users PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250714120520_auto_14040423_153515') THEN
    CREATE UNIQUE INDEX ix_app_settings_key ON ess.app_settings (key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250714120520_auto_14040423_153515') THEN
    CREATE UNIQUE INDEX ix_users_identity_id ON ess.users (identity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250714120520_auto_14040423_153515') THEN
    CREATE UNIQUE INDEX ix_users_national_code ON ess.users (national_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250714120520_auto_14040423_153515') THEN
    CREATE UNIQUE INDEX ix_users_personal_code ON ess.users (personal_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250714120520_auto_14040423_153515') THEN
    CREATE UNIQUE INDEX ix_users_phone_number ON ess.users (phone_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250714120520_auto_14040423_153515') THEN
    INSERT INTO ess."__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20250714120520_auto_14040423_153515', '9.0.9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250716061403_auto_14040425_094353') THEN
    DELETE FROM ess.app_settings;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250716061403_auto_14040425_094353') THEN
    ALTER TABLE ess.app_settings ADD user_id character varying(500) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250716061403_auto_14040425_094353') THEN
    CREATE INDEX ix_app_settings_user_id ON ess.app_settings (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250716061403_auto_14040425_094353') THEN
    ALTER TABLE ess.app_settings ADD CONSTRAINT fk_app_settings_users_user_id FOREIGN KEY (user_id) REFERENCES ess.users (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250716061403_auto_14040425_094353') THEN
    INSERT INTO ess."__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20250716061403_auto_14040425_094353', '9.0.9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250720053755_auto_14040429_090749') THEN
    ALTER TABLE ess.app_settings DROP CONSTRAINT fk_app_settings_users_user_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250720053755_auto_14040429_090749') THEN
    DROP INDEX ess.ix_app_settings_user_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250720053755_auto_14040429_090749') THEN
    ALTER TABLE ess.app_settings DROP COLUMN user_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250720053755_auto_14040429_090749') THEN
    INSERT INTO ess."__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20250720053755_auto_14040429_090749', '9.0.9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250915104654_auto_14040624_141636') THEN
    ALTER TABLE ess.users ADD avatar_key character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM ess."__EFMigrationsHistory" WHERE "migration_id" = '20250915104654_auto_14040624_141636') THEN
    INSERT INTO ess."__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20250915104654_auto_14040624_141636', '9.0.9');
    END IF;
END $EF$;
COMMIT;

