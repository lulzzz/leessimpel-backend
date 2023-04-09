using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

public class GPT4Summarizer
{
    readonly string _systemMessage;
    public GPT4Summarizer()
    {
       _systemMessage = Tools.ReadTextFromResource("GPT4SystemMessage", typeof(GPT4Summarizer).Assembly);
    }
    static async IAsyncEnumerable<string?> ExtractMessagesFromResponse(IAsyncEnumerable<ChatCompletionCreateResponse> completions)
    {
        await foreach (var c in completions)
            yield return c.Choices.FirstOrDefault()?.Message.Content;
    }
    
    public IAsyncEnumerable<string> SummarizeStreamedRaw(string ocrResult, string model)
    {
        var request = MakeCompletionRequest(ocrResult, model);
        request.Stream = true;
        var completionAsStream = OpenAITools.Service.ChatCompletion.CreateCompletionAsStream(request);
        
        var fullLinesAsync = Tools.EnumerateFullLines(ExtractMessagesFromResponse(completionAsStream));
        return fullLinesAsync;
    }
    
    ChatCompletionCreateRequest MakeCompletionRequest(string ocrResult, string model)
    {
        return new()
        {
            Temperature = 0,
            Messages = new List<ChatMessage>()
            {
                ChatMessage.FromSystem(_systemMessage),
                ChatMessage.FromUser(ocrResult),
            },
            Model = model
        };
    }
}