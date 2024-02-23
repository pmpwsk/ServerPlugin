using MimeKit;
using System.Diagnostics;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Mail;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override async Task Handle(ApiRequest request, string path, string pathPrefix)
    {
        if (!request.IsAdmin())
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
                if (AllowLogClearing)
                    Console.WriteLine("wrapper log-clear");
                else request.Status = 403;
                break;
            case "/reload-config":
                Console.WriteLine("wrapper reload-config");
                break;
            case "/rollback":
                Console.WriteLine("wrapper rollback");
                Server.Exit(false);
                break;
            case "/ssh/allow":
                {
                    string ip = AllowSsh(request);
                    Console.WriteLine($"{request.User.Username} ({request.User.Id}) allowed SSH access to {ip}.");
                } break;
            case "/ssh/change":
                {
                    var ips = DeleteSshRules();
                    string ip = AllowSsh(request);
                    if (ips.Any())
                        Console.WriteLine($"{request.User.Username} ({request.User.Id}) changed SSH access from {Parsers.EnumerationText(ips)} to {ip}.");
                    else Console.WriteLine($"{request.User.Username} ({request.User.Id}) allowed SSH access for {ip}.");
                } break;
            case "/ssh/block":
                {
                    var ips = DeleteSshRules();
                    if (ips.Any())
                        Console.WriteLine($"{request.User.Username} ({request.User.Id}) removed SSH access for {Parsers.EnumerationText(ips)}.");
                } break;
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
                {
                    if (request.Query.TryGetValue("to", out var to) && request.Query.TryGetValue("from", out var from)
                        && request.Query.TryGetValue("subject", out var subject) && request.Query.TryGetValue("text", out var text))
                    {
                        var result = MailManager.Out.Send(new MailGen(new(from, from),
                            to.Split(' ', ',', ';').Where(x => x != "").Select(x => new MailboxAddress(x, x)),
                            subject, null, text));
                        if (result.FromSelf != null)
                            await request.WriteLine($"Self: {result.FromSelf.ResultType}");
                        if (result.FromBackup != null)
                            await request.WriteLine($"Backup: {result.FromBackup.ResultType}");
                    }
                    else request.Status = 400;
                }
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
            case "/backups/restore":
                if (!AllowBackupManagement)
                {
                    request.Status = 403;
                    break;
                }
                else
                {
                    if ((!request.Query.TryGetValue("id", out var id)) || !long.TryParse(id, out _))
                    {
                        request.Status = 400;
                        break;
                    }
                    if (!Directory.Exists($"{Server.Config.Backup.Directory}{id}"))
                    {
                        request.Status = 404;
                        break;
                    }
                    await Server.Restore(id);
                }
                break;
            default:
                request.Status = 404;
                break;
        }
    }
}