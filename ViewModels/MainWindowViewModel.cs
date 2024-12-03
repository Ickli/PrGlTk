using System;

namespace _5pr;

public class MainWindowViewModel {
    private MainWindow window;
    public string Greeting => "HELLO!!!";

    public MainWindowViewModel(MainWindow window) {
        this.window = window;
    } 

    public void AddModel(object nameObj) {
        window.AddModel(nameObj);
    }

    public void DeleteModel(int id) {
        window.DeleteModel(id);
    }

    public void SelectModel(int id) {
        window.SelectModel(id);
    }
}
