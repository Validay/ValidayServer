using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServerTest
{
    public class RoomTests
    {
        class FakeCommand : IClientCommand
        {
            public byte[] GetRawData() => new byte[] { 0x01, 0x00 };
        }

        static (FakeServer server, RoomManager manager) MakeRoomManager(bool destroyEmptyRooms = true)
        {
            var server = new FakeServer();
            var logger = new ConsoleLogger(LogType.Info);
            var manager = new RoomManager(server, logger, destroyEmptyRooms);
            manager.Start();
            return (server, manager);
        }

        // ─── RoomManager CRUD ────────────────────────────────────────────────────

        [Fact]
        public void RoomManager_Create_RoomExists()
        {
            var (_, manager) = MakeRoomManager();
            manager.Create("lobby");
            Assert.NotNull(manager.Get("lobby"));
        }

        [Fact]
        public void RoomManager_Create_DuplicateId_ThrowsInvalidOperationException()
        {
            var (_, manager) = MakeRoomManager();
            manager.Create("lobby");
            Assert.Throws<InvalidOperationException>(() => manager.Create("lobby"));
        }

        [Fact]
        public void RoomManager_Get_ExistingRoom_ReturnsRoom()
        {
            var (_, manager) = MakeRoomManager();
            var created = manager.Create("arena");
            var fetched = manager.Get("arena");
            Assert.Same(created, fetched);
        }

        [Fact]
        public void RoomManager_Get_NonExistentRoom_ReturnsNull()
        {
            var (_, manager) = MakeRoomManager();
            Assert.Null(manager.Get("ghost"));
        }

        [Fact]
        public void RoomManager_GetOrCreate_CreatesIfNotExists()
        {
            var (_, manager) = MakeRoomManager();
            var room = manager.GetOrCreate("new-room");
            Assert.NotNull(room);
            Assert.Equal("new-room", room.Id);
        }

        [Fact]
        public void RoomManager_GetOrCreate_ReturnsExistingIfExists()
        {
            var (_, manager) = MakeRoomManager();
            var first = manager.Create("room");
            var second = manager.GetOrCreate("room");
            Assert.Same(first, second);
        }

        [Fact]
        public void RoomManager_Destroy_RoomNoLongerExists()
        {
            var (_, manager) = MakeRoomManager();
            manager.Create("temp");
            manager.Destroy("temp");
            Assert.Null(manager.Get("temp"));
        }

        [Fact]
        public void RoomManager_Destroy_NonExistentRoom_ReturnsFalse()
        {
            var (_, manager) = MakeRoomManager();
            bool result = manager.Destroy("ghost");
            Assert.False(result);
        }

        // ─── Room membership ─────────────────────────────────────────────────────

        [Fact]
        public void Room_Join_ClientAddedToMembers()
        {
            var (_, manager) = MakeRoomManager();
            var room = manager.Create("r");
            var client = new FakeClient();

            room.Join(client);

            Assert.True(room.Contains(client));
        }

        [Fact]
        public void Room_Join_SameClientTwice_ReturnsFalse()
        {
            var (_, manager) = MakeRoomManager();
            var room = manager.Create("r");
            var client = new FakeClient();

            room.Join(client);
            bool second = room.Join(client);

            Assert.False(second);
        }

        [Fact]
        public void Room_Leave_ClientRemovedFromMembers()
        {
            var (_, manager) = MakeRoomManager();
            var room = manager.Create("r");
            var client = new FakeClient();

            room.Join(client);
            room.Leave(client);

            Assert.False(room.Contains(client));
        }

        [Fact]
        public void Room_Leave_NonMember_ReturnsFalse()
        {
            var (_, manager) = MakeRoomManager();
            var room = manager.Create("r");
            var client = new FakeClient();

            bool result = room.Leave(client);

            Assert.False(result);
        }

        [Fact]
        public void Room_Contains_Member_ReturnsTrue()
        {
            var (_, manager) = MakeRoomManager();
            var room = manager.Create("r");
            var client = new FakeClient();
            room.Join(client);
            Assert.True(room.Contains(client));
        }

        [Fact]
        public void Room_Contains_NonMember_ReturnsFalse()
        {
            var (_, manager) = MakeRoomManager();
            var room = manager.Create("r");
            var client = new FakeClient();
            Assert.False(room.Contains(client));
        }

        [Fact]
        public void Room_IsEmpty_WhenNoMembers()
        {
            var (_, manager) = MakeRoomManager();
            var room = manager.Create("r");
            Assert.True(room.IsEmpty);
        }

        [Fact]
        public void Room_Count_ReflectsMemberCount()
        {
            var (_, manager) = MakeRoomManager();
            var room = manager.Create("r");
            var c1 = new FakeClient("1.1.1.1", 1);
            var c2 = new FakeClient("2.2.2.2", 2);

            room.Join(c1);
            room.Join(c2);

            Assert.Equal(2, room.Count);
        }

        // ─── Disconnect auto-cleanup ──────────────────────────────────────────────

        [Fact]
        public void RoomManager_ClientDisconnects_AutoRemovedFromRoom()
        {
            var (server, manager) = MakeRoomManager(destroyEmptyRooms: false);
            var room = manager.Create("r");
            var client = new FakeClient();

            room.Join(client);
            server.SimulateClientDisconnected(client);

            Assert.False(room.Contains(client));
        }

        [Fact]
        public void RoomManager_ClientDisconnects_EmptyRoomDestroyed_WhenDestroyEmptyRooms()
        {
            var (server, manager) = MakeRoomManager(destroyEmptyRooms: true);
            var room = manager.Create("r");
            var client = new FakeClient();

            room.Join(client);
            server.SimulateClientDisconnected(client);

            Assert.Null(manager.Get("r"));
        }

        [Fact]
        public void RoomManager_ClientDisconnects_EmptyRoomKept_WhenNotDestroyEmptyRooms()
        {
            var (server, manager) = MakeRoomManager(destroyEmptyRooms: false);
            var room = manager.Create("r");
            var client = new FakeClient();

            room.Join(client);
            server.SimulateClientDisconnected(client);

            Assert.NotNull(manager.Get("r"));
        }

        [Fact]
        public void RoomManager_GetRoomsOf_ReturnsCorrectRooms()
        {
            var (_, manager) = MakeRoomManager();
            var r1 = manager.Create("r1");
            var r2 = manager.Create("r2");
            var r3 = manager.Create("r3");
            var client = new FakeClient();

            r1.Join(client);
            r3.Join(client);

            var rooms = manager.GetRoomsOf(client);

            Assert.Equal(2, rooms.Count);
            Assert.Contains(r1, rooms);
            Assert.Contains(r3, rooms);
            Assert.DoesNotContain(r2, rooms);
        }

        // ─── Broadcast ───────────────────────────────────────────────────────────

        [Fact]
        public void Room_Broadcast_SendsToAllMembers()
        {
            var (server, manager) = MakeRoomManager();
            var room = manager.Create("r");
            var c1 = new FakeClient("1.1.1.1", 1);
            var c2 = new FakeClient("2.2.2.2", 2);

            room.Join(c1);
            room.Join(c2);

            var cmd = new FakeCommand();
            room.Broadcast(cmd);

            var sentClients = server.SentCommands.Select(s => s.Client).ToList();
            Assert.Contains(c1, sentClients);
            Assert.Contains(c2, sentClients);
        }

        [Fact]
        public void Room_BroadcastExcept_SkipsExcludedClient()
        {
            var (server, manager) = MakeRoomManager();
            var room = manager.Create("r");
            var c1 = new FakeClient("1.1.1.1", 1);
            var c2 = new FakeClient("2.2.2.2", 2);
            var c3 = new FakeClient("3.3.3.3", 3);

            room.Join(c1);
            room.Join(c2);
            room.Join(c3);

            var cmd = new FakeCommand();
            room.BroadcastExcept(cmd, c2);

            var sentClients = server.SentCommands.Select(s => s.Client).ToList();
            Assert.Contains(c1, sentClients);
            Assert.DoesNotContain(c2, sentClients);
            Assert.Contains(c3, sentClients);
        }

        // ─── Constructor validation ───────────────────────────────────────────────

        [Fact]
        public void RoomManager_NullServer_ThrowsArgumentNullException()
        {
            var logger = new ConsoleLogger(LogType.Info);
            Assert.Throws<ArgumentNullException>(() =>
                new RoomManager(null!, logger));
        }

        [Fact]
        public void RoomManager_NullLogger_ThrowsArgumentNullException()
        {
            var server = new FakeServer();
            Assert.Throws<ArgumentNullException>(() =>
                new RoomManager(server, null!));
        }
    }
}
