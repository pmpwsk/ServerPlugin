using System.Diagnostics;
using System.IO.Compression;
using uwap.WebFramework.Elements;
using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin
{
    private async Task<IResponse> HandleWrapper(Request req)
    {
        switch (req.Path)
        {
            // MANAGE WRAPPER
            case "/wrapper":
            { CreatePage(req, "Wrapper", out var page, out var e, true);
                if (!EnableWrapper)
                    return StatusResponse.Forbidden;
                page.Navigation.Add(new Button("Back", ".", "right"));
                page.Scripts.Add(new Script("wrapper.js"));
                e.Add(new HeadingElement("Wrapper"));
                page.AddError();
                e.Add(new ButtonElement("Log", null, "wrapper/log"));
                e.Add(new ContainerElement("Update", new FileSelector("update-file")) { Button = new ButtonJS("Start", "Update()", "green", id: "updateButton") });
                e.Add(new ButtonElementJS("Revert to backed up version", null, "Revert()"));
                e.Add(new ButtonElementJS("Restart program", null, "Restart()"));
                e.Add(new ButtonElementJS("Stop program", null, "Stop()", id: "stopButton"));
                e.Add(new ButtonElementJS("Reload Wrapper config", null, "ReloadConfig()"));
                return new LegacyPageResponse(page, req);
            }

            case "/wrapper/revert":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableWrapper)
                    return StatusResponse.Forbidden;
                Console.WriteLine("wrapper rollback");
                Server.Exit(false);
                return StatusResponse.Success;
            }

            case "/wrapper/restart":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableWrapper)
                    return StatusResponse.Forbidden;
                Server.Exit(false);
                return StatusResponse.Success;
            }

            case "/wrapper/stop":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableWrapper)
                    return StatusResponse.Forbidden;
                Server.Exit(true);
                return StatusResponse.Success;
            }

            case "/wrapper/reload-config":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableWrapper)
                    return StatusResponse.Forbidden;
                Console.WriteLine("wrapper reload-config");
                return StatusResponse.Success;
            }

            case "/wrapper/update":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableWrapper)
                    return StatusResponse.Forbidden;
                if (Directory.Exists("../Update"))
                    Directory.Delete("../Update", true);
                if (Directory.Exists("../UpdateTemp"))
                    Directory.Delete("../UpdateTemp", true);
                req.BodySizeLimit = null;
                if (req.Files.Count != 1)
                    return StatusResponse.BadRequest;
                var execPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (execPath == null)
                    return StatusResponse.ServiceUnavailable;
                string execName = execPath.Split('/', '\\').Last();
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
                            return StatusResponse.Teapot;
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
                else
                    return StatusResponse.Teapot;

                Console.WriteLine($"{req.User.Username} ({req.User.Id}) uploaded an update.");
                Server.Exit(false);
                return StatusResponse.Success;
            }




            // WRAPPER LOG
            case "/wrapper/log":
            { CreatePage(req, "Log", out var page, out var e, false);
                if (!EnableWrapper)
                    return StatusResponse.Forbidden;
                page.Navigation.Add(new Button("Back", "../wrapper", "right"));
                bool wide = req.Query.TryGet("wide") == "true";
                page.Scripts.Add(new Script("log.js"));
                if (wide)
                    page.Styles.Add(new CustomStyle("div.sidebar { display: none; } div.content { width: 100% !important; flex: 0 !important; } div.full { width: auto !important; display: block !important; margin: 0 0.6rem; !important }"));
                List<IContent> contents = [];
                if (File.Exists("../Wrapper.log"))
                {
                    foreach (string line in await File.ReadAllLinesAsync("../Wrapper.log"))
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
                return new LegacyPageResponse(page, req);
            }

            case "/wrapper/log-event":
            { req.ForceGET(); req.ForceAdmin(false);
                if (!EnableWrapper)
                    return StatusResponse.Forbidden;
                var t = req.Query.GetOrThrow<long>("t");
                int countdown = 0;
                var response = new EventResponse();
                response.OnTick = async () =>
                {
                    if (countdown == 0)
                    {
                        if (File.GetLastWriteTimeUtc("../Wrapper.log").Ticks != t)
                        {
                            await Task.Delay(2000);
                            await response.EventMessage("refresh");
                        }
                        countdown = 20;
                    }

                    countdown--;
                };
                return response;
            }
            
            case "/wrapper/clear-log":
            { req.ForcePOST(); req.ForceAdmin(false);
                if (!EnableWrapperLogClearing)
                    return StatusResponse.Forbidden;
                Console.WriteLine("wrapper log-clear");
                return StatusResponse.Success;
            }

            case "/wrapper/log-raw":
            { req.ForceGET(); req.ForceAdmin();
                if (!EnableWrapper)
                    return StatusResponse.Forbidden;
                return new FileResponse("../Wrapper.log", false, null);
            }




            // 404
            default:
                return StatusResponse.NotFound;
        }
    }
}