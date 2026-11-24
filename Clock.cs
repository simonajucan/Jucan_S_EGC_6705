namespace Egc;

/// <summary>
/// NOU: ceasul aplicației (secțiunea 2). Primește la fiecare cadru timpul REAL scurs
/// (args.Time din OnUpdateFrame) și calculează timpul de JOC (DeltaTime, TotalTime),
/// pe care îl folosesc animațiile: poate fi pus pe pauză, încetinit sau accelerat.
/// </summary>
public sealed class Clock
{
    /// <summary>Cel mai mare pas de timp acceptat, în secunde (vezi secțiunea 2.2).</summary>
    public const float MaxDelta = 0.1f;

    public const float MinScale = 0.125f;
    public const float MaxScale = 4.0f;

    /// <summary>Timpul real scurs de la cadrul anterior, în secunde (nemodificat).</summary>
    public float RealDeltaTime { get; private set; }

    /// <summary>Timpul de joc scurs de la cadrul anterior: limitat, scalat, 0 în pauză.</summary>
    public float DeltaTime { get; private set; }

    /// <summary>Timpul de joc total, în secunde (se oprește în pauză).</summary>
    public double TotalTime { get; private set; }

    /// <summary>Scara timpului: 1 = normal, 0.5 = de două ori mai încet, 2 = de două ori mai repede.</summary>
    public float TimeScale { get; private set; } = 1.0f;

    public bool Paused { get; set; }

    /// <summary>Apelată O DATĂ pe cadru, la începutul lui Game.OnUpdateFrame.</summary>
    public void Tick(double realSeconds)
    {
        RealDeltaTime = (float)realSeconds;

        // TODO 6.1: timpul de joc
        //   1. limitați pasul real la MaxDelta (Math.Min / MathF.Min);

        float clempedDelta = MathF.Min(RealDeltaTime, MaxDelta);
        //   2. în pauză pasul de joc este 0, altfel este pasul limitat înmulțit cu TimeScale.
        if (Paused)
        {
            DeltaTime = 0;
        }
        else
        {
            DeltaTime = clempedDelta * TimeScale;
        }
        //DeltaTime = RealDeltaTime;   // provizoriu: timpul de joc = timpul real

        TotalTime += DeltaTime;
    }

    // intrebare:
    //ce s-ar intampla cu un obiect care cade sub gravitatie, daca pasul nu ar fi limitat?
    //R: Am avea o eroare uriasa de integrare(Tunneling), obiectul va sari peste o distanta uriasa intr-o singura
    //fractiune de secunda, trecand direct prin podea sau prin alte coliziuni fara ca sistemul de fizica sa le poata detecta, si va face 
    //ulterior ca obiectul sa prinda o viteza nerealista la primul cadru dupa deblocare

    /// <summary>Înmulțește scara timpului cu <paramref name="factor"/> (de exemplu 2 sau 0.5), între limite.</summary>
    public void ChangeScale(float factor)
    {
        TimeScale = Math.Clamp(TimeScale * factor, MinScale, MaxScale);
    }

    public void ResetScale() => TimeScale = 1.0f;
}
