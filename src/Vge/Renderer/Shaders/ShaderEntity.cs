﻿using System.Collections.Generic;
using Vge.Util;
using WinGL.OpenGL;

namespace Vge.Renderer.Shaders
{
    public class ShaderEntity : ShaderProgram
    {
        public ShaderEntity(GL gl, string name)
        {
            this.gl = gl;
            string vsh = FileAssets.ReadStringToShader(name + ".vsh");
            string fsh = FileAssets.ReadStringToShader(name + ".fsh");

            Create(name, vsh, fsh,
                new Dictionary<uint, string> {
                    { 0, "v_position" },
                    { 1, "v_normal" },
                    { 2, "v_texCoord" },
                    { 3, "v_jointId" },
                    { 4, "v_clothId" }
                });
        }
    }
}
