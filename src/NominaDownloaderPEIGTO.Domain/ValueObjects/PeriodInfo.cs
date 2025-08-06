namespace NominaDownloaderPEIGTO.Domain.ValueObjects
{
    /// <summary>
    /// Objeto de valor para información de período de nómina
    /// </summary>
    public record PeriodInfo
    {
        public int Year { get; }
        public int Period { get; }
        public string Description { get; }
        public string DisplayName { get; }
        
        /// <summary>
        /// Propiedad para compatibilidad con código existente que espera Month
        /// </summary>
        public int Month => Period;

        public PeriodInfo(int year, int period, string description = "")
        {
            if (year < 2000 || year > DateTime.Now.Year + 1)
                throw new ArgumentException($"El año debe estar entre 2000 y {DateTime.Now.Year + 1}", nameof(year));
            
            if (period < 0 || period > 99)
                throw new ArgumentException("El período debe estar entre 0 y 99", nameof(period));

            Year = year;
            Period = period;
            Description = description;
            DisplayName = string.IsNullOrEmpty(description) ? 
                $"Período {period:D2} - {year}" : 
                $"Período {period:D2}: {description}";
        }
        
        /// <summary>
        /// Constructor para compatibilidad con código existente que usa mes
        /// </summary>
        public PeriodInfo(int year, int month) : this(year, month, GetMonthName(month))
        {
        }

        private static string GetMonthName(int month) => month switch
        {
            0 => "Complementaría",
            1 => "Enero",
            2 => "Febrero",
            3 => "Marzo",
            4 => "Abril",
            5 => "Mayo",
            6 => "Junio",
            7 => "Julio",
            8 => "Agosto",
            9 => "Septiembre",
            10 => "Octubre",
            11 => "Noviembre",
            12 => "Diciembre",
            _ => throw new ArgumentException("Mes inválido")
        };

        public override string ToString() => DisplayName;

        /// <summary>
        /// Obtiene una clave única para el período
        /// </summary>
        public string GetPeriodKey() => $"{Year}-{Period:D2}";
    }
}
