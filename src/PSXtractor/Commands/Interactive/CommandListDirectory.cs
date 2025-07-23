using System;
using System.Text;
using SimpleCommandLib;

namespace PSXtractor.Commands;

public class CommandListDirectory : ICommandRunner
{
  public string CommandName => "ls";

  public string CommandUsage => "lists the contents of the current directory";

  public bool Execute(string[] args)
  {
    var env = InteractiveDispatcher.Environment;

    if (env.LoadedMedia is null || env.CurrentDirectoryRecord is null)
    {
      Console.WriteLine("No image loaded");
      return false;
    }

    var sb = new StringBuilder();
    env.CurrentDirectoryRecord.DumpFileListing(sb, 0, false);

    Console.WriteLine(sb.ToString());

    return true;
  }
}
