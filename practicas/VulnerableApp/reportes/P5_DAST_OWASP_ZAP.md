# Práctica 5 - DAST con OWASP ZAP contra VulnerableApp

## Datos generales

- Proyecto evaluado: `VulnerableApp`
- Herramienta DAST: OWASP ZAP mediante Docker
- Fecha de ejecución: 2026-07-23
- Alcance autorizado: únicamente `http://vulnerable_app:8080` dentro de la red Docker interna
- Entorno: Docker aislado, sin publicación de puertos de la aplicación vulnerable al host

## 1. Objetivo

Ejecutar pruebas DAST con OWASP ZAP contra `VulnerableApp`, documentar hallazgos con CWE y CVSS estimado, validar remediaciones y dejar una integración básica de ZAP en GitHub Actions.

## 2. Preparación del entorno aislado

Se creó un entorno separado en `pentest-compose.yml` con tres servicios:

| Servicio | Propósito |
|---|---|
| `vulnerable_sqlserver` | Base de datos SQL Server interna para que la app funcione en contenedor. |
| `vulnerable_app` | Aplicación ASP.NET Core 10 escaneada por ZAP. |
| `zap` | Contenedor OWASP ZAP usado para ejecutar baseline y full scan. |

La red configurada fue `practicas_pentest-net` con `internal: true`, lo que evita exponer el entorno vulnerable a Internet.

Evidencias:

- `VulnerableApp/evidencias/P5-DAST-ZAP/01-docker-compose-ps.txt`
- `VulnerableApp/evidencias/P5-DAST-ZAP/02-network-inspect.json`
- `VulnerableApp/evidencias/P5-DAST-ZAP/03-zap-to-app-status.txt`
- `VulnerableApp/evidencias/P5-DAST-ZAP/04-docker-compose-ps-final.txt`
- `VulnerableApp/evidencias/P5-DAST-ZAP/05-network-inspect-final.json`

## 3. Escaneo baseline inicial

Comando ejecutado dentro del contenedor ZAP:

```powershell
docker exec zap zap-baseline.py -t http://vulnerable_app:8080 -r baseline-report.html -x baseline-report.xml -J baseline-report.json -l WARN -I
```

Reportes generados:

- `VulnerableApp/evidencias/P5-DAST-ZAP/zap-reports/baseline-report.html`
- `VulnerableApp/evidencias/P5-DAST-ZAP/zap-reports/baseline-report.xml`
- `VulnerableApp/evidencias/P5-DAST-ZAP/zap-reports/baseline-report.json`

Resultado inicial:

| Riesgo | Cantidad |
|---|---:|
| Medium | 2 |
| Low | 1 |
| High | 0 |

## 4. Full scan activo

Comando ejecutado:

```powershell
docker exec zap zap-full-scan.py -t http://vulnerable_app:8080 -r fullscan-report.html -x fullscan-report.xml -J fullscan-report.json -m 5 -I
```

Reportes generados:

- `VulnerableApp/evidencias/P5-DAST-ZAP/zap-reports/fullscan-report.html`
- `VulnerableApp/evidencias/P5-DAST-ZAP/zap-reports/fullscan-report.xml`
- `VulnerableApp/evidencias/P5-DAST-ZAP/zap-reports/fullscan-report.json`

Resultado del full scan:

| Regla | Resultado |
|---|---|
| SQL Injection | PASS |
| Reflected XSS | PASS |
| Persistent XSS | PASS |
| Path Traversal | PASS |
| Remote OS Command Injection | PASS |
| Missing Security Headers | WARN |

Nota de evaluación: la rúbrica menciona documentar al menos tres hallazgos High. En esta ejecución real, ZAP no reportó hallazgos High porque la aplicación ya contenía correcciones previas de P2 contra SQL Injection, XSS, autenticación e IDOR. Por integridad técnica, se documentan todos los hallazgos reales detectados por ZAP.

## 5. Tabla de hallazgos

