﻿using Vge.Gui.Controls;
using Vge.Network.Packets.Client;
using Vge.Renderer.Font;
using WinGL.Actions;
using WinGL.Util;

namespace Vge.Gui.Screens
{
    /// <summary>
    /// Скрин окна чата
    /// </summary>
    public class ScreenChat : ScreenBase
    {
        /// <summary>
        /// Максимальное количество строк в чате
        /// </summary>
        protected int _countMaxLine = 16;

        /// <summary>
        /// Флаг дублирования клавиши с WM_KEYDOWN и WM_CHAR
        /// Чат открывается по WM_KEYDOWN и сразу же пишет букву
        /// </summary>
        private bool _flagDuplication = true;
        /// <summary>
        /// Строка списка сообщений
        /// </summary>
        private Label _labelMessages;

        /// <summary>
        /// Контрол написания текста
        /// </summary>
        private readonly TextBox _textBoxMessage;

        /// <summary>
        /// Сохраняет положение того, какое сообщение чата вы выберете при нажатии вверх
        /// (не увеличивается для дублированных сообщений, отправленных сразу после друг друга)
        /// </summary>
        private int _sentHistoryCursor = -1;
        /// <summary>
        /// Позиция скролинга текста
        /// </summary>
        private int _scrollPos = 0;
        private string _historyBuffer = "";

        public ScreenChat(WindowMain window) : base(window)
        {
            FontBase font = window.Render.FontMain;
            _labelMessages = new Label(window, 
                window.Game.Player.Chat.Font, "", true).SetTextAlight(EnumAlight.Left, EnumAlightVert.Bottom);
            _labelMessages.SetSize(window.Game.Player.Chat.Width, 270);
            _labelMessages.Multiline();
            _textBoxMessage = new TextBox(window, window.Render.FontMain, 500, "", TextBox.EnumRestrictions.All, 128);
            _textBoxMessage.FixFocus();

            _sentHistoryCursor = window.Game.Player.Chat.SentMessages.Count;
            _PageUpdate();

            window.Game.Hud.ChatOn();
        }

        protected void _Close() => window.LScreen.Close();

        /// <summary>
        /// Игровой такт
        /// </summary>
        /// <param name="deltaTime">Дельта последнего тика в mc</param>
        public override void OnTick(float deltaTime)
        {
            base.OnTick(deltaTime);
            if (window.Game.Player.Chat.FlagUpdate)
            {
                _UpMessages();
            }
        }

        /// <summary>
        /// Запускается при создании объекта и при смене режима FullScreen
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            AddControls(_textBoxMessage);
            AddControls(_labelMessages);
            _UpMessages();
        }

        /// <summary>
        /// Изменён размер окна
        /// </summary>
        protected override void OnResized()
        {
            base.OnResized();
            int w = Width / 2;
            int h = Height / 2;
            _textBoxMessage.SetPosition(8, Height - 48);
            _labelMessages.SetPosition(16, Height - 324);
        }

        public override void OnKeyDown(Keys keys)
        {
            if (keys == Keys.Escape)
            {
                _Close();
            }
            else if (keys == Keys.Enter)
            {
                // Тут действие текста
                window.Game.Player.Chat.AddMessageHistory(_textBoxMessage.Text);
                window.Game.TrancivePacket(new PacketC14Message(_textBoxMessage.Text,
                    window.Game.Player.MovingObject));
                //_textBoxMessage.SetText("");
                //_textBoxMessage.UpCursor();
                _Close();
            }
            else if (keys == Keys.PageUp)
            {
                _PageBackNext(true);
            }
            else if (keys == Keys.PageDown)
            {
                _PageBackNext(false);
            }
            else if (keys == Keys.Up)
            {
                _SentHistory(-1);
            }
            else if (keys == Keys.Down)
            {
                _SentHistory(1);
            }
            else
            {
                base.OnKeyDown(keys);
            }
        }

        /// <summary>
        /// Нажата клавиша в char формате
        /// </summary>
        public override void OnKeyPress(char key)
        {
            if (_flagDuplication)
            {
                _flagDuplication = false;
            }
            else
            {
                base.OnKeyPress(key);
            }
        }

        /// <summary>
        /// Вращение колёсика мыши
        /// </summary>
        /// <param name="delta">смещение</param>
        public override void OnMouseWheel(int delta, int x, int y)
        {
            if (delta != 0) _PageBackNext(delta > 0);
        }

        public override void Dispose()
        {
            base.Dispose();
            window.Game.Hud.ChatOff();
        }

        /// <summary>
        /// Обновить сообщения чата
        /// </summary>
        private void _UpMessages()
        {
            string message = "";
            string textResult = "";

            int count = window.Game.Player.Chat.Messages.Count;
            if (count > 0)
            {
                int i;
                int countMax = 0;
                count--;
                count -= _scrollPos;
                for (i = count; i >= 0; i--)
                {
                    message = window.Game.Player.Chat.Messages[i];
                    countMax++;
                    if (textResult == "")
                    {
                        textResult = message;
                    }
                    else
                    {
                        textResult = message + "\r\n" + textResult;
                    }
                    if (countMax >= _countMaxLine) { break; }
                }
            }
            window.Game.Player.Chat.Update();
            _labelMessages.SetText(textResult);
        }

        /// <summary>
        /// Показать шаг истории
        /// </summary>
        private void _SentHistory(int step)
        {
            int cursor = _sentHistoryCursor + step;
            int count = window.Game.Player.Chat.SentMessages.Count;
            cursor = Mth.Clamp(cursor, 0, count);

            if (cursor != _sentHistoryCursor)
            {
                if (cursor == count)
                {
                    _sentHistoryCursor = count;
                    _textBoxMessage.SetText(_historyBuffer);
                }
                else
                {
                    if (_sentHistoryCursor == count)
                    {
                        _historyBuffer = _textBoxMessage.Text;
                    }
                    _textBoxMessage.SetText(window.Game.Player.Chat.SentMessages[cursor]);
                    _sentHistoryCursor = cursor;
                }
                _textBoxMessage.UpCursor();
            }
        }

        private void _PageBackNext(bool next)
        {
            int old = _scrollPos;
            int countMax = window.Game.Player.Chat.Messages.Count;
            _scrollPos += next ? 1 : -1;
            if (_scrollPos > countMax - _countMaxLine) _scrollPos = countMax - _countMaxLine;
            if (_scrollPos < 0) _scrollPos = 0;
        //    if (old != _scrollPos)
            {
                _UpMessages();
                _PageUpdate();
               // _isRender = true;
            }
        }

        private void _PageUpdate()
        {
            //if (_scrollPos > 0) buttonBack.Enabled = true;
            //else buttonBack.Enabled = false;
            //if (_scrollPos + _countMaxLine < listMessages.Count) buttonNext.Enabled = true;
            //else buttonNext.Enabled = false;
        }
    }
}