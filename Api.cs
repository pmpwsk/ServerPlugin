using MimeKit;
using System.Diagnostics;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Mail;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override async Task Handle(ApiRequest request, string path, string pathPrefix)
    {
        if (request.User == null || !request.IsAdmin())
        {
            request.Status = 403;
            return;
        }
        switch (path)
        {
            case "/log":
                await request.SendFile("../Wrapper.log");
                break;
            case "/clear-log":
                Console.WriteLine("wrapper log-clear");
                break;
            case "/reload-config":
                Console.WriteLine("wrapper reload-config");
                break;
            case "/rollback":
                Console.WriteLine("wrapper rollback");
                Server.Exit(false);
                break;
            case "/ssh/allow":
                AllowSsh(request);
                break;
            case "/ssh/change":
                DeleteSshRules();
                AllowSsh(request);
                break;
            case "/ssh/block":
                DeleteSshRules();
                break;
            case "/work":
                Server.Work();
                break;
            case "/ip":
                await request.Write(request.Context.IP() ?? "unknown");
                break;
            case "/stop":
                Server.Exit(true);
                break;
            case "/restart":
                Server.Exit(false);
                break;
            case "/restart-mail":
                MailManager.In.Restart();
                break;
            case "/send-mail":
                if (!request.Query.ContainsKeys("to", "from", "subject", "text"))
                {
                    request.Status = 400;
                    break;
                }
                var result = MailManager.Out.Send(new MailGen(new(request.Query["from"], request.Query["from"]),
                    request.Query["to"].Split(' ', ',', ';').Where(x => x != "").Select(x => new MailboxAddress(x, x)),
                    request.Query["subject"], request.Query["text"], true));
                if (result.FromSelf != null)
                    await request.WriteLine($"Self: {result.FromSelf.ResultType}");
                if (result.FromBackup != null)
                    await request.WriteLine($"Backup: {result.FromBackup.ResultType}");
                break;
            case "/ssh/enable":
                {
                    if (!request.Query.TryGetValue("user", out var username)) request.Status = 400;
                    else if (username.Contains("..")) request.Status = 400;
                    else
                    {
                        string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                        if (File.Exists(file + ".disabled"))
                            File.Move(file + ".disabled", file, true);
                        else File.WriteAllText(file, "");
                        await request.Write("ok");
                        Console.WriteLine($"{request.User.Username} ({request.User.Id}) enabled SSH for {username}.");
                    }
                }
                break;
            case "/ssh/disable":
                {
                    if (!request.Query.TryGetValue("user", out var username)) request.Status = 400;
                    else if (username.Contains("..")) request.Status = 400;
                    else
                    {
                        string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                        if (File.Exists(file))
                            File.Move(file, file + ".disabled", true);
                        await request.Write("ok");
                        Console.WriteLine($"{request.User.Username} ({request.User.Id}) disabled SSH for {username}.");
                    }
                }
                break;
            case "/ssh/add":
                {
                    if (!request.Query.TryGetValue("user", out var username)) request.Status = 400;
                    else if (username.Contains("..")) request.Status = 400;
                    else if (!request.Query.TryGetValue("pk", out var pk)) request.Status = 400;
                    else
                    {
                        string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                        File.AppendAllLines(file, new[] { pk });
                        await request.Write("ok");
                        Console.WriteLine($"{request.User.Username} ({request.User.Id}) added a SSH key for {username}.");
                    }
                }
                break;
            case "/ssh/delete":
                {
                    if (!request.Query.TryGetValue("user", out var username)) request.Status = 400;
                    else if (username.Contains("..")) request.Status = 400;
                    else if (!request.Query.TryGetValue("pk", out var pk)) request.Status = 400;
                    else
                    {
                        string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                        File.WriteAllLines(file, File.ReadAllLines(file).Where(x => x != pk));
                        await request.Write("ok");
                        Console.WriteLine($"{request.User.Username} ({request.User.Id}) removed a SSH key for {username}.");
                    }
                }
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}