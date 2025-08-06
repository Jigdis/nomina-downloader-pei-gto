using NominaDownloaderPEIGTO.Domain.ValueObjects;

namespace NominaDownloaderPEIGTO.Console.Services;

public class ConsoleUserInteractionService : IUserInteractionService
{
    public LoginCredentials? GetCredentialsFromUser()
    {
        try
        {
            System.Console.Write("👤 RFC: ");
            var username = System.Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(username))
                return null;

            System.Console.Write("🔒 Contraseña: ");
            var password = ReadPassword();
            
            if (string.IsNullOrWhiteSpace(password))
                return null;

            return new LoginCredentials(username, password);
        }
        catch
        {
            return null;
        }
    }

    public DownloadScope SelectDownloadScope()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("📦 Seleccione el alcance de descarga:");
        System.Console.WriteLine("  1. Períodos específicos de un año");
        System.Console.WriteLine("  2. Todo un año completo");
        System.Console.WriteLine("  3. Todos los años disponibles");
        System.Console.WriteLine();
        System.Console.Write("Seleccione una opción (1-3): ");
        
        var input = System.Console.ReadLine();
        
        return input?.Trim() switch
        {
            "1" => DownloadScope.SpecificPeriods,
            "2" => DownloadScope.EntireYear,
            "3" => DownloadScope.AllYears,
            _ => DownloadScope.SpecificPeriods // Default
        };
    }

    public int? SelectYear(IEnumerable<int> availableYears)
    {
        var yearsList = availableYears.OrderByDescending(y => y).ToList();
        
        if (yearsList.Count == 0)
        {
            System.Console.WriteLine("⚠️ No hay años disponibles");
            return null;
        }

        if (yearsList.Count == 1)
        {
            System.Console.WriteLine($"📅 Solo hay un año disponible: {yearsList[0]}");
            return yearsList[0];
        }

        System.Console.WriteLine();
        System.Console.WriteLine("📅 Años disponibles:");
        
        for (int i = 0; i < yearsList.Count; i++)
        {
            System.Console.WriteLine($"  {i + 1}. {yearsList[i]}");
        }

        System.Console.WriteLine();
        System.Console.Write($"Seleccione un año (1-{yearsList.Count}): ");
        
        var input = System.Console.ReadLine();
        
        if (int.TryParse(input, out int selection) && 
            selection >= 1 && selection <= yearsList.Count)
        {
            return yearsList[selection - 1];
        }

        System.Console.WriteLine("❌ Selección inválida");
        return null;
    }

    public IEnumerable<PeriodInfo> SelectPeriods(IEnumerable<PeriodInfo> availablePeriods)
    {
        var periodsList = availablePeriods.ToList();
        
        System.Console.WriteLine();
        System.Console.WriteLine("📅 Períodos disponibles:");
        
        for (int i = 0; i < periodsList.Count; i++)
        {
            System.Console.WriteLine($"  {i + 1}. {periodsList[i].DisplayName}");
        }

        System.Console.WriteLine($"  {periodsList.Count + 1}. Todos los períodos");
        System.Console.WriteLine();
        System.Console.Write("Seleccione una opción (números separados por coma, o 'enter' para todos): ");
        
        var input = System.Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(input))
        {
            return periodsList;
        }

        var selectedPeriods = new List<PeriodInfo>();
        var selections = input.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var selection in selections)
        {
            if (int.TryParse(selection.Trim(), out int index))
            {
                if (index == periodsList.Count + 1)
                {
                    return periodsList; // Todos los períodos
                }
                else if (index >= 1 && index <= periodsList.Count)
                {
                    selectedPeriods.Add(periodsList[index - 1]);
                }
            }
        }

        return selectedPeriods.Distinct();
    }

    private static string ReadPassword()
    {
        var password = string.Empty;
        ConsoleKeyInfo key;

        do
        {
            key = System.Console.ReadKey(true);

            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
            {
                password += key.KeyChar;
                System.Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password[0..^1];
                System.Console.Write("\b \b");
            }
        }
        while (key.Key != ConsoleKey.Enter);

        System.Console.WriteLine();
        return password;
    }
}
