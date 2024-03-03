using System.Diagnostics;
using System.Web;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    private void HandleSSH(AppRequest req, string path, string pathPrefix)
    {
        if (!EnableSSH)
        {
            req.Status = 403;
            return;
        }

        req.Init(out var page, out var e);
        switch (path)
        {
            case "":
                {
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
                        page.Title = "SSH: " + username;
                        e.Add(new HeadingElement("SSH: " + username));
                        AddStatusAndSidebar(req, pathPrefix);
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
                        page.Title = "SSH";
                        e.Add(new LargeContainerElement("SSH", "You are using IPv" + ((req.Context.IP() ?? ".").Contains('.') ? "4" : "6") + "."));
                        AddStatusAndSidebar(req, pathPrefix);
                        page.Scripts.Add(new Script(pathPrefix + "/ssh-menu.js"));
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
                            e.Add(new ContainerElement(null, "UFW not available!", "red"));
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
                                e.Add(new ContainerElement(user.Key, "")
                                {
                                    Buttons =
                                [
                                    new Button("More", $"{pathPrefix}/ssh?user=" + user.Key),
                                    user.Value ? new ButtonJS("Disable", $"Disable('{user.Key}')", "red") : new ButtonJS("Enable", $"Enable('{user.Key}')", "green")
                                ]
                                });
                            if (users.Count == 0)
                                e.Add(new ContainerElement("No items!", "", "red"));
                        }
                        catch
                        {
                            e.Add(new ContainerElement(null, "Users not available!", "red"));
                        }
                    }
                } break;
            default:
                req.Status = 404;
                break;
        }
    }

    private async Task HandleSSH(ApiRequest req, string path, string pathPrefix)
    {
        if (!EnableSSH)
        {
            req.Status = 403;
            return;
        }

        switch (path)
        {
            case "/allow":
                {
                    string ip = AllowSsh(req);
                    Console.WriteLine($"{req.User.Username} ({req.User.Id}) allowed SSH access to {ip}.");
                }
                break;
            case "/change":
                {
                    var ips = DeleteSshRules();
                    string ip = AllowSsh(req);
                    if (ips.Any())
                        Console.WriteLine($"{req.User.Username} ({req.User.Id}) changed SSH access from {Parsers.EnumerationText(ips)} to {ip}.");
                    else Console.WriteLine($"{req.User.Username} ({req.User.Id}) allowed SSH access for {ip}.");
                }
                break;
            case "/block":
                {
                    var ips = DeleteSshRules();
                    if (ips.Any())
                        Console.WriteLine($"{req.User.Username} ({req.User.Id}) removed SSH access for {Parsers.EnumerationText(ips)}.");
                }
                break;
            case "/enable":
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
            case "/disable":
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
            case "/add":
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
            case "/delete":
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
            default:
                req.Status = 404;
                break;
        }
    }

    private static IEnumerable<string> AllowedSshIps()
    {
        Process process = new();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.FileName = "ufw";
        process.StartInfo.Arguments = $"status";
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        var rules = output.Split('\n').Where(x => x.StartsWith("22/tcp") && x.Contains("ALLOW"));
        var ips = rules.Select(x => x.TrimEnd(' ')).Select(x => x.Remove(0, x.LastIndexOf(' ') + 1));
        process.WaitForExit();
        return ips;
    }

    private static IEnumerable<string> DeleteSshRules()
    {
        var ips = AllowedSshIps();
        foreach (var ip in ips)
        {
            Process p = new();
            p.StartInfo.FileName = "ufw";
            p.StartInfo.Arguments = $"delete allow from {ip} to any proto tcp port 22";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            p.StandardOutput.ReadToEnd();
            p.WaitForExit();
        }
        return ips;
    }

    private static string AllowSsh(IRequest req)
    {
        var ip = req.Context.IP();
        if (ip == null)
            return "unknown";
        Process process = new();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.FileName = "ufw";
        process.StartInfo.Arguments = $"allow from {ip} to any proto tcp port 22";
        process.Start();
        process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return ip;
    }
}