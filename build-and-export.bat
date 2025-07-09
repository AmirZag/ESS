@echo off
setlocal EnableDelayedExpansion

REM ESS API Production Build and Export Script for Windows
echo ==============================================
echo ESS API Production Build and Export Script
echo ==============================================

REM Configuration
set PROJECT_NAME=amard_ess
set API_IMAGE_NAME=essapi:production
set DEPLOYMENT_DIR=deployment-package
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set BUILD_DATE=%dt:~0,4%%dt:~4,2%%dt:~6,2%_%dt:~8,2%%dt:~10,2%%dt:~12,2%
set PACKAGE_NAME=ess-api-deployment-%BUILD_DATE%

REM Hardcoded credentials (as requested)
set DB_USERNAME=postgres
set DB_PASSWORD=postgres
set DB_NAME=amard_ess
set PGADMIN_USERNAME=postgres
set PGADMIN_PASSWORD=postgres
set ASPIRE_API_KEY=Rez@09119029195

echo [INFO] Starting build process...

REM Check prerequisites
echo [INFO] Checking prerequisites...

docker --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker is not installed or not in PATH
    pause
    exit /b 1
)

docker-compose --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker Compose is not installed or not in PATH
    pause
    exit /b 1
)

dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] .NET SDK is not installed or not in PATH
    pause
    exit /b 1
)

echo [SUCCESS] All prerequisites are available

REM Clean previous builds
echo [INFO] Cleaning up previous builds...
if exist "%DEPLOYMENT_DIR%" rmdir /s /q "%DEPLOYMENT_DIR%"
docker-compose -f docker-compose.yml down --remove-orphans >nul 2>&1
docker rmi "%API_IMAGE_NAME%" >nul 2>&1
echo [SUCCESS] Cleanup completed

REM Build the application
echo [INFO] Building .NET application...
dotnet restore Amard.ESS.sln
if errorlevel 1 (
    echo [ERROR] Failed to restore packages
    pause
    exit /b 1
)

dotnet build Amard.ESS.sln --configuration Release --no-restore
if errorlevel 1 (
    echo [ERROR] Failed to build application
    pause
    exit /b 1
)
echo [SUCCESS] Application built successfully

REM Build Docker images
echo [INFO] Building Docker images...
docker build -f src/ESS.Api/Dockerfile -t "%API_IMAGE_NAME%" .
if errorlevel 1 (
    echo [ERROR] Failed to build Docker image
    pause
    exit /b 1
)

echo [INFO] Pulling required base images...
docker pull postgres:16-alpine
docker pull dpage/pgadmin4:8
docker pull mcr.microsoft.com/dotnet/aspire-dashboard:9.3
echo [SUCCESS] Docker images built successfully

REM Export Docker images
echo [INFO] Exporting Docker images...
mkdir "%DEPLOYMENT_DIR%\images" 2>nul

echo [INFO] Exporting API image...
docker save -o "%DEPLOYMENT_DIR%\images\ess-api.tar" "%API_IMAGE_NAME%"
if errorlevel 1 (
    echo [ERROR] Failed to export API image
    pause
    exit /b 1
)

echo [INFO] Exporting PostgreSQL image...
docker save -o "%DEPLOYMENT_DIR%\images\postgres.tar" postgres:16-alpine
if errorlevel 1 (
    echo [ERROR] Failed to export PostgreSQL image
    pause
    exit /b 1
)

echo [INFO] Exporting PgAdmin image...
docker save -o "%DEPLOYMENT_DIR%\images\pgadmin.tar" dpage/pgadmin4:8
if errorlevel 1 (
    echo [ERROR] Failed to export PgAdmin image
    pause
    exit /b 1
)

echo [INFO] Exporting Aspire Dashboard image...
docker save -o "%DEPLOYMENT_DIR%\images\aspire-dashboard.tar" mcr.microsoft.com/dotnet/aspire-dashboard:9.3
if errorlevel 1 (
    echo [ERROR] Failed to export Aspire Dashboard image
    pause
    exit /b 1
)

echo [SUCCESS] All Docker images exported successfully

