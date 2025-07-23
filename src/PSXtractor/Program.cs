using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ISO9660Lib.ISO9660FS;
using PSXtractor.Commands;

namespace PSXtractor
{
  internal class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Please LOAD a disk to continue");
      var dispatcher = new InteractiveDispatcher();

      while (true)
      {
        Console.Write($"{InteractiveDispatcher.Environment.CurrentDirectory}> ");
        var text = Console.ReadLine();

        if (!string.IsNullOrEmpty(text))
          dispatcher.ParseAndRunCommand(text);
        else
          Console.WriteLine("Try again!");
      }
    }
  }
}
