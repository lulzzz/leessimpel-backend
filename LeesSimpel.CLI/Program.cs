using Spectre.Console.Cli;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

var builder = new ConfigurationBuilder().AddUserSecrets<Program>();
var configuration = builder.Build();
Secrets.Initialize(configuration);

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<RunAzureFormRecognizerCommand>("azureformrecognizer");
    config.AddCommand<RunAppleOCRCommand>("appleocr");
    config.AddCommand<SummarizeCommand>("summarize");
    config.AddCommand<TestAccuracyEvaluatorCommand>("testevaluator");
    config.AddCommand<EvaluateCommand>("evaluate");
    config.AddCommand<GraphCommand>("graph");
    config.AddCommand<TempTest>("temptest");
});

return app.Run(args);