﻿using System.IO;

namespace Vge.Util
{
    /// <summary>
    /// Объект настроек
    /// </summary>
    public class Options
    {
        /// <summary>
        /// путь к ресурсам
        /// </summary>
        public static string PathAssets { get; set; } = "Assets" + Path.DirectorySeparatorChar;
        /// <summary>
        /// Путь к папке шейдеров
        /// </summary>
        public static string PathShaders { get; private set; }
        /// <summary>
        /// Путь к папке звуков
        /// </summary>
        public static string PathSounds { get; private set; }
        /// <summary>
        /// Путь к папке текстур
        /// </summary>
        public static string PathTextures { get; private set; }

        /// <summary>
        /// Общая громкость
        /// </summary>
        public static int SoundVolume { get; set; } = 100;
        /// <summary>
        /// Получить громкость звуковых эффектов
        /// </summary>
        public static float SoundVolumeFloat { get; private set; }

        /// <summary>
        /// Желаемый FPS
        /// </summary>
        public static int Fps { get; set; } = 60;

        /// <summary>
        /// Чувствительность мышки, 0 - 100, где 0 это минимум, 100 максимум, 50 середина
        /// </summary>
        public static int MouseSensitivity { get; set; } = 50;
        /// <summary>
        /// Получить чувствительность мыши
        /// </summary>
        public static float MouseSensitivityFloat { get; private set; } = 50;

        /// <summary>
        /// Обновить данные переменных
        /// </summary>
        public static void UpData()
        {
            SoundVolumeFloat = SoundVolume / 100f;
            if (MouseSensitivity > 50)
            {
                MouseSensitivityFloat = 3f + (MouseSensitivity - 50) / 7f;
            }
            else
            {
                MouseSensitivityFloat = .5f + MouseSensitivity / 20f;
            }

            PathShaders = PathAssets + "Shaders" + Path.DirectorySeparatorChar;
            PathSounds = PathAssets + "Sounds" + Path.DirectorySeparatorChar;
            PathTextures = PathAssets + "Textures" + Path.DirectorySeparatorChar;
        }
    }
}
