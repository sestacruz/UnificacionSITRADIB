using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnificacionSITRADIB
{
    public class RegistroLSD
    {
        public string Tipo { get; set; }
        public string Texto { get; set; }
        public string Detalle
        {
            get
            {
                try
                {
                    return Tipo switch
                    {
                        "03" => $"DNI: {Texto.Substring(2, 11)} - Nombre: {Texto.Substring(13, 30).Trim()}",
                        "04" => $"Concepto: {Texto.Substring(13, 6)} - Importe: {decimal.Parse(Texto.Substring(19, 13)) / 100m:#,##0.00}",
                        "05" => $"SAC o Vacaciones: {Texto.Substring(13, 6)} - Importe: {decimal.Parse(Texto.Substring(19, 13)) / 100m:#,##0.00}",
                        "06" => $"Horas extras/No remunerativo: {Texto.Substring(13, 6)} - Importe: {decimal.Parse(Texto.Substring(19, 13)) / 100m:#,##0.00}",
                        _ => Texto
                    };
                }
                catch
                {
                    return "(Error al interpretar)";
                }
            }
        }

        public RegistroLSD(string linea)
        {
            Texto = linea;
            Tipo = linea.Substring(0, 2);
        }
    }

    public class Registro01 : RegistroLSD
    {
        public string CUIT { get; set; }

        public Registro01(string linea) : base(linea)
        {
            CUIT = linea.Substring(2, 11); // Posiciones 3-13 (CUIT)
        }
    }

    public class RegistroGenerico : RegistroLSD
    {
        public RegistroGenerico(string linea) : base(linea) { }
    }

    public class Empleado
    {
        public string CUIL { get; set; }
        public string Nombre { get; set; }
    }
    public class ConceptoLiquidado
    {
        public string Codigo { get; set; }
        public string Concepto => ConceptosARCA.Descripciones.TryGetValue(Codigo, out var descripcion)
                    ? descripcion
                    : "Código no reconocido";
        public decimal Cantidad { get; set; }
        public string Unidad { get; set; }
        public decimal Importe { get; set; }
        public string Indicador { get; set; } // 'C' o 'D'
        public DateTime PeriodoAjusteRetroactivo { get; set; }
        public string PeriodoFormateado => PeriodoAjusteRetroactivo.ToString("MMM-yyyy", new CultureInfo("es-AR"));
    }


}
