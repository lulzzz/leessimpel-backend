public static class Secrets
{
    static IConfigurationRoot? _configurationRoot;

    public static string Get(string key)
    {
        var value = _configurationRoot?[key];
        if (value != null)
            return value;

        throw new ArgumentException("Key '{key}' not present in ConfigurationRoot");
    }

    public static void Initialize(IConfigurationRoot configurationManager)
    {
        _configurationRoot = configurationManager;
    }
}