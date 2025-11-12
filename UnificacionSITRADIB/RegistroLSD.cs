using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
                        "02" => $"CUIL: {Texto.Substring(2, 11)} - Nombre: {Texto.Substring(23, 40).Trim()}",
                        "03" => $"Concepto: {Texto.Substring(19, 6)} - Importe: ${decimal.Parse(Texto.Substring(41, 13)) / 100m:#,##0.00}",
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
            Tipo = linea.Length >= 2 ? linea.Substring(0, 2) : "00";
        }
    }

    public class Registro01 : RegistroLSD
    {
        public string CUIT { get; set; }

        public Registro01(string linea) : base(linea)
        {
            CUIT = linea.Substring(2, 11);
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
        public string Concepto => ConceptosRepository.Obtener(Codigo).Descripcion;
        public decimal Cantidad { get; set; }
        public string Unidad { get; set; }
        public decimal Importe { get; set; }
        public string Indicador { get; set; } // 'C' o 'D'
        public DateTime PeriodoAjusteRetroactivo { get; set; }
        public string PeriodoFormateado => PeriodoAjusteRetroactivo != DateTime.MinValue
            ? PeriodoAjusteRetroactivo.ToString("MMM-yyyy", new CultureInfo("es-AR"))
            : "";
    }
}