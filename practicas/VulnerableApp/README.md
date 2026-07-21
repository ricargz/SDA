# VulnerableApp

`VulnerableApp` es una aplicacion ASP.NET Core MVC usada como laboratorio academico para identificar, explotar de forma controlada, remediar y observar vulnerabilidades comunes alineadas con OWASP.

El proyecto nacio como una aplicacion deliberadamente vulnerable y despues fue llevado a una rama/estado seguro. Por eso el repositorio conserva evidencias, reportes, migraciones, scripts e infraestructura de observabilidad que muestran la evolucion completa: linea base vulnerable, explotacion local, remediacion, analisis con SonarQube, logging con Serilog/Seq y observabilidad con Grafana, Loki y Promtail.

## Alcance y advertencia

Esta aplicacion debe ejecutarse solo en entorno local de laboratorio. No debe usarse para probar servicios, redes, cuentas, bases de datos o sistemas reales. Los payloads documentados son exclusivamente para validar controles dentro de `VulnerableApp`.

## Tecnologias principales

| Componente | Uso |
| --- | --- |
| ASP.NET Core MVC `net10.0` | Aplicacion web y controladores MVC/API. |
| Entity Framework Core | Acceso a datos y migraciones. |
| SQL Server LocalDB | Base de datos local `VulnerableDb`. |
| BCrypt.Net-Next | Hash y verificacion segura de contrasenas. |
| Serilog | Logging estructurado. |
| Seq | Exploracion local de eventos Serilog. |
| Docker Compose | Infraestructura de Seq, SonarQube, Loki, Promtail y Grafana. |
| Grafana + Loki + Promtail | Plataforma de observabilidad y consultas LogQL. |
| xUnit | Pruebas automatizadas de seguridad, controladores y middleware. |
| SonarQube | Analisis estatico de calidad, seguridad y mantenibilidad. |

## Estructura relevante del repositorio

```text
practicas/
|-- VulnerableApp/                  # Aplicacion ASP.NET Core MVC
|   |-- Controllers/                # Home, Auth, Search, Comment, Api
|   |-- Data/                       # AppDbContext y datos semilla
|   |-- Middleware/                 # CorrelationId, request logging, exception logging
|   |-- Models/                     # Modelo User
|   |-- Security/                   # Detectores SQLi/XSS y sanitizacion para logs
|   |-- Services/                   # Almacen de comentarios en memoria
|   |-- Views/                      # Vistas MVC
|   |-- Migrations/                 # InitialCreate y SecureRemediation
|   |-- scripts/                    # SonarQube, carga de logs y analisis
|   |-- reportes/                   # Reportes de practicas
|   |-- evidencias/                 # Capturas, JSON y evidencias por practica
|   |-- appsettings.json            # Serilog, conexion y AllowedHosts
|   |-- dotnet-tools.json           # dotnet-ef local
|   `-- README.md
|-- VulnerableApp.Tests/            # Pruebas automatizadas xUnit
|-- seq-infra/                      # Seq, Loki, Promtail y Grafana
|-- sonarqube-infra/                # SonarQube + PostgreSQL
`-- segg-u1-p1/                     # Practica previa con ejemplos OWASP en JS
```

## Estado actual de seguridad

La version actual es la version remediada. Las vulnerabilidades de las primeras practicas ya no deben ser explotables en ejecucion normal.

| Riesgo trabajado | Estado actual |
| --- | --- |
| SQL Injection | La busqueda usa LINQ con EF Core, no SQL concatenado. |
| Autenticacion insegura | El login busca por usuario y verifica `PasswordHash` con BCrypt. |
| Contrasenas en texto claro | El modelo actual usa `PasswordHash`; la API no expone hashes. |
| XSS almacenado/reflejado en comentarios | La vista codifica la salida con `Html.Encode`. |
| IDOR | `/api/user/{id}` requiere sesion y valida ownership. |
| Exposicion masiva de API | `/api/users` requiere sesion y retorna solo `Id`, `Username`, `Email`. |
| CSRF | Formularios POST usan antiforgery token. |
| Sesion | Cookies `HttpOnly`, `SameSite=Strict`, `SecurePolicy=Always`, timeout de 20 min. |
| Logging sensible | No se registran contrasenas; comentarios se registran por longitud. |

