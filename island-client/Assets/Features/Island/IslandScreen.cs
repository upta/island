using System.Collections.Generic;
using Reflex.Attributes;
using SpacetimeDB.Types;
using UnityEngine;

public class IslandScreen : MonoBehaviour
{
    [SerializeField]
    private GameObject _entityPrefab;

    [Inject]
    private readonly EntitySpawnService _entitySpawnService;

    [Inject]
    private readonly EntityMoveService _entityMoveService;

    private readonly Dictionary<int, GameObject> _spawnedEntities = new();

    public void Awake()
    {
        _entitySpawnService.EntitySpawned += OnEntitySpawned;
        _entityMoveService.EntityMoved += OnEntityMoved;

        foreach (var entity in _entitySpawnService.Existing)
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
    }

    private void OnEntityMoved(Entity entity)
    {
        Debug.Log($"Entity moved: {entity.EntityId} to position {entity.Position}");

        if (
            _spawnedEntities.TryGetValue(entity.EntityId, out var spawnedEntity)
            && spawnedEntity != null
        )
        {
            spawnedEntity.transform.position = entity.Position;
            spawnedEntity.transform.rotation = entity.Rotation;
        }
    }

    private void OnDestroy()
    {
        if (_entitySpawnService != null)
        {
            _entitySpawnService.EntitySpawned -= OnEntitySpawned;
        }

        if (_entityMoveService != null)
        {
            _entityMoveService.EntityMoved -= OnEntityMoved;
        }

        foreach (var spawnedEntity in _spawnedEntities.Values)
        {
            if (spawnedEntity != null)
            {
                Destroy(spawnedEntity);
            }
        }

        _spawnedEntities.Clear();
    }
}
