using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            foreach (var archivo in archivos)
            {
                var lineas = File.ReadAllLines(archivo);
                foreach (var linea in lineas)
                {
                    var registro = new RegistroLSD(linea);

                    // Proceso específico para tipo "01" (datos de la empresa)
                    if (registro.Tipo == "01")
                    {
                        var cuit = linea.Substring(2, 11);
                        DateTime periodo = DateTime.ParseExact(linea.Substring(15, 6),"yyyyMM",null);

                        if (cuitReferencia == null)
                        {
                            cuitReferencia = cuit;
                            Cuit = cuit;
                            Periodo = periodo.ToString("MMM-yyyy");
                        }
                        else if (cuit != cuitReferencia)
                        {
                            // Si el CUIT no coincide, se ignora este archivo
                            break;
                        }
                    }

                    // Agregar el registro procesado
                    Registros.Add(registro);
                }
            }

            // Agrupar los registros por empleado
            AgruparPorEmpleado();
            ActualizarTotales();
        }

        // Agrupar los registros de acuerdo con el empleado (por DNI)
        private void AgruparPorEmpleado()
        {
            var grupos = Registros
                .Where(r => r.Tipo == "03") // Tipo 03 es para los datos del empleado
                .GroupBy(r => r.Texto.Substring(2, 11)); // Agrupar por el DNI o identificador del empleado

            foreach (var grupo in grupos)
            {
                var dni = grupo.Key;
                var registrosEmpleado = Registros
                    .Where(r => new[] { "03", "04", "05", "06" }.Contains(r.Tipo)
                             && r.Texto.Substring(2, 11) == dni)
                    .ToList();

                Empleados.Add(new EmpleadoViewModel
                {
                    Dni = dni,
                    Registros = registrosEmpleado
                });
            }
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

            // Sueldos = registros tipo 04 con valores numéricos
            TotalSueldos = Registros
                .Where(r => r.Tipo == "04")
                .Select(r =>
                {
                    if (decimal.TryParse(r.Texto.Substring(161, 15), out var monto)) // adaptá si cambia la posición
                        return monto / 100; // suponiendo dos decimales
                    return 0;
                })
                .Sum();
        }
    }
}
