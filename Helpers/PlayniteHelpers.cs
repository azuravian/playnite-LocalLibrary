using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LocalLibrary.Helpers
{
    public class ListCollectionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // Accept any IList (List<T>, ObservableCollection<T>, etc.)
            return typeof(IList).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is IEnumerable collection)
            {
                var names = new List<string>();

                foreach (var item in collection)
                {
                    var nameProp = item?.GetType().GetProperty("Name");
                    if (nameProp != null)
                    {
                        var nameVal = nameProp.GetValue(item)?.ToString();
                        if (!string.IsNullOrEmpty(nameVal))
                            names.Add(nameVal);
                    }
                }

                serializer.Serialize(writer, names);
            }
            else
            {
                writer.WriteNull();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var stringList = serializer.Deserialize<List<string>>(reader) ?? new List<string>();

            // Determine the item type in the collection
            var itemType = objectType.IsGenericType ? objectType.GetGenericArguments()[0] : typeof(object);

            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));

            foreach (var name in stringList)
            {
                // Use the Playnite API to reconstruct the object by name if possible
                object item = null;

                if (itemType == typeof(Tag) || itemType == typeof(Platform) || itemType == typeof(Genre))
                    item = Activator.CreateInstance(itemType, name);
                else
                {
                    // Fallback: create an instance and set Name property
                    item = Activator.CreateInstance(itemType);
                    var prop = itemType.GetProperty("Name");
                    if (prop != null)
                        prop.SetValue(item, name);
                }

                if (item != null)
                    list.Add(item);
            }

            return list;
        }
    }
    public class CustomDateTimeConverter : JsonConverter
    {
        private const string Format = "yyyy-MM-dd HH:mm:ss";
        private static readonly ILogger logger = LogManager.GetLogger();

        public override bool CanConvert(Type objectType)
        {
            // Support DateTime and Nullable<DateTime>
            return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                // Return null for DateTime?, or DateTime.MinValue for non-nullable
                if (objectType == typeof(DateTime?))
                    return null;

                return DateTime.MinValue;
            }

            var dateString = reader.Value.ToString();
            DateTime parsed;

            if (DateTime.TryParseExact(dateString, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return parsed;

            // Fallback to general parsing with multiple format attempts
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
                return parsed;

            // If all parsing attempts fail, log the issue and return a sensible default
            logger.Warn($"Failed to parse datetime string '{dateString}'. Expected format: '{Format}'. Returning default value.");

            if (objectType == typeof(DateTime?))
                return null;

            return DateTime.MinValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DateTime)
            {
                var dateTime = (DateTime)value;
                writer.WriteValue(dateTime.ToString(Format, CultureInfo.InvariantCulture));
            }
            else
            {
                writer.WriteNull();
            }
        }
    }

    public class ReleaseDateConverter : JsonConverter
    {
        private static readonly string[] AcceptedFormats = new[]
        {
        "M/d/yyyy", "MM/dd/yyyy", "M/d/yy",
        "yyyy-MM-dd", "yyyy/M/d", "MM/yyyy", "M/yyyy", "yyyy"
    };

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ReleaseDate);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return default(ReleaseDate);

            var str = reader.Value.ToString();
            DateTime dateTime;

            // Try to parse any accepted format
            if (DateTime.TryParseExact(str, AcceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                return new ReleaseDate(dateTime);

            // Fallback to general DateTime.Parse
            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                return new ReleaseDate(dateTime);

            // Fallback to year-only
            int year;
            if (int.TryParse(str, out year))
                return new ReleaseDate(year);

            // Nothing worked, return default
            return default(ReleaseDate);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is ReleaseDate)
            {
                var rd = (ReleaseDate)value;
                // Assuming your struct has a Date or Year property (adjust if needed)
                // You may need to expose DateTime or Year from ReleaseDate for this to work.
                writer.WriteValue(rd.ToString());
            }
            else
            {
                writer.WriteNull();
            }
        }
    }

    public static class PlayniteHelpers
    {
        public static Dictionary<string, object> GetDatabaseCollections(IPlayniteAPI api)
        {
            return new Dictionary<string, object>
            {
                ["Features"] = api.Database.Features,
                ["Tags"] = api.Database.Tags,
                ["Categories"] = api.Database.Categories,
                ["Genres"] = api.Database.Genres,
                ["Series"] = api.Database.Series,
                ["AgeRatings"] = api.Database.AgeRatings,
                ["Platforms"] = api.Database.Platforms,
                ["Regions"] = api.Database.Regions,
                ["Publishers"] = api.Database.Companies,
                ["Developers"] = api.Database.Companies
                // add more as needed
            };
        }

        public static Dictionary<string, IList> GetGameCollections(Game game)
        {
            return new Dictionary<string, IList>
            {
                ["Features"] = game.Features,
                ["Tags"] = game.Tags,
                ["Categories"] = game.Categories,
                ["Genres"] = game.Genres,
                ["Developers"] = game.Developers,
                ["Publishers"] = game.Publishers,
                ["Platforms"] = game.Platforms,
                ["Series"] = game.Series,
                ["AgeRatings"] = game.AgeRatings,
                ["Regions"] = game.Regions
                // add more as needed
            };
        }
    }
}
