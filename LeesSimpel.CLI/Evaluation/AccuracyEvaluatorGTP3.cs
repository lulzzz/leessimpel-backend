using Spectre.Console;
using Newtonsoft.Json.Linq;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;

public static class AccuracyEvaluatorGTP3
{
    static readonly OpenAIService _service = new OpenAIService(new()
    {
        ApiKey = Secrets.Get("OpenAIServiceOptions:ApiKey")
    });
    
    public static async Task<bool[]> Evaluate(Summary summary, AccuracyEvaluationCriteria accuracyEvaluationCriteria)
    {
        var s = @$"
What follows is a bullet point summary of a letter:

{summary.summary_sentences.Select(s=>$"- {s.text}\n").SeparateWith("\n")}


I will now present a set of messages/facts.
I want you to tell me for each message if they are present in the summary. 
restrict your response to a json array of booleans where each value indicates whether the corresponding message is communicated in the summary

{accuracyEvaluationCriteria.Things.OfType<AccuracyEvaluationCriteria.ContainsFact>().Select(f=>$"- {f.fact}").SeparateWith("\n")}
";

        var completionResult = await _service.Completions.CreateCompletion(new()
        {
            Prompt = s,
            Model = Models.TextDavinciV3,
            MaxTokens = 100,
            Temperature = 0
        });

        if (!completionResult.Successful)
            throw new Exception("GTP3 prompt was unsuccessful. "+completionResult.Error?.Message);

        var response = completionResult.Choices.FirstOrDefault()!.Text;

        
        AnsiConsole.MarkupLine("[yellow]Prompt:[/]");
        Console.WriteLine(s);
        Console.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Response:[/]");
        Console.WriteLine(response);
        
        return JArray.Parse(response).ToObject<bool[]>();
    }
}

static class EnumerableExtensions
{
    public static string SeparateWith(this IEnumerable<string> things, string seperator) => string.Join(seperator, things);
}