using Reflex.Attributes;
using UnityEngine;

public class Entry : MonoBehaviour
{
    [Inject]
    private readonly ConnectionService _connectionService;

    [SerializeField]
    private IslandScreen _islandScreen;

    private async void Start()
    {
        await _connectionService.Connect();

        ShowIsland();
    }

    private void ShowIsland()
    {
        _islandScreen = Instantiate(_islandScreen);
    }
}
