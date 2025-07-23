using SimpleCommandLib;

namespace IsoProbe.Commands;

public class CommandCloseDisk : ICommandRunner
{
  public string CommandName => "close";

  public string CommandUsage => "closes the currently open disk image";

  public bool Execute(string[] args)
  {
    InteractiveDispatcher.Environment.UnloadMedia();
    return true;
  }
}
