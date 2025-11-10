namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin
{
	public override byte[]? GetFile(string relPath, string pathPrefix, string domain)
		=> relPath switch
		{
			"/backups.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File0"),
			"/icon.ico" => (byte[]?)PluginFiles_ResourceManager.GetObject("File1"),
			"/icon.png" => (byte[]?)PluginFiles_ResourceManager.GetObject("File2"),
			"/icon.svg" => (byte[]?)PluginFiles_ResourceManager.GetObject("File3"),
			"/mail.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File4"),
			"/manifest.json" => System.Text.Encoding.UTF8.GetBytes($"{{\r\n  \"name\": \"Manage {domain}\",\r\n  \"short_name\": \"Manage {domain}\",\r\n  \"start_url\": \"{(pathPrefix == "" ? "/" : pathPrefix)}\",\r\n  \"display\": \"minimal-ui\",\r\n  \"background_color\": \"#000000\",\r\n  \"theme_color\": \"#202024\",\r\n  \"orientation\": \"portrait-primary\",\r\n  \"icons\": [\r\n    {{\r\n      \"src\": \"{pathPrefix}/icon.svg\",\r\n      \"type\": \"image/svg+xml\",\r\n      \"sizes\": \"any\"\r\n    }},\r\n    {{\r\n      \"src\": \"{pathPrefix}/icon.png\",\r\n      \"type\": \"image/png\",\r\n      \"sizes\": \"512x512\"\r\n    }},\r\n    {{\r\n      \"src\": \"{pathPrefix}/icon.ico\",\r\n      \"type\": \"image/x-icon\",\r\n      \"sizes\": \"16x16 24x24 32x32 48x48 64x64 72x72 96x96 128x128 256x256\"\r\n    }}\r\n  ],\r\n  \"launch_handler\": {{\r\n    \"client_mode\": \"navigate-new\"\r\n  }},\r\n  \"related_applications\": [\r\n    {{\r\n      \"platform\": \"webapp\",\r\n      \"url\": \"{pathPrefix}/manifest.json\"\r\n    }}\r\n  ],\r\n  \"offline_enabled\": false,\r\n  \"omnibox\": {{\r\n    \"keyword\": \"server\"\r\n  }},\r\n  \"version\": \"1.0.0\"\r\n}}\r\n"),
			"/menu.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File5"),
			"/ssh.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File6"),
			"/status.js" => System.Text.Encoding.UTF8.GetBytes($"let statusElem = document.querySelector(\"#status\");\r\nlet statusEvent = new EventSource('{pathPrefix}/status-event');\r\nonbeforeunload = (event) => {{ statusEvent.close(); }};\r\nstatusEvent.onmessage = function (event) {{\r\n    if (!event.data.startsWith(\":\")) {{\r\n        statusElem.innerHTML = event.data;\r\n        if (event.data.startsWith(\"<h\"))\r\n            statusElem.className = \"elem red\";\r\n        else statusElem.className = \"elem\";\r\n    }}\r\n}};\r\n"),
			"/wrapper.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File7"),
			"/mail/send.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File8"),
			"/ssh/user.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File9"),
			"/wrapper/log.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File10"),
			_ => null
		};
	
	public override string? GetFileVersion(string relPath)
		=> relPath switch
		{
			"/backups.js" => "1719520503099",
			"/icon.ico" => "1704142601000",
			"/icon.png" => "1704142515000",
			"/icon.svg" => "1704142502000",
			"/mail.js" => "1719616450913",
			"/manifest.json" => "1704152593000",
			"/menu.js" => "1719507296424",
			"/ssh.js" => "1719622495188",
			"/status.js" => "1719509990329",
			"/wrapper.js" => "1719523726713",
			"/mail/send.js" => "1719614870928",
			"/ssh/user.js" => "1719623202761",
			"/wrapper/log.js" => "1719511753743",
			_ => null
		};
	
	private static readonly System.Resources.ResourceManager PluginFiles_ResourceManager = new("ServerPlugin.Properties.PluginFiles", typeof(ServerPlugin).Assembly);
}