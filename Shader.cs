using System;
using System.IO;
using OpenTK.Mathematics;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Shader {
    public int Handle;

    public int modelMatrixLocation;
    public int viewMatrixLocation;
    public int projectionMatrixLocation;

    public Shader(string vPath, string fPath) {
        var vSource = File.ReadAllText(vPath);
        var fSource = File.ReadAllText(fPath);

        var vShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vShader, vSource);
        GL.CompileShader(vShader);
        GL.GetShader(vShader, ShaderParameter.CompileStatus, out int success);
        if(success == 0) {
            string log = GL.GetShaderInfoLog(vShader);
            Console.WriteLine(log);
        }

        var fShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fShader, fSource);
        GL.CompileShader(fShader);
        GL.GetShader(fShader, ShaderParameter.CompileStatus, out success);
        if(success == 0) {
            string log = GL.GetShaderInfoLog(fShader);
            Console.WriteLine(log);
        }

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vShader);
        GL.AttachShader(Handle, fShader);

        GL.LinkProgram(Handle);

        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out success);
        if(success == 0) {
            string log = GL.GetProgramInfoLog(Handle);
            Console.WriteLine(log);
        }

        GL.DetachShader(Handle, vShader);
        GL.DetachShader(Handle, fShader);
        GL.DeleteShader(vShader);
        GL.DeleteShader(fShader);

        modelMatrixLocation = GL.GetUniformLocation(Handle, "model");
        viewMatrixLocation = GL.GetUniformLocation(Handle, "view");
        projectionMatrixLocation = GL.GetUniformLocation(Handle, "projection");
    }

    public void Use(ref Matrix4 view, ref Matrix4 projection) {
        GL.UseProgram(Handle);
        GL.UniformMatrix4(GL.GetUniformLocation(Handle, "projection"), false, ref projection);
        GL.UniformMatrix4(GL.GetUniformLocation(Handle, "view"), false, ref view);
    }

    public void SetModelMatrix(ref Matrix4 model) {
        GL.UniformMatrix4(modelMatrixLocation, false, ref model);
    }

    private bool disposedValue = false;

    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            GLFuncs.DeleteProgram(Handle);

            disposedValue = true;
        }
    }

    ~Shader() {
        if (disposedValue == false) {
            Console.WriteLine("GPU Resource leak! Did you forget to call Dispose()?");
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
