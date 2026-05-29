namespace PythonWiki.Models
{
    public class IoTDeviceConfig
    {
        public string DeviceId { get; set; } = default!;
        public string Keyword { get; set; } = "python";
        public bool OutputEnabled { get; set; } = true;
    }
}
