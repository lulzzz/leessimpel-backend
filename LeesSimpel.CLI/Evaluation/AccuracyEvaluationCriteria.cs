using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class AccuracyEvaluationCriteria
{
    public Criterion[] Criteria;
    
    public class Criterion
    {
        public float weight;
    }
    
    public class ContainsKeyMessage : Criterion
    {
        public string keyMessage;

        public override string ToString() => keyMessage;
    }

    public class HasSender : Criterion
    {
        public string sender;
        public override string ToString() => sender;
    }

    public static AccuracyEvaluationCriteria Parse(string json)
    {
        var array = JArray.Load(new JsonTextReader(new StringReader(json))) ?? throw new ArgumentException("Expected json array");
        return Parse(array);
    }

    public static AccuracyEvaluationCriteria Parse(JArray array)
    {
        var criteria = new List<Criterion>();
        foreach (var jo in array)
        {
            var jObject = jo as JObject ??
                          throw new ArgumentException("evaluation criterea array contains something that's not an object");

            JProperty jp = jObject.Properties().FirstOrDefault() ??
                           throw new ArgumentException("Object didn't have a property");

            switch (jp.Name)
            {
                case "contains_fact":
                    var value = jp.Value.Value<string>();
                    var jToken = jo["weight"];
                    var weight = jToken?.Value<float>();
                    criteria.Add(new ContainsKeyMessage {keyMessage = value, weight = weight ?? 1f});
                    break;
                case "has_sender":
                    criteria.Add(new HasSender() {sender = jp.Value.Value<string>(), weight = jo["weight"]?.Value<float>() ?? 1f});
                    break;
                default:
                    throw new ArgumentException($"'{jp.Name}' is an unknown criteria type");
            }
        }

        return new() {Criteria = criteria.ToArray()};
    }
}