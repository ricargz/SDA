# Cómo correr VulnerableApp

Esta guía contiene los comandos necesarios para levantar la aplicación `VulnerableApp`, ejecutar sus pruebas y correr los scripts de generación/análisis de evidencias de logging.

## 1. Abrir PowerShell en la carpeta del proyecto

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas
```

## 2. Restaurar dependencias y compilar

```powershell
dotnet restore
dotnet build
```

## 3. Levantar Seq para monitoreo de logs

Seq es la herramienta usada para visualizar los logs generados por Serilog.

```powershell
docker compose -f seq-infra\docker-compose.yml up -d
```

Una vez levantado, Seq estará disponible en:

```text
http://localhost:8081
```

## 4. Ejecutar la aplicación con HTTP

```powershell
dotnet run --project VulnerableApp\VulnerableApp.csproj --launch-profile http
```

La aplicación quedará disponible en:

```text
http://localhost:5088
```

## 5. Ejecutar la aplicación con HTTPS

Si se desea correr con HTTPS:

```powershell
dotnet run --project VulnerableApp\VulnerableApp.csproj --launch-profile https
```

URLs disponibles con este perfil:

```text
https://localhost:7243
http://localhost:5088
```

## 6. Ejecutar pruebas automatizadas

```powershell
dotnet test VulnerableApp.Tests\VulnerableApp.Tests.csproj
```

## 7. Ejecutar carga de pruebas y generar evidencias

Antes de ejecutar el script, define la contraseña de pruebas en una variable de entorno:

```powershell
$env:P3G_TEST_PASSWORD = "Admin#2026!"
```

Luego ejecuta la carga de pruebas:

```powershell
.\VulnerableApp\scripts\run-p3g-observability-load.ps1
```

Después analiza los logs generados:

```powershell
.\VulnerableApp\scripts\analyze-p3g-logs.ps1
```

## 8. Comando rápido recomendado

Para levantar todo lo necesario en una sesión normal de trabajo:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas
docker compose -f seq-infra\docker-compose.yml up -d
dotnet run --project VulnerableApp\VulnerableApp.csproj --launch-profile http
```

Después abre:

```text
Aplicación: http://localhost:5088
Seq:        http://localhost:8081
```

## 9. Detener Seq

Cuando ya no se necesite el monitor de logs:

```powershell
docker compose -f seq-infra\docker-compose.yml down
```

