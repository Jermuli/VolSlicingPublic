#version 330 core

layout(location = 0) in vec3 aPosition;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec4 FragPos;


void main(void)
{
    FragPos = vec4(aPosition, 1.0) * model;
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;
}