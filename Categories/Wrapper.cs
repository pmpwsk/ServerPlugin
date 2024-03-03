using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    private void HandleWrapper(AppRequest req, string path, string pathPrefix)
    {
        if (!EnableWrapper)
        {
            req.Status = 403;
            return;
        }

        req.Init(out var page, out var e);
        switch (path)
        {
            case "":
                page.Title = "Wrapper";
                page.Scripts.Add(new Script(pathPrefix + "/wrapper.js"));
                e.Add(new LargeContainerElement("Wrapper"));
                AddStatusAndSidebar(req, pathPrefix);
                page.AddError();
                e.Add(new ButtonElement("Log", null, $"{pathPrefix}/wrapper/log"));
                e.Add(new ContainerElement("Update", new FileSelector("update-file")) { Button = new ButtonJS("Start", "Update()", "green", id: "updateButton") });
                e.Add(new ButtonElementJS("Revert to backed up version", null, "Revert()"));
                e.Add(new ButtonElementJS("Restart program", null, "Restart()"));
                e.Add(new ButtonElement("Stop program", null, $"{pathPrefix}/wrapper/stop"));
                e.Add(new ButtonElement("Reload Wrapper config", null, $"{pathPrefix}/wrapper/reload-config"));
                break;
            case "/log":
                page.Title = "Log";
                AddStatusAndSidebar(req, pathPrefix, false);
                bool wide = req.Query.TryGet("wide") == "true";
                page.Scripts.Add(new Script(pathPrefix + "/log.js"));
                if (wide)
                    page.Styles.Add(new CustomStyle("div.sidebar { display: none; } div.content { width: 100% !important; flex: 0 !important; } div.full { width: auto !important; display: block !important; margin: 0 0.6rem; !important }"));
                List<IContent> contents = [];
                if (File.Exists("../Wrapper.log"))
                    foreach (string line in File.ReadAllLines("../Wrapper.log"))
                        contents.Add(new Paragraph(line.HtmlSafe()));
                else contents.Add(new Paragraph("../Wrapper.log not found!"));
                List<IButton> buttons =
                [
                    new Button("Raw", $"/api{pathPrefix}/wrapper/log", newTab: true),
                    wide ? new Button("Normal", $"{pathPrefix}/wrapper/log") : new Button("Wide", $"{pathPrefix}/wrapper/log?wide=true")
                ];
                if (EnableWrapperLogClearing)
                    buttons.Add(new ButtonJS("Clear", "Clear()", "red"));
                e.Add(new LargeContainerElement("Log", contents) { Buttons = buttons });
                break;
            case "/stop":
                page.Title = "Stop program";
                e.Add(new LargeContainerElement("Stop program"));
                AddStatusAndSidebar(req, pathPrefix, false);
                e.Add(new ContainerElement("Success!", "The program is shutting down.", "green"));
                Server.Exit(true);
                break;
            case "/reload-config":
                page.Title = "Reload Wrapper config";
                e.Add(new LargeContainerElement("Reload Wrapper config"));
                AddStatusAndSidebar(req, pathPrefix);
                e.Add(new ContainerElement("Success!", "The Wrapper has been told to reload the configuration, the new one will be used on the next restart of the program.", "green"));
                Console.WriteLine("wrapper reload-config");
                break;
            default:
                req.Status = 404;
                break;
        }
    }

    private async Task HandleWrapper(ApiRequest req, string path, string pathPrefix)
    {
        if (!EnableWrapper)
        {
            req.Status = 403;
            return;
        }

        switch (path)
        {
            case "/log":
                await req.SendFile("../Wrapper.log");
                break;
            case "/clear-log":
                if (EnableWrapperLogClearing)
                    Console.WriteLine("wrapper log-clear");
                else req.Status = 403;
                break;
            case "/revert":
                Console.WriteLine("wrapper rollback");
                Server.Exit(false);
                break;
            case "/restart":
                Server.Exit(false);
                break;
            default:
                req.Status = 404;
                break;
        }
    }
}