## Requisitos

- Windows con PowerShell.
- .NET 10 SDK.
- SQL Server LocalDB.
- Docker Desktop, si se desea levantar Seq, SonarQube, Loki, Promtail o Grafana.
- Herramientas locales restauradas desde `VulnerableApp/dotnet-tools.json`.

## Preparacion inicial

Desde la raiz del repositorio:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas
dotnet restore
dotnet build
```

Restaurar `dotnet-ef` y aplicar migraciones:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas\VulnerableApp
dotnet tool restore
dotnet tool run dotnet-ef database update
```

La cadena de conexion por defecto esta en `VulnerableApp/appsettings.json`:

```text
Server=(localdb)\mssqllocaldb;Database=VulnerableDb;Trusted_Connection=true;
```

## Como correr la aplicacion

Perfil HTTP:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas
dotnet run --project VulnerableApp\VulnerableApp.csproj --launch-profile http
```

URL:

```text
http://localhost:5088
```

Perfil HTTPS:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas
dotnet run --project VulnerableApp\VulnerableApp.csproj --launch-profile https
```

URLs:

```text
https://localhost:7243
http://localhost:5088
```

## Credenciales de prueba

Las credenciales vulnerables historicas (`admin/admin`, `user1/123456`, `user2/password`) pertenecen a la linea base inicial y ya no deben autenticar en la version segura.

| Usuario | Contrasena actual |
| --- | --- |
| `admin` | `Admin#2026!` |
| `user1` | `User1#2026!` |
| `user2` | `User2#2026!` |

## Rutas y funcionalidades

| Ruta | Metodo | Descripcion | Que probar |
| --- | --- | --- | --- |
| `/` | GET | Pagina principal. | Verificar que la app responde y genera logs. |
| `/Home/Privacy` | GET | Vista simple de privacidad. | Validar request logging. |
| `/Home/ControlledException` | GET | Genera una excepcion controlada. | Debe responder `422` y registrar Warning. |
| `/Home/UnhandledException` | GET | Genera excepcion no controlada. | Debe responder `500` con `correlationId`. |
| `/Auth/Login` | GET/POST | Login con sesion. | Probar credenciales validas e invalidas. |
| `/Auth/Dashboard` | GET | Panel del usuario autenticado. | Debe redirigir al login si no hay sesion. |
| `/Auth/Logout` | GET | Cierre de sesion. | Debe limpiar la sesion. |
| `/Search/Index?search=admin` | GET | Busqueda de usuarios. | Debe encontrar coincidencias normales. |
| `/Search/Index?search=' OR '1'='1` | GET | Payload de SQLi controlado. | No debe devolver todos los usuarios; debe generar Warning. |
| `/Comment/Index` | GET | Listado/formulario de comentarios. | Debe mostrar comentarios codificados. |
| `/Comment/AddComment` | POST | Agrega comentario. | Requiere antiforgery token desde el formulario. |
| `/api/user/{id}` | GET | Consulta del usuario autenticado por id. | Sin sesion: `401`; otro id: `403`; propio id: `200`. |
| `/api/users` | GET | Lista usuarios sin campos sensibles. | Sin sesion: `401`; con sesion: `200`. |

## Pruebas manuales sugeridas

1. Login seguro:

```text
URL: http://localhost:5088/Auth/Login
Usuario: admin
Contrasena: Admin#2026!
Resultado esperado: redireccion a Dashboard.
```

2. Credencial historica insegura:

```text
Usuario: admin
Contrasena: admin
Resultado esperado: credenciales invalidas.
```

3. SQL Injection en busqueda:

