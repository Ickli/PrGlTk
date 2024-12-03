using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System.IO;
using System;

namespace _5pr;

public partial class MsgDialog : Window
{
    public MsgDialog() {
        InitializeComponent();
        this.DataContext = this;

    }

    public MsgDialog(string msg): this() {
        textBlock.Text = msg;
    }
}
