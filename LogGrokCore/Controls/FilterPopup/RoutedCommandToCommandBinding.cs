using System.Windows.Input;

namespace LogGrokCore.Controls.FilterPopup
{
    public class RoutedCommandToCommandBinding 
    {
        public RoutedCommandToCommandBinding(RoutedCommand routedCommand, ICommand command)
        {
            RoutedCommand = routedCommand;
            Command = command;
        }

        public RoutedCommand RoutedCommand { get;  }

        public ICommand Command { get; }
    }
}
