using System.Reflection;
using System.Text;

static class Tools
{
    public static string ReadTextFromResource(string resourceName, Assembly assembly)
    {
        var executingAssembly = assembly;
        var manifestResourceNames = executingAssembly.GetManifestResourceNames();
        var name = manifestResourceNames.Single(s => s.Contains(resourceName));

        using Stream? stream = executingAssembly.GetManifestResourceStream(name);
        
        if (stream == null)
            throw new InvalidOperationException($"Embedded resource '{name}' not found.");

        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}