# ServerPlugin
Plugin for [WebFramework](https://github.com/pmpwsk/WebFramework) that adds a simple interface to manage the program and server in general.

The functionality is mainly directed for Linux servers with WF programs that use [Wrapper](https://github.com/pmpwsk/Wrapper).

All fully logged in users with AccessLevel=ushort.MaxValue (administrators) will have access to this plugin, nobody else can access it.

Website: https://uwap.org/projects/server-plugin

Changelog: https://uwap.org/changes/server-plugin

## Main features
- Log viewing and clearing (requires Wrapper with LogFile=Wrapper.log)
- SSH management (requires Linux structure for key management, and ufw for port whitelisting)
- Viewing your IP address from the server's perspective
- Updating the program and requesting version rollbacks (requires Wrapper, rollbacks require CreateBackup=true)
- Restarting and exiting the program (requires Wrapper)
- Reloading the Wrapper configuration (requires Wrapper, obviously)
- Sending simple emails
- Restarting the mail server (shouldn't really ever be necessary)
- Calling the worker
- Showing how much time is left before the worker runs again
- Showing that the system needs to be rebooted after a system upgrade (requires Linux)

## Installation
You can install this library to your WF project by downloading a .dll file or the source code and referencing it in your project file. In the future, there will be a NuGet package.

If you're using the source code, you will need to update the project reference to WebFramework according to where you have it. The reference will soon be replaced with a NuGet dependency so the library becomes smaller and you don't need to reference WF manually.

Once installed, add the following things to your program start code:
- Add <code>using uwap.WebFramework.Plugins;</code> to the top, otherwise you have to prepend it to <code>ServerPlugin</code>
- Create a new object of the plugin: <code>ServerPlugin serverPlugin = new();</code>
- Map the plugin to a path of your choosing (like any/server): <code>PluginManager.Map("any/server", serverPlugin);</code>

You can do all that with a single line of code before starting the WF server:<br/><code>PluginManager.Map("any/server", new uwap.WebFramework.Plugins.ServerPlugin());</code>

## Plans for the future
- Arranging the menu items in a nicer manner
- Adding stuff to test WebFramework
- Whitelisting/management for custom ports
- Utilizing the sidebar, somehow
