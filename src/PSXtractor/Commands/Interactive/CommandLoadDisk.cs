using System;
using System.IO;
using SimpleCommandLib;

namespace PSXtractor.Commands;

public class CommandLoadDisk : ICommandRunner
{
  public string CommandName => "load";

  public string CommandUsage => "Loads an ISO-9660 format disk into memory for use\n  Usage: load <filePath>";

  public bool Execute(string[] args)
  {
    if (args.Length == 0)
    {
      Console.WriteLine(CommandUsage);
      return false;
    }

    var fileName = args[0];
    if (fileName.StartsWith('"'))
      fileName = fileName.Trim('"');

    FileInfo info = new(fileName);
    var success = InteractiveDispatcher.Environment.LoadMedia(info);

    if (success)
      Console.WriteLine("Image loaded");
    else
      Console.WriteLine("Failed to load image");

    return success;
  }
}
