# 🏗️ **NominaDownloader-PEI-GTO - Descarga Automatizada de Nóminas**

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![Version](https://img.shields.io/badge/Version-1.0-blue.svg)]()
[![PEI-GTO](https://img.shields.io/badge/Portal-PEI%20Guanajuato-green.svg)]()
[![Selenium](https://img.shields.io/badge/Selenium-4.24.0-brightgreen.svg)](https://selenium.dev/)
[![Tests](https://img.shields.io/badge/Tests-267%## 👨‍💻 **Autor**

**Javier Ismael Díaz González** - *Desarrollador Principal*
- 🐙 GitHub: [@Jigdis](https://github.com/Jigdis)
- 📅 Creado: Agosto 2025
- 🔄 Última actualización: Agosto 6, 2025
- 🏗️ Arquitectura: Clean Architecture + CQRS
- ⚡ Tecnologías: .NET 9.0, Selenium 4.24.0, WolverineFx
- 🧪 Estado: 267 tests unitarios - 100% passingng-success.svg)](https://github.com/Jigdis/nomina-downloader-pei-gto)
[![GitHub](https://img.shields.io/badge/GitHub-nomina--downloader--pei--gto-blue?logo=github)](https://github.com/Jigdis/nomina-downloader-pei-gto)

## 📋 **Descripción**

Descargador automatizado para descarga masiva de recibos de nómina del **Portal Estatal de Información del Estado de Guanajuato** ([https://pei.guanajuato.gob.mx/](https://pei.guanajuato.gob.mx/)), desarrollado con **Clean Architecture** y **alta concurrencia**.

### 🎯 **Problemática Resuelta**

El portal oficial **no ofrece funcionalidades** para:
- ❌ Descarga masiva de recibos por período completo
- ❌ Descarga masiva de recibos por año completo  
- ❌ Exportación de archivos en lote
- ❌ Automatización de procesos de descarga

### 💡 **Solución Implementada**

NominaDownloader-PEI-GTO automatiza completamente el proceso manual de descarga con arquitectura empresarial:

## 📊 **Características Principales**

### 🚀 **Rendimiento y Escalabilidad**
- **16 navegadores paralelos** para máximo rendimiento
- **Ejecución concurrente** de descargas independientes
- **Optimización de memoria** con gestión inteligente de recursos
- **Soporte período 0 (Complementaría)** para casos especiales

### 🏗️ **Arquitectura Empresarial**
- **Clean Architecture** con separación clara de responsabilidades
- **CQRS + Mediator** con WolverineFx para comandos y queries
- **Dependency Injection** para máxima testabilidad
- **Interfaces desacopladas** para facilitar mantenimiento

### 🛡️ **Robustez y Recuperación**
- **Sistema de snapshots** para reanudar descargas interrumpidas
- **Recuperación automática** de errores con reintentos inteligentes
- **Detección robusta de iframes** con 11 estrategias de fallback
- **Scroll inteligente** para cargar todos los períodos disponibles

### ✅ **Validación y Calidad**
- **Validación específica de archivos** (2 PDFs + 1 XML con patrones)
- **267 tests unitarios** ejecutándose en < 6 segundos
- **Cobertura completa** de casos de uso y escenarios de error
- **Mocks optimizados** para tests rápidos y confiables
- **Soporte completo período 0** validado en tests

### 📊 **Monitoreo y UX**
- **Progreso en tiempo real** con logging estructurado (Serilog)
- **Modo enhanced** con interface interactiva avanzada
- **Reportes detallados** de descargas y errores
- **Logs persistentes** para auditoría y debugging
- **Soporte período 0 (Complementaría)** integrado

## 🚀 **Inicio Rápido**

### Prerrequisitos
- ✅ **.NET 9.0** instalado
- ✅ **Google Chrome** actualizado
- ✅ **ChromeDriver** compatible (se descarga automáticamente)

### Instalación y Ejecución

```bash
# Clonar el repositorio
git clone https://github.com/Jigdis/nomina-downloader-pei-gto.git
cd nomina-downloader-pei-gto

# Restaurar dependencias
dotnet restore

# Compilar la solución
dotnet build

# Ejecutar la aplicación
dotnet run --project src/NominaDownloaderPEIGTO.Console
```

### Primera Ejecución

1. **Ejecutar la aplicación** en modo enhanced
2. **Ingresar credenciales** del portal PEI Guanajuato
3. **Seleccionar períodos** a descargar:
   - Todos los períodos disponibles
   - Períodos de un año específico
   - Período específico
4. **Especificar directorio** de descarga
5. **Iniciar descarga** automática

## 🧪 **Testing**

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar tests con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Tests específicos por categoría
dotnet test --filter "Category=Unit"

# Verificar tests antes de Pull Request
dotnet test --verbosity normal
```

**Estadísticas de Tests:**
- 📊 **267 tests** ejecutándose en **~6 segundos**
- 🎯 **100% passing** rate
- ⚡ **97% mejora** en velocidad vs. tests de integración
- 🧪 **Tests unitarios con mocks** para máxima velocidad

### 🔍 **Validación Pre-Pull Request**
Antes de enviar un Pull Request, ejecutar:
```bash
# 1. Compilar sin errores
dotnet build

# 2. Ejecutar todos los tests
dotnet test --verbosity minimal

# 3. Verificar que todos los tests pasen
# Resultado esperado: "No test failures were found"
```

## 🏗️ **Arquitectura del Sistema**

```
src/
├── NominaDownloaderPEIGTO.Domain/          # Entidades, Value Objects, Enums
├── NominaDownloaderPEIGTO.Application/     # Casos de uso, Comandos, Queries
├── NominaDownloaderPEIGTO.Infrastructure/  # Servicios externos, Repositories
├── NominaDownloaderPEIGTO.Common/          # Utilidades compartidas
└── NominaDownloaderPEIGTO.Console/         # UI, DI, Configuración

tests/
└── NominaDownloaderPEIGTO.Tests/          # Tests unitarios optimizados
```

### Componentes Principales

- **🌐 WebPortalService**: Automatización del navegador con Selenium
- **📁 FileValidationService**: Validación específica de archivos descargados
- **🔄 ErrorRecoveryService**: Sistema de recuperación automática
- **📸 SnapshotService**: Persistencia de estado y reanudación
- **📊 ProgressService**: Reportes en tiempo real
- **🔧 PathUtils**: Utilidades para sanitización de nombres de carpetas

## ⚙️ **Configuración Avanzada**

### Configuración de Concurrencia
```csharp
var config = new DownloadConfig(
    downloadPath: @"C:\Downloads", 
    maxParallelBrowsers: 16  // Ajustar según hardware
);
```

### Configuración de Logging
```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/app-.log" } }
    ]
  }
}
```


## 📈 **Optimizaciones Implementadas**

### 🧹 **Limpieza del Proyecto (Agosto 2025)**
- ✅ **Eliminación de archivos obsoletos**: Removidos proyectos ReciboDownloader.* no utilizados
- ✅ **Handlers simplificados**: Eliminados DownloadFileHandler y GetSessionStatusHandler redundantes
- ✅ **Estructura optimizada**: Solo archivos esenciales para funcionalidad core
- ✅ **Tests validados**: 267 tests unitarios completamente alineados con implementaciones

### 🔧 **Refactoring del Proyecto Common (Agosto 2025)**
- ✅ **Creación del proyecto Common**: Nuevo proyecto `NominaDownloaderPEIGTO.Common` para funcionalidades compartidas
- ✅ **Referencias actualizadas**: Infrastructure y Tests ahora usan la funcionalidad común
- ✅ **Mantenibilidad mejorada**: Un solo lugar para mantener lógica de sanitización de carpetas
- ✅ **Tests validados**: 267 tests ejecutándose exitosamente tras el refactoring

### Rendimiento de Tests
- ✅ **Eliminación de delays**: Removidos `Thread.Sleep()` innecesarios
- ✅ **Mocks optimizados**: Tests unitarios vs. integración pesada
- ✅ **Paralelización xUnit**: Ejecución concurrente de tests
- ✅ **Configuración optimizada**: Compilación y análisis deshabilitados
- ✅ **Soporte período 0**: Tests específicos para período "Complementaría"

### Rendimiento de Aplicación
- ✅ **16 navegadores paralelos**: Máxima concurrencia
- ✅ **Gestión inteligente de memoria**: Liberación automática de recursos
- ✅ **Scroll optimizado**: Carga eficiente de períodos dinámicos
- ✅ **Detección robusta**: 11 estrategias de fallback para iframes

## �️ **Desarrollo y Contribución**

### Estructura de Branches
- `main`: Versión estable de producción
- `develop`: Desarrollo activo
- `feature/*`: Nuevas características (ej: `feature/upload-project`)
- `bugfix/*`: Correcciones de errores

### Comandos de Desarrollo
```bash
# Limpiar y recompilar
dotnet clean && dotnet build

# Ejecutar tests en watch mode
dotnet test --watch

# Formato de código
dotnet format

# Análisis de código
dotnet run --verbosity diagnostic
```

## 📝 **Licencia**

Este proyecto está licenciado bajo la [MIT License](LICENSE).

## 🤝 **Soporte**

Para reportar problemas o solicitar características:
- 📧 Crear un issue en el repositorio
- 📖 Consultar los logs en `logs/app-YYYYMMDD.log`
- 🔍 Verificar compatibilidad de ChromeDriver

---

**Desarrollado con ❤️ para automatizar tareas repetitivas y mejorar la productividad.**
- **Optimización**: Evita descargas duplicadas y detecta fin de contenido

### 🔍 **Detección Robusta de Elementos**
- **11 selectores de iframe**: Máxima compatibilidad con cambios del portal
- **Estrategias múltiples**: Fallback automático entre métodos de detección
- **Manejo resiliente**: Continúa funcionando ante cambios menores del sitio

### ✅ **Validación Específica de Archivos**
- **Patrones obligatorios**:
  - 1 XML: `CFDI_xml_*.xml`
  - 1 PDF Recibo: `Recibo_*.PDF`
  - 1 PDF CFDI: `CFDI_pdf_*.pdf`
- **Validación pre/post descarga**: Evita descargas innecesarias
- **Limpieza automática**: Elimina carpetas con archivos incorrectos

## 🏗️ **Arquitectura**

```
┌─────────────────────────────────────────┐
│           Console Application           │
│    (Presentation & Composition Root)    │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│         Application Layer               │
│   Commands, Queries, Handlers,         │
│   Interfaces, CQRS Logic               │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│        Infrastructure Layer            │
│   Selenium, File System, Repositories, │
│   External Services Implementation     │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│            Domain Layer                 │
│   Entities, Value Objects, Enums,      │
│   Domain Logic, Business Rules         │
└─────────────────────────────────────────┘
```

### 📁 **Estructura de Proyectos**

- **🔷 Domain** - Lógica de negocio pura y reglas del dominio
- **🔶 Application** - Casos de uso y orquestación (CQRS)
- **🔸 Infrastructure** - Servicios externos (Selenium, archivos)
- **🔹 Console** - Interfaz de usuario y composición
- **🔧 Common** - Utilidades compartidas y funcionalidades comunes
- **🧪 Tests** - Pruebas unitarias y de integración

### 📂 **Estructura del Proyecto**
```
NominaDownloader-PEI-GTO/
├── README.md                                    📄 Documentación completa
├── NominaDownloaderPEIGTO.sln                  🔧 Solución principal
├── Directory.Packages.props                    📦 Gestión centralizada de paquetes
├── .gitignore                                  🔒 Control de archivos excluidos
├── src/
│   ├── NominaDownloaderPEIGTO.Domain/          🔷 Entidades y lógica de negocio
│   ├── NominaDownloaderPEIGTO.Application/     🔶 Casos de uso (CQRS)
│   ├── NominaDownloaderPEIGTO.Infrastructure/  🔸 Selenium y servicios externos
│   ├── NominaDownloaderPEIGTO.Common/          🔧 Utilidades compartidas
│   └── NominaDownloaderPEIGTO.Console/         🔹 Aplicación de consola
│       └── logs/                               📊 Logs de la aplicación
└── tests/
    └── NominaDownloaderPEIGTO.Tests/           🧪 Pruebas unitarias
```

## 🚀 **Inicio Rápido**

### **Prerrequisitos**
- .NET 9.0 SDK
- Google Chrome (para Selenium)

### **Instalación**
```bash
# Clonar repositorio
git clone https://github.com/Jigdis/nomina-downloader-pei-gto.git
cd nomina-downloader-pei-gto

# Restaurar dependencias
dotnet restore

# Compilar solución
dotnet build

# Ejecutar aplicación
dotnet run --project src/NominaDownloaderPEIGTO.Console
```

### **Uso**

#### **Modo Estándar**
```bash
# Ejecutar con configuración básica
dotnet run --project src/NominaDownloaderPEIGTO.Console
```

#### **🔧 Modo Avanzado (Recomendado)**
```bash
# Ejecutar con funciones avanzadas (snapshots, recuperación automática)
dotnet run --project src/NominaDownloaderPEIGTO.Console
```

#### **Otras Opciones**
```bash
# Ejecutar tests unitarios
dotnet test

# Publicar para producción
dotnet publish src/NominaDownloaderPEIGTO.Console -c Release -o publish
```

### **🔧 Modo Enhanced - Funciones Avanzadas**

El modo enhanced incluye:
- ✅ **Análisis de snapshots**: Detecta carpetas vacías automáticamente
- 🔄 **Recuperación de errores**: Reintenta descargas fallidas hasta 3 veces
- 📊 **Interface interactiva**: Menús de selección y confirmaciones
- 📸 **Gestión de snapshots**: Crea y restaura estados de descarga
- 🛡️ **Validación avanzada**: Verifica integridad de archivos antes/después de descarga

## ⚙️ **Configuración**

### **📁 Archivos Generados**
- **`download_snapshot_YYYYMMDD_HHMMSS.json`**: Estado inicial antes de descarga
- **`error_recovery_session_YYYYMMDD_HHMMSS.json`**: Sesión de recuperación de errores
- **`src/NominaDownloaderPEIGTO.Console/logs/nomina-downloader-YYYYMMDD.log`**: Logs detallados de la aplicación
- **Soporte período 0**: Archivos de período "Complementaría" completamente soportados

### **🔧 Configuración de Descarga**
```csharp
var config = new DownloadConfig(
    downloadPath: @"C:\Recibos",           // Ruta base de descarga
    maxParallelBrowsers: 16,              // Navegadores paralelos
    maxRetryAttempts: 3,                  // Reintentos por error
    timeoutPerDownload: TimeSpan.FromMinutes(5),
    validateDownloads: true,              // Validación de archivos
    preferredFileType: FileType.ReciboPdf
);
```

### **📂 Estructura de Archivos**
```
C:\Recibos\
├── 2024\
│   ├── Periodo_01_ENERO_2024\
│   │   ├── CFDI_xml_*.xml          (1 archivo)
│   │   ├── Recibo_*.PDF            (1 archivo)
│   │   └── CFDI_pdf_*.pdf          (1 archivo)
│   ├── Periodo_02_FEBRERO_2024\
│   │   └── ...
│   └── Periodo_00_COMPLEMENTARIA_2024\     ✨ Soporte período 0
│       ├── CFDI_xml_*.xml
│       ├── Recibo_*.PDF
│       └── CFDI_pdf_*.pdf
└── 2025\
    └── ...
```

## 📊 **Características Técnicas**

### **🔄 Paralelización**
- **SemaphoreSlim** para control de concurrencia
- **16 navegadores Chrome** ejecutando simultáneamente
- **Task.WhenAll** para coordinación de descargas
- **CancellationToken** para cancelación cooperativa

### **🛡️ Robustez y Recuperación**
- **Snapshots automáticos**: Estado inicial antes de cada descarga
- **Recuperación de errores**: Análisis y reintento automático de fallos
- **Validación específica**: Verifica patrones exactos de archivos (CFDI_xml_, Recibo_, CFDI_pdf_)
- **Cleanup inteligente**: Elimina carpetas con archivos incorrectos o incompletos
- **Scroll dinámico**: Carga todos los períodos disponibles en tabla lazy-loading

### **📈 Monitoreo y Logging**
- **Progreso en tiempo real** con emojis y colores
- **Logging estructurado** con Serilog (archivo + consola)
- **Métricas detalladas** (duración, archivos procesados, errores)
- **Trazabilidad completa** de snapshots y recuperaciones

## 🔄 **Flujo de Trabajo Enhanced**

### **1. Análisis Inicial**
```
📸 Crear snapshot inicial
🔍 Detectar carpetas vacías
📊 Mostrar resumen de estado
```

### **2. Proceso de Descarga**
```
🚀 Login automático al portal
📜 Scroll para cargar todos los períodos
🔍 Detección robusta de elementos (11 estrategias)
✅ Validación pre-descarga (evita duplicados)
📥 Descarga con múltiples fallbacks
✅ Validación post-descarga (2 PDFs + 1 XML)
```

### **3. Recuperación de Errores**
```
📋 Análisis de carpetas problemáticas
🗑️ Limpieza de archivos incorrectos
🔄 Reintento automático (hasta 3 veces)
📊 Reporte final de resultados
```

## 🧪 **Testing**

```bash
# Ejecutar todos los tests
dotnet test

# Con cobertura de código
dotnet test --collect:"XPlat Code Coverage"
```

### **Estructura de Tests**
- **Unit Tests** - Lógica de dominio y aplicación
- **Integration Tests** - Servicios de infraestructura  
- **E2E Tests** - Flujos completos de usuario

## 📝 **Notas Importantes**

### **🔒 Seguridad**
- Las credenciales se solicitan en tiempo de ejecución
- No se almacenan credenciales en archivos
- Sesiones se limpian automáticamente al finalizar

### **🎯 Compatibilidad**
- Portal del Estado de Guanajuato (Recibos de Nómina)
- Chrome headless para máximo rendimiento
- .NET 9.0 multiplataforma (Windows, Linux, macOS)

### **⚡ Rendimiento**
- 16 navegadores paralelos por defecto
- Validación inteligente evita descargas duplicadas
- Scroll optimizado para cargar todos los períodos
- Fallbacks automáticos mantienen velocidad ante errores

---

**NominaDownloader-PEI-GTO** - Descargador robusto y escalable para automatización de recibos de nómina con recuperación inteligente de errores y validación específica de archivos.

## �‍💻 **Autor**

**Javier Ismael Díaz González** - *Desarrollador Principal*
- 🐙 GitHub: [@Jigdis](https://github.com/Jigdis)
- 📅 Creado: Agosto 2025
- 🏗️ Arquitectura: Clean Architecture + CQRS
- ⚡ Tecnologías: .NET 9.0, Selenium 4.24.0, WolverineFx

## ⚠️ **Descargo de Responsabilidad**

### 🎓 **Propósito Educativo**
Este proyecto fue creado con **fines educativos** para aprender técnicas de web scraping y automatización. El autor no se hace responsable del uso que terceros puedan darle a esta aplicación.

### 🔒 **Privacidad y Seguridad**
- ✅ **No almacena credenciales**: RFC y contraseñas solo se utilizan temporalmente para autenticación
- ✅ **Sin persistencia de datos**: Las credenciales no se guardan en archivos ni bases de datos
- ✅ **Sesión temporal**: Los datos de login se eliminan automáticamente al finalizar
- ✅ **Uso local**: La aplicación funciona completamente en el equipo del usuario

### ⚖️ **Responsabilidad de Uso**
- El usuario es responsable del cumplimiento de términos de servicio del portal oficial
- Se recomienda usar la aplicación de manera responsable y ética
- El autor no se hace responsable de posibles infracciones o mal uso por parte de terceros

## 🤝 **Contribuir**

¡Las contribuciones son bienvenidas! Para contribuir:

1. Fork el proyecto
2. Crea tu feature branch (`git checkout -b feature/upload-project`)
3. Commit tus cambios (`git commit -m 'Agregar nueva característica'`)
4. Push al branch (`git push origin feature/upload-project`)
5. Abre un Pull Request

### 🧪 **Requisitos de Testing para Pull Requests**

**OBLIGATORIO**: Toda nueva funcionalidad debe incluir pruebas unitarias antes de ser aprobada.

#### ✅ **Checklist de Testing**
- [ ] **Tests unitarios creados** para nuevas clases/métodos
- [ ] **Tests de casos límite** (valores nulos, vacíos, límites)
- [ ] **Tests de manejo de errores** (excepciones esperadas)
- [ ] **Tests de integración** si se modifica infraestructura
- [ ] **Ejecución exitosa**: `dotnet test` debe mostrar 100% passing
- [ ] **Nomenclatura consistente**: Seguir patrón `MethodName_Scenario_ExpectedResult`

#### 📊 **Estructura de Tests**
```csharp
[Test]
public void MethodName_ValidInput_ReturnsExpectedResult()
{
    // Arrange
    var service = new ServiceUnderTest();
    
    // Act
    var result = service.Method(validInput);
    
    // Assert
    result.Should().Be(expectedValue);
}
```

#### ⚠️ **Pull Request será rechazado si:**
- Falta cobertura de tests para nuevas implementaciones
- Los tests existentes fallan después de los cambios
- No se incluyen tests para casos de error críticos
- La implementación no sigue Clean Architecture

### 📋 **Pautas de Contribución**
- Seguir los principios de Clean Architecture
- **Incluir tests unitarios** para todas las nuevas funcionalidades
- **Cobertura de tests obligatoria**: Cada nueva implementación debe tener sus respectivas pruebas
- **Tests deben pasar**: Verificar que `dotnet test` ejecute exitosamente (267+ tests)
- **Validar implementaciones**: Los tests deben cubrir casos de uso, casos límite y manejo de errores
- Documentar cambios en el README
- Usar commits descriptivos
- **Pull Request no será aprobado** sin las pruebas unitarias correspondientes

## 📄 **Licencia**

Este proyecto está bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para más detalles.

### ⚖️ **Uso Comercial**
- ✅ Uso comercial permitido
- ✅ Modificación permitida
- ✅ Distribución permitida
- ⚠️ Se requiere incluir la licencia original

## �🙏 **Agradecimientos**

- Clean Architecture by Robert C. Martin
- Selenium WebDriver Team
- .NET Community
- WolverineFx Framework

---

✨ **Sistema empresarial con Clean Architecture completamente optimizado** ✨
