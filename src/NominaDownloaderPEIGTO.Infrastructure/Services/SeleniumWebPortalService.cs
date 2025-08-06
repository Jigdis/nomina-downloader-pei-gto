using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using NominaDownloaderPEIGTO.Application.Interfaces;
using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.Exceptions;
using NominaDownloaderPEIGTO.Domain.ValueObjects;
using System.Security.Cryptography;
using System.Text;

namespace NominaDownloaderPEIGTO.Infrastructure.Services
{
    /// <summary>
    /// Implementación del servicio de portal web usando Selenium
    /// </summary>
    public class SeleniumWebPortalService : IWebPortalService, IDisposable
    {
        private readonly ChromeDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly DownloadConfig _config;
        private readonly string _tempDownloadPath;
        private bool _isLoggedIn = false;

        public SeleniumWebPortalService(DownloadConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tempDownloadPath = Path.Combine(Path.GetTempPath(), "NominaDownloaderPEIGTO");
            Directory.CreateDirectory(_tempDownloadPath);
            
            var chromeOptions = CreateChromeOptions(config);
            
            // Configurar ChromeDriverService para suprimir logs
            var service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;
            
            _driver = new ChromeDriver(service, chromeOptions);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(15);
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(5);
            
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        }

        public async Task<bool> LoginAsync(LoginCredentials credentials, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("🌐 Navegando al portal de login...");
                _driver.Navigate().GoToUrl("https://pei.guanajuato.gob.mx/recibo/login.aspx");
                await Task.Delay(2000, cancellationToken); // Esperar más tiempo para cargar

                Console.WriteLine("🔍 Buscando elementos de login...");
                bool loggedIn = TryLoginInIframes(credentials) || TryLoginInMainPage(credentials);

                if (loggedIn)
                {
                    Console.WriteLine("✅ Login exitoso, esperando confirmación...");
                    WaitForLoginConfirmation();
                    NavigateToWelcomePage();
                    _isLoggedIn = true;
                    Console.WriteLine("🎉 Sesión iniciada correctamente");
                }
                else
                {
                    Console.WriteLine("❌ No se pudieron encontrar los elementos de login o falló la autenticación");
                    Console.WriteLine($"📍 URL actual: {_driver.Url}");
                    Console.WriteLine($"📄 Título de página: {_driver.Title}");
                }

                return loggedIn;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error durante el login: {ex.Message}");
                throw new LoginException("Error durante el proceso de login", ex);
            }
        }

        public async Task<IEnumerable<int>> GetAvailableYearsAsync(CancellationToken cancellationToken = default)
        {
            if (!_isLoggedIn)
                throw new InvalidOperationException("Debe estar logueado para obtener años disponibles");

            try
            {
                Console.WriteLine("📅 Obteniendo años disponibles...");
                
                // Navegar a la página de consulta de recibos
                _driver.Navigate().GoToUrl("https://pei.guanajuato.gob.mx/recibo/consultarecibo.aspx");
                await Task.Delay(1000, cancellationToken);

                // Obtener las opciones del dropdown de años (vEJERCICIO)
                var exerciseSelect = _driver.FindElement(By.Id("vEJERCICIO"));
                var yearOptions = exerciseSelect.FindElements(By.TagName("option"));
                
                var availableYears = new List<int>();
                
                foreach (var option in yearOptions)
                {
                    var yearText = option.Text.Trim();
                    if (int.TryParse(yearText, out int year))
                    {
                        availableYears.Add(year);
                        Console.WriteLine($"   ✅ Año disponible: {year}");
                    }
                }

                Console.WriteLine($"📅 Total años encontrados: {availableYears.Count}");
                return availableYears.OrderByDescending(y => y);
            }
            catch (Exception ex)
            {
                throw new NavigationException("Error al obtener años disponibles", ex);
            }
        }

        public async Task<IEnumerable<PeriodInfo>> GetAvailablePeriodsAsync(int year, CancellationToken cancellationToken = default)
        {
            if (!_isLoggedIn)
                throw new InvalidOperationException("Debe estar logueado para obtener períodos");

            try
            {
                var periods = new List<PeriodInfo>();

                // Navegar a la página de consulta de recibos
                Console.WriteLine($"📋 Obteniendo períodos para el año {year}...");
                _driver.Navigate().GoToUrl("https://pei.guanajuato.gob.mx/recibo/consultarecibo.aspx");
                await Task.Delay(1000, cancellationToken);

                // Seleccionar el año específico y hacer click en Consultar
                try
                {
                    var exerciseSelect = _driver.FindElement(By.Id("vEJERCICIO"));
                    var selectElement = new SelectElement(exerciseSelect);
                    
                    // Seleccionar el año específico pasado como parámetro
                    selectElement.SelectByValue(year.ToString());
                    Console.WriteLine($"✅ Año {year} seleccionado");
                    
                    // Hacer click en el botón Consultar
                    var consultarButton = _driver.FindElement(By.Id("BTNCONSULTAR"));
                    consultarButton.Click();
                    Console.WriteLine("🔄 Botón Consultar presionado, esperando grid...");
                    
                    // Esperar a que aparezca el grid
                    await Task.Delay(3000, cancellationToken);
                    
                    // Hacer scroll en la tabla para cargar todos los períodos
                    Console.WriteLine("📜 Haciendo scroll para cargar todos los períodos...");
                    await ScrollToLoadAllPeriods(cancellationToken);
                    
                    // Buscar filas en el grid Grid1ContainerTbl
                    var gridRows = _driver.FindElements(By.CssSelector("#Grid1ContainerTbl tbody tr"));
                    Console.WriteLine($"📊 Filas encontradas en el grid después del scroll: {gridRows.Count}");
                    
                    foreach (var row in gridRows)
                    {
                        try
                        {
                            // Extraer período y descripción usando los IDs reales del HTML
                            var periodSpan = row.FindElement(By.CssSelector("span[id*='span_CTLPERIODO_']"));
                            var descriptionSpan = row.FindElement(By.CssSelector("span[id*='span_CTLDESCRIPCION_']"));
                            var statusSpan = row.FindElement(By.CssSelector("span[id*='span_CTLESTATUSDESC_']"));
                            
                            var periodText = periodSpan.Text.Trim();
                            var description = descriptionSpan.Text.Trim();
                            var status = statusSpan.Text.Trim();
                            
                            Console.WriteLine($"   📄 Período: {periodText}, Descripción: {description}, Estatus: {status}");
                            
                            // Solo agregar períodos con estatus "Timbrado"
                            if (status.Equals("Timbrado", StringComparison.OrdinalIgnoreCase) && 
                                int.TryParse(periodText, out int periodNumber))
                            {
                                // Crear PeriodInfo usando el año seleccionado y el número de período con descripción
                                var periodInfo = new PeriodInfo(year, periodNumber, description);
                                periods.Add(periodInfo);
                                Console.WriteLine($"   ✅ Período {periodNumber:D2} ({description}) agregado");
                            }
                            else if (!status.Equals("Timbrado", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine($"   ⚠️ Período {periodText} omitido por estatus: {status}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error procesando fila del grid: {ex.Message}");
                            // Intentar método alternativo si falla el principal
                            try
                            {
                                var cells = row.FindElements(By.TagName("td"));
                                if (cells.Count >= 3)
                                {
                                    var periodText = cells[0].Text.Trim();
                                    var description = cells[1].Text.Trim();
                                    var status = cells[2].Text.Trim();
                                    
                                    Console.WriteLine($"   🔄 Método alternativo - Período: {periodText}, Descripción: {description}, Estatus: {status}");
                                    
                                    if (status.Equals("Timbrado", StringComparison.OrdinalIgnoreCase) && 
                                        int.TryParse(periodText, out int periodNumber))
                                    {
                                        var periodInfo = new PeriodInfo(year, periodNumber, description);
                                        periods.Add(periodInfo);
                                        Console.WriteLine($"   ✅ Período {periodNumber:D2} agregado (método alternativo)");
                                    }
                                }
                            }
                            catch (Exception ex2)
                            {
                                Console.WriteLine($"❌ Error en método alternativo: {ex2.Message}");
                            }
                        }
                    }
                    
                    if (periods.Count == 0)
                    {
                        Console.WriteLine("⚠️ No se encontraron períodos en el grid, intentando debug...");
                        
                        // Debug: mostrar HTML del grid
                        try
                        {
                            var gridContainer = _driver.FindElement(By.Id("Grid1ContainerDiv"));
                            var gridHtml = gridContainer.GetAttribute("innerHTML");
                            Console.WriteLine($"📄 HTML del grid (primeros 500 chars): {gridHtml.Substring(0, Math.Min(500, gridHtml.Length))}...");
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error al consultar períodos para el año {year}: {ex.Message}");
                    
                    // Fallback: agregar períodos estimados para el año solicitado
                    Console.WriteLine($"🔄 Generando períodos estimados para el año {year}");
                    
                    // Para el año solicitado, agregar períodos estimados (quincenales)
                    for (int period = 1; period <= 24; period++)
                    {
                        periods.Add(new PeriodInfo(year, period, $"Período estimado {period:D2}"));
                    }
                    Console.WriteLine($"   ✅ Año {year} agregado con períodos estimados");
                }

                Console.WriteLine($"📅 Total períodos encontrados para {year}: {periods.Count}");
                return periods.OrderByDescending(p => p.Period);
            }
            catch (Exception ex)
            {
                throw new NavigationException($"Error al obtener períodos disponibles para el año {year}", ex);
            }
        }

        public async Task<FileMetadata> DownloadFileAsync(PeriodInfo period, string downloadPath, CancellationToken cancellationToken = default)
        {
            if (!_isLoggedIn)
                throw new InvalidOperationException("Debe estar logueado para descargar archivos");

            try
            {
                // Navegar a la página de consulta de recibos
                Console.WriteLine($"📋 Navegando a consulta de recibos para período {period.DisplayName}...");
                _driver.Navigate().GoToUrl("https://pei.guanajuato.gob.mx/recibo/consultarecibo.aspx");
                await Task.Delay(1000, cancellationToken);

                // Seleccionar el año en el dropdown vEJERCICIO
                var exerciseSelect = _driver.FindElement(By.Id("vEJERCICIO"));
                var selectElement = new SelectElement(exerciseSelect);
                selectElement.SelectByValue(period.Year.ToString());
                Console.WriteLine($"✅ Año {period.Year} seleccionado");
                
                // Hacer click en el botón Consultar
                var consultarButton = _driver.FindElement(By.Id("BTNCONSULTAR"));
                consultarButton.Click();
                Console.WriteLine("🔄 Botón Consultar presionado, esperando grid...");
                
                // Esperar a que aparezca el grid
                await Task.Delay(2000, cancellationToken);
                
                // Hacer scroll en la tabla para cargar todos los períodos
                Console.WriteLine("📜 Haciendo scroll para cargar todos los períodos...");
                await ScrollToLoadAllPeriods(cancellationToken);
                
                // Buscar la fila correspondiente al período en el grid
                var gridRows = _driver.FindElements(By.CssSelector("#Grid1ContainerTbl tbody tr"));
                IWebElement? targetRow = null;
                string periodDescription = "";
                
                foreach (var row in gridRows)
                {
                    try
                    {
                        var periodCell = row.FindElement(By.CssSelector("td:nth-child(1) span"));
                        var descriptionCell = row.FindElement(By.CssSelector("td:nth-child(2) span"));
                        
                        var periodText = periodCell.Text.Trim();
                        var description = descriptionCell.Text.Trim();
                        
                        // Buscar por número de período
                        if (periodText == period.Period.ToString("D2"))
                        {
                            targetRow = row;
                            periodDescription = description;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error procesando fila: {ex.Message}");
                    }
                }
                
                if (targetRow == null)
                {
                    throw new NavigationException($"No se encontró el período {period.Period:D2} en el grid");
                }
                
                Console.WriteLine($"📄 Período encontrado: {periodDescription}");
                
                // Crear la estructura de carpetas
                var folderName = SanitizeFolderName($"Periodo_{period.Period:D2}_{periodDescription}");
                var targetDirectory = Path.Combine("C:\\Recibos", period.Year.ToString(), folderName);
                
                // Verificar si la carpeta ya existe y tiene archivos
                if (Directory.Exists(targetDirectory))
                {
                    var existingFiles = Directory.GetFiles(targetDirectory);
                    Console.WriteLine($"📁 Carpeta existente encontrada: {targetDirectory}");
                    Console.WriteLine($"📄 Archivos actuales: {existingFiles.Length}");
                    
                    // Usar validación detallada para verificar si la carpeta está completa
                    if (ValidatePeriodFolder(targetDirectory, out string validationResult))
                    {
                        Console.WriteLine($"✅ {validationResult}, omitiendo descarga");
                        
                        // Mostrar archivos existentes
                        Console.WriteLine($"📋 Archivos existentes:");
                        foreach (var file in existingFiles)
                        {
                            var info = new FileInfo(file);
                            var fileName = Path.GetFileName(file);
                            Console.WriteLine($"   📄 {fileName} ({info.Length / 1024.0:F1} KB)");
                        }
                        
                        // Retornar metadata del primer archivo encontrado
                        var existingFile = existingFiles.First();
                        var existingFileInfo = new FileInfo(existingFile);
                        var existingFileHash = await CalculateFileHashAsync(existingFile);
                        
                        return new FileMetadata(
                            fileName: existingFileInfo.Name,
                            filePath: existingFile,
                            fileSize: existingFileInfo.Length,
                            fileType: GetFileType(existingFileInfo.Extension),
                            downloadedAt: DateTime.UtcNow,
                            hash: existingFileHash
                        );
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Carpeta no válida: {validationResult}");
                        Console.WriteLine($"📋 Archivos actuales:");
                        foreach (var file in existingFiles)
                        {
                            var info = new FileInfo(file);
                            var fileName = Path.GetFileName(file);
                            Console.WriteLine($"   📄 {fileName} ({info.Length / 1024.0:F1} KB)");
                        }
                        Console.WriteLine($"🗑️ Eliminando carpeta para redescargar...");
                        
                        try
                        {
                            Directory.Delete(targetDirectory, true);
                            Console.WriteLine($"✅ Carpeta eliminada exitosamente");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error eliminando carpeta: {ex.Message}");
                            throw new DownloadException($"No se pudo eliminar la carpeta con archivos incorrectos: {targetDirectory}", ex);
                        }
                    }
                }
                
                Directory.CreateDirectory(targetDirectory);
                Console.WriteLine($"📁 Carpeta creada: {targetDirectory}");
                
                // Hacer click en el botón "Ver" de la fila
                var verButton = targetRow.FindElement(By.CssSelector("td:nth-child(4) span a"));
                verButton.Click();
                Console.WriteLine("🔄 Botón Ver presionado, esperando modal...");
                
                // Esperar a que aparezca el modal/popup con múltiples intentos
                await Task.Delay(3000, cancellationToken);
                
                // Intentar múltiples selectores para el iframe
                IWebElement? iframe = null;
                var iframeSelectors = new[]
                {
                    "gxp0_ifrm",           // ID original
                    "gx_popup_frame",      // ID alternativo común
                    "popup_frame",         // ID genérico
                    "modal_frame",         // ID genérico
                    "gxp0_popup",          // Variación del ID original
                    "iframe[id*='gxp']",   // Cualquier iframe con 'gxp' en el ID
                    "iframe[src*='popup']", // Cualquier iframe con 'popup' en src
                    "iframe[src*='modal']", // Cualquier iframe con 'modal' en src
                    "iframe[id*='popup']", // Cualquier iframe con 'popup' en el ID
                    "iframe[class*='popup']", // Cualquier iframe con 'popup' en la clase
                    "iframe[class*='modal']"  // Cualquier iframe con 'modal' en la clase
                };
                
                Console.WriteLine($"🔍 Buscando iframe con {iframeSelectors.Length} selectores diferentes...");
                
                // Primero intentar encontrar CUALQUIER iframe como fallback
                try
                {
                    var allIframes = _driver.FindElements(By.TagName("iframe"));
                    Console.WriteLine($"📋 Total iframes encontrados en la página: {allIframes.Count}");
                    
                    foreach (var frame in allIframes)
                    {
                        var frameId = frame.GetAttribute("id");
                        var frameSrc = frame.GetAttribute("src");
                        var frameClass = frame.GetAttribute("class");
                        Console.WriteLine($"   🖼️ Iframe: id='{frameId}', src='{frameSrc}', class='{frameClass}'");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error listando iframes: {ex.Message}");
                }
                
                Exception? lastException = null;
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    foreach (var selector in iframeSelectors)
                    {
                        try
                        {
                            // Usar FindElement directamente sin _wait.Until para evitar timeout largo
                            if (selector.Contains("[") || selector.Contains("*"))
                            {
                                // Es un selector CSS
                                iframe = _driver.FindElement(By.CssSelector(selector));
                            }
                            else
                            {
                                // Es un ID
                                iframe = _driver.FindElement(By.Id(selector));
                            }
                            
                            if (iframe != null)
                            {
                                Console.WriteLine($"🖼️ Iframe encontrado con selector: {selector}");
                                break;
                            }
                        }
                        catch (NoSuchElementException ex)
                        {
                            lastException = ex;
                            Console.WriteLine($"⚠️ Intento {attempt + 1}: No se encontró iframe con selector '{selector}'");
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            Console.WriteLine($"⚠️ Intento {attempt + 1}: Error buscando iframe con selector '{selector}': {ex.Message}");
                        }
                    }
                    
                    if (iframe != null) break;
                    
                    // Si no se encontró el iframe, esperar un poco más y reintentar
                    Console.WriteLine($"🔄 Reintentando buscar iframe... (intento {attempt + 1}/3)");
                    await Task.Delay(2000, cancellationToken);
                }
                
                // Si no se encontró con selectores específicos, intentar usar el primer iframe disponible
                if (iframe == null)
                {
                    Console.WriteLine("🔄 No se encontró iframe con selectores específicos, intentando usar cualquier iframe disponible...");
                    try
                    {
                        var allIframes = _driver.FindElements(By.TagName("iframe"));
                        if (allIframes.Count > 0)
                        {
                            iframe = allIframes[0]; // Usar el primer iframe disponible
                            var frameId = iframe.GetAttribute("id");
                            var frameSrc = iframe.GetAttribute("src");
                            Console.WriteLine($"🖼️ Usando primer iframe disponible: id='{frameId}', src='{frameSrc}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error intentando usar iframe genérico: {ex.Message}");
                    }
                }
                
                if (iframe == null)
                {
                    // Si no se encuentra el iframe, intentar buscar enlaces directamente en la página principal
                    Console.WriteLine("⚠️ No se encontró iframe, intentando buscar enlaces en la página principal...");
                    _driver.SwitchTo().DefaultContent(); // Volver al contenido principal
                }
                else
                {
                    _driver.SwitchTo().Frame(iframe);
                    Console.WriteLine("🖼️ Cambiado al iframe del modal");
                }
                
                // Buscar todos los enlaces de descarga
                var downloadLinks = await FindDownloadLinksAsync(iframe != null);
                
                Console.WriteLine($"🔗 Se encontraron {downloadLinks.Count} enlaces de descarga");
                
                if (downloadLinks.Count == 0)
                {
                    // Si no se encontraron enlaces, intentar métodos alternativos
                    Console.WriteLine("⚠️ No se encontraron enlaces de descarga, intentando métodos alternativos...");
                    downloadLinks = await TryAlternativeDownloadMethods();
                }
                
                if (downloadLinks.Count == 0)
                {
                    throw new NavigationException($"No se encontraron enlaces de descarga para el período {period.Period:D2}");
                }
                
                // También buscar enlaces que no tengan href pero tengan onclick
                try
                {
                    var onclickLinks = _driver.FindElements(By.CssSelector("a[onclick]"));
                    foreach (var link in onclickLinks)
                    {
                        var onclick = link.GetAttribute("onclick");
                        if (!string.IsNullOrEmpty(onclick) && 
                            (onclick.Contains("pdf") || onclick.Contains("xml") || onclick.Contains("PDF") || onclick.Contains("XML")))
                        {
                            if (!downloadLinks.Contains(link))
                            {
                                downloadLinks.Add(link);
                                Console.WriteLine($"🔗 Enlace onclick encontrado: {onclick}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error buscando enlaces onclick: {ex.Message}");
                }
                
                Console.WriteLine($"📎 Total enlaces de descarga encontrados: {downloadLinks.Count}");
                
                // Debug: mostrar todos los enlaces encontrados
                if (downloadLinks.Count == 0)
                {
                    Console.WriteLine("🔍 Debug: Buscando TODOS los enlaces en el iframe...");
                    try
                    {
                        var allLinks = _driver.FindElements(By.TagName("a"));
                        Console.WriteLine($"📋 Total enlaces en iframe: {allLinks.Count}");
                        
                        foreach (var link in allLinks.Take(10)) // Solo los primeros 10
                        {
                            var href = link.GetAttribute("href");
                            var onclick = link.GetAttribute("onclick");
                            var text = link.Text?.Trim();
                            var title = link.GetAttribute("title");
                            
                            Console.WriteLine($"   🔗 Link: href='{href}', onclick='{onclick}', text='{text}', title='{title}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error en debug de enlaces: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("📋 Enlaces de descarga detectados:");
                    foreach (var link in downloadLinks)
                    {
                        var href = link.GetAttribute("href");
                        var text = link.Text?.Trim();
                        Console.WriteLine($"   🔗 {href} (texto: '{text}')");
                    }
                }
                
                var downloadedFiles = new List<string>();
                var failedFiles = new List<string>();
                
                foreach (var link in downloadLinks)
                {
                    try
                    {
                        var href = link.GetAttribute("href");
                        var fileName = ExtractFileNameFromUrl(href);
                        
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            Console.WriteLine($"⬇️ Descargando: {fileName}");
                            
                            // Para archivos XML, intentar descarga directa primero
                            if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine($"🔄 Intentando descarga directa primero para XML...");
                                if (await TryDirectDownload(href, fileName, targetDirectory, cancellationToken))
                                {
                                    downloadedFiles.Add(Path.Combine(targetDirectory, fileName));
                                    Console.WriteLine($"✅ Descarga directa exitosa: {fileName}");
                                    continue; // Continuar con el siguiente archivo
                                }
                                else
                                {
                                    Console.WriteLine($"🔄 Descarga directa falló, intentando con Selenium...");
                                }
                            }
                            
                            // Usar JavaScript para descargar sin cambiar la página
                            _driver.ExecuteScript($"window.open('{href}', '_blank');");
                            await Task.Delay(2000, cancellationToken); // Más tiempo para iniciar descarga
                            
                            // Esperar a que se complete la descarga
                            try
                            {
                                await WaitForSpecificDownload(fileName, _tempDownloadPath, cancellationToken);
                                
                                // Mover el archivo a la carpeta correcta
                                var sourceFile = Path.Combine(_tempDownloadPath, fileName);
                                var targetFile = Path.Combine(targetDirectory, fileName);
                                
                                if (File.Exists(sourceFile))
                                {
                                    File.Move(sourceFile, targetFile);
                                    downloadedFiles.Add(targetFile);
                                    Console.WriteLine($"✅ Archivo movido a: {targetFile}");
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Archivo no encontrado después de descarga: {fileName}");
                                    Console.WriteLine($"🔄 Intentando descarga directa...");
                                    
                                    // Intentar descarga directa como respaldo
                                    if (await TryDirectDownload(href, fileName, targetDirectory, cancellationToken))
                                    {
                                        downloadedFiles.Add(Path.Combine(targetDirectory, fileName));
                                        Console.WriteLine($"✅ Descarga directa exitosa: {fileName}");
                                    }
                                    else
                                    {
                                        failedFiles.Add(fileName);
                                    }
                                }
                            }
                            catch (Exception downloadEx)
                            {
                                Console.WriteLine($"❌ Error descargando {fileName}: {downloadEx.Message}");
                                Console.WriteLine($"🔄 Intentando descarga directa...");
                                
                                // Intentar descarga directa como respaldo
                                if (await TryDirectDownload(href, fileName, targetDirectory, cancellationToken))
                                {
                                    downloadedFiles.Add(Path.Combine(targetDirectory, fileName));
                                    Console.WriteLine($"✅ Descarga directa exitosa: {fileName}");
                                }
                                else
                                {
                                    failedFiles.Add(fileName);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error procesando enlace de descarga: {ex.Message}");
                        // Continuar con el siguiente enlace
                    }
                }
                
                // Mostrar resumen de descargas
                if (downloadedFiles.Count > 0)
                {
                    Console.WriteLine($"✅ Archivos descargados exitosamente: {downloadedFiles.Count}");
                    foreach (var file in downloadedFiles)
                    {
                        var downloadedFileInfo = new FileInfo(file);
                        Console.WriteLine($"   📄 {downloadedFileInfo.Name} ({downloadedFileInfo.Length / 1024.0:F1} KB)");
                    }
                }
                
                if (failedFiles.Count > 0)
                {
                    Console.WriteLine($"⚠️ Archivos que fallaron: {failedFiles.Count}");
                    foreach (var fileName in failedFiles)
                    {
                        Console.WriteLine($"   ❌ {fileName}");
                    }
                }
                
                // Volver al contenido principal
                _driver.SwitchTo().DefaultContent();
                
                // Cerrar el modal
                try
                {
                    var closeButton = _driver.FindElement(By.Id("gxp0_cls"));
                    closeButton.Click();
                }
                catch { }
                
                // Validar que se descargaron exactamente 3 archivos con los tipos correctos
                Console.WriteLine($"🔍 Validando archivos descargados en: {targetDirectory}");
                var finalFiles = Directory.GetFiles(targetDirectory);
                Console.WriteLine($"📄 Total archivos en carpeta: {finalFiles.Length}");
                
                if (finalFiles.Length != 3)
                {
                    Console.WriteLine($"⚠️ ERROR: Se esperaban 3 archivos pero se encontraron {finalFiles.Length}");
                    Console.WriteLine($"📋 Archivos encontrados:");
                    foreach (var file in finalFiles)
                    {
                        var info = new FileInfo(file);
                        Console.WriteLine($"   📄 {info.Name} ({info.Length / 1024.0:F1} KB)");
                    }
                    
                    // Eliminar carpeta y reintentar (solo una vez)
                    Console.WriteLine($"🗑️ Eliminando carpeta con archivos incorrectos...");
                    try
                    {
                        Directory.Delete(targetDirectory, true);
                        Console.WriteLine($"✅ Carpeta eliminada, se requiere reintento manual");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error eliminando carpeta: {ex.Message}");
                    }
                    
                    throw new DownloadException($"Error: Se descargaron {finalFiles.Length} archivos en lugar de 3 para {period.DisplayName}. Carpeta eliminada, reintente la descarga.");
                }
                
                // Validación detallada de tipos de archivo
                if (!ValidatePeriodFolder(targetDirectory, out string detailedValidation))
                {
                    Console.WriteLine($"⚠️ ERROR: Validación detallada falló: {detailedValidation}");
                    Console.WriteLine($"📋 Archivos encontrados:");
                    foreach (var file in finalFiles)
                    {
                        var info = new FileInfo(file);
                        var fileName = Path.GetFileName(file);
                        Console.WriteLine($"   📄 {fileName} ({info.Length / 1024.0:F1} KB)");
                    }
                    
                    // Eliminar carpeta y reintentar
                    Console.WriteLine($"🗑️ Eliminando carpeta con tipos de archivo incorrectos...");
                    try
                    {
                        Directory.Delete(targetDirectory, true);
                        Console.WriteLine($"✅ Carpeta eliminada, se requiere reintento manual");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error eliminando carpeta: {ex.Message}");
                    }
                    
                    throw new DownloadException($"Error: {detailedValidation} para {period.DisplayName}. Carpeta eliminada, reintente la descarga.");
                }
                
                Console.WriteLine($"✅ Validación exitosa: {detailedValidation}");
                
                if (downloadedFiles.Count == 0)
                {
                    throw new DownloadException($"No se descargó ningún archivo para {period.DisplayName}");
                }
                
                // Retornar metadata del primer archivo descargado
                var firstFile = downloadedFiles.First();
                var fileInfo = new FileInfo(firstFile);
                var hash = await CalculateFileHashAsync(firstFile);

                return new FileMetadata(
                    fileName: fileInfo.Name,
                    filePath: firstFile,
                    fileSize: fileInfo.Length,
                    fileType: GetFileType(fileInfo.Extension),
                    downloadedAt: DateTime.UtcNow,
                    hash: hash
                );
            }
            catch (Exception ex)
            {
                throw new DownloadException($"Error al descargar archivo para {period.DisplayName}", ex);
            }
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_isLoggedIn)
                {
                    // Intentar hacer logout
                    try
                    {
                        var logoutButton = _driver.FindElement(By.LinkText("Cerrar Sesión"));
                        logoutButton.Click();
                        await Task.Delay(500, cancellationToken);
                    }
                    catch
                    {
                        // Si no hay botón de logout, simplemente navegar a página de logout
                        _driver.Navigate().GoToUrl("https://pei.guanajuato.gob.mx/recibo/logout.aspx");
                    }
                    
                    _isLoggedIn = false;
                }
            }
            catch (Exception)
            {
                // Ignorar errores en logout
                _isLoggedIn = false;
            }
        }

        public Task<bool> ValidateSessionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Verificar si estamos en una página que requiere autenticación
                return Task.FromResult(_isLoggedIn && !_driver.Url.Contains("login.aspx"));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        #region Private Methods

        private ChromeOptions CreateChromeOptions(DownloadConfig config)
        {
            var chromeOptions = new ChromeOptions();
            
            // Configure automatic downloads
            chromeOptions.AddUserProfilePreference("download.default_directory", _tempDownloadPath);
            chromeOptions.AddUserProfilePreference("download.prompt_for_download", false);
            chromeOptions.AddUserProfilePreference("download.directory_upgrade", true);
            chromeOptions.AddUserProfilePreference("safebrowsing.enabled", false); // Deshabilitar para evitar bloqueos
            chromeOptions.AddUserProfilePreference("plugins.always_open_pdf_externally", true);
            chromeOptions.AddUserProfilePreference("profile.default_content_settings.popups", 0);
            chromeOptions.AddUserProfilePreference("profile.default_content_setting_values.automatic_downloads", 1);
            
            // Configuraciones adicionales para descargas
            chromeOptions.AddUserProfilePreference("download.extensions_to_open", "");
            chromeOptions.AddUserProfilePreference("profile.default_content_settings.multiple-automatic-downloads", 1);
            
            // Headless mode for performance
            chromeOptions.AddArgument("--headless=new");
            chromeOptions.AddArgument("--disable-notifications");
            chromeOptions.AddArgument("--disable-extensions");
            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArgument("--disable-dev-shm-usage");
            chromeOptions.AddArgument("--disable-gpu");
            chromeOptions.AddArgument("--disable-images"); // Optimización para headless
            chromeOptions.AddArgument("--disable-web-security");
            chromeOptions.AddArgument("--allow-running-insecure-content");
            chromeOptions.AddArgument("--disable-features=VizDisplayCompositor");
            
            // Silenciar completamente los logs de Chrome
            chromeOptions.AddArgument("--log-level=3"); // Solo errores fatales
            chromeOptions.AddArgument("--silent");
            chromeOptions.AddArgument("--disable-logging");
            chromeOptions.AddArgument("--disable-dev-tools");
            chromeOptions.AddArgument("--disable-background-networking");
            chromeOptions.AddArgument("--disable-background-timer-throttling");
            chromeOptions.AddArgument("--disable-renderer-backgrounding");
            chromeOptions.AddArgument("--disable-backgrounding-occluded-windows");
            chromeOptions.AddArgument("--disable-client-side-phishing-detection");
            chromeOptions.AddArgument("--disable-sync");
            chromeOptions.AddArgument("--metrics-recording-only");
            chromeOptions.AddArgument("--no-report-upload");
            chromeOptions.AddArgument("--no-first-run");
            chromeOptions.AddArgument("--disable-crash-reporter");
            chromeOptions.AddArgument("--disable-component-update");
            
            return chromeOptions;
        }

        private bool TryLoginInIframes(LoginCredentials credentials)
        {
            try
            {
                var iframes = _driver.FindElements(By.TagName("iframe"));
                foreach (var iframe in iframes)
                {
                    _driver.SwitchTo().Frame(iframe);
                    if (TryLoginInCurrentContext(credentials))
                    {
                        return true;
                    }
                    _driver.SwitchTo().DefaultContent();
                }
                return false;
            }
            catch
            {
                _driver.SwitchTo().DefaultContent();
                return false;
            }
        }

        private bool TryLoginInMainPage(LoginCredentials credentials)
        {
            try
            {
                return TryLoginInCurrentContext(credentials);
            }
            catch
            {
                return false;
            }
        }

        private bool TryLoginInCurrentContext(LoginCredentials credentials)
        {
            try
            {
                Console.WriteLine("🔍 Buscando campos de usuario y contraseña...");
                
                // Intentar múltiples selectores comunes, empezando por los correctos
                IWebElement? usernameField = null;
                IWebElement? passwordField = null;
                IWebElement? loginButton = null;

                // Intentar encontrar campo de usuario (RFC) - selectores actualizados según HTML real
                var userSelectors = new[] { "vUSUARIO", "txtUsuario", "usuario", "username", "user", "login" };
                foreach (var selector in userSelectors)
                {
                    try
                    {
                        usernameField = _driver.FindElement(By.Id(selector));
                        Console.WriteLine($"✅ Campo de usuario encontrado: {selector}");
                        break;
                    }
                    catch { }
                }

                // Intentar encontrar campo de contraseña - selectores actualizados según HTML real
                var passwordSelectors = new[] { "vPASSWORD", "txtPassword", "password", "pass", "pwd", "clave" };
                foreach (var selector in passwordSelectors)
                {
                    try
                    {
                        passwordField = _driver.FindElement(By.Id(selector));
                        Console.WriteLine($"✅ Campo de contraseña encontrado: {selector}");
                        break;
                    }
                    catch { }
                }

                // Intentar encontrar botón de login - selectores actualizados según HTML real
                var buttonSelectors = new[] { "BUTTON1", "btnIniciarSesion", "btnLogin", "login", "submit", "entrar" };
                foreach (var selector in buttonSelectors)
                {
                    try
                    {
                        loginButton = _driver.FindElement(By.Id(selector));
                        Console.WriteLine($"✅ Botón de login encontrado: {selector}");
                        break;
                    }
                    catch { }
                }

                if (usernameField == null || passwordField == null || loginButton == null)
                {
                    Console.WriteLine("❌ No se encontraron todos los elementos necesarios para el login");
                    Console.WriteLine($"   Usuario: {usernameField != null}");
                    Console.WriteLine($"   Contraseña: {passwordField != null}");
                    Console.WriteLine($"   Botón: {loginButton != null}");
                    
                    // Mostrar elementos disponibles para debugging
                    try
                    {
                        var allInputs = _driver.FindElements(By.TagName("input"));
                        Console.WriteLine($"📋 Total elementos input encontrados: {allInputs.Count}");
                        foreach (var input in allInputs.Take(10)) // Solo los primeros 10
                        {
                            var id = input.GetAttribute("id");
                            var type = input.GetAttribute("type");
                            var name = input.GetAttribute("name");
                            Console.WriteLine($"   Input: id='{id}', type='{type}', name='{name}'");
                        }
                    }
                    catch { }
                    
                    return false;
                }

                Console.WriteLine("📝 Llenando campos de login...");
                usernameField.Clear();
                usernameField.SendKeys(credentials.Username);
                
                passwordField.Clear();
                passwordField.SendKeys(credentials.Password);
                
                Console.WriteLine("🔄 Haciendo clic en el botón de login...");
                loginButton.Click();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en login: {ex.Message}");
                return false;
            }
        }

        private void WaitForLoginConfirmation()
        {
            // Esperar a que aparezca un elemento que confirme el login exitoso
            _wait.Until(driver => 
                driver.Url.Contains("bienvenido.aspx") || 
                driver.Url.Contains("consultarecibo.aspx") ||
                driver.Url.Contains("disponible.aspx") || 
                driver.FindElements(By.LinkText("Cerrar Sesión")).Count > 0);
        }

        private void NavigateToWelcomePage()
        {
            try
            {
                // Primero navegar a la página de bienvenida
                Console.WriteLine("🏠 Navegando a página de bienvenida...");
                _driver.Navigate().GoToUrl("https://pei.guanajuato.gob.mx/recibo/bienvenido.aspx");
                
                // Esperar un momento para que cargue
                System.Threading.Thread.Sleep(1000);
                
                // Luego forzar redirección a consulta de recibos
                Console.WriteLine("📋 Redirigiendo a consulta de recibos...");
                _driver.Navigate().GoToUrl("https://pei.guanajuato.gob.mx/recibo/consultarecibo.aspx");
                
                // Esperar a que cargue la página de consulta
                System.Threading.Thread.Sleep(1000);
                Console.WriteLine($"✅ Navegación completada: {_driver.Url}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error en navegación post-login: {ex.Message}");
                // Si no puede navegar, mantener la página actual
            }
        }

        private FileType GetFileType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => FileType.ReciboPdf,
                ".xml" => FileType.CfdiXml,
                _ => FileType.ReciboPdf
            };
        }

        private async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = await Task.Run(() => md5.ComputeHash(stream));
            return Convert.ToHexString(hashBytes);
        }

        private string SanitizeFolderName(string folderName)
        {
            // Reemplazar caracteres no válidos con underscore
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var invalidChar in invalidChars)
            {
                folderName = folderName.Replace(invalidChar, '_');
            }
            
            // Reemplazar espacios con underscore
            folderName = folderName.Replace(' ', '_');
            
            // Reemplazar caracteres especiales comunes
            folderName = folderName.Replace('-', '_')
                                   .Replace('(', '_')
                                   .Replace(')', '_')
                                   .Replace(',', '_')
                                   .Replace('.', '_');
            
            // Remover underscores múltiples
            while (folderName.Contains("__"))
            {
                folderName = folderName.Replace("__", "_");
            }
            
            return folderName.Trim('_');
        }

        private string ExtractFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var fileName = Path.GetFileName(uri.LocalPath);
                
                // Si no hay nombre de archivo en la URL, intentar extraer de query parameters
                if (string.IsNullOrEmpty(fileName) || fileName == "/" || !fileName.Contains('.'))
                {
                    // Buscar en query parameters
                    var query = uri.Query;
                    if (!string.IsNullOrEmpty(query))
                    {
                        // Buscar patrones comunes de nombres de archivo
                        var patterns = new[]
                        {
                            @"filename=([^&]+)",
                            @"file=([^&]+)",
                            @"name=([^&]+)",
                            @"[?&](CFDI_[^&]+)",
                            @"[?&](Recibo_[^&]+)",
                            @"[?&](recibo_[^&]+)"
                        };
                        
                        foreach (var pattern in patterns)
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(query, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                var extractedName = match.Groups[1].Value;
                                if (!string.IsNullOrEmpty(extractedName) && extractedName.Contains('.'))
                                {
                                    fileName = System.Web.HttpUtility.UrlDecode(extractedName);
                                    Console.WriteLine($"📄 Nombre extraído de query: {fileName}");
                                    break;
                                }
                            }
                        }
                    }
                    
                    // Si aún no hay nombre, generar uno basado en el contenido de la URL
                    if (string.IsNullOrEmpty(fileName) || fileName == "/" || !fileName.Contains('.'))
                    {
                        var extension = ".bin";
                        if (url.ToLower().Contains("pdf"))
                            extension = ".pdf";
                        else if (url.ToLower().Contains("xml"))
                            extension = ".xml";
                        else if (url.ToLower().Contains("cfdi"))
                            extension = url.ToLower().Contains("xml") ? ".xml" : ".pdf";
                        else if (url.ToLower().Contains("recibo"))
                            extension = ".pdf";
                            
                        fileName = $"download_{Guid.NewGuid()}{extension}";
                        Console.WriteLine($"📄 Nombre generado: {fileName}");
                    }
                }
                
                // Limpiar el nombre de archivo
                fileName = System.Web.HttpUtility.UrlDecode(fileName);
                
                return fileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error extrayendo nombre de archivo de {url}: {ex.Message}");
                
                // Si falla el parsing de URL, generar nombre único
                var extension = ".bin";
                if (url.ToLower().Contains("pdf"))
                    extension = ".pdf";
                else if (url.ToLower().Contains("xml"))
                    extension = ".xml";
                else if (url.ToLower().Contains("cfdi"))
                    extension = url.ToLower().Contains("xml") ? ".xml" : ".pdf";
                else if (url.ToLower().Contains("recibo"))
                    extension = ".pdf";
                    
                return $"download_{Guid.NewGuid()}{extension}";
            }
        }

        private async Task WaitForSpecificDownload(string fileName, string downloadPath, CancellationToken cancellationToken)
        {
            // Aumentar timeout para archivos XML que pueden tardar más
            var timeout = fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ? 
                TimeSpan.FromMinutes(3) : TimeSpan.FromMinutes(2);
            
            var startTime = DateTime.UtcNow;
            var filePath = Path.Combine(downloadPath, fileName);
            
            Console.WriteLine($"🕐 Esperando descarga: {fileName} (timeout: {timeout.TotalMinutes:F1} min)");

            while (DateTime.UtcNow - startTime < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Verificar si el archivo existe y no está siendo descargado
                if (File.Exists(filePath))
                {
                    try
                    {
                        // Intentar abrir el archivo para verificar que no está en uso
                        using var stream = File.OpenRead(filePath);
                        var fileSizeKB = stream.Length / 1024.0;
                        Console.WriteLine($"✅ Descarga completada: {fileName} ({fileSizeKB:F1} KB)");
                        return;
                    }
                    catch (IOException)
                    {
                        // El archivo aún está siendo escrito
                        var elapsed = DateTime.UtcNow - startTime;
                        Console.WriteLine($"📝 Archivo en proceso: {fileName} ({elapsed.TotalSeconds:F0}s)");
                    }
                }
                else
                {
                    // Mostrar progreso cada 10 segundos
                    var elapsed = DateTime.UtcNow - startTime;
                    if ((int)elapsed.TotalSeconds % 10 == 0 && elapsed.TotalSeconds > 0)
                    {
                        Console.WriteLine($"⏳ Esperando descarga: {fileName} ({elapsed.TotalSeconds:F0}s/{timeout.TotalSeconds:F0}s)");
                    }
                }

                await Task.Delay(500, cancellationToken);
            }

            Console.WriteLine($"⚠️ Timeout esperando descarga: {fileName} después de {timeout.TotalMinutes:F1} minutos");
        }

        private async Task<bool> TryDirectDownload(string url, string fileName, string targetDirectory, CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5); // Timeout más largo
                
                // Copiar cookies del navegador para mantener la sesión
                var cookies = _driver.Manage().Cookies.AllCookies;
                var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                
                // Headers importantes para mantener la sesión
                httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36");
                httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "es-ES,es;q=0.9,en;q=0.8");
                httpClient.DefaultRequestHeaders.Add("Referer", _driver.Url);
                
                Console.WriteLine($"🌐 Descarga directa: {fileName}");
                Console.WriteLine($"📍 URL: {url}");
                Console.WriteLine($"🍪 Cookies: {cookies.Count} enviadas");
                
                var response = await httpClient.GetAsync(url, cancellationToken);
                
                Console.WriteLine($"📡 Respuesta HTTP: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error HTTP: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
                
                var targetFile = Path.Combine(targetDirectory, fileName);
                await using var fileStream = File.Create(targetFile);
                await response.Content.CopyToAsync(fileStream, cancellationToken);
                
                var fileSizeKB = fileStream.Length / 1024.0;
                Console.WriteLine($"✅ Descarga directa completada: {fileName} ({fileSizeKB:F1} KB)");
                
                // Verificar que el archivo no esté vacío o sea muy pequeño
                if (fileStream.Length < 100)
                {
                    Console.WriteLine($"⚠️ Archivo sospechosamente pequeño ({fileSizeKB:F1} KB), posible error");
                    File.Delete(targetFile);
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en descarga directa de {fileName}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        #endregion

        #region Helper Methods for Download Link Detection

        private async Task ScrollToLoadAllPeriods(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("📜 Iniciando scroll para cargar todos los períodos...");
                
                // Buscar el contenedor de la tabla
                IWebElement gridContainer;
                try
                {
                    gridContainer = _driver.FindElement(By.Id("Grid1ContainerDiv"));
                }
                catch
                {
                    // Si no se encuentra el contenedor específico, usar la tabla directamente
                    gridContainer = _driver.FindElement(By.Id("Grid1ContainerTbl"));
                }
                
                // Contar filas iniciales
                var initialRows = _driver.FindElements(By.CssSelector("#Grid1ContainerTbl tbody tr"));
                var previousRowCount = initialRows.Count;
                Console.WriteLine($"📊 Filas iniciales: {previousRowCount}");
                
                // Hacer scroll dentro del contenedor de la tabla
                for (int i = 0; i < 10; i++) // Máximo 10 intentos de scroll
                {
                    // Scroll hacia abajo en el contenedor
                    _driver.ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight;", gridContainer);
                    await Task.Delay(1000, cancellationToken); // Esperar a que carguen nuevas filas
                    
                    // También hacer scroll en la página completa por si acaso
                    _driver.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
                    await Task.Delay(500, cancellationToken);
                    
                    // Contar filas actuales
                    var currentRows = _driver.FindElements(By.CssSelector("#Grid1ContainerTbl tbody tr"));
                    var currentRowCount = currentRows.Count;
                    
                    Console.WriteLine($"📊 Scroll {i + 1}: {currentRowCount} filas encontradas");
                    
                    // Si no hay nuevas filas después de 2 intentos consecutivos, terminar
                    if (currentRowCount == previousRowCount)
                    {
                        Console.WriteLine($"✅ No hay más períodos que cargar. Total filas: {currentRowCount}");
                        break;
                    }
                    
                    previousRowCount = currentRowCount;
                }
                
                // Scroll final para asegurar que estamos al final
                _driver.ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight;", gridContainer);
                await Task.Delay(1000, cancellationToken);
                
                var finalRows = _driver.FindElements(By.CssSelector("#Grid1ContainerTbl tbody tr"));
                Console.WriteLine($"📊 Total períodos cargados después del scroll: {finalRows.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error durante el scroll de períodos: {ex.Message}");
                Console.WriteLine("🔄 Continuando sin scroll...");
            }
        }

        private bool ValidatePeriodFolder(string folderPath, out string validationMessage)
        {
            validationMessage = "";
            
            if (!Directory.Exists(folderPath))
            {
                validationMessage = "Carpeta no existe";
                return false;
            }
            
            var files = Directory.GetFiles(folderPath);
            
            if (files.Length == 0)
            {
                validationMessage = "Carpeta vacía";
                return false;
            }
            
            if (files.Length != 3)
            {
                validationMessage = $"Carpeta tiene {files.Length} archivos (debe tener 3)";
                return false;
            }
            
            // Verificar que hay exactamente 1 XML y 2 PDFs con los patrones correctos
            var xmlFiles = files.Where(f => Path.GetExtension(f).Equals(".xml", StringComparison.OrdinalIgnoreCase)).ToArray();
            var pdfFiles = files.Where(f => Path.GetExtension(f).Equals(".pdf", StringComparison.OrdinalIgnoreCase)).ToArray();
            
            if (xmlFiles.Length != 1)
            {
                validationMessage = $"Debe tener exactamente 1 archivo XML (encontrados: {xmlFiles.Length})";
                return false;
            }
            
            if (pdfFiles.Length != 2)
            {
                validationMessage = $"Debe tener exactamente 2 archivos PDF (encontrados: {pdfFiles.Length})";
                return false;
            }
            
            // Verificar patrones de nombres específicos
            var xmlFile = xmlFiles[0];
            var xmlFileName = Path.GetFileName(xmlFile);
            if (!xmlFileName.StartsWith("CFDI_xml_", StringComparison.OrdinalIgnoreCase))
            {
                validationMessage = $"Archivo XML no sigue el patrón 'CFDI_xml_': {xmlFileName}";
                return false;
            }
            
            // Verificar que hay un PDF de recibo y un PDF de CFDI
            var reciboPdf = pdfFiles.FirstOrDefault(f => Path.GetFileName(f).StartsWith("Recibo_", StringComparison.OrdinalIgnoreCase));
            var cfdiPdf = pdfFiles.FirstOrDefault(f => Path.GetFileName(f).StartsWith("CFDI_pdf_", StringComparison.OrdinalIgnoreCase));
            
            if (reciboPdf == null)
            {
                validationMessage = "Falta archivo PDF de recibo (patrón 'Recibo_')";
                return false;
            }
            
            if (cfdiPdf == null)
            {
                validationMessage = "Falta archivo PDF de CFDI (patrón 'CFDI_pdf_')";
                return false;
            }
            
            // Verificar tamaños de archivo (archivos muy pequeños pueden estar corruptos)
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.Length < 100) // Menos de 100 bytes probablemente sea un error
                {
                    validationMessage = $"Archivo {fileInfo.Name} es muy pequeño ({fileInfo.Length} bytes)";
                    return false;
                }
            }
            
            validationMessage = "Carpeta válida: 1 XML + 2 PDFs (Recibo + CFDI) con patrones correctos";
            return true;
        }

        private Task<List<IWebElement>> FindDownloadLinksAsync(bool isInFrame)
        {
            var downloadLinks = new List<IWebElement>();
            
            // Intentar múltiples selectores para capturar todos los enlaces
            var selectors = new[]
            {
                "a[href*='.pdf']",
                "a[href*='.xml']", 
                "a[href*='.PDF']",
                "a[href*='.XML']",
                "a[href*='CFDI_pdf']",
                "a[href*='CFDI_xml']",
                "a[href*='Recibo_']",
                "a[href*='recibo_']",
                "a[onclick*='download']",
                "a[onclick*='Descargar']",
                "a[title*='Descargar']",
                "a[title*='Download']",
                "button[onclick*='download']",
                "button[title*='Descargar']",
                "input[onclick*='download']",
                "[data-action*='download']"
            };
            
            foreach (var selector in selectors)
            {
                try
                {
                    var elements = _driver.FindElements(By.CssSelector(selector));
                    foreach (var element in elements)
                    {
                        if (!downloadLinks.Contains(element))
                        {
                            var href = element.GetAttribute("href");
                            var onclick = element.GetAttribute("onclick");
                            var title = element.GetAttribute("title");
                            var dataAction = element.GetAttribute("data-action");
                            
                            // Verificar que sea realmente un enlace de descarga
                            if (IsValidDownloadElement(href, onclick, title, dataAction, element.Text))
                            {
                                downloadLinks.Add(element);
                                Console.WriteLine($"🔗 Enlace encontrado: {href ?? onclick ?? title} (selector: {selector})");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error con selector {selector}: {ex.Message}");
                }
            }
            
            return Task.FromResult(downloadLinks);
        }

        private Task<List<IWebElement>> TryAlternativeDownloadMethods()
        {
            var alternativeLinks = new List<IWebElement>();
            
            try
            {
                // Método 1: Buscar cualquier elemento clickeable con texto relacionado a descarga
                var textBasedSelectors = new[]
                {
                    "//*[contains(text(), 'Descargar')]",
                    "//*[contains(text(), 'Download')]",
                    "//*[contains(text(), 'PDF')]",
                    "//*[contains(text(), 'XML')]",
                    "//*[contains(text(), 'CFDI')]",
                    "//*[contains(text(), 'Recibo')]"
                };
                
                foreach (var xpath in textBasedSelectors)
                {
                    try
                    {
                        var elements = _driver.FindElements(By.XPath(xpath));
                        foreach (var element in elements)
                        {
                            if (element.Enabled && element.Displayed)
                            {
                                alternativeLinks.Add(element);
                                Console.WriteLine($"🔗 Elemento alternativo encontrado: {element.Text} (xpath: {xpath})");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error con xpath {xpath}: {ex.Message}");
                    }
                }
                
                // Método 2: Buscar elementos con atributos de datos específicos
                try
                {
                    var dataElements = _driver.FindElements(By.CssSelector("[data-url], [data-file], [data-download]"));
                    alternativeLinks.AddRange(dataElements.Where(e => e.Enabled && e.Displayed));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error buscando elementos con data attributes: {ex.Message}");
                }
                
                // Método 3: Buscar forms con action de descarga
                try
                {
                    var forms = _driver.FindElements(By.CssSelector("form[action*='download'], form[action*='export']"));
                    foreach (var form in forms)
                    {
                        var submitButtons = form.FindElements(By.CssSelector("input[type='submit'], button[type='submit']"));
                        alternativeLinks.AddRange(submitButtons.Where(b => b.Enabled && b.Displayed));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error buscando formularios: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error en métodos alternativos: {ex.Message}");
            }
            
            return Task.FromResult(alternativeLinks);
        }

        private bool IsValidDownloadElement(string? href, string? onclick, string? title, string? dataAction, string? text)
        {
            // Verificar si es un enlace de descarga válido
            var allAttributes = new[] { href, onclick, title, dataAction, text }.Where(s => !string.IsNullOrEmpty(s));
            
            foreach (var attr in allAttributes)
            {
                if (attr != null && (
                    attr.Contains(".pdf", StringComparison.OrdinalIgnoreCase) ||
                    attr.Contains(".xml", StringComparison.OrdinalIgnoreCase) ||
                    attr.Contains("CFDI", StringComparison.OrdinalIgnoreCase) ||
                    attr.Contains("Recibo", StringComparison.OrdinalIgnoreCase) ||
                    attr.Contains("download", StringComparison.OrdinalIgnoreCase) ||
                    attr.Contains("Descargar", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            
            return false;
        }

        #endregion

        public void Dispose()
        {
            try
            {
                _driver?.Quit();
                _driver?.Dispose();
            }
            catch
            {
                // Ignorar errores al cerrar
            }
        }
    }
}
