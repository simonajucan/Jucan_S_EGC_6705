using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Varianta B: bara de viata si scutul
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class HealthShieldScene : Scene
{
    private const string HitAction = "lovitură (scade viața)";
    private const string HealAction = "vindecare (crește viața)";
    private const string ShieldAction = "scut: pornit / oprit";

    private Material _healthMat = null!;
    private Material _shieldMat = null!;
    private SceneObject _player = null!;
    private SceneObject _shieldObj = null!;

    private float _targetHealth = 1.0f;
    private float _flash = 0.0f;
    private float _shieldFlash = 0.0f;
    private bool _shieldActive = true;

    private Tween _healthTween = null!;
    private Tween _trailTween = null!;
    private Tween _shieldTween = null!;

    public override string Name => "bara de viață și scutul";

    public override IEnumerable<string> ActionNames => new[] { HitAction, HealAction, ShieldAction };

    public override string Status => $"viață: {_targetHealth * 100:F0}% | scut: {(_shieldActive ? "ON" : "OFF")}";

    protected override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 2.2f, 7.5f);
        CameraTarget = new Vector3(0.0f, 1.8f, 0.0f);

        // Tastele proprii ale scenei
        input.Bind(HitAction, Keys.Z, Keys.KeyPad1);
        input.Bind(HealAction, Keys.X, Keys.KeyPad2);
        input.Bind(ShieldAction, Keys.Space);

        // Forme geometrice
        Mesh grid = AddMesh(Shapes.Grid(10, 1.0f, new Color4(0.30f, 0.33f, 0.38f, 1.0f)));
        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh sphere = AddMesh(Shapes.ShadedSphere(1.15f, 48, 24));
        Mesh barQuad = AddMesh(Shapes.Quad(2.4f, 0.45f));

        AddObject("podea", grid);

        // 1. Materialul barei de viata (transparent pentru colturile rotunjite)
        _healthMat = AddMaterial("bară viață", "procedural.vert", "healthbar.frag")
            .Param("uSegments", 1, 20, 10)
            .Param("uPulseSpeed", 1.0f, 15.0f, 8.0f);
        _healthMat.Transparent = true;
        _healthMat.Uniforms.Set("uHealth", 1.0f);
        _healthMat.Uniforms.Set("uTrailHealth", 1.0f);
        _healthMat.Uniforms.Set("uFlash", 0.0f);
        _healthMat.Uniforms.Set("uAspect", new Vector2(2.4f / 0.45f, 1.0f));

        // 2. Materialul scutului (transparent, cu benzi de energie animate)
        _shieldMat = AddMaterial("scut", "procedural.vert", "shield.frag")
            .Param("uBandFreq", 2.0f, 20.0f, 8.0f)
            .Param("uBandSpeed", 0.0f, 3.0f, 0.6f);
        _shieldMat.Transparent = true;
        _shieldMat.Uniforms.Set("uShieldAlpha", 1.0f);
        _shieldMat.Uniforms.Set("uShieldFlash", 0.0f);
        _shieldMat.Uniforms.Set("uShieldColor", new Vector3(0.20f, 0.75f, 1.0f));

        // 3. Personajul (cubul)
        _player = AddObject("personaj", cube);
        _player.Transform.Position = new Vector3(0.0f, 1.35f, 0.0f);
        _player.Transform.Scale = new Vector3(0.85f);
        _player.Tint = new Color4(0.35f, 0.60f, 0.90f, 1.0f);
        _player.Spin = new Vector3(0.0f, 18.0f, 0.0f);

        // 4. Scutul (sfera transparenta centrata pe personaj)
        _shieldObj = AddObject("scut", sphere, _shieldMat);
        _shieldObj.Transform.Position = _player.Transform.Position;

        // 5. Bonus: bara de viata afisata deasupra personajului, ca obiect-copil (ierarhie)
        SceneObject anchor = AddObject("ancoră bară", null);
        anchor.Transform.Position = _player.Transform.Position;

        SceneObject healthBar = AddObject("bară viață", barQuad, anchor);
        healthBar.Material = _healthMat;
        healthBar.Transform.Position = new Vector3(0.0f, 1.85f, 0.0f);

        // Tween-uri folosind Vector3 (valoarea este stocata pe componenta X)
        var one = new Vector3(1.0f, 0.0f, 0.0f);
        _healthTween = new Tween(one, one, 0.25f, Easing.EaseOut);
        _trailTween = new Tween(one, one, 0.85f, Easing.EaseInOut);
        _shieldTween = new Tween(one, one, 0.4f, Easing.EaseInOut);
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        // Pornire / oprire lina a scutului cu Space
        if (input.WasPressed(ShieldAction))
        {
            _shieldActive = !_shieldActive;
            _shieldTween.GoTo(new Vector3(_shieldActive ? 1.0f : 0.0f, 0.0f, 0.0f), 0.4f);
            ConsolePanel.PrintMessage($"scut: {(_shieldActive ? "pornit" : "oprit")}");
        }

        // Lovitura (Z): scutul reduce daunele de 3 ori si clipeste
        if (input.WasPressed(HitAction))
        {
            float damage = _shieldActive ? 0.06f : 0.18f;
            _targetHealth = MathF.Max(0.0f, _targetHealth - damage);

            var targetVec = new Vector3(_targetHealth, 0.0f, 0.0f);
            _healthTween.GoTo(targetVec, 0.20f);
            _trailTween.GoTo(targetVec, 0.90f);

            _flash = 1.0f;
            if (_shieldActive)
            {
                _shieldFlash = 1.0f;
            }
            ConsolePanel.PrintMessage($"lovitură (-{damage * 100:F0}%) -> viață: {_targetHealth * 100:F0}%");
        }

        // Vindecare (X)
        if (input.WasPressed(HealAction))
        {
            float heal = 0.20f;
            _targetHealth = MathF.Min(1.0f, _targetHealth + heal);

            var targetVec = new Vector3(_targetHealth, 0.0f, 0.0f);
            _healthTween.GoTo(targetVec, 0.35f);
            _trailTween.GoTo(targetVec, 0.35f);
            ConsolePanel.PrintMessage($"vindecare (+{heal * 100:F0}%) -> viață: {_targetHealth * 100:F0}%");
        }

        // Scadere exponentiala a flash-ului (independenta de FPS: flash *= exp(-k * dt))
        const float decayRate = 8.0f;
        _flash *= MathF.Exp(-decayRate * dt);
        _shieldFlash *= MathF.Exp(-decayRate * dt);

        // Actualizare tween-uri
        _healthTween.Update(dt);
        _trailTween.Update(dt);
        _shieldTween.Update(dt);

        // Trimitem valorile actualizate (.Value.X) catre shadere
        float currentHealth = _healthTween.Value.X;
        float currentTrail = _trailTween.Value.X;

        _healthMat.Uniforms.Set("uHealth", currentHealth);
        _healthMat.Uniforms.Set("uTrailHealth", MathF.Max(currentHealth, currentTrail));
        _healthMat.Uniforms.Set("uFlash", _flash);

        _shieldMat.Uniforms.Set("uShieldAlpha", _shieldTween.Value.X);
        _shieldMat.Uniforms.Set("uShieldFlash", _shieldFlash);
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Viață curentă  : {_targetHealth * 100:F0}%";
        yield return $"Scut energetic : {(_shieldActive ? "activ" : "dezactivat")}";
    }
}