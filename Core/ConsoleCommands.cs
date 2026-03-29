using NLog;

namespace Plus.Core;

internal sealed class ConsoleCommandHandler : IConsoleCommandHandler
{
    private static readonly ILogger Log = LogManager.GetLogger("Plus.Core.ConsoleCommands");
    private readonly IRuntimeControlService _runtimeControlService;

    public ConsoleCommandHandler(IRuntimeControlService runtimeControlService)
    {
        _runtimeControlService = runtimeControlService;
    }

    public void InvokeCommand(string inputData)
    {
        if (string.IsNullOrEmpty(inputData))
            return;
        try
        {
            var parameters = inputData.Split(' ');
            switch (parameters[0].ToLower())
            {
                case "stop":
                case "shutdown":
                {
                    Log.Warn("The server is saving users furniture, rooms, etc. WAIT FOR THE SERVER TO CLOSE, DO NOT EXIT THE PROCESS IN TASK MANAGER!!");
                    _runtimeControlService.PerformShutdown("Console command: shutdown");
                    break;
                }
                case "alert":
                {
                    var notice = inputData.Substring(6);
                    _runtimeControlService.BroadcastAlert(notice);
                    Log.Info("Alert successfully sent.");
                    break;
                }
                default:
                {
                    Log.Error($"{parameters[0].ToLower()} is an unknown or unsupported command. Type help for more information");
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error in command [{inputData}]: {e}");
        }
    }
}
