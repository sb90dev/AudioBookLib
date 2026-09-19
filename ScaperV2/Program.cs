using AngleSharp;
using ScraperV2;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

List<(string goodreadsId, string audibleId)> Ids =
[
    ( "15839976", "B00I2W0H9W" ), // Red Rising by Pierce Brown
    ( "22055262", "B0FKCXXKXH" ), // A Darker Shade of Magic by V. E. Schwab
    ( "50202953", "1526622416" )  // Piranesi by Susanna Clarke
];


var service = new AudiobookMetadataService();

foreach (var (goodreadsId, audibleId) in Ids)
{
    var result = await service.FetchMetadataAsync(goodreadsId, audibleId);

    Console.WriteLine(JsonSerializer.Serialize(
        result,
        new JsonSerializerOptions
        {
            WriteIndented = true
        }));
}

namespace ScraperV2
{
    public sealed record AudiobookMetadata
    {
        public string Id { get; set; } = string.Empty;
        public string? GoodReadsId { get; set; }
        public string? AudibleId { get; set; }
        public string? Title { get; set; }
        public List<string> Authors { get; set; } = [];
        public string? SeriesName { get; set; }
        public string? SeriesVolume { get; set; }
        public double? GoodreadsRating { get; set; }
        public int? GoodreadsRatingCount { get; set; }
        public List<string> Narrators { get; set; } = [];
        public string? Length { get; set; }
        public double? AudibleRating { get; set; }
        public int? AudibleRatingCount { get; set; }
        public string? ImageUrl { get; set; }
        public DateTimeOffset? MetadataDate { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }

    public sealed class AudiobookMetadataService
    {
        private const string GoodReadsBaseUrl = "https://www.goodreads.com/book/show/";
        private const string AudibleBaseUrl = "https://www.audible.co.uk/pd/";

        private readonly HttpClient _httpClient;

        public AudiobookMetadataService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<AudiobookMetadata> FetchMetadataAsync(string goodReadsBookId, string audibleBookId)
        {
            // 1. Fetch metadata in parallel
            var goodreadsTask = ExtractJsonLdAsync($"{GoodReadsBaseUrl}{goodReadsBookId}");
            var audibleTask = ExtractJsonLdAsync($"{AudibleBaseUrl}{audibleBookId}");
            await Task.WhenAll(goodreadsTask, audibleTask);

            var grJson = goodreadsTask.Result;
            var audJson = audibleTask.Result;

            // 2. Synthesize data into your domain model
            var rawTitle = grJson?["name"]?.ToString();
            var seriesExtraction = BookTitleParser.Parse(rawTitle ?? string.Empty);
            var bookData = new AudiobookMetadata
            {
                Id = Guid.NewGuid().ToString("N"),
                GoodReadsId = goodReadsBookId,
                AudibleId = audibleBookId,
                Title = seriesExtraction.CleanTitle,
                Authors = ExtractNamesFromArray(grJson?["author"]),
                SeriesName = seriesExtraction.SeriesName,
                SeriesVolume = seriesExtraction.EntryNumber,
                GoodreadsRating = double.TryParse(grJson?["aggregateRating"]?["ratingValue"]?.ToString(), out var grRating) ? grRating : null,
                GoodreadsRatingCount = int.TryParse(grJson?["aggregateRating"]?["ratingCount"]?.ToString(), out var grRatingCount) ? grRatingCount : null,
                Narrators = ExtractNamesFromArray(audJson?["readBy"]),
                Length = audJson?["duration"]?.ToString(),
                AudibleRating = double.TryParse(audJson?["aggregateRating"]?["ratingValue"]?.ToString(), out var audRating) ? audRating : null,
                AudibleRatingCount = int.TryParse(audJson?["aggregateRating"]?["ratingCount"]?.ToString(), out var audRatingCount) ? audRatingCount : null,
                ImageUrl = grJson?["image"]?.ToString(),
                MetadataDate = DateTimeOffset.Now
            };

            return bookData;
        }

        private static List<string> ExtractNamesFromArray(JsonNode? node)
        {
            var names = new List<string>();
            if (node is JsonArray array)
            {
                foreach (var item in array.OfType<JsonObject>())
                {
                    var name = item["name"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        names.Add(name);
                    }
                }
            }
            return names;
        }

        private async Task<JsonObject?> ExtractJsonLdAsync(string url)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(url);

                // Initialise AngleSharp virtual DOM context
                var config = Configuration.Default;
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(req => req.Content(html));

                // 1. Grab ALL JSON-LD script blocks on the page
                var scriptElements = document.QuerySelectorAll("script[type='application/ld+json']");

                foreach (var script in scriptElements)
                {
                    var rawJson = script.TextContent;
                    if (string.IsNullOrWhiteSpace(rawJson)) continue;

                    var jsonNode = JsonSerializer.Deserialize<JsonNode>(rawJson);
                    if (jsonNode == null) continue;

                    // Handle both objects and arrays
                    JsonObject? targetObject = null;

                    if (jsonNode is JsonObject obj)
                    {
                        targetObject = obj;
                    }
                    else if (jsonNode is JsonArray arr && arr.Count > 0)
                    {
                        // Check each item in the array for the schema type we want
                        foreach (var item in arr.OfType<JsonObject>())
                        {
                            var schemaType = item["@type"]?.ToString();
                            if (schemaType == "Book" || schemaType == "Audiobook")
                            {
                                return item;
                            }
                        }
                        continue;
                    }

                    // Process the single object
                    if (targetObject != null)
                    {
                        var schemaType = targetObject["@type"]?.ToString();
                        if (schemaType == "Book" || schemaType == "Audiobook")
                        {
                            return targetObject;
                        }
                    }
                }

                return null;
            }
            catch
            {
                // Fail gracefully or log if network/parsing breaks
                return null;
            }
        }
    }

    public static class BookTitleParser
    {
        // Matches patterns like: Title Name (Series Name, #1) or Title Name (Series Name #1)
        private static readonly Regex SeriesRegex = new Regex(@"\((.+?),\s*#?([0-9\.]+)\)$", RegexOptions.Compiled);

        public static ParsedSeriesResult Parse(string? rawTitle)
        {
            if (string.IsNullOrWhiteSpace(rawTitle))
                return new();

            var match = SeriesRegex.Match(rawTitle);

            if (match.Success)
            {
                return new ParsedSeriesResult
                {
                    // Everything before the trailing parentheses
                    CleanTitle = rawTitle.Substring(0, match.Index).Trim(),
                    SeriesName = match.Groups[1].Value.Trim(),
                    EntryNumber = match.Groups[2].Value.Trim()
                };
            }

            // Return title as-is if no series pattern exists
            return new ParsedSeriesResult { CleanTitle = rawTitle.Trim() };
        }
    }

    public class ParsedSeriesResult
    {
        public string CleanTitle { get; set; } = string.Empty;
        public string? SeriesName { get; set; }
        public string? EntryNumber { get; set; }
    }
}
