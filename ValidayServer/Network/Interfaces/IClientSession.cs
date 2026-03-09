namespace ValidayServer.Network.Interfaces
{
    /// <summary>
    /// Per-client key/value session store.
    /// Implementations must be thread-safe.
    /// </summary>
    public interface IClientSession
    {
        /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>.</summary>
        void Set<T>(string key, T value);

        /// <summary>Returns the value stored under <paramref name="key"/>, or the default for <typeparamref name="T"/> if absent.</summary>
        T? Get<T>(string key);

        /// <summary>Returns <c>true</c> and the stored value if <paramref name="key"/> exists; otherwise <c>false</c>.</summary>
        bool TryGet<T>(string key, out T? value);

        /// <summary>Returns <c>true</c> if <paramref name="key"/> exists in the session.</summary>
        bool Has(string key);

        /// <summary>Removes <paramref name="key"/> from the session. No-op if absent.</summary>
        void Remove(string key);

        /// <summary>Removes all entries from the session.</summary>
        void Clear();
    }
}
