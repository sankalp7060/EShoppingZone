using System.Text.Json;
using System.Text.Json.Serialization;

namespace EShoppingZone.Common.Helpers
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };

        public static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, _options);

        public static T? Deserialize<T>(string json) =>
            JsonSerializer.Deserialize<T>(json, _options);
    }
}
