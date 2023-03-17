using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.Managers;

public static class OpenAITools
{
    public static IOpenAIService Service { get; } = new OpenAIService(new()
    {
        ApiKey = Secrets.Get("OpenAIServiceOptions:ApiKey")
    });
}