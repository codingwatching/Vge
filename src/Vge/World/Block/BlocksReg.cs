﻿using System;
using System.Collections.Generic;
using Vge.Json;
using Vge.Util;
using Vge.World.Block.List;

namespace Vge.World.Block
{
    /// <summary>
    /// Регистрация блоков
    /// </summary>
    public sealed class BlocksReg
    {
        /// <summary>
        /// Таблица блоков для регистрации
        /// </summary>
        public static readonly BlockRegTable Table = new BlockRegTable();
        /// <summary>
        /// Объект генерации атласа блоков
        /// </summary>
        public static readonly GeneratingBlockAtlas BlockAtlas = new GeneratingBlockAtlas();

        /// <summary>
        /// Справочник всех форм
        /// </summary>
        private static readonly Dictionary<string, JsonCompound> _shapes = new Dictionary<string, JsonCompound>();
        /// <summary>
        /// Справочник родлителей стат блока
        /// </summary>
        private static Dictionary<string, JsonCompound> _parentStats = new Dictionary<string, JsonCompound>();

        /// <summary>
        /// Инициализация блоков, если window не указывать, прорисовки о статусе не будет (для сервера)
        /// </summary>
        public static void Initialization(WindowMain window = null)
        {
            if (window != null)
            {
                window.LScreen.Process(L.T("CreateBlocks"));
                window.DrawFrame();
            }

            _InitializationBegin();
        }

        /// <summary>
        /// Перед инициализацией
        /// </summary>
        private static void _InitializationBegin()
        {
            // Создаём графический объект гдля генерации атласа блокоы
            BlockAtlas.CreateImage(64, 16);

            // Очистить таблицы и вспомогательные данные json
            _Clear();

            // Регистрация обязательных блоков
            // Воздух
            string alias = "Air";
            BlockAir blockAir = new BlockAir();
            blockAir.InitAliasAndJoinN1(alias, new JsonCompound(), new JsonCompound(new JsonKeyValue[] { }));
            Table.Add(alias, blockAir);

            // Отладочный
            RegisterBlockClass("Debug", new BlockDebug());
        }

        /// <summary>
        /// Инициализация атласа блоков, после инициализации блоков
        /// </summary>
        public static void InitializationAtlas(WindowMain window) => BlockAtlas.EndImage(window.Render.Texture);

        /// <summary>
        /// Корректировка блоков после загрузки, если загрузки нет,
        /// всё равно надо, для активации
        /// </summary>
        public static void Correct(CorrectTable correct)
        {
            //Ce.Blocks = new BlockArrays(); // 2025-06-04 Не помню почему так было, но бывали нал, возможно до создание мира
            correct.CorrectRegLoad(Table);
            Ce.Blocks = new BlockArrays();

            // Очистить таблицы и вспомогательные данные json
            _Clear();
        }

        /// <summary>
        /// Очистить таблицы и вспомогательные данные json
        /// </summary>
        private static void _Clear()
        {
            // Очистить массивы регистрации
            Table.Clear();
            // Очистить вспомогательные данные json
            _parentStats.Clear();
            _shapes.Clear();
        }

