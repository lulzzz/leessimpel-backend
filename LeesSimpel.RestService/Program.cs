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

app.MapGet($"/privacy", async (HttpContext context) =>
{
    IEnumerable<AppMessage> messages = new TextBlockAppMessage[]
    {
        new("De app slaat de brief niet op.", "🙈"),
        new("De app gebruikt AI van OpenAI om de tekst in de brief te versimpelen", "🤖"),
        new("OpenAI bewaart de tekst niet langer dan 30 dagen. Dat doen ze alleen om te controleren of mensen OpenAI niet misbruiken voor dingen die verboden zijn.", "🔒"),
        new("We vragen bij elke brief om je toestemming", "👍"),
        new("Je kunt ook eerst je naam en adres zwart maken", "🏴"),
        new("De app doet het dan nog steeds", "✅"),
        new("Meer weten over privacy? Kijk op leessimpel.nl/privacy", "🔒"),
    };
    foreach (var msg in messages.Prepend(new TitleAppMessage("Wat gebeurt er met je foto", success:true)))
    {
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(msg, AppMessageJsonConverter.Options));
        await context.Response.WriteAsync("\n");
    }
    return Results.Empty;
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