```text
http://localhost:5088/Search/Index?search=' OR '1'='1
Resultado esperado: no se devuelven todos los usuarios y se registra Warning.
```

4. XSS en comentarios:

```text
Comentario: <script>alert('XSS')</script>
Resultado esperado: el texto se muestra codificado; no se ejecuta alerta.
```

5. IDOR:

```text
Iniciar sesion como admin.
Consultar http://localhost:5088/api/user/2
Resultado esperado: 403 Forbidden.
```

6. API sin sesion:

```text
http://localhost:5088/api/users
Resultado esperado: 401 Unauthorized.
```

7. Excepcion no controlada:

```text
http://localhost:5088/Home/UnhandledException
Resultado esperado: 500 con ProblemDetails y X-Correlation-ID.
```

## Pruebas automatizadas

Ejecutar:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas
dotnet test VulnerableApp.Tests\VulnerableApp.Tests.csproj
```

La suite valida:

- generacion y propagacion de `X-Correlation-ID`;
- respuesta segura del middleware de excepciones;
- logging HTTP con metodo, ruta, estado, duracion y correlacion;
- deteccion de patrones SQL Injection y XSS;
- sanitizacion de caracteres de control para logs;
- instrumentacion de acciones en `HomeController`, `SearchController`, `AuthController`, `CommentController` y `ApiController`;
- ausencia de contrasenas en eventos capturados;
- ramas `401`, `403`, `200`, warnings y errores.

## Logs locales

Serilog esta configurado en `VulnerableApp/appsettings.json`.

Sinks habilitados:

| Sink | Destino | Uso |
| --- | --- | --- |
| Console | Terminal de `dotnet run` | Diagnostico inmediato. |
| File | `VulnerableApp/Logs/log-.txt` con rotacion diaria | Evidencia local y recoleccion por Promtail. |
| Seq | `http://localhost:5341` como ingestion | Exploracion estructurada en Seq. |

Los archivos se generan como:

```text
VulnerableApp/Logs/log-YYYYMMDD.txt
```

Formato principal:

```text
Timestamp [Nivel] [CorrelationId] SourceContext Mensaje Excepcion
```

Ejemplos de busqueda local:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas\VulnerableApp
Get-ChildItem Logs
Select-String -Path Logs\log-*.txt -Pattern "SQL Injection"
Select-String -Path Logs\log-*.txt -Pattern "Autenticacion fallida"
Select-String -Path Logs\log-*.txt -Pattern "XSS"
Select-String -Path Logs\log-*.txt -Pattern "CorrelationId"
```

## Seq

Levantar infraestructura de logs:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas
docker compose -f seq-infra\docker-compose.yml up -d seq
```

Abrir:

```text
http://localhost:8081
```

Ingestion Serilog:

```text
http://localhost:5341
```

Consultas utiles en Seq:

```text
SourceContext = 'VulnerableApp.Controllers.AuthController'
SourceContext = 'VulnerableApp.Controllers.SearchController'
@Level = 'Warning'
@Level = 'Error'
Contains(@Message, 'SQL Injection')
Contains(@Message, 'XSS')
CorrelationId = 'valor-del-correlation-id'
```

## Grafana, Loki y Promtail

Levantar la plataforma completa:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas
docker compose -f seq-infra\docker-compose.yml up -d
```

Servicios:

| Servicio | URL/Puerto | Uso |
| --- | --- | --- |
| Seq | `http://localhost:8081` | Exploracion Serilog. |
| Loki | `http://localhost:3100` | Almacenamiento/consulta de logs. |
| Grafana | `http://localhost:3000` | Dashboards y Explore. |
| Promtail | interno | Lee `VulnerableApp/Logs/log-*.txt`. |

Grafana queda provisionado con usuario/contrasena `admin/admin` y tambien con acceso anonimo de administrador para laboratorio local.

Promtail lee los logs mediante bind mount:

```text
Host:       C:\Projects\cuatrimestre8\sdapps\SDA\practicas\VulnerableApp\Logs
Contenedor: /var/log/vulnerableapp
```

