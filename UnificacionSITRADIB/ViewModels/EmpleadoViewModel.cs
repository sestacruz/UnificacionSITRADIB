namespace UnificacionSITRADIB.ViewModels
{
    public class EmpleadoViewModel
    {
        public string Dni { get; set; }
        public string Nombre { get; set; }
        public List<RegistroLSD> Registros { get; set; } = new();

        public List<ConceptoLiquidado> Conceptos
        {
            get
            {
                return Registros
                    .Where(r => r.Tipo == "03")
                    .Select(r => new ConceptoLiquidado
                    {
                        Codigo = r.Texto.Substring(13, 10).Trim(),
                        Cantidad = int.TryParse(r.Texto.AsSpan(23, 5), out var cantidad) ? cantidad : 0,
                        Unidad = TraducirUnidad(r.Texto.Substring(28, 1).Trim()),
                        Importe = decimal.TryParse(r.Texto.AsSpan(29, 15), out var importe) ? importe / 100 : 0,
                        Indicador = r.Texto.Substring(44, 1) == "C" ? "Crédito" : "Débito",
                    })
                    .ToList();
            }
        }

        private string TraducirUnidad(string unidad)
        {
            return unidad switch
            {
                "A" => "Año",
                "M" => "Mes",
                "Q" => "Quincena",
                "S" => "Semana",
                "D" => "Días",
                "H" => "Horas",
                _ => unidad
            };
        }
    }
}
