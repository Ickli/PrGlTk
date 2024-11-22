using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

public struct Matrix {
    public Matrix4 matrix;
    public Vector3 pos;
    public Vector3 scale;
    public Vector3 angle;

    public Matrix(Vector3? pos,
        Vector3? scale = null,
        Vector3? angle = null
    ) {
        this.pos = pos ?? new Vector3(0,0,0);
        this.scale = scale ?? new Vector3(1,1,1);
        this.angle = angle ?? new Vector3(0,0,0);
        Recompute();
    }

    public void Recompute() {
        matrix = 
            Matrix4.CreateScale(scale)
//            * Matrix4.CreateRotation()
            * Matrix4.CreateTranslation(pos);
//            * Matrix4.CreateScale(scale);
    }
}
