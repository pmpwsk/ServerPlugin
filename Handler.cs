namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override async Task Handle(Request req)
    {
        switch (Parsers.GetFirstSegment(req.Path, out _))
        {
            case "wrapper":
                await Wrapper(req);
                break;
            case "ssh":
                await SSH(req);
                break;
            case "mail":
                await Mail(req);
                break;
            case "backups":
                await Backups(req);
                break;
            default:
                await Other(req);
                break;
        }
    }
}