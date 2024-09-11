﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Vge.Event;
using Vge.Network;
using Vge.Network.Packets.Server;
using Vge.Util;
using WinGL.Util;

namespace Vge.Games
{
    public class Server
    {
        /// <summary>
        /// Объект лога
        /// </summary>
        public readonly Logger Log;
        /// <summary>
        /// Объект сыщик
        /// </summary>
        public readonly Profiler Filer;

        /// <summary>
        /// Указывает, запущен сервер или нет. Установите значение false, 
        /// чтобы инициировать завершение работы. 
        /// </summary>
        public bool IsServerRunning { get; private set; } = true;
        /// <summary>
        /// Увеличивается каждый тик 
        /// </summary>
        public uint TickCounter { get; private set; }
        /// <summary>
        /// Дельта последнего тика в mc
        /// </summary>
        public float DeltaTime { get; private set; }

        /// <summary>
        /// Часы для Tps
        /// </summary>
        private readonly Stopwatch stopwatchTps = new Stopwatch();
        /// <summary>
        /// Для перевода тактов в мили секунды Stopwatch.Frequency / 1000;
        /// </summary>
        private readonly long frequencyMs;
        /// <summary>
        /// Устанавливается при появлении предупреждения «Не могу угнаться», 
        /// которое срабатывает снова через 15 секунд. 
        /// </summary>
        private long timeOfLastWarning;
        /// <summary>
        /// Хранение тактов за 1/5 секунды игры, для статистики определения среднего времени такта
        /// </summary>
        private readonly long[] tickTimeArray = new long[4];
        /// <summary>
        /// Пауза в игре, только для одиночной версии
        /// </summary>
        private bool isGamePaused = false;

        /// <summary>
        /// Принято пакетов за секунду
        /// </summary>
        private int rx = 0;
        /// <summary>
        /// Передано пакетов за секунду
        /// </summary>
        private int tx = 0;
        /// <summary>
        /// Принято пакетов за предыдущую секунду
        /// </summary>
        private int rxPrev = 0;
        /// <summary>
        /// Передано пакетов за предыдущую секунду
        /// </summary>
        private int txPrev = 0;
        /// <summary>
        /// статус запуска сервера
        /// </summary>
        private string strNet = "";
        /// <summary>
        /// Сокет для сети
        /// </summary>
        private SocketServer socketServer;
        /// <summary>
        /// Объект работы с пакетами
        /// </summary>
        private ProcessServerPackets packets;
        /// <summary>
        /// Объект для запаковки пакетов в массив для отправки
        /// </summary>
        //private readonly WritePacket streamPacket = new WritePacket();

        public Server(Logger log)
        {
            Log = log;
            Filer = new Profiler(Log, "[Server] ");
            packets = new ProcessServerPackets(this);
            frequencyMs = Stopwatch.Frequency / 1000;
            stopwatchTps.Start();
        }

        #region StartStopPause

        /// <summary>
        /// Запустить сервер в отдельном потоке
        /// </summary>
        public void Starting()
        {
            byte slot = 1;
            int indexVersion = 1;
            Log.Server(Srl.Starting, slot, indexVersion);
            Thread myThread = new Thread(Loop) { Name = "ServerLoop" };
            myThread.Start();
        }

        /// <summary>
        /// Запрос остановки сервера
        /// </summary>
        public void Stop() => IsServerRunning = false;

        /// <summary>
        /// Задать паузу для одиночной игры
        /// </summary>
        public bool SetGamePauseSingle(bool value)
        {
            isGamePaused = !IsRunNet() && value;
            OnTextDebug();
            return isGamePaused;
        }

        #endregion

        #region Net

        /// <summary>
        /// Получить истину запущена ли сеть
        /// </summary>
        public bool IsRunNet() => socketServer != null && socketServer.IsConnected();

        /// <summary>
        /// Запустить на сервере сеть
        /// </summary>
        public void RunNet(int port)
        {
            if (!IsRunNet())
            {
                socketServer = new SocketServer(port);
                socketServer.ReceivePacket += SocketServer_ReceivePacket;
                socketServer.Stopped += SocketServer_Stopped;
                socketServer.Runned += SocketServer_Runned;
                socketServer.Error += SocketServer_Error;
                socketServer.UserJoined += SocketServer_UserJoined;
                socketServer.UserLeft += SocketServer_UserLeft;
                socketServer.WarningString += SocketServer_WarningString;
                if (!socketServer.Run())
                {
                    socketServer = null;
                }
            }
        }

        private void SocketServer_WarningString(object sender, StringEventArgs e)
        {
            Log.Server(e.Text);
            OnRecieveMessage(e.Text);
        }

        private void SocketServer_UserJoined(object sender, StringEventArgs e)
            => Log.Server(Srl.ConnectedToTheServer, e.Text);

        private void SocketServer_UserLeft(object sender, StringEventArgs e)
            => Log.Server(Srl.DisconnectedFromServer, e.Text, e.Tag.ToString());

        private void SocketServer_Error(object sender, ErrorEventArgs e)
        {
            Log.Error(e.GetException());
            // В краш улетит
            OnError(e.GetException());
        }

        private void SocketServer_Runned(object sender, EventArgs e)
        {
            isGamePaused = false;
            Log.Server(Srl.NetworkModeIsEnabled);
        }

        private void SocketServer_Stopped(object sender, EventArgs e)
        {
            socketServer = null;
            if (IsServerRunning)
            {
                // Если сеть остановилась, а луп нет, 
                // запускаем остановку лупа и последующее завершение
                Stop();
            }
            else
            {
                // Если луп уже остановлен, то сразу к закрытию
                Stoped();
            }
        }

        private void SocketServer_ReceivePacket(object sender, PacketBufferEventArgs e)
        {
            LocalReceivePacket(e.Side, e.Buffer.bytes);
        }

        /// <summary>
        /// Локальная передача пакета
        /// </summary>
        public void LocalReceivePacket(SocketSide socketClient, byte[] buffer)
        {
            rx++;
            packets.ReceiveBuffer(socketClient, buffer);
        }

        /// <summary>
        /// Обновить количество клиентов
        /// </summary>
        public void UpCountClients() => strNet = IsRunNet() ? "net[" + socketServer.SocketCount() + "]" : "";

        /// <summary>
        /// Отправить пакет клиенту
        /// </summary>
        public void ResponsePacket(SocketSide socketClient, IPacket packet)
        {
            WritePacket streamPacket = new WritePacket();
            streamPacket.Trancive(packet);
            tx++;
            if (socketClient != null)
            {
                socketServer.SendPacket(socketClient, streamPacket.ToArray());
            }
            else
            {
                OnRecievePacket(new PacketBufferEventArgs(new PacketBuffer(streamPacket.ToArray()), null));
            }
        }

        /// <summary>
        /// Отправить пакет всем клиентам
        /// </summary>
        public void ResponsePacketAll(IPacket packet)
        {
            ResponsePacket(null, packet);
            if (socketServer != null)
            {
                SocketSide[] sockets = socketServer.GetSocketClients();
                for (int i = 0; i < sockets.Length; i++)
                {
                    //  System.Threading.Thread.Sleep(40);
                    if (sockets[i].IsConnect())
                    {
                        ResponsePacket(sockets[i], packet);
                    }
                }
            }
            //if (World != null)
            //{
            //    World.Players.SendToAll(packet);
            //}
        }

        /// <summary>
        /// Разорвать соединение с игроком по сокету
        /// </summary>
        //public void DisconnectPlayer(Socket socket) => socketServer.DisconnectPlayer(socket);

        #endregion

        #region Loop & Tick

        /// <summary>
        /// До игрового цикла
        /// </summary>
        private void ToLoop()
        {
            // Запуск игрока
            // Запуск чанков 
        }

