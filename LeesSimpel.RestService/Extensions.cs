public static class Extensions
{
    public static void Add<T>(this List<T> list, IEnumerable<T> items) => list.AddRange(items);

    public static string SeparateWith(this IEnumerable<string> list, string separator) => string.Join(separator, list);
}