REM Create deployment structure
echo [INFO] Creating deployment package structure...
mkdir "%DEPLOYMENT_DIR%\config" 2>nul
mkdir "%DEPLOYMENT_DIR%\config\SqlScripts" 2>nul
mkdir "%DEPLOYMENT_DIR%\scripts" 2>nul
mkdir "%DEPLOYMENT_DIR%\logs" 2>nul
mkdir "%DEPLOYMENT_DIR%\reports" 2>nul
mkdir "%DEPLOYMENT_DIR%\backup" 2>nul

REM Copy configuration files and SQL scripts
echo [INFO] Copying configuration files...
if exist "config\appsettings.Production.json" (
    copy "config\appsettings.Production.json" "%DEPLOYMENT_DIR%\config\" >nul
    echo [SUCCESS] appsettings.Production.json copied
) else (
    echo [WARNING] config\appsettings.Production.json not found. Creating basic one...
    call :create_appsettings
)

REM Copy SQL migration scripts
if exist "config\SqlScripts\ApplicationDbContext_Migration.sql" (
    copy "config\SqlScripts\ApplicationDbContext_Migration.sql" "%DEPLOYMENT_DIR%\config\SqlScripts\" >nul
    echo [SUCCESS] ApplicationDbContext_Migration.sql copied
) else (
    echo [WARNING] config\SqlScripts\ApplicationDbContext_Migration.sql not found!
    echo [WARNING] You will need to run migrations manually.
)

if exist "config\SqlScripts\ApplicationIdentityDbContext_Migration.sql" (
    copy "config\SqlScripts\ApplicationIdentityDbContext_Migration.sql" "%DEPLOYMENT_DIR%\config\SqlScripts\" >nul
    echo [SUCCESS] ApplicationIdentityDbContext_Migration.sql copied
) else (
    echo [WARNING] config\SqlScripts\ApplicationIdentityDbContext_Migration.sql not found!
    echo [WARNING] You will need to run migrations manually.
)

REM Create docker-compose.yml using simple method
echo [INFO] Creating docker-compose.yml...
call :create_docker_compose

REM Create PostgreSQL configuration
echo [INFO] Creating PostgreSQL configuration...
call :create_postgres_config

REM Create PgAdmin servers configuration
echo [INFO] Creating PgAdmin servers configuration...
call :create_pgadmin_config

echo [SUCCESS] Deployment structure created

REM Create installation script
echo [INFO] Creating installation script...
call :create_install_script

REM Create utility scripts
echo [INFO] Creating utility scripts...
call :create_utility_scripts

REM Create documentation
echo [INFO] Creating documentation...
call :create_documentation

REM Create package info
echo [INFO] Creating package info...
call :create_package_info

echo.
echo [SUCCESS] ==============================================
echo [SUCCESS] Build and Export Completed Successfully!
echo [SUCCESS] ==============================================
echo.
echo [INFO] Deployment package: %DEPLOYMENT_DIR%
echo [INFO] Database: %DB_NAME%
echo [INFO] Database User: %DB_USERNAME%
echo [INFO] Database Pass: %DB_PASSWORD%
echo [INFO] PgAdmin User: %DB_USERNAME%@ess.local
echo [INFO] PgAdmin Pass: %DB_PASSWORD%
echo [INFO] Aspire API Key: %ASPIRE_API_KEY%
echo.
echo [INFO] Access URLs after installation:
echo [INFO] - API: http://localhost:5000
echo [INFO] - PgAdmin: http://localhost:5050
echo [INFO] - Monitoring: http://localhost:18888
echo.
echo [SUCCESS] Ready to deploy! Run install.bat in the deployment package.
echo.
pause
goto :end

REM ===============================================
REM SUBROUTINES - Broken into smaller functions
REM ===============================================

