using Egc;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.CompilerServices;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (main game loop).
/// Față de laboratorul 3: o scenă cu mai multe obiecte, fiecare cu matricea lui model,
/// o cameră cu matricele view și projection, tastatura și mouse-ul.
/// </summary>
public class Game : GameWindow
{
    // Obiecte care NU depind de OpenGL pot fi create imediat.
    private readonly Camera _camera = new();

    // Obiectele care folosesc OpenGL se creează în OnLoad (abia atunci există contextul OpenGL).
    private Shader _shader = null!;
    private readonly List<SceneObject> _scene = new();
    private readonly List<Transform2D> _initialTransforms = new();   // pentru tasta R

    // Starea aplicației (se modifică în OnUpdateFrame, se folosește în OnRenderFrame).
    private int _selected;            // indexul obiectului selectat în _scene (tasta Tab)
    private bool _wireframe;          // contur (true) sau plin (false)

    // Contoare afișate în titlul ferestrei.
    private readonly string _baseTitle;
    private double _secondTimer;
    private int _frames, _lastFrames;
    private bool _titleDirty = true;
    private Vector2 _pivot; // Punctul din lume folosit ca pivot pentru rotație (T4)
    private SceneObject _satellite = null!; // Obiectul satelit (T5)
    private float _totalTime;               // Timpul acumulat pentru animație
   
    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),          // cerem OpenGL 3.3 ...
                   Profile = ContextProfile.Core,           // ... profil Core (fără modul imediat)
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        VSync = VSyncMode.On;   // swap-ul bufferelor așteaptă reîmprospătarea monitorului
    }

    /// <summary>Apelată O SINGURĂ DATĂ, după crearea ferestrei și a contextului OpenGL.</summary>
    protected override void OnLoad()
    {
        base.OnLoad();

        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        // Scena: aceleași forme din laboratorul 3, acum plasate în lume prin Transform2D.
        // Depth (z) contează doar în perspectivă (tasta O, secțiunea 7.2).
        AddObject("pătrat", Shapes.Quad(),
                  new Transform2D { Position = new Vector2(-1.8f, -1.0f), Depth = -3.0f });
        AddObject("triunghi", Shapes.Triangle(),
                  new Transform2D { Position = new Vector2(-1.2f, 1.0f) });
        AddObject("hexagon", Shapes.RegularPolygon(6, 0.5f),
                  new Transform2D { Position = new Vector2(2.0f, 1.0f) });
        AddObject("cerc", Shapes.RegularPolygon(64, 0.5f),
                  new Transform2D { Position = new Vector2(0.8f, -0.6f), Depth = 2.0f });
        
        var p = new Vector4(2, 1, 0, 1);    // punctul (2; 1), cu a patra coordonată 1
        Console.WriteLine(p * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)));
        Console.WriteLine(p * Matrix4.CreateScale(-1, 1, 1));
        Console.WriteLine(p * Matrix4.CreateTranslation(3, -2, 0));
        Console.WriteLine(Matrix4.CreateTranslation(3, -2, 0));

        AddObject("triunghi", Shapes.Triangle(),
          new Transform2D
          {
              Position = new Vector2(-1.2f, 1.0f),
              Rotation = 45.0f,
              Scale = new Vector2(1.5f, 0.5f)
          });
        //Raspuns intrebare T1
        //Daca translatia ar fi implementata inainte: obiectul nu s-ar mai roti in jurul centrului sau propriu, ci s-ar roti
        //pe o orbita in jurul originii spatiului (0,0,0). Pozitia obiectului de pe ecran s-ar schimba total,deoarece translatia l-ar deplasa mai intai la 
        //coordonatele position, iar apoi rotatia ar roti intregul vector de pozitie in jurul originii, aruncand obiecyu; intr-o alta regiune a scenei.
        //Scalarea aplicata la final ar scalat distanta deja translatata fata de origine, deformand attat obiectul, cat si pozitia sa in scena.


        // T5: Creăm satelitul (un triunghi mai mic cu nuanță diferită)
        _satellite = new SceneObject("satelit", Shapes.Triangle(), new Transform2D());
        _satellite.Tint = Color4.Yellow; // Opțional: o culoare distinctă pentru satelit
    }

    /// <summary>Adaugă un obiect în scenă și reține transformarea lui inițială (pentru tasta R).</summary>
    private void AddObject(string name, Mesh mesh, Transform2D transform)
    {
        _scene.Add(new SceneObject(name, mesh, transform));
        _initialTransforms.Add(transform.Clone());
    }

    /// <summary>Apelată când se schimbă dimensiunea ferestrei.</summary>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);   // zona ferestrei în care se desenează
        _camera.Resize(e.Width, e.Height);      // proiecția se adaptează la noul raport de aspect
    }

    /// <summary>Apelată la fiecare iterație a buclei: LOGICA aplicației (intrări, mișcare, timp).</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        // --- Obiectul selectat: tastatura (secțiunea 8) ------------------------------

        // --- Obiectul selectat: tastatura (secțiunea 8) ------------------------------

        float dt = (float)args.Time;
        SceneObject sel = _scene[_selected];

        // Folosim 'float' (nu 'const') și punem 'f' după numerele zecimale (3.0f, 1.0f)
        float speedMultiplier = KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift) ? 3.0f : 1.0f;

        // Am scos cuvântul cheie 'const' din fața lui moveSpeed
        float moveSpeed = 2.0f * speedMultiplier;

        if (KeyboardState.IsKeyDown(Keys.Left)) sel.Transform.Position.X -= moveSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.Right)) sel.Transform.Position.X += moveSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.Down)) sel.Transform.Position.Y -= moveSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.Up)) sel.Transform.Position.Y += moveSpeed * dt;

        // 2. Rotație (Q / E) - 90 grade pe secundă
        float rotSpeed = 90.0f * speedMultiplier;
        if (KeyboardState.IsKeyDown(Keys.Q)) sel.Transform.Rotation += rotSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.E)) sel.Transform.Rotation -= rotSpeed * dt;

        // 3. Scalare (Z / X) - factor 1.5f pe secundă
        float scaleFactor = MathF.Pow(1.5f, speedMultiplier * dt);
        if (KeyboardState.IsKeyDown(Keys.Z)) sel.Transform.Scale /= scaleFactor;
        if (KeyboardState.IsKeyDown(Keys.X)) sel.Transform.Scale *= scaleFactor;

        // Limitare scalare între 0.1f și 10.0f
        sel.Transform.Scale.X = Math.Clamp(sel.Transform.Scale.X, 0.1f, 10.0f);
        sel.Transform.Scale.Y = Math.Clamp(sel.Transform.Scale.Y, 0.1f, 10.0f);

        //Raspuns la intrebarea T2
        //De ce scalarea se face prin inmultire si nu prin adunare?
        //Inmultirea garanteaza ca rata de schimbare este proportionala cu dimensiunea curenta a obiectlui. Daca un obiect are dimensiunea 
        //1 si se mareste cu un factor de 1,5, creste la 1,5 .Daca un obiect este deja foarte mare (ex. : dimensiune 100), prin inmultire va creste la 150, 
        //mentinand aceasi senzatie vizuala de ritm de marire/micsorrare. Daca am folosi adunarea/scaderea,un obiect foarte mare s-ar mari insesizabil de incet, iar un obiect mic s-ar
        //micsora mult prea repede. In plus, prin scadere repetata s-ar ajunge rapid la valori negative, ceea ce ar reflecta sau ar invizibiliza forma. Inmultirea cu un factor f > 1
        //mareste obiectul, iar impartirea la f il micsoreaza exact in mod inver; prin adunare/scadere nu poti obtine aceasta reciprocitate geometrica naturala.

        if (KeyboardState.IsKeyPressed(Keys.Tab)) _selected = (_selected + 1) % _scene.Count;
        if (KeyboardState.IsKeyPressed(Keys.O))   _camera.IsPerspective = !_camera.IsPerspective;
        if (KeyboardState.IsKeyPressed(Keys.F))   _wireframe = !_wireframe;
        if (KeyboardState.IsKeyPressed(Keys.R))   ResetScene();

        if (KeyboardState.IsKeyPressed(Keys.Tab) || KeyboardState.IsKeyPressed(Keys.O) ||
            KeyboardState.IsKeyPressed(Keys.R))
        {
            _titleDirty = true;
        }

        // --- Obiectul selectat: mouse-ul (secțiunea 9) -------------------------------

        // TODO (T3): la clic stânga, obiectul selectat se mută în punctul din lume aflat sub cursor
        //            (folosiți ScreenToWorld; Depth nu se schimbă)
        // --- Obiectul selectat: mouse-ul (secțiunea 9) -------------------------------

        // T3: la clic stânga, obiectul selectat se mută în punctul din lume aflat sub cursor
        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            Vector2 mouseWorld = ScreenToWorld(MouseState.Position);
            sel.Transform.Position = mouseWorld; // Depth nu se schimbă!
        }

        // TODO (T3): pasul 1 (pixel → NDC), apoi pasul 2 (NDC → lume, cu inversa lui view * projection)

        // TODO (T4, facultativ): clic dreapta = pivot; tasta P ținută = rotație în jurul pivotului
        // --- Obiectul selectat: mouse-ul și pivotul (T4) -------------------------------

        // 1. Clic dreapta: stabilește pivotul în punctul din lume sub cursor
        if (MouseState.IsButtonPressed(MouseButton.Right))
        {
            _pivot = ScreenToWorld(MouseState.Position);
        }

        // 2. Tasta P ținută apăsată: rotește obiectul în jurul pivotului
        if (KeyboardState.IsKeyDown(Keys.P))
        {
            // Calculăm unghiul de rotație pentru cadrul curent (90°/sec respectă și viteza crescută cu Shift)
            float rotAngleDegrees = 90.0f * speedMultiplier * dt;
            float rotAngleRadians = MathHelper.DegreesToRadians(rotAngleDegrees);

            // Pasul A: Rotim poziția obiectului în jurul pivotului
            Vector2 relativePos = sel.Transform.Position - _pivot;

            // Matrice / formulă de rotație 2D pe vectorul relativ
            float cos = MathF.Cos(rotAngleRadians);
            float sin = MathF.Sin(rotAngleRadians);
            Vector2 rotatedRelativePos = new Vector2(
                relativePos.X * cos - relativePos.Y * sin,
                relativePos.X * sin + relativePos.Y * cos
            );

            // Actualizăm noua poziție a obiectului în lume
            sel.Transform.Position = _pivot + rotatedRelativePos;

            // Pasul B: Rotația proprie crește cu aceeași valoare (efectul de rotație sincronă, ca Luna față de Pământ)
            sel.Transform.Rotation += rotAngleDegrees;
        }
        // Raspuns  T4:
        // Rotația în jurul unui pivot presupune doi pași geometrici consecutivi:
        // 1. Rotația poziției: Translatăm originea în pivot (Pos - Pivot), aplicăm matricea de rotație 2D, 
        //    apoi adăugăm inapoi pivotul. Astfel, obiectul își schimbă coordonatele pe o orbită circulară.
        // 2. Rotația proprie: Adăugăm exact același unghi și la proprietatea Rotation a obiectului. 
        //    Acest lucru garantează că obiectul își rotește și orientarea locală odată cu mișcarea pe orbită 
        //    (păstrând aceeași față îndreptată spre pivot, ca o mișcare de rotație sincronă).
        // --- Camera: tastatura și rotița (secțiunea 6.2) -----------------------------

        float camSpeed = 3.0f / _camera.Zoom;                    // unități de lume pe secundă
        if (KeyboardState.IsKeyDown(Keys.A)) _camera.Position.X -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.D)) _camera.Position.X += camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.S)) _camera.Position.Y -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.W)) _camera.Position.Y += camSpeed * dt;

        _camera.Zoom *= MathF.Pow(1.1f, MouseState.ScrollDelta.Y);   // un pas al rotiței = ±10 %
        _camera.Zoom = Math.Clamp(_camera.Zoom, 0.1f, 10.0f);

        _totalTime += (float)args.Time; 

        // --- Titlul ferestrei: obiectul selectat, proiecția, cadre pe secundă --------

        _secondTimer += args.Time;
        if (_secondTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _secondTimer -= 1.0;
            _titleDirty = true;
        }

        if (_titleDirty)
        {
            string projection = _camera.IsPerspective ? "perspectivă" : "ortografică";
            Title = $"{_baseTitle} | selectat: {_scene[_selected].Name} | proiecție {projection} | FPS: {_lastFrames}";
            _titleDirty = false;
        }
    }

    /// <summary>Apelată la fiecare iterație a buclei: DESENAREA unui cadru.</summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        // 1. Ștergem imaginea veche din back buffer.
        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // 2. Starea de rasterizare și programul shader.
        SetWireframe(_wireframe);
        _shader.Use();

        // 3. View și projection sunt aceleași pentru toate obiectele: le trimitem o singură dată.
        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);

        // 4. Fiecare obiect: matricea lui model, nuanța, apoi desenarea.
        foreach (SceneObject obj in _scene)
        {
            _shader.SetMatrix4("uModel", obj.Transform.ModelMatrix);
            _shader.SetColor("uTint", obj.Tint);
            obj.Mesh.Draw();
        }

        // TODO (T5, facultativ): satelitul obiectului selectat
        // --- T5: Calculul matricei model a satelitului -----------------------------

        // 1. O rotație completă (360 grade sau 2*PI radiani) la fiecare 4 secunde
        float orbitAngle = _totalTime * (MathF.PI * 2.0f / 4.0f);

        // 2. Transformarea locală a satelitului față de părinte:
        //    Scalare (0.3) -> Translație la distanța de 1.2 pe X -> Rotație pe orbită
        Matrix4 localMatrix = Matrix4.CreateScale(0.3f, 0.3f, 1.0f) *
                              Matrix4.CreateTranslation(1.2f, 0.0f, 0.0f) *
                              Matrix4.CreateRotationZ(orbitAngle);

        // 3. Matricea globală a satelitului = Transformarea Locală * Matricea Model a Părintelui
        SceneObject parent = _scene[_selected];
        Matrix4 satelliteModelMatrix = localMatrix * parent.Transform.ModelMatrix;

        // 4. Trimiterea matricei în shader și desenarea satelitului
        _shader.SetMatrix4("uModel", satelliteModelMatrix);
        _shader.SetColor("uTint", _satellite.Tint);
        _satellite.Mesh.Draw();
        // 5. Afișăm cadrul terminat: back buffer <-> front buffer.
        SwapBuffers();
    }

    /// <summary>Apelată O SINGURĂ DATĂ, la închiderea ferestrei: eliberăm resursele GPU.</summary>
    protected override void OnUnload()
    {
        foreach (SceneObject obj in _scene)
        {
            obj.Dispose();
        }
        _shader.Dispose();

        base.OnUnload();

        _satellite.Dispose();
    }

