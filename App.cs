using uwap.WebFramework.Elements;
using System.Web;

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
        switch (path)
        {
            case "":
                page.Title = "Server";
                page.Scripts.Add(new Script(pathPrefix + "/update.js"));
                if (File.Exists("/var/run/reboot-required"))
                    e.Add(new HeadingElement("Reboot required!", "", "red"));
                if (Server.WorkerWorking)
                    e.Add(new HeadingElement("Worker is working!", "", "red"));
                else if (Server.WorkerNextTick == DateTime.MaxValue)
                    e.Add(new ContainerElement("Worker is disabled", ""));
                else
                {
                    TimeSpan timeLeft = Server.WorkerNextTick - DateTime.UtcNow;
                    int seconds = (int)Math.Round(timeLeft.TotalSeconds, 0, MidpointRounding.AwayFromZero);
                    string text = $"{seconds / 60}min {seconds % 60}s";
                    if (timeLeft < TimeSpan.FromMinutes(2))
                        e.Add(new HeadingElement("Worker scheduled soon!", text + " left", "red"));
                    else e.Add(new ContainerElement("Worker scheduled in " + text, ""));
                }
                e.Add(new ButtonElement("Log (raw)", null, $"/api{pathPrefix}/log", newTab: true));
                e.Add(new ButtonElement("Log (UI)", null, $"{pathPrefix}/log"));
                if (AllowLogClearing)
                    e.Add(new ButtonElement("Clear log", null, $"/api{pathPrefix}/clear-log", newTab: true));
                e.Add(new ButtonElement("SSH management", null, $"{pathPrefix}/ssh"));
                e.Add(new ContainerElement("Update file", new FileSelector("update-file")));
                page.AddError();
                e.Add(new ButtonElementJS("Start update", null, "Update()", id: "updateButton"));
                if (AllowBackupManagement)
                    e.Add(new ButtonElement("Backups", null, $"{pathPrefix}/backups"));
                e.Add(new ButtonElement("Call the worker", null, $"/api{pathPrefix}/work", newTab: true));
                e.Add(new ButtonElement("Rollback version", null, $"/api{pathPrefix}/rollback", newTab: true));
                e.Add(new ButtonElement("Stop program", null, $"/api{pathPrefix}/stop", newTab: true));
                e.Add(new ButtonElement("Restart program", null, $"/api{pathPrefix}/restart", newTab: true));
                e.Add(new ButtonElement("Show my IP address", null, $"/api{pathPrefix}/ip", newTab: true));
                e.Add(new ButtonElement("Send an email", null, $"{pathPrefix}/send-mail"));
                e.Add(new ButtonElement("Restart mail server", null, $"/api{pathPrefix}/restart-mail", newTab: true));
                e.Add(new ButtonElement("Reload Wrapper config", null, $"/api{pathPrefix}/reload-config", newTab: true));
                break;
            case "/log":
                page.Title = "Log";
                bool wide = req.Query.TryGet("wide") == "true";
                page.Scripts.Add(new Script(pathPrefix + "/log.js"));
                if (wide)
                    page.Styles.Add(new CustomStyle("div.sidebar { display: none; } div.content { width: 100% !important; flex: 0 !important; } div.full { width: auto !important; display: block !important; margin: 0 0.6rem; !important }"));
                List<IContent> contents = [];
                if (File.Exists("../Wrapper.log"))
                    foreach (string line in File.ReadAllLines("../Wrapper.log"))
                        contents.Add(new Paragraph(line.HtmlSafe()));
                else contents.Add(new Paragraph("../Wrapper.log not found!"));
                List<IButton> buttons = [wide ? new Button("Normal", $"{pathPrefix}/log") : new Button("Wide", $"{pathPrefix}/log?wide=true")];
                if (AllowLogClearing)
                    buttons.Add(new ButtonJS("Clear", "Clear()", "red"));
                e.Add(new LargeContainerElement("Server log", contents) { Buttons = buttons });
                break;
            case "/ssh":
                {
                    page.Title = "SSH";
                    if (req.Query.TryGetValue("user", out string? username))
                    {
                        if (username.Contains(".."))
                        {
                            req.Status = 400;
                            break;
                        }
                        string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                        bool enabled = File.Exists(file);
                        if ((!enabled) && (!File.Exists(file + ".disabled")))
                        {
                            req.Status = 404;
                            break;
                        }
                        e.Add(new HeadingElement("SSH: " + username));
                        page.Scripts.Add(new Script(pathPrefix + "/ssh-user.js"));
                        if (enabled)
                        {
                            e.Add(new ButtonElementJS("Disable SSH", null, "Disable()"));
                            e.Add(new ContainerElement("Add public key", new TextBox("Enter your public key...", null, "pk", TextBoxRole.NoSpellcheck, "Add()")) { Button = new ButtonJS("Add", "Add()", "green") });
                            page.AddError();
                            foreach (string line in File.ReadAllLines(file))
                                e.Add(new ContainerElement(line.Remove(0, line.LastIndexOf(' ')), line) { Button = new ButtonJS("Delete", $"Delete('{HttpUtility.UrlEncode(line)}')", "red") });
                        }
                        else
                        {
                            page.AddError();
                            e.Add(new ButtonElementJS("Enable SSH", null, "Enable()"));
                        }
                    }
                    else
                    {
                        page.Scripts.Add(new Script(pathPrefix + "/ssh-menu.js"));
                        e.Add(new ContainerElement(null, "You are using IPv" + ((req.Context.IP() ?? ".").Contains('.') ? "4" : "6")));
                        try
                        {
                            var ips = AllowedSshIps();
                            if (ips.Contains(req.Context.IP()))
                                e.Add(new ButtonElementJS("Block SSH", null, "Block()", "red"));
                            else if (ips.Any())
                                e.Add(new ButtonElementJS("Change SSH rule", null, "Change()", "red"));
                            else e.Add(new ButtonElementJS("Allow SSH", null, "Allow()", "green"));
                        }
                        catch
                        {
                            e.Add(new ContainerElement(null, "ufw not available!", "red"));
                        }
                        try
                        {
                            Dictionary<string, bool> users = [];
                            foreach (var user in new DirectoryInfo("/home").GetDirectories("*", SearchOption.TopDirectoryOnly).Select(x => x.Name))
                            {
                                if (File.Exists($"/home/{user}/.ssh/authorized_keys"))
                                    users[user] = true;
                                else if (File.Exists($"/home/{user}/.ssh/authorized_keys.disabled"))
                                    users[user] = false;
                                if (File.Exists("/root/.ssh/authorized_keys"))
                                    users["root"] = true;
                                else if (File.Exists("/root/.ssh/authorized_keys.disabled"))
                                    users["root"] = false;
                            }
                            foreach (var user in users)
                                e.Add(new ContainerElement(user.Key, "") { Buttons =
                                [
                                    new Button("More", $"{pathPrefix}/ssh?user=" + user.Key),
                                    user.Value ? new ButtonJS("Disable", $"Disable('{user.Key}')", "red") : new ButtonJS("Enable", $"Enable('{user.Key}')", "green")
                                ] });
                            if (users.Count == 0)
                                e.Add(new ContainerElement("No items!", "", "red"));
                        }
                        catch
                        {
                            e.Add(new ContainerElement(null, "SSH users not available!", "red"));
                        }
                    }
                }
                break;
            case "/send-mail":
                page.Title = "Send an email";
                page.Scripts.Add(new Script(pathPrefix + "/send-mail.js"));
                e.Add(new ContainerElement(null,
                [
                    new Heading("To:"),
                    new TextBox("Recipient...", null, "to", TextBoxRole.Email),
                    new Heading("From:"),
                    new TextBox("Sender...", null, "from", TextBoxRole.Email),
                    new Heading("Subject:"),
                    new TextBox("Subject...", null, "subject", TextBoxRole.Email),
                    new Heading("Text:"),
                    new TextArea("Body...", null, "text", 10)
                ]));
                e.Add(new ButtonElementJS("Send", null, "Send()"));
                page.AddError();
                break;
            case "/backups":
                {
                    if (!AllowBackupManagement)
                    {
                        req.Status = 403;
                        break;
                    }
                    page.Title = "Backups";
                    page.Scripts.Add(new Script(pathPrefix + "/backup.js"));
                    e.Add(new LargeContainerElement("A backup is being created!", "", "red", Server.BackupRunning ? null : "display: none", "backup-running"));
                    if (Server.RestoreRunning)
                        e.Add(new LargeContainerElement("A backup is being restored!", "", "red"));
                    e.Add(new LargeContainerElement("Backups"));
                    page.AddError();
                    e.Add(new ContainerElement(null, "New:") { Buttons =
                    [
                        new ButtonJS("Normal", "BackupNow('false')", "green"),
                        new ButtonJS("Fresh", "BackupNow('true')", "green")
                    ] });
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
                        e.Add(new ContainerElement(DateTimeString(id) + " UTC", [ id.Ticks.ToString(), type ]) { Buttons =
                        [
                            new Button("Download", $"/dl{pathPrefix}/backup?id={id.Ticks}", newTab: true),
                            new ButtonJS("Restore", $"Restore('{id.Ticks}')", "red", id: $"restore-{id.Ticks}")
                        ] });
                    }
                }
                break;
            default:
                req.Status = 404;
                break;
        }

        return Task.CompletedTask;
    }

    private static string DateTimeString(DateTime dt)
        => $"{dt.DayOfWeek}, {dt.Year}/{dt.Month}/{dt.Day}, {dt.ToShortTimeString()}";
}