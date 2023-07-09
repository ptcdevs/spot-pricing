namespace aws_console;

public static class LinqPlus
{
    public static TAccumulate AggregateUntil<TSource, TAccumulate>(this IEnumerable<TSource> source, 
        TAccumulate seed, 
        Func<TAccumulate, TSource, TAccumulate> func, 
        Func<TAccumulate,bool> untilFunc) 
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }
        TAccumulate result = seed;
        foreach (TSource element in source)
        {
            result = func(result, element);
            if (untilFunc(result))
            {
                break;
            }
        }
        return result;
    }
}