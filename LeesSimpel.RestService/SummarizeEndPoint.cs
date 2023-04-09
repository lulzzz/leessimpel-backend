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

        if (summarizeTextParameters.TextToSummarize.Length < 40)
        {
            await response.WriteAppMessage(new TitleAppMessage("Er ging iets mis.", false));
            await response.WriteAppMessage(new TextBlockAppMessage("Er lijkt geen tekst in de foto te staan.", "📷"));
            await response.WriteAppMessage(new TextBlockAppMessage("Probeer het nog een keer.", "🔁"));
            return Results.Empty;
        }
        
        bool sentTitle = false;
        try
        {
            var promptResponseMessages =
                gpt4Summarizer.PromptResponseMessagesFor(summarizeTextParameters.TextToSummarize, /*"gpt-3.5-turbo"*/ "gpt-4");

            await foreach (var appMessage in AppMessagesFor(promptResponseMessages))
            {
                await response.WriteAppMessage(appMessage);
                await response.Body.FlushAsync();
                
                if (appMessage is TitleAppMessage)
                    sentTitle = true;
            }
        }
        catch (Exception)
        {
            foreach (var appMessage in ErrorMessagesFor(sentTitle)) 
                await response.WriteAppMessage(appMessage);
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
        bool sentTitle = false;
        bool sentContentsSection = false;
        bool hasSender = false;
        int nextSectionID = 1;
        
        await foreach (var promptResponseMessage in promptResponseMessages)
        {
            if (firstMessage)
            {
                firstMessage = false;
                if (promptResponseMessage is GPT4Summarizer.SenderMessage senderMessage)
                {
                    if (senderMessage.sender != null)
                    {
                        yield return new TitleAppMessage(title: "Hier is je versimpelde brief", true);
                        sentTitle = true;
                        hasSender = true;
                        yield return new SectionAppMessage(title: "Van wie is de brief?",
                            index: (nextSectionID++).ToString());
                        yield return new TextBlockAppMessage(text: senderMessage.sender, emoji: null);
                    }

                    continue;
                }

            }

            if (!sentTitle)
            {
                yield return new TitleAppMessage(title: "Gelukt!", true);
                sentTitle = true;
            }
            
            switch (promptResponseMessage)
            {
                case GPT4Summarizer.AdvertisementMessage msgAdvertisementMessage:
                {
                    if (msgAdvertisementMessage.is_advertisement)
                    {
                        yield return new SectionAppMessage(title: "Wat is dit voor brief?", index:(nextSectionID++).ToString());
                        yield return new TextBlockAppMessage(text: "Reclame", emoji:null);
                    }
                    continue;
                }
                    
                case GPT4Summarizer.TextBlockMessage msg:
                {
                    if (!sentContentsSection)
                    {
                        yield return new SectionAppMessage(title: hasSender ? "Wat staat er in?" : "Wat staat er?", index: (nextSectionID++).ToString());
                        sentContentsSection = true;
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

        if (firstMessage)
        {
            foreach (var msg in ErrorMessagesFor(sentTitle))
                yield return msg;
        }
    }
}