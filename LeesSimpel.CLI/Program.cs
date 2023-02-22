using Spectre.Console.Cli;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder().AddUserSecrets<Program>();
var configuration = builder.Build();
Secrets.Initialize(configuration);

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<RunAzureFormRecognizer>("azureformrecognizer");
    config.AddCommand<RunAppleOCR>("appleocr");
    config.AddCommand<Summarize>("summarize");
});

return app.Run(args);