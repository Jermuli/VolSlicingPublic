#version 330 core

layout(location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec3 outColor;

void main(void)
{
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;
    outColor = aColor;
}