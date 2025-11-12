using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using UnificacionSITRADIB.Helpers;

namespace UnificacionSITRADIB.ViewModels
{
    public class LibroSueldosViewModel : ViewModelBase
    {
        // Propiedades principales
        public ObservableCollection<string> ArchivosSeleccionados { get; } = new();
        public ObservableCollection<RegistroLSD> Registros { get; } = new();
        public ObservableCollection<EmpleadoViewModel> Empleados { get; } = new();
        public ObservableCollection<RegistroLSD> DetalleEmpleado { get; } = new();

        private bool _edicionHabilitada;
        public bool EdicionHabilitada
        {
            get => _edicionHabilitada;
            set => SetProperty(ref _edicionHabilitada, value);
        }

        private RegistroLSD? _registroSeleccionado;
        public RegistroLSD? RegistroSeleccionado
        {
            get => _registroSeleccionado;
            set => SetProperty(ref _registroSeleccionado, value);
        }

        public string TituloDetalleEmpleado => EmpleadoSeleccionado != null
    ? $"Detalle del empleado {EmpleadoSeleccionado.Dni}"
    : "Detalle del empleado";

        private EmpleadoViewModel _empleadoSeleccionado;
        public EmpleadoViewModel EmpleadoSeleccionado
        {
            get => _empleadoSeleccionado;
            set
            {
                _empleadoSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DetalleEmpleado));
                OnPropertyChanged(nameof(TituloDetalleEmpleado));
            }
        }

        private int _totalEmpleados;
        public int TotalEmpleados
        {
            get => _totalEmpleados;
            set => SetProperty(ref _totalEmpleados, value);
        }

        private decimal _totalSueldos;
        public decimal TotalSueldos
        {
            get => _totalSueldos;
            set => SetProperty(ref _totalSueldos, value);
        }

        // Datos de la empresa
        private string _cuit;
        public string Cuit
        {
            get => _cuit;
            set => SetProperty(ref _cuit, value);
        }

        private string _razonSocial;
        public string RazonSocial
        {
            get => _razonSocial;
            set => SetProperty(ref _razonSocial, value);
        }

        private string _periodo;
        public string Periodo
        {
            get => _periodo;
            set => SetProperty(ref _periodo, value);
        }

        private bool _soloLectura = true;
        public bool SoloLectura
        {
            get => _soloLectura;
            set => SetProperty(ref _soloLectura, value);
        }

        // Comandos
        public ICommand SeleccionarArchivosCommand { get; }
        public ICommand HabilitarEdicionCommand { get; }
        public ICommand EliminarRegistroCommand { get; }
        public ICommand ExportarCommand { get; }


        public LibroSueldosViewModel()
        {
            // Inicializar comandos
            SeleccionarArchivosCommand = new DelegateCommand(SeleccionarArchivos);
            HabilitarEdicionCommand = new DelegateCommand(() => SoloLectura = false);
            EliminarRegistroCommand = new DelegateCommand(EliminarRegistro, () => RegistroSeleccionado != null);
            ExportarCommand = new DelegateCommand(ExportarArchivo);
        }
        private void ExportarArchivo()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Archivos TXT (*.txt)|*.txt",
                FileName = $"LibroSueldos_{Cuit}_{Periodo}.txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                File.WriteAllLines(saveDialog.FileName, Registros.Select(r => r.Texto));
            }
        }


        // Seleccionar archivos desde el explorador de archivos
        private void SeleccionarArchivos()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos TXT (*.txt)|*.txt",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ArchivosSeleccionados.Clear();
                Registros.Clear();
                Empleados.Clear();
                EmpleadoSeleccionado = null;
                Cuit = RazonSocial = Periodo = null;

                foreach (var archivo in openFileDialog.FileNames)
                    ArchivosSeleccionados.Add(archivo);

                CargarArchivos(openFileDialog.FileNames);
            }
        }

        // Cargar los archivos y procesar cada registro
        private void CargarArchivos(string[] archivos)
        {
            string cuitReferencia = null;
            string cuilActual = null;
            var empleadosDict = new Dictionary<string, List<RegistroLSD>>();

            Registros.Clear();
            Empleados.Clear();

            foreach (var archivo in archivos)
            {
                var lineas = File.ReadAllLines(archivo);
                foreach (var linea in lineas)
                {
                    if (string.IsNullOrWhiteSpace(linea) || linea.Length < 2) continue;

                    var tipo = linea.Substring(0, 2);
                    var registro = new RegistroLSD(linea);

                    // 01 → datos de la empresa
                    if (tipo == "01")
                    {
                        if (linea.Length < 21) continue;

                        var cuit = linea.Substring(2, 11);
                        DateTime periodo = DateTime.ParseExact(linea.Substring(15, 6), "yyyyMM", null);

                        if (cuitReferencia == null)
                        {
                            cuitReferencia = cuit;
                            Cuit = cuit;
                            Periodo = periodo.ToString("MMM-yyyy", new CultureInfo("es-AR"));
                        }
                        else if (cuit != cuitReferencia)
                        {
                            break; // Ignorar archivo con CUIT diferente
                        }

                        Registros.Add(registro);
                        continue;
                    }

                    // 02 → empleado (datos generales)
                    if (tipo == "02")
                    {
                        if (linea.Length < 13) continue;

                        // CUIL está en posiciones 3-13 (índice 2-12)
                        cuilActual = linea.Substring(2, 11);

                        if (!empleadosDict.ContainsKey(cuilActual))
                        {
                            empleadosDict[cuilActual] = new List<RegistroLSD>();
                        }
                        empleadosDict[cuilActual].Add(registro);
                        Registros.Add(registro);
                        continue;
                    }

                    // 03, 04 → detalles del empleado actual
                    if ((tipo == "03" || tipo == "04") && !string.IsNullOrEmpty(cuilActual))
                    {
                        if (!empleadosDict.ContainsKey(cuilActual))
                        {
                            empleadosDict[cuilActual] = new List<RegistroLSD>();
                        }
                        empleadosDict[cuilActual].Add(registro);
                    }

                    Registros.Add(registro);
                }
            }

            // Crear ViewModels de empleados
            foreach (var kvp in empleadosDict.OrderBy(x => x.Key))
            {
                var cuil = kvp.Key;
                var registros = kvp.Value;

                // Buscar nombre en registro tipo 02
                var reg02 = registros.FirstOrDefault(r => r.Tipo == "02");
                string nombre = "(Nombre no disponible)";

                if (reg02 != null && reg02.Texto.Length >= 63)
                {
                    // Apellido y Nombres están en posiciones 24-63 (40 caracteres)
                    nombre = reg02.Texto.Substring(23, 40).Trim();
                }

                Empleados.Add(new EmpleadoViewModel
                {
                    Dni = cuil,
                    Nombre = nombre,
                    Registros = registros
                });
            }
            //AgruparPorEmpleado();
            ActualizarTotales();
        }


        // Agrupar los registros por empleado usando el CUIL actual
        private void AgruparPorEmpleado()
        {
            // agrupamos según el CUIL (posiciones 2–12 luego de haberlo inyectado en CargarArchivos)
            var grupos = Registros
                .Where(r => new[] { "03", "04", "05", "06" }.Contains(r.Tipo))
                .GroupBy(r => r.Texto.Substring(2, 11));

            Empleados.Clear();

            foreach (var grupo in grupos)
            {
                string cuil = grupo.Key;
                string nombre = grupo
                    .Where(r => r.Tipo == "03")
                    .Select(r => r.Texto.Substring(13, 30).Trim())
                    .FirstOrDefault() ?? "(Nombre no disponible)";

                Empleados.Add(new EmpleadoViewModel
                {
                    Dni = cuil,
                    Nombre = nombre,
                    Registros = grupo.ToList()
                });
            }

            // Refrescar el total de empleados
            TotalEmpleados = Empleados.Count;
        }


        // Eliminar un registro seleccionado
        private void EliminarRegistro()
        {
            if (RegistroSeleccionado != null)
            {
                DetalleEmpleado.Remove(RegistroSeleccionado);
            }
        }

        private void ActualizarTotales()
        {
            TotalEmpleados = Empleados.Count;
            TotalSueldos = Empleados.Sum(emp => emp.TotalNeto);
        }
    }
}
