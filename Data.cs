namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin
{
    public bool EnableBackups { get; set; } = true;

    public bool EnableSSH { get; set; } = true;

    public bool EnableMail { get; set; } = true;

    public bool EnableWrapper { get; set; } = true;

    public bool EnableWrapperLogClearing { get; set; } = true;

    public bool EnableMemory { get; set; } = true;
}