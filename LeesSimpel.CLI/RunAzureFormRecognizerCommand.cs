using Spectre.Console.Cli;

// ReSharper disable once ClassNeverInstantiated.Global
class RunAzureFormRecognizerCommand : AsyncCommand
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