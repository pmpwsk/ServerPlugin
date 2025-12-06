using MimeKit;
using uwap.WebFramework.Elements;
using uwap.WebFramework.Mail;
using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin
{
    private async Task<IResponse> HandleMail(Request req)
    {
        switch (req.Path)
        {
            // MANAGE MAIL
            case "/mail":
            { CreatePage(req, "Mail", out var page, out var e, true);
                if (!EnableMail)
                    return StatusResponse.Forbidden;
                page.Navigation.Add(new Button("Back", ".", "right"));
                page.Scripts.Add(new Script("mail.js"));
                e.Add(new HeadingElement("Mail"));
                e.Add(new ButtonElement("Send an email", null, "mail/send"));
                e.Add(new ButtonElementJS("Restart the mail server", null, "Restart()"));
                return new LegacyPageResponse(page, req);
            }
            
            case "/mail/restart":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableMail)
                    return StatusResponse.Forbidden;
                await MailManager.In.RestartAsync();
                return StatusResponse.Success;
            }




            // SEND AN EMAIL
            case "/mail/send":
            { CreatePage(req, "Send an email", out var page, out var e, false);
                if (!EnableMail)
                    return StatusResponse.Forbidden;
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
                return new LegacyPageResponse(page, req);
            }

            case "/mail/send/try":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableMail)
                    return StatusResponse.Forbidden;
                var to = req.Query.GetOrThrow("to");
                var from = req.Query.GetOrThrow("from");
                var subject = req.Query.GetOrThrow("subject");
                var text = req.Query.GetOrThrow("text");
                var (result, _) = await MailManager.Out.SendAsync(new MailGen(new(from, from),
                    to.Split(' ', ',', ';').Where(x => x != "").Select(x => new MailboxAddress(x, x)),
                    subject, null, text));
                List<string> response = [];
                if (result.FromSelf != null)
                    response.Add($"Self: {result.FromSelf.ResultType}");
                if (result.FromBackup != null)
                    response.Add($"Backup: {result.FromBackup.ResultType}");
                return new TextResponse(response.Count == 0 ? "The email was not sent." : string.Join('\n', response));
            }




            // 404
            default:
                return StatusResponse.NotFound;
        }
    }
}