:create_appsettings
echo { > "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo   "Logging": { >> "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo     "LogLevel": { >> "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo       "Default": "Information", >> "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo       "Microsoft.AspNetCore": "Warning" >> "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo     } >> "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo   }, >> "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo   "AllowedHosts": "*", >> "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo   "ConnectionStrings": { >> "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo     "DefaultConnection": "Host=ess.postgres;Port=5432;Database=%DB_NAME%;Username=%DB_USERNAME%;Password=%DB_PASSWORD%", >> "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo     "IdentityConnection": "Host=ess.postgres;Port=5432;Database=%DB_NAME%;Username=%DB_USERNAME%;Password=%DB_PASSWORD%" >> "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo   } >> "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo } >> "%DEPLOYMENT_DIR%\config\appsettings.Production.json"
echo [SUCCESS] appsettings.Production.json created
goto :eof

:create_docker_compose
echo version: '3.8' > "%DEPLOYMENT_DIR%\docker-compose.yml"
echo. >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo services: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo   ess.postgres: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     image: postgres:16-alpine >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     container_name: ess-postgres >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     environment: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       POSTGRES_DB: %DB_NAME% >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       POSTGRES_USER: %DB_USERNAME% >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       POSTGRES_PASSWORD: %DB_PASSWORD% >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     ports: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       - "5432:5432" >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     volumes: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       - postgres_data:/var/lib/postgresql/data >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     restart: unless-stopped >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     networks: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       - ess-network >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo. >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo   ess.pgadmin: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     image: dpage/pgadmin4:8 >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     container_name: ess-pgadmin >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     environment: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       PGADMIN_DEFAULT_EMAIL: %PGADMIN_USERNAME%@ess.local >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       PGADMIN_DEFAULT_PASSWORD: %PGADMIN_PASSWORD% >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     ports: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       - "5050:8080" >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     volumes: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       - pgadmin_data:/var/lib/pgadmin >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     restart: unless-stopped >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     networks: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       - ess-network >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo. >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo   ess.api: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     image: %API_IMAGE_NAME% >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     container_name: ess-api >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     environment: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       ASPNETCORE_ENVIRONMENT: Production >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       ASPNETCORE_URLS: http://+:80 >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     ports: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       - "5000:80" >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     volumes: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       - ./config/appsettings.Production.json:/app/appsettings.Production.json >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     restart: unless-stopped >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     networks: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       - ess-network >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     depends_on: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       - ess.postgres >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo. >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo   ess.aspire: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     image: mcr.microsoft.com/dotnet/aspire-dashboard:9.3 >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     container_name: ess-aspire >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     environment: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       DOTNET_DASHBOARD_FRONTEND_APIKEY: %ASPIRE_API_KEY% >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     ports: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       - "18888:18888" >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     restart: unless-stopped >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     networks: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo       - ess-network >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo. >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo volumes: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo   postgres_data: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     driver: local >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo   pgadmin_data: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     driver: local >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo. >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo networks: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo   ess-network: >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo     driver: bridge >> "%DEPLOYMENT_DIR%\docker-compose.yml"
echo [SUCCESS] docker-compose.yml created
goto :eof

:create_postgres_config
echo listen_addresses = '*' > "%DEPLOYMENT_DIR%\config\postgres.conf"
echo max_connections = 100 >> "%DEPLOYMENT_DIR%\config\postgres.conf"
echo shared_buffers = 256MB >> "%DEPLOYMENT_DIR%\config\postgres.conf"
echo effective_cache_size = 1GB >> "%DEPLOYMENT_DIR%\config\postgres.conf"
echo work_mem = 4MB >> "%DEPLOYMENT_DIR%\config\postgres.conf"
echo [SUCCESS] postgres.conf created
goto :eof

:create_pgadmin_config
echo { > "%DEPLOYMENT_DIR%\config\servers.json"
echo     "Servers": { >> "%DEPLOYMENT_DIR%\config\servers.json"
echo         "1": { >> "%DEPLOYMENT_DIR%\config\servers.json"
echo             "Name": "ESS Production Database", >> "%DEPLOYMENT_DIR%\config\servers.json"
echo             "Group": "Servers", >> "%DEPLOYMENT_DIR%\config\servers.json"
echo             "Host": "ess.postgres", >> "%DEPLOYMENT_DIR%\config\servers.json"
echo             "Port": 5432, >> "%DEPLOYMENT_DIR%\config\servers.json"
echo             "MaintenanceDB": "%DB_NAME%", >> "%DEPLOYMENT_DIR%\config\servers.json"
echo             "Username": "%DB_USERNAME%" >> "%DEPLOYMENT_DIR%\config\servers.json"
echo         } >> "%DEPLOYMENT_DIR%\config\servers.json"
echo     } >> "%DEPLOYMENT_DIR%\config\servers.json"
echo } >> "%DEPLOYMENT_DIR%\config\servers.json"
echo [SUCCESS] servers.json created
goto :eof

:create_install_script
echo @echo off > "%DEPLOYMENT_DIR%\install.bat"
echo echo ESS API Production Installation >> "%DEPLOYMENT_DIR%\install.bat"
echo echo ================================ >> "%DEPLOYMENT_DIR%\install.bat"
echo echo. >> "%DEPLOYMENT_DIR%\install.bat"
echo echo Loading Docker images... >> "%DEPLOYMENT_DIR%\install.bat"
echo docker load -i images\ess-api.tar >> "%DEPLOYMENT_DIR%\install.bat"
echo docker load -i images\postgres.tar >> "%DEPLOYMENT_DIR%\install.bat"
echo docker load -i images\pgadmin.tar >> "%DEPLOYMENT_DIR%\install.bat"
echo docker load -i images\aspire-dashboard.tar >> "%DEPLOYMENT_DIR%\install.bat"
echo echo. >> "%DEPLOYMENT_DIR%\install.bat"
echo echo Starting services... >> "%DEPLOYMENT_DIR%\install.bat"
echo docker-compose up -d >> "%DEPLOYMENT_DIR%\install.bat"
echo echo. >> "%DEPLOYMENT_DIR%\install.bat"
echo echo Waiting for services to start... >> "%DEPLOYMENT_DIR%\install.bat"
echo timeout /t 15 /nobreak ^>nul >> "%DEPLOYMENT_DIR%\install.bat"
echo echo. >> "%DEPLOYMENT_DIR%\install.bat"
echo echo ============================================== >> "%DEPLOYMENT_DIR%\install.bat"
echo echo Installation completed! >> "%DEPLOYMENT_DIR%\install.bat"
echo echo ============================================== >> "%DEPLOYMENT_DIR%\install.bat"
echo echo. >> "%DEPLOYMENT_DIR%\install.bat"
echo echo Access URLs: >> "%DEPLOYMENT_DIR%\install.bat"
echo echo - API: http://localhost:5000 >> "%DEPLOYMENT_DIR%\install.bat"
echo echo - PgAdmin: http://localhost:5050 >> "%DEPLOYMENT_DIR%\install.bat"
echo echo - Monitoring: http://localhost:18888 >> "%DEPLOYMENT_DIR%\install.bat"
echo echo. >> "%DEPLOYMENT_DIR%\install.bat"
echo echo Database Info: >> "%DEPLOYMENT_DIR%\install.bat"
echo echo - Host: localhost:5432 >> "%DEPLOYMENT_DIR%\install.bat"
echo echo - Database: %DB_NAME% >> "%DEPLOYMENT_DIR%\install.bat"
echo echo - Username: %DB_USERNAME% >> "%DEPLOYMENT_DIR%\install.bat"
echo echo - Password: %DB_PASSWORD% >> "%DEPLOYMENT_DIR%\install.bat"
echo echo. >> "%DEPLOYMENT_DIR%\install.bat"
echo echo IMPORTANT: Run the migration scripts manually after installation! >> "%DEPLOYMENT_DIR%\install.bat"
echo echo Migration scripts are located in: config\SqlScripts\ >> "%DEPLOYMENT_DIR%\install.bat"
echo echo. >> "%DEPLOYMENT_DIR%\install.bat"
echo pause >> "%DEPLOYMENT_DIR%\install.bat"
echo [SUCCESS] install.bat created
goto :eof

:create_utility_scripts
echo @echo off > "%DEPLOYMENT_DIR%\scripts\health-check.bat"
echo echo Service Status: >> "%DEPLOYMENT_DIR%\scripts\health-check.bat"
echo docker-compose ps >> "%DEPLOYMENT_DIR%\scripts\health-check.bat"
echo pause >> "%DEPLOYMENT_DIR%\scripts\health-check.bat"

echo @echo off > "%DEPLOYMENT_DIR%\scripts\backup.bat"
echo echo Creating backup... >> "%DEPLOYMENT_DIR%\scripts\backup.bat"
echo mkdir backup 2^>nul >> "%DEPLOYMENT_DIR%\scripts\backup.bat"
echo docker-compose exec -T ess.postgres pg_dump -U %DB_USERNAME% %DB_NAME% ^> backup\database_%date:~10,4%%date:~4,2%%date:~7,2%.sql >> "%DEPLOYMENT_DIR%\scripts\backup.bat"
echo echo Backup completed! >> "%DEPLOYMENT_DIR%\scripts\backup.bat"
echo pause >> "%DEPLOYMENT_DIR%\scripts\backup.bat"

echo @echo off > "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo echo Running database migrations... >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo echo. >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo if exist "..\config\SqlScripts\ApplicationDbContext_Migration.sql" ( >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo     echo Running ApplicationDbContext migration... >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo     docker-compose exec -T ess.postgres psql -U %DB_USERNAME% -d %DB_NAME% ^< ..\config\SqlScripts\ApplicationDbContext_Migration.sql >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo     echo ApplicationDbContext migration completed. >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo ) else ( >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo     echo ApplicationDbContext_Migration.sql not found! >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo ) >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo echo. >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo if exist "..\config\SqlScripts\ApplicationIdentityDbContext_Migration.sql" ( >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo     echo Running ApplicationIdentityDbContext migration... >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo     docker-compose exec -T ess.postgres psql -U %DB_USERNAME% -d %DB_NAME% ^< ..\config\SqlScripts\ApplicationIdentityDbContext_Migration.sql >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo     echo ApplicationIdentityDbContext migration completed. >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo ) else ( >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo     echo ApplicationIdentityDbContext_Migration.sql not found! >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo ) >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo echo. >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo echo Migration process finished! >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"
echo pause >> "%DEPLOYMENT_DIR%\scripts\run-migrations.bat"

echo @echo off > "%DEPLOYMENT_DIR%\scripts\stop-services.bat"
echo echo Stopping all services... >> "%DEPLOYMENT_DIR%\scripts\stop-services.bat"
echo docker-compose down >> "%DEPLOYMENT_DIR%\scripts\stop-services.bat"
echo echo Services stopped. >> "%DEPLOYMENT_DIR%\scripts\stop-services.bat"
echo pause >> "%DEPLOYMENT_DIR%\scripts\stop-services.bat"

echo @echo off > "%DEPLOYMENT_DIR%\scripts\restart-services.bat"
echo echo Restarting all services... >> "%DEPLOYMENT_DIR%\scripts\restart-services.bat"
echo docker-compose restart >> "%DEPLOYMENT_DIR%\scripts\restart-services.bat"
echo echo Services restarted. >> "%DEPLOYMENT_DIR%\scripts\restart-services.bat"
echo pause >> "%DEPLOYMENT_DIR%\scripts\restart-services.bat"

echo [SUCCESS] Utility scripts created
goto :eof

:create_documentation
echo # ESS API Production Deployment > "%DEPLOYMENT_DIR%\README.md"
echo. >> "%DEPLOYMENT_DIR%\README.md"
echo ## Installation Steps >> "%DEPLOYMENT_DIR%\README.md"
echo 1. Run `install.bat` to install and start all services >> "%DEPLOYMENT_DIR%\README.md"
echo 2. Wait for all services to start (about 15 seconds) >> "%DEPLOYMENT_DIR%\README.md"
echo 3. Run `scripts\run-migrations.bat` to apply database migrations >> "%DEPLOYMENT_DIR%\README.md"
echo. >> "%DEPLOYMENT_DIR%\README.md"
echo ## Access URLs >> "%DEPLOYMENT_DIR%\README.md"
echo - API: http://localhost:5000 >> "%DEPLOYMENT_DIR%\README.md"
echo - PgAdmin: http://localhost:5050 >> "%DEPLOYMENT_DIR%\README.md"
echo - Monitoring Dashboard: http://localhost:18888 >> "%DEPLOYMENT_DIR%\README.md"
echo. >> "%DEPLOYMENT_DIR%\README.md"
echo ## Credentials >> "%DEPLOYMENT_DIR%\README.md"
echo ### Database >> "%DEPLOYMENT_DIR%\README.md"
echo - Host: localhost >> "%DEPLOYMENT_DIR%\README.md"
echo - Port: 5432 >> "%DEPLOYMENT_DIR%\README.md"
echo - Database: %DB_NAME% >> "%DEPLOYMENT_DIR%\README.md"
echo - Username: %DB_USERNAME% >> "%DEPLOYMENT_DIR%\README.md"
echo - Password: %DB_PASSWORD% >> "%DEPLOYMENT_DIR%\README.md"
echo. >> "%DEPLOYMENT_DIR%\README.md"
echo ### PgAdmin >> "%DEPLOYMENT_DIR%\README.md"
echo - Email: %DB_USERNAME%@ess.local >> "%DEPLOYMENT_DIR%\README.md"
echo - Password: %DB_PASSWORD% >> "%DEPLOYMENT_DIR%\README.md"
echo. >> "%DEPLOYMENT_DIR%\README.md"
echo ### Aspire Dashboard >> "%DEPLOYMENT_DIR%\README.md"
echo - API Key: %ASPIRE_API_KEY% >> "%DEPLOYMENT_DIR%\README.md"
echo. >> "%DEPLOYMENT_DIR%\README.md"
echo ## Utility Scripts >> "%DEPLOYMENT_DIR%\README.md"
echo - `scripts\health-check.bat` - Check service status >> "%DEPLOYMENT_DIR%\README.md"
echo - `scripts\backup.bat` - Create database backup >> "%DEPLOYMENT_DIR%\README.md"
echo - `scripts\run-migrations.bat` - Run database migrations >> "%DEPLOYMENT_DIR%\README.md"
echo - `scripts\stop-services.bat` - Stop all services >> "%DEPLOYMENT_DIR%\README.md"
echo - `scripts\restart-services.bat` - Restart all services >> "%DEPLOYMENT_DIR%\README.md"
echo. >> "%DEPLOYMENT_DIR%\README.md"
echo ## Troubleshooting >> "%DEPLOYMENT_DIR%\README.md"
echo If the database creation fails during installation: >> "%DEPLOYMENT_DIR%\README.md"
echo 1. Run `scripts\health-check.bat` to verify all services are running >> "%DEPLOYMENT_DIR%\README.md"
echo 2. Wait a few more seconds for PostgreSQL to fully initialize >> "%DEPLOYMENT_DIR%\README.md"
echo 3. Run `scripts\run-migrations.bat` manually >> "%DEPLOYMENT_DIR%\README.md"
echo [SUCCESS] README.md created
goto :eof

:create_package_info
echo ESS API Production Deployment Package > "%DEPLOYMENT_DIR%\package-info.txt"
echo Build Date: %date% %time% >> "%DEPLOYMENT_DIR%\package-info.txt"
echo Package: %PACKAGE_NAME% >> "%DEPLOYMENT_DIR%\package-info.txt"
echo. >> "%DEPLOYMENT_DIR%\package-info.txt"
echo Database Configuration: >> "%DEPLOYMENT_DIR%\package-info.txt"
echo - Database Name: %DB_NAME% >> "%DEPLOYMENT_DIR%\package-info.txt"
echo - Database User: %DB_USERNAME% >> "%DEPLOYMENT_DIR%\package-info.txt"
echo. >> "%DEPLOYMENT_DIR%\package-info.txt"
echo Services: >> "%DEPLOYMENT_DIR%\package-info.txt"
echo - ESS API (Port 5000) >> "%DEPLOYMENT_DIR%\package-info.txt"
echo - PostgreSQL Database (Port 5432) >> "%DEPLOYMENT_DIR%\package-info.txt"
echo - PgAdmin (Port 5050) >> "%DEPLOYMENT_DIR%\package-info.txt"
echo - Aspire Dashboard (Port 18888) >> "%DEPLOYMENT_DIR%\package-info.txt"
echo [SUCCESS] package-info.txt created
goto :eof

:end