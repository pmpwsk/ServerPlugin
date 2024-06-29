using System.Diagnostics;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    private void CreatePage(Request req, string title, out Page page, out List<IPageElement> e, bool addStatus)
    {
        req.ForceGET();
        req.CreatePage(title, out page, out e);
        page.Head.Add($"<link rel=\"manifest\" href=\"{req.PluginPathPrefix}/manifest.json\" />");
        page.Favicon = $"{req.PluginPathPrefix}/icon.ico";
        req.ForceAdmin();

        if (addStatus)
        {
            page.Scripts.Add(new Script($"{req.PluginPathPrefix}/status.js"));
            page.Elements.Add(new ContainerElement(null, "Loading status...", id: "status"));
        }

        if (req.Path == "/")
            return;

        page.Sidebar.Add(new ButtonElement("Menu:", null, req.PluginPathPrefix));
        if (EnableWrapper)
            page.Sidebar.Add(new ButtonElement(null, "Wrapper", $"{req.PluginPathPrefix}/wrapper"));
        if (EnableSSH)
            page.Sidebar.Add(new ButtonElement(null, "SSH", $"{req.PluginPathPrefix}/ssh"));
        if (EnableBackups)
            page.Sidebar.Add(new ButtonElement(null, "Backups", $"{req.PluginPathPrefix}/backups"));
        if (EnableMail)
            page.Sidebar.Add(new ButtonElement(null, "Mail", $"{req.PluginPathPrefix}/mail"));

        foreach (IPageElement element in page.Sidebar)
            if (element is ButtonElement button && button.Title == null && req.Context.ProtoHostPath().StartsWith(button.Link))
                button.Class = "green";
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

    private static string AllowSsh(Request req)
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