using Spectre.Console.Cli;

var app = new CommandApp();


app.Configure(config =>
{
    config.AddCommand<RunAzureFormRecognizer>("azureformrecognizer");
    config.AddCommand<RunAppleOCR>("appleocr");
});

return app.Run(args);