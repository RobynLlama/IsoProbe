using System;
using System.IO;
using ISO9660Lib.ISO9660FS;
using SimpleCommandLib;

namespace IsoProbe.Commands;

public class CommandDumpFile : ICommandRunner
{
  public string CommandName => "fileDump";

  public string CommandUsage => "Outputs the given file to the given location on the host filesystem. *WARNING* Will overwrite local files without asking!\n  Usage: fileDump <file> <saveLocation>";

  public bool Execute(string[] args)
  {
    if (args.Length < 2)
    {
      Console.WriteLine(CommandUsage);
      return false;
    }

    if (InteractiveDispatcher.Environment.LoadedMedia is null)
    {
      Console.WriteLine("Please load a disk first");
      return false;
    }

    var rawFileIn = InteractiveDispatcher.Environment.ResolveUserPath(args[0]);
    var rawFileOut = Path.GetFullPath(args[1]);

    FileInfo fileOut = new(rawFileOut);
    if (InteractiveDispatcher.Environment.LoadedMedia.GetRecordFromPath(rawFileIn) is not DataRecord fileIn || fileIn.FlagIsDirectory)
    {
      Console.WriteLine($"Error: {rawFileIn} does not exist or is a directory!");
      return false;
    }

    if (fileOut.Exists)
    {
      Console.WriteLine($"Warning: overwriting {fileOut.FullName}");
      fileOut.Delete();
    }

    using var writer = new FileStream(fileOut.FullName, FileMode.OpenOrCreate, FileAccess.Write);

    var data = new byte[fileIn.Owner.SECTOR_SIZE];
    var output = 0;

    foreach (var bytes in fileIn.GetFileContentsSectors(data))
    {
      output += bytes;
      writer.Write(data, 0, bytes);
    }

    var smolWrite = output < 2048;

    if (!smolWrite)
      Console.Write($"Wrote {output / 1000f:N1} kb ");
    else
      Console.Write($"Wrote {output:N0} bytes ");

    Console.WriteLine($"to file: {fileOut.FullName}");

    return true;
  }
}
