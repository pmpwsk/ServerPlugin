namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override Task Handle(UploadRequest request, string path, string pathPrefix)
    {
        if (!request.IsAdmin())
        {
            request.Status = 403;
            return Task.CompletedTask;
        }
        switch (path)
        {
            case "/update":
                if (Directory.Exists("../Update")) Directory.Delete("../Update", true);
                Directory.CreateDirectory("../Update");
                foreach (var file in request.Files)
                    file.Download("../Update/" + file.FileName, 67108864);
                Server.Exit(false);
                break;
            default:
                request.Status = 404;
                break;
        }
        return Task.CompletedTask;
    }
}