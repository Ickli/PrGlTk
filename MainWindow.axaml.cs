using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System;

namespace _5pr;

public partial class MainWindow : Window
{
    /* Defined in axaml */
    /* MainOpenGlControl openGlControl */
    /* StackPanel modelPanels */

    public static Dictionary<string, Func<Model>> modelConstructors = new Dictionary<string, Func<Model>>{
        {"sphere", Model.Sphere},
        {"pyramid", Model.Pyramid},
        {"cube", Model.Cube},
    };

    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new MainWindowViewModel(this);
    }

    public void AddModel(object nameObj) {
        string name = nameObj.ToString()!;
        Console.WriteLine("Adding {0}", name);
        foreach(var nameCtor in modelConstructors) {
            if(nameCtor.Key == name) {
                //openGlControl.modelsMutex.WaitOne();
                openGlControl.newModelTypeName = name;
                openGlControl.IsModelAdded = true;
                //openGlControl.modelsMutex.ReleaseMutex();
                modelPanels.Children.Add(CreateModelPanel(nameCtor.Key));
                return;
            }
        }
        throw new Exception($"MainWindow.AddModel: {name} is not in nameCtor dict");
    }

    public void DeleteModel(int id) {
        modelPanels.Children.RemoveAt(id);
        openGlControl.DeleteModel(id);
        RecalculatePanelIds();
    }

    public void SelectModel(int id) {
        openGlControl.SelectModel(id);
    }

    static TextBlock? textNameSelected = null;

    public Panel CreateModelPanel(string name) {
        int id = modelPanels.Children.Count;
        StackPanel panel = new();
        TextBlock textId = new();
        TextBlock textName = new();
        textId.Text = id.ToString();
        textId.Margin = new Thickness(5);
        textId.Name = "id";
        textName.Text = name;

        Button chooseBtn = new();
        chooseBtn.Content = "Choose";
        chooseBtn.Click += (object? sender, RoutedEventArgs e) => {
            if(textNameSelected != null) {
                textNameSelected.FontWeight = FontWeight.Normal;
            }
            textName.FontWeight = FontWeight.Bold;
            SelectModel(Int32.Parse(textId.Text));
            textNameSelected = textName;
        };

        Button deleteBtn = new();
        deleteBtn.Content = "Delete";
        deleteBtn.Click += (object? sender, RoutedEventArgs e) => {
            if(textNameSelected == textName) {
                textNameSelected = null;
            }
            DeleteModel(Int32.Parse(textId.Text));
        };

        panel.Children.Add(textId);
        panel.Children.Add(textName);
        panel.Children.Add(chooseBtn);
        panel.Children.Add(deleteBtn);

        panel.Orientation = Orientation.Horizontal;

        return panel;
    }

    public async void OnSaveButtonClick(object sender, RoutedEventArgs args) {
        StringInputDialog inputDialog = new("Path to file: ");
        string path = await inputDialog.ShowDialog<string>(this);
        SaveImage(path);
    }

    private void SaveImage(string path) {
        try {
            Console.WriteLine("SaveImage: Rendering to bitmap");
            Bitmap bmap = openGlControl.RenderToBitmap();
            Console.WriteLine("SaveImage: Writing to file \"{0}\"", path);
            FileStream stream = new(path, FileMode.Create);
            bmap.Save(stream); // Saves in png format, according to Avalonia specs
            stream.Close();
        } catch(Exception e) {
            MsgDialog errDialog = new(e.ToString());
            errDialog.ShowDialog(this);
            return;
        }
        Console.WriteLine("SaveImage: Done");
        MsgDialog msg = new($"\"{path}\" saved successfully");
        msg.ShowDialog(this);
    }

    private void RecalculatePanelIds() {
        for(int i = 0; i < modelPanels.Children.Count; i++) {
            StackPanel panel = (StackPanel)modelPanels.Children[i];

            foreach(var child in panel.Children) {
                if(child.Name == "id") {
                    ((TextBlock)child).Text = i.ToString();
                    break;
                }
            }
        }
    }
}
