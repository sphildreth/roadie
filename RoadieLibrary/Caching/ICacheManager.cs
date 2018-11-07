using System;
using System.Threading.Tasks;

namespace Roadie.Library.Caching
{
    public interface ICacheManager : IDisposable
    {
        bool Add<TCacheValue>(string key, TCacheValue value);

        bool Add<TCacheValue>(string key, TCacheValue value, string region);

        bool Add<TCacheValue>(string key, TCacheValue value, CachePolicy policy);

        bool Add<TCacheValue>(string key, TCacheValue value, string region, CachePolicy policy);

        void Clear();

        void ClearRegion(string region);

        bool Exists<TOut>(string key);

        bool Exists<TOut>(string key, string region);

        TOut Get<TOut>(string key, string region);

        TOut Get<TOut>(string key);

    //    Task<TOut> GetAsync<TOut>(string key, Func<Task<TOut>> getItem, string region, CachePolicy policy);

        Task<TOut> GetAsync<TOut>(string key, Func<Task<TOut>> getItem, string region);

        TOut Get<TOut>(string key, Func<TOut> getItem, string region, CachePolicy policy);

        bool Remove(string key);

        bool Remove(string key, string region);
    }
}