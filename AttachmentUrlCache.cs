// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026 TheMonHub

using Microsoft.Extensions.Caching.Memory;

namespace JimmyRewrite;

public static class AttachmentUrlCache
{
    private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
    private static long _currentKey;

    public static long Add(string value, DateTimeOffset expirationTime)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(expirationTime);

        var key = Interlocked.Increment(ref _currentKey);
        Cache.Set(key, value, cacheEntryOptions);
        return key;
    }

    public static string? Get(long key)
    {
        return Cache.TryGetValue(key, out string? value) ? value : null;
    }
}