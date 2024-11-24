using Avalonia.Controls;
using Avalonia.Interactivity;
using System.IO;
using System;

namespace _5pr;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new MainWindowViewModel();
    }

    public void ButtonClicked(object source, RoutedEventArgs e) {
        Console.WriteLine("Click!");
    }
}
