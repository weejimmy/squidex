﻿// ==========================================================================
//  CachingSchemaProvider.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Squidex.Infrastructure;
using Squidex.Read.Schemas.Repositories;
using Squidex.Read.Utils;

// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
// ReSharper disable InvertIf

namespace Squidex.Read.Schemas.Services.Implementations
{
    public class CachingSchemaProvider : CachingProvider, ISchemaProvider
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
        private readonly ISchemaRepository repository;

        public CachingSchemaProvider(IMemoryCache cache, ISchemaRepository repository)
            : base(cache)
        {
            Guard.NotNull(repository, nameof(repository));

            this.repository = repository;
        }

        public async Task<ISchemaEntityWithSchema> FindSchemaByIdAsync(Guid id)
        {
            var cacheKey = BuildIdCacheKey(id);

            ISchemaEntityWithSchema result;

            if (!Cache.TryGetValue(cacheKey, out result))
            {
                result = await repository.FindSchemaAsync(id);

                Cache.Set(cacheKey, result, CacheDuration);

                if (result != null)
                {
                    Cache.Set(BuildNameCacheKey(result.AppId, result.Name), result, CacheDuration);
                }
            }

            return result;
        }

        public async Task<ISchemaEntityWithSchema> FindSchemaByNameAsync(Guid appId, string name)
        {
            Guard.NotNullOrEmpty(name, nameof(name));

            var cacheKey = BuildNameCacheKey(appId, name);

            ISchemaEntityWithSchema result;

            if (!Cache.TryGetValue(cacheKey, out result))
            {
                result = await repository.FindSchemaAsync(appId, name);

                Cache.Set(cacheKey, result, CacheDuration);

                if (result != null)
                {
                    Cache.Set(BuildIdCacheKey(result.Id), result, CacheDuration);
                }
            }

            return result;
        }

        public void Remove(NamedId<Guid> appId, NamedId<Guid> schemaId)
        {
            Cache.Remove(BuildIdCacheKey(schemaId.Id));
            Cache.Remove(BuildNameCacheKey(appId.Id, schemaId.Name));
        }

        private static string BuildNameCacheKey(Guid appId, string name)
        {
            return $"Schema_Ids_{appId}_{name}";
        }

        private static string BuildIdCacheKey(Guid schemaId)
        {
            return $"Schema_Names_{schemaId}";
        }
    }
}
