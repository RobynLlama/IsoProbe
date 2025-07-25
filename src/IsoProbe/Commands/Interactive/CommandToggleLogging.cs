using System;
using System.Text;
using SimpleCommandLib;

namespace IsoProbe.Commands;

public class CommandToggleLogging : ICommandRunner
{
  public string CommandName => "log";

  public string CommandUsage => "Toggles all ECMAFS logging";

  public bool Execute(string[] args)
  {
    var env = InteractiveDispatcher.Environment;

    if (env.LoadedMedia is null || env.CurrentDirectoryRecord is null)
    {
      Console.WriteLine("No image loaded");
      return false;
    }

    if (env.LoadedMedia._logger is null)
      return true;

    var log = env.LoadedMedia._logger;
    log.AllowError = !log.AllowError;
    log.AllowMessage = !log.AllowMessage;

    string status = log.AllowError ? "enabled" : "disabled";

    Console.WriteLine($"Logging {status}");

    return true;
  }
}
