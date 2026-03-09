using System;
using Xunit;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers;
using ValidayServer.Network;
using ValidayServer.Network.Commands;
using ValidayServer.Network.Interfaces;

namespace ValidayServerTest
{
    public class TypedCommandTests
    {
        // ─── Test payload and command ─────────────────────────────────────────────

        record MovePayload(float X, float Y);

        class MoveCommand : ServerCommandBase<MovePayload>
        {
            public MovePayload? LastPayload { get; private set; }
            public IClient? LastSender { get; private set; }

            protected override MovePayload Read(PacketReader reader)
                => new MovePayload(reader.ReadFloat(), reader.ReadFloat());

            protected override void Handle(IClient sender, MovePayload payload)
            {
                LastPayload = payload;
                LastSender = sender;
            }
        }

        static byte[] BuildMovePacket(ushort commandId, float x, float y)
        {
            return new PacketWriter(commandId)
                .WriteFloat(x)
                .WriteFloat(y)
                .Build();
        }

        [Fact]
        public void ServerCommandBase_Execute_DeserializesPayload()
        {
            var cmd = new MoveCommand();
            var client = new FakeClient();
            byte[] packet = BuildMovePacket(1, 3.5f, 7.25f);

            cmd.Execute(client, packet);

            Assert.NotNull(cmd.LastPayload);
        }

        [Fact]
        public void ServerCommandBase_Execute_PassesSenderCorrectly()
        {
            var cmd = new MoveCommand();
            var client = new FakeClient("10.0.0.1", 8080);
            byte[] packet = BuildMovePacket(1, 0f, 0f);

            cmd.Execute(client, packet);

            Assert.Same(client, cmd.LastSender);
        }

        [Fact]
        public void ServerCommandBase_Execute_PayloadFieldsCorrect()
        {
            var cmd = new MoveCommand();
            var client = new FakeClient();
            byte[] packet = BuildMovePacket(1, 12.5f, -3.0f);

            cmd.Execute(client, packet);

            Assert.NotNull(cmd.LastPayload);
            Assert.Equal(12.5f, cmd.LastPayload!.X, precision: 4);
            Assert.Equal(-3.0f, cmd.LastPayload.Y, precision: 4);
        }

        [Fact]
        public void ServerCommandBase_IntegrationWith_CommandHandlerManager()
        {
            const ushort MoveCommandId = 42;

            var server = new FakeServer();
            ILogger logger = new ConsoleLogger(LogType.Info);
            var handler = new CommandHandlerManager(server, logger);
            handler.RegistrationCommand<MoveCommand>(MoveCommandId);
            handler.Start();

            var client = new FakeClient();
            byte[] packet = BuildMovePacket(MoveCommandId, 1.0f, 2.0f);

            // No exception expected — command is dispatched and executed via the pool.
            var ex = Record.Exception(() =>
                server.SimulateDataReceived(client, packet));

            Assert.Null(ex);
        }
    }
}
