using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class SummarizeEndPoint
{
    public static void Register(WebApplication app)
    {
        app.MapPost($"/v2/summarize_text", Post_Summarize_Text);
    }

    static async Task<IResult> Post_Summarize_Text(HttpResponse response, GPT4Summarizer gpt4Summarizer, SummarizeTextParameters summarizeTextParameters)
    {
        response.Headers.Add("Content-Type", "text/event-stream");
        try
        {
            var rawPromptResponseLines =
                gpt4Summarizer.SummarizeStreamedRaw(summarizeTextParameters.TextToSummarize, "gpt-3.5-turbo");

            await foreach (var o in ResultViewElementsFor(rawPromptResponseLines))
            {
                await response.WriteAsync(o);
                await response.WriteAsync("\n");
                await response.Body.FlushAsync();
            }
        }
        catch (Exception)
        {
            await response.WriteAsync("{\"text\":\"Het is niet gelukt\", \"emoji\":\"🚨\"}\n");
            await response.WriteAsync("{\"text\":\"Probeer het nog een keer\", \"emoji\":\"🔁\"}\n");
            await response.Body.FlushAsync();
        }

        return Results.Empty;
    }

    static async IAsyncEnumerable<string> ResultViewElementsFor(IAsyncEnumerable<string> rawPromptResponseLines)
    {
        bool firstLine = true;
        await foreach (var line in rawPromptResponseLines)
        {
            if (firstLine)
            {
                yield return "{\"kind\":\"title\",\"title\": \"Hier is je versimpelde brief\"}";
                yield return "{\"kind\":\"section\",\"title\": \"Van wie is de brief?\",\"index\":\"1\"}";
                yield return JsonConvert.SerializeObject(new JObject()
                {
                    ["kind"] = "textblock",
                    ["text"] = line
                });

                yield return "{\"kind\":\"section\",\"title\": \"Wat staat er in?\",\"index\":\"2\"}";
                firstLine = false;
                continue;
            }

            var (text, emoji) = ParseIntoTextAndEmoji(line);
            if (text == null) 
                continue;
            var jObject = new JObject()
            {
                ["kind"] = "textblock",
                ["text"] = text,
                ["emoji"] = emoji ?? ""
            };
                
            yield return JsonConvert.SerializeObject(jObject);
        }
        
        static (string? text, string? emoji) ParseIntoTextAndEmoji(string line)
        {
            try
            {
                var jobject = JObject.Parse(line);
                var text = jobject["text"]?.Value<string>();
                var emoji = jobject["emoji"]?.Value<string>();
                return (text, emoji);
            }
            catch (Exception)
            {
                return (null, null);
            }
        }
    }
}