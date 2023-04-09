using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

class EvaluateCommand : AsyncCommand<EvaluateCommand.Settings>
{
    internal class Settings : CommandSettings
    {
        [CommandArgument(0, "<inputset>")]
        public string InputSet { get; set; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        return await TrainingSetDataTransformation.ExecuteAsync("Evaluate", "json", settings.InputSet,
            settings.InputSet + "_evaluations",
            async (inputFile, outputFile) =>
            {
                var summary = inputFile.ReadAllLines();
                var criteriafile = Directories.TrainingSet.Combine("evaluation_criteria").Combine(inputFile.FileName);

                if (!criteriafile.FileExists())
                {
                    AnsiConsole.MarkupLine($"[yellow] criteria file {criteriafile.FileName} does not exist[/]");
                    return;
                }
                var evaluationCriteria = AccuracyEvaluationCriteria.Parse(criteriafile.ReadAllText());
                var evaluationResult = await AccuracyEvaluator.Evaluate(summary, evaluationCriteria);
                int characterCount = summary.Sum(s => (JObject.Parse(s)["text"] ?? "").Value<string>()?.Length ?? 0);

                var foundWeight = evaluationResult.KeyMessageResults.Where(m => m.FoundAt != 0).Sum(m => m.Weight);
                var totalWeight = evaluationResult.KeyMessageResults.Sum(m => m.Weight);
                
                var outputObject = new JObject()
                {
                    ["length"] = characterCount,
                    ["accuracy_score"] = foundWeight / totalWeight,
                    ["accuracy"] = new JArray() { evaluationResult.KeyMessageResults.Select(r=>JObject.FromObject(r)) }
                };
                outputFile.WriteAllText(JsonConvert.SerializeObject(outputObject, Formatting.Indented));
            });
    }
}