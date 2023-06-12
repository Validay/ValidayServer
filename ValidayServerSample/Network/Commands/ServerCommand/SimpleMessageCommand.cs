using System.Text;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServerSample.Network.Commands.ServerCommand
{
    public class SimpleMessageCommand : IServerCommand
    {
        public short Id => 1;

        private const int countHeadBytes = 2;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Execute(
            IClient sender, 
            IReadOnlyCollection<IManager> managers, 
            byte[] rawData)
        {
            string stringData = Encoding.UTF8.GetString(
                rawData,
                countHeadBytes,
                rawData.Length - countHeadBytes);

            Console.WriteLine($"Client [{sender.Ip}:{sender.Port}]: {stringData}");
        }
    }
}