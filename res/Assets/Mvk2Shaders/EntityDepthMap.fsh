#version 330 core
 
in vec2 a_texCoord;
in float a_depth;
in float a_eyeLips;

out vec4 f_color;

uniform sampler2DArray sampler_small;
uniform sampler2DArray sampler_big;

void main()
{
    if (a_eyeLips > 0) discard;
    float depth = a_depth;
    bool big = depth > 65535;
    if (big) depth -= 65536;
    vec3 uv = vec3(a_texCoord, depth);
    vec4 tex_color;
    if (big) tex_color = texture(sampler_big, uv);
    else tex_color = texture(sampler_small, uv);
    if (tex_color.a < 0.1) discard;
    f_color = tex_color;
}