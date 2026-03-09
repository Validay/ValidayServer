using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ValidayServer.Network;
using ValidayServer.Network.Framing;
using ValidayServer.Network.Interfaces;

namespace ValidayServerTest
{
    public class FramingTests
    {
        static IClient MakeClient() => new FakeClient();

        // ─── PassthroughFramer ────────────────────────────────────────────────────

        [Fact]
        public void PassthroughFramer_ReturnsRawDataUnchanged()
        {
            var framer = new PassthroughFramer();
            byte[] input = new byte[] { 1, 2, 3, 4 };

            var packets = framer.ProcessIncoming(MakeClient(), input).ToList();

            Assert.Single(packets);
            Assert.Equal(input, packets[0]);
        }

        [Fact]
        public void PassthroughFramer_Frame_ReturnsSameArray()
        {
            var framer = new PassthroughFramer();
            byte[] body = new byte[] { 10, 20 };
            Assert.Same(body, framer.Frame(body));
        }

        // ─── LengthPrefixFramer — single complete packet ──────────────────────────

        [Fact]
        public void LengthPrefixFramer_SingleCompletePacket_Returned()
        {
            var framer = new LengthPrefixFramer();
            var client = MakeClient();
            byte[] packet = new PacketWriter(1).WriteInt(42).BuildFramed();

            var results = framer.ProcessIncoming(client, packet).ToList();

            Assert.Single(results);
            // Body = [commandId: 2][int: 4] = 6 bytes
            Assert.Equal(6, results[0].Length);
        }

        [Fact]
        public void LengthPrefixFramer_CorrectBodyContent()
        {
            var framer = new LengthPrefixFramer();
            var client = MakeClient();

            byte[] framed = new PacketWriter(77).WriteInt(12345).BuildFramed();
            byte[] body = framer.ProcessIncoming(client, framed).Single();

            var reader = new PacketReader(body).SkipCommandId();
            Assert.Equal(12345, reader.ReadInt());
        }

        // ─── Fragmentation (split across multiple receives) ───────────────────────

        [Fact]
        public void LengthPrefixFramer_FragmentedPacket_ReassembledCorrectly()
        {
            var framer = new LengthPrefixFramer();
            var client = MakeClient();
            byte[] full = new PacketWriter(1).WriteString("hello").BuildFramed();

            // Split at an arbitrary byte boundary.
            int splitAt = full.Length / 2;
            byte[] part1 = full[..splitAt];
            byte[] part2 = full[splitAt..];

            var after1 = framer.ProcessIncoming(client, part1).ToList();
            Assert.Empty(after1); // Not complete yet.

            var after2 = framer.ProcessIncoming(client, part2).ToList();
            Assert.Single(after2);

            string text = new PacketReader(after2[0]).SkipCommandId().ReadString();
            Assert.Equal("hello", text);
        }

        [Fact]
        public void LengthPrefixFramer_OneByteParts_ReassembledCorrectly()
        {
            var framer = new LengthPrefixFramer();
            var client = MakeClient();
            byte[] full = new PacketWriter(1).WriteByte(0xAB).BuildFramed();

            List<byte[]> packets = new List<byte[]>();
            foreach (byte b in full)
                packets.AddRange(framer.ProcessIncoming(client, new byte[] { b }));

            Assert.Single(packets);
            Assert.Equal((byte)0xAB, new PacketReader(packets[0]).SkipCommandId().ReadByte());
        }

        // ─── Coalescing (multiple packets in one receive) ─────────────────────────

        [Fact]
        public void LengthPrefixFramer_TwoPacketsInOneChunk_BothReturned()
        {
            var framer = new LengthPrefixFramer();
            var client = MakeClient();

            byte[] pkt1 = new PacketWriter(1).WriteInt(11).BuildFramed();
            byte[] pkt2 = new PacketWriter(2).WriteInt(22).BuildFramed();
            byte[] combined = pkt1.Concat(pkt2).ToArray();

            var results = framer.ProcessIncoming(client, combined).ToList();

            Assert.Equal(2, results.Count);

            int val1 = new PacketReader(results[0]).SkipCommandId().ReadInt();
            int val2 = new PacketReader(results[1]).SkipCommandId().ReadInt();

            Assert.Equal(11, val1);
            Assert.Equal(22, val2);
        }

        [Fact]
        public void LengthPrefixFramer_ManyPackets_AllReturned()
        {
            var framer = new LengthPrefixFramer();
            var client = MakeClient();

            byte[] combined = Enumerable.Range(0, 10)
                .Select(i => new PacketWriter(1).WriteInt(i).BuildFramed())
                .SelectMany(b => b)
                .ToArray();

            var results = framer.ProcessIncoming(client, combined).ToList();

            Assert.Equal(10, results.Count);
            for (int i = 0; i < 10; i++)
                Assert.Equal(i, new PacketReader(results[i]).SkipCommandId().ReadInt());
        }

        // ─── Edge cases ───────────────────────────────────────────────────────────

        [Fact]
        public void LengthPrefixFramer_EmptyData_ReturnsNothing()
        {
            var framer = new LengthPrefixFramer();
            var results = framer.ProcessIncoming(MakeClient(), Array.Empty<byte>()).ToList();
            Assert.Empty(results);
        }

        [Fact]
        public void LengthPrefixFramer_OnlyHeader_NoPacketYet()
        {
            var framer = new LengthPrefixFramer();
            var client = MakeClient();
            byte[] headerOnly = BitConverter.GetBytes((ushort)10); // says body is 10 bytes, but no body follows

            var results = framer.ProcessIncoming(client, headerOnly).ToList();

            Assert.Empty(results);
        }

        [Fact]
        public void LengthPrefixFramer_MinimalPacket_CommandIdOnly()
        {
            var framer = new LengthPrefixFramer();
            var client = MakeClient();
            byte[] framed = new PacketWriter(5).BuildFramed(); // body = just commandId (2 bytes)

            var results = framer.ProcessIncoming(client, framed).ToList();

            Assert.Single(results);
            Assert.Equal((ushort)5, new PacketReader(results[0]).ReadUshort());
        }

        // ─── RemoveClient ─────────────────────────────────────────────────────────

        [Fact]
        public void LengthPrefixFramer_RemoveClient_ClearsBuffer()
        {
            var framer = new LengthPrefixFramer();
            var client = MakeClient();

            // Feed a partial packet (just the header) to establish buffer state.
            byte[] pkt1 = new PacketWriter(1).WriteInt(12345).BuildFramed();
            var partial = framer.ProcessIncoming(client, pkt1[..4]).ToList(); // header + partial body
            Assert.Empty(partial); // not complete yet

            // Remove the client — should discard all accumulated state.
            framer.RemoveClient(client);

            // Now send a COMPLETE fresh packet for the same client object.
            // If state was properly cleared, we should get exactly this new packet back.
            byte[] pkt2 = new PacketWriter(99).WriteInt(99999).BuildFramed();
            var results = framer.ProcessIncoming(client, pkt2).ToList();

            Assert.Single(results);
            var reader = new PacketReader(results[0]);
            Assert.Equal((ushort)99, reader.ReadUshort()); // correct commandId, not leftover from pkt1
            Assert.Equal(99999, reader.ReadInt());
        }

        [Fact]
        public void LengthPrefixFramer_Frame_TooLargeBody_ThrowsArgumentOutOfRangeException()
        {
            var framer = new LengthPrefixFramer();
            byte[] tooBig = new byte[ushort.MaxValue + 1];

            Assert.Throws<ArgumentOutOfRangeException>(() => framer.Frame(tooBig));
        }

        // ─── Multi-client isolation ───────────────────────────────────────────────

        [Fact]
        public void LengthPrefixFramer_MultipleClients_HaveIndependentBuffers()
        {
            var framer = new LengthPrefixFramer();
            var clientA = MakeClient();
            var clientB = MakeClient();

            byte[] framedA = new PacketWriter(1).WriteInt(100).BuildFramed();
            byte[] framedB = new PacketWriter(2).WriteInt(200).BuildFramed();

            var resultsA = framer.ProcessIncoming(clientA, framedA).ToList();
            var resultsB = framer.ProcessIncoming(clientB, framedB).ToList();

            Assert.Single(resultsA);
            Assert.Single(resultsB);
            Assert.Equal(100, new PacketReader(resultsA[0]).SkipCommandId().ReadInt());
            Assert.Equal(200, new PacketReader(resultsB[0]).SkipCommandId().ReadInt());
        }
    }
}
