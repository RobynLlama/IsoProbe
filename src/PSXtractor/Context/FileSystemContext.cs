using System;
using System.IO;
using ISO9660Lib.ISO9660FS;

namespace PSXtractor.Context;

public class FileSystemContext
{
  public ECMAFS? LoadedMedia { get; protected set; }
  public string CurrentDirectory { get; protected set; } = Path.DirectorySeparatorChar.ToString();
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

    newPath = ResolveUserPath(newPath);

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
    CurrentDirectory = Path.DirectorySeparatorChar.ToString();
    CurrentDirectoryRecord = null;
    LoadedMedia = null;
  }

  /// <summary>
  /// Processes a user-inputted path string by removing surrounding quotes if present,
  /// and converting relative paths to absolute paths based on the current environment directory.
  /// </summary>
  /// <param name="inputPath">The user-inputted path string, which may be quoted and/or relative.</param>
  /// <returns>The absolute, unquoted path string.</returns>
  public string ResolveUserPath(string inputPath)
  {
    string fullIdentifier;

    //Trim quotes if needed
    fullIdentifier = inputPath.StartsWith('"') && inputPath.EndsWith('"') ? inputPath.Trim('"') : inputPath;

    //Resolve a relative path
    if (!fullIdentifier.StartsWith(Path.DirectorySeparatorChar))
      fullIdentifier = Path.Combine(CurrentDirectory, fullIdentifier);

    //Resolve symbols in path such as '.' and '..'
    fullIdentifier = Path.GetFullPath(fullIdentifier);

    return fullIdentifier;
  }
}
