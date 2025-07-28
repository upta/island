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
    }

    private void OnEntityMoved((Entity entity, Vector3 position, Quaternion rotation) moveData)
    {
        var (entity, position, rotation) = moveData;
        if (
            _spawnedEntities.TryGetValue(entity.EntityId, out var spawnedEntity)
            && spawnedEntity != null
        )
        {
            spawnedEntity.transform.SetPositionAndRotation(position, rotation);
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
    }
}
