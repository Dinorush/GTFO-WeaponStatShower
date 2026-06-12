using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using WeaponStatShower.Dependencies;

namespace WeaponStatShower.Json
{
    public static class JSON
    {
        private static readonly JsonSerializerOptions _setting = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            IgnoreReadOnlyProperties = true,
        };

        static JSON()
        {
            _setting.Converters.Add(new JsonStringEnumConverter());
            if (PartialData.HasPData)
                _setting.Converters.Add(PartialData.PDataIDConverter);
        }

        public static T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _setting);
        }

        public static T? Deserialize<T>(ref Utf8JsonReader reader)
        {
            return JsonSerializer.Deserialize<T>(ref reader, _setting);
        }

        public static bool TryDeserializeSafe<T>(string json, [MaybeNullWhen(false)] out T value)
        {
            try
            {
                value = JsonSerializer.Deserialize<T>(json, _setting);
                return value != null;
            }
            catch (JsonException e)
            {
                WeaponStatShowerPlugin.LogError($"Caught exception while reading json: {e.Message}\n{e.StackTrace}");
                value = default;
                return false;
            }
        }

        public static bool TryDeserialize<T>(string json, [MaybeNullWhen(false)] out T value)
        {
            value = JsonSerializer.Deserialize<T>(json, _setting);
            return value != null;
        }

        public static bool TryDeserialize<T>(ref Utf8JsonReader reader, [MaybeNullWhen(false)] out T value)
        {
            value = JsonSerializer.Deserialize<T>(ref reader, _setting);
            return value != null;
        }

        public static object? Deserialize(Type type, string json)
        {
            return JsonSerializer.Deserialize(json, type, _setting);
        }

        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, _setting);
        }

        public static void Serialize<T>(Utf8JsonWriter writer, T value)
        {
            JsonSerializer.Serialize(writer, value, _setting);
        }

        public static void Serialize<T>(Utf8JsonWriter writer, string name, T value)
        {
            writer.WritePropertyName(name);
            JsonSerializer.Serialize(writer, value, _setting);
        }
    }
}
