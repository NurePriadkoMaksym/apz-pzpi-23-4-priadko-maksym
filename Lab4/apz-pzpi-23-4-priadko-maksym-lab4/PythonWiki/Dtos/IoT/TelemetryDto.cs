namespace PythonWiki.Dtos.IoT
{
    public class TelemetryDto
    {
        public string DeviceId { get; set; } = default!;
        public string Keyword { get; set; } = default!;
        public bool Success { get; set; }
        public int ResultLength { get; set; }
        public long Timestamp { get; set; }
    }
}
