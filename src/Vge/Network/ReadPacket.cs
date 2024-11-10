﻿using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Vge.Network
{
    /// <summary>
    /// Объект который из буфера данных склеивает пакеты
    /// </summary>
    public class ReadPacket : IDisposable
    {
        /// <summary>
        /// Массив
        /// </summary>
        private byte[] _buffer;
        /// <summary>
        /// Расположение где читаем
        /// </summary>
        private int _position;

        public void SetBuffer(byte[] buffer)
        {
            _buffer = buffer;
            _position = 1;
        }

        public IPacket Receive(IPacket packet)
        {
            packet.ReadPacket(this);
            return packet;
        }

        public void Dispose()
        {
            _buffer = null;
            GC.SuppressFinalize(this);
        }

        #region Read

        /// <summary>
        /// Прочесть логический тип (0..1) 1 байт
        /// </summary>
        public bool Bool() => _buffer[_position++] != 0;

        public byte[] GetBuffer() => _buffer;
        /// <summary>
        /// Прочесть массив байт
        /// </summary>
        public byte[] Bytes()
        {
            // Первый параметр длинна массива
            int count = Int();
            byte[] b = new byte[count];
            Buffer.BlockCopy(_buffer, _position, b, 0, count);
            _position += count;
            return b;
        }

        /// <summary>
        /// Прочесть массив байт c декомпрессией
        /// </summary>
        public byte[] BytesDecompress()
        {
            int count = Int();
            using (MemoryStream inStream = new MemoryStream(_buffer, _position, count))
            using (GZipStream bigStream = new GZipStream(inStream, CompressionMode.Decompress))
            using (MemoryStream bigStreamOut = new MemoryStream())
            {
                _position += count;
                bigStream.CopyTo(bigStreamOut);
                return bigStreamOut.ToArray();
            }
        }

        public int BytesDecompress(byte[] vs, int offset)
        {
            int count = Int();
            int countOut;
            using (MemoryStream inStream = new MemoryStream(_buffer, _position, count))
            using (GZipStream bigStream = new GZipStream(inStream, CompressionMode.Decompress))
            using (MemoryStream bigStreamOut = new MemoryStream())
            {
                _position += count;
                bigStream.CopyTo(bigStreamOut);
                countOut = (int)bigStreamOut.Length;
                Buffer.BlockCopy(bigStreamOut.GetBuffer(), 0, vs, offset, countOut);
            }
            return countOut;
        }

        /// <summary>
        /// Прочесть тип byte (0..255) 1 байт
        /// </summary>
        public byte Byte() => _buffer[_position++];
        /// <summary>
        /// Прочесть тип ushort (0..65535) 2 байта
        /// </summary>
        public ushort UShort() => (ushort)((_buffer[_position++] << 8) | _buffer[_position++]);
        /// <summary>
        /// Прочесть тип uint (0..4 294 967 295) 4 байта
        /// </summary>
        public uint UInt() => (uint)((_buffer[_position++] << 24) | (_buffer[_position++] << 16) 
            | (_buffer[_position++] << 8) | _buffer[_position++]);
        /// <summary>
        /// Прочесть тип uint (0..18 446 744 073 709 551 615) 8 байт
        /// </summary>
        public ulong ULong() => (ulong)((_buffer[_position++] << 56) | (_buffer[_position++] << 48) 
            | (_buffer[_position++] << 40) | (_buffer[_position++] << 32)
            | (_buffer[_position++] << 24) | (_buffer[_position++] << 16) 
            | (_buffer[_position++] << 8) | _buffer[_position++]);
        /// <summary>
        /// Прочесть тип sbyte (-128..127) 1 байт
        /// </summary>
        public sbyte SByte() => (sbyte)Byte();
        /// <summary>
        /// Прочесть тип short (-32768..32767) 2 байта
        /// </summary>
        public short ReadShort() => (short)UShort();
        /// <summary>
        /// Прочесть тип int (-2 147 483 648..2 147 483 647) 4 байта
        /// </summary>
        public int Int() => (int)UInt();
        /// <summary>
        /// Прочесть тип int (–9 223 372 036 854 775 808..9 223 372 036 854 775 807) 8 байт
        /// </summary>
        public long Long() => (long)ULong();

        /// <summary>
        /// Прочесть строку в UTF-16
        /// </summary>
        public string String() => Encoding.BigEndianUnicode.GetString(Bytes());

        /// <summary>
        /// Прочесть тип float (точность 0,0001) 4 байта
        /// </summary>
        public float Float() => Int() / 10000f; // Этот быстрее на ~10-20%
        //public float Float() => BitConverter.ToSingle(new byte[] { Byte(), Byte(), Byte(), Byte() }, 0);

        #endregion

        
    }
}
