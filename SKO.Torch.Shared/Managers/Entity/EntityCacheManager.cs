using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace SKO.Torch.Shared.Managers.Entity
{
    /// <summary>
    ///     Entity Cache Manager for Space Engineers.
    ///     by SKO - sko85gaming@gmail.com
    ///     It initializes all entities and keeps track of changes by the game. Reduces the need to always get a fresh list of
    ///     entities from the API.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EntityCacheManager<T> : IDisposable
        where T : MyEntity
    {
        public ConcurrentDictionary<long, T> Entities;

        public Action<T> EntityAdded;
        public Action<long> EntityRemoved;

        public EntityCacheManager()
        {
            var dict = MyEntities.GetEntities().ToDictionary(c => c.EntityId, c => c as T);
            Entities = new ConcurrentDictionary<long, T>(dict);

            // Set events to add/remove entities.
            MyAPIGateway.Entities.OnEntityAdd += OnOnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnOnEntityRemove;
        }

        public void Dispose()
        {
            // Remove events.
            MyAPIGateway.Entities.OnEntityAdd -= OnOnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnOnEntityRemove;
        }

        public IEnumerable<TEntity> GetOf<TEntity>() where TEntity : class, IMyEntity
        {
            return Entities.Values.OfType<TEntity>();
        }

        public TEntity GetOf<TEntity>(long entityId) where TEntity : class, IMyEntity
        {
            if (Entities.ContainsKey(entityId))
                return Entities[entityId] as TEntity;

            return null;
        }

        public T Get(long entityId)
        {
            if (Entities.ContainsKey(entityId))
                return Entities[entityId];

            return null;
        }

        private void OnOnEntityAdd(IMyEntity obj)
        {
            if (obj != null)
                if (!Entities.ContainsKey(obj.EntityId))
                {
                    Entities[obj.EntityId] = obj as T;
                    EntityAdded?.Invoke(Entities[obj.EntityId]);
                }
        }

        private void OnOnEntityRemove(IMyEntity obj)
        {
            if (obj != null)
            {
                var entityId = obj.EntityId;
                if (Entities.ContainsKey(entityId))
                    if (Entities.TryRemove(entityId, out _))
                        EntityRemoved?.Invoke(entityId);
            }
        }
    }
}