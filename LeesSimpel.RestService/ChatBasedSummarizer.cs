using Newtonsoft.Json;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace DefaultNamespace;

public static class ChatBasedSummarizer
{
    const string SystemMessage = @"
You summarise letters into bulletpoints. 
Include only key bulletpoints the recipient is likely to care strongly about. Discard the rest agressively.
Each bullet is a simple, friendly, complete, very short, relevant, dutch sentence a kid would understand.
Figure out the sender of the letter. If it is a person from an organisation use the organisation name.  
First line is the sender. 
All subsequent lines are the bullet points encoded as individual json objects like this one: {""emoji"":""📚"",""text"":""This is a sentence.""}";

    public static async Task<Summary> Summarize(string ocrResult, string model)
    {
        var completion = await OpenAITools.Service.ChatCompletion.CreateCompletion(new()
        {
            Temperature = 0,
            Messages = new List<ChatMessage>()
            {
                ChatMessage.FromSystem(SystemMessage),
                ChatMessage.FromUser(ocrResult),
            }, Model = model
        });

        if (!completion.Successful)
            throw new SummarizeException($"completion was unsuccessful: {completion.Error?.Message}");
        
        var response = completion.Choices.First().Message.Content;

        var stringReader = new StringReader(response);
        var sender = await stringReader.ReadLineAsync() ?? throw new SummarizeException($"Unable to parse response: {response}");
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
        };
    }
}