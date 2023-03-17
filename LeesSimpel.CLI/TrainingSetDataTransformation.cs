using NiceIO;
using Spectre.Console;

static class TrainingSetDataTransformation
{
    public static async Task<int> ExecuteAsync(string processDescription, string outputExtension, string inputSet, string outputSet,
        Func<NPath, NPath, Task> transformationFunction)
    {
        var queue = new Queue<NPath>(Directories.TrainingSet.Combine(inputSet).Files().Where(f=>!f.FileName.StartsWith('.'))
            //.Take(1)
        );

        var outputDir = Directories.TrainingSet
            .Combine(outputSet)
            .DeleteIfExists()
            .EnsureDirectoryExists();

        bool anyTaskFailed = false;
        
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

                anyTaskFailed = true;
                var azureJobException = task.Exception;
                if (azureJobException != null)
                {
                    AnsiConsole.WriteException(azureJobException);
                    resultFile.WriteAllText($"{processDescription} failed with exception: " + azureJobException);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]{processDescription} had no exception[/]");
                    resultFile.WriteAllText($"{processDescription} had no exception");
                }
            });
            
            return new()
            {
                Description = $"{processDescription} {inputFile.RelativeTo(Directories.TrainingSet)} -> {resultFile.RelativeTo(Directories.TrainingSet)}",
                Task = task
            };
        });

        await throttledExecution.ExecuteAsync();
        return anyTaskFailed ? 1 : 0;
    }
}