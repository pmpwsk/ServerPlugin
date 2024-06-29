using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    private async Task Other(Request req)
    {
        switch (req.Path)
        {
            // MAIN SERVER PAGE
            case "/":
            { CreatePage(req, "Server", out var page, out var e, true);
                page.Scripts.Add(new Script("menu.js"));
                var heading = new LargeContainerElement("Server") { Button = new ButtonJS("Work", "Work()") };
                e.Add(heading);
                page.AddError();
                if (EnableWrapper)
                {
                    heading.Buttons.Add(new Button("Log", "wrapper/log"));
                    e.Add(new ButtonElement("Wrapper", null, "wrapper"));
                }
                if (EnableSSH)
                    e.Add(new ButtonElement("SSH", null, "ssh"));
                if (EnableBackups)
                    e.Add(new ButtonElement("Backups", null, "backups"));
                if (EnableMail)
                    e.Add(new ButtonElement("Mail", null, "mail"));
            } break;

            case "/work":
            { req.ForcePOST(); req.ForceAdmin(false);
                Server.Work();
            } break;




            // STATUS EVENT
            case "/status-event":
            { req.ForceGET(); req.ForceAdmin(false);
                int rebootCheckCountdown = 0;
                while (!req.Context.RequestAborted.IsCancellationRequested)
                {
                    if (rebootCheckCountdown == 0)
                        rebootCheckCountdown = File.Exists("/var/run/reboot-required") ? -1 : 600;

                    if (Server.BackupRunning)
                        await req.EventMessage("<h2>A backup is being created!</h2>");
                    else if (Server.RestoreRunning)
                        await req.EventMessage("<h2>A backup is being restored!</h2>");
                    else if (Server.WorkerWorking)
                        await req.EventMessage("<h2>Worker is working!</h2>");
                    else if (Server.WorkerNextTick == DateTime.MaxValue)
                        if (rebootCheckCountdown == -1)
                            await req.EventMessage("<h2>Reboot required!</h2>");
                        else await req.EventMessage("<p>Worker is disabled</p>");
                    else
                    {
                        TimeSpan timeLeft = Server.WorkerNextTick - DateTime.UtcNow;
                        int seconds = (int)Math.Round(timeLeft.TotalSeconds, 0, MidpointRounding.AwayFromZero);
                        string text = $"{seconds / 60}min {seconds % 60}s";
                        if (timeLeft < TimeSpan.FromMinutes(2))
                            await req.EventMessage($"<h2>Worker scheduled soon!</h2><p>{text}</p>");
                        else if (rebootCheckCountdown == -1)
                            await req.EventMessage($"<h2>Reboot required!</h2><p>Worker scheduled in {text}</p>");
                        else await req.EventMessage($"<p>Worker scheduled in {text}</p>");
                    }

                    if (rebootCheckCountdown > 0)
                        rebootCheckCountdown--;
                    await Task.Delay(1000);
                }
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }
    }
}