namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin
{
    public override Task WorkAsync()
    {
        CollectMemory();
        return Task.CompletedTask;
    }
}