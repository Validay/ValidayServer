using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Network.Commands
{
    /// <summary>
    /// Base class for typed server commands that auto-deserialize packet payload via <see cref="PacketReader"/>.
    /// <para>
    /// Subclasses implement <see cref="Read"/> to deserialize the payload and
    /// <see cref="Handle"/> to process the result.  The 2-byte command ID is
    /// skipped automatically before <see cref="Read"/> is called.
    /// </para>
    /// </summary>
    /// <typeparam name="TPayload">The strongly-typed payload produced by <see cref="Read"/>.</typeparam>
    public abstract class ServerCommandBase<TPayload> : IServerCommand
    {
        /// <inheritdoc/>
        public void Execute(IClient sender, byte[] rawData)
        {
            PacketReader reader = new PacketReader(rawData).SkipCommandId();
            TPayload payload = Read(reader);
            Handle(sender, payload);
        }

        /// <summary>
        /// Deserializes the payload from the reader (positioned after the command ID).
        /// </summary>
        protected abstract TPayload Read(PacketReader reader);

        /// <summary>
        /// Processes the deserialized payload for the given client.
        /// </summary>
        protected abstract void Handle(IClient sender, TPayload payload);
    }
}
