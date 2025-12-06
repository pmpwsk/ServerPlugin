using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override Task<IResponse> HandleAsync(Request req)
        => Parsers.GetFirstSegment(req.Path, out _) switch
        {
            "wrapper" => HandleWrapper(req),
            "ssh" => HandleSSH(req),
            "mail" => HandleMail(req),
            "backups" => HandleBackups(req),
            _ => Task.FromResult(HandleOther(req))
        };
}