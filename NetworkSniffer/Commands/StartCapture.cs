using NetworkSniffer.ViewModels;
using System;
using System.Windows.Input;

namespace NetworkSniffer.Commands
{
    internal class StartCapture : ICommand
    {
        private MainViewModel viewModel;

        public StartCapture(MainViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            throw new NotImplementedException();
        }

        public void Execute(object? parameter)
        {
            Console.WriteLine("Test");
            throw new NotImplementedException();
        }
    }
}