using System.Text.Json;

public static class Extensions
{
    public static void Add<T>(this List<T> list, IEnumerable<T> items) => list.AddRange(items);

    public static string SeparateWith(this IEnumerable<string> list, string separator) => string.Join(separator, list);
    
    public static async Task<T[]> ToArray<T>(this IAsyncEnumerable<T> asyncEnumerable)
    {
        var list = new List<T>();
        await foreach (var item in asyncEnumerable.ConfigureAwait(false)) 
            list.Add(item);

        return list.ToArray();
    }

    internal static async Task WriteAppMessage(this HttpResponse response, AppMessage msg)
    {
        await response.WriteAsync(JsonSerializer.Serialize(msg, AppMessageJsonConverter.Options));
        await response.WriteAsync("\n");
    }
}