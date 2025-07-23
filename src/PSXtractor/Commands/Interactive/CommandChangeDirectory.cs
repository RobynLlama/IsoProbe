using System;
using SimpleCommandLib;

namespace PSXtractor.Commands;

public class CommandChangeDirectory : ICommandRunner
{
  public string CommandName => "cd";

  public string CommandUsage => "changes the directory to the specified directory\n  Usage: cd <directory> OR cd to list the current directory";

  public bool Execute(string[] args)
  {
    if (args.Length == 0)
    {
      Console.WriteLine(InteractiveDispatcher.Environment.CurrentDirectory);
      return true;
    }

    var success = InteractiveDispatcher.Environment.ChangeDirectory(args[0]);

    if (success)
      return true;

    Console.WriteLine($"Not a directory or doesn't exist: {args[0]}");
    return false;

  }
}
