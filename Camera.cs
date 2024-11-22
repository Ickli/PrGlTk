using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Camera {
    public Matrix4 view;

    public Camera(Vector3 pos, Vector3 target, Vector3 up) {
        view = Matrix4.LookAt(pos, target, up);
    }

    public void Print() {
        Console.WriteLine(view);
    }

}
