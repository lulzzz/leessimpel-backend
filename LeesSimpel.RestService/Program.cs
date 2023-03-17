using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI();

// if (!app.Environment.IsDevelopment())
// {
//     app.UseExceptionHandler("/Error");
//     app.UseHsts();
//}

Secrets.Initialize(builder.Configuration);

var openAISummarizer = new OpenAISummarizer();
var azureOCR = new AzureFormRecognizer();

async Task<IResult> LogTelemetryAndSummarizeContents(HttpContext httpContext, string contentsOfLetter)
{
    // Write request body to App Insights
    var requestTelemetry = httpContext.Features.Get<RequestTelemetry>();
    requestTelemetry?.Properties.Add("OCRResult", contentsOfLetter);
    var summary = await openAISummarizer.Summarize(contentsOfLetter);
    return Results.Text(JsonConvert.SerializeObject(summary));
}

app.MapPost("/summarize_image", async (HttpContext context, IFormFile imageFile) =>
{
    var ocrResult = await azureOCR.ImageToText(imageFile.OpenReadStream());
    return await LogTelemetryAndSummarizeContents(context, ocrResult);
});

app.MapPost("/summarize_text", async (HttpContext context, SummarizeTextParameters summarizeTextParameters) =>
{
    return await LogTelemetryAndSummarizeContents(context, summarizeTextParameters.TextToSummarize);
});

app.MapPost($"/v2/summarize_text", async (SummarizeTextParameters summarizeTextParameters) =>
{
    var summary = await openAISummarizer.Summarize(summarizeTextParameters.TextToSummarize);

    return Results.Text(JsonConvert.SerializeObject(new JArray
    {
        new JObject {["kind"] = "title", ["title"] = "Hier is je versimpelde brief"},
        new JObject {["kind"] = "section", ["title"] = "Van wie is de brief?", ["index"] = "1"},
        new JObject {["kind"] = "textblock", ["text"] = summary.sender},
        new JObject {["kind"] = "section", ["title"] = "Wat staat er in?", ["index"] = "2"},
        summary.summary_sentences.Select(s => new JObject {["kind"] = "textblock", ["text"] = s.text, ["emoji"] = s.emoji})
    }));
});

app.MapGet("/exception", () =>
{
    throw new ArgumentException("EXCEPTION_TEST_LUCAS");
});

app.MapGet("/notfound", () =>
{
    return Results.NotFound();
});

app.UseHttpsRedirection();
app.UseRouting();
app.Run();
