using Spectre.Console;

class ThrottledExecution
{
    Func<ActiveJob?> StartNextJobFunc { get; }
    int MaxConcurrentTasks { get; }

    public record ActiveJob
    {
        public string Description;
        public Task Task;
    }
    
    public ThrottledExecution(
        Func<ActiveJob?> startNextJobFunc,
        int maxConcurrentTasks = 2)
    {
        StartNextJobFunc = startNextJobFunc;
        MaxConcurrentTasks = maxConcurrentTasks;
    }
    
    public async Task ExecuteAsync()
    {
        var activeJobs = new List<ActiveJob>();

        var progressTasks = new Dictionary<ActiveJob, ProgressTask>();
        
        await AnsiConsole
            .Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new SpinnerColumn(),
                new ElapsedTimeColumn()
            )
            .HideCompleted(false)
            .StartAsync(async ansiConsoleProgressContext =>
            {
                bool depleted = false;
                while (true)
                {
                    while (activeJobs.Count < MaxConcurrentTasks && !depleted)
                    {
                        var nextJob = StartNextJobFunc();
                        if (nextJob == null)
                        {
                            depleted = true;
                            break;
                        }

                        var ansiConsoleTask = ansiConsoleProgressContext.AddTask(nextJob.Description);
                        ansiConsoleTask.IsIndeterminate = true;
                        ansiConsoleTask.MaxValue = 1;
                        progressTasks.Add(nextJob, ansiConsoleTask);
                        
                        activeJobs.Add(nextJob);
                    }

                    if (activeJobs.Count == 0 && depleted)
                        break;

                    await Task.WhenAny(activeJobs.Select(a => a.Task).ToArray());

                    foreach (var request in activeJobs.Where(r => r.Task.IsCompleted).ToArray())
                    {
                        activeJobs.Remove(request);
                        var progressTask = progressTasks[request];
                        
                        progressTask.Description = $"[green]{request.Description}[/]";
                        progressTask.Value = 1;
                        progressTask.StopTask();
                        
                    }
                }
            });
    }
}