using System.Text;
using Newtonsoft.Json.Linq;
using OpenAI.GPT3.ObjectModels.RequestModels;

public static class AccuracyEvaluator
{
    const string SystemMessageText = @"
Your job is to evaluate a summary of a letter. 
The first user message is the bulletpoint summary. 
The second user message is a list of key messages that a good summary should contain.
respond with a json object where the key is a key message, and the value is the number of the bulletpoint where you found that key message being communicated in the summary.
If you dont find the key message anywhere, use 0.
";
    
    public static async Task<EvaluationResult> Evaluate(Summary summary, AccuracyEvaluationCriteria accuracyEvaluationCriteria)
    {
        string MakeNumberedBulletList(Summary s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i != s.summary_sentences.Length; i++)
                sb.AppendLine($"{i+1}: {s.summary_sentences[i].text}");
            return sb.ToString();
        }

        var chatCompletionCreateRequest = new ChatCompletionCreateRequest()
        {
            Model = "gpt-4",
            MaxTokens = 3000,
            Temperature = 0,
            Messages = new List<ChatMessage>()
            {
                ChatMessage.FromSystem(SystemMessageText),
                ChatMessage.FromUser(MakeNumberedBulletList(summary)),
                ChatMessage.FromUser(accuracyEvaluationCriteria.Criteria.OfType<AccuracyEvaluationCriteria.ContainsKeyMessage>().Select(f=>$"- {f.keyMessage}").SeparateWith("\n"))
            }
        };
        var completionResult = await OpenAITools.Service.ChatCompletion.CreateCompletion(chatCompletionCreateRequest);

        if (!completionResult.Successful)
            throw new Exception("GPT prompt was unsuccessful. "+completionResult);

        var response = completionResult.Choices.First().Message.Content;

        var debugInfo = new StringBuilder();
        
        debugInfo.AppendLine("[yellow]Prompt:[/]");
        foreach (var message in chatCompletionCreateRequest.Messages)
        {
            debugInfo.AppendLine($"{message.Role}: {message.Content}");
        }
        debugInfo.AppendLine();
        debugInfo.AppendLine("[yellow]Response:[/]");
        debugInfo.AppendLine(response);

        var jo = JObject.Parse(response);
        var keyMessageResults = new List<KeyMessageResult>();
        foreach (var kvp in jo)
        {
            int index = kvp.Value?.Value<int>() ?? 999;
            keyMessageResults.Add(new() { Message = kvp.Key, FoundAt = index, Weight = 1 /*todo: fix weight*/});
        }

        return new()
        {
            KeyMessageResults = keyMessageResults.ToArray(),
            DebugGpt3Info = debugInfo.ToString()
        };
    }
}

public record EvaluationResult()
{
    public required KeyMessageResult[] KeyMessageResults { get; init; }
    public required string DebugGpt3Info { get; init; }
}

public record KeyMessageResult
{
    public required string Message { get; init; }
    public required float Weight { get; init; }
    public required int FoundAt { get; init; }
}

static class EnumerableExtensions
{
    public static string SeparateWith(this IEnumerable<string> things, string seperator) => string.Join(seperator, things);
}