using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;

namespace UnificacionSITRADIB
{
    public partial class MainWindow : Window
    {
        private List<string> registros = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnSeleccionarArchivos_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                registros.Clear();

                foreach (var archivo in openFileDialog.FileNames)
                {
                    var lineas = File.ReadAllLines(archivo, Encoding.UTF8);
                    registros.AddRange(lineas);
                }

                txtCantidadArchivos.Text = $"Archivos seleccionados: {openFileDialog.FileNames.Length}";
                CargarGrilla();
            }
        }

        private void CargarGrilla()
        {
            // Para simplificar, mostramos cada registro como string plano
            dataGridRegistros.ItemsSource = registros.Select((linea, index) => new { Nro = index + 1, Registro = linea }).ToList();
        }

        private void BtnUnificar_Click(object sender, RoutedEventArgs e)
        {
            if (registros.Count == 0)
            {
                MessageBox.Show("No hay registros para unificar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivo de texto (*.txt)|*.txt",
                FileName = "Archivo_Unificado.txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllLines(saveFileDialog.FileName, registros, Encoding.UTF8);
                MessageBox.Show("Archivo unificado guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSalir_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
