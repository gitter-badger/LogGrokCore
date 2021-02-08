using System.Windows;
using System.Windows.Input;

namespace LogGrokCore
{
    public static class RoutedCommands
    {
        public static readonly RoutedCommand Cancel = new RoutedUICommand(
            "Cancel", "Cancel", typeof(UIElement), 
            new InputGestureCollection { new KeyGesture(Key.Escape) });
        
        public static readonly RoutedCommand CopyToClipboard = new RoutedUICommand(
            ApplicationCommands.Copy.Text,
            "CopyToClipboard", ApplicationCommands.Copy.OwnerType, ApplicationCommands.Copy.InputGestures);
    }
}
