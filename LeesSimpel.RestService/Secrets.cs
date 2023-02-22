using System.Collections;

public static class Secrets
{
    static ConfigurationManager? _configurationManager;

    public static string Get(string key)
    {
        var value = _configurationManager?[key];
        if (value != null)
            return value;

        value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(value))
            return value;

        foreach (DictionaryEntry kvp in Environment.GetEnvironmentVariables())
        {
            Console.WriteLine($"Key: {kvp.Key} Value={kvp.Value}");
            Console.Error.WriteLine($"Key: {kvp.Key} Value={kvp.Value}");
        }
        
        throw new ApplicationException($"The configuration entry {key} has no value3"); 
    }

    public static void Initialize(ConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }
}