using System.Collections.Generic;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Network.Framing.Interfaces
{
    /// <summary>
    /// Pluggable packet framing strategy.
    /// The server calls <see cref="ProcessIncoming"/> for every TCP chunk received and fires
    /// <c>OnReceivedData</c> once per complete packet returned.
    /// Use <see cref="PassthroughFramer"/> to disable framing (raw bytes, backward-compatible).
    /// Use <see cref="LengthPrefixFramer"/> for the built-in length-prefix framing protocol.
    /// </summary>
    public interface IPacketFramer
    {
        /// <summary>
        /// Feeds raw TCP bytes for the given client into the framer.
        /// Returns zero or more complete, fully-assembled packets.
        /// </summary>
        IEnumerable<byte[]> ProcessIncoming(IClient client, byte[] rawData);

        /// <summary>
        /// Wraps an outgoing body with whatever framing header this framer expects.
        /// Called by <see cref="PacketWriter.BuildFramed"/> to produce the correct wire format.
        /// </summary>
        byte[] Frame(byte[] body);

        /// <summary>
        /// Removes any per-client state held by the framer.
        /// Must be called when a client disconnects.
        /// </summary>
        void RemoveClient(IClient client);
    }
}
