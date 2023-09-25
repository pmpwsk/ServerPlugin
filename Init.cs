namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public ServerPlugin(bool allowLogClearing = true)
    {
        AllowLogClearing = allowLogClearing;
    }
}