using System;
using System.Collections.Generic;
using System.Linq;

namespace UnificacionSITRADIB.ViewModels
{
    public class EmpleadoViewModel
    {
        public string Dni { get; set; }
        public string Nombre { get; set; }
        public List<RegistroLSD> Registros { get; set; } = new();

        /// <summary>
        /// Conceptos liquidados interpretados desde los registros tipo 03.
        /// </summary>
        public List<ConceptoLiquidado> Conceptos
        {
            get
            {
                var conceptos = new List<ConceptoLiquidado>();

                // Procesar registros tipo 03 (detalle de conceptos)
                foreach (var r in Registros.Where(r => r.Tipo == "03"))
                {
                    try
                    {
                        if (r.Texto.Length < 51) continue;

                        // Formato tipo 03 según AFIP:
                        // Pos 1-2:   Tipo de registro (03)
                        // Pos 3-13:  CUIL (11)
                        // Pos 14-23: Código Concepto (10)
                        // Pos 24-28: Cantidad (3 con 2 decimales)
                        // Pos 29:    Unidad (1)
                        // Pos 30-44: Importe (13 con 2 decimales)
                        // Pos 45:    Indicador C/D (1)
                        // Pos 46-51: Período ajuste (6, AAAAMM) - opcional

                        // Código Concepto
                        string codigo = r.Texto.Substring(13,10).Trim();

                        // Cantidad(3 con 2 decimales)
                        decimal cantidad = 0;
                        string cantStr = r.Texto.Substring(23, 5).Trim();
                        if (decimal.TryParse(cantStr, out var cant))
                            cantidad = cant / 100m;
                        
                        // Unidad
                        string unidad = r.Texto.Substring(29, 1);

                        // Importe (13 caracteres, 2 decimales)
                        decimal importe = 0;
                        string impStr = r.Texto.Substring(29, 15).Trim();
                        if (decimal.TryParse(impStr, out var imp))
                            importe = imp / 100m;
                        
                        // Indicador C/D
                        string indicador = r.Texto.Substring(44, 1);

                        // Si es débito, el importe debe ser negativo
                        if (indicador == "D")
                            importe = -Math.Abs(importe);

                        // Período ajuste retroactivo
                        DateTime? periodoRetroactivo = null;
                        string periodoStr = r.Texto.Substring(45, 6).Trim();
                        if (!string.IsNullOrWhiteSpace(periodoStr) && periodoStr.Length == 6)
                        {
                            if (DateTime.TryParseExact(periodoStr, "yyyyMM", null,
                                System.Globalization.DateTimeStyles.None, out var fecha))
                            {
                                periodoRetroactivo = fecha;
                            }
                        }

                        conceptos.Add(new ConceptoLiquidado
                        {
                            Codigo = codigo,
                            Cantidad = cantidad,
                            Unidad = TraducirUnidad(unidad),
                            Importe = importe,
                            Indicador = indicador == "C" ? "Crédito" : "Débito",
                            PeriodoAjusteRetroactivo = periodoRetroactivo ?? DateTime.MinValue
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error procesando registro 03: {ex.Message}");
                    }
                }

                return conceptos;
            }
        }

        /// <summary>
        /// Total de conceptos remunerativos (códigos 110000 a 199999)
        /// </summary>
        public decimal TotalDebitos => Conceptos
                    .Where(c => !string.IsNullOrEmpty(c.Codigo))
                    .Where(static c => c.Indicador.StartsWith("D"))
                    .Sum(c => c.Importe);

        /// <summary>
        /// Total de conceptos no remunerativos (códigos 510000 a 799999)
        /// </summary>
        public decimal TotalCreditos => Conceptos
                    .Where(c => !string.IsNullOrEmpty(c.Codigo))
                    .Where(static c => c.Indicador.StartsWith("C"))
                    .Sum(c => c.Importe);

        /// <summary>
        /// Neto a cobrar = remunerativos + no remunerativos - descuentos
        /// </summary>
        public decimal TotalNeto => TotalCreditos + TotalDebitos;

        private string TraducirUnidad(string unidad)
        {
            return unidad?.Trim() switch
            {
                "A" => "Año",
                "M" => "Mes",
                "Q" => "Quincena",
                "S" => "Semana",
                "D" => "Días",
                "H" => "Horas",
                _ => unidad ?? ""
            };
        }
    }
}