using System;
using System.Collections.Generic;
using ObjLoader.Loader.Loaders;
using ObjLoader.Loader.Data.VertexData;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

public class Box {
    private static readonly int VerticesLength = 8;
    private static readonly int NormalsLength = 3;
    private static readonly int ElementPerPlane = GLFuncs.ElementPerFace*2;

    // groups of two planes must be parallel
    // groups of two faces must be on the same plane
    private static readonly uint[] StaticElements = {
        // 0 1
        // 3 2
        //
        // 4 5
        // 7 6

        0, 1, 2, // top
        0, 2, 3,

        4, 5, 6, // bottom
        4, 6, 7,

        0, 1, 5, // back
        0, 4, 5,

        2, 3, 7, // front
        2, 6, 7,

        0, 3, 7, // left
        0, 4, 7,

        1, 2, 6, // right
        1, 5, 6
    };
    public static readonly int ElementLength = StaticElements.Length;
    private Vector3[] startVertices = new Vector3[VerticesLength];
    public Vector3[] vertices = new Vector3[VerticesLength];
    // normals are normalized
    private Vector3[] normals = new Vector3[NormalsLength];
    // projections of vertices on each parallel plane
    private Vector3[] projectedVertices = new Vector3[VerticesLength*NormalsLength];

    public Box(float[] coords, Vector3[] normals, uint[] elements) {
        List<Vector3> hull = new();
        List<Vector3> projections = new();
        Vector3[] curPrism = new Vector3[8];
        double curV;
        Vector3[] minPrism = new Vector3[8];
        double minV = Double.MaxValue;
        Vector3[] curRect = new Vector3[4];

        for(
            var elementIndex = 0;
            elementIndex <= elements.Length - GLFuncs.ElementPerFace;
            elementIndex += GLFuncs.ElementPerFace
        ) {
            // TODO: caching for parallel faces
            //       and create hull only from vertices used in 'elements'
            
            GetHull(
                coords, normals,
                elements[elementIndex + 0],
                elements[elementIndex + 1],
                elements[elementIndex + 2],
                hull,
                projections
            );
            

            float curRectArea = GetMinRect(hull, curRect);
            curV = ExtendMinRect(curRect, curRectArea, coords, normals, curPrism, projections);

            if(curV < minV) {
                minV = curV;
                var tmpPrism = minPrism;
                minPrism = curPrism;
                curPrism = tmpPrism;
            }
        }

        if(Math.Abs(minV - Double.MaxValue) < Double.Epsilon) {
            throw new Exception("Box ctor: couldn't find any prism for model");
        }

        startVertices = minPrism;
        ApplyToStartVertices(Matrix4.Identity);
    }

    public float[] GetCoords() {
        float[] coords = new float[vertices.Length*3];
        for(int i = 0; i < vertices.Length; i++) {
            coords[i*3 + 0] = vertices[i].X;
            coords[i*3 + 1] = vertices[i].Y;
            coords[i*3 + 2] = vertices[i].Z;
        }
        return coords;
    }

    public static uint[] GetElements() => StaticElements;

    public static void GetHull(float[] coords, Vector3[] normals, uint e1, uint e2, uint e3, List<Vector3> hull, List<Vector3> projections) {
        hull.Capacity = 3*100;

        // face's first vector
        var f = new Vector3(
            coords[e1*3 + 0] - coords[e2*3 + 0],
            coords[e1*3 + 1] - coords[e2*3 + 1],
            coords[e1*3 + 2] - coords[e2*3 + 2]
        );
        // face's second vector
        var s = new Vector3(
            coords[e2*3 + 0] - coords[e3*3 + 0],
            coords[e2*3 + 1] - coords[e3*3 + 1],
            coords[e2*3 + 2] - coords[e3*3 + 2]
        );
        // normal unit vector
        var n = Vector3.Cross(f, s).Normalized();
        n.Normalize();

        projections.Clear();
        projections.Capacity = coords.Length / 3;
        for(var i = 0; i < coords.Length; i += 3) {
            projections.Add(ProjectOnPlane(f, n, new Vector3(
                coords[i+0], coords[i+1], coords[i+2]
            )));
        }

        var pointOnHull = projections[FindFarthest(projections)];
        Vector3 endPoint = new(0);
        
        hull.Clear();
        do {
            hull.Add(pointOnHull); // update hull
            endPoint = projections[0]; // take random point from projections
            var pointEndPoint = endPoint - pointOnHull;

            foreach(var proj in projections) {
                var normalDir = Vector3.Dot(Vector3.Cross(pointEndPoint, proj - endPoint), n);
                
                if(endPoint == pointOnHull // if cur best point is hull's end
                || normalDir < 0
                ) {
                    endPoint = proj;
                    pointEndPoint = endPoint - pointOnHull;
                }
            }
            pointOnHull = endPoint;
            
        } while(endPoint != hull[0]);
    }

