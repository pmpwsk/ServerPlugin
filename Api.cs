using MimeKit;
using uwap.WebFramework.Mail;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override async Task Handle(ApiRequest req, string path, string pathPrefix)
    {
        if (!req.IsAdmin())
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
                if (AllowLogClearing)
                    Console.WriteLine("wrapper log-clear");
                else req.Status = 403;
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
                    string ip = AllowSsh(req);
                    Console.WriteLine($"{req.User.Username} ({req.User.Id}) allowed SSH access to {ip}.");
                } break;
            case "/ssh/change":
                {
                    var ips = DeleteSshRules();
                    string ip = AllowSsh(req);
                    if (ips.Any())
                        Console.WriteLine($"{req.User.Username} ({req.User.Id}) changed SSH access from {Parsers.EnumerationText(ips)} to {ip}.");
                    else Console.WriteLine($"{req.User.Username} ({req.User.Id}) allowed SSH access for {ip}.");
                } break;
            case "/ssh/block":
                {
                    var ips = DeleteSshRules();
                    if (ips.Any())
                        Console.WriteLine($"{req.User.Username} ({req.User.Id}) removed SSH access for {Parsers.EnumerationText(ips)}.");
                } break;
            case "/work":
                Server.Work();
                break;
            case "/ip":
                await req.Write(req.Context.IP() ?? "unknown");
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
                    if (req.Query.TryGetValue("to", out var to) && req.Query.TryGetValue("from", out var from)
                        && req.Query.TryGetValue("subject", out var subject) && req.Query.TryGetValue("text", out var text))
                    {
                        var result = MailManager.Out.Send(new MailGen(new(from, from),
                            to.Split(' ', ',', ';').Where(x => x != "").Select(x => new MailboxAddress(x, x)),
                            subject, null, text));
                        if (result.FromSelf != null)
                            await req.WriteLine($"Self: {result.FromSelf.ResultType}");
                        if (result.FromBackup != null)
                            await req.WriteLine($"Backup: {result.FromBackup.ResultType}");
                    }
                    else req.Status = 400;
                }
                break;
            case "/ssh/enable":
                {
                    if (!req.Query.TryGetValue("user", out var username))
                        req.Status = 400;
                    else if (username.Contains(".."))
                        req.Status = 400;
                    else
                    {
                        string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                        if (File.Exists(file + ".disabled"))
                            File.Move(file + ".disabled", file, true);
                        else File.WriteAllText(file, "");
                        await req.Write("ok");
                        Console.WriteLine($"{req.User.Username} ({req.User.Id}) enabled SSH for {username}.");
                    }
                }
                break;
            case "/ssh/disable":
                {
                    if (!req.Query.TryGetValue("user", out var username))
                        req.Status = 400;
                    else if (username.Contains(".."))
                        req.Status = 400;
                    else
                    {
                        string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                        if (File.Exists(file))
                            File.Move(file, file + ".disabled", true);
                        await req.Write("ok");
                        Console.WriteLine($"{req.User.Username} ({req.User.Id}) disabled SSH for {username}.");
                    }
                }
                break;
            case "/ssh/add":
                {
                    if (!req.Query.TryGetValue("user", out var username))
                        req.Status = 400;
                    else if (username.Contains(".."))
                        req.Status = 400;
                    else if (!req.Query.TryGetValue("pk", out var pk))
                        req.Status = 400;
                    else
                    {
                        string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                        File.AppendAllLines(file, [pk]);
                        await req.Write("ok");
                        Console.WriteLine($"{req.User.Username} ({req.User.Id}) added a SSH key for {username}.");
                    }
                }
                break;
            case "/ssh/delete":
                {
                    if (!req.Query.TryGetValue("user", out var username))
                        req.Status = 400;
                    else if (username.Contains("..")) req.Status = 400;
                    else if (!req.Query.TryGetValue("pk", out var pk))
                        req.Status = 400;
                    else
                    {
                        string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                        File.WriteAllLines(file, File.ReadAllLines(file).Where(x => x != pk));
                        await req.Write("ok");
                        Console.WriteLine($"{req.User.Username} ({req.User.Id}) removed a SSH key for {username}.");
                    }
                }
                break;
            case "/backups/list":
                if (!AllowBackupManagement)
                {
                    req.Status = 403;
                    break;
                }
                await req.Write(string.Join('\n', new DirectoryInfo(Server.Config.Backup.Directory).GetDirectories("*", SearchOption.TopDirectoryOnly).Where(d => long.TryParse(d.Name, out _)).Select(d => d.Name)));
                break;
            case "/backups/new":
                if (!AllowBackupManagement)
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
            case "/backups/restore":
                if (!AllowBackupManagement)
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