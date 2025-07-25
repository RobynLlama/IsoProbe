using System;
using System.IO;
using IsoProbe.Commands;

namespace IsoProbe
{
  internal class Program
  {
    private static bool _GCRequest = false;
    public static void GCRequest() => _GCRequest = true;
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

        if (_GCRequest)
        {
          GC.WaitForPendingFinalizers();
          GC.Collect();
          _GCRequest = false;
        }

      }
    }
  }
}
