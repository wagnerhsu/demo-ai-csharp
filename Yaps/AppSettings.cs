using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yaps;

public sealed class ScanDefaults
{
    public int StartPort { get; set; } = 3500;
    public int StopPort { get; set; } = 3600;
    public int TimeoutMs { get; set; } = 2500;
    public int Simultaneous { get; set; } = 100;
    public string StartAddress { get; set; } = "192.168.100.1";
    public string StopAddress { get; set; } = "192.168.100.1";
    public ScanOrder ScanOrder { get; set; } = ScanOrder.PortsFirst;
    public bool ResolveNames { get; set; }
    public bool Continuous { get; set; }
    public bool HideErrors { get; set; }
    public bool ProbePorts { get; set; }
    public bool Hex { get; set; }
}

public sealed class AppSettings
{
    private const string FileName = "appsettings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ScanDefaults Defaults { get; set; } = new();

    // A missing file falls back to built-in defaults; a broken file does too, with the reason in `error`.
    public static AppSettings Load(out string? error)
    {
        error = null;
        var path = Path.Combine(AppContext.BaseDirectory, FileName);
        if (!File.Exists(path)) return new AppSettings();

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            error = $"Could not read {FileName}, using built-in defaults.\r\n\r\n{ex.Message}";
            return new AppSettings();
        }
    }
}
