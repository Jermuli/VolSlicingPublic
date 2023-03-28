#version 330 core
out vec4 FragColor;

uniform vec3 CubeDims;

in vec4 FragPos;

void main()
{
    if(abs(FragPos.x) >= CubeDims.x || abs(FragPos.y) >= CubeDims.y || abs(FragPos.z) >= CubeDims.z) discard;
    else FragColor = vec4(0.5, 0.5, 0.5, 1.0);
}