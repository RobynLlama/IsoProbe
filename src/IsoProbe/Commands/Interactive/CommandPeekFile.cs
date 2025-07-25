using System;
using System.IO;
using System.Linq;
using System.Text;
using SimpleCommandLib;

namespace IsoProbe.Commands;

public class CommandPeekFile : ICommandRunner
{
  public string CommandName => "peek";

  public string CommandUsage => "peeks at 128 bytes of data from the start of the given file\n  Usage: peek <fileName> OR peek --raw <filename>";

  private static readonly string _separator = new('-', 107);

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

    byte[] buffer = new byte[item.Owner.SECTOR_SIZE];
    int bytesRead = item.GetFileContentsSectors(buffer).First();
    int dataToRead = Math.Min(bytesRead, 128);
    byte[] peekBuffer = buffer[..dataToRead];

    Console.WriteLine($"Dumping: {item.FullyQualifiedIdentifier}");
    Console.WriteLine(_separator);

    ConsoleColor orig = Console.ForegroundColor;

    ConsoleColor ASCIIHex = ConsoleColor.White;
    ConsoleColor ASCIIChar = ConsoleColor.Green;
    ConsoleColor Unprintable = ConsoleColor.DarkGray;
    ConsoleColor Offset = ConsoleColor.Yellow;

    if (!rawMode)
    {
      const int bytesPerLine = 16;
      int offset = 0;

      while (offset < dataToRead)
      {
        int lineLength = Math.Min(bytesPerLine, dataToRead - offset);
        var lineSlice = peekBuffer.AsSpan(offset, lineLength);

        //hex print offset
        Console.ForegroundColor = Offset;
        Console.Write($"0x{offset:X4}: ");

        bool isPrintable(byte b) =>
          b >= 32 && b <= 126;

        //hex print bytes
        foreach (var b in lineSlice)
        {
          Console.ForegroundColor = isPrintable(b) ? ASCIIHex : Unprintable;
          Console.Write($"0x{b:X2} ");
        }


        //pad the hex area if line shorter than 16 bytes
        if (lineLength < bytesPerLine)
        {
          int missing = bytesPerLine - lineLength;
          Console.ForegroundColor = ConsoleColor.DarkGray;
          Console.Write(new string(' ', missing * 5)); // 5 chars per byte "0xXX "
        }

        Console.ForegroundColor = orig;
        //print the string values
        Console.Write(" | ");
        foreach (var b in lineSlice)
        {
          //replace invalid characters with '.'
          var printable = isPrintable(b);
          char c = printable ? (char)b : '.';
          Console.ForegroundColor = printable ? ASCIIChar : Unprintable;
          Console.Write(c);
        }

        //push the line forward on the console and read a new line
        Console.ForegroundColor = orig;
        Console.WriteLine();
        offset += lineLength;
      }
    }
    else
      Console.WriteLine(Encoding.ASCII.GetString(peekBuffer));

    Console.ForegroundColor = orig;
    Console.WriteLine(_separator);

    return true;
  }
}
