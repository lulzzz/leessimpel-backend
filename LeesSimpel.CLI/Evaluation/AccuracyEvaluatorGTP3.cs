using System.Text;
using Newtonsoft.Json.Linq;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;

public static class AccuracyEvaluatorGTP3
{
    static readonly OpenAIService _service = new(new()
    {
        ApiKey = Secrets.Get("OpenAIServiceOptions:ApiKey")
    });

    public class EvaluationResult
    {
        public bool[] present_messges;
        public string debug_gpt3_info;
    }
    
    public static async Task<EvaluationResult> Evaluate(Summary summary, AccuracyEvaluationCriteria accuracyEvaluationCriteria)
    {
        string MakeNumberedBulletList(Summary s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i != s.summary_sentences.Length; i++)
                sb.AppendLine($"{i+1}: {s.summary_sentences[i].text}");
            return sb.ToString();
        }
        
        var s = @$"
What follows is a bullet point summary of a letter:

{MakeNumberedBulletList(summary)}

In order to know if the summary is of high quality, I want to know which of the following key messages of the original text appear summary:
{accuracyEvaluationCriteria.Things.OfType<AccuracyEvaluationCriteria.ContainsFact>().Select(f=>$"- {f.fact}").SeparateWith("\n")}

Restrict your response to a json object where each key is the message, and the value is the number of the bulletpoint that communicates that message. use 0 if the message is not present in the summary.
";

        var completionResult = await _service.Completions.CreateCompletion(new()
        {
            Prompt = s,
            Model = Models.TextDavinciV3,
            MaxTokens = 3000,
            Temperature = 0
        });

        if (!completionResult.Successful)
            throw new Exception("GTP3 prompt was unsuccessful. "+completionResult.Error?.Message);

        var response = completionResult.Choices.FirstOrDefault()!.Text;

        var debugInfo = new StringBuilder();
        
        debugInfo.AppendLine("[yellow]Prompt:[/]");
        debugInfo.AppendLine(s);
        debugInfo.AppendLine();
        debugInfo.AppendLine("[yellow]Response:[/]");
        debugInfo.AppendLine(response);

        var jo = JObject.Parse(response);
        var bools = new List<bool>();
        foreach (var kvp in jo)
        {
            int index = kvp.Value.Value<int>();
            bools.Add(index != 0);
        }

        return new EvaluationResult()
        {
            present_messges = bools.ToArray(),
            debug_gpt3_info = debugInfo.ToString()
        };
    }
}

static class EnumerableExtensions
{
    public static string SeparateWith(this IEnumerable<string> things, string seperator) => string.Join(seperator, things);
}