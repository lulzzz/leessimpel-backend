using System.Text;
using Newtonsoft.Json;
using Spectre.Console.Cli;

// ReSharper disable once ClassNeverInstantiated.Global
class SummarizeCommand : AsyncCommand<SummarizeCommand.Settings>
{
    
    internal class Settings : CommandSettings
    {
        [CommandArgument(0, "<inputset>")]
        public string InputSet { get; set; } = null!;
        
        [CommandArgument(1, "[technique]")]
        public string? Technique { get; set; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        return await TrainingSetDataTransformation.ExecuteAsync("Summarize", "json", settings.InputSet,
            settings.InputSet + "_summarized"+(settings.Technique  == null ? "" : $"-{settings.Technique}"),
            async (inputFile, outputFile) =>
            {
                var readAllText = inputFile.ReadAllText();
                var summary = await SummarizeWithTechnique(readAllText, settings.Technique);
                outputFile.WriteAllBytes(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(summary, Formatting.Indented)));
            });
    }

    static Task<Summary> SummarizeWithTechnique(string readAllText, string? settingsTechnique) =>
        (settingsTechnique ?? "gpt3") switch
        {
            "gpt35" => GPT4Summarizer.Summarize(readAllText, "gpt-3.5-turbo"),
            "gpt4" => GPT4Summarizer.Summarize(readAllText, "gpt-4"),
            "gpt3" => ClassicHackathonSummarizer.Summarize(readAllText),
            _ => throw new ArgumentException($"Unknown technique: {settingsTechnique}")
        };
}