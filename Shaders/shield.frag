#version 330 core

in vec3 vColor;
in vec2 vUV;

uniform bool uShowUV;
uniform float uTime;
uniform float uShieldAlpha;  // 0 = oprit, 1 = pornit
uniform float uShieldFlash;  // clipire la lovitura [0, 1]
uniform float uBandFreq;     // frecventa benzilor de energie
uniform float uBandSpeed;    // viteza de animatie a benzilor
uniform vec3 uShieldColor;   // culoarea scutului

out vec4 FragColor;

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    if (uShieldAlpha <= 0.001)
        discard;

    float wave1 = 0.5 + 0.5 * sin(6.2831853 * (vUV.y * uBandFreq - uTime * uBandSpeed));
    float wave2 = 0.5 + 0.5 * sin(6.2831853 * (vUV.x * uBandFreq * 2.0 + uTime * uBandSpeed * 0.5));

    float grid = smoothstep(0.65, 0.95, wave1) + 0.5 * smoothstep(0.75, 0.98, wave2);
    grid = clamp(grid, 0.0, 1.0);

    vec3 col = mix(uShieldColor * 0.5, uShieldColor * 1.4, grid);
    col = mix(col, vec3(1.0), uShieldFlash * 0.85);

    float alpha = mix(0.10, 0.65, grid) + uShieldFlash * 0.35;
    alpha *= uShieldAlpha;

    FragColor = vec4(col * vColor, clamp(alpha, 0.0, 0.95));
}