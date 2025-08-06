using Xunit;

namespace NominaDownloaderPEIGTO.Tests.Collections;

/// <summary>
/// Colección de tests que modifican Console.Out para evitar ejecución paralela
/// </summary>
[CollectionDefinition("Console Tests")]
public class ConsoleTestCollection
{
    // Esta clase existe solo para definir la colección
    // No necesita implementación
}
