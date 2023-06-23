using System;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Network
{
    /// <summary>
    /// Converter bytes to id ushort type
    /// </summary>
    public class UshortConverterId : IConverterId<ushort>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public ushort Convert(byte[] bytes)
        {
            return BitConverter.ToUInt16(bytes, 0);
        }
    }
}
