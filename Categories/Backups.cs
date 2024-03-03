using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    private void HandleBackups(AppRequest req, string path, string pathPrefix)
    {
        if (!EnableBackups)
        {
            req.Status = 403;
            return;
        }

        req.Init(out var page, out var e);
        switch (path)
        {
            case "":
                page.Title = "Backups";
                e.Add(new LargeContainerElement("Backups"));
                AddStatusAndSidebar(req, pathPrefix);
                page.Scripts.Add(new Script(pathPrefix + "/backup.js"));
                page.AddError();
                e.Add(new ContainerElement(null, "New:")
                {
                    Buttons =
                [
                    new ButtonJS("Normal", "BackupNow('false')", "green"),
                        new ButtonJS("Fresh", "BackupNow('true')", "green")
                ]
                });
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
                    e.Add(new ContainerElement(DateTimeString(id) + " UTC", [id.Ticks.ToString(), type])
                    {
                        Buttons =
                    [
                        new Button("Download", $"/dl{pathPrefix}/backup?id={id.Ticks}", newTab: true),
                            new ButtonJS("Restore", $"Restore('{id.Ticks}')", "red", id: $"restore-{id.Ticks}")
                    ]
                    });
                }
                break;
            default:
                req.Status = 404;
                break;
        }
    }

    private async Task HandleBackups(ApiRequest req, string path, string pathPrefix)
    {
        if (!EnableBackups)
        {
            req.Status = 403;
            return;
        }

        switch (path)
        {
            case "/list":
                if (!EnableBackups)
                {
                    req.Status = 403;
                    break;
                }
                await req.Write(string.Join('\n', new DirectoryInfo(Server.Config.Backup.Directory).GetDirectories("*", SearchOption.TopDirectoryOnly).Where(d => long.TryParse(d.Name, out _)).Select(d => d.Name)));
                break;
            case "/new":
                if (!EnableBackups)
                {
                    req.Status = 403;
                    break;
                }
                else
                {
                    if ((!req.Query.TryGetValue("fresh", out var freshString)) || !bool.TryParse(freshString, out bool fresh))
                    {
                        req.Status = 400;
                        break;
                    }
                    await Server.BackupNow(fresh);
                }
                break;
            case "/restore":
                if (!EnableBackups)
                {
                    req.Status = 403;
                    break;
                }
                else
                {
                    if ((!req.Query.TryGetValue("id", out var id)) || !long.TryParse(id, out _))
                    {
                        req.Status = 400;
                        break;
                    }
                    if (!Directory.Exists($"{Server.Config.Backup.Directory}{id}"))
                    {
                        req.Status = 404;
                        break;
                    }
                    await Server.Restore(id);
                }
                break;
            default:
                req.Status = 404;
                break;
        }
    }
}