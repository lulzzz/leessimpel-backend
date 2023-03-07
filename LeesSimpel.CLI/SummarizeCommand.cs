using Newtonsoft.Json;
using Spectre.Console.Cli;

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
                outputFile.WriteAllText(JsonConvert.SerializeObject(summary));
            });
    }
}