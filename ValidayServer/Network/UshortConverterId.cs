using System;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Network
{
    /// <summary>
    /// Converts the first two bytes of a byte array to a little-endian <see cref="ushort"/> command ID.
    /// </summary>
    public class UshortConverterId : IConverterId<ushort>
    {
        /// <inheritdoc/>
        /// <exception cref="ArgumentException">Thrown when <paramref name="bytes"/> has fewer than 2 elements.</exception>
        public ushort Convert(byte[] bytes)
        {
            if (bytes == null || bytes.Length < sizeof(ushort))
                throw new ArgumentException(
                    $"Buffer must contain at least {sizeof(ushort)} bytes to extract a ushort command ID, " +
                    $"but received {bytes?.Length ?? 0} bytes.",
                    nameof(bytes));

            return BitConverter.ToUInt16(bytes, 0);
        }
    }
}
