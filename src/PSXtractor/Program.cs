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
      string filePath = @"/home/qtmims/mednafen/Rampage 2 - Universal Tour.bin";
      FileInfo input = new(filePath);

      Console.WriteLine($"Creating ECMAFS from: {input.Name}");

      ECMAFS disk = new(input);

      Console.WriteLine($"""

      Disk:
        Size:        {disk.FileSize / 1000000f:N1}mb

        VolumeSet:   {disk.VolumeSetID}
        Publisher:   {disk.PublisherID}
        Preparer:    {disk.PreparerID}
        Application: {disk.ApplicationID}

        Copyright:   {disk.CopyrightFile}
        Abstract:    {disk.AbstractFile}
        Biblio:      {disk.BiblioFile}
      
      Primary Volume Record:
        Version:     {disk.PVD.VolumeDescriptorVersion}

        SystemID:    {disk.PVD.SystemID}
        VolumeID:    {disk.PVD.VolumeID}

        VolumeSet:   {disk.PVD.VolumeSetSize}
        VolumeNo:    {disk.PVD.VolumeSetNumber}
        Blocks:      {disk.PVD.LogicalBlockCount}
        BlockSize:   {disk.PVD.LogicalBlockSize}
      
      Path Table:
        Location:    {disk.PVD.PathTable.LocationOfExtent}
        Size:        {disk.PVD.PathTable.DataLength}

      Root Record:
        Identifier:  {disk.PVD.RootRecord.Identifier}
        Extent:      {disk.PVD.RootRecord.LocationOfExtent}
        Size:        {disk.PVD.RootRecord.DataLength}

      """);
    }
  }
}
