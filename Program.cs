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

public class GraphicsWindow: GameWindow {
    private static readonly float sens = 0.2f;
    private static readonly float speed = 0.005f;
    private static readonly float fovy = MathHelper.DegreesToRadians(50);
    private static readonly float ratio = 1.5f;
    private static readonly float zNear = 0.01f;
    private static readonly float zFar = 100f;
    // centered i.e. halved
    private static Vector2 screenSizeWorldSpaceHalved;
    private static Matrix4 identityMatrix = Matrix4.Identity;

    Vector2 worldSpacePerPixel;
    Vector3 position = new Vector3(1, 0, 0);
    Vector3 front = new Vector3(-1, 0, 0);
    Vector3 up = new Vector3(0,1,0);
    Vector3 right;
    Matrix4 view;
    Matrix4 projection;
    Matrix4 invProjection;
    float yaw = 0;
    float pitch = 0;
    int firstMove = 0;
    Vector2 lastPos = new(0,0);

    Vector3 lightPos = new (5, 0, 0);
    Vector3 lightDir = (new Vector3(-1, 0, 0)) * -1;

    float[] lineCoords = new float[] {
        0,8,0,
        -1,0,0,
    };

    Shader shader;
    Shader boxShader;

    public List<Model> models = new();

    static GraphicsWindow() {
        // TODO: try understand why it is sufficient to use only tan
        //       without any consant to multiply with.
        float y = (float)Math.Tan(fovy / 2);
        screenSizeWorldSpaceHalved = new Vector2(ratio*y, y);
    }

    public GraphicsWindow(int width, int height, string title) : base(
        new GameWindowSettings(), 
        new NativeWindowSettings() { 
            ClientSize = new OpenTK.Mathematics.Vector2i(width, height),
            Title = title 
        }
    ) {
        worldSpacePerPixel = new Vector2(
            screenSizeWorldSpaceHalved.X / ClientSize.X,
            screenSizeWorldSpaceHalved.Y / ClientSize.Y
        );
        view = Matrix4.LookAt(position, position + front, up);
        shader = new Shader("shaders/vertex.glsl", "shaders/fragment.glsl");
        boxShader = new Shader("shaders/box_vertex.glsl", "shaders/box_fragment.glsl");
    }

    private static void OnDebugMessage(
            DebugSource source,     
            DebugType type,         
            int id,                 
            DebugSeverity severity, 
            int length,             
            IntPtr pMessage,        
            IntPtr pUserParam
    ) {
        string message = Marshal.PtrToStringAnsi(pMessage, length);
        
        Console.WriteLine("[{0} source={1} type={2} id={3}] {4}", severity, source, type, id, message);
        
        if (type == DebugType.DebugTypeError) {
            throw new Exception(message);
        }
    }

    protected override void OnRenderFrame(FrameEventArgs e) {
        base.OnRenderFrame(e);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        shader.Use(ref view, ref projection);
        foreach(var m in models) {
            shader.SetModelMatrix(ref m.matrix.matrix);
            m.Draw();
        }

        boxShader.Use(ref view, ref projection);
        foreach(var m in models) {
            boxShader.SetModelMatrix(ref m.matrix.matrix);
            m.DrawBox();
        }
        for(int modelIndex = 0; modelIndex < models.Count; modelIndex++) {
            if(!drawLineFlags[modelIndex]) {
                continue;
            }
            models[modelIndex].DrawLines(boxShader);
        }

        SwapBuffers();
    }

    protected override void OnLoad() {
        Console.WriteLine("Hello!");
        GL.DebugMessageCallback(OnDebugMessage, IntPtr.Zero);
        GL.Enable(EnableCap.DebugOutput);

        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
//            GL.Enable(EnableCap.DebugOutputSynchronous);

        projection = Matrix4.CreatePerspectiveFieldOfView(fovy, ratio, zNear, zFar);
        invProjection = projection.Inverted();
        shader.Use(ref view, ref projection);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.Uniform3(GL.GetUniformLocation(shader.Handle, "light_dir"), lightDir.X, lightDir.Y, lightDir.Z);

//        CursorState = CursorState.Grabbed;
    }

    protected override void OnUnload() {
        Console.WriteLine("Bye!");
        shader.Dispose();
    }

