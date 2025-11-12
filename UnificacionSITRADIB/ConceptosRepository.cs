using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnificacionSITRADIB.Models;

namespace UnificacionSITRADIB
{
    public static class ConceptosRepository
    {
        public static Dictionary<string, ConceptoAFIP> Conceptos { get; private set; }

        public static void Cargar(string path = "Datos/conceptos_afip.json")
        {
            if (!File.Exists(path))
            {
                Conceptos = new Dictionary<string, ConceptoAFIP>();
                return;
            }

            var json = File.ReadAllText(path);
            var lista = JsonSerializer.Deserialize<List<ConceptoAFIP>>(json);
            Conceptos = lista.ToDictionary(c => c.Codigo, c => c);
        }

        public static ConceptoAFIP Obtener(string codigo)
        {
            if (Conceptos.TryGetValue(codigo, out var concepto))
                return concepto;
            return new ConceptoAFIP { Codigo = codigo, Descripcion = "Código no reconocido", Tipo = "Desconocido" };
        }
    }
}
