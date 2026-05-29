using PythonWiki.Dtos.IoT;
using PythonWiki.Models;

namespace PythonWiki.Services.Interfaces
{
    public interface IIoTService
    {
        IoTDeviceConfig GetConfig(string deviceId);
        IoTDeviceConfig UpdateConfig(string deviceId, DeviceConfigDto dto);
        void LogTelemetry(TelemetryDto dto);
        IEnumerable<IoTTelemetry> GetLogs();
    }
}