    static bool[] drawLineFlags = new bool[1024];
    protected override void OnUpdateFrame(FrameEventArgs e) {
        if (!IsFocused) {
            return;
        }

        KeyboardState input = KeyboardState;

        if (input.IsKeyDown(Keys.W)) {
            position += front * speed; //Forward 
        }

        if (input.IsKeyDown(Keys.S)) {
            position -= front * speed; //Backwards
        }

        if (input.IsKeyDown(Keys.A)) {
            position -= Vector3.Normalize(Vector3.Cross(front, up)) * speed; //Left
        }

        if (input.IsKeyDown(Keys.D)) {
            position += Vector3.Normalize(Vector3.Cross(front, up)) * speed; //Right
        }

        if (input.IsKeyDown(Keys.Space)) {
            position += up * speed; //Up 
        }

        if (input.IsKeyDown(Keys.LeftShift)) {
            position -= up * speed; //Down
        }

        if (input.IsKeyDown(Keys.Up)) {
            models[0].Scale(new Vector3(1,1,1) * speed);
//            models[0].Move(new Vector3(-1,0,0) * speed);
        }

        if (input.IsKeyDown(Keys.Down)) {
            models[0].Scale(new Vector3(1,1,1) * -speed);
//            models[0].Move(new Vector3(1,0,0) * speed);
        }

        if (input.IsKeyDown(Keys.Left)) {
//            models[0].Move(new Vector3(1,0,0) * speed);
            yaw -= speed * 50;
        }

        if (input.IsKeyDown(Keys.Right)) {
//            models[0].Move(new Vector3(-1,0,0) * speed);
            yaw += speed * 50;
        }

        if (input.IsKeyDown(Keys.C)) {
            models[0].Move(new Vector3(0,0,1) * speed);
        }

        if (input.IsKeyDown(Keys.V)) {
            models[0].Move(new Vector3(0,0,-1) * speed);
        }

        if(input.IsKeyReleased(Keys.X)) {
            Vector3 dir = new Vector3(MouseState.X, MouseState.Y, 0);
            dir.X = 2*dir.X - ClientSize.X;
            dir.Y = 2*dir.Y - ClientSize.Y;

            dir.X *= worldSpacePerPixel.X;
            dir.Y *= -worldSpacePerPixel.Y;
            dir = right*dir.X + up*dir.Y + front;

            for(int modelIndex = 0; modelIndex < models.Count; modelIndex++) {
                var m = models[modelIndex];
                bool intersects = m.IntersectsLine(position, dir, out bool anyOnSameSide);
                drawLineFlags[modelIndex] = intersects;
            }
            Console.WriteLine();
        }

        front.X = -(float)Math.Cos(MathHelper.DegreesToRadians(pitch)) 
            * (float)Math.Cos(MathHelper.DegreesToRadians(yaw));

        front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(pitch));

        front.Z = -(float)Math.Cos(MathHelper.DegreesToRadians(pitch))
            * (float)Math.Sin(MathHelper.DegreesToRadians(yaw));
        front = Vector3.Normalize(front);


        view = Matrix4.LookAt(position, position + front, up);
        right = Vector3.Cross(front, up);
        right.Normalize();
        GL.UniformMatrix4(GL.GetUniformLocation(shader.Handle, "view"), false, ref view);
//        GL.Uniform3(GL.GetUniformLocation(shader.Handle, "pos"), position.X, position.Y, position.Z);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e) {
        OnMouseMove_New(e);
    }

    protected void OnMouseMove_New(MouseMoveEventArgs e) {
        /*
        by mouse pos calculate ray which will hit some model,
        this model needs to react to the hit
         */
        if(!IsFocused) {
            return;
        }

    }

    protected void OnMouseMove_Old(MouseMoveEventArgs e) {
        if(!IsFocused) {
            return;
        } 

        float deltaX = 0;
        float deltaY = 0;
        if (firstMove < 10) {
            lastPos = new Vector2(MouseState.X, MouseState.Y);
            firstMove += 1;
        }
        else {
            deltaX = MouseState.X - lastPos.X;
            deltaY = MouseState.Y - lastPos.Y;
            lastPos = new Vector2(MouseState.X, MouseState.Y);

            yaw += deltaX * sens;
            if(pitch > 89.0f) {
                pitch = 89.0f;
            }
            else if(pitch < -89.0f) {
                pitch = -89.0f;
            }
            else {
                pitch -= deltaY * sens;
            }
            front.X = -(float)Math.Cos(MathHelper.DegreesToRadians(pitch)) 
                * (float)Math.Cos(MathHelper.DegreesToRadians(yaw));

            front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(pitch));

            front.Z = -(float)Math.Cos(MathHelper.DegreesToRadians(pitch))
                * (float)Math.Sin(MathHelper.DegreesToRadians(yaw));
            front = Vector3.Normalize(front);
        }
    }

    public static void Main() {
        var w = new GraphicsWindow(800, 600, "hello world");
        w.models.Add(Model.Cube());
        w.Run();
        return; 

        /*
        Vector3[] vertices = {
            new Vector3(1,0,0),
            new Vector3(3,1,0),
            new Vector3(2,3,0),
            new Vector3(0,2,0),

            new Vector3(0,2,-2),
//            new Vector3(1,0,-2),
//            new Vector3(1,1,-1),
        };
        var f = new float[vertices.Length*6];
        */
    }

    public static string arrX(Vector3[] vertices) {
        string str = "";
        for(int i = 0; i < vertices.Length; i++) {
            str += vertices[i].X.ToString();
            str += ", ";
        }
        return str;
    }
    public static string arrY(Vector3[] vertices) {
        string str = "";
        for(int i = 0; i < vertices.Length; i++) {
            str += vertices[i].Y.ToString();
            str += ", ";
        }
        return str;
    }
}
