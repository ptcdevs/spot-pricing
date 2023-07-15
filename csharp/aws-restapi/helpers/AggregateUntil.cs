namespace aws_restapi;

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
    
    public static async Task<TAccumulate> AggregateUntilAsync<TSource, TAccumulate>(this IEnumerable<TSource> source, 
        TAccumulate seed, 
        Func<Task<TAccumulate>, TSource, Task<TAccumulate>> func, 
        Func<TAccumulate, bool> untilFunc) 
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }
        Task<TAccumulate> result = Task.FromResult(seed);
        foreach (TSource element in source)
        {
            //result = Task.FromResult(await func(result, element));
            result = Task.FromResult(await func(result, element));
            if (untilFunc(await result))
            {
                break;
            }
        }
        return await result;
    }
}