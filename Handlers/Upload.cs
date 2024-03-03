using System.Diagnostics;
using System.IO.Compression;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin : Plugin
{
    public override Task Handle(UploadRequest req, string path, string pathPrefix)
    {
        if (!req.IsAdmin())
        {
            req.Status = 403;
            return Task.CompletedTask;
        }
        switch (path)
        {
            case "/wrapper/update":
                {
                    if (!EnableWrapper)
                    {
                        req.Status = 403;
                        return Task.CompletedTask;
                    }
                    if (Directory.Exists("../Update"))
                        Directory.Delete("../Update", true);
                    if (Directory.Exists("../UpdateTemp"))
                        Directory.Delete("../UpdateTemp", true);
                    if (req.Files.Count != 1)
                    {
                        req.Status = 400;
                        break;
                    }
                    string? execName = Process.GetCurrentProcess().MainModule?.FileName;
                    if (execName == null)
                    {
                        req.Status = 503;
                        break;
                    }
                    execName = execName.Split('/', '\\').Last();
                    var file = req.Files[0];
                    if (file.FileName.EndsWith(".zip"))
                    {
                        file.Download("../UpdateTemp.zip", 1073741824);
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
                                req.Status = 418;
                                break;
                            }
                        }
                    }
                    else if (file.FileName == execName)
                    {
                        Directory.CreateDirectory("../UpdateTemp");
                        file.Download("../UpdateTemp/" + file.FileName, 1073741824);
                        Directory.Move("../UpdateTemp", "../Update");
                        //update with single file
                    }
                    else
                    {
                        req.Status = 418;
                        break;
                    }

                    Console.WriteLine($"{req.User.Username} ({req.User.Id}) uploaded an update.");
                    Server.Exit(false);
                }
                break;
            default:
                req.Status = 404;
                break;
        }
        return Task.CompletedTask;
    }
}