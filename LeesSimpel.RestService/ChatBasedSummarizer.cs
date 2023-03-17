using OpenAI.GPT3.ObjectModels.RequestModels;

namespace DefaultNamespace;

public static class ChatBasedSummarizer
{
    const string SystemMessage = @"
you are a summarisation tool for letters.
you write in Dutch, using simple language a kid would understand.
include only key messages the recipient is likely to care about. 
Discard the rest.
Figure out the sender of the letter. If it is a person from an organisation use the organisation name.  

Response should be newline delimited.
First line is the sender. 
All subsequent lines are key messages of the summary, unprefixed, using punctionation";

    public static async Task<Summary> Summarize(string ocrResult)
    {
        var completion = await OpenAITools.Service.ChatCompletion.CreateCompletion(new()
        {
            Temperature = 0,
            Messages = new List<ChatMessage>()
            {
                ChatMessage.FromSystem(SystemMessage),
                ChatMessage.FromUser(ocrResult),
            }, Model = "gpt-4"
        });

        if (!completion.Successful)
            throw new SummarizeException($"completion was unsuccessful: {completion.Error?.Message}");
        
        var response = completion.Choices.First().Message.Content;

        var stringReader = new StringReader(response);
        var sender = await stringReader.ReadLineAsync() ?? throw new SummarizeException($"Unable to parse response: {response}");
        var messages = new List<string>();
        while (await stringReader.ReadLineAsync() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0)
                messages.Add(trimmed);
        }

        return new()
        {
            sender = sender,
            summary_sentences = messages.Select(m => new Summary.Sentence() {text = m, emoji = ""}).ToArray(),
        };
    }
}