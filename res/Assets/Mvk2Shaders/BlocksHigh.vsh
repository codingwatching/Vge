#version 330 core

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in int v_rgbl;
layout(location = 3) in int v_anim;
layout(location = 4) in int v_normal;


out vec4 a_color;
out vec2 a_texCoord;
out float fog_factor;
out vec3 fog_color;
out vec2 a_light;
out float a_sharpness;

out vec4 a_fragToLight;
out vec3 a_normal;
out vec3 a_lightDir;
out float a_brightness;

uniform mat4 view;
uniform mat4 lightMatrix;
uniform vec3 lightDir;
uniform float brightness;

uniform int takt;
uniform float overview;
uniform vec3 colorfog;
uniform float torch;
uniform float animOffset;
uniform float wind;
uniform vec3 player;

uniform vec2 chunk;

void main()
{
    a_brightness = brightness;
    a_lightDir = lightDir;
    
    vec3 pos = vec3(chunk.x - player.x, -player.y, chunk.y - player.z);
    a_fragToLight = lightMatrix * vec4(pos + v_position, 1.0);
    vec3 camera = vec3(player.x - chunk.x, player.y, player.z - chunk.y);
    
    fog_color = colorfog;
    float camera_distance = distance(camera, vec3(v_position));
    fog_factor = pow(clamp(camera_distance / overview, 0.0, 1.0), 4.0);

    float r = (v_rgbl & 0xFF) / 255.0;
    float g = ((v_rgbl >> 8) & 0xFF) / 255.0;
    float b = ((v_rgbl >> 16) & 0xFF) / 255.0;
    
    float lightSky = ((v_rgbl >> 24) & 0xF) / 16.0 + 0.03125;
    float lightBlock = ((v_rgbl >> 28) & 0xF) / 16.0 + 0.03125;
    
    if (torch > 0 && camera_distance < torch)
    {
        float t2 = torch / 1.4;
        if (camera_distance < t2)
        {
            float lb = (t2 - camera_distance) / t2 * torch / 16.0;
            if (lb > lightBlock) lightBlock = lb;
        }
    }

    a_light = vec2(lightBlock, lightSky);
    a_texCoord = v_texCoord;
    a_sharpness = (v_anim >> 18) & 1;
    
    int frame = (v_anim & 0xFF);
    if (frame > 0)
    {
        int pause = ((v_anim >> 8) & 0xFF);
        int t;
        if (pause > 1) {
            int maxframe = frame * pause;
            t = (takt - takt / maxframe * maxframe) / pause;
        } else {
            t = takt - takt / frame * frame;
        }
        a_texCoord.y += t * animOffset;
    }
    
    if (((v_anim >> 16) & 1) == 1)
    {
        vec3 posanim = v_position;
        posanim.x += wind;
        posanim.z += wind;
        gl_Position = view * vec4(pos + posanim, 1.0);
    }
    else if (((v_anim >> 17) & 1) == 1)
    {
        vec3 posanim = v_position;
        posanim.y += wind * 0.5;
        gl_Position = view * vec4(pos + posanim, 1.0);
    }
    else
    {
        gl_Position = view * vec4(pos + v_position, 1.0);
    }
    
    a_normal = normalize(vec3(float((v_normal & 0xFF) - 127) / 127.0, 
        float(((v_normal >> 8) & 0xFF) - 127) / 127.0, 
        float(((v_normal >> 16) & 0xFF) - 127) / 127.0));
    a_color = vec4(r, g, b, 1.0);
}