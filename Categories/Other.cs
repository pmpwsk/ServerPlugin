using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    private void HandleOther(AppRequest req, string path, string pathPrefix)
    {
        req.Init(out var page, out var e);
        switch (path)
        {
            case "":
                page.Scripts.Add(new Script($"{pathPrefix}/menu.js", true));
                var heading = new LargeContainerElement("Server") { Button = new ButtonJS("Work", "Work()") };
                e.Add(heading);
                page.AddError();

                AddStatusAndSidebar(req, pathPrefix);

                if (EnableWrapper)
                {
                    heading.Buttons.Add(new Button("Log", $"{pathPrefix}/wrapper/log"));
                    e.Add(new ButtonElement("Wrapper", null, $"{pathPrefix}/wrapper"));
                }
                if (EnableSSH)
                    e.Add(new ButtonElement("SSH", null, $"{pathPrefix}/ssh"));
                if (EnableBackups)
                    e.Add(new ButtonElement("Backups", null, $"{pathPrefix}/backups"));
                if (EnableMail)
                    e.Add(new ButtonElement("Mail", null, $"{pathPrefix}/mail"));
                break;
            default:
                req.Status = 404;
                break;
        }
    }

    private static void HandleOther(ApiRequest req, string path, string pathPrefix)
    {
        switch (path)
        {
            case "/work":
                Server.Work();
                break;
            default:
                req.Status = 404;
                break;
        }
    }
}