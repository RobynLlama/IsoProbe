using System;
using System.Collections.Generic;
using PSXtractor.Context;
using SimpleCommandLib;

namespace PSXtractor.Commands;

public class InteractiveDispatcher : CommandDispatcher
{
  public readonly static FileSystemContext Environment = new();
  protected override Dictionary<string, ICommandRunner> CommandsMap { get => _commands; set { } }
  private readonly Dictionary<string, ICommandRunner> _commands = new(StringComparer.InvariantCultureIgnoreCase);

  public InteractiveDispatcher()
  {
    TryAddCommand(new CommandLoadDisk());
    TryAddCommand(new CommandCloseDisk());
    TryAddCommand(new CommandChangeDirectory());
    TryAddCommand(new CommandListDirectory());
    TryAddCommand(new CommandExit());
  }

  public override void OnCommandNotFound(string commandName)
  {
    Console.WriteLine($"Command not found {commandName}");
  }
}
