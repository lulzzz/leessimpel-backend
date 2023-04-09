using System.Reflection;
using System.Text;

static class Tools
{
    public static async IAsyncEnumerable<string> EnumerateFullLines(IAsyncEnumerable<string?> chunks)
    {
        StringBuilder sb = new StringBuilder();
        await foreach (var chunk in chunks)
        {
            sb.Append(chunk);
            var lines = sb.ToString().Split('\n');

            for (int i = 0; i < lines.Length - 1; i++)
            {
                var fullLine = lines[i].TrimEnd('\r');
                yield return fullLine;
            }

            sb.Clear();
            sb.Append(lines[^1]);
        }

        if (sb.Length > 0)
        {
            var lastLine = sb.ToString().TrimEnd('\r', '\n');
            // Operate on the last line, if not empty
            if (lastLine.Length > 0)
                yield return lastLine;
        }
    }

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