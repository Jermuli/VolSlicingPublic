#version 330 core

layout(location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor;

uniform mat4 model;
uniform vec3 offset;

out vec3 outColor;

void main(void)
{
    gl_Position = vec4(aPosition, 1.0) * model + vec4(offset, 0);
    outColor = aColor;
}