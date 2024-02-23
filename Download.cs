using System.IO.Compression;

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
            case "/backup":
                if (!AllowBackupManagement)
                {
                    req.Status = 403;
                    break;
                }
                if ((!req.Query.TryGetValue("id", out var id)) || !long.TryParse(id, out _))
                {
                    req.Status = 400;
                    break;
                }
                string dir = $"{Server.Config.Backup.Directory}{id}";
                if (!Directory.Exists(dir))
                {
                    req.Status = 404;
                    break;
                }
                string zip = $"{dir}.zip";
                if (File.Exists(zip))
                    File.Delete(zip);
                ZipFile.CreateFromDirectory(dir, zip, CompressionLevel.Optimal, false);
                await req.SendFile(zip, $"backup-{id}.zip");
                File.Delete(zip);
                break;
            default:
                req.Status = 404;
                break;
        }
    }
}