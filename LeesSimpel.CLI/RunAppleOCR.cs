using System.Text;
using CliWrap;
using Spectre.Console.Cli;

// ReSharper disable once ClassNeverInstantiated.Global
class Summarize : AsyncCommand<Summarize.Settings>
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
                outputFile.WriteAllText(summary);
            });
    }
}



// ReSharper disable once ClassNeverInstantiated.Global
class RunAppleOCR : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        await TrainingSetDataTransformation.ExecuteAsync("AppleOCR", "txt","originals","appleocr", async (inputFile, outputFile) =>
        {
            var outputString = new StringBuilder();
            await Cli.Wrap("swift")
                .WithArguments(Directories.Backend.Combine($"Tools/VNRecognizeTextRequestTool.swift {inputFile}").ToString())
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(outputString))
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync();
            
            outputFile.WriteAllText(outputString.ToString());
        });
        
        return 0;
    }
}