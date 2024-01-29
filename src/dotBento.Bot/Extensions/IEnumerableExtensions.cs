namespace dotBento.Bot.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<List<T>> ChunkBy<T>(this List<T> list, int chunkSize)
    {
        for( var index = 0; index < list.Count; index += chunkSize)
        {
            yield return list.GetRange(index, Math.Min(chunkSize, list.Count - index));
        }
    }
}