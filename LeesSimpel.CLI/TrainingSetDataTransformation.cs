using NiceIO;
using Spectre.Console;

static class TrainingSetDataTransformation
{
    public static async Task<int> ExecuteAsync(string inputSet, string outputSet,
        Func<NPath, NPath, Task> transformationFunction)
    {
        var queue = new Queue<NPath>(TrainingSet.Directory.Combine(inputSet).Files());

        var outputDir = TrainingSet.Directory
            .Combine(outputSet)
            .DeleteIfExists()
            .EnsureDirectoryExists();

        var throttledExecution = new ThrottledExecution<string>(() =>
        {
            if (!queue.Any())
                return null;

            var inputFile = queue.Dequeue();
            var resultFile = outputDir.Combine($"{inputFile.FileNameWithoutExtension}.txt");
            
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
                Description = inputFile.FileNameWithoutExtension,
                Task = task
            };
        });

        await throttledExecution.ExecuteAsync();
        return 0;
    }
}