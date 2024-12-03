using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTKAvalonia;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System;

namespace _5pr.Controls {
    public class MainOpenGlControl: BaseTkOpenGlControl {
        private static int StaticWidth = 1400;
        private static int StaticHeight = 750;
        private static readonly float sens = 0.2f;
        private static readonly float speed = 0.02f;
        private static readonly float fovy = MathHelper.DegreesToRadians(50);
        private static readonly float ratio = 2.0f;
        private static readonly float zNear = 0.01f;
        private static readonly float zFar = 100f;
        // centered i.e. halved
        private static Vector2 screenSizeWorldSpaceHalved;
        private static Matrix4 identityMatrix = Matrix4.Identity;

        public bool IsModelAdded = false;
        public static Mutex modelsMutex = new Mutex();
        public string newModelTypeName = "";

        List<Func<Model>> addedModelCtors = new();
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
        bool isPressing = false;

        Vector3 lightPos = new (5, 0, 0);
        Vector3 lightDir = (new Vector3(-1, 0, 0)) * -1;
        Model? selectedModel = null;

        float[] lineCoords = new float[] {
            0,8,0,
            -1,0,0,
        };

        Shader? shader = null;
        Shader? boxShader = null;

        public List<Model> models = new();

        public MainOpenGlControl() {}

        static MainOpenGlControl() {
            // TODO: try understand why it is sufficient to use only tan
            //       without any consant to multiply with.
            float y = (float)Math.Tan(fovy / 2);
            screenSizeWorldSpaceHalved = new Vector2(ratio*y, y);
        }

        protected override void OpenTkInit() {
            Console.WriteLine("Hello!");
            worldSpacePerPixel = 2*new Vector2(
                screenSizeWorldSpaceHalved.X / StaticWidth,
                screenSizeWorldSpaceHalved.Y / StaticHeight
            );
            view = Matrix4.LookAt(position, position + front, up);
            shader = new Shader("shaders/vertex.glsl", "shaders/fragment.glsl");
            boxShader = new Shader("shaders/box_vertex.glsl", "shaders/box_fragment.glsl");

            GL.DebugMessageCallback(OnDebugMessage, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);
            // CursorState = CursorState.Grabbed;

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
    //            GL.Enable(EnableCap.DebugOutputSynchronous);

            projection = Matrix4.CreatePerspectiveFieldOfView(fovy, ratio, zNear, zFar);
            invProjection = projection.Inverted();
            shader.Use(ref view, ref projection);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Uniform3(GL.GetUniformLocation(shader.Handle, "light_dir"), lightDir.X, lightDir.Y, lightDir.Z);
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

        protected override void OpenTkRender() {
            //modelsMutex.WaitOne();
            if(IsModelAdded) {
                models.Add(MainWindow.modelConstructors[newModelTypeName]());
                IsModelAdded = false;
            }
            //modelsMutex.ReleaseMutex();

            DoUpdate();
            modelsMutex.WaitOne();
            DoRender();
            modelsMutex.ReleaseMutex();
        }

        private void DoRender() {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader!.Use(ref view, ref projection);
            for(int i = 0; i < models.Count; i++) {
                Model m = models[i];
                shader.SetModelMatrix(ref m.matrix.matrix);
                m.Draw();
            }

            if(selectedModel != null) {
                boxShader!.Use(ref view, ref projection);
                boxShader!.SetModelMatrix(ref selectedModel.matrix.matrix);
                selectedModel.DrawBox();
            }
        }

        private void DoUpdate() {
            AvaloniaKeyboardState input = KeyboardState;

            /* Player */

            if (input.IsKeyDown(Avalonia.Input.Key.W)) {
                position += front * speed; //Forward 
            }

            if (input.IsKeyDown(Avalonia.Input.Key.S)) {
                position -= front * speed; //Backwards
            }

            if (input.IsKeyDown(Avalonia.Input.Key.A)) {
                position -= Vector3.Normalize(Vector3.Cross(front, up)) * speed; //Left
            }

            if (input.IsKeyDown(Avalonia.Input.Key.D)) {
                position += Vector3.Normalize(Vector3.Cross(front, up)) * speed; //Right
            }

            if (input.IsKeyDown(Avalonia.Input.Key.Space)) {
                position += up * speed; //Up 
            }

            if (input.IsKeyDown(Avalonia.Input.Key.LeftShift)) {
                position -= up * speed; //Down
            }

            if(selectedModel == null) {
                RecalculateView();
                return;
            }

            /* Model */

            if (input.IsKeyDown(Avalonia.Input.Key.NumPad7)) {
                selectedModel.Scale(new Vector3(0,1,0) * speed);
            }

            if (input.IsKeyDown(Avalonia.Input.Key.NumPad9)) {
                selectedModel.Scale(new Vector3(0,1,0) * -speed);
            }

            if (input.IsKeyDown(Avalonia.Input.Key.NumPad4)) {
                selectedModel.Scale(new Vector3(0,0,1) * speed);
            }

            if (input.IsKeyDown(Avalonia.Input.Key.NumPad6)) {
                selectedModel.Scale(new Vector3(0,0,1) * -speed);
            }

            if (input.IsKeyDown(Avalonia.Input.Key.NumPad1)) {
                selectedModel.Scale(new Vector3(1,0,0) * speed);
            }

            if (input.IsKeyDown(Avalonia.Input.Key.NumPad3)) {
                selectedModel.Scale(new Vector3(1,0,0) * -speed);
            }

            if (input.IsKeyDown(Avalonia.Input.Key.NumPad8)) {
                selectedModel.Move(new Vector3(0,1,0) * speed);
            }

            if (input.IsKeyDown(Avalonia.Input.Key.NumPad2)) {
                selectedModel.Move(new Vector3(0,-1,0) * speed);
            }

            if (input.IsKeyDown(Avalonia.Input.Key.Up)) {
                selectedModel.Move(new Vector3(1,0,0) * speed);
            }

            if (input.IsKeyDown(Avalonia.Input.Key.Down)) {
                selectedModel.Move(new Vector3(-1,0,0) * speed);
            }

            if (input.IsKeyDown(Avalonia.Input.Key.Left)) {
                selectedModel.Move(new Vector3(0,0,1) * speed);
            }

            if (input.IsKeyDown(Avalonia.Input.Key.Right)) {
                selectedModel.Move(new Vector3(0,0,-1) * speed);
            }

            RecalculateView();
        }

        protected override void OnPointerMoved(PointerEventArgs e) {
            Avalonia.Point curPosPoint = e.GetPosition(null);
            Vector2 curPos = new((float)curPosPoint.X, (float)curPosPoint.Y);

            if(!isPressing) {
                lastPos = curPos;
            }

            float deltaX = 0;
            float deltaY = 0;
            if (firstMove < 10) {
                lastPos = new Vector2(curPos.X, curPos.Y);
                firstMove += 1;
            } else {
                deltaX = curPos.X - lastPos.X;
                deltaY = curPos.Y - lastPos.Y;
                lastPos = curPos;

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
            GL.UniformMatrix4(GL.GetUniformLocation(shader!.Handle, "view"), false, ref view);
        }

        protected override void OpenTkTeardown() {
            shader!.Dispose();
            boxShader!.Dispose();
            Console.WriteLine("Bye");
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e) {
            isPressing = false;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e) {
            isPressing = true;
        }

        private Vector3 TranslateScreenToWorld(float deltaX, float deltaY) {
            return (worldSpacePerPixel.Y * deltaY) * up 
                + (worldSpacePerPixel.X * deltaX) * right;
        }


        public int AddModel(Func<Model> modelCtor) {
            addedModelCtors.Add(modelCtor);
            int id = models.Count;
            return id;
        }

        public void SelectModel(int id) {
            if(id >= models.Count) {
                throw new Exception("MainOpenGlControl.SelectModel: id is out of bounds of 'models' list");
            }
            selectedModel = models[id];
        }

        public void DeleteModel(int id) {
            if(id >= models.Count) {
                throw new Exception("MainOpenGlControl.SelectModel: id is out of bounds of 'models' list");
            }
            var toDelete = models[id];
            if(selectedModel == toDelete) {
                selectedModel = null;
            } 
            toDelete.Dispose();
            models.RemoveAt(id);
        }

        static int DPI = 144;
        public Bitmap RenderToBitmap() {
            DoRender();
            System.Drawing.Rectangle rect = new(0, 0, (int)Bounds.Width, (int)Bounds.Height);

            Console.WriteLine("Construct bitmap");
            WriteableBitmap bmap = new WriteableBitmap(
                new Avalonia.PixelSize((int)Bounds.Width, (int)Bounds.Height),
                new Avalonia.Vector(DPI, DPI),
                Avalonia.Platform.PixelFormat.Rgba8888, 
                Avalonia.Platform.AlphaFormat.Opaque
            );

            using(var dataLocked = bmap.Lock()) {
                IntPtr dataPtr = dataLocked.Address;
                Console.WriteLine("Start read");
                GLFuncs.ReadPixels(rect, dataPtr);
                Console.WriteLine("End read");
            }

            return bmap;
        }
    }
}