| # | Nombre del hallazgo | Riesgo ZAP | CWE | CVSS estimado | URL afectada | Evidencia | Remediación propuesta |
|---:|---|---|---:|---:|---|---|---|
| 1 | Content Security Policy (CSP) Header Not Set | Medium | CWE-693 | 4.3 | `http://vulnerable_app:8080` | `fullscan-report.xml`, método `GET`, respuesta `200 OK` sin CSP | Agregar cabecera `Content-Security-Policy` restrictiva. |
| 2 | Missing Anti-clickjacking Header | Medium | CWE-1021 | 4.3 | `http://vulnerable_app:8080` | `fullscan-report.xml`, parámetro `x-frame-options` ausente | Agregar `X-Frame-Options: DENY` o usar `frame-ancestors` en CSP. |
| 3 | X-Content-Type-Options Header Missing | Low | CWE-693 | 3.1 | `http://vulnerable_app:8080` | `fullscan-report.xml`, parámetro `x-content-type-options` ausente | Agregar `X-Content-Type-Options: nosniff`. |

Los CVSS son estimaciones académicas basadas en impacto de configuración insegura y explotación dependiente de navegador/usuario.

## 6. Análisis detallado del hallazgo principal

### Hallazgo seleccionado

`Content Security Policy (CSP) Header Not Set`

### Clasificación OWASP Top 10

OWASP Top 10 2021: A05 - Security Misconfiguration.

### Request HTTP usado por ZAP

El reporte XML del full scan registró la instancia:

```http
GET / HTTP/1.1
Host: vulnerable_app:8080
```

La URL afectada fue:

```text
http://vulnerable_app:8080
```

### Response HTTP que reveló la vulnerabilidad

Antes de la remediación, ZAP identificó una respuesta `200 OK` sin cabecera `Content-Security-Policy`.

La evidencia se encuentra en:

```text
VulnerableApp/evidencias/P5-DAST-ZAP/zap-reports/fullscan-report.xml
```

La instancia del hallazgo contiene:

```xml
<alert>Content Security Policy (CSP) Header Not Set</alert>
<riskdesc>Medium (High)</riskdesc>
<uri>http://vulnerable_app:8080</uri>
<method>GET</method>
<cweid>693</cweid>
```

### Por qué confirma la vulnerabilidad

La ausencia de CSP permite que el navegador no tenga una política explícita para restringir orígenes de scripts, estilos, imágenes, formularios o framing. Aunque CSP no sustituye el saneamiento de entradas, sí reduce el impacto de ataques como XSS, inyección de contenido o carga de recursos no autorizados.

### Impacto potencial

Un atacante podría aprovechar una vulnerabilidad XSS futura o una mala carga de recursos para ejecutar scripts no deseados, robar información visible en la sesión, modificar la interfaz o realizar acciones en nombre del usuario.

### Código/configuración correcta aplicada

Se agregó un middleware de cabeceras HTTP en `VulnerableApp/Program.cs`:

```csharp
headers["Content-Security-Policy"] =
    "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
headers["X-Frame-Options"] = "DENY";
headers["X-Content-Type-Options"] = "nosniff";
```

Evidencia post-fix:

- `VulnerableApp/evidencias/P5-DAST-ZAP/zap-reports/response-headers-after-fix.txt`
- `VulnerableApp/evidencias/P5-DAST-ZAP/07-http-response-after-fix.txt`

Headers observados después de la remediación:

```http
Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'
X-Frame-Options: DENY
X-Content-Type-Options: nosniff
```

## 7. Verificación post-fix

Después de aplicar las cabeceras de seguridad se reconstruyó el contenedor y se ejecutó el re-escaneo:

```powershell
docker compose -f pentest-compose.yml up -d --build --force-recreate vulnerable_app zap
docker exec zap zap-baseline.py -t http://vulnerable_app:8080 -r baseline-after-fix.html -x baseline-after-fix.xml -J baseline-after-fix.json -l WARN -I
```

Resultado post-fix:

```text
FAIL-NEW: 0
WARN-NEW: 0
INFO: 0
PASS: 60
```

Comparativa:

| Hallazgo | Baseline inicial | Full scan inicial | Baseline post-fix | Estado |
|---|---:|---:|---:|---|
| Content Security Policy Header Not Set | 1 | 1 | 0 | Eliminado |
| Missing Anti-clickjacking Header | 1 | 1 | 0 | Eliminado |
| X-Content-Type-Options Header Missing | 1 | 1 | 0 | Eliminado |

