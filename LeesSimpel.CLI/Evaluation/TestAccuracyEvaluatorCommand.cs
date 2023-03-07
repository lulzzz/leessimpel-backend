using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

// ReSharper disable once ClassNeverInstantiated.Global
class TestAccuracyEvaluatorCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var testFiles = Directories.Backend.Combine("LeesSimpel.CLI/Evaluation/TestData").Files("*.json");

        foreach (var testFile in testFiles)
        {
            var stringReader = new StringReader(testFile.ReadAllText());
            var jsonReader = new JsonTextReader(stringReader);
            var testCaseObject = JObject.Load(jsonReader);

            var summary = testCaseObject["summary_to_evaluate"].ToObject<Summary>();
            var propertyName = "evaluation_criteria";
            var evaluationCriteriaArray = testCaseObject[propertyName] as JArray ?? throw new ArgumentException($"{propertyName} was not a JArray");
            var evaluationCriteria = AccuracyEvaluationCriteria.Parse(evaluationCriteriaArray);
            var evaluationResult = await AccuracyEvaluatorGTP3.Evaluate(summary, evaluationCriteria);
            bool[] expectedResult = testCaseObject["expected_results"].ToObject<bool[]>();

            if (!Compare(evaluationResult, expectedResult, evaluationCriteria, out var reasons))
            {
                AnsiConsole.MarkupLine($"[red]FAIL[/] {testFile.FileName}");
                foreach(var reason in reasons)
                    AnsiConsole.MarkupLine($"[red]x[/] {reason}");
                AnsiConsole.WriteLine();
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]PASS[/] {testFile.FileName}");
            }
        }

        return 0;
    }
    
    static bool Compare(bool[] actual, bool[] expected, AccuracyEvaluationCriteria criteria, out string[] reasons)
    {
        if (actual.Length != expected.Length)
        {
            reasons = new[] {$"Expected length of {expected.Length} but got {actual.Length}"};
            return false;
        }

        var reasonList = new List<string>();
        for (int i = 0; i != actual.Length; i++)
        {
            if (actual[i] == expected[i])
                continue;

            reasonList.Add($"Expected result is {expected[i]} for '{criteria.Things[i]}', but evaluator returned {actual[i]}");
        }

        reasons = reasonList.ToArray();
        return !reasons.Any();
    }
}