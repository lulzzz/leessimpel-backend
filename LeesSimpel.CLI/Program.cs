using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;
using NiceIO;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();


app.Configure(config =>
{
    config.AddCommand<RunAzureFormRecognizer>("azureformrecognizer");
});

return app.Run(args);

class RunAzureFormRecognizer : AsyncCommand
{
    readonly int MaxConcurrentJobs = 4;

    record ActiveJob
    {
        public Task<string> azureJob;
        public NPath photo;
        public ProgressTask ansiConsoleTask;
    }
    
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var queue = new Queue<NPath>(TrainingSet.OriginalPhotos);
        var activeRequests = new List<ActiveJob>();
        var azure = new AzureFormRecognizer();
        
        var completedTasks = new List<ActiveJob>();

        await AnsiConsole.Progress().StartAsync(async ansiConsoleProgressContext =>
        {
            while (true)
            {
                
                while (activeRequests.Count < MaxConcurrentJobs && queue.Any())
                {
                    var photo = queue.Dequeue();

                    var ansiConsoleTask = ansiConsoleProgressContext.AddTask(
                        $"AzureFormRecognizer on [yellow]{photo.FileName}[/]");
                    ansiConsoleTask.IsIndeterminate = true;
                    ansiConsoleTask.MaxValue = 1;
                    activeRequests.Add(new()
                    {
                        photo = photo,
                        azureJob = azure.ImageToText(new MemoryStream(photo.ReadAllBytes())),
                        ansiConsoleTask = ansiConsoleTask
                    });
                }

                if (activeRequests.Count == 0 && !queue.Any())
                    break;

                await Task.WhenAny(activeRequests.Select(a => a.azureJob).ToArray());

                foreach (var request in activeRequests.Where(r=>r.azureJob.IsCompleted).ToArray())
                {
                    completedTasks.Add(request);
                    activeRequests.Remove(request);
                    request.ansiConsoleTask.StopTask();
                    request.ansiConsoleTask.Value = 1;

                    var resultFile = TrainingSet.Directory
                        .Combine($"azureformrecognizer/{request.photo.FileNameWithoutExtension}.txt")
                        .EnsureParentDirectoryExists();

                    if (request.azureJob.IsCompletedSuccessfully)
                    {
                        //AnsiConsole.MarkupLine($"[green]{request.photo.FileName}[/]");
                        resultFile.WriteAllText(request.azureJob.Result);
                    }
                    else
                    {
                        //AnsiConsole.MarkupLine($"[red]{request.photo.FileName}[/]");

                        var azureJobException = request.azureJob.Exception;
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
                    }
                }
            }
        });

        return 0;
    }

}

static class TrainingSet
{
    static NPath BackendDirectory { get; set; } = NPath.CurrentDirectory.ParentContaining("leessimpel-backend");
    public static IEnumerable<NPath> OriginalPhotos => Directory.Combine("originals").Files();
    public static NPath Directory => BackendDirectory.Combine("leessimpel-trainingset");
}
