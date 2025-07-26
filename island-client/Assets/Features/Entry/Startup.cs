using Reflex.Core;
using UnityEditor.MemoryProfiler;
using UnityEngine;

public class Startup : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.AddSingleton(typeof(ConnectionService));
        builder.AddSingleton(a => a.Resolve<ConnectionService>().Connection);
        builder.AddSingleton(typeof(EntityService));
    }
}
