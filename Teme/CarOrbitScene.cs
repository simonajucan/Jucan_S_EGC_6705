using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class CarOrbitScene : Scene
{
    private const string ActToggleMove = "pornește / oprește mașina";
    private const string ActFaster = "mărește viteza mașinii";
    private const string ActSlower = "scade viteza mașinii";

    private SceneObject _car = null!;
    private SceneObject _orbitPivot = null!;
    private readonly SceneObject[] _wheelRollPivots = new SceneObject[4];

    private float _carTime;
    private float _wheelAngle;
    private float _speedMultiplier = 1.0f;
    private bool _moving = true;

    private const float RoadHalfLength = 6.5f;
    private const float WheelRadius = 0.35f;
    private const float OrbitRadius = 2.4f;

    public override string Name => "Mașină cu sferă pe orbită";

    public override IEnumerable<string> ActionNames
    {
        get
        {
            yield return ActToggleMove;
            yield return ActFaster;
            yield return ActSlower;
        }
    }

    public override void Load(Input input)
    {
        input.Bind(ActToggleMove, Keys.Space);
        input.Bind(ActFaster, Keys.Up);
        input.Bind(ActSlower, Keys.Down);

        CameraPosition = new Vector3(0.0f, 6.0f, 13.0f);
        CameraTarget = new Vector3(0.0f, 1.0f, 0.0f);

        BuildScene();
    }

    private void BuildScene()
    {
        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh sphereMesh = AddMesh(Shapes.ShadedSphere(0.35f, 24, 16));
        Mesh wheelMesh = AddMesh(Shapes.ShadedCylinder(WheelRadius, 0.22f, 20));
        Mesh grid = AddMesh(Shapes.Grid(12, 1.0f, new Color4(0.28f, 0.30f, 0.35f, 1.0f)));

        AddObject("Podea", grid);

        // Drumul pe axa X
        var road = AddObject("Drum", cube);
        road.Tint = new Color4(0.22f, 0.24f, 0.28f, 1.0f);
        road.Transform.Position = new Vector3(0.0f, 0.02f, 0.0f);
        road.Transform.Scale = new Vector3(RoadHalfLength * 2.4f, 0.04f, 2.6f);

        // Marcaj central întrerupt
        for (float x = -RoadHalfLength; x <= RoadHalfLength; x += 2.0f)
        {
            var stripe = AddObject($"Marcaj_{x}", cube);
            stripe.Tint = new Color4(0.95f, 0.85f, 0.25f, 1.0f);
            stripe.Transform.Position = new Vector3(x, 0.05f, 0.0f);
            stripe.Transform.Scale = new Vector3(0.9f, 0.02f, 0.14f);
        }

        // --- Mașina (părintele principal) ---
        _car = AddObject("Masina", null);
        _car.Transform.Position = new Vector3(0.0f, WheelRadius, 0.0f);

        // Șasiul
        var chassis = AddObject("Sasiu", cube, _car);
        chassis.Tint = new Color4(0.20f, 0.60f, 0.95f, 1.0f);
        chassis.Transform.Position = new Vector3(0.0f, 0.30f, 0.0f);
        chassis.Transform.Scale = new Vector3(2.4f, 0.45f, 1.2f);

        // Cabina mașinii
        var cabin = AddObject("Cabina", cube, _car);
        cabin.Tint = new Color4(0.75f, 0.90f, 1.0f, 1.0f);
        cabin.Transform.Position = new Vector3(-0.15f, 0.72f, 0.0f);
        cabin.Transform.Scale = new Vector3(1.25f, 0.42f, 1.05f);

        // Cele 4 roți:
        // 1. Pivotul roții (copil al mașinii) se rotește pe axa Z (rostogolire înainte/înapoi)
        // 2. Cilindrul roții (copil al pivotului) stă culcat cu Rotation.X = 90° și are o spiță vizibilă
        Vector3[] wheelOffsets =
        {
            new( 0.75f, 0.0f,  0.65f), // față-dreapta
            new( 0.75f, 0.0f, -0.65f), // față-stânga
            new(-0.75f, 0.0f,  0.65f), // spate-dreapta
            new(-0.75f, 0.0f, -0.65f), // spate-stânga
        };

        for (int i = 0; i < 4; i++)
        {
            var rollPivot = AddObject($"PivotRoata_{i + 1}", null, _car);
            rollPivot.Transform.Position = wheelOffsets[i];
            _wheelRollPivots[i] = rollPivot;

            var wheel = AddObject($"Roata_{i + 1}", wheelMesh, rollPivot);
            wheel.Tint = new Color4(0.20f, 0.20f, 0.22f, 1.0f);
            wheel.Transform.Rotation = new Vector3(90.0f, 0.0f, 0.0f);

            // Capac / spiță vizibilă ca să se observe clar sensul de rotație
            float capZ = wheelOffsets[i].Z > 0 ? 0.12f : -0.12f;
            var spoke = AddObject($"SpitaRoata_{i + 1}", cube, rollPivot);
            spoke.Tint = new Color4(0.85f, 0.85f, 0.90f, 1.0f);
            spoke.Transform.Position = new Vector3(0.0f, 0.0f, capZ);
            spoke.Transform.Scale = new Vector3(WheelRadius * 1.5f, 0.08f, 0.03f);
        }

        // --- Pivotul sferei (copil al mașinii -> o urmărește oriunde merge) ---
        _orbitPivot = AddObject("PivotSfera", null, _car);
        _orbitPivot.Transform.Position = new Vector3(0.0f, 0.7f, 0.0f);
        _orbitPivot.Spin = new Vector3(0.0f, 140.0f, 0.0f);

        var sphere = AddObject("SferaOrbitala", sphereMesh, _orbitPivot);
        sphere.Tint = new Color4(1.0f, 0.45f, 0.15f, 1.0f);
        sphere.Transform.Position = new Vector3(OrbitRadius, 0.0f, 0.0f);
    }

    public override void Update(float dt, Input input)
    {
        if (input.WasPressed(ActToggleMove)) _moving = !_moving;
        if (input.IsDown(ActFaster)) _speedMultiplier = Math.Clamp(_speedMultiplier + 1.5f * dt, 0.2f, 4.0f);
        if (input.IsDown(ActSlower)) _speedMultiplier = Math.Clamp(_speedMultiplier - 1.5f * dt, 0.2f, 4.0f);

        if (_moving)
        {
            float prevX = _car.Transform.Position.X;

            // Mișcare lină înainte (+X) și înapoi (-X)
            _carTime += dt * _speedMultiplier;
            float newX = MathF.Sin(_carTime * 0.9f) * RoadHalfLength;
            float dx = newX - prevX;

            _car.Transform.Position = new Vector3(newX, WheelRadius, 0.0f);

            // Când mașina merge în față (dx > 0), unghiul pe Z scade -> sens orar
            // Când mașina merge în spate (dx < 0), unghiul pe Z crește -> sens trigonometric
            _wheelAngle -= MathHelper.RadiansToDegrees(dx / WheelRadius);

            foreach (SceneObject pivot in _wheelRollPivots)
            {
                pivot.Transform.Rotation = new Vector3(0.0f, 0.0f, _wheelAngle);
            }
        }

        _orbitPivot.Update(dt);
    }

    public override string Status =>
        $"{(_moving ? "În mișcare" : "Oprită")} | X = {_car.Transform.Position.X:0.0} m | Viteză ×{_speedMultiplier:0.0}";

    public override IEnumerable<string> Describe()
    {
        yield return $"Mașina         : poziție X = {_car.Transform.Position.X:0.00} m (cursă ±{RoadHalfLength} m)";
        yield return $"Sferă orbitală : rază {OrbitRadius:0.0} m, viteză rotație {_orbitPivot.Spin.Y:0}°/s";
    }
}