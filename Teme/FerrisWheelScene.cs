// Varianta C: roata panoramică
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class FerrisWheelScene : Scene
{
    private const string ActSpeedUp = "crește viteza țintă";
    private const string ActSpeedDown = "scade viteza țintă";
    private const string ActStop = "oprește roata lin";
    private static readonly string[] ActCabin =
    {
        "adu cabina 1 la peron",
        "adu cabina 2 la peron",
        "adu cabina 3 la peron",
        "adu cabina 4 la peron",
        "adu cabina 5 la peron",
        "adu cabina 6 la peron",
        "adu cabina 7 la peron",
        "adu cabina 8 la peron",
    };

    private const int CabinCount = 8;
    private const float WheelRadius = 3.2f;
    private const float HubHeight = 4.2f;

    private SceneObject _hub = null!;
    private readonly SceneObject[] _cabinPivots = new SceneObject[CabinCount];

    private float _wheelAngle;        // unghiul curent al roții (grade)
    private float _currentSpeed;      // viteza reală (grade/s)
    private float _targetSpeed = 30f; // viteza țintă (grade/s)

    // Bonus: balansul cabinelor (resort amortizat)
    private float _swingAngle;
    private float _swingVelocity;

    // Tween pentru aducerea unei cabine la peron
    private bool _isDocking;
    private int _targetCabin = -1;
    private float _dockStartAngle;
    private float _dockEndAngle;
    private float _dockElapsed;
    private const float DockDuration = 2.0f;

    public override string Name => "Varianta C: Roata panoramică";

    public override IEnumerable<string> ActionNames
    {
        get
        {
            yield return ActSpeedUp;
            yield return ActSpeedDown;
            yield return ActStop;
            foreach (string act in ActCabin)
                yield return act;
        }
    }

    public override void Load(Input input)
    {
        // Legarea acțiunilor (fără citire directă din KeyboardState în Update)
        input.Bind(ActSpeedUp, Keys.Up);
        input.Bind(ActSpeedDown, Keys.Down);
        input.Bind(ActStop, Keys.Space);

        for (int i = 0; i < CabinCount; i++)
        {
            input.Bind(ActCabin[i], Keys.D1 + i);
        }

        // Poziționare cameră
        CameraPosition = new Vector3(0.0f, 4.8f, 12.0f);
        CameraTarget = new Vector3(0.0f, 3.6f, 0.0f);

        BuildWheel();
    }

    private void BuildWheel()
    {
        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh grid = AddMesh(Shapes.Grid(10, 1.0f, new Color4(0.28f, 0.30f, 0.35f, 1.0f)));
        Mesh axes = AddMesh(Shapes.Axes(1.5f));

        AddObject("Podea", grid);
        AddObject("Axe", axes);

        // Baza / Peronul
        var basePlatform = AddObject("Peron", cube);
        basePlatform.Tint = new Color4(0.55f, 0.38f, 0.22f, 1.0f);
        basePlatform.Transform.Position = new Vector3(0.0f, 0.125f, 0.0f);
        basePlatform.Transform.Scale = new Vector3(3.6f, 0.25f, 2.4f);

        // Stâlpii de susținere (față și spate)
        AddSupportLegs(cube, -0.85f);
        AddSupportLegs(cube, 0.85f);

        // Butucul (pivotul central al roții)
        _hub = AddObject("Butuc", null);
        _hub.Transform.Position = new Vector3(0.0f, HubHeight, 0.0f);

        var axle = AddObject("Ax", cube, _hub);
        axle.Tint = new Color4(0.35f, 0.35f, 0.40f, 1.0f);
        axle.Transform.Scale = new Vector3(0.35f, 0.35f, 2.0f);

        // Janta (poligon cu 32 de segmente)
        int rimSegments = 32;
        float segLength = 2.0f * MathF.PI * WheelRadius / rimSegments + 0.05f;
        for (int s = 0; s < rimSegments; s++)
        {
            float deg = s * (360.0f / rimSegments);
            float rad = MathHelper.DegreesToRadians(deg);
            Vector3 pos = new(MathF.Cos(rad) * WheelRadius, MathF.Sin(rad) * WheelRadius, 0.0f);

            foreach (float zOffset in new[] { -0.55f, 0.55f })
            {
                var rimPiece = AddObject($"Janta_{s}_{zOffset}", cube, _hub);
                rimPiece.Tint = new Color4(0.82f, 0.84f, 0.88f, 1.0f);
                rimPiece.Transform.Position = pos + new Vector3(0.0f, 0.0f, zOffset);
                rimPiece.Transform.Rotation = new Vector3(0.0f, 0.0f, deg);
                rimPiece.Transform.Scale = new Vector3(0.12f, segLength, 0.12f);
            }
        }

        // Culori distincte pentru cele 8 cabine
        Color4[] palette =
        {
            new(0.95f, 0.30f, 0.30f, 1.0f), // 1 - Roșu
            new(0.95f, 0.60f, 0.20f, 1.0f), // 2 - Portocaliu
            new(0.95f, 0.85f, 0.25f, 1.0f), // 3 - Galben
            new(0.35f, 0.80f, 0.40f, 1.0f), // 4 - Verde
            new(0.25f, 0.80f, 0.85f, 1.0f), // 5 - Cyan
            new(0.30f, 0.50f, 0.95f, 1.0f), // 6 - Albastru
            new(0.65f, 0.35f, 0.90f, 1.0f), // 7 - Mov
            new(0.95f, 0.40f, 0.75f, 1.0f), // 8 - Roz
        };

        // Spițele și cele 8 cabine
        for (int i = 0; i < CabinCount; i++)
        {
            // Cabina 0 (tasta 1) pornește jos (-90 grade)
            float angleDeg = -90.0f + i * (360.0f / CabinCount);
            float rad = MathHelper.DegreesToRadians(angleDeg);
            Vector3 rimPoint = new(MathF.Cos(rad) * WheelRadius, MathF.Sin(rad) * WheelRadius, 0.0f);

            // Spițe dublate (față/spate)
            foreach (float zOffset in new[] { -0.55f, 0.55f })
            {
                var spoke = AddObject($"Spita_{i}_{zOffset}", cube, _hub);
                spoke.Tint = new Color4(0.70f, 0.72f, 0.76f, 1.0f);
                spoke.Transform.Position = rimPoint * 0.5f + new Vector3(0.0f, 0.0f, zOffset);
                spoke.Transform.Rotation = new Vector3(0.0f, 0.0f, angleDeg);
                spoke.Transform.Scale = new Vector3(WheelRadius, 0.07f, 0.07f);
            }

            // Pivotul cabinei (copil al butucului)
            var cabinPivot = AddObject($"PivotCabina_{i + 1}", null, _hub);
            cabinPivot.Transform.Position = rimPoint;
            _cabinPivots[i] = cabinPivot;

            // Brațul de prindere și corpul cabinei (copii ai pivotului cabinei)
            var hanger = AddObject($"BratCabina_{i + 1}", cube, cabinPivot);
            hanger.Tint = new Color4(0.25f, 0.25f, 0.30f, 1.0f);
            hanger.Transform.Position = new Vector3(0.0f, -0.22f, 0.0f);
            hanger.Transform.Scale = new Vector3(0.08f, 0.45f, 0.08f);

            var cabinBody = AddObject($"Cabina_{i + 1}", cube, cabinPivot);
            cabinBody.Tint = palette[i % palette.Length];
            cabinBody.Transform.Position = new Vector3(0.0f, -0.55f, 0.0f);
            cabinBody.Transform.Scale = new Vector3(0.65f, 0.55f, 0.75f);
        }
    }

    private void AddSupportLegs(Mesh cube, float z)
    {
        float legLength = MathF.Sqrt(HubHeight * HubHeight + 1.4f * 1.4f);
        float tiltDeg = MathHelper.RadiansToDegrees(MathF.Atan2(1.4f, HubHeight));

        var leftLeg = AddObject($"PiciorStanga_{z}", cube);
        leftLeg.Tint = new Color4(0.45f, 0.48f, 0.52f, 1.0f);
        leftLeg.Transform.Position = new Vector3(-0.7f, HubHeight * 0.5f, z);
        leftLeg.Transform.Rotation = new Vector3(0.0f, 0.0f, -tiltDeg);
        leftLeg.Transform.Scale = new Vector3(0.16f, legLength, 0.16f);

        var rightLeg = AddObject($"PiciorDreapta_{z}", cube);
        rightLeg.Tint = new Color4(0.45f, 0.48f, 0.52f, 1.0f);
        rightLeg.Transform.Position = new Vector3(0.7f, HubHeight * 0.5f, z);
        rightLeg.Transform.Rotation = new Vector3(0.0f, 0.0f, tiltDeg);
        rightLeg.Transform.Scale = new Vector3(0.16f, legLength, 0.16f);
    }

    public override void Update(float dt, Input input)
    {
        // 1. Verificăm tastele 1-8 pentru aducerea cabinei la peron
        for (int i = 0; i < CabinCount; i++)
        {
            if (input.WasPressed(ActCabin[i]))
            {
                StartDocking(i);
            }
        }

        // 2. Modificarea vitezei țintă cu Săgeți Sus / Jos sau oprire cu Space
        if (input.IsDown(ActSpeedUp))
        {
            _isDocking = false;
            _targetSpeed = Math.Clamp(_targetSpeed + 45.0f * dt, -120.0f, 120.0f);
        }
        if (input.IsDown(ActSpeedDown))
        {
            _isDocking = false;
            _targetSpeed = Math.Clamp(_targetSpeed - 45.0f * dt, -120.0f, 120.0f);
        }
        if (input.WasPressed(ActStop))
        {
            _isDocking = false;
            _targetSpeed = 0.0f;
        }

        float prevSpeed = _currentSpeed;

        // 3. Actualizarea unghiului roții (fie prin Tween de andocare, fie prin viteză amortizată)
        if (_isDocking)
        {
            _dockElapsed += dt;
            float t = Math.Clamp(_dockElapsed / DockDuration, 0.0f, 1.0f);
            float eased = Easing.EaseInOut(t);

            float prevAngle = _wheelAngle;
            _wheelAngle = Easing.Lerp(_dockStartAngle, _dockEndAngle, eased);
            _currentSpeed = dt > 0.0001f ? (_wheelAngle - prevAngle) / dt : 0.0f;

            if (t >= 1.0f)
            {
                _isDocking = false;
                _currentSpeed = 0.0f;
                _targetSpeed = 0.0f;
            }
        }
        else
        {
            // Formula cerută: v <- v + (v_țintă - v) * (1 - e^(-k * dt))
            const float k = 3.0f;
            float factor = 1.0f - MathF.Exp(-k * dt);
            _currentSpeed += (_targetSpeed - _currentSpeed) * factor;
            _wheelAngle += _currentSpeed * dt;

            _wheelAngle %= 360.0f;
            if (_wheelAngle < 0.0f) _wheelAngle += 360.0f;
        }

        // 4. Bonus: balansul cabinelor la accelerație / frânare (resort amortizat)
        float angularAccel = dt > 0.0001f ? (_currentSpeed - prevSpeed) / dt : 0.0f;
        const float springK = 28.0f;
        const float damping = 3.5f;
        float swingAccel = -angularAccel * 0.25f - springK * _swingAngle - damping * _swingVelocity;
        _swingVelocity += swingAccel * dt;
        _swingAngle += _swingVelocity * dt;
        _swingAngle = Math.Clamp(_swingAngle, -35.0f, 35.0f);

        // 5. Aplicăm transformările în ierarhie
        _hub.Transform.Rotation = new Vector3(0.0f, 0.0f, _wheelAngle);

        for (int i = 0; i < CabinCount; i++)
        {
            // Fiecare cabină se rotește invers față de roată (-_wheelAngle) + balansul bonus
            _cabinPivots[i].Transform.Rotation = new Vector3(0.0f, 0.0f, -_wheelAngle + _swingAngle);
        }
    }

    private void StartDocking(int cabinIndex)
    {
        _isDocking = true;
        _targetCabin = cabinIndex;
        _targetSpeed = 0.0f;
        _dockElapsed = 0.0f;

        // Cabina i se află la unghiul inițial (-90 + i * 45).
        // Ca să ajungă jos (-90), roata trebuie să aibă unghiul: -i * 45 grade.
        float desiredAngle = -cabinIndex * (360.0f / CabinCount);

        // Calculăm cel mai scurt drum unghiular în intervalul [-180, +180]
        float delta = (desiredAngle - _wheelAngle) % 360.0f;
        if (delta > 180.0f) delta -= 360.0f;
        if (delta < -180.0f) delta += 360.0f;

        _dockStartAngle = _wheelAngle;
        _dockEndAngle = _wheelAngle + delta;
    }

    public override string Status =>
        _isDocking
            ? $"Andocare cabina {_targetCabin + 1} | Unghi: {_wheelAngle:0.0}°"
            : $"Viteză: {_currentSpeed:0.0}°/s (țintă: {_targetSpeed:0.0}°/s)";

    public override IEnumerable<string> Describe()
    {
        yield return $"Roata panoramică: {CabinCount} cabine, rază {WheelRadius:0.0} m";
        yield return $"Unghi butuc    : {_wheelAngle:0.0}° | Viteză: {_currentSpeed:0.0}°/s (țintă: {_targetSpeed:0.0}°/s)";
        yield return _isDocking
            ? $"Mod activ      : aducere cabina {_targetCabin + 1} la peron ({_dockEndAngle - _dockStartAngle:+0.0;-0.0}°)"
            : "Mod activ      : rotație liberă";
    }
}