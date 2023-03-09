using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using JsonSerializer = System.Text.Json.JsonSerializer;

// ReSharper disable once ClassNeverInstantiated.Global
class SummarizeCommand : AsyncCommand<SummarizeCommand.Settings>
{
    internal class Settings : CommandSettings
    {
        [CommandArgument(0, "<inputset>")]
        public string InputSet { get; set; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var openai = new OpenAISummarizer();

        return await TrainingSetDataTransformation.ExecuteAsync("Summarize", "json", settings.InputSet,
            settings.InputSet + "_summarized",
            async (inputFile, outputFile) =>
            {
                var summary = await openai.Summarize(inputFile.ReadAllText());
                outputFile.WriteAllText(JsonConvert.SerializeObject(summary, Formatting.Indented));
            });
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
class EvaluateCommand : AsyncCommand<EvaluateCommand.Settings>
{
    internal class Settings : CommandSettings
    {
        [CommandArgument(0, "<inputset>")]
        public string InputSet { get; set; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var openai = new OpenAISummarizer();

        return await TrainingSetDataTransformation.ExecuteAsync("Evaluate", "json", settings.InputSet,
            settings.InputSet + "_evaluations",
            async (inputFile, outputFile) =>
            {
                var summary = JObject.Parse(inputFile.ReadAllText()).ToObject<Summary>();
                var criteriafile = Directories.TrainingSet.Combine("evaluation_criteria").Combine(inputFile.FileName);

                if (!criteriafile.FileExists())
                {
                    AnsiConsole.MarkupLine($"[yellow] criteria file {criteriafile.FileName} does not exist[/]");
                    return;
                }
                var evaluationCriteria = AccuracyEvaluationCriteria.Parse(criteriafile.ReadAllText());
                var evaluationResult = await AccuracyEvaluatorGTP3.Evaluate(summary, evaluationCriteria);
                int characterCount = summary.summary_sentences.Sum(s => s.text.Length);
                var outputObject = new JObject()
                {
                    ["length"] = characterCount,
                    ["accuracy_score"] = evaluationResult.keyMessageResults.Where(m=>m.FoundAt == 0).Sum(m=>m.Weight),
                    ["accuracy"] = new JArray() { evaluationResult.keyMessageResults.Select(r=>JObject.FromObject(r)) }
                };
                outputFile.WriteAllText(JsonConvert.SerializeObject(outputObject, Formatting.Indented));
            });
    }
}