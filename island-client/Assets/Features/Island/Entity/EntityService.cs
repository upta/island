using System;
using System.Collections.Generic;
using SpacetimeDB.Types;

public class EntityService
{
    private readonly DbConnection _connection;

    public EntityService(DbConnection connection)
    {
        _connection = connection;

        foreach (var entity in _connection.Db.Entities.Iter())
        {
            EntitySpawned?.Invoke(entity);
        }

        _connection.Db.Entities.OnInsert += (ctx, entity) =>
        {
            EntitySpawned?.Invoke(entity);
        };

        _connection.Db.Entities.OnDelete += (ctx, entity) =>
        {
            EntityDespawned?.Invoke(entity.EntityId);
        };

        _connection.Db.Entities.OnUpdate += (ctx, prev, next) =>
        {
            if (prev.Position != next.Position || prev.Rotation != next.Rotation)
            {
                EntityMoved?.Invoke(next);
            }

            EntityUpdated?.Invoke(prev, next);
        };
    }

    public event Action<Entity> EntitySpawned;
    public event Action<int> EntityDespawned;
    public event Action<Entity> EntityMoved;
    public event Action<Entity, Entity> EntityUpdated;

    public IEnumerable<Entity> Existing => _connection.Db.Entities.Iter();
}