Labels principales en Loki:

| Label | Valores relevantes |
| --- | --- |
| `application` | `VulnerableApp` |
| `environment` | `dev` |
| `job` | `vulnerableapp` |
| `log_type` | `Application`, `Security`, `Audit` |
| `module` | `application`, `security`, `authentication`, `orders` |
| `level` | `INF`, `WRN`, `ERR` |
| `source_context` | clase/controlador origen |

Consultas LogQL utiles:

```logql
{application="VulnerableApp", environment="dev"}
{application="VulnerableApp", environment="dev", log_type="Security"}
{application="VulnerableApp", environment="dev", log_type="Audit"}
{application="VulnerableApp", environment="dev", level="ERR"}
{application="VulnerableApp", environment="dev", level="WRN"}
{application="VulnerableApp", environment="dev", module="authentication"} |= "Autenticacion fallida"
{application="VulnerableApp", environment="dev", module="security"} |~ "(SQL Injection|XSS)"
{application="VulnerableApp", environment="dev"} |~ "(ERR|Exception|InvalidOperationException|500)"
```

Validaciones rapidas:

```powershell
Invoke-WebRequest -Uri http://127.0.0.1:3100/ready -UseBasicParsing
Invoke-WebRequest -Uri http://127.0.0.1:3000/api/health -UseBasicParsing
Invoke-WebRequest -Uri 'http://127.0.0.1:3100/loki/api/v1/labels' -UseBasicParsing
docker logs vulnerableapp-promtail --tail 120
```

Detener la plataforma:

```powershell
docker compose -f seq-infra\docker-compose.yml down
```

## Generar carga y analizar logs

Antes de generar carga, levantar la app y definir la contrasena de prueba:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas
$env:P3G_TEST_PASSWORD = "Admin#2026!"
```

Ejecutar carga:

```powershell
.\VulnerableApp\scripts\run-p3g-observability-load.ps1
```

Analizar logs generados:

```powershell
.\VulnerableApp\scripts\analyze-p3g-logs.ps1
```

La carga genera actividad sobre:

- visitas a Home;
- busquedas validas, vacias, especiales y con patrones SQLi;
- logins validos e invalidos;
- comentarios validos y con patrones XSS;
- consultas API validas e invalidas;
- excepciones controladas y no controladas.

Resultados por defecto:

```text
VulnerableApp/evidencias/P3G-Continuacion/load-results.json
VulnerableApp/evidencias/P3G-Continuacion/analysis-results.json
```

## SonarQube

Levantar SonarQube:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas\sonarqube-infra
docker compose up -d
```

Abrir:

```text
http://localhost:9000
```

Crear el proyecto `VulnerableApp`, generar un token y definirlo solo en la terminal:

```powershell
$env:SONAR_TOKEN = "token_generado_en_sonarqube"
```

