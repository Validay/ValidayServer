using System.Collections.Generic;
using ValidayServer.Network.Framing.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Network.Framing
{
    /// <summary>
    /// No-op framer — passes raw bytes through unchanged.
    /// This is the default framer and preserves the original library behaviour.
    /// Use it when the application-layer protocol handles its own framing,
    /// or when packets always fit into a single TCP segment.
    /// </summary>
    public sealed class PassthroughFramer : IPacketFramer
    {
        /// <inheritdoc/>
        public IEnumerable<byte[]> ProcessIncoming(IClient client, byte[] rawData)
        {
            yield return rawData;
        }

        /// <inheritdoc/>
        public byte[] Frame(byte[] body) => body;

        /// <inheritdoc/>
        public void RemoveClient(IClient client) { }
    }
}
