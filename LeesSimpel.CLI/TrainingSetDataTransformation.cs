using NiceIO;
using Spectre.Console;

static class TrainingSetDataTransformation
{
    public static async Task ExecuteAsync(string processDescription, string outputExtension, string inputSet, string outputSet,
        Func<NPath, NPath, Task> transformationFunction)
    {
        var queue = new Queue<NPath>(Directories.TrainingSet.Combine(inputSet).Files());

        var outputDir = Directories.TrainingSet
            .Combine(outputSet)
            .DeleteIfExists()
            .EnsureDirectoryExists();

        var throttledExecution = new ThrottledExecution(() =>
        {
            if (!queue.Any())
                return null;

            var inputFile = queue.Dequeue();
            var resultFile = outputDir.Combine($"{inputFile.FileNameWithoutExtension}.txt").ChangeExtension(outputExtension);
            
            var task = transformationFunction(inputFile, resultFile);

            task.ContinueWith(_ =>
            {
                if (task.IsCompletedSuccessfully)
                    return;

                var azureJobException = task.Exception;
                if (azureJobException != null)
                {
                    AnsiConsole.WriteException(azureJobException);
                    resultFile.WriteAllText("Job failed with exception: " + azureJobException);
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]azurejob had no exception[/]");
                    resultFile.WriteAllText("azurejob had no exception");
                }
            });
            
            return new()
            {
                Description = $"{processDescription} {inputFile.RelativeTo(Directories.TrainingSet)} -> {resultFile.RelativeTo(Directories.TrainingSet)}",
                Task = task
            };
        });

        await throttledExecution.ExecuteAsync();
    }
}