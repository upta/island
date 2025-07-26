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
        Debug.Log($"Entity spawned: {entity.EntityId}");

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

        // Create interpolation buffer for this entity
        var stateBuffer = new TransformInterpolator.StateBuffer(
            maxSize: 32,
            maxStateAge: 2f,
            interpDelay: 0.2f, // Increased delay for better network interpolation
            timeSyncSamples: 10
        );

        // Add initial state - use server timestamp if available, otherwise current time with simulated delay
        float timestamp = GetTimestampInSeconds(entity.LastUpdated);
        var initialState = new TransformInterpolator.TransformState(
            entity.Position,
            entity.Rotation,
            entity.Velocity,
            entity.AngularVelocity,
            timestamp
        );
        stateBuffer.AddStateWithTimeSync(initialState, Time.time);

        Debug.Log(
            $"Created interpolation buffer for entity {entity.EntityId} with server timestamp {timestamp} (local time {Time.time})"
        );

        _entityBuffers[entity.EntityId] = stateBuffer;
    }

    private void OnEntityMoved(Entity entity)
    {
        if (_entityBuffers.TryGetValue(entity.EntityId, out var stateBuffer))
        {
            // Add new movement state to the interpolation buffer
            // Use server timestamp for realistic network behavior
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
            // Fallback to direct update if no buffer exists
            var spawnedEntity = _spawnedEntities[entity.EntityId];

            if (spawnedEntity != null)
            {
                spawnedEntity.transform.SetPositionAndRotation(entity.Position, entity.Rotation);
            }
        }
    }

    private void OnEntityDespawned(int entityId)
    {
        Debug.Log($"Entity despawned: {entityId}");

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
        // Update all entity positions using interpolation
        foreach (var kvp in _entityBuffers)
        {
            int entityId = kvp.Key;
            var stateBuffer = kvp.Value;

            if (
                _spawnedEntities.TryGetValue(entityId, out var spawnedEntity)
                && spawnedEntity != null
            )
            {
                // Get interpolated state for current time
                var currentState = stateBuffer.GetCurrentState(Time.time);

                // Apply interpolated transform
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

    // Track the first timestamp we receive to use as a reference point
    private static long? _baseServerTimestamp = null;
    private static float _baseLocalTime = 0f;

    private float GetTimestampInSeconds(SpacetimeDB.Timestamp timestamp)
    {
        try
        {
            // Convert microseconds since Unix epoch to a relative timestamp
            var microseconds = timestamp.MicrosecondsSinceUnixEpoch;

            // Initialize base timestamp on first call
            if (_baseServerTimestamp == null)
            {
                _baseServerTimestamp = microseconds;
                _baseLocalTime = Time.time;

                return _baseLocalTime;
            }

            // Calculate relative time from the base timestamp
            long relativeMicroseconds = microseconds - _baseServerTimestamp.Value;
            float relativeSeconds = relativeMicroseconds / 1_000_000.0f;
            float adjustedTimestamp = _baseLocalTime + relativeSeconds;

            return adjustedTimestamp;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to convert timestamp: {ex.Message}");

            // Fallback: use current time
            Debug.LogWarning("Could not convert server timestamp, using local time");
            return Time.time;
        }
    }
}
