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
    private static int StaticWidth = 1690;
    private static int StaticHeight = 1080;
    private static readonly float sens = 0.2f;
    private static readonly float speed = 0.005f;
    private static readonly float fovy = MathHelper.DegreesToRadians(50);
    private static readonly float ratio = 1.4f;
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
    Model? selectedModel = null;

    float[] lineCoords = new float[] {
        0,8,0,
        -1,0,0,
    };

    Shader shader;
    Shader boxShader;

    public List<Model> models = new();

    private Menu menu = new();

    static GraphicsWindow() {
        // TODO: try understand why it is sufficient to use only tan
        //       without any consant to multiply with.
        float y = (float)Math.Tan(fovy / 2);
        screenSizeWorldSpaceHalved = new Vector2(ratio*y, y);
    }

    public GraphicsWindow(int width, int height, string title) : base(
        new GameWindowSettings(), 
        new NativeWindowSettings {
            MaximumClientSize = new Vector2i(StaticWidth, StaticHeight),
            MinimumClientSize = new Vector2i(StaticWidth, StaticHeight),
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

        menu = new Menu{
            {"Выбрать модель", ChooseModel},
            {"Добавить модель", AddModel},
            {"Удалить модель", DeleteModel},
            {"Сделать скриншот", Screenshot},
        };
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

    protected override void OnFocusedChanged(FocusedChangedEventArgs e) {
        base.OnFocusedChanged(e);
        if(!IsFocused) {
            CursorState = CursorState.Normal;
        } else {
            CursorState = CursorState.Grabbed;
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

        if(selectedModel != null) {
            boxShader.Use(ref view, ref projection);
            boxShader.SetModelMatrix(ref selectedModel.matrix.matrix);
            selectedModel.DrawBox();
        }

        SwapBuffers();
    }

    protected override void OnLoad() {
        Console.WriteLine("Hello!");
        GL.DebugMessageCallback(OnDebugMessage, IntPtr.Zero);
        GL.Enable(EnableCap.DebugOutput);
        CursorState = CursorState.Grabbed;

        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
//            GL.Enable(EnableCap.DebugOutputSynchronous);

        projection = Matrix4.CreatePerspectiveFieldOfView(fovy, ratio, zNear, zFar);
        invProjection = projection.Inverted();
        shader.Use(ref view, ref projection);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.Uniform3(GL.GetUniformLocation(shader.Handle, "light_dir"), lightDir.X, lightDir.Y, lightDir.Z);

    }

    protected override void OnUnload() {
        Console.WriteLine("Bye!");
        shader.Dispose();
        boxShader.Dispose();
    }

    protected override void OnResize(ResizeEventArgs e) {
        GL.Viewport(0, 0, Size.X, Size.Y);
        base.OnResize(e);
    }

    protected override void OnUpdateFrame(FrameEventArgs e) {
        if (!IsFocused) {
            return;
        }

        KeyboardState input = KeyboardState;

        /* Player */

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

        if(input.IsKeyReleased(Keys.X)) {
            CursorState = CursorState.Normal;
            menu.Run();
            firstMove = 0;
            CursorState = CursorState.Grabbed;
        }


        if(selectedModel == null) {
            RecalculateView();
            return;
        }

        /* Model */

        if (input.IsKeyDown(Keys.KeyPad7)) {
            selectedModel.Scale(new Vector3(0,1,0) * speed);
        }

        if (input.IsKeyDown(Keys.KeyPad9)) {
            selectedModel.Scale(new Vector3(0,1,0) * -speed);
        }

        if (input.IsKeyDown(Keys.KeyPad4)) {
            selectedModel.Scale(new Vector3(0,0,1) * speed);
        }

        if (input.IsKeyDown(Keys.KeyPad6)) {
            selectedModel.Scale(new Vector3(0,0,1) * -speed);
        }

        if (input.IsKeyDown(Keys.KeyPad1)) {
            selectedModel.Scale(new Vector3(1,0,0) * speed);
        }

        if (input.IsKeyDown(Keys.KeyPad3)) {
            selectedModel.Scale(new Vector3(1,0,0) * -speed);
        }

        if (input.IsKeyDown(Keys.KeyPad8)) {
            selectedModel.Move(new Vector3(0,1,0) * speed);
        }

        if (input.IsKeyDown(Keys.KeyPad2)) {
            selectedModel.Move(new Vector3(0,-1,0) * speed);
        }

        if (input.IsKeyDown(Keys.Up)) {
            selectedModel.Move(new Vector3(1,0,0) * speed);
        }

        if (input.IsKeyDown(Keys.Down)) {
            selectedModel.Move(new Vector3(-1,0,0) * speed);
        }

        if (input.IsKeyDown(Keys.Left)) {
            selectedModel.Move(new Vector3(0,0,1) * speed);
        }

        if (input.IsKeyDown(Keys.Right)) {
            selectedModel.Move(new Vector3(0,0,-1) * speed);
        }

        RecalculateView();
    }

    protected override void OnMouseMove(MouseMoveEventArgs e) {
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

            /*
             * RecalculateView(); happens in OnUpdateFrame
            */
        }
    }

    private void RecalculateView() {
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
    }

    private void PrintModels() {
        Console.WriteLine("Текущие модели:");
        for(int i = 0; i < models.Count; i++) {
            Console.WriteLine($"{i}. {models[i]}");
        }
    }

    private void AddModel() {
        Console.WriteLine("0. Пирамида\n1. Сфера\n2. Куб\n");
        int index = SafeInput.Uint("Модель: ", null);
        if(index > 2 || index < 0) {
            Console.WriteLine("Индекс вне границ, модель не добавлена. Возврат в меню.");
            return;
        }

        switch(index) {
        case 0: models.Add(Model.Pyramid()); break;
        case 1: models.Add(Model.Sphere()); break;
        case 2: models.Add(Model.Cube()); break;
        }
    }

    private void DeleteModel() {
        PrintModels();
        int index = SafeInput.Uint("Индекс: ", null);
        if(index > models.Count) {
            Console.WriteLine("Индекс вне границ массива, модель не удалена. Возврат в меню.");
        } else {
            models.RemoveAt(index);
        }
    }

    private void ChooseModel() {
        PrintModels();
        int index = SafeInput.Uint("Индекс: ", null);
        if(index > models.Count) {
            Console.WriteLine("Индекс вне границ массива, модель не выбрана. Возврат в меню.");
        } else {
            selectedModel = models[index];
        }
    }

    private void Screenshot() {
        Console.WriteLine("Пока не доступно.");
    }

    public static void Main() {
        var w = new GraphicsWindow(StaticWidth, StaticHeight, "hello world");
        w.models.Add(Model.Sphere());
        w.models.Add(Model.Cube());
        w.Run();
        return; 
    }
}