    public static Vector3 ProjectOnPlane(Vector3 source, Vector3 normalNormalized, Vector3 point) {
        var v = point - source;
        var dist = Vector3.Dot(v, normalNormalized);
        return point - normalNormalized*dist;
    }

    public static Vector3 ProjectOnLine(Vector3 source, Vector3 dirNormalized, Vector3 point) {
        var frac = Vector3.Dot(dirNormalized, point - source);
        return source + dirNormalized*frac;
    }

    public static Vector3 ProjectOnLineWithFrac(Vector3 source, Vector3 dirNormalized, Vector3 point, out float frac) {
        frac = Vector3.Dot(dirNormalized, point - source);
        return source + dirNormalized*frac;
    }

    // Farthest relatively to some dot :)
    public static int FindFarthest(List<Vector3> projections) {
        var unit = projections[1] - projections[0];

        float maxDistProjected = unit.Length;
        int leftmost = 1;

        for(var i = 2; i < projections.Count; i++) {
            var vec = projections[i] - projections[0];
            var distProjected = vec.Length;
            if(distProjected > maxDistProjected) {
                maxDistProjected = distProjected;
                leftmost = i;
            }
        }

        return leftmost;
    }

    public static float GetMinRect(List<Vector3> hull, Vector3[] curRect) {
        // writing comments so i can possibly gaslight myself in the future
        float frac; // amount of unit vector to use to get projection of current point (toProject)
        float normalLen; // length of normal from current line to current point (toProject)
        Vector3 normal; // normal from current line to current point

        // parameters of rect with minimal area
        // ...Frac variables describe how much of dir... vector needed to use to get 'upper' and 'lower' boundaries
        float minFracTotal = Single.MaxValue;
        float maxFracTotal = Single.MinValue;
        float longestNormalLenTotal = Single.MinValue;
        float minArea = Single.MaxValue;
        Vector3 dirNormalizedTotal = new(0);
        Vector3 normalNormalizedTotal = new(0);
        Vector3 originTotal = new(0);
        

        for(int i = 1; i < hull.Count; i++) {
            float minFrac = Single.MaxValue;
            float maxFrac = Single.MinValue;
            float longestNormalLen = Single.MinValue;

            var dirNormalized = (hull[i] - hull[i - 1]).Normalized();
            Vector3 normalNormalized = new(0);
            for(var j = 0; j < hull.Count; j++) {
                var samplePoint = hull[j];
                var projected = ProjectOnLineWithFrac(hull[i-1], dirNormalized, samplePoint, out frac);
                if(samplePoint != projected) {
                    normalNormalized = samplePoint - projected;
                    normalNormalized.Normalize();
                    break;
                }
            }

            for(int toProject = 0; toProject < hull.Count; toProject++) {
                var projected = ProjectOnLineWithFrac(hull[i - 1], dirNormalized, hull[toProject], out frac);
                if(frac > maxFrac) maxFrac = frac;
                if(frac < minFrac) minFrac = frac;
                normal = hull[toProject] - projected;
                normalLen = normal.Length;

                if(longestNormalLen < normalLen) {
                    longestNormalLen = normalLen;
                }
            }

            var area = (maxFrac - minFrac)*longestNormalLen;
            if(area < minArea) {
                originTotal = hull[i - 1];
                minFracTotal = minFrac;
                maxFracTotal = maxFrac;
                longestNormalLenTotal = longestNormalLen;
                dirNormalizedTotal = dirNormalized;
                normalNormalizedTotal = normalNormalized;
            }
        }

        curRect[0] = originTotal + dirNormalizedTotal*minFracTotal;
        curRect[1] = originTotal + dirNormalizedTotal*maxFracTotal;
        curRect[2] = curRect[1] + normalNormalizedTotal * longestNormalLenTotal;
        curRect[3] = curRect[0] + normalNormalizedTotal * longestNormalLenTotal;

        
        

        return (maxFracTotal - minFracTotal)*longestNormalLenTotal;
    }

