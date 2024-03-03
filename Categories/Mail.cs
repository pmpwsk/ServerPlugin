using MimeKit;
using uwap.WebFramework.Elements;
using uwap.WebFramework.Mail;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    private void HandleMail(AppRequest req, string path, string pathPrefix)
    {
        if (!EnableMail)
        {
            req.Status = 403;
            return;
        }

        req.Init(out var page, out var e);
        switch (path)
        {
            case "":
                page.Title = "Mail";
                e.Add(new LargeContainerElement("Mail"));
                AddStatusAndSidebar(req, pathPrefix);
                e.Add(new ButtonElement("Send an email", null, $"{pathPrefix}/mail/send"));
                e.Add(new ButtonElement("Restart the mail server", null, $"{pathPrefix}/mail/restart"));
                break;
            case "/send":
                page.Title = "Send an email";
                page.Scripts.Add(new Script(pathPrefix + "/send-mail.js"));
                e.Add(new LargeContainerElement("Send an email"));
                AddStatusAndSidebar(req, pathPrefix, false);
                e.Add(new ContainerElement(null,
                [
                    new TextBox("Recipient...", null, "to", TextBoxRole.Email),
                    new TextBox("Sender...", null, "from", TextBoxRole.Email),
                    new TextBox("Subject...", null, "subject", TextBoxRole.Email),
                    new TextArea("Body...", null, "text", 10)
                ]));
                e.Add(new ButtonElementJS("Send", null, "Send()", "green"));
                page.AddError();
                break;
            case "/restart":
                page.Title = "Restart the mail server";
                e.Add(new LargeContainerElement("Restart the mail server"));
                AddStatusAndSidebar(req, pathPrefix);
                try
                {
                    MailManager.In.Restart();
                    e.Add(new ContainerElement("Success!", "The mail server has been restarted.", "green"));
                }
                catch (Exception ex)
                {
                    e.Add(new ContainerElement("Failed!", ex.Message, "red"));
                }
                break;
            default:
                req.Status = 404;
                break;
        }
    }

    private async Task HandleMail(ApiRequest req, string path, string pathPrefix)
    {
        if (!EnableMail)
        {
            req.Status = 403;
            return;
        }

        switch (path)
        {
            case "/send":
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
            default:
                req.Status = 404;
                break;
        }
    }
}