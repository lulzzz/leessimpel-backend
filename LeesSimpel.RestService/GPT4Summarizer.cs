using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

public class GPT4Summarizer
{
    readonly string _systemMessage4;
    readonly string _systemMessage35;
    public GPT4Summarizer()
    {
       _systemMessage4 = Tools.ReadTextFromResource("GPT4SystemMessage", typeof(GPT4Summarizer).Assembly);
       _systemMessage35 = Tools.ReadTextFromResource("GPT35SystemMessage", typeof(GPT4Summarizer).Assembly);
    }
    
    //these types represent the dataprotocol between data languagemodel and the server
    
    [JsonDerivedType(typeof(SenderMessage))]
    [JsonDerivedType(typeof(TextBlockMessage))]
    [JsonDerivedType(typeof(ErrorMessage))]
    [JsonDerivedType(typeof(AdvertisementMessage))]
    public abstract record PromptResponseMessage;

    public record SenderMessage(string sender) : PromptResponseMessage;
    public record TextBlockMessage(string? emoji, string text) : PromptResponseMessage;
    public record ErrorMessage(string error) : PromptResponseMessage;
    public record AdvertisementMessage(bool is_advertisement) : PromptResponseMessage;
    

    
    public async IAsyncEnumerable<PromptResponseMessage> PromptResponseMessagesFor(string ocrResult, string model)
    {
        IAsyncEnumerable<ChatCompletionCreateResponse> completionAsStream = OpenAITools.Service.ChatCompletion.CreateCompletionAsStream(new()
        {
            Temperature = 0,
            Messages = new List<ChatMessage>()
            {
                ChatMessage.FromSystem(model == "gtp-3.5-turbo" ? _systemMessage35 : _systemMessage4),
                ChatMessage.FromUser(ocrResult),
            },
            Model = model,
            Stream = true
        });

        await foreach (var fullLine in FullLinesFrom(completionAsStream))
            yield return ParseLineFromPromptResponse(fullLine);
    }

    static async IAsyncEnumerable<string> FullLinesFrom(IAsyncEnumerable<ChatCompletionCreateResponse> chatResponses)
    {
        StringBuilder sb = new StringBuilder();
        await foreach (var chatResponse in chatResponses)
        {
            if (!chatResponse.Successful)
                yield break;
            
            var chunk = chatResponse.Choices.First().Message.Content;
            if (string.IsNullOrEmpty(chunk))
                continue;

            sb.Append(chunk);
            var lines = sb.ToString().Split('\n');

            for (int i = 0; i < lines.Length - 1; i++)
            {
                var fullLine = lines[i].TrimEnd('\r');
                if (fullLine.Length>0)
                    yield return fullLine;
            }

            sb.Clear();
            sb.Append(lines[^1]);
        }

        if (sb.Length > 0)
        {
            var lastLine = sb.ToString().TrimEnd('\r', '\n');
            // Operate on the last line, if not empty
            if (lastLine.Length > 0)
                yield return lastLine;
        }
    }


    public static PromptResponseMessage ParseLineFromPromptResponse(string promptResponseLine)
    {
        try
        {
            var rootElement = JsonDocument.Parse(promptResponseLine).RootElement;
            
            if (rootElement.TryGetProperty("sender", out _))
                return JsonSerializer.Deserialize<SenderMessage>(promptResponseLine)!;

            if (rootElement.TryGetProperty("is_advertisement", out _))
                return JsonSerializer.Deserialize<AdvertisementMessage>(promptResponseLine)!;
            
            if (rootElement.TryGetProperty("text", out _))
                return JsonSerializer.Deserialize<TextBlockMessage>(promptResponseLine)!;

            if (rootElement.TryGetProperty("error", out _))
                return JsonSerializer.Deserialize<ErrorMessage>(promptResponseLine)!;
            
            return new ErrorMessage(promptResponseLine);
        }
        catch (JsonException)
        {
            return new ErrorMessage(promptResponseLine);
        }
    }

}