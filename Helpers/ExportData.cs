using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalLibrary.Helpers
{
    public class ExportData
    {
        public static string ExportToCsv(List<string[]> data, string[] headers)
        {
            StringBuilder csv = new StringBuilder();
            // Add headers
            csv.AppendLine(string.Join(",", headers));
            // Add data rows
            foreach (var row in data)
            {
                csv.AppendLine(string.Join(",", row.Select(field => $"\"{field.Replace("\"", "\"\"")}\"")));
            }
            return csv.ToString();
        }

        public static string ExportToJson(List<Dictionary<string, object>> data)
        {
            var json = new StringBuilder();
            json.Append("[");
            for (int i = 0; i < data.Count; i++)
            {
                var row = data[i];
                json.Append("{");
                int j = 0;
                foreach (var kvp in row)
                {
                    json.Append($"\"{kvp.Key}\": \"{kvp.Value.ToString().Replace("\"", "\\\"")}\"");
                    if (j < row.Count - 1) json.Append(", ");
                    j++;
                }
                json.Append("}");
                if (i < data.Count - 1) json.Append(", ");
            }
            json.Append("]");
            return json.ToString();
        }

        public static Dictionary<string, object> GetExportData(LocalLibrarySettings settings, Game game)
        {
            Dictionary<string, object> data;
            var textelements = settings.ExportTextElements;
            GetTextElements(out data, game, textelements);
            


            return new Dictionary<string, object>
            {
                { "Title", "Sample Book" },
                { "Author", "John Doe" },
                { "Year", 2023 }
            };
        }

        public static void GetTextElements(out Dictionary<string, object> data, Game game, ObservableCollection<ExportMetadataItem> textelements)
        {
            foreach (var element in textelements)
            {
                if (element.IsSelected)
                {
                    switch (element.Name)
                    {
                        case "Name":
                            // data[element.Name] = game.Name;
                            break;
                        case "Sorting Name":
                            // data[element.Name] = game.SortingName;
                            break;
                        case "Series":
                            // data[element.Name] = game.Series;
                            break;
                        case "Description":
                            // data[element.Name] = game.Description;
                            break;
                        case "Region":
                            // data[element.Name] = game.Region;
                            break;
                        case "Platforms":
                            // data[element.Name] = string.Join(", ", game.Platforms);
                            break;
                        case "Categories":
                            // data[element.Name] = string.Join(", ", game.Categories);
                            break;
                        case "Features":
                            // data[element.Name] = string.Join(", ", game.Features);
                            break;
                        case "Genres":
                            // data[element.Name] = string.Join(", ", game.Genres);
                            break;
                        case "Links":
                            // data[element.Name] = string.Join(", ", game.Links.Select(link => link.Name));
                            break;
                        case "Tags":
                            // data[element.Name] = string.Join(", ", game.Tags);
                            break;
                        case "Version":
                            // data[element.Name] = game.Version;
                            break;
                        case "Developers":
                            // data[element.Name] = string.Join(", ", game.Developers);
                            break;
                        case "Publishers":
                            // data[element.Name] = string.Join(", ", game.Publishers);
                            break;
                        case "Source":
                            // data[element.Name] = game.Source;
                            break;
                        case "AgeRatings":
                            // data[element.Name] = string.Join(", ", game.AgeRatings.Select(r => r.Rating));
                            break;
                        default:
                            break;
                    }

                    // For example:
                    // data[element.Name] = FetchTextElementValue(element);
                }
            }

            data = new Dictionary<string, object>
            {
                { "Chapter1", "Introduction to C#" },
                { "Chapter2", "Advanced C# Concepts" },
                { "Chapter3", "C# in Practice" }
            };
        }
    }
}
