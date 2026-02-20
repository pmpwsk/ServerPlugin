using System.IO.Compression;
using uwap.WebFramework.Elements;
using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin
{
    private async Task<IResponse> HandleBackups(Request req)
    {
        switch (req.Path)
        {
            // MANAGE BACKUPS
            case "/backups":
            { CreatePage(req, "Backups", out var page, out var e, true);
                page.Navigation.Add(new Button("Back", ".", "right"));
                if (!EnableBackups)
                    return StatusResponse.Forbidden;
                e.Add(new HeadingElement("Backups"));
                page.Scripts.Add(new Script("backups.js"));
                page.AddError();
                e.Add(new ContainerElement(null, "New:") { Buttons =
                [
                    new ButtonJS("Normal", "BackupNow('false')", "green"),
                    new ButtonJS("Fresh", "BackupNow('true')", "green")
                ]});
                
                if (!Directory.Exists(Server.Config.Backup.Directory))
                    return new LegacyPageResponse(page, req);
                
                SortedSet<DateTime> ids = [];
                foreach (var d in new DirectoryInfo(Server.Config.Backup.Directory).GetDirectories("*", SearchOption.TopDirectoryOnly))
                    if (long.TryParse(d.Name, out var id))
                        ids.Add(new DateTime(id));
                foreach (var id in ids.Reverse())
                {
                    string type;
                    try
                    {
                        type = await File.ReadAllTextAsync($"{Server.Config.Backup.Directory}{id.Ticks}/BasedOn.txt") == "-" ? "Fresh" : "Based on previous";
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
                return new LegacyPageResponse(page, req);
            }

            case "/backups/new":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableBackups)
                    return StatusResponse.Forbidden;
                if ((!req.Query.TryGetValue("fresh", out var freshString)) || !bool.TryParse(freshString, out bool fresh))
                    return StatusResponse.BadRequest;
                await Server.BackupNow(fresh);
                return StatusResponse.Success;
            }

            case "/backups/restore":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableBackups)
                    return StatusResponse.Forbidden;
                if ((!req.Query.TryGetValue("id", out var id)) || !long.TryParse(id, out _))
                    return StatusResponse.BadRequest;
                if (!Directory.Exists($"{Server.Config.Backup.Directory}{id}"))
                    return StatusResponse.NotFound;
                await Server.Restore(id);
                return StatusResponse.Success;
            }

            case "/backups/download":
            { req.ForceGET(); req.ForceAdmin(false);
                if (!EnableBackups)
                    return StatusResponse.Forbidden;
                if ((!req.Query.TryGetValue("id", out var id)) || !long.TryParse(id, out _))
                    return StatusResponse.BadRequest;
                string dir = $"{Server.Config.Backup.Directory}{id}";
                if (!Directory.Exists(dir))
                    return StatusResponse.NotFound;
                string zip = $"{dir}.zip";
                if (File.Exists(zip))
                    File.Delete(zip);
                ZipFile.CreateFromDirectory(dir, zip, CompressionLevel.Optimal, false);
                return new FileDownloadResponse(zip, $"backup-{id}.zip", DateTime.UtcNow.Ticks.ToString()) { DeleteAfter = true };
            }




            // LIST BACKUP IDS (API)
            case "/backups/list":
            { req.ForceGET(); req.ForceAdmin(false);
                if (!EnableBackups)
                    return StatusResponse.Forbidden;
                return new TextResponse(string.Join('\n', new DirectoryInfo(Server.Config.Backup.Directory).GetDirectories("*", SearchOption.TopDirectoryOnly).Where(d => long.TryParse(d.Name, out _)).Select(d => d.Name)));
            }




            // 404
            default:
                return StatusResponse.NotFound;
        }
    }
}