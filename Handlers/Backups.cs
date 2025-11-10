using System.IO.Compression;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin
{
    private async Task HandleBackups(Request req)
    {
        switch (req.Path)
        {
            // MANAGE BACKUPS
            case "/backups":
            { CreatePage(req, "Backups", out var page, out var e, true);
                page.Navigation.Add(new Button("Back", ".", "right"));
                if (!EnableBackups)
                    throw new ForbiddenSignal();
                e.Add(new HeadingElement("Backups"));
                page.Scripts.Add(new Script("backups.js"));
                page.AddError();
                e.Add(new ContainerElement(null, "New:") { Buttons =
                [
                    new ButtonJS("Normal", "BackupNow('false')", "green"),
                    new ButtonJS("Fresh", "BackupNow('true')", "green")
                ]});
                
                if (!Directory.Exists(Server.Config.Backup.Directory))
                    break;
                
                SortedSet<DateTime> ids = [];
                foreach (var d in new DirectoryInfo(Server.Config.Backup.Directory).GetDirectories("*", SearchOption.TopDirectoryOnly))
                    if (long.TryParse(d.Name, out var id))
                        ids.Add(new DateTime(id));
                foreach (var id in ids.Reverse())
                {
                    string type;
                    try
                    {
                        type = File.ReadAllText($"{Server.Config.Backup.Directory}{id.Ticks}/BasedOn.txt") == "-" ? "Fresh" : "Based on previous";
                    }
                    catch
                    {
                        type = "Broken";
                    }
                    e.Add(new ContainerElement($"{id.DayOfWeek}, {id.Year}/{id.Month}/{id.Day}, {id.ToShortTimeString()} UTC", [id.Ticks.ToString(), type]) { Buttons =
                    [
                        new Button("Download", $"backups/download?id={id.Ticks}", newTab: true),
                        new ButtonJS("Restore", $"Restore('{id.Ticks}')", "red", id: $"restore-{id.Ticks}")
                    ]});
                }
            } break;

            case "/backups/new":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableBackups)
                    throw new ForbiddenSignal();
                if ((!req.Query.TryGetValue("fresh", out var freshString)) || !bool.TryParse(freshString, out bool fresh))
                    throw new BadRequestSignal();
                await Server.BackupNow(fresh);
            } break;

            case "/backups/restore":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableBackups)
                    throw new ForbiddenSignal();
                if ((!req.Query.TryGetValue("id", out var id)) || !long.TryParse(id, out _))
                    throw new BadRequestSignal();
                if (!Directory.Exists($"{Server.Config.Backup.Directory}{id}"))
                    throw new NotFoundSignal();
                await Server.Restore(id);
            } break;

            case "/backups/download":
            { req.ForceGET(); req.ForceAdmin(false);
                if (!EnableBackups)
                    throw new ForbiddenSignal();
                if ((!req.Query.TryGetValue("id", out var id)) || !long.TryParse(id, out _))
                    throw new BadRequestSignal();
                string dir = $"{Server.Config.Backup.Directory}{id}";
                if (!Directory.Exists(dir))
                    throw new NotFoundSignal();
                string zip = $"{dir}.zip";
                if (File.Exists(zip))
                    File.Delete(zip);
                ZipFile.CreateFromDirectory(dir, zip, CompressionLevel.Optimal, false);
                await req.WriteFileAsDownload(zip, $"backup-{id}.zip");
                File.Delete(zip);
            } break;




            // LIST BACKUP IDS (API)
            case "/backups/list":
            { req.ForceGET(); req.ForceAdmin(false);
                if (!EnableBackups)
                    throw new ForbiddenSignal();
                await req.Write(string.Join('\n', new DirectoryInfo(Server.Config.Backup.Directory).GetDirectories("*", SearchOption.TopDirectoryOnly).Where(d => long.TryParse(d.Name, out _)).Select(d => d.Name)));
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }
    }
}