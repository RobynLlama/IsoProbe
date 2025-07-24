using System;
using System.IO;
using System.Text;
using SimpleCommandLib;

namespace IsoProbe.Commands;

public class CommandPeekFile : ICommandRunner
{
  public string CommandName => "peek";

  public string CommandUsage => "peeks at 128 bytes of data from the start of the given file\n  Usage: peek <fileName> OR peek --raw <filename>";

  public bool Execute(string[] args)
  {
    bool rawMode = false;
    string fileName = string.Empty;

    if (args.Length == 0)
    {
      Console.WriteLine(CommandUsage);
      return true;
    }
    else if (args.Length == 1)
    {
      fileName = args[0];
    }
    else if (args.Length == 2)
    {
      if (args[0].Equals("--raw", StringComparison.InvariantCultureIgnoreCase))
        rawMode = true;
      else
      {
        Console.WriteLine($"error: unknown flag {args[0]}");
        return false;
      }

      fileName = args[1];
    }

    if (InteractiveDispatcher.Environment.LoadedMedia is null)
    {
      Console.WriteLine("No disk loaded!");
      return false;
    }

    var fullIdentifier = InteractiveDispatcher.Environment.ResolveUserPath(fileName);
    var item = InteractiveDispatcher.Environment.LoadedMedia.GetRecordFromPath(fullIdentifier);

    if (item is null || item.FlagIsDirectory)
    {
      Console.WriteLine($"Error, item does not exist or is a directory {fullIdentifier}");
      return false;
    }

    using MemoryStream contents = new(item.ExtentSector.GetFileContents());
    using BinaryReader reader = new(contents);

    int dataToRead = contents.Length > 127
    ? 128
    : (int)contents.Length;

    byte[] data = reader.ReadBytes(dataToRead);

    Console.WriteLine("----------");

    if (!rawMode)
    {
      var bytesWritten = 0;
      var bytesLine = 16;
      Console.Write($"0x0000: ");

      foreach (var thing in data)
      {
        Console.Write($"0x{thing:X2} ");
        bytesWritten++;

        if (bytesWritten % bytesLine == 0)
        {
          Console.Write(" | ");
          for (int i = bytesWritten - bytesLine; i < bytesWritten; i++)
          {
            byte current = data[i];
            if (current >= 32 && current <= 126)
              Console.Write((char)current);
            else
              Console.Write('.');

          }

          Console.WriteLine();

          if (bytesWritten != data.Length)
            Console.Write($"0x{bytesWritten:X4}: ");
        }
      }
    }
    else
      Console.WriteLine(Encoding.ASCII.GetString(data));

    Console.WriteLine("----------");

    return true;
  }
}
