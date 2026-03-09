using System.Collections.Concurrent;
using ValidayServer.Network.Interfaces;

namespace ValidayServer.Network
{
    /// <summary>
    /// Thread-safe per-client session store backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// </summary>
    public sealed class ClientSession : IClientSession
    {
        private readonly ConcurrentDictionary<string, object?> _store =
            new ConcurrentDictionary<string, object?>();

        /// <inheritdoc/>
        public void Set<T>(string key, T value)
        {
            _store[key] = value;
        }

        /// <inheritdoc/>
        public T? Get<T>(string key)
        {
            if (_store.TryGetValue(key, out object? raw) && raw is T typed)
                return typed;

            return default;
        }

        /// <inheritdoc/>
        public bool TryGet<T>(string key, out T? value)
        {
            if (_store.TryGetValue(key, out object? raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool Has(string key) => _store.ContainsKey(key);

        /// <inheritdoc/>
        public void Remove(string key) => _store.TryRemove(key, out _);

        /// <inheritdoc/>
        public void Clear() => _store.Clear();
    }
}
