using System.Collections.Generic;

namespace MasloBot.Services
{
    public static class Extensions
    {
        public static K SafeGetValue<T, K>(this Dictionary<T, K> source, T key)
        {
            if(source.TryGetValue(key, out var result))
            {
                return result;
            }

            return default;
        }
    }
}
