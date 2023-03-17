using Newtonsoft.Json;
using Spectre.Console.Cli;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
        var openai = new OpenAISummarizer();

        return await TrainingSetDataTransformation.ExecuteAsync("Summarize", "json", settings.InputSet,
            settings.InputSet + "_summarized"+(settings.Technique  == null ? "" : $"-{settings.Technique}"),
            async (inputFile, outputFile) =>
            {
                var summary = await openai.Summarize(inputFile.ReadAllText());
                outputFile.WriteAllText(JsonConvert.SerializeObject(summary, Formatting.Indented));
            });
    }
}

// ReSharper disable once ClassNeverInstantiated.Global