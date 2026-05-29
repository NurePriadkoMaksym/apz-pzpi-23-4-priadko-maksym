using PythonWiki.Dtos.IoT;
using PythonWiki.Models;
using PythonWiki.Services.Interfaces;

namespace PythonWiki.Services.Implementations
{
    public class IoTService : IIoTService
    {
        private static readonly Dictionary<string, IoTDeviceConfig> _configs = new();
        private static readonly List<IoTTelemetry> _logs = new();

        public IoTDeviceConfig GetConfig(string deviceId)
        {
            if (!_configs.ContainsKey(deviceId))
            {
                _configs[deviceId] = new IoTDeviceConfig
                {
                    DeviceId = deviceId,
                    Keyword = "python",
                    OutputEnabled = true
                };
            }

            return _configs[deviceId];
        }

        public IoTDeviceConfig UpdateConfig(string deviceId, DeviceConfigDto dto)
        {
            var config = GetConfig(deviceId);

            config.Keyword = dto.Keyword;
            config.OutputEnabled = dto.OutputEnabled;

            return config;
        }

        public void LogTelemetry(TelemetryDto dto)
        {
            _logs.Add(new IoTTelemetry
            {
                DeviceId = dto.DeviceId,
                Keyword = dto.Keyword,
                Success = dto.Success,
                ResultLength = dto.ResultLength,
                Timestamp = dto.Timestamp
            });
        }

        public IEnumerable<IoTTelemetry> GetLogs()
        {
            return _logs;
        }
    }
}
