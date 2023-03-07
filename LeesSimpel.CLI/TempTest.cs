using Spectre.Console.Cli;

class TempTest : Command
{
    public override int Execute(CommandContext context)
    {
        var response = @"
        {
          ""sender"": ""Team Gegevenshuis"",
        ""call_to_action"": ""Vraag een betalingsregeling aan of een verzoek om kwijtschelding"",
        ""call_to_action_is_call"": false,
        ""phone_number"": null,
        ""summary_sentences"": [
        {
            ""emoji"": ""🤔"",
            ""text"": ""Je hebt een aanslag gementebelastingen ontvangen voor een belastingiaar dat al is verstreken.""
        },
        {
            ""emoji"": ""💸"",
        ""text"": ""Als je de aanslag niet op tijd kunt betalen, kun je een betalingsregeling aanvragen.""
        }
        ]
            }
        ";
        
        var summary = System.Text.Json.JsonSerializer.Deserialize<Summary>(response);

        
        return 0;
    }
}