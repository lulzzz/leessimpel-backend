using System.Text;
using CliWrap;
using Spectre.Console.Cli;


// ReSharper disable once ClassNeverInstantiated.Global
class RunAppleOCRCommand : AsyncCommand
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