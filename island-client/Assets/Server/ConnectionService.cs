using System;
using Cysharp.Threading.Tasks;
using SpacetimeDB;
using SpacetimeDB.Types;
using UnityEngine;

public class ConnectionService
{
    const string _serverUrl = "http://127.0.0.1:3000";
    const string _moduleName = "island";

    public DbConnection Connection { get; private set; }

    private UniTaskCompletionSource<bool> connectCompletionSource;

    public UniTask Connect()
    {
        var builder = DbConnection
            .Builder()
            .WithUri(_serverUrl)
            .WithModuleName(_moduleName)
            .OnConnect(OnConnect)
            .OnConnectError(OnConnectError)
            .OnDisconnect(OnDisconnect);

        if (AuthToken.Token != "")
        {
            builder = builder.WithToken(AuthToken.Token);
        }

        Connection = builder.Build();

        connectCompletionSource = new UniTaskCompletionSource<bool>();

        return connectCompletionSource.Task;
    }

    private void OnConnect(DbConnection _, Identity identity, string token)
    {
        Debug.Log("Connected to SpacetimeDB server.");
        AuthToken.SaveToken(token);

        Connection.SubscriptionBuilder().OnApplied(OnSubscriptionApplied).SubscribeToAllTables();
    }

    private void OnConnectError(Exception ex)
    {
        Debug.LogError($"Connection error: {ex}");

        connectCompletionSource.TrySetException(ex);
    }

    private void OnDisconnect(DbConnection _, Exception ex)
    {
        Debug.Log("Disconnected.");

        if (ex != null)
        {
            Debug.LogException(ex);
        }
    }

    private void OnSubscriptionApplied(SubscriptionEventContext ctx)
    {
        Debug.Log("Subscription applied");

        connectCompletionSource.TrySetResult(true);
    }
}
