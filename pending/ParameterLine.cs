using System;
using OpenTK.Mathematics;

// TODO: rethink bindings to model (maybe you should remove it)
// It's obvious that every model will have some pre-existing lines
public abstract class ParameterLine {
    private static readonly string LineDirectory = "./lines/";

    public Model? model;
    public Model lineModel = Model.Line();
    public Vector3 dir;

    public abstract void Trigger(Vector3 vec);

    protected virtual void ProcessRawModel(ObjModel rawModel) {}
    
    public static List<ParameterLine> ForPyramid() {
        return ParseFile(LineDirectory + "pyramid.txt");
/*
        return new List<ParameterLine>{
            new ScaleLine(),
            new MoveLine()
        }
*/;
    }

    public static List<ParameterLine> ForSphere() {
        return ParseFile(LineDirectory + "sphere.txt");
/*
        return new List<ParameterLine>{
            new ScaleLine(),
            new MoveLine()
        }
*/;
    }

    public static List<ParameterLine> ForCube() {
        return ParseFile(LineDirectory + "cube.txt");
/*
        return new List<ParameterLine>{
            new ScaleLine(),
            new MoveLine()
        }
*/;
    }

    public void Bind(Model model, ObjModel rawModel) {
        this.model = model;
        ProcessRawModel(rawModel);
    }

    public void Draw() {
        lineModel.Draw();
    }

    public void Move(Vector3 vec) {
        lineModel.Move(vec);
    }

    public void Scale(Vector3 vec) {
        lineModel.Scale(vec);
    }

    public bool IntersectsLine(Vector3 pos, Vector3 dirNormalized, ref bool anyOnSameSide) {
        return lineModel.IntersectsLine(pos, dirNormalized, out anyOnSameSide);
    }

    public static List<ParameterLine> ParseFile(string absPath) {
        FileInfo fi = new FileInfo(absPath);

        if(!fi.Exists) {
            throw new Exception(String.Format("File {0} does not exist", absPath));
        }

        StreamReader reader = new(absPath);

        List<ParameterLine> list = new();
        string? textLine = reader.ReadLine();
        while(textLine != null) {
            if(!TryParse(new ArraySegment<string>(textLine.Split(' ')), out ParameterLine? line)
            || line == null) {
                throw new Exception(String.Format("Can't parse file with lines: {0}", absPath));
            }
            list.Add(line!);
        }
        return list;
    }

    protected static bool TryParse(ArraySegment<string> str, out ParameterLine? line) {
        line = null;
        if(ScaleLine.TryParse(str, out line)) return true;
        if(MoveLine.TryParse(str, out line)) return true;

        return false;
    }

    protected static bool TryParseTriple(ArraySegment<string> strings, ref Vector3 res) {
        if(strings.Count != 3) {
            Console.WriteLine("TryParseCoords: ERROR: strings.Count != 3");
            return false;
        }

        for(int i = 0; i < 3; i++) {
            float curCoord;
            if(Single.TryParse(strings[i], out curCoord)) {
                res[i] = curCoord;
                continue;
            }
            Console.WriteLine("TryParseCoords: ERROR: Couldn't parse {0}'th number", i);
            return false;
        }
        return true;
    }

    protected static bool TryParsePosAndDir(ArraySegment<string> strings, ref Vector3 pos, ref Vector3 dir) {
        if(strings.Count != 6) {
            Console.WriteLine("TryParsePosAndDir: ERROR: strings.Count != 6");
            return false;
        }

        if(!TryParseTriple(strings.Slice(0,3), ref pos)
        || !TryParseTriple(strings.Slice(3,3), ref dir)
        ) {
            return false;
        }
        return true;
    }
}

public sealed class ScaleLine: ParameterLine {
    public ScaleLine() {}

    public override void Trigger(Vector3 vec) {
        model?.Scale(vec);
    }

    public static new bool TryParse(ArraySegment<string> str, out ParameterLine? line) {
        line = null;
        if(str[0] != "s") return false;
        
        ScaleLine scaleLine = new();
        ParameterLine.TryParsePosAndDir(str.Slice(1), ref scaleLine.lineModel.matrix.pos, ref scaleLine.dir);
        scaleLine.lineModel.matrix.Recompute();

        line = scaleLine;
        return true;
    }
}

public sealed class MoveLine: ParameterLine {
    public MoveLine() {}

    public override void Trigger(Vector3 vec) {
        model?.Scale(vec);
    }

    public static new bool TryParse(ArraySegment<string> str, out ParameterLine? line) {
        line = null;
        if(str[0] != "m") return false;

        MoveLine moveLine = new();
        ParameterLine.TryParsePosAndDir(str.Slice(1), ref moveLine.lineModel.matrix.pos, ref moveLine.dir);
        moveLine.lineModel.matrix.Recompute();

        line = moveLine;

        return true;
    }
}

public sealed class AnchoredScaleLine: ParameterLine {
    private Vector3 anchor = new(0);

    public AnchoredScaleLine() {}

    public override void Trigger(Vector3 vec) {
        // TODO: model?.Scale(vec);
    }

    protected override void ProcessRawModel(ObjModel rawModel) {}

    public static new bool TryParse(ArraySegment<string> str, out ParameterLine? line) {
        line = null;
        if(str[0] != "m") return false;

        var anchorLine = new AnchoredScaleLine();
        if(!ParameterLine.TryParsePosAndDir(str.Slice(1), ref anchorLine.lineModel.matrix.pos, ref anchorLine.dir)
        || !ParameterLine.TryParseTriple(str.Slice(6), ref anchorLine.anchor)
        ) {
            return false;
        }
        anchorLine.lineModel.matrix.Recompute();

        line = anchorLine;
        return true;
    }
}
