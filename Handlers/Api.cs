namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override async Task Handle(ApiRequest req, string path, string pathPrefix)
    {
        if (!req.IsAdmin())
        {
            req.Status = 403;
            return;
        }
        switch (Parsers.GetFirstSegment(path, out string rest))
        {
            case "wrapper":
                await HandleWrapper(req, rest, pathPrefix);
                break;
            case "ssh":
                await HandleSSH(req, rest, pathPrefix);
                break;
            case "mail":
                await HandleMail(req, rest, pathPrefix);
                break;
            case "backups":
                await HandleBackups(req, rest, pathPrefix);
                break;
            default:
                HandleOther(req, path, pathPrefix);
                break;
        }
        return;
    }
}