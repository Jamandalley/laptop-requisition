using System.Text.Json;
using LaptopRequisition.Domain.Enums;

namespace LaptopRequisition.Domain.Common;

public static class ErrorMessageProvider
{
    private static readonly Lazy<Dictionary<string, string>> Messages = new(LoadMessages);

    private static Dictionary<string, string> LoadMessages()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "errorMessages.json");

        if (!File.Exists(path))
            throw new FileNotFoundException("Error message JSON file not found.");

        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
               ?? new Dictionary<string, string>();
    }

    public static string Get(ResponseCode code)
    {
        var key = code.ToString();

        return Messages.Value.TryGetValue(key, out var msg)
            ? msg
            : $"No message defined for {key}";
    }
}