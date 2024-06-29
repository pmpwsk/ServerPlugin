using System.Diagnostics;
using System.IO.Compression;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    private async Task Wrapper(Request req)
    {
        switch (req.Path)
        {
            // MANAGE WRAPPER
            case "/wrapper":
            { CreatePage(req, "Wrapper", out var page, out var e, true);
                if (!EnableWrapper)
                    throw new ForbiddenSignal();
                page.Navigation.Add(new Button("Back", "..", "right"));
                page.Scripts.Add(new Script("wrapper.js"));
                e.Add(new HeadingElement("Wrapper"));
                page.AddError();
                e.Add(new ButtonElement("Log", null, "wrapper/log"));
                e.Add(new ContainerElement("Update", new FileSelector("update-file")) { Button = new ButtonJS("Start", "Update()", "green", id: "updateButton") });
                e.Add(new ButtonElementJS("Revert to backed up version", null, "Revert()"));
                e.Add(new ButtonElementJS("Restart program", null, "Restart()"));
                e.Add(new ButtonElementJS("Stop program", null, "Stop()", id: "stopButton"));
                e.Add(new ButtonElementJS("Reload Wrapper config", null, "ReloadConfig()"));
            } break;

            case "/wrapper/revert":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableWrapper)
                    throw new ForbiddenSignal();
                Console.WriteLine("wrapper rollback");
                Server.Exit(false);
            } break;

            case "/wrapper/restart":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableWrapper)
                    throw new ForbiddenSignal();
                Server.Exit(false);
            } break;

            case "/wrapper/stop":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableWrapper)
                    throw new ForbiddenSignal();
                Server.Exit(true);
            } break;

            case "/wrapper/reload-config":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableWrapper)
                    throw new ForbiddenSignal();
                Console.WriteLine("wrapper reload-config");
            } break;

            case "/wrapper/update":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableWrapper)
                    throw new ForbiddenSignal();
                if (Directory.Exists("../Update"))
                    Directory.Delete("../Update", true);
                if (Directory.Exists("../UpdateTemp"))
                    Directory.Delete("../UpdateTemp", true);
                req.BodySizeLimit = null;
                if (req.Files.Count != 1)
                    throw new BadRequestSignal();
                string execName = ((Process.GetCurrentProcess().MainModule?.FileName) ?? throw new HttpStatusSignal(503)).Split('/', '\\').Last();
                var file = req.Files[0];
                if (file.FileName.EndsWith(".zip"))
                {
                    file.Download("../UpdateTemp.zip", long.MaxValue);
                    ZipFile.ExtractToDirectory("../UpdateTemp.zip", "../UpdateTemp");
                    File.Delete("../UpdateTemp.zip");
                    if (File.Exists("../UpdateTemp/" + execName))
                    {
                        Directory.Move("../UpdateTemp", "../Update");
                        //update zip without subfolder
                    }
                    else
                    {
                        var folders = Directory.GetDirectories("../UpdateTemp", "*", SearchOption.TopDirectoryOnly);
                        var files = Directory.GetFiles("../UpdateTemp", "*", SearchOption.TopDirectoryOnly);
                        if (files.Length == 0 && folders.Length == 1 && File.Exists(folders[0] + "/" + execName))
                        {
                            Directory.Move(folders[0], "../UpdateTemp2");
                            Directory.Delete("../UpdateTemp", true);
                            Directory.Move("../UpdateTemp2", "../Update");
                            //update zip with subfolder
                        }
                        else
                        {
                            Directory.Delete("../UpdateTemp", true);
                            throw new HttpStatusSignal(418);
                        }
                    }
                }
                else if (file.FileName == execName)
                {
                    Directory.CreateDirectory("../UpdateTemp");
                    file.Download("../UpdateTemp/" + file.FileName, long.MaxValue);
                    Directory.Move("../UpdateTemp", "../Update");
                    //update with single file
                }
                else throw new HttpStatusSignal(418);

                Console.WriteLine($"{req.User.Username} ({req.User.Id}) uploaded an update.");
                Server.Exit(false);
            } break;




            // WRAPPER LOG
            case "/wrapper/log":
            { CreatePage(req, "Log", out var page, out var e, false);
                if (!EnableWrapper)
                    throw new ForbiddenSignal();
                page.Navigation.Add(new Button("Back", "../wrapper", "right"));
                bool wide = req.Query.TryGet("wide") == "true";
                page.Scripts.Add(new Script("log.js"));
                if (wide)
                    page.Styles.Add(new CustomStyle("div.sidebar { display: none; } div.content { width: 100% !important; flex: 0 !important; } div.full { width: auto !important; display: block !important; margin: 0 0.6rem; !important }"));
                List<IContent> contents = [];
                if (File.Exists("../Wrapper.log"))
                {
                    foreach (string line in File.ReadAllLines("../Wrapper.log"))
                        contents.Add(new Paragraph(line.HtmlSafe()));
                    page.Scripts.Add(new CustomScript($"let logEvent = new EventSource('log-event?t={File.GetLastWriteTimeUtc("../Wrapper.log").Ticks}');\nonbeforeunload = (event) => {{ logEvent.close(); }};\nlogEvent.onmessage = function (event) {{\n\tif (event.data === 'refresh')\n\t\twindow.location.reload();\n}};"));
                }
                else contents.Add(new Paragraph("../Wrapper.log not found!"));
                List<IButton> buttons =
                [
                    new Button("Raw", "log-raw", newTab: true),
                    wide ? new Button("Normal", "log") : new Button("Wide", "log?wide=true")
                ];
                if (EnableWrapperLogClearing)
                    buttons.Add(new ButtonJS("Clear", "Clear()", "red"));
                e.Add(new LargeContainerElement("Log", contents) { Buttons = buttons });
            } break;

            case "/wrapper/log-event":
            { req.ForceGET(); req.ForceAdmin(false);
                if (!EnableWrapper)
                    throw new ForbiddenSignal();
                if (!req.Query.TryGetValue("t", out long t))
                    throw new BadMethodSignal();
                int countdown = 0;
                while (!req.Context.RequestAborted.IsCancellationRequested)
                {
                    if (countdown == 0)
                    {
                        if (File.GetLastWriteTimeUtc("../Wrapper.log").Ticks != t)
                        {
                            await Task.Delay(2000);
                            await req.EventMessage("refresh");
                            await req.KeepEventAlive();
                        }
                        countdown = 20;
                    }

                    countdown--;
                    await req.EventMessage(":keepalive");
                    await Task.Delay(30000);
                }
            } break;
            
            case "/wrapper/clear-log":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableWrapperLogClearing)
                    throw new ForbiddenSignal();
                Console.WriteLine("wrapper log-clear");
            } break;

            case "/wrapper/log-raw":
            { req.ForceGET(); req.ForceAdmin();
                if (!EnableWrapper)
                    throw new ForbiddenSignal();
                req.Context.Response.ContentType = "text/plain;charset=utf-8";
                await req.WriteFile("../Wrapper.log");
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }
    }
}