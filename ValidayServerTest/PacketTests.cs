using System;
using Xunit;
using ValidayServer.Network;

namespace ValidayServerTest
{
    public class PacketTests
    {
        // ─── PacketWriter.Build() ────────────────────────────────────────────────

        [Fact]
        public void PacketWriter_Build_StartsWithCommandId()
        {
            byte[] data = new PacketWriter(42).Build();

            ushort commandId = BitConverter.ToUInt16(data, 0);
            Assert.Equal((ushort)42, commandId);
        }

        [Fact]
        public void PacketWriter_Build_NoPayload_HasOnlyCommandId()
        {
            byte[] data = new PacketWriter(1).Build();

            Assert.Equal(sizeof(ushort), data.Length);
        }

        [Fact]
        public void PacketWriter_BuildFramed_HasLengthPrefixHeader()
        {
            byte[] framed = new PacketWriter(1).WriteInt(99).BuildFramed();

            ushort bodyLength = BitConverter.ToUInt16(framed, 0);
            // body = [commandId: 2] + [int: 4] = 6
            Assert.Equal((ushort)6, bodyLength);
            Assert.Equal(sizeof(ushort) + bodyLength, framed.Length);
        }

        // ─── Round-trip tests ────────────────────────────────────────────────────

        [Fact]
        public void RoundTrip_Byte()
        {
            byte[] data = new PacketWriter(1).WriteByte(200).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.Equal((byte)200, reader.ReadByte());
        }

        [Fact]
        public void RoundTrip_Bool_True()
        {
            byte[] data = new PacketWriter(1).WriteBool(true).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.True(reader.ReadBool());
        }

        [Fact]
        public void RoundTrip_Bool_False()
        {
            byte[] data = new PacketWriter(1).WriteBool(false).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.False(reader.ReadBool());
        }

        [Fact]
        public void RoundTrip_Short()
        {
            byte[] data = new PacketWriter(1).WriteShort(-1234).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.Equal((short)-1234, reader.ReadShort());
        }

        [Fact]
        public void RoundTrip_Ushort()
        {
            byte[] data = new PacketWriter(1).WriteUshort(65000).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.Equal((ushort)65000, reader.ReadUshort());
        }

        [Fact]
        public void RoundTrip_Int()
        {
            byte[] data = new PacketWriter(1).WriteInt(-987654321).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.Equal(-987654321, reader.ReadInt());
        }

        [Fact]
        public void RoundTrip_Uint()
        {
            byte[] data = new PacketWriter(1).WriteUint(3_000_000_000u).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.Equal(3_000_000_000u, reader.ReadUint());
        }

        [Fact]
        public void RoundTrip_Long()
        {
            long value = long.MinValue + 42;
            byte[] data = new PacketWriter(1).WriteLong(value).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.Equal(value, reader.ReadLong());
        }

        [Fact]
        public void RoundTrip_Float()
        {
            byte[] data = new PacketWriter(1).WriteFloat(3.14f).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.Equal(3.14f, reader.ReadFloat());
        }

        [Fact]
        public void RoundTrip_Double()
        {
            byte[] data = new PacketWriter(1).WriteDouble(Math.PI).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.Equal(Math.PI, reader.ReadDouble());
        }

        [Fact]
        public void RoundTrip_String()
        {
            byte[] data = new PacketWriter(1).WriteString("Hello, 🌍!").Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.Equal("Hello, 🌍!", reader.ReadString());
        }

        [Fact]
        public void RoundTrip_EmptyString()
        {
            byte[] data = new PacketWriter(1).WriteString(string.Empty).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.Equal(string.Empty, reader.ReadString());
        }

        [Fact]
        public void RoundTrip_NullString_TreatedAsEmpty()
        {
            byte[] data = new PacketWriter(1).WriteString(null).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.Equal(string.Empty, reader.ReadString());
        }

        [Fact]
        public void RoundTrip_Bytes()
        {
            byte[] payload = new byte[] { 1, 2, 3, 4, 5 };
            byte[] data = new PacketWriter(1).WriteBytes(payload, lengthPrefixed: true).Build();
            var reader = new PacketReader(data).SkipCommandId();
            Assert.Equal(payload, reader.ReadBytesWithLength());
        }

        [Fact]
        public void RoundTrip_MultipleFields()
        {
            byte[] data = new PacketWriter(7)
                .WriteInt(42)
                .WriteFloat(1.5f)
                .WriteBool(true)
                .WriteString("test")
                .Build();

            var reader = new PacketReader(data).SkipCommandId();

            Assert.Equal(42, reader.ReadInt());
            Assert.Equal(1.5f, reader.ReadFloat());
            Assert.True(reader.ReadBool());
            Assert.Equal("test", reader.ReadString());
        }

        [Fact]
        public void RoundTrip_CommandId_ReadableFromStart()
        {
            byte[] data = new PacketWriter(255).WriteInt(0).Build();
            var reader = new PacketReader(data);
            Assert.Equal((ushort)255, reader.ReadUshort()); // commandId at position 0
        }

        // ─── PacketReader safety ─────────────────────────────────────────────────

        [Fact]
        public void PacketReader_ReadPastEnd_ThrowsInvalidOperationException()
        {
            byte[] data = new PacketWriter(1).WriteByte(10).Build();
            var reader = new PacketReader(data).SkipCommandId();
            reader.ReadByte(); // consumes the 1 byte payload

            Assert.Throws<InvalidOperationException>(() => reader.ReadByte());
        }

        [Fact]
        public void PacketReader_Skip_AdvancesPosition()
        {
            byte[] data = new PacketWriter(1).WriteByte(0xAA).WriteByte(0xBB).Build();
            var reader = new PacketReader(data).SkipCommandId();
            reader.Skip(1); // skip 0xAA
            Assert.Equal((byte)0xBB, reader.ReadByte());
        }

        [Fact]
        public void PacketReader_Remaining_DecrementsOnRead()
        {
            byte[] data = new PacketWriter(1).WriteInt(0).Build();
            var reader = new PacketReader(data).SkipCommandId();
            int before = reader.Remaining;
            reader.ReadInt();
            Assert.Equal(before - sizeof(int), reader.Remaining);
        }

        [Fact]
        public void PacketWriter_NullBytes_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PacketWriter(1).WriteBytes(null!));
        }

        [Fact]
        public void PacketWriter_Build_WithFramer_UsesFramer()
        {
            var framer = new ValidayServer.Network.Framing.LengthPrefixFramer();
            byte[] framed = new PacketWriter(1).WriteInt(7).Build(framer);
            byte[] expected = new PacketWriter(1).WriteInt(7).BuildFramed();
            Assert.Equal(expected, framed);
        }
    }
}
