namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin(bool backups = true, bool ssh = true, bool mail = true, bool wrapper = true, bool wrapperLogClearing = true)
{
    public bool EnableBackups { get; } = backups;

    public bool EnableSSH { get; } = ssh;

    public bool EnableMail { get; } = mail;

    public bool EnableWrapper { get; } = wrapper;

    public bool EnableWrapperLogClearing { get; } = wrapperLogClearing;
}