/// <summary>Transformă poziția mouse-ului (pixeli, origine stânga-sus) în coordonate de lume.</summary>
/// <summary>Transformă poziția mouse-ului (pixeli, origine stânga-sus) în coordonate de lume.</summary>
    private Vector2 ScreenToWorld(Vector2 mousePixels)
    {
        // 1. Convertim din pixeli (ecran: [0, width] x [0, height], originea stânga-sus)
        //    în Normalized Device Coordinates (NDC: [-1, 1] x [-1, 1], originea în centru, Y în sus)
        float xNdc = (2.0f * mousePixels.X / ClientSize.X) - 1.0f;
        float yNdc = 1.0f - (2.0f * mousePixels.Y / ClientSize.Y);

        // Poziția cursorului în spațiul NDC (pe z = 0 pentru planul 2D)
        Vector4 positionNdc = new Vector4(xNdc, yNdc, 0.0f, 1.0f);

        // 2. Calculăm inversa matricei combinate View * Projection
        Matrix4 viewProjection = _camera.ViewMatrix * _camera.ProjectionMatrix;
        Matrix4 invViewProjection = Matrix4.Invert(viewProjection);

        // 3. Înmulțim punctul NDC cu matricea inversă pentru a ajunge în spațiul Lume (World)
        Vector4 worldPos = positionNdc * invViewProjection;

        // Împărțim la coordonata w (perspectivă / omogenizare) și returnăm coordonatele X și Y
        return worldPos.Xy / worldPos.W;
    }

    //raspuns intrebare T3
    // COMENTARIU T3:
    // Semnul minus (respectiv expresia '1.0f - ...') apare din cauza diferenței dintre sistemele de coordonate pe axa Y:
    // 1. În spațiul ecran (pixeli), originea (0,0) se află în colțul din STÂNGA-SUS, iar axa Y crește în JOS.
    // 2. În spațiul NDC (Normalized Device Coordinates), originea se află în CENTRU, iar axa Y crește în SUS (de la -1 la +1).
    // Scăderea din 1.0f inversează axa Y, astfel încât coordonata y = 0 de pe ecran (sus) devine +1 în NDC.


    /// <summary>Tasta R: obiectele revin la transformările inițiale, camera la starea inițială.</summary>
    private void ResetScene()
    {
        for (int i = 0; i < _scene.Count; i++)
        {
            _scene[i].Transform = _initialTransforms[i].Clone();
        }
        _camera.Reset();
    }

    /// <summary>
    /// Contur / plin. Dacă Visual Studio semnalează o eroare la primul parametru,
    /// folosiți aici tipul acceptat de versiunea voastră (vezi laboratorul 2, Problema 3):
    /// TriangleFace.FrontAndBack sau MaterialFace.FrontAndBack.
    /// </summary>
    private static void SetWireframe(bool wireframe)
    {
        GL.PolygonMode(TriangleFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
    }

    // Raspuns T5:
    // Când obiectul selectat (părintele) este scalat (de exemplu, turtit sau mărit neuniform):
    // 1. CE SE ÎNTÂMPLĂ: Orbita satelitului nu mai rămâne un cerc perfect, ci devine o ELIPSĂ 
    //    (deformată direct proporțional cu scara părintelui), iar dimensiunea satelitului se deformează și ea.
    // 2. DE CE: Matricea model a satelitului se obține prin înmulțirea transformării locale cu matricea model 
    //    a părintelui (localMatrix * parent.ModelMatrix). Deoarece matricea părintelui conține deja transformarea 
    //    de scalare, toate punctele din spațiul local al satelitului (atât distanța sa față de centru/raza orbitei, 
    //    cât și forma sa proprie) sunt multiplicate cu factorii Scale.X și Scale.Y ai părintelui.
}
