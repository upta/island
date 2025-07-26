using System;
using System.Collections.Generic;
using SpacetimeDB.Types;
using Unity.VisualScripting;

public class EntitySpawnService
{
    private readonly DbConnection _connection;

    public EntitySpawnService(DbConnection connection)
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
    }

    public event Action<Entity> EntitySpawned;
    public event Action<int> EntityDespawned;

    public IEnumerable<Entity> Existing => _connection.Db.Entities.Iter();
}
