using System;
using System.Drawing;
using System.Collections.Generic;
using ObjLoader.Loader.Loaders;
using ObjLoader.Loader.Data.VertexData;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

public static class GLFuncs {
    public static readonly int FPV = 6; // FloatPerVertex
    public static readonly int ElementPerFace = 3;

    public static int CreateAndPopulateBuffers(out int VBO, float[] coords, Vector3[]? normals = null, uint[]? elements = null) {
//        GL.GetInteger(GetPName.ArrayBufferBinding, out int oldVAO);

        int VAO;
        if(normals != null) {
            float[] coordNormals = GetCoordNormals(coords, normals);
            VAO = CreateAndPopulateBuffers_WithNormals(out VBO, coordNormals, elements);
        } else {
            VAO = CreateAndPopulateBuffers_WithoutNormals(out VBO, coords, elements);
        }

//        GL.BindVertexArray(oldVAO);
        return VAO;
    }

    public static void DrawArrays(int VAO, int count, PrimitiveType type = PrimitiveType.Triangles) {
        GL.BindVertexArray(VAO);
        GL.DrawArrays(type, 0, count);
    }

    public static void DrawElements(int VAO, int elemLength, PrimitiveType type = PrimitiveType.Triangles) {
        GL.BindVertexArray(VAO);
        GL.DrawElements(type, elemLength, DrawElementsType.UnsignedInt, 0);
    }

    public static void BufferSubData<T3>(BufferTarget target, IntPtr offset, IntPtr size, T3[] data)
    where T3: struct {
        GL.BufferSubData(target, offset, size, data);
    }

    public static void DeleteBuffer(int VBO) {
        GL.DeleteBuffer(VBO);
    }

    public static void DeleteVertexArray(int VAO) {
        GL.DeleteVertexArray(VAO);
    }

    public static void DeleteProgram(int handle) {
        GL.DeleteProgram(handle);
    }

    private static unsafe void WriteWhite(byte* ptr, int size) {
        for(int i = 0; i < size; i++) {
            ptr[i] = 0xFF;
        }
    }

    public static void ReadPixels(Rectangle rect, IntPtr dest) {
        GL.Finish();
        GL.Flush();
        GL.ReadBuffer(ReadBufferMode.Front);
        unsafe {
            WriteWhite((byte*)dest.ToPointer(), rect.Width*rect.Height*4);
        }
        _5pr.Controls.MainOpenGlControl.modelsMutex.WaitOne();
        GL.ReadPixels(rect.X, rect.Y, rect.Width, rect.Height, PixelFormat.Rgba, PixelType.UnsignedByte, dest);
        GL.Finish();
        _5pr.Controls.MainOpenGlControl.modelsMutex.ReleaseMutex();
        Console.WriteLine($"!!! {GL.GetError()}");
    }

    private static int CreateAndPopulateBuffers_WithNormals(out int VBO, float[] coordNormals, uint[]? elements) {
        VBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, coordNormals.Length * sizeof(float), coordNormals, BufferUsageHint.DynamicDraw);

        int VAO = GL.GenVertexArray();
        GL.BindVertexArray(VAO);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float)*6, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, sizeof(float)*6, sizeof(float)*3);
        GL.EnableVertexAttribArray(1);

        if(elements == null) {
            return VAO;
        }

        int EBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, elements!.Length * sizeof(uint), elements!, BufferUsageHint.StaticDraw);

        return VAO;
    }

    private static int CreateAndPopulateBuffers_WithoutNormals(out int VBO, float[] coords, uint[]? elements) {
        VBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, coords.Length * sizeof(float), coords, BufferUsageHint.DynamicDraw);

        int VAO = GL.GenVertexArray();
        Console.WriteLine($"CreateAndPopulateBuffers_WithoutNormals VAO = {VAO}");
        GL.BindVertexArray(VAO);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float)*3, 0);
        GL.EnableVertexAttribArray(0);
        if(elements == null) {
            return VAO;
        }

        int EBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, elements!.Length * sizeof(uint), elements!, BufferUsageHint.StaticDraw);

        return VAO;
    }

    private static float[] GetCoordNormals(float[] coords, Vector3[] normals) {
        int vertexCount = coords.Length / 3;
        int length =  vertexCount * FPV;
        float[] coordNormals = new float[length];

        for(int i = 0; i < vertexCount; i += 1) {
            coordNormals[i*FPV + 0] = coords[i*3 + 0];
            coordNormals[i*FPV + 1] = coords[i*3 + 1];
            coordNormals[i*FPV + 2] = coords[i*3 + 2];
        }

        for(int i = 0; i < vertexCount; i += 1) {
            coordNormals[i*FPV + 3] = normals[i].X;
            coordNormals[i*FPV + 4] = normals[i].Y;
            coordNormals[i*FPV + 5] = normals[i].Z;
        }
        return coordNormals;
    }
}
