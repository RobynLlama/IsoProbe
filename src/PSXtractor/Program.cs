using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ISO9660Lib.ISO9660FS;

namespace PSXtractor
{
  internal class Program
  {
    static void Main(string[] args)
    {
      Stopwatch sw = new();
      long creationTime;
      long buildTime;
      long dumpTime;

      string filePath = @"/home/qtmims/mednafen/Rampage 2 - Universal Tour.bin";
      FileInfo input = new(filePath);

      Console.WriteLine($"Creating ECMAFS from: {input.Name}");

      ECMAFS disk = new(input);
      creationTime = sw.ElapsedMilliseconds;
      sw.Restart();

      Console.WriteLine($"Read a total of {disk.SectorCount} sectors!");

      Console.WriteLine($"""
        Version:       {disk.PVD.VolumeDescriptorVersion}

        SystemID:      {disk.PVD.SystemID}
        VolumeID:      {disk.PVD.VolumeID}

        VolumeSet:     {disk.PVD.VolumeSetSize}
        VolumeNo:      {disk.PVD.VolumeSetNumber}
        Blocks:        {disk.PVD.LogicalBlockCount}
        BlockSize:     {disk.PVD.LogicalBlockSize}
        PathTableSize: {disk.PVD.PathTableSize}

        Root Record:
         Identifier:   {disk.PVD.RootRecord.Identifier}
         Extent:       {disk.PVD.RootRecord.LocationOfExtent}
         Size:         {disk.PVD.RootRecord.DataLength}

      """);

      StringBuilder sb = new("Dumping contents of disc:\n");

      //disk.PVD.RootRecord.DumpDirectory(sb, 1);

      buildTime = sw.ElapsedMilliseconds;
      sw.Restart();

      //Console.WriteLine(sb.ToString());
      Console.WriteLine($"Dumped {disk.FileSize:N0} bytes to default directory");

      DirectoryInfo oDir = new("dump");
      if (!oDir.Exists)
        oDir.Create();

      //foreach (var item in disk.PVD.RootRecord.ContainedItems)
      //item.DumpContents(oDir.FullName);

      var randomFile = disk.PVD.RootRecord.OwningSector.GetDirectoryContents().Where(x => x.Identifier == "SYSTEM.CNF;1").First();
      Console.WriteLine(randomFile.Identifier);
      Console.WriteLine(Encoding.ASCII.GetString(randomFile.OwningSector.GetFileContents()));

      dumpTime = sw.ElapsedMilliseconds;
      sw.Stop();

      Console.WriteLine($"""

      Timing:
        Creation: {creationTime} ms
        Build:    {buildTime} ms
        Dump:     {dumpTime} ms

        Total:    {creationTime + buildTime + dumpTime} ms
      """);
    }
  }
}
