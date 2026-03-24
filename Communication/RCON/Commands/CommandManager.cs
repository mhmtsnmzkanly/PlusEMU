namespace Plus.Communication.RCON.Commands;

public class CommandManager : ICommandManager
{
    /// <summary>
    /// Commands registered for use.
    /// </summary>
    private readonly Dictionary<string, IRconCommand> _commands;

    /// <summary>
    /// The default initializer for the CommandManager
    /// </summary>
    public CommandManager(IEnumerable<IRconCommand> commands)
    {
        _commands = commands.ToDictionary(command => command.Key);
    }

    /// <summary>
    /// Request the text to parse and check for commands that need to be executed.
    /// </summary>
    /// <param name="data">A string of data split by char(1), the first part being the command and the second part being the parameters.</param>
    /// <returns>True if parsed or false if not.</returns>
    public bool Parse(string data)
    {
        if (string.IsNullOrEmpty(data))
            return false;
        var segments = data.Split(Convert.ToChar(1));
        if (segments.Length == 0)
            return false;

        var cmd = segments[0];
        if (_commands.TryGetValue(cmd.ToLower(), out var command))
        {
            string[] parameters = Array.Empty<string>();
            if (segments.Length > 1 && !string.IsNullOrEmpty(segments[1]))
            {
                var param = segments[1];
                parameters = param.Split(':');
            }
            return command.TryExecute(parameters).Result;
        }
        return false;
    }
}
