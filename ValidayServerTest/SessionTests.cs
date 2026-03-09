using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ValidayServer.Network;
using ValidayServer.Network.Interfaces;

namespace ValidayServerTest
{
    public class SessionTests
    {
        static IClientSession MakeSession() => new ClientSession();

        [Fact]
        public void ClientSession_Set_And_Get_Int()
        {
            var session = MakeSession();
            session.Set("score", 42);
            Assert.Equal(42, session.Get<int>("score"));
        }

        [Fact]
        public void ClientSession_Set_And_Get_String()
        {
            var session = MakeSession();
            session.Set("name", "Alice");
            Assert.Equal("Alice", session.Get<string>("name"));
        }

        [Fact]
        public void ClientSession_Set_And_Get_Bool()
        {
            var session = MakeSession();
            session.Set("ready", true);
            Assert.True(session.Get<bool>("ready"));
        }

        [Fact]
        public void ClientSession_Get_NonExistentKey_ReturnsDefault()
        {
            var session = MakeSession();
            int result = session.Get<int>("missing");
            Assert.Equal(default(int), result);
        }

        [Fact]
        public void ClientSession_TryGet_ExistingKey_ReturnsTrueAndValue()
        {
            var session = MakeSession();
            session.Set("hp", 100);

            bool found = session.TryGet("hp", out int value);

            Assert.True(found);
            Assert.Equal(100, value);
        }

        [Fact]
        public void ClientSession_TryGet_NonExistentKey_ReturnsFalse()
        {
            var session = MakeSession();

            bool found = session.TryGet("missing", out int value);

            Assert.False(found);
            Assert.Equal(default(int), value);
        }

        [Fact]
        public void ClientSession_Has_ExistingKey_ReturnsTrue()
        {
            var session = MakeSession();
            session.Set("key", "value");
            Assert.True(session.Has("key"));
        }

        [Fact]
        public void ClientSession_Has_NonExistentKey_ReturnsFalse()
        {
            var session = MakeSession();
            Assert.False(session.Has("nope"));
        }

        [Fact]
        public void ClientSession_Remove_KeyNoLongerExists()
        {
            var session = MakeSession();
            session.Set("temp", 1);
            session.Remove("temp");
            Assert.False(session.Has("temp"));
        }

        [Fact]
        public void ClientSession_Clear_AllKeysRemoved()
        {
            var session = MakeSession();
            session.Set("a", 1);
            session.Set("b", 2);
            session.Set("c", 3);

            session.Clear();

            Assert.False(session.Has("a"));
            Assert.False(session.Has("b"));
            Assert.False(session.Has("c"));
        }

        [Fact]
        public void ClientSession_Set_OverwritesExistingValue()
        {
            var session = MakeSession();
            session.Set("x", 1);
            session.Set("x", 99);
            Assert.Equal(99, session.Get<int>("x"));
        }

        [Fact]
        public void ClientSession_AttachedToClient()
        {
            var client = new FakeClient();
            Assert.NotNull(client.Session);
        }

        [Fact]
        public void ClientSession_ConcurrentAccess_DoesNotThrow()
        {
            var session = MakeSession();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
            {
                try
                {
                    session.Set($"key{i}", i);
                    session.Get<int>($"key{i}");
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));

            Task.WaitAll(tasks.ToArray());

            Assert.Empty(exceptions);
        }
    }
}
