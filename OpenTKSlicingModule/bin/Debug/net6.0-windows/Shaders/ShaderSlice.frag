#version 330 core
out vec4 FragColor;

uniform vec3 CubeDims;
uniform sampler2D texture0;
uniform float LvlMax;
uniform float LvlMin;

in vec4 FragPos;
in vec2 texCoord;

void main()
{
    if(abs(FragPos.x) >= CubeDims.x || abs(FragPos.y) >= CubeDims.y || abs(FragPos.z) >= CubeDims.z) discard;
    else {
        float intensity = texture(texture0, texCoord).x;
        if(intensity < LvlMin) intensity = 0.0;
        else if (intensity > LvlMax) intensity = 1.0;
        else intensity = (intensity-LvlMin)*((1)/(LvlMax-LvlMin));
        vec4 col = vec4(intensity, intensity, intensity,1);
        FragColor = col;
    }

    //Test to see whole slice
    //if(abs(FragPos.x) >= CubeDims.x || abs(FragPos.y) >= CubeDims.y || abs(FragPos.z) >= CubeDims.z) float temp = 1;
    //float intensity = texture(texture0, texCoord).x;
    //if(intensity < LvlMin) intensity = 0.0;
    //else if (intensity > LvlMax) intensity = 1.0;
    //else intensity = (intensity-LvlMin)*((1)/(LvlMax-LvlMin));
    //vec4 col = vec4(1, 1, 1,1);
    //FragColor = col;
}