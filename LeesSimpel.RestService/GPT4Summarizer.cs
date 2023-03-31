using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

public static class GPT4Summarizer
{
    static string GetSystemMessage()
    {
        var executingAssembly = typeof(GPT4Summarizer).Assembly;
        var manifestResourceNames = executingAssembly.GetManifestResourceNames();
        var name = manifestResourceNames.Single(s => s.Contains("GPT4SystemMessage"));

        using Stream? stream = executingAssembly.GetManifestResourceStream(name);
        
        if (stream == null)
            throw new InvalidOperationException($"Embedded resource '{name}' not found.");

        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    public static async Task<Summary> Summarize(string ocrResult, string model, RequestTelemetry? requestTelemetry = null)
    {
        try
        {
            return await AttemptSummarize(ocrResult, model);
        }
        catch (SummarizeException)
        {
            requestTelemetry?.Properties.Add("35-failure", ocrResult);
            if (model == "gpt-3.5-turbo")
                return await AttemptSummarize(ocrResult, "gpt-4");
            throw;
        }
    }

    static async IAsyncEnumerable<string?> ExtractMessagesFromResponse(IAsyncEnumerable<ChatCompletionCreateResponse> completions)
    {
        await foreach (var c in completions)
            yield return c.Choices.FirstOrDefault()?.Message.Content;
    }
    
    public static async IAsyncEnumerable<string> SummarizeStreamed(string ocrResult, string model)
    {
        var request = MakeCompletionRequest(ocrResult, model);
        request.Stream = true;
        var completionAsStream = OpenAITools.Service.ChatCompletion.CreateCompletionAsStream(request);
        var fullLinesAsync = Tools.EnumerateFullLines(ExtractMessagesFromResponse(completionAsStream));
        
        bool firstLine = true;
        await foreach (var line in fullLinesAsync)
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
    }

    static (string? text, string? emoji) ParseIntoTextAndEmoji(string line)
    {
        try
        {
            var jobject = JObject.Parse(line);
            var text = jobject["text"].Value<string>();
            var emoji = jobject["emoji"].Value<string>();
            return (text, emoji);
        }
        catch (Exception)
        {
            return (null, null);
        }
    }

    static async Task<Summary> AttemptSummarize(string ocrResult, string model)
    {
        var completion = await OpenAITools.Service.ChatCompletion.CreateCompletion(MakeCompletionRequest(ocrResult, model));

        if (!completion.Successful)
            throw new SummarizeException($"completion was unsuccessful: {completion.Error?.Message}");

        var response = completion.Choices.First().Message.Content;

        var stringReader = new StringReader(response);
        var sender = await stringReader.ReadLineAsync() ??
                     throw new SummarizeException($"Unable to parse response: {response}");
        var messages = new List<Summary.Sentence>();
        while (await stringReader.ReadLineAsync() is { } line)
        {
            if (line.Length == 0)
                continue;

            try
            {
                var open = line.IndexOf('{');
                var close = line.IndexOf('}');
                var onlyJson = line.Substring(open, close - open + 1);
                messages.Add(JsonConvert.DeserializeObject<Summary.Sentence>(onlyJson));
            }
            catch (Exception)
            {
                throw new SummarizeException($"invalid json in response: {response}");
            }
        }

        return new()
        {
            sender = sender,
            summary_sentences = messages.ToArray(),
            call_to_action_is_call = false,
            call_to_action = ""
        };
    }

    static ChatCompletionCreateRequest MakeCompletionRequest(string ocrResult, string model)
    {
        return new()
        {
            Temperature = 0,
            Messages = new List<ChatMessage>()
            {
                ChatMessage.FromSystem(GetSystemMessage()),
                ChatMessage.FromUser(ocrResult),
            },
            Model = model
        };
    }
}