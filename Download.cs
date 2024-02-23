namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override async Task Handle(DownloadRequest req, string path, string pathPrefix)
    {
        if (!req.IsAdmin())
        {
            req.Status = 403;
            return;
        }
        switch (path)
        {
            default:
                req.Status = 404;
                break;
        }
    }
}