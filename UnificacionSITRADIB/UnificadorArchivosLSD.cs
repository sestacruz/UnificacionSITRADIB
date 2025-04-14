using System.IO;


namespace UnificacionSITRADIB
{
    public class UnificadorArchivosLSD
    {
        public string CuitEmpresaReferencia { get; private set; }
        public List<RegistroLSD> Registros { get; private set; } = new();
        public List<string> ArchivosDescartados { get; private set; } = new();

        public bool CargarArchivo(string path)
        {
            var lineas = File.ReadAllLines(path);
            if (lineas.Length == 0) return false;

            var primerRegistro = lineas[0];
            if (!primerRegistro.StartsWith("01")) return false;

            var reg01 = new Registro01(primerRegistro);
            if (string.IsNullOrWhiteSpace(CuitEmpresaReferencia))
            {
                CuitEmpresaReferencia = reg01.CUIT;
            }
            else if (reg01.CUIT != CuitEmpresaReferencia)
            {
                ArchivosDescartados.Add(path);
                return false;
            }

            foreach (var linea in lineas)
            {
                var tipo = linea.Substring(0, 2);
                RegistroLSD registro = tipo switch
                {
                    "01" => new Registro01(linea),
                    _ => new RegistroGenerico(linea)
                };
                Registros.Add(registro);
            }

            return true;
        }
    }

}
