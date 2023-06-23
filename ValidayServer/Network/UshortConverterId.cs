using System;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Network
{
    public class UshortConverterId : IConverterId<ushort>
    {
        public ushort Convert(byte[] bytes)
        {
            return BitConverter.ToUInt16(bytes, 0);
        }
    }
}
