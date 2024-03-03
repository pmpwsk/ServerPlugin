namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override async Task Handle(EventRequest req, string path, string pathPrefix)
    {
        if (!req.IsAdmin())
        {
            req.Status = 403;
            return;
        }
        switch (path)
        {
            case "/status":
                {
                    int rebootCheckCountdown = 0;
                    while (!req.Context.RequestAborted.IsCancellationRequested)
                    {
                        if (rebootCheckCountdown == 0)
                            rebootCheckCountdown = File.Exists("/var/run/reboot-required") ? -1 : 600;

                        if (Server.WorkerWorking)
                            await req.Send("<h2>Worker is working!</h2>");
                        else if (Server.BackupRunning)
                            await req.Send("<h2>A backup is being created!</h2>");
                        else if (Server.RestoreRunning)
                            await req.Send("<h2>A backup is being restored!</h2>");
                        else if (Server.WorkerNextTick == DateTime.MaxValue)
                        {
                            if (rebootCheckCountdown == -1)
                                await req.Send("<h2>Reboot required!</h2>");
                            else await req.Send("<p>Worker is disabled</p>");
                        }
                        else
                        {
                            TimeSpan timeLeft = Server.WorkerNextTick - DateTime.UtcNow;
                            int seconds = (int)Math.Round(timeLeft.TotalSeconds, 0, MidpointRounding.AwayFromZero);
                            string text = $"{seconds / 60}min {seconds % 60}s";
                            if (timeLeft < TimeSpan.FromMinutes(2))
                                await req.Send($"<h2>Worker scheduled soon!</h2><p>{text}</p>");
                            else if (rebootCheckCountdown == -1)
                                await req.Send("<h2>Reboot required!</h2>");
                            else await req.Send($"<p>Worker scheduled in {text}</p>");
                        }

                        if (rebootCheckCountdown > 0)
                            rebootCheckCountdown--;
                        await Task.Delay(1000);
                    }
                }
                break;
            case "/wrapper/log":
                {
                    if (!req.Query.TryGetValue("t", out long t))
                        return;

                    int countdown = 0;
                    while (!req.Context.RequestAborted.IsCancellationRequested)
                    {
                        if (countdown == 0)
                        {
                            if (File.GetLastWriteTimeUtc("../Wrapper.log").Ticks != t)
                            {
                                await Task.Delay(2000);
                                await req.Send("refresh");
                                await req.KeepAlive();
                            }
                            countdown = 20;
                        }

                        countdown--;
                        await req.Send(":keepalive");
                        await Task.Delay(30000);
                    }
                }
                break;
            default:
                req.Status = 404;
                break;
        }
    }
}