using System.Web;
using uwap.WebFramework.Elements;
using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin
{
    private async Task<IResponse> HandleSSH(Request req)
    {
        switch (req.Path)
        {
            // MANAGE SSH
            case "/ssh":
            { CreatePage(req, "SSH", out var page, out var e, true);
                if (!EnableSSH)
                    return StatusResponse.Forbidden;
                page.Navigation.Add(new Button("Back", ".", "right"));
                e.Add(new HeadingElement("SSH", "You are using IPv" + ((req.ClientAddress ?? ".").Contains('.') ? "4" : "6") + "."));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("ssh.js"));
                try
                {
                    var ips = AllowedSshIps();
                    var ip = req.ClientAddress;
                    if (ip != null && ips.Contains(ip))
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
                        e.Add(new ContainerElement(user.Key, "") { Buttons = [
                            new Button("More", $"ssh/user?username=" + user.Key),
                            user.Value ? new ButtonJS("Disable", $"Disable('{user.Key}')", "red") : new ButtonJS("Enable", $"Enable('{user.Key}')", "green")
                        ]});
                    if (users.Count == 0)
                        e.Add(new ContainerElement("No items!", "", "red"));
                }
                catch
                {
                    e.Add(new ContainerElement(null, "Users not available!", "red"));
                }
                return new LegacyPageResponse(page, req);
            }

            case "/ssh/allow":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableSSH)
                    return StatusResponse.Forbidden;
                string ip = AllowSsh(req);
                Console.WriteLine($"{req.User.Username} ({req.User.Id}) allowed SSH access to {ip}.");
                return StatusResponse.Success;
            }

            case "/ssh/change":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableSSH)
                    return StatusResponse.Forbidden;
                var ips = DeleteSshRules();
                string ip = AllowSsh(req);
                Console.WriteLine(ips.Count != 0
                    ? $"{req.User.Username} ({req.User.Id}) changed SSH access from {ips.EnumerationText()} to {ip}."
                    : $"{req.User.Username} ({req.User.Id}) allowed SSH access for {ip}.");
                return StatusResponse.Success;
            }

            case "/ssh/block":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableSSH)
                    return StatusResponse.Forbidden;
                var ips = DeleteSshRules();
                if (ips.Count != 0)
                    Console.WriteLine($"{req.User.Username} ({req.User.Id}) removed SSH access for {ips.EnumerationText()}.");
                return StatusResponse.Success;
            }




            // MANAGE SSH USER
            case "/ssh/user":
            { CreatePage(req, "SSH", out var page, out var e, true);
                if (!EnableSSH)
                    return StatusResponse.Forbidden;
                page.Navigation.Add(new Button("Back", "../ssh", "right"));
                if ((!req.Query.TryGetValue("username", out string? username)) || username.Contains(".."))
                    return StatusResponse.BadRequest;
                string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                bool enabled = File.Exists(file);
                if ((!enabled) && (!File.Exists(file + ".disabled")))
                    return StatusResponse.NotFound;
                page.Title = "SSH: " + username;
                e.Add(new HeadingElement("SSH: " + username));
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("user.js"));
                if (enabled)
                {
                    e.Add(new ButtonElementJS("Disable SSH", null, "Disable()"));
                    e.Add(new ContainerElement("Add public key", new TextBox("Enter your public key...", null, "key", TextBoxRole.NoSpellcheck, "Add()")) { Button = new ButtonJS("Add", "Add()", "green") });
                    page.AddError();
                    foreach (string line in await File.ReadAllLinesAsync(file))
                        e.Add(new ContainerElement(line.Remove(0, line.LastIndexOf(' ')), line) { Button = new ButtonJS("Delete", $"Delete('{HttpUtility.UrlEncode(line)}')", "red") });
                }
                else
                {
                    page.AddError();
                    e.Add(new ButtonElementJS("Enable SSH", null, "Enable()"));
                }
                return new LegacyPageResponse(page, req);
            }
            
            case "/ssh/user/enable":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableSSH)
                    return StatusResponse.Forbidden;
                if ((!req.Query.TryGetValue("username", out var username)) || username.Contains(".."))
                    return StatusResponse.BadRequest;
                string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                if (File.Exists(file + ".disabled"))
                    File.Move(file + ".disabled", file, true);
                else await File.WriteAllTextAsync(file, "");
                Console.WriteLine($"{req.User.Username} ({req.User.Id}) enabled SSH for {username}.");
                return StatusResponse.Success;
            }

            case "/ssh/user/disable":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableSSH)
                    return StatusResponse.Forbidden;
                if ((!req.Query.TryGetValue("username", out var username)) || username.Contains(".."))
                    return StatusResponse.BadRequest;
                string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                if (File.Exists(file))
                    File.Move(file, file + ".disabled", true);
                Console.WriteLine($"{req.User.Username} ({req.User.Id}) disabled SSH for {username}.");
                return StatusResponse.Success;
            }

            case "/ssh/user/add":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableSSH)
                    return StatusResponse.Forbidden;
                if ((!req.Query.TryGetValue("username", out var username)) || username.Contains("..") || !req.Query.TryGetValue("key", out var key))
                    return StatusResponse.BadRequest;
                string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                await File.AppendAllLinesAsync(file, [key]);
                Console.WriteLine($"{req.User.Username} ({req.User.Id}) added a SSH key for {username}.");
                return StatusResponse.Success;
            }

            case "/ssh/user/delete":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableSSH)
                    return StatusResponse.Forbidden;
                if ((!req.Query.TryGetValue("username", out var username)) || username.Contains("..") || !req.Query.TryGetValue("key", out var key))
                    return StatusResponse.BadRequest;
                string file = (username == "root" ? "/root" : $"/home/{username}") + "/.ssh/authorized_keys";
                await File.WriteAllLinesAsync(file, (await File.ReadAllLinesAsync(file)).Where(x => x != key));
                Console.WriteLine($"{req.User.Username} ({req.User.Id}) removed a SSH key for {username}.");
                return StatusResponse.Success;
            }




            // 404
            default:
                return StatusResponse.NotFound;
        }
    }
}