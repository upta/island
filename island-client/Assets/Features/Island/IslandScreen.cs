using System.Collections.Generic;
using Reflex.Attributes;
using SpacetimeDB.Types;
using UnityEngine;

public class IslandScreen : MonoBehaviour
{
    [SerializeField]
    private GameObject _entityPrefab;

    [Inject]
    private readonly EntityService _entityService;

    private readonly Dictionary<int, GameObject> _spawnedEntities = new();
    private readonly Dictionary<int, TransformInterpolator.StateBuffer> _entityBuffers = new();

    public void Awake()
    {
        _entityService.EntitySpawned += OnEntitySpawned;
        _entityService.EntityDespawned += OnEntityDespawned;
        _entityService.EntityMoved += OnEntityMoved;

        foreach (var entity in _entityService.Existing)
        {
            OnEntitySpawned(entity);
        }
    }

    private void OnEntitySpawned(Entity entity)
    {
        if (_entityPrefab == null)
        {
            Debug.LogWarning("Entity prefab is not assigned!");
            return;
        }

        if (_spawnedEntities.ContainsKey(entity.EntityId))
        {
            return;
        }

        var spawnedEntity = Instantiate(_entityPrefab, entity.Position, entity.Rotation);
        _spawnedEntities[entity.EntityId] = spawnedEntity;

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
        }
        else if (_spawnedEntities.ContainsKey(entity.EntityId))
        {
            var spawnedEntity = _spawnedEntities[entity.EntityId];

            if (spawnedEntity != null)
            {
                spawnedEntity.transform.SetPositionAndRotation(entity.Position, entity.Rotation);
            }
        }
    }

    private void OnEntityDespawned(int entityId)
    {
        if (_spawnedEntities.TryGetValue(entityId, out var spawnedEntity))
        {
            if (spawnedEntity != null)
            {
                Destroy(spawnedEntity);
            }
            _spawnedEntities.Remove(entityId);
        }

        if (_entityBuffers.ContainsKey(entityId))
        {
            _entityBuffers.Remove(entityId);
        }
    }

    private void Update()
    {
        foreach (var kvp in _entityBuffers)
        {
            int entityId = kvp.Key;
            var stateBuffer = kvp.Value;

            if (
                _spawnedEntities.TryGetValue(entityId, out var spawnedEntity)
                && spawnedEntity != null
            )
            {
                var currentState = stateBuffer.GetCurrentState(Time.time);

                spawnedEntity.transform.SetPositionAndRotation(
                    currentState.position,
                    currentState.rotation
                );
            }
        }
    }

    private void OnDestroy()
    {
        if (_entityService != null)
        {
            _entityService.EntitySpawned -= OnEntitySpawned;
            _entityService.EntityDespawned -= OnEntityDespawned;
            _entityService.EntityMoved -= OnEntityMoved;
        }

        foreach (var spawnedEntity in _spawnedEntities.Values)
        {
            if (spawnedEntity != null)
            {
                Destroy(spawnedEntity);
            }
        }

        _spawnedEntities.Clear();
        _entityBuffers.Clear();
    }

    private static long? _baseServerTimestamp = null;
    private static float _baseLocalTime = 0f;

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
