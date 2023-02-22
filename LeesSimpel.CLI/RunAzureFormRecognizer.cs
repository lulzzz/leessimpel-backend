using System.Text;
using CliWrap;
using Spectre.Console.Cli;

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

class RunAzureFormRecognizer : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var azure = new AzureFormRecognizer();
        await TrainingSetDataTransformation.ExecuteAsync("AzureFormRecognizer", "txt","originals","azureformrecognizer", async (inputFile, outputFile) =>
        {
            var ocrString = await azure.ImageToText(new MemoryStream(inputFile.ReadAllBytes()));
            outputFile.WriteAllText(ocrString);
        });
        
        return 0;
    }
}