namespace PythonWiki.Models
{
    public class IoTTelemetry
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = default!;
        public string Keyword { get; set; } = default!;
        public bool Success { get; set; }
        public int ResultLength { get; set; }
        public long Timestamp { get; set; }
    }
}
