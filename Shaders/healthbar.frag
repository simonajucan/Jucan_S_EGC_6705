#version 330 core

in vec3 vColor;
in vec2 vUV;

uniform bool uShowUV;
uniform float uTime;
uniform float uHealth;       // viata curenta [0, 1]
uniform float uTrailHealth;  // urma vietii pierdute [0, 1]
uniform int uSegments;       // numarul de segmente
uniform float uPulseSpeed;   // viteza pulsatiei sub 25%
uniform float uFlash;        // intensitatea clipirii la lovitura [0, 1]
uniform vec2 uAspect;        // raportul latime/inaltime al barei

out vec4 FragColor;

float sdRoundedBox(vec2 p, vec2 b, float r)
{
    vec2 q = abs(p) - b + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    vec2 p = (vUV * 2.0 - 1.0) * uAspect;
    vec2 halfSize = uAspect - vec2(0.05);
    float radius = 0.25;

    float dOuter = sdRoundedBox(p, halfSize, radius);
    float aa = fwidth(dOuter);
    float outerMask = 1.0 - smoothstep(-aa, aa, dOuter);

    if (outerMask < 0.01)
        discard;

    float border = 0.12;
    float dInner = sdRoundedBox(p, halfSize - vec2(border), max(0.05, radius - border));
    float innerMask = 1.0 - smoothstep(-aa, aa, dInner);

    float innerWidth = (halfSize.x - border) * 2.0;
    float xNorm = clamp((p.x + (halfSize.x - border)) / innerWidth, 0.0, 1.0);

    // Separatoare intre segmente
    float segPos = fract(xNorm * float(uSegments));
    float sepWidth = 0.05;
    float segMask = smoothstep(0.0, sepWidth, segPos) * smoothstep(1.0, 1.0 - sepWidth, segPos);
    if (uSegments <= 1) segMask = 1.0;

    // Culoarea in functie de viata (rosu -> galben -> verde)
    vec3 colRed    = vec3(0.92, 0.18, 0.18);
    vec3 colYellow = vec3(0.95, 0.80, 0.15);
    vec3 colGreen  = vec3(0.20, 0.85, 0.35);

    vec3 barColor = mix(colRed, colYellow, smoothstep(0.2, 0.5, uHealth));
    barColor = mix(barColor, colGreen, smoothstep(0.5, 0.85, uHealth));

    // Pulsatie cand viata este sub 25%
    if (uHealth < 0.25 && uHealth > 0.0)
    {
        float pulse = 0.60 + 0.40 * sin(uTime * uPulseSpeed);
        barColor *= pulse;
    }

    float fillMask  = 1.0 - smoothstep(uHealth - 0.005, uHealth + 0.005, xNorm);
    float trailMask = 1.0 - smoothstep(uTrailHealth - 0.005, uTrailHealth + 0.005, xNorm);

    vec3 bgColor    = vec3(0.10, 0.11, 0.13);
    vec3 trailColor = vec3(0.95, 0.90, 0.80);

    vec3 interior = bgColor;
    interior = mix(interior, trailColor, trailMask * segMask);
    interior = mix(interior, barColor, fillMask * segMask);
    interior = mix(interior, vec3(1.0), uFlash * 0.75);

    vec3 frameColor = mix(vec3(0.85), vec3(1.0), uFlash);
    vec3 finalColor = mix(frameColor, interior, innerMask);

    FragColor = vec4(finalColor * vColor, outerMask);
}