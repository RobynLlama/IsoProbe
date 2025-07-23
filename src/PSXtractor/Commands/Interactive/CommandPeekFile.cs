using System;
using System.IO;
using System.Text;
using SimpleCommandLib;

namespace PSXtractor.Commands;

public class CommandPeekFile : ICommandRunner
{
  public string CommandName => "peek";

  public string CommandUsage => "peeks at 128 bytes of data from the start of the given file\n  Usage: peek <fileName>";

  public bool Execute(string[] args)
  {
    if (args.Length == 0)
    {
      Console.WriteLine(CommandUsage);
      return true;
    }

    if (InteractiveDispatcher.Environment.LoadedMedia is null)
    {
      Console.WriteLine("No disk loaded!");
      return false;
    }

    string fullIdentifier;

    fullIdentifier = args[0].StartsWith(Path.DirectorySeparatorChar)
    ? args[0]
    : Path.Combine(InteractiveDispatcher.Environment.CurrentDirectory, args[0]);

    var item = InteractiveDispatcher.Environment.LoadedMedia.GetRecordFromPath(fullIdentifier);

    if (item is null || item.FlagIsDirectory)
    {
      Console.WriteLine($"Error, item does not exist or is a directory {fullIdentifier}");
      return false;
    }

    using MemoryStream contents = new(item.OwningSector.GetFileContents());
    using BinaryReader reader = new(contents);

    int dataToRead = contents.Length > 127
    ? 128
    : (int)contents.Length;

    byte[] data = reader.ReadBytes(dataToRead);
    Console.WriteLine("----------");
    Console.WriteLine(Encoding.ASCII.GetString(data));
    Console.WriteLine("----------");

    return true;
  }
}