Reportes post-fix:

- `VulnerableApp/evidencias/P5-DAST-ZAP/zap-reports/baseline-after-fix.html`
- `VulnerableApp/evidencias/P5-DAST-ZAP/zap-reports/baseline-after-fix.xml`
- `VulnerableApp/evidencias/P5-DAST-ZAP/zap-reports/baseline-after-fix.json`

## 8. Escaneo de API REST

Se verificó el endpoint sugerido por la práctica:

```powershell
docker exec zap curl -s -o /dev/null -w '%{http_code}' http://vulnerable_app:8080/swagger/v1/swagger.json
```

Resultado:

```text
404
```

Conclusión: el escaneo `zap-api-scan.py` no aplica en esta práctica porque `VulnerableApp` no expone una especificación Swagger/OpenAPI.

Evidencia:

- `VulnerableApp/evidencias/P5-DAST-ZAP/06-openapi-status.txt`

## 9. GitHub Actions DAST

Se agregó el workflow:

```text
.github/workflows/security.yml
```

Incluye:

- Job `build-test`.
- Job `dast-zap`.
- Configuración de .NET 10.
- SQL Server como servicio de CI.
- Ejecución de `VulnerableApp`.
- ZAP Baseline Scan con `zaproxy/action-baseline@v0.12.0`.
- Carga de artefactos ZAP.

También se agregó:

```text
.zap/rules.tsv
```

Este archivo clasifica hallazgos conocidos como advertencias documentadas para evitar bloqueos falsos en CI mientras se mantiene visibilidad de seguridad.

## 10. Evidencias visuales

Capturas generadas desde los reportes HTML:

- `VulnerableApp/evidencias/P5-DAST-ZAP/08-baseline-report.png`
- `VulnerableApp/evidencias/P5-DAST-ZAP/09-fullscan-report.png`
- `VulnerableApp/evidencias/P5-DAST-ZAP/10-baseline-after-fix.png`

## 11. Validaciones técnicas

Pruebas unitarias:

```powershell
dotnet test VulnerableApp.Tests\VulnerableApp.Tests.csproj --artifacts-path .artifacts --no-restore --nologo --verbosity minimal
```

Resultado:

```text
Correctas: 17
Con error: 0
Omitido: 0
```

Validación de Docker Compose:

```powershell
docker compose -f pentest-compose.yml config --quiet
```

Resultado: correcto, sin errores.

Validación de reportes:

| Reporte | Alertas |
|---|---:|
| `baseline-report.json` | 3 |
| `fullscan-report.json` | 3 |
| `baseline-after-fix.json` | 0 |

## 12. Cierre ético

Al finalizar la práctica se destruyó el entorno vulnerable con:

```powershell
docker compose -f pentest-compose.yml down --volumes --remove-orphans
```

Evidencias:

- `VulnerableApp/evidencias/P5-DAST-ZAP/11-docker-compose-down.txt`
- `VulnerableApp/evidencias/P5-DAST-ZAP/12-docker-ps-after-down.txt`
- `VulnerableApp/evidencias/P5-DAST-ZAP/13-network-after-down.txt`

La verificación final mostró que no quedaron contenedores `vulnerable_app`, `vulnerable_sqlserver` ni `zap`, y que la red `practicas_pentest-net` fue eliminada.

## 13. Conclusiones

La práctica DAST quedó completada con un entorno Docker aislado, escaneo baseline, full scan activo, análisis de hallazgos, remediación, re-escaneo y cierre ético. Los hallazgos reales correspondieron a cabeceras HTTP defensivas ausentes; tras agregar CSP, anti-clickjacking y `nosniff`, el re-escaneo post-fix quedó sin alertas.

ZAP confirmó además que las clases de vulnerabilidad más críticas de prácticas anteriores, como SQL Injection y XSS, no se reprodujeron en el full scan actual. Esto respalda que las correcciones de P2 continúan funcionando y que el pipeline DAST puede usarse como control automatizado en CI.
