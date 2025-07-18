﻿using System.Collections.Generic;
using Vge.Util;
using WinGL.OpenGL;

namespace Vge.Renderer.Shaders
{
    public class ShaderGuiLine : ShaderProgram
    {
        public ShaderGuiLine(GL gl)
        {
            this.gl = gl;
            string vsh = FileAssets.ReadStringToShader("GuiLine.vsh");
            string fsh = FileAssets.ReadStringToShader("GuiLine.fsh");

            Create("GuiLine", vsh, fsh,
                new Dictionary<uint, string> {
                    { 0, "v_position" },
                    { 1, "v_color" }
                });
        }
    }
}
