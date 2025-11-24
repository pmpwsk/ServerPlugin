using MimeKit;
using uwap.WebFramework.Elements;
using uwap.WebFramework.Mail;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin
{
    private async Task HandleMail(Request req)
    {
        switch (req.Path)
        {
            // MANAGE MAIL
            case "/mail":
            { CreatePage(req, "Mail", out var page, out var e, true);
                if (!EnableMail)
                    throw new ForbiddenSignal();
                page.Navigation.Add(new Button("Back", ".", "right"));
                page.Scripts.Add(new Script("mail.js"));
                e.Add(new HeadingElement("Mail"));
                e.Add(new ButtonElement("Send an email", null, "mail/send"));
                e.Add(new ButtonElementJS("Restart the mail server", null, "Restart()"));
            } break;
            
            case "/mail/restart":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableMail)
                    throw new ForbiddenSignal();
                try
                {
                    await MailManager.In.RestartAsync();
                }
                catch (Exception ex)
                {
                    req.Status = 500;
                    await req.Write("Exception: " + ex.Message);
                }
            } break;




            // SEND AN EMAIL
            case "/mail/send":
            { CreatePage(req, "Send an email", out var page, out var e, false);
                if (!EnableMail)
                    throw new ForbiddenSignal();
                page.Navigation.Add(new Button("Back", "../mail", "right"));
                page.Scripts.Add(new Script("send.js"));
                e.Add(new HeadingElement("Send an email"));
                e.Add(new ContainerElement(null,
                [
                    new TextBox("Recipient...", null, "to", TextBoxRole.Email),
                    new TextBox("Sender...", null, "from", TextBoxRole.Email),
                    new TextBox("Subject...", null, "subject", TextBoxRole.Email),
                    new TextArea("Body...", null, "text", 10)
                ]));
                e.Add(new ButtonElementJS("Send", null, "Send()", "green"));
                page.AddError();
            } break;

            case "/mail/send/try":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableMail)
                    throw new ForbiddenSignal();
                if (!(req.Query.TryGetValue("to", out var to) && req.Query.TryGetValue("from", out var from)
                    && req.Query.TryGetValue("subject", out var subject) && req.Query.TryGetValue("text", out var text)))
                    throw new BadRequestSignal();
                var (result, _) = await MailManager.Out.SendAsync(new MailGen(new(from, from),
                    to.Split(' ', ',', ';').Where(x => x != "").Select(x => new MailboxAddress(x, x)),
                    subject, null, text));
                List<string> response = [];
                if (result.FromSelf != null)
                    response.Add($"Self: {result.FromSelf.ResultType}");
                if (result.FromBackup != null)
                    response.Add($"Backup: {result.FromBackup.ResultType}");
                await req.Write(response.Count == 0 ? "The email was not sent." : string.Join('\n', response));
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }
    }
}