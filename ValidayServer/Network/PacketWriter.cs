using System;
using System.Collections.Generic;
using System.Text;
using ValidayServer.Network.Framing.Interfaces;
using ValidayServer.Network.Framing;

namespace ValidayServer.Network
{
    /// <summary>
    /// Fluent builder for outgoing packets.
    /// <para>
    /// A packet body is: <c>[commandId : ushort (2 bytes)][payload bytes]</c>.
    /// </para>
    /// <para>
    /// Call <see cref="Build"/> to get the raw body (use with <see cref="PassthroughFramer"/>).
    /// Call <see cref="BuildFramed"/> to prepend the length header (use with <see cref="LengthPrefixFramer"/>).
    /// Call <see cref="Build(IPacketFramer)"/> to let the framer decide.
    /// </para>
    /// <example>
    /// <code>
    /// // Server sends position update to a client:
    /// byte[] data = new PacketWriter(CommandId.Position)
    ///     .WriteFloat(player.X)
    ///     .WriteFloat(player.Y)
    ///     .WriteString(player.Name)
    ///     .BuildFramed();
    /// </code>
    /// </example>
    /// </summary>
    public sealed class PacketWriter
    {
        private readonly ushort _commandId;
        private readonly List<byte> _payload = new List<byte>();

        /// <param name="commandId">The command ID written at the start of the body.</param>
        public PacketWriter(ushort commandId)
        {
            _commandId = commandId;
        }

        // ─── Primitive write methods ─────────────────────────────────────────────

        /// <summary>Writes a single byte.</summary>
        public PacketWriter WriteByte(byte value)
        {
            _payload.Add(value);
            return this;
        }

        /// <summary>Writes a boolean as a single byte (1 = true, 0 = false).</summary>
        public PacketWriter WriteBool(bool value)
        {
            _payload.Add(value ? (byte)1 : (byte)0);
            return this;
        }

        /// <summary>Writes a signed 16-bit integer (little-endian).</summary>
        public PacketWriter WriteShort(short value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>Writes an unsigned 16-bit integer (little-endian).</summary>
        public PacketWriter WriteUshort(ushort value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>Writes a signed 32-bit integer (little-endian).</summary>
        public PacketWriter WriteInt(int value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>Writes an unsigned 32-bit integer (little-endian).</summary>
        public PacketWriter WriteUint(uint value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>Writes a signed 64-bit integer (little-endian).</summary>
        public PacketWriter WriteLong(long value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>Writes a 32-bit IEEE 754 float (little-endian).</summary>
        public PacketWriter WriteFloat(float value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>Writes a 64-bit IEEE 754 double (little-endian).</summary>
        public PacketWriter WriteDouble(double value)
        {
            _payload.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        /// <summary>
        /// Writes a UTF-8 string as <c>[ushort length][UTF-8 bytes]</c>.
        /// A null value is treated as an empty string.
        /// </summary>
        public PacketWriter WriteString(string? value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);

            if (bytes.Length > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value),
                    $"UTF-8 encoded string is {bytes.Length} bytes, exceeding the 65535-byte limit.");

            _payload.AddRange(BitConverter.GetBytes((ushort)bytes.Length));
            _payload.AddRange(bytes);
            return this;
        }

        /// <summary>Writes a raw byte array (no length prefix — use <see cref="WriteBytes(byte[], bool)"/> for framed blobs).</summary>
        public PacketWriter WriteBytes(byte[] value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _payload.AddRange(value);
            return this;
        }

        /// <summary>
        /// Writes a byte array with a <c>ushort</c> length prefix so the reader can determine the boundary.
        /// </summary>
        public PacketWriter WriteBytes(byte[] value, bool lengthPrefixed)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (lengthPrefixed)
            {
                if (value.Length > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"Byte array is {value.Length} bytes, exceeding the 65535-byte limit.");

                _payload.AddRange(BitConverter.GetBytes((ushort)value.Length));
            }

            _payload.AddRange(value);
            return this;
        }

        // ─── Build methods ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the raw packet body: <c>[commandId : 2 bytes][payload]</c>.
        /// Use this with <see cref="PassthroughFramer"/> or when framing is handled externally.
        /// </summary>
        public byte[] Build()
        {
            byte[] result = new byte[sizeof(ushort) + _payload.Count];
            BitConverter.GetBytes(_commandId).CopyTo(result, 0);
            _payload.CopyTo(result, sizeof(ushort));
            return result;
        }

        /// <summary>
        /// Returns the packet wrapped with a 2-byte length prefix header:
        /// <c>[body length : ushort (2 bytes)][commandId : 2 bytes][payload]</c>.
        /// Use this with <see cref="LengthPrefixFramer"/>.
        /// </summary>
        public byte[] BuildFramed()
        {
            return new LengthPrefixFramer().Frame(Build());
        }

        /// <summary>
        /// Returns the packet processed by the provided framer.
        /// Lets the caller stay decoupled from the concrete framing strategy.
        /// </summary>
        public byte[] Build(IPacketFramer framer)
        {
            if (framer == null)
                throw new ArgumentNullException(nameof(framer));

            return framer.Frame(Build());
        }
    }
}