        /// <summary>
        /// Метод должен работать в отдельном потоке
        /// </summary>
        private void Loop()
        {
            try
            {
                ToLoop();
                Log.Server(Srl.Go);

                // Текущее время для расчёта сна потока
                long currentTime = stopwatchTps.ElapsedMilliseconds;
                // Накопленное время для определении сна
                long accumulatedTime = 0;
                // Время сна потока, в мс
                int timeSleep;
                // Разница времени для такта
                long differenceTime;
                // Текущее время такта
                long currentTimeTakt;
                // Конечное время такта
                long endTimeTakt = currentTime;
                // Начальное время такта
                long beginTime;

                // Рабочий цикл сервера
                while (IsServerRunning)
                {
                    beginTime = stopwatchTps.ElapsedMilliseconds;
                    // Разница в милсекунда с прошлого такта
                    differenceTime = beginTime - currentTime;
                    if (differenceTime < 0) differenceTime = 0;

                    // Если выше 2 секунд задержка
                    if (differenceTime > 2000 && currentTime - timeOfLastWarning >= 15000)
                    {
                        // Не успеваю!Изменилось ли системное время, или сервер перегружен?
                        // Отставание на {differenceTime} мс, пропуск тиков({differenceTime / 50}) 
                        Log.Server(Srl.LaggingBehind, differenceTime, differenceTime / 50);
                        differenceTime = 2000;
                        timeOfLastWarning = currentTime;
                    }

                    accumulatedTime += differenceTime;
                    currentTime = beginTime;

                    while (accumulatedTime > Ce.Tick​​Time)
                    {
                        accumulatedTime -= Ce.Tick​​Time;
                        if (!isGamePaused)
                        {
                            // фиксируем начальное время такта
                            beginTime = stopwatchTps.ElapsedTicks;
                            // =======
                            OnTick();
                            // =======
                            // фиксируем текущее время такта
                            currentTimeTakt = stopwatchTps.ElapsedTicks;
                            // Находим дельту времени между тактами
                            DeltaTime = (currentTimeTakt - endTimeTakt) / (float)frequencyMs;
                            //tickTimeArray[TickCounter % 4] = currentTimeTakt - endTimeTakt;
                            // фиксируем время выполнения такта
                            tickTimeArray[TickCounter % 4] = currentTimeTakt - beginTime;
                            // фиксируем конечное время
                            endTimeTakt = currentTimeTakt;
                        }
                    }
                    timeSleep = Ce.Tick​​Time - (int)accumulatedTime;
                    Thread.Sleep(timeSleep > 0 ? timeSleep : 1);
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                Stoping();
            }
        }

        /// <summary>
        /// Сохраняет все необходимые данные для подготовки к остановке сервера
        /// </summary>
        private void Stoping()
        {
            Log.Server(Srl.Stopping);
            //World.WorldStoping();
            // Тут надо сохранить мир
            packets.Clear();
            if (socketServer != null)
            {
                socketServer.Stop();
            }
            else
            {
                Stoped();
            }
        }

        /// <summary>
        /// Закрыт луп и сокет
        /// </summary>
        private void Stoped()
        {
            Log.Server(Srl.StoppedServer);
            OnCloseded();
        }

        /// <summary>
        /// Игровой тик
        /// </summary>
        private void OnTick()
        {
            TickCounter++;

            //DeltaTime

            // Сетевые пакеты
            packets.Update();

           // ResponsePacketAll(new PacketS03TimeUpdate(TickCounter));
            // Прошла секунда
            if (TickCounter % Ce.Tps == 0)
            {
                UpCountClients();

                if (TickCounter % 600 == 0)
                {
                    // раз в 30 секунд обновляем тик с клиентом
                    ResponsePacketAll(new PacketS03TimeUpdate(TickCounter));
                }

                rxPrev = rx;
                txPrev = tx;
                rx = 0;
                tx = 0;
            }

            //Thread.Sleep(10);
            // Тут игровые мировые тики

            // Прошла 1/5 секунда, или 4 такта
            if (TickCounter % 4 == 0)
            {
                // лог статистика за это время
                OnTextDebug();
            }
        }

        /// <summary>
        /// Получить время в милисекундах с момента запуска проекта
        /// </summary>
        public long Time() => stopwatchTps.ElapsedMilliseconds;

        #endregion

        /// <summary>
        /// Строка для дебага, формируется по запросу
        /// </summary>
        private string ToStringDebugTps()
        {
            // Среднее время выполнения 4 тактов, должно быть меньше 50
            float averageTime = Mth.Average(tickTimeArray) / frequencyMs;
            // TPS за последние 4 тактов (1/5 сек), должен быть 20
            float tps = averageTime > Ce.Tick​​Time ? Ce.Tick​​Time / averageTime * Ce.Tps : Ce.Tps;
            return string.Format("{0:0.00} tps {1:0.00} ms Rx {2} Tx {3} {4}{5}" + Ce.Br + "{6}",
                tps, averageTime, rxPrev, txPrev, strNet, isGamePaused ? " PAUSE" : "", debugText);
        }

        public void TestUserAllKill()
        {
            if (IsRunNet())
            {
                socketServer.TestUserAllKill();
            }
        }

        public string debugText = "";

        #region Event

        /// <summary>
        /// Событие ошибки на сервере
        /// </summary>
        public event ThreadExceptionEventHandler Error;
        private void OnError(Exception ex)
            => Error?.Invoke(this, new ThreadExceptionEventArgs(ex));

        /// <summary>
        /// Событие закрыть
        /// </summary>
        public event EventHandler Closeded;
        private void OnCloseded() => Closeded?.Invoke(this, new EventArgs());

        /// <summary>
        /// Событие текст для отладки
        /// </summary>
        public event StringEventHandler TextDebug;
        private void OnTextDebug() 
            => TextDebug?.Invoke(this, new StringEventArgs(ToStringDebugTps()));

        /// <summary>
        /// Событие получить от сервера пакет
        /// </summary>
        public event PacketBufferEventHandler RecievePacket;
        private void OnRecievePacket(PacketBufferEventArgs e) 
            => RecievePacket?.Invoke(this, e);

        /// <summary>
        /// Событие получить от сервера сообщение
        /// </summary>
        public event StringEventHandler RecieveMessage;
        private void OnRecieveMessage(string message)
            => RecieveMessage?.Invoke(this, new StringEventArgs(message));

        #endregion
    }
}
