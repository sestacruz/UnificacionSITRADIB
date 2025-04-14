using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UnificacionSITRADIB.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }

        protected bool SetProperty<T>(ref T field, T nuevoValor, [CallerMemberName] string propiedad = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, nuevoValor))
                return false;

            field = nuevoValor;
            OnPropertyChanged(propiedad);
            return true;
        }
    }

}
