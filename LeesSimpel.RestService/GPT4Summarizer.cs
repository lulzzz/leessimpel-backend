using System.Reflection;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using OpenAI.GPT3.ObjectModels.RequestModels;

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

    static async Task<Summary> AttemptSummarize(string ocrResult, string model)
    {
        var completion = await OpenAITools.Service.ChatCompletion.CreateCompletion(new()
        {
            Temperature = 0,
            Messages = new List<ChatMessage>()
            {
                ChatMessage.FromSystem(GetSystemMessage()),
                ChatMessage.FromUser(ocrResult),
            },
            Model = model
        });

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
}