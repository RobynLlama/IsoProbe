using System.IO;

namespace ISO9660Lib;

/// <summary>
/// Represents a sink for logging
/// </summary>
/// <param name="logSink">The stream to write to</param>
public class LogWriter(Stream logSink)
{
  /// <summary>
  /// Should errors be written?
  /// </summary>
  public bool AllowError = true;

  /// <summary>
  /// Should messages be written?
  /// </summary>
  public bool AllowMessage = true;

  private readonly TextWriter _logSink = new StreamWriter(logSink);

  /// <summary>
  /// Writes a log message to the error stream
  /// </summary>
  /// <param name="message"></param>
  public void LogError(object message)
  {
    if (AllowError)
      WriteToLog("ERR", message);
  }

  /// <summary>
  /// Writes a log message to the message stream
  /// </summary>
  /// <param name="message"></param>
  public void LogMessage(object message)
  {
    if (AllowMessage)
      WriteToLog("MSG", message);
  }

  private void WriteToLog(string code, object message)
  {
    _logSink.WriteLine($"[{code}] {message}");
    _logSink.Flush();
  }

}
