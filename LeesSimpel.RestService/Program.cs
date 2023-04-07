using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;

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

var azureOCR = new AzureFormRecognizer();

async Task<IResult> LogTelemetryAndSummarizeContents(HttpContext httpContext, string contentsOfLetter)
{
    var requestTelemetry = httpContext.Features.Get<RequestTelemetry>();
    requestTelemetry?.Properties.Add("OCRResult", contentsOfLetter);
    //var summary = await ClassicHackathonSummarizer.Summarize(contentsOfLetter);
    var summary = await GPT4Summarizer.Summarize(contentsOfLetter, "gpt-3.5-turbo", requestTelemetry);
    var serializeObject = JsonConvert.SerializeObject(summary);
    
    //Console.WriteLine($"Response: {serializeObject}");
    return Results.Text(serializeObject);
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

app.MapPost($"/v2/summarize_text", async (HttpResponse response, SummarizeTextParameters summarizeTextParameters) =>
{
    response.Headers.Add("Content-Type", "text/event-stream");
    
    await foreach (var o in GPT4Summarizer.SummarizeStreamed(summarizeTextParameters.TextToSummarize, "gpt-3.5-turbo"))
    {
        await response.WriteAsync(o);
        await response.WriteAsync("\n");
        await response.Body.FlushAsync();
    }

    return Results.Ok();
});

app.MapPost("/feedback", async (HttpContext context, IFormFileCollection images) =>
{
    // Read the parameters from the request body
    //var form = await context.Request.ReadFormAsync();
    //
    // if (form.TryGetValue("ocrresult", out var ocrResult))
    //     Console.WriteLine($"OCRResult: {ocrResult}");
    //
    // if (form.TryGetValue("humanfeedback", out var humanfeedback))
    //     Console.WriteLine($"humanfeedback: {humanfeedback}");
    //
    // if (form.TryGetValue("summary", out var summary))
    //     Console.WriteLine($"summary: {summary}");
    //
    // foreach (var image in images)
    // {
    //     Console.WriteLine("image: " + image.FileName);
    // }

    await Task.Delay(TimeSpan.FromSeconds(3));

    // Return JSON object with feedbackid
    return Results.Json(new { feedbackid = "123456" });
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
