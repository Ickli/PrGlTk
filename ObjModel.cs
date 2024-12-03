using System;
using System.IO;
using System.Collections.Generic;
using ObjLoader.Loader.Loaders;
using ObjLoader.Loader.Data.VertexData;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

// TODO:
// Use one layout format for many VBOS
// Or store every object in one VBO and call MultiDraw
public class ObjModel: IDisposable {
    static Matrix4 identityMatrix = Matrix4.Identity;
    static ObjLoaderFactory oFactory = new ObjLoaderFactory();

    int VAO;
    int boxVAO;
    int VBO;
    int boxVBO;
    bool disposedValue = false;

    public Box box;

    /* TODO: maybe rotation */
    public Matrix matrix;
    private int elemLength;

    public Box GetBox() {
        return box;
    }

    public ObjModel(string filename) {
        Console.WriteLine("Model \"{0}\" ctor: start", filename);
        var f = File.OpenRead(filename);
        var oLoader = oFactory.Create();
        var info = oLoader.Load(f);

        float[] coords = new float[info.Vertices.Count * 3];
        Vector3[] normals = new Vector3[info.Vertices.Count];
        elemLength = info.Groups[0].Faces.Count * GLFuncs.ElementPerFace;
        uint[] elements = new uint[elemLength];

        PopulateFrom(info, coords, normals, elements);

        Console.WriteLine("Model \"{0}\" ctor: creating GL buffers", filename);
        VAO = GLFuncs.CreateAndPopulateBuffers(out VBO, coords, normals, elements);

        matrix = new Matrix(new Vector3(0,0,0));

        Console.WriteLine("Model \"{0}\" ctor: compute OBB", filename);
        box = new Box(coords, normals, elements);
        boxVAO = GLFuncs.CreateAndPopulateBuffers(out boxVBO, box.GetCoords(), null, Box.GetElements());
    }

    public void Draw() {
        GLFuncs.DrawElements(VAO, elemLength);
    }

    public void DrawBox() {
        GLFuncs.DrawElements(boxVAO, Box.ElementLength, PrimitiveType.Lines);
    }

    private void PopulateFrom(LoadResult info, float[] coords, Vector3[] normals, uint[] elements) {
        for(int i = 0; i < info.Vertices.Count; i++) {
            var v = info.Vertices[i];
            coords[i*3 + 0] = v.X;
            coords[i*3 + 1] = v.Y;
            coords[i*3 + 2] = v.Z;
        }

        for(int faceIndex = 0; faceIndex < info.Groups[0].Faces.Count; faceIndex++) {
            var f = info.Groups[0].Faces[faceIndex];
            for(var i = 0; i < f.Count; i++) {
                var vertexIndex = f[i].VertexIndex - 1;
                var normalIndex = f[i].NormalIndex - 1;

                elements[faceIndex*GLFuncs.ElementPerFace + i] = (uint)vertexIndex;
                var normal = ObjNormalToVector3(info.Normals[normalIndex]);
                normals[vertexIndex] = (normals[vertexIndex] + normal).Normalized();
            }
        }
    }

    private Vector3 ObjNormalToVector3(in Normal n) => new Vector3(n.X, n.Y, n.Z);

    protected virtual void Dispose(bool disposing) {
        if(!disposedValue) {
            GLFuncs.DeleteVertexArray(VAO);
            GLFuncs.DeleteVertexArray(boxVAO);
            GLFuncs.DeleteBuffer(VBO);
            GLFuncs.DeleteBuffer(boxVBO);
            disposedValue = true;
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ObjModel() {
        if (disposedValue == false) {
            Console.WriteLine("ObjModel: GPU Resource leak! Did you forget to call Dispose()?");
        }
    }
}
