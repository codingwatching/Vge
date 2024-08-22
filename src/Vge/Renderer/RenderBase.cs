﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using Vge.Renderer.Font;
using Vge.Renderer.Shaders;
using Vge.Util;
using WinGL.OpenGL;
using WinGL.Util;

namespace Vge.Renderer
{
    /// <summary>
    /// Базовый класс отвечающий за прорисовку
    /// </summary>
    public class RenderBase
    {
        /// <summary>
        /// Основной шрифт
        /// </summary>
        public FontBase FontMain { get; private set; }

        /// <summary>
        /// Объект окна
        /// </summary>
        protected readonly WindowMain window;
        /// <summary>
        /// Объект OpenGL для элемента управления
        /// </summary>
        protected readonly GL gl;
        /// <summary>
        /// Объект текстур
        /// </summary>
        protected readonly TextureMap textureMap;
        /// <summary>
        /// Шейдоры для 2д
        /// </summary>
        protected readonly Shader2d shader2D;
        /// <summary>
        /// Шейдоры для текста
        /// </summary>
        protected readonly ShaderText shaderText;

        public RenderBase(WindowMain window, GL gl)
        {
            this.window = window;
            this.gl = gl;
            textureMap = new TextureMap(gl);
            shader2D = new Shader2d(gl);
            shaderText = new ShaderText(gl);
            meshTextDebug = new Mesh(gl, new float[0], new int[] { 2, 2, 3 });
        }

        /// <summary>
        /// Стартовая инициализация до загрузчика
        /// </summary>
        public virtual void InitializeFirst()
        {
            // Задать количество текстур
            TextureSetCount();

            FontMain = new FontBase(gl, SetTexture(Options.PathTextures + "FontMain.png", 0), 1);
        }

        #region Texture

        /// <summary>
        /// Задать количество текстур
        /// </summary>
        protected virtual void TextureSetCount() => textureMap.SetCount(1);

        /// <summary>
        /// Запустить текстуру основного шрифта
        /// </summary>
        public void BindTexutreFontMain() => textureMap.BindTexture(0);
        /// <summary>
        /// Запустить текстуру, указав индекс текстуры массива
        /// </summary>
        public void BindTexture(int index, uint texture = 0) => textureMap.BindTexture(index, texture);

        /// <summary>
        /// Задать текстуру
        /// </summary>
        protected BufferedImage SetTexture(string fileName, int index)
        {
            Bitmap bitmap = Image.FromFile(fileName) as Bitmap;
            BufferedImage image = new BufferedImage(bitmap.Width, bitmap.Height, BitmapToByteArray(bitmap));
            textureMap.SetTexture(index, image);
            return image;
        }

        /// <summary>
        /// Конвертация из Bitmap в объект BufferedImage
        /// </summary>
        private byte[] BitmapToByteArray(Bitmap bitmap)
        {
            BitmapData bmpdata = null;
            try
            {
                bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int numbytes = bmpdata.Stride * bitmap.Height;
                byte[] bytedata = new byte[numbytes];
                IntPtr ptr = bmpdata.Scan0;

                Marshal.Copy(ptr, bytedata, 0, numbytes);

                return bytedata;
            }
            finally
            {
                if (bmpdata != null)
                    bitmap.UnlockBits(bmpdata);
            }
        }

        #endregion

        #region TextDebug

        private string textDebug = "";
        private bool isTextDebug = false;
        private Mesh meshTextDebug;

        /// <summary>
        /// Внести текст отладки
        /// </summary>
        public void SetTextDebug(string text)
        {
            textDebug = text;
            isTextDebug = true;
        }

        /// <summary>
        /// Рендер текста отладки
        /// </summary>
        protected void RenderTextDebug()
        {
            if (isTextDebug)
            {
                isTextDebug = false;
                FontMain.RenderText(11, 11, textDebug, new Vector3(.2f, .2f, .2f));
                FontMain.RenderText(10, 10, textDebug, new Vector3(.9f, .9f, .9f));
                
                meshTextDebug.Reload(FontMain.ToBuffer());
                FontMain.BufferClear();
            }
        }

        /// <summary>
        /// Прорисовка отладки
        /// </summary>
        protected void DrawTextDebug()
        {
            BindTexutreFontMain();
            meshTextDebug.Draw();
        }

        #endregion

        #region Draw + Debug

        /// <summary>
        /// Прорисовка кадра
        /// </summary>
        public void Draw()
        {
            DrawBegin();
            OnDraw();
            DrawEnd();
        }

        private float speedFrameAll;
        private float speedTickAll;
        private long timerSecond;
        private int fps;
        private int tps;
        private long tickDraw;

        protected virtual void DrawBegin()
        {
            fps++;
            tickDraw = window.TimeTicks();
        }

        protected virtual void OnDraw() { }

        /// <summary>
        /// После прорисовки каждого кадра OpenGL
        /// </summary>
        protected virtual void DrawEnd()
        {
            // Перерасчёт кадров раз в секунду, и среднее время прорисовки кадра
            if (window.Time() >= timerSecond + 1000)
            {
                float speedTick = 0;
                if (tps > 0) speedTick = speedTickAll / tps;
                window.debug.SetTpsFps(fps, speedFrameAll / fps, tps, speedTick);

                timerSecond += 1000;
                speedFrameAll = 0;
                speedTickAll = 0;
                fps = 0;
                tps = 0;
            }

            speedFrameAll += (float)(window.TimeTicks() - tickDraw) / Ticker.TimerFrequency;
        }

        /// <summary>
        /// В такте игрового времени
        /// </summary>
        /// <param name="time">время затраченное на такт</param>
        public void UpdateTick(float time)
        {
            speedTickAll += time;
            tps++;
        }

        #endregion
    }
}