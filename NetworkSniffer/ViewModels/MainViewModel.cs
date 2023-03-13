using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSniffer.ViewModels
{
    internal class MainViewModel : BaseViewModel
    {
        private BaseViewModel _selectedViewModel = new SnifferViewModel();
        public BaseViewModel SelectedViewModel
        {
            get { return _selectedViewModel; }
            set
            {
                _selectedViewModel = value;
                OnPropertyChanged(nameof(SelectedViewModel));
            }
        }
    }
}
