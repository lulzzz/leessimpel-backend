public class Summary
{
    public struct Sentence
    {
        public string text  { get; set; }
        public string emoji  { get; set; }
    }

    public required Sentence[] summary_sentences { get; set; }
    public string? sender  { get; set; }
    public string? call_to_action  { get; set; }
    public bool? call_to_action_is_call  { get; set; }
    public string? phone_number  { get; set; }
}