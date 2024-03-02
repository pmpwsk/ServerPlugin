using System.Diagnostics;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    private readonly bool AllowLogClearing;
    private readonly bool AllowBackupManagement;

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