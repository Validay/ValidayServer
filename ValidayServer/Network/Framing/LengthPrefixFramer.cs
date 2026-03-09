using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ValidayServer.Network.Framing.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Network.Framing
{
    /// <summary>
    /// Framer that uses a 2-byte little-endian length prefix.
    /// <para>Wire format: <c>[body length : ushort (2 bytes)][body bytes]</c></para>
    /// <para>
    /// The "body" is everything after the header — typically
    /// <c>[commandId : ushort (2 bytes)][payload]</c> as produced by <see cref="PacketWriter.BuildFramed"/>.
    /// </para>
    /// Handles TCP fragmentation (partial packets) and coalescing (multiple packets per read).
    /// Each client gets its own accumulation buffer; state is removed on disconnect via
    /// <see cref="RemoveClient"/>.
    /// </summary>
    public sealed class LengthPrefixFramer : IPacketFramer
    {
        private const int HeaderSize = sizeof(ushort); // 2 bytes

        // Per-client accumulation buffer.
        // Concurrent dictionary for safe multi-client access; each ClientBuffer is accessed
        // sequentially (one BeginReceive chain per client) so no inner lock is needed.
        private readonly ConcurrentDictionary<IClient, ClientBuffer> _buffers
            = new ConcurrentDictionary<IClient, ClientBuffer>();

        /// <inheritdoc/>
        public IEnumerable<byte[]> ProcessIncoming(IClient client, byte[] rawData)
        {
            if (rawData == null || rawData.Length == 0)
                yield break;

            ClientBuffer buf = _buffers.GetOrAdd(client, _ => new ClientBuffer());
            buf.Append(rawData);

            while (buf.TryReadPacket(out byte[]? packet))
                yield return packet!;
        }

        /// <inheritdoc/>
        public byte[] Frame(byte[] body)
        {
            if (body == null)
                throw new ArgumentNullException(nameof(body));

            if (body.Length > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(body),
                    $"Body length {body.Length} exceeds maximum {ushort.MaxValue} bytes supported by the 2-byte header.");

            byte[] framed = new byte[HeaderSize + body.Length];
            BitConverter.GetBytes((ushort)body.Length).CopyTo(framed, 0);
            body.CopyTo(framed, HeaderSize);
            return framed;
        }

        /// <inheritdoc/>
        public void RemoveClient(IClient client)
        {
            _buffers.TryRemove(client, out _);
        }

        // ─── Per-client accumulation buffer ─────────────────────────────────────

        private sealed class ClientBuffer
        {
            private byte[] _data = Array.Empty<byte>();
            private int _filled; // how many bytes are valid in _data

            /// <summary>Appends incoming bytes to the buffer, growing if necessary.</summary>
            public void Append(byte[] incoming)
            {
                int required = _filled + incoming.Length;

                if (required > _data.Length)
                {
                    // Grow by at least doubling to amortise allocations.
                    int newSize = Math.Max(required, Math.Max(_data.Length * 2, 64));
                    byte[] newData = new byte[newSize];
                    if (_filled > 0)
                        Array.Copy(_data, newData, _filled);
                    _data = newData;
                }

                Array.Copy(incoming, 0, _data, _filled, incoming.Length);
                _filled += incoming.Length;
            }

            /// <summary>
            /// Attempts to extract the next complete packet.
            /// Returns <c>true</c> and sets <paramref name="packet"/> when successful.
            /// Returns <c>false</c> when there are not enough bytes for a full packet yet.
            /// </summary>
            public bool TryReadPacket(out byte[]? packet)
            {
                if (_filled < HeaderSize)
                {
                    packet = null;
                    return false;
                }

                ushort bodyLength = BitConverter.ToUInt16(_data, 0);

                if (_filled < HeaderSize + bodyLength)
                {
                    packet = null;
                    return false;
                }

                packet = new byte[bodyLength];
                Array.Copy(_data, HeaderSize, packet, 0, bodyLength);

                // Shift remaining bytes to the front.
                int consumed = HeaderSize + bodyLength;
                _filled -= consumed;
                if (_filled > 0)
                    Array.Copy(_data, consumed, _data, 0, _filled);

                return true;
            }
        }
    }
}
