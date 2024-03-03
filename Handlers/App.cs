using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override Task Handle(AppRequest req, string path, string pathPrefix)
    {
        Presets.CreatePage(req, "Server", out var page, out var e);
        Presets.Navigation(req, page);
        page.Head.Add($"<link rel=\"manifest\" href=\"{pathPrefix}/manifest.json\" />");
        page.Favicon = pathPrefix + "/icon.ico";

        if (!req.IsAdmin())
        {
            req.Status = 403;
            return Task.CompletedTask;
        }

        switch (Parsers.GetFirstSegment(path, out string rest))
        {
            case "wrapper":
                HandleWrapper(req, rest, pathPrefix);
                break;
            case "ssh":
                HandleSSH(req, rest, pathPrefix);
                break;
            case "mail":
                HandleMail(req, rest, pathPrefix);
                break;
            case "backups":
                HandleBackups(req, rest, pathPrefix);
                break;
            default:
                HandleOther(req, path, pathPrefix);
                break;
        }
        return Task.CompletedTask;
    }

    private void AddStatusAndSidebar(AppRequest req, string pathPrefix, bool addStatus = true)
    {
        Page page = (Page)(req.Page ?? throw new Exception("No page was set."));

        if (addStatus)
        {
            page.Scripts.Add(new Script($"{pathPrefix}/status.js"));
            page.Elements.Add(new ContainerElement(null, "Loading status...", id: "status"));
        }

        string pathHome = pathPrefix == "" ? "/" : pathPrefix;
        if (pathHome == req.Path)
            return;

        page.Sidebar.Add(new ButtonElement("Menu:", null, pathHome));
        if (EnableWrapper)
            page.Sidebar.Add(new ButtonElement(null, "Wrapper", $"{pathPrefix}/wrapper"));
        if (EnableSSH)
            page.Sidebar.Add(new ButtonElement(null, "SSH", $"{pathPrefix}/ssh"));
        if (EnableBackups)
            page.Sidebar.Add(new ButtonElement(null, "Backups", $"{pathPrefix}/backups"));
        if (EnableMail)
            page.Sidebar.Add(new ButtonElement(null, "Mail", $"{pathPrefix}/mail"));

        foreach (IPageElement element in page.Sidebar)
            if (element is ButtonElement button && button.Title == null && req.Path.StartsWith(button.Link))
                button.Class = "green";
    }

    private static string DateTimeString(DateTime dt)
        => $"{dt.DayOfWeek}, {dt.Year}/{dt.Month}/{dt.Day}, {dt.ToShortTimeString()}";
}