using System.Text;
using System.Text.Encodings.Web;
using Spectre.Console.Cli;
using System.Text.Json;
using System.Text.RegularExpressions;

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
                var promptMessages = await SummarizeWithTechnique(readAllText, settings.Technique);
                var jsons = promptMessages.Select(m => Regex.Unescape(JsonSerializer.Serialize(m))).ToArray();
                var lines = jsons.SeparateWith("\n");
                outputFile.WriteAllBytes(new UTF8Encoding(true).GetBytes(lines));
            });
    }

    static Task<GPT4Summarizer.PromptResponseMessage[]> SummarizeWithTechnique(string readAllText, string? settingsTechnique) =>
        (settingsTechnique ?? "gpt3") switch
        {
            "gpt35" => new GPT4Summarizer().PromptResponseMessagesFor(readAllText, "gpt-3.5-turbo").ToArray(),
            "gpt4" => new GPT4Summarizer().PromptResponseMessagesFor(readAllText, "gpt-4").ToArray(),
            _ => throw new ArgumentException($"Unknown technique: {settingsTechnique}")
        };
    

}