using System;
using System.Collections.Concurrent;
using VRage.ModAPI;

namespace SKO.Torch.Shared.Managers.Entity
{
    public struct EntityExpirationCacheData<T> where T : class, IMyEntity
    {
        public long EntityId;
        public T Entity;
        public TimeSpan ExpiresAt;
        public TimeSpan SetAt;
        public int IntervalSeconds;
    }

    public class EntityExpirationCacheManager<T> : IDisposable where T : class, IMyEntity
    {
        public ConcurrentDictionary<long, EntityExpirationCacheData<T>> Data =
            new ConcurrentDictionary<long, EntityExpirationCacheData<T>>();

        public EntityExpirationCacheManager(int defaultIntervalSeconds = 3)
        {
            DefaultIntervalSeconds = defaultIntervalSeconds;
        }

        public int DefaultIntervalSeconds { get; set; }

        public void Dispose()
        {
        }

        public bool Expired(long id)
        {
            try
            {
                if (Data.ContainsKey(id))
                {
                    var item = Data[id];
                    if (DateTime.Now.TimeOfDay.Subtract(item.ExpiresAt).TotalSeconds >= 0)
                        Data.TryRemove(id, out _);
                    else
                        return false;
                }
            }
            catch
            {
            }

            return true;
        }

        public void SetData(T entity, int? cacheIntervalSeconds = null)
        {
            try
            {
                if (entity != null)
                {
                    var interval = cacheIntervalSeconds.HasValue ? cacheIntervalSeconds.Value : DefaultIntervalSeconds;
                    var currentTime = DateTime.Now.TimeOfDay;

                    Data[entity.EntityId] = new EntityExpirationCacheData<T>
                    {
                        EntityId = entity.EntityId,
                        SetAt = currentTime,
                        ExpiresAt = currentTime.Add(TimeSpan.FromSeconds(interval)),
                        Entity = entity,
                        IntervalSeconds = interval
                    };
                }
            }
            catch
            {
            }
        }

        public EntityExpirationCacheData<T> GetCacheInfo(long id)
        {
            if (Data.ContainsKey(id)) return Data[id];

            return default;
        }

        public T GetData(long id)
        {
            if (!Expired(id)) return Data[id].Entity;

            return default;
        }

        public Q GetData<Q>(long id)
        {
            var data = GetData(id);
            return (Q)Convert.ChangeType(data, typeof(Q));
        }
    }
}