    public static double ExtendMinRect(Vector3[] curRect, float curRectArea, float[] coords, Vector3[] normals, Vector3[] curPrism, List<Vector3> projections) {
        float minFrac = Single.MaxValue;
        float maxFrac = Single.MinValue;

        Vector3 rectNormalNormalized = Vector3.Cross(curRect[1] - curRect[0], curRect[2] - curRect[0]).Normalized();

        for(int i = 0; i < projections.Count; i++) {
            var normal = new Vector3(
                coords[i*3 + 0],
                coords[i*3 + 1],
                coords[i*3 + 2]
            );
            normal -= projections[i];

//            float frac = normal.Length * Math.Sign(Vector3.Dot(normal, rectNormalNormalized));
            float frac = Vector3.Dot(normal, rectNormalNormalized);
            
            if(minFrac > frac) minFrac = frac;
            if(maxFrac < frac) maxFrac = frac;
        }

        var backNormal = minFrac*rectNormalNormalized;
        var frontNormal = maxFrac*rectNormalNormalized;
        
        curPrism[0] = curRect[0] + backNormal;
        curPrism[1] = curRect[1] + backNormal;
        curPrism[2] = curRect[2] + backNormal;
        curPrism[3] = curRect[3] + backNormal;
        curPrism[4] = curRect[0] + frontNormal;
        curPrism[5] = curRect[1] + frontNormal;
        curPrism[6] = curRect[2] + frontNormal;
        curPrism[7] = curRect[3] + frontNormal;

        return curRectArea*(maxFrac - minFrac);
    }

    public static double GetAngle(Vector3 f, Vector3 s) {
        var dot = Vector3.Dot(f, s);
        var det = (new Vector3(
            f.Y*s.Z - s.Y*f.Z,
            f.X*s.Z - f.Z*s.X, /* actually should be negative, but we're calculating length so... */
            f.X*s.Y - f.Y*s.X
        )).Length;
        var angle = Math.Atan2(det, dot);
        return angle;
    }

    // uses SAT (separating axis theorem) for line.
    // Also checks if any vertex (of all planes) faces the same direction as dirNormalized relatively to posProjected.
    public bool IntersectsLine(Vector3 posUnprojected, Vector3 dirNormalized, out bool allOnSameSide) {
        allOnSameSide = true;

        var nullVec = new Vector3(0);
        for(int normalIndex = 0; normalIndex < normals.Length; normalIndex++) {
            bool anyOnSameSidePerPlane = false;

            var pos = ProjectOnPlane(nullVec, normals[normalIndex], posUnprojected);
            var projectedDir = ProjectOnPlane(nullVec, normals[normalIndex], dirNormalized);
            var lineNormalNormalized = Vector3.Cross(
                projectedDir,
                normals[normalIndex]
            );
            lineNormalNormalized.Normalize();

            float minFrac = Single.MaxValue;
            float maxFrac = Single.MinValue;

            for(int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++) {
                Vector3 vertex = projectedVertices[normalIndex*vertices.Length + vertexIndex];
                float frac = Vector3.Dot(
                    vertex - pos,
                    lineNormalNormalized
                );
                if(minFrac > frac) minFrac = frac;
                if(maxFrac < frac) maxFrac = frac;

                if(allOnSameSide && !anyOnSameSidePerPlane) {
                    float dot = Vector3.Dot(
                        vertex - (pos + lineNormalNormalized*frac),
                        projectedDir
                    );
                    
                    anyOnSameSidePerPlane = dot > 0;
                }
            }

            allOnSameSide = allOnSameSide && anyOnSameSidePerPlane;

            // comparing to 0 because frac is computed relative to 'pos' for every projection
            if(minFrac > 0 || maxFrac < 0) {
                return false;
            }
        }
        return true;
    }

    // accepts row-major, default OpenTK matrix
    public void ApplyToStartVertices(Matrix4 matrix) {
        matrix.Transpose();

        for(int i = 0; i < startVertices.Length; i++) {
            // vertices[i] = Vector3.TransformPosition(startVertices[i], matrix);
            var vec4 = matrix* new Vector4(startVertices[i], 1);
            vertices[i].X = vec4.X;
            vertices[i].Y = vec4.Y;
            vertices[i].Z = vec4.Z;
        }
        ComputeNormals();
    }

    private void ComputeNormals() {
        uint elemPerParallelPlane = (uint)ElementPerPlane*2;
        for(uint i = 0; i < StaticElements.Length; i += elemPerParallelPlane) {
            var normalIndex = i / elemPerParallelPlane;
            normals[normalIndex] = Vector3.Cross(
                VertexByElem(i+1) - VertexByElem(i),
                VertexByElem(i+2) - VertexByElem(i)
            ).Normalized();
        }

        var nullVec = new Vector3(0);
        for(int i = 0; i < normals.Length; i++) {
            for(int j = 0; j < vertices.Length; j++) {
                projectedVertices[vertices.Length*i + j] =
                    ProjectOnPlane(nullVec, normals[i], vertices[j]);
            }
        }
    }


    private Vector3 VertexByElem(uint staticElementsOffset) {
        return vertices[StaticElements[staticElementsOffset]];
    }
}
