using Microsoft.EntityFrameworkCore;
using PythonWiki.Dtos.IoT;
using PythonWiki.Models;
using PythonWiki.Persistence.DbContext;
using PythonWiki.Services.Interfaces;

namespace PythonWiki.Services.Implementations
{
    public class EfIoTService : IIoTService
    {
        private readonly PythonWikiDbContext _context;

        public EfIoTService(PythonWikiDbContext context)
        {
            _context = context;
        }

        public IoTDeviceConfig GetConfig(string deviceId)
        {
            var config = _context.IoTDeviceConfigs
                .FirstOrDefault(c => c.DeviceId == deviceId);

            if (config == null)
            {
                config = new IoTDeviceConfig
                {
                    DeviceId = deviceId,
                    Keyword = "python",
                    OutputEnabled = true
                };

                _context.IoTDeviceConfigs.Add(config);
                _context.SaveChanges();
            }

            return config;
        }

        public IoTDeviceConfig UpdateConfig(string deviceId, DeviceConfigDto dto)
        {
            var config = _context.IoTDeviceConfigs
                .FirstOrDefault(c => c.DeviceId == deviceId);

            if (config == null)
            {
                config = new IoTDeviceConfig
                {
                    DeviceId = deviceId
                };

                _context.IoTDeviceConfigs.Add(config);
            }

            config.Keyword = dto.Keyword;
            config.OutputEnabled = dto.OutputEnabled;

            _context.SaveChanges();
            return config;
        }

        public void LogTelemetry(TelemetryDto dto)
        {
            var entry = new IoTTelemetry
            {
                DeviceId = dto.DeviceId,
                Keyword = dto.Keyword,
                Success = dto.Success,
                ResultLength = dto.ResultLength,
                Timestamp = dto.Timestamp
            };

            _context.IoTTelemetries.Add(entry);
            _context.SaveChanges();
        }

        public IEnumerable<IoTTelemetry> GetLogs()
        {
            return _context.IoTTelemetries
                .OrderByDescending(x => x.Timestamp)
                .ToList();
        }
    }
}
