using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System.IO;
using System;

namespace _5pr;

public partial class StringInputDialog : Window
{
    public StringInputDialog() {
        InitializeComponent();
        this.DataContext = this;
    }
    public StringInputDialog(string msg): this() {
        textBlock.Text = msg;
    }

    public void OnSaveClick(object sender, RoutedEventArgs args) {
        Close(input.Text);
    }
}
