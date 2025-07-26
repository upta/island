using System;
using SpacetimeDB.Types;
using UnityEngine;

public class EntityMoveService
{
    private readonly DbConnection _connection;

    public event Action<Entity> EntityMoved;

    public EntityMoveService(DbConnection connection)
    {
        _connection = connection;

        _connection.Db.Entities.OnUpdate += (ctx, prev, next) =>
        {
            if (prev.Position != next.Position || prev.Rotation != next.Rotation)
            {
                EntityMoved?.Invoke(next);
            }

            Debug.Log(
                $"Entity {next.EntityId} moved from {prev.Position} to {next.Position} with rotation {next.Rotation}"
            );
        };
    }
}