Ejecutar analisis:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas\VulnerableApp
.\scripts\run-sonarqube-analysis.ps1
```

El script:

- inicia `dotnet sonarscanner begin`;
- excluye `Migrations`, `wwwroot/lib`, `bin` y `obj`;
- compila con `dotnet build --no-incremental`;
- detecta proyectos con `Microsoft.NET.Test.Sdk`;
- ejecuta pruebas con cobertura OpenCover;
- finaliza con `dotnet sonarscanner end`.

## Resumen de practicas realizadas

### U1/P1 - Ejemplos OWASP vulnerables y seguros

Ubicacion: `segg-u1-p1/`.

Se documentaron ejemplos en JavaScript de:

- A03 Injection: consulta SQL concatenada vs consulta parametrizada;
- A01 Broken Access Control: consulta directa por id vs validacion de propietario/rol;
- A02 Cryptographic Failures: hash debil vs BCrypt con salt y costo.

Esta practica sirvio como antecedente conceptual de las vulnerabilidades implementadas despues en `VulnerableApp`.

### P2A - Linea base de VulnerableApp

Se preparo la aplicacion vulnerable inicial:

- creacion del proyecto MVC con `dotnet new mvc -n VulnerableApp -f net10.0`;
- instalacion de paquetes EF Core SQL Server, Tools y Design;
- creacion del manifiesto local de herramientas y `dotnet-ef`;
- generacion de la migracion `InitialCreate`;
- compilacion correcta con `dotnet build`;
- creacion de `VulnerableDb` en SQL Server LocalDB;
- tabla `Users` con datos semilla;
- contrasenas historicas en texto plano (`admin`, `123456`, `password`);
- rutas historicas de prueba: `/Search/Index`, `/Auth/Login` y `/Auth/Dashboard`;
- evidencias visuales en `VulnerableApp/evidencias/P2A/`.

La migracion `InitialCreate` conserva esta linea base para contexto historico. La guia original de instalacion esta en `VulnerableApp/instalacion.md`.

### P2B - SQL Injection

Reporte: `VulnerableApp/reportes/P2B_SQL_Injection.docx`.

Se identifico que `SearchController.Index` construia una consulta vulnerable mediante concatenacion/SQL dinamico. El payload de laboratorio principal fue:

```text
' OR '1'='1
```

Riesgo observado: exposicion de datos por consulta no parametrizada.

Remediacion actual:

- busqueda con LINQ/EF Core;
- normalizacion con `Trim()`;
- deteccion de patrones sospechosos con `SecurityPatternDetector`;
- registro `Warning` sin ejecutar SQL dinamico.

### P2C - Autenticacion insegura

Reporte: `VulnerableApp/reportes/P2C_Autenticacion.docx`.

Hallazgos iniciales:

- credencial predeterminada `admin/admin`;
- contrasenas en texto claro;
- validacion de login con consulta SQL concatenada;
- riesgo de bypass con entrada manipulada.

Remediacion actual:

- `AuthController.Login` busca por usuario normalizado;
- `BCrypt.Verify` valida `PasswordHash`;
- credenciales historicas ya no autentican;
- el password nunca se envia al logger;
- formularios POST usan antiforgery token.

### P2D - XSS

Reporte: `VulnerableApp/reportes/P2D_XSS.docx`.

Hallazgo inicial:

- el modulo de comentarios renderizaba contenido no confiable con `Html.Raw(comment)`;
- un payload como `<script>alert('XSS')</script>` podia ejecutarse en el navegador.

Remediacion actual:

- `Views/Comment/Index.cshtml` usa `Html.Encode(comment)`;
- comentarios vacios se rechazan;
- `InMemoryCommentStore` limita longitud y cantidad;
- patrones XSS generan `Warning` sin guardar contenido en logs.

### P2E - IDOR y exposicion de API

Reporte: `VulnerableApp/reportes/P2E_IDOR.docx`.

Hallazgos iniciales:

- `/api/user/{id}` permitia consultar usuarios por ids directos;
- `/api/users` exponia listado completo;
- respuestas incluian campos sensibles como `Password`, `Balance` y datos de terceros;
- ids consecutivos facilitaban enumeracion.

Remediacion actual:

- se requiere sesion para `/api/user/{id}` y `/api/users`;
- `/api/user/{id}` valida que el id solicitado sea el del usuario autenticado;
- si no hay sesion responde `401`;
- si el id no pertenece al usuario responde `403`;
- las respuestas solo incluyen `Id`, `Username` y `Email`.

### P2F - Remediacion consolidada

Reporte: `VulnerableApp/reportes/P2F_Reporte_Final_Consolidado.docx`.

Se consolidaron las mitigaciones:

- SQL Injection mitigado con LINQ/EF;
- autenticacion segura con BCrypt;
- contrasenas migradas de `Password` a `PasswordHash`;
- XSS mitigado con codificacion de salida;
- IDOR mitigado con sesion y ownership;
- API sin `Password` ni `PasswordHash`;
- validaciones esperadas documentadas para cada riesgo.

La migracion `SecureRemediation` refleja el cambio de columna `Password` a `PasswordHash` y actualiza los datos semilla con hashes BCrypt.

### P3 - SonarQube

Reporte: `VulnerableApp/reportes/P3_SonarQube.md`.

Se agrego infraestructura Docker para SonarQube en `sonarqube-infra/` y un script reproducible de analisis en:

```text
VulnerableApp/scripts/run-sonarqube-analysis.ps1
```

Hallazgos corregidos durante esta practica:

- formularios POST sin CSRF;
- cookie de sesion con configuracion por defecto;
- lista estatica mutable para comentarios;
- entradas sin normalizacion;
- BCrypt calculado dentro de `OnModelCreating`;
- hosts permitidos demasiado amplios;
- respuestas y ramas de API mejoradas.

### P3G - Serilog e instrumentacion de controladores

Reporte: `VulnerableApp/reportes/P3G_Serilog_Instrumentacion.md`.

Se configuro Serilog con:

- consola;
- archivo rotativo;
- Seq;
- propiedad global `Application=VulnerableApp`;
- eventos estructurados por controlador, accion, usuario, IP, parametros seguros, resultado y duracion.

Se creo `InstrumentedController<TController>` y se instrumentaron acciones de:

- `HomeController`;
- `SearchController`;
- `AuthController`;
- `CommentController`;
- `ApiController`.

Resultado esperado: eventos `Information`, `Warning` y `Error` consultables sin registrar contrasenas.

### P3G continuacion - CorrelationId, excepciones y analisis de volumen

Reporte: `VulnerableApp/reportes/P3G_Reporte_Final_Observabilidad.md`.

Se agregaron tres middleware globales:

- `CorrelationIdMiddleware`: genera/propaga `X-Correlation-ID` y acepta `X-Test-Run-ID`;
- `RequestLoggingMiddleware`: registra metodo, ruta, status, duracion, usuario, IP y correlacion;
- `ExceptionLoggingMiddleware`: captura excepciones no controladas y responde ProblemDetails seguro.

Tambien se agregaron scripts de carga y analisis:

```text
VulnerableApp/scripts/run-p3g-observability-load.ps1
VulnerableApp/scripts/analyze-p3g-logs.ps1
```

El analisis documentado proceso un lote con miles de eventos, intentos fallidos de autenticacion, SQLi, XSS, excepciones controladas/no controladas y verificacion de que las contrasenas no aparecieran en logs.

### P4H-1 - Plataforma Grafana, Loki y Promtail

Reporte: `VulnerableApp/reportes/P4H_Plataforma_Observabilidad_Grafana_Loki_Promtail.md`.

Se extendio `seq-infra/docker-compose.yml` para agregar:

- Loki en `3100`;
- Promtail leyendo archivos `VulnerableApp/Logs/log-*.txt`;
- Grafana en `3000`;
- dashboards provisionados;
- datasource Loki provisionado.

La aplicacion no fue contenida en Docker; se mantiene ejecutandose con `dotnet run`. Docker aloja la infraestructura de observabilidad.

### P4H-2 - Enriquecimiento Promtail/Loki

Reporte: `VulnerableApp/reportes/P4H_2_Integracion_Enriquecimiento_Promtail_Loki.md`.

Se amplio `seq-infra/promtail/config.yml` para:

- monitorear `log-*.txt`, `security-*.txt` y `audit-*.txt`;
- extraer `level` y `source_context` por regex;
- clasificar eventos con `log_type=Application`, `Security` y `Audit`;
- agregar `environment=dev`;
- clasificar modulos como `application`, `security`, `authentication` y `orders`;
- validar consultas por labels en Grafana/Loki.

### P4H-3 - LogQL, dashboards e investigacion de incidentes

Reporte: `VulnerableApp/reportes/P4H_3_LogQL_Dashboards_Analisis_Registros.md`.

Se trabajaron consultas LogQL, Grafana Explore y un dashboard de monitoreo de seguridad:

```text
seq-infra/grafana/dashboards/vulnerableapp-logql-security-monitoring.json
```

Se investigaron escenarios de:

- errores;
- warnings;
- autenticaciones fallidas;
- eventos de auditoria;
- intentos SQL Injection/XSS;
- excepciones criticas;
- correlacion temporal de incidentes.

## Evidencias y reportes

Reportes principales:

```text
VulnerableApp/instalacion.md
VulnerableApp/reportes/P2B_SQL_Injection.docx
VulnerableApp/reportes/P2C_Autenticacion.docx
VulnerableApp/reportes/P2D_XSS.docx
VulnerableApp/reportes/P2E_IDOR.docx
VulnerableApp/reportes/P2F_Reporte_Final_Consolidado.docx
VulnerableApp/reportes/P3_SonarQube.md
VulnerableApp/reportes/P3G_Serilog_Instrumentacion.md
VulnerableApp/reportes/P3G_Reporte_Final_Observabilidad.md
VulnerableApp/reportes/P4H_Plataforma_Observabilidad_Grafana_Loki_Promtail.md
VulnerableApp/reportes/P4H_2_Integracion_Enriquecimiento_Promtail_Loki.md
VulnerableApp/reportes/P4H_3_LogQL_Dashboards_Analisis_Registros.md
```

Evidencias por carpeta:

```text
VulnerableApp/evidencias/P2A
VulnerableApp/evidencias/P2B
VulnerableApp/evidencias/P2C
VulnerableApp/evidencias/P2D
VulnerableApp/evidencias/P2E
VulnerableApp/evidencias/P2F
VulnerableApp/evidencias/P3G
VulnerableApp/evidencias/P3G-Continuacion
VulnerableApp/evidencias/P4H
VulnerableApp/evidencias/P4H-2
VulnerableApp/evidencias/P4H-3
```

## Solucion de problemas

Si `dotnet-ef` no existe:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas\VulnerableApp
dotnet tool restore
```

Si la base no existe o esta desactualizada:

```powershell
dotnet tool run dotnet-ef database update
```

Si la app no envia eventos a Seq:

1. Verificar que Seq este activo en `http://localhost:8081`.
2. Verificar ingestion en `http://localhost:5341`.
3. Revisar que `appsettings.json` tenga `serverUrl: http://localhost:5341`.
4. Reiniciar la aplicacion.

Si Grafana no muestra logs:

1. Verificar que la app haya generado archivos en `VulnerableApp/Logs`.
2. Verificar `docker ps --filter "name=vulnerableapp"`.
3. Revisar `docker logs vulnerableapp-promtail --tail 120`.
4. Confirmar Loki con `http://127.0.0.1:3100/ready`.
5. Revisar que el rango de tiempo de Grafana incluya la hora de los eventos.

Si falla la carga P3G por antiforgery o sesion:

1. Confirmar que la app este corriendo en `http://localhost:5088`.
2. Definir `$env:P3G_TEST_PASSWORD = "Admin#2026!"`.
3. Ejecutar la carga desde la raiz `practicas`.

Si SonarQube rechaza el analisis:

1. Confirmar que `http://localhost:9000` este disponible.
2. Confirmar que el proyecto `VulnerableApp` exista.
3. Confirmar que `$env:SONAR_TOKEN` este definido en la terminal actual.
4. No guardar el token en archivos del repositorio.

## Comando rapido de trabajo

Para una sesion normal con app, Seq, Loki, Promtail y Grafana:

```powershell
cd C:\Projects\cuatrimestre8\sdapps\SDA\practicas
docker compose -f seq-infra\docker-compose.yml up -d
dotnet run --project VulnerableApp\VulnerableApp.csproj --launch-profile http
```

Abrir:

```text
Aplicacion: http://localhost:5088
Seq:        http://localhost:8081
Grafana:    http://localhost:3000
Loki:       http://localhost:3100
```
