using System.Text;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Commands.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServerSample.Network.Commands.ServerCommand
{
    /// <summary>
    /// A simple command to convert the received data to a string 
    /// and display it in the console
    /// </summary>
    public class SimpleMessageCommand : IServerCommand
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public ushort Id => 1;

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
                sizeof(short),
                rawData.Length - sizeof(short));

            Console.WriteLine($"Client [{sender.Ip}:{sender.Port}]: {stringData}");
        }
    }
}