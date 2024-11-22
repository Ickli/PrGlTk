using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

public class Model {
    private List<ParameterLine> lines = new();
    private ObjModel obj;

    public ref Matrix matrix => ref obj.matrix;

    public static Model Pyramid() {
        var raw = new ObjModel("cube.obj");
        return new Model(raw, ParameterLine.ForPyramid());
    }

    public static Model Sphere() {
        var raw = new ObjModel("sphere.obj");
        return new Model(raw, ParameterLine.ForSphere());
    }

    public static Model Cube() {
        var raw = new ObjModel("cube.obj");
        return new Model(raw, ParameterLine.ForCube());
    }

    public static Model Line() {
        var raw = new ObjModel("line.obj");
        return new Model(raw, new List<ParameterLine>());
    }

    public void Draw() {
        obj.Draw();
    }

    public void DrawBox() {
        obj.DrawBox();
    }

    public void DrawLines(Shader shader) {
        foreach(var line in lines) {
            shader.SetModelMatrix(
                    ref line.lineModel.matrix.matrix
            );
            line.Draw();
        }
    }

    public void Move(Vector3 vec) {
        obj.matrix.pos += vec;
        obj.matrix.Recompute();

        obj.box.ApplyToStartVertices(obj.matrix.matrix);
    }

    public void Scale(Vector3 vec) {
        obj.matrix.scale += vec;
        obj.matrix.Recompute();

        obj.box.ApplyToStartVertices(obj.matrix.matrix);
    }

    public bool IntersectsLine(Vector3 pos, Vector3 dirNormalized, out bool anyOnSameSide) {
        return obj.box.IntersectsLine(pos, dirNormalized, out anyOnSameSide);
    }

    private Model(ObjModel model, List<ParameterLine> lines) {
        this.obj = model;
        this.lines = lines;

        Console.WriteLine("Final rect");
        for(int i = 0; i < obj.box.vertices.Length; i++) {
            Console.WriteLine(obj.box.vertices[i]);
        }

        foreach(var line in lines) {
            line.Bind(this, this.obj);
        }
    }
}
