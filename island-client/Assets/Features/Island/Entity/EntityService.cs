using System;
using System.Collections.Generic;
using SpacetimeDB.Types;
using UnityEngine;

public class EntityService
{
    private readonly DbConnection _connection;
    private readonly Dictionary<int, TransformInterpolator.StateBuffer> _entityBuffers = new();

    // Time synchronization
    private static long? _baseServerTimestamp = null;
    private static float _baseLocalTime = 0f;

    public EntityService(DbConnection connection)
    {
        _connection = connection;

        foreach (var entity in _connection.Db.Entities.Iter())
        {
            OnEntitySpawned(entity);
        }

        _connection.Db.Entities.OnInsert += (ctx, entity) =>
        {
            OnEntitySpawned(entity);
        };

        _connection.Db.Entities.OnDelete += (ctx, entity) =>
        {
            OnEntityDespawned(entity.EntityId);
        };

        _connection.Db.Entities.OnUpdate += (ctx, prev, next) =>
        {
            OnEntityMoved(next);
        };
    }

    public event Action<Entity> EntitySpawned;
    public event Action<int> EntityDespawned;
    public event Action<(Entity entity, Vector3 position, Quaternion rotation)> EntityMoved;

    public IEnumerable<Entity> Existing => _connection.Db.Entities.Iter();

    private void OnEntitySpawned(Entity entity)
    {
        var stateBuffer = new TransformInterpolator.StateBuffer(
            maxSize: 32,
            maxStateAge: 2f,
            interpDelay: 0.2f,
            timeSyncSamples: 10
        );

        float timestamp = GetTimestampInSeconds(entity.LastUpdated);
        var initialState = new TransformInterpolator.TransformState(
            entity.Position,
            entity.Rotation,
            entity.Velocity,
            entity.AngularVelocity,
            timestamp
        );
        stateBuffer.AddStateWithTimeSync(initialState, Time.time);

        _entityBuffers[entity.EntityId] = stateBuffer;

        EntitySpawned?.Invoke(entity);
    }

    private void OnEntityMoved(Entity entity)
    {
        if (_entityBuffers.TryGetValue(entity.EntityId, out var stateBuffer))
        {
            float timestamp = GetTimestampInSeconds(entity.LastUpdated);
            var newState = new TransformInterpolator.TransformState(
                entity.Position,
                entity.Rotation,
                entity.Velocity,
                entity.AngularVelocity,
                timestamp
            );
            stateBuffer.AddStateWithTimeSync(newState, Time.time);

            // Get the current interpolated state and fire the event
            var currentState = stateBuffer.GetCurrentState(Time.time);
            EntityMoved?.Invoke((entity, currentState.position, currentState.rotation));
        }
    }

    private void OnEntityDespawned(int entityId)
    {
        if (_entityBuffers.ContainsKey(entityId))
        {
            _entityBuffers.Remove(entityId);
        }

        EntityDespawned?.Invoke(entityId);
    }

    /// <summary>
    /// Gets the current interpolated transform for an entity
    /// </summary>
    /// <param name="entityId">The entity ID</param>
    /// <param name="position">The interpolated position</param>
    /// <param name="rotation">The interpolated rotation</param>
    /// <returns>True if the entity exists and has interpolated data</returns>
    public bool TryGetInterpolatedTransform(
        int entityId,
        out Vector3 position,
        out Quaternion rotation
    )
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (_entityBuffers.TryGetValue(entityId, out var stateBuffer))
        {
            var currentState = stateBuffer.GetCurrentState(Time.time);
            position = currentState.position;
            rotation = currentState.rotation;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all entity IDs that have interpolated data
    /// </summary>
    public IEnumerable<int> GetInterpolatedEntityIds()
    {
        return _entityBuffers.Keys;
    }

    private float GetTimestampInSeconds(SpacetimeDB.Timestamp timestamp)
    {
        try
        {
            var microseconds = timestamp.MicrosecondsSinceUnixEpoch;

            if (_baseServerTimestamp == null)
            {
                _baseServerTimestamp = microseconds;
                _baseLocalTime = Time.time;

                return _baseLocalTime;
            }

            long relativeMicroseconds = microseconds - _baseServerTimestamp.Value;
            float relativeSeconds = relativeMicroseconds / 1_000_000.0f;
            float adjustedTimestamp = _baseLocalTime + relativeSeconds;

            return adjustedTimestamp;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to convert timestamp: {ex.Message}");
            Debug.LogWarning("Could not convert server timestamp, using local time");
            return Time.time;
        }
    }
}
