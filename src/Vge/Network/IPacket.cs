﻿namespace Vge.Network
{
    /// <summary>
    /// Интерфейс для сетевых пакетов
    /// </summary>
    public interface IPacket
    {
        /// <summary>
        /// Порядковый номер сетевого пакета, для клиента и серваера отдельный порядок.
        /// </summary>
        byte Id { get; }
        void ReadPacket(ReadPacket stream);
        void WritePacket(WritePacket stream);
    }
}
