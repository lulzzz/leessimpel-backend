using System.Text.Json;
using System.Text.Json.Serialization;

public static class SummarizeEndPoint
{
    public static void Register(WebApplication app)
    {
        app.MapPost($"/v2/summarize_text", Post_Summarize_Text);
    }

    static async Task<IResult> Post_Summarize_Text(HttpResponse response, GPT4Summarizer gpt4Summarizer, SummarizeTextParameters summarizeTextParameters)
    {
        response.Headers.Add("Content-Type", "text/event-stream");
        bool sentTitle = false;
        var jsonOptions = new JsonSerializerOptions() {Converters = {new AppMessageJsonConverter()}, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault};
        try
        {
            var promptResponseMessages =
                gpt4Summarizer.PromptResponseMessagesFor(summarizeTextParameters.TextToSummarize, "gpt-3.5-turbo");

            await foreach (var appMessage in AppMessagesFor(promptResponseMessages))
            {
                await response.WriteAsync(JsonSerializer.Serialize(appMessage, jsonOptions));
                await response.WriteAsync("\n");
                await response.Body.FlushAsync();
                
                if (appMessage is TitleAppMessage)
                    sentTitle = true;
            }
        }
        catch (Exception)
        {
            foreach (var appMessage in ErrorMessagesFor(sentTitle))
            {
                await response.WriteAsync(JsonSerializer.Serialize(appMessage, jsonOptions));
                await response.WriteAsync("\n");
            }
        }

        return Results.Empty;
    }

    //these types represent the dataprotocol between the app server and the app client
    static IEnumerable<AppMessage> ErrorMessagesFor(bool alreadySentTitle)
    {
        var errorMessage = "Er is iets mis gegaan";
        if (alreadySentTitle)
            yield return new TextBlockAppMessage(emoji: "🚨", text: errorMessage);
        else
            yield return new TitleAppMessage(title: errorMessage, false);
                
        yield return new TextBlockAppMessage(emoji: "🔁", text: "Probeer het nog een keer");
    }
    
    static async IAsyncEnumerable<AppMessage> AppMessagesFor(IAsyncEnumerable<GPT4Summarizer.PromptResponseMessage> promptResponseMessages)
    {
        bool firstMessage = true;
        bool hadSender = false;
        bool sentTitle = false;
        bool sentSection = false;
        
        await foreach (var promptResponseMessage in promptResponseMessages)
        {
            if (firstMessage)
            {
                if (promptResponseMessage is GPT4Summarizer.SenderMessage senderMessage && senderMessage.sender != null)
                {
                    yield return new TitleAppMessage(title: "Hier is je versimpelde brief", true);
                    sentTitle = true;
                    yield return new SectionAppMessage(title: "Van wie is de brief?", index: "1");
                    yield return new TextBlockAppMessage(text: senderMessage.sender, emoji:null);
                    yield return new SectionAppMessage(title: "Wat staat er in?", index: "2");
                    sentSection = true;
                    continue;
                }

                firstMessage = false;
            }

            switch (promptResponseMessage)
            {
                case GPT4Summarizer.TextBlockMessage msg:
                {
                    if (!sentSection)
                    {
                        yield return new TitleAppMessage(title: "Gelukt!", true);
                        yield return new SectionAppMessage(title: "Wat staat er?", index: "1");
                        sentSection = true;
                    }
                
                    yield return new TextBlockAppMessage(emoji: msg.emoji, text: msg.text);
                    continue;
                }
                case GPT4Summarizer.ErrorMessage:
                {
                    foreach (var msg in ErrorMessagesFor(sentTitle))
                        yield return msg;
                    yield break;
                }
            }
        }
    }
}