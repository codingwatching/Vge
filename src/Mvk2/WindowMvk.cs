﻿using System;
using WinGL.OpenGL;
using WinGL.Actions;
using System.Reflection;
using Vge.Renderer;
using Vge;
using Mvk2.Util;
using Mvk2.Audio;
using Mvk2.Renderer;

namespace Mvk2
{
    public class WindowMvk : WindowMain
    {
        /// <summary>
        /// Объект сетки курсора, временно
        /// </summary>
        private Mesh cursorVBO;
        /// <summary>
        /// Виден ли курсор
        /// </summary>
        private bool cursorShow = false;

        private AudioMvk audio = new AudioMvk();

        public WindowMvk() : base()
        {
            Version = "Test VBO by Ant " + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //FullScreen = true;
            //  CursorShow(true);

            //WinUser.CursorShow(false);
        }

        protected override void OnMouseDown(MouseButton button, int x, int y)
        {
            base.OnMouseDown(button, x, y);
            if (button == MouseButton.Left) cursorShow = true;
            client.IsRunGameLoop = true;
        }

        protected override void OnMouseUp(MouseButton button, int x, int y)
        {
            base.OnMouseUp(button, x, y);
            if (button == MouseButton.Left) cursorShow = false;
            client.IsRunGameLoop = false;
        }

        protected override void OnMouseEnter()
        {
            base.OnMouseEnter();
            //cursorShow = true;
            //CursorShow(false);
        }

        protected override void OnMouseLeave()
        {
            cursorShow = false;
            //CursorShow(true);
        }

        protected override void OnOpenGLInitialized()
        {
            new OptionsFileMvk().Load();
            new OptionsFileMvk().Save();

            // Загрузка опций должна быть до инициализации графики, 
            // так -как нужно знать откуда грузить шейдера
            base.OnOpenGLInitialized();

            // Инициализация звука и загрузка семплов
            audio.Initialize(2);
            audio.InitializeSample();

            cursorVBO = new Mesh(gl, RenderFigure.Rectangle2d(0, 0, 24, 24, 0, 0, 1, 1), new int[] { 2, 2 });

            gl.ShadeModel(GL.GL_SMOOTH);
            gl.ClearColor(0.0f, .5f, 0.0f, 1f);
            gl.Clear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);
            gl.ClearDepth(1.0f);
            gl.Enable(GL.GL_DEPTH_TEST);
            gl.DepthFunc(GL.GL_LEQUAL);
            gl.Hint(GL.GL_PERSPECTIVE_CORRECTION_HINT, GL.GL_NICEST);
        }

        /// <summary>
        /// Инициализаця объекта рендера
        /// </summary>
        protected override void RenderInitialized()
        {
            render = new RenderMvk(this, gl);
            render.InitializeFirst();
        }

        protected override void OnOpenGlDraw()
        {
            base.OnOpenGlDraw();
            
            gl.Clear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);
            gl.Enable(GL.GL_DEPTH_TEST);
            // группа для сглаживания, но может жутко тормазить
            gl.BlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
            gl.ClearColor(.7f, .4f, .4f, 1f);
            gl.Enable(GL.GL_BLEND);

            ((RenderMvk)render).DrawDebug();

            if (cursorShow)
            {
                render.BindTexture((int)AssetsTexture.cursor);
                shader2D.Bind(gl);
                shader2D.SetUniformMatrix4(gl, "projview", Ortho2D);
                shader2D.SetUniform1(gl, "biasX", MouseX);
                shader2D.SetUniform1(gl, "biasY", MouseY);
                shader2D.SetUniform4(gl, "color", 1, 1, 1, 1);
                cursorVBO.Draw();

            }
        }

        protected override void OnResized(int width, int height)
        {
            base.OnResized(width, height);
           // t1 = null;
        }

        protected override void Client_Frame(object sender, EventArgs e)
        {
            DrawFrame();
            //Thread.Sleep(25);
        }

        protected override void Client_Tick(object sender, EventArgs e)
        {
            base.Client_Tick(sender, e);

            int x = ((RenderMvk)render).xx;
            x -= 100;
            if (x < 0) x = 0;
            ((RenderMvk)render).xx = x;
        }

        private string textDb = "";

        protected override void OnKeyDown(Keys keys)
        {
            base.OnKeyDown(keys);
            if (keys == Keys.Space)
            {
                audio.PlaySound(0, 0, 0, 0, 1, 1);
            }
            else if (keys == Keys.Enter)
            {
                audio.PlaySound(1, 0, 0, 0, 1, 1);
            }
            //map.ContainsKey(keys);
            textDb = "d* " + keys.ToString();// + " " + Convert.ToString(lParam.ToInt32(), 2);
        }

        protected override void OnKeyUp(Keys keys)
        {
            base.OnKeyUp(keys);
            textDb = "up " + keys.ToString();
        }

        protected override void OnKeyPress(char key)
        {
            try
            {
                //textDb += key;
                return;
            }
            catch (Exception ex)
            {
                return;
            }
        }

        //protected override void Server_Tick(object sender, EventArgs e)
        //{
        //    xx -= 100;
        //    if (xx < 0) xx = 0;
        //}
    }
}
