using System.Diagnostics;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;
using ValidayServer.Managers.Interfaces;
using ValidayServer.Network.Interfaces;

namespace ValidayServerSample.Managers
{
    /// <summary>
    /// Manager for info server
    /// </summary>
    public class ConsoleInfoManager : IManager
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string Name => nameof(ConsoleInfoManager);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool IsActive { get; set; }

        private float _intervalUpdateInfoSeconds;
        private Timer? _updateInfoTimer;
        private TimeSpan _previousCpuTime;
        private DateTime _previousUpdateTime;
        private IServer? _server;
        private ILogger? _logger;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Initialize(
            IServer server, 
            ILogger logger)
        {
            _intervalUpdateInfoSeconds = 1f;
            _server = server;
            _logger = logger;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Start()
        {
            if (_server == null)
            {
                _logger?.Log(
                    $"{nameof(ConsoleInfoManager)}: _server is null!",
                    LogType.Warning);

                return;
            }

            IsActive = true;

            _updateInfoTimer = new Timer(
                UpdateInfo,
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(_intervalUpdateInfoSeconds));

            _logger?.Log(
                $"{nameof(ConsoleInfoManager)} started!",
                LogType.Info);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Stop()
        {
            if (_server == null)
                return;

            IsActive = false;

            _updateInfoTimer?.Dispose();
            _updateInfoTimer = null;

            _logger?.Log(
                 $"{nameof(ConsoleInfoManager)} stopped!",
                LogType.Info);
        }

        private void UpdateInfo(object? state)
        {
            Console.Title = $"Validay Server Sample" +
                $" | Memory: {GetMemoryUsage()  / (1024 * 1024)} MB" +
                $" | CPU: {GetCPUUsage() / 100:0.0}%" +
                $" | Active connections: {_server?.GetAllConnections().Count}";
        }

        private long GetMemoryUsage()
        {
            Process currentProcess = Process.GetCurrentProcess();
            long memoryUsageBytes = currentProcess.WorkingSet64;

            return memoryUsageBytes;
        }

        private float GetCPUUsage()
        {
            Process currentProcess = Process.GetCurrentProcess();
            DateTime currentTime = DateTime.Now;
            TimeSpan currentCpuTime = currentProcess.TotalProcessorTime;           
            TimeSpan cpuUsageTime = currentCpuTime - _previousCpuTime;
            TimeSpan elapsedTime = currentTime - _previousUpdateTime;
            float cpuUsagePercentage = (float)(cpuUsageTime.TotalMilliseconds / elapsedTime.TotalMilliseconds) * 100;

            _previousCpuTime = currentCpuTime;
            _previousUpdateTime = currentTime;

            return cpuUsagePercentage;
        }
    }
}
