namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override async Task Handle(Request req)
    {
        switch (Parsers.GetFirstSegment(req.Path, out _))
        {
            case "wrapper":
                await HandleWrapper(req);
                break;
            case "ssh":
                await HandleSSH(req);
                break;
            case "mail":
                await HandleMail(req);
                break;
            case "backups":
                await HandleBackups(req);
                break;
            default:
                await HandleOther(req);
                break;
        }
    }
}