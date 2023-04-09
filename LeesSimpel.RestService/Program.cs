using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<UserFeedbackUploader>();
builder.Services.AddSingleton<AzureFormRecognizer>();
builder.Services.AddSingleton<GPT4Summarizer>();

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

SummarizeEndPoint.Register(app);


app.MapPost("/feedback", async (HttpContext context, IFormFileCollection images, UserFeedbackUploader userFeedbackUploader) =>
{
     //Read the parameters from the request body
    var form = await context.Request.ReadFormAsync();

    form.TryGetValue("humanfeedback", out var humanfeedback);
    form.TryGetValue("ocrResult", out var ocrResult);
    form.TryGetValue("summary", out var summary);

    string feedbackId = await userFeedbackUploader.Upload(ocrResult, humanfeedback, summary, images);     
     
    // Return JSON object with feedbackid
    return Results.Json(new { feedbackid = feedbackId });
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
