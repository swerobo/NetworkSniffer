using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace NetworkSniffer.ViewModels
{
    public class HelpViewModel : BaseViewModel
    {
        #region Constructors
        /// <summary>
        /// Initializes new instance of the HelpViewModel class
        /// </summary>
        public HelpViewModel()
        {
            GoToSourceRepository = new RelayCommand(() => GoToSourceRepositoryExecute());

            try
            {
                MemoryStream memoryResStream = new MemoryStream(Encoding.Default.GetBytes(HelpResource.Help));

                HelpTextBox = new RichTextBox();
                HelpTextBox.IsReadOnly = true;
                HelpTextBox.Padding = new Thickness(5);
                HelpTextBox.SelectAll();
                HelpTextBox.Selection.Load(memoryResStream, DataFormats.Rtf);
            }
            catch
            {
                return;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Used to store help document
        /// </summary>
        public RichTextBox HelpTextBox { get; private set; }

        public ICommand GoToSourceRepository { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Opens source code repository on GitHub in a default browser
        /// </summary>
        private void GoToSourceRepositoryExecute()
        {
            Process.Start("https://github.com/gcupko00/NetworkSniffer");
        }
        #endregion
    }
}