        /// <summary>
        /// Зврегистрировать блок
        /// </summary>
        public static void RegisterBlockClass(string alias, BlockBase blockObject)
        {
            JsonRead jsonRead = new JsonRead(Options.PathBlocks + alias + ".json");
            
            if (jsonRead.IsThereFile)
            {
                JsonCompound state = _ParentState(jsonRead.Compound);
                List<JsonKeyValue> shapes = new List<JsonKeyValue>();
                if (state.IsKey(Ctb.Variants))
                {
                    JsonArray variants = state.GetArray(Ctb.Variants);
                    bool bodyShape = false;
                    foreach (JsonCompound variant in variants.Items)
                    {
                        string shapeName = variant.GetString(Ctb.Shape);
                        foreach (JsonKeyValue shape in shapes)
                        {
                            if (shape.IsKey(shapeName))
                            {
                                bodyShape = true;
                                break;
                            }
                        }
                        if (bodyShape)
                        {
                            bodyShape = false;
                        }
                        else
                        {
                            shapes.Add(new JsonKeyValue(shapeName, _GetShape(shapeName)));
                        }
                    }
                }
                if (state.IsKey(Ctb.Variant))
                {
                    string shapeName = state.GetString(Ctb.Variant);
                    shapes.Add(new JsonKeyValue(shapeName, _GetShape(shapeName)));
                }
                blockObject.InitAliasAndJoinN1(alias, state, new JsonCompound(shapes.ToArray()));
                Table.Add(alias, blockObject);
            }
            else
            {
                throw new Exception(Sr.GetString(Sr.FileMissingJsonBlock, alias));
            }
            
            //_window.LScreen.Process("Block init " + alias);
            //_window.DrawFrame();
        }

        #region Shape

        /// <summary>
        /// Добавить фигуру
        /// </summary>
        private static JsonCompound _GetShape(string name)
        {
            if(name != "" && !_shapes.ContainsKey(name))
            {
                // Добавляем фигуру
                JsonRead jsonRead = new JsonRead(Options.PathShapeBlocks + name + ".json");
                if (jsonRead.IsThereFile)
                {
                    return _ParentShape(jsonRead.Compound);
                }
            }
            return new JsonCompound();
        }

        /// <summary>
        /// Проверяем наличие парента для фигуры, если имеется то корректируем JsonCompound и возвращаем с его учётом
        /// </summary>
        private static JsonCompound _ParentShape(JsonCompound compound)
        {
            string parent = compound.GetString("Parent");
            if (parent != "")
            {
                // Имеется родитель
                if (_shapes.ContainsKey(parent))
                {
                    // Имеется в справочнике
                    return _SetChildState(_shapes[parent], compound);
                }
                else
                {
                    JsonRead jsonRead = new JsonRead(Options.PathShapeBlocks + parent + ".json");
                    if (jsonRead.IsThereFile)
                    {
                        JsonCompound state = _ParentShape(jsonRead.Compound);
                        _shapes.Add(parent, state);
                        return _SetChildState(state, compound);
                    }
                }
            }
            return compound;
        }

        #endregion

        #region State

        /// <summary>
        /// Проверяем наличие парента для стат, если имеется то корректируем JsonCompound и возвращаем с его учётом
        /// </summary>
        private static JsonCompound _ParentState(JsonCompound compound)
        {
            string parent = compound.GetString("Parent");
            if (parent != "")
            {
                // Имеется родитель
                if (_parentStats.ContainsKey(parent))
                {
                    // Имеется в справочнике
                    return _SetChildState(_parentStats[parent], compound);
                }
                else
                {
                    JsonRead jsonRead = new JsonRead(Options.PathBlocks + parent + ".json");
                    if (jsonRead.IsThereFile)
                    {
                        JsonCompound state = _ParentState(jsonRead.Compound);
                        _parentStats.Add(parent, state);
                        return _SetChildState(state, compound);
                    }
                }
            }
            return compound;
        }

        /// <summary>
        /// Склеить ребёнка к основе для стат блока
        /// </summary>
        private static JsonCompound _SetChildState(JsonCompound main, JsonCompound child)
        {
            bool add = true;
            int i, count;
            List<JsonKeyValue> list = new List<JsonKeyValue>(main.Items);

            foreach (JsonKeyValue json in child.Items)
            {
                count = main.Items.Length;
                for (i = 0; i < count; i++)
                {
                    if (main.Items[i].IsKey(json.Key))
                    {
                        list[i] = json;
                        add = false;
                        break;
                    }
                }
                if (add)
                {
                    list.Add(json);
                }
                else
                {
                    add = true;
                }
            }

            return new JsonCompound(list.ToArray());
        }

        #endregion
    }
}
