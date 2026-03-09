using System;
using System.Text;

namespace ValidayServer.Network
{
    /// <summary>
    /// Sequential binary reader for incoming packet data.
    /// <para>
    /// The raw data passed to <c>IServerCommand.Execute</c> starts with the 2-byte command ID.
    /// Use <see cref="SkipCommandId"/> (or construct with <c>offset: sizeof(ushort)</c>)
    /// to position the reader after the command ID before reading payload fields.
    /// </para>
    /// <example>
    /// <code>
    /// public void Execute(IClient sender, byte[] rawData)
    /// {
    ///     var reader = new PacketReader(rawData).SkipCommandId();
    ///     float x = reader.ReadFloat();
    ///     float y = reader.ReadFloat();
    ///     string name = reader.ReadString();
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public sealed class PacketReader
    {
        private readonly byte[] _data;
        private int _position;

        /// <summary>Current read position (byte offset from the start of the buffer).</summary>
        public int Position => _position;

        /// <summary>Number of bytes remaining after the current position.</summary>
        public int Remaining => _data.Length - _position;

        /// <summary>
        /// Creates a reader over <paramref name="data"/> starting at byte 0.
        /// </summary>
        public PacketReader(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _position = 0;
        }

        /// <summary>
        /// Creates a reader over <paramref name="data"/> starting at <paramref name="offset"/>.
        /// </summary>
        public PacketReader(byte[] data, int offset)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));

            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            _position = offset;
        }

        // ─── Navigation ──────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the position past the 2-byte command ID prefix.
        /// Returns <c>this</c> for fluent use.
        /// </summary>
        public PacketReader SkipCommandId()
        {
            Skip(sizeof(ushort));
            return this;
        }

        /// <summary>Advances the position by <paramref name="count"/> bytes.</summary>
        public PacketReader Skip(int count)
        {
            EnsureAvailable(count);
            _position += count;
            return this;
        }

        // ─── Primitive read methods ──────────────────────────────────────────────

        /// <summary>Reads a single byte.</summary>
        public byte ReadByte()
        {
            EnsureAvailable(1);
            return _data[_position++];
        }

        /// <summary>Reads a boolean (non-zero byte = true).</summary>
        public bool ReadBool() => ReadByte() != 0;

        /// <summary>Reads a signed 16-bit integer (little-endian).</summary>
        public short ReadShort()
        {
            EnsureAvailable(sizeof(short));
            short value = BitConverter.ToInt16(_data, _position);
            _position += sizeof(short);
            return value;
        }

        /// <summary>Reads an unsigned 16-bit integer (little-endian).</summary>
        public ushort ReadUshort()
        {
            EnsureAvailable(sizeof(ushort));
            ushort value = BitConverter.ToUInt16(_data, _position);
            _position += sizeof(ushort);
            return value;
        }

        /// <summary>Reads a signed 32-bit integer (little-endian).</summary>
        public int ReadInt()
        {
            EnsureAvailable(sizeof(int));
            int value = BitConverter.ToInt32(_data, _position);
            _position += sizeof(int);
            return value;
        }

        /// <summary>Reads an unsigned 32-bit integer (little-endian).</summary>
        public uint ReadUint()
        {
            EnsureAvailable(sizeof(uint));
            uint value = BitConverter.ToUInt32(_data, _position);
            _position += sizeof(uint);
            return value;
        }

        /// <summary>Reads a signed 64-bit integer (little-endian).</summary>
        public long ReadLong()
        {
            EnsureAvailable(sizeof(long));
            long value = BitConverter.ToInt64(_data, _position);
            _position += sizeof(long);
            return value;
        }

        /// <summary>Reads a 32-bit IEEE 754 float (little-endian).</summary>
        public float ReadFloat()
        {
            EnsureAvailable(sizeof(float));
            float value = BitConverter.ToSingle(_data, _position);
            _position += sizeof(float);
            return value;
        }

        /// <summary>Reads a 64-bit IEEE 754 double (little-endian).</summary>
        public double ReadDouble()
        {
            EnsureAvailable(sizeof(double));
            double value = BitConverter.ToDouble(_data, _position);
            _position += sizeof(double);
            return value;
        }

        /// <summary>
        /// Reads a UTF-8 string written by <see cref="PacketWriter.WriteString"/>.
        /// Format: <c>[ushort length][UTF-8 bytes]</c>.
        /// </summary>
        public string ReadString()
        {
            ushort length = ReadUshort();
            EnsureAvailable(length);
            string value = Encoding.UTF8.GetString(_data, _position, length);
            _position += length;
            return value;
        }

        /// <summary>Reads exactly <paramref name="count"/> raw bytes.</summary>
        public byte[] ReadBytes(int count)
        {
            EnsureAvailable(count);
            byte[] value = new byte[count];
            Array.Copy(_data, _position, value, 0, count);
            _position += count;
            return value;
        }

        /// <summary>
        /// Reads a length-prefixed byte array written by
        /// <see cref="PacketWriter.WriteBytes(byte[], bool)"/> with <c>lengthPrefixed: true</c>.
        /// </summary>
        public byte[] ReadBytesWithLength()
        {
            ushort length = ReadUshort();
            return ReadBytes(length);
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private void EnsureAvailable(int count)
        {
            if (_position + count > _data.Length)
                throw new InvalidOperationException(
                    $"PacketReader: attempted to read {count} byte(s) at position {_position} " +
                    $"but only {Remaining} byte(s) remain in the buffer (total length: {_data.Length}).");
        }
    }
}
