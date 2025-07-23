using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ISO9660Lib.ISO9660FS;

namespace PSXtractor.Context;

public class FileSystemContext
{
  public ECMAFS? LoadedMedia { get; protected set; }
  public string CurrentDirectory { get; protected set; } = "/";
  public DataRecord? CurrentDirectoryRecord { get; protected set; }

  public bool LoadMedia(FileInfo info)
  {
    if (LoadedMedia is not null)
      UnloadMedia();
    try
    {
      LoadedMedia = new(info);
      CurrentDirectoryRecord = LoadedMedia.PVD.RootRecord;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Full file info {info.FullName}");
      Console.WriteLine($"Error loading disk {ex}");
      return false;
    }

    return true;
  }

  public void UnloadMedia() => Reset();

  public bool ChangeDirectory(string newPath)
  {
    if (LoadedMedia is null)
      return false;

    //determine if the change is relative
    if (!newPath.StartsWith('/'))
      newPath = Path.Combine(CurrentDirectory, newPath);

    //check if the path makes sense
    var record = LoadedMedia.GetRecordFromPath(newPath);

    if (record is not null && record.FlagIsDirectory)
    {
      CurrentDirectory = Path.GetFullPath(newPath);
      CurrentDirectoryRecord = record;
      return true;
    }

    return false;
  }

  internal void Reset()
  {
    CurrentDirectory = "/";
    CurrentDirectoryRecord = null;
    LoadedMedia = null;
  }
}
