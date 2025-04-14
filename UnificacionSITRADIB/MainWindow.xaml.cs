using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using UnificacionSITRADIB.ViewModels;

namespace UnificacionSITRADIB
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new LibroSueldosViewModel();
        }
    }
}
