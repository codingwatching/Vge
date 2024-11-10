﻿using Vge.Games;
using Vge.Network.Packets;
using Vge.Network.Packets.Server;
using Vge.Util;
using Vge.World.Block;

namespace Vge.Network
{
    /// <summary>
    /// Обработка клиентсиких пакетов для сервером
    /// </summary>
    public class ProcessClientPackets
    {
        /// <summary>
        /// Основной клиент
        /// </summary>
        public GameBase Game { get; private set; }

        /// <summary>
        /// Трафик в байтах
        /// </summary>
        public long Traffic { get; private set; } = 0;

        /// <summary>
        /// Массив очередей пакетов
        /// </summary>
        private readonly DoubleList<IPacket> _packets = new DoubleList<IPacket>();

        // Объект который из буфера данных склеивает пакеты
        ReadPacket _readPacket = new ReadPacket();

        public ProcessClientPackets(GameBase client) => Game = client;

        /// <summary>
        /// Передача данных для клиента
        /// </summary>
        public void ReceiveBuffer(byte[] buffer, int count)
        {
            Traffic += count + Ce.SizeHeaderTCP;
            _readPacket.SetBuffer(buffer);
            _ReceivePacket(_readPacket.Receive(PacketsInit.InitServer(buffer[0])));
        }

        private void _ReceivePacket(IPacket packet)
        {
            byte index = packet.Id;
            if (index <= 2 || index == 0x20)
            {
                switch (index)
                {
                    case 0x00: _Handle00Pong((Packet00PingPong)packet); break;
                    case 0x01: _Handle01KeepAlive((Packet01KeepAlive)packet); break;
                    case 0x02: _Handle02LoadingGame((PacketS02LoadingGame)packet); break;
                    case 0x20: _Handle20ChunkSend((PacketS20ChunkSend)packet); break;
                }
            }
            else
            {
                // Мир есть, заносим в пакет с двойным буфером, для обработки в такте
                _packets.Add(packet);
            }
        }

        /// <summary>
        /// Передача данных для клиента в последовотельности игрового такта
        /// </summary>
        private void _UpdateReceivePacket(IPacket packet)
        {
            switch (packet.Id)
            {
                case 0x03: _Handle03JoinGame((PacketS03JoinGame)packet); break;
                case 0x04: _Handle04TimeUpdate((PacketS04TimeUpdate)packet); break;
                case 0x05: _Handle05TableBlocks((PacketS05TableBlocks)packet); break;
                case 0x07: _Handle07RespawnInWorld((PacketS07RespawnInWorld)packet); break;
                case 0x08: _Handle08PlayerPosLook((PacketS08PlayerPosLook)packet); break;
                case 0x21: _Handle21ChunkData((PacketS21ChunkData)packet); break;
            }
        }

        /// <summary>
        /// Игровой такт клиента
        /// </summary>
        public void Update()
        {
            if (!_packets.Empty())
            {
                _packets.Step();
                int count = _packets.CountBackward;
                for (int i = 0; i < count; i++)
                {
                    _UpdateReceivePacket(_packets.GetNextNull());
                }
            }
        }

        /// <summary>
        /// Очистить пакеты в двойной буферизации
        /// </summary>
        public void Clear() => _packets.Clear();

        #region Handles

        /// <summary>
        /// Пакет связи
        /// </summary>
        private void _Handle00Pong(Packet00PingPong packet) 
            => Game.Player.SetPing(packet.ClientTime);

        /// <summary>
        /// KeepAlive
        /// </summary>
        private void _Handle01KeepAlive(Packet01KeepAlive packet)
            => Game.TrancivePacket(packet);

        /// <summary>
        /// Загрузка игры
        /// </summary>
        private void _Handle02LoadingGame(PacketS02LoadingGame packet)
            => Game.PacketLoadingGame(packet);

        /// <summary>
        /// Пакет соединения игрока с сервером
        /// </summary>
        private void _Handle03JoinGame(PacketS03JoinGame packet)
            => Game.PlayerOnTheServer(packet.Index, packet.Uuid);
        
        /// <summary>
        /// Пакет синхронизации времени с сервером
        /// </summary>
        private void _Handle04TimeUpdate(PacketS04TimeUpdate packet)
            => Game.SetTickCounter(packet.Time);

        /// <summary>
        /// Пакет передать таблицу блоков
        /// </summary>
        private void _Handle05TableBlocks(PacketS05TableBlocks packet)
            => BlocksReg.Correct(new CorrectTable(packet.Blocks));

        /// <summary>
        /// Пакет Возраждение в мире
        /// </summary>
        private void _Handle07RespawnInWorld(PacketS07RespawnInWorld packet)
            => Game.Player.PacketRespawnInWorld(packet);

        /// <summary>
        /// Пакет расположения игрока, при старте, телепорт, рестарте и тп
        /// </summary>
        private void _Handle08PlayerPosLook(PacketS08PlayerPosLook packet)
        {
            Game.Player.Position.X = packet.GetPos().X;
            Game.Player.Position.Y = packet.GetPos().Y;
            Game.Player.Position.Z = packet.GetPos().Z;

            Debug.Player = Game.Player.Position.GetChunkPosition();
        }

        /// <summary>
        /// Замер скорости закачки чанков.
        /// Обрабатываем сразу, не дожидаясь такта
        /// </summary>
        private void _Handle20ChunkSend(PacketS20ChunkSend packet)
            => Game.Player.PacketChunckSend(packet);

        /// <summary>
        /// Пакет изменённые псевдо чанки
        /// </summary>
        private void _Handle21ChunkData(PacketS21ChunkData packet)
            => Game.World.ChunkPrClient.PacketChunckData(packet);

        #endregion
    }
}
