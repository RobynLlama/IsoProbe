using System;
using SimpleCommandLib;

namespace IsoProbe.Commands;

public class CommandExit : ICommandRunner
{
  public string CommandName => "exit";

  public string CommandUsage => "quits the program";

  public bool Execute(string[] args)
  {
    Environment.Exit(0);
    return true;
  }
}
