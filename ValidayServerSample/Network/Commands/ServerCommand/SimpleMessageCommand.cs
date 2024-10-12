using System.Text;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServerSample.Network.Commands.ServerCommands
{
    public class SimpleMessageServerCommand : IServerCommand
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Execute(
            IClient sender, 
            byte[] rawData)
        {
            string stringData = Encoding.UTF8.GetString(
                rawData,
                sizeof(ushort),
                rawData.Length - sizeof(ushort));

            Console.WriteLine($"Client [{sender.Ip}:{sender.Port}]: {stringData}");
        }
    }
}