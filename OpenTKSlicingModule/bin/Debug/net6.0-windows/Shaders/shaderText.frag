#version 330 core
out vec4 FragColor;
in vec2 texCoord;

uniform sampler2D texture0;

void main()
{
    vec4 col = texture(texture0, texCoord);
    if(col.a < 0.1) discard;
    FragColor = col;
}