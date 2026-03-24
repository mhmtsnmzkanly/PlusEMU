using System.Data;
using Plus.Database;

namespace Plus.HabboHotel.Rooms.Chat.Pets.Commands;

public class PetCommandManager : IPetCommandManager
{
    private readonly IDatabase _database;
    private readonly Dictionary<string, string> _commandDatabase;
    private readonly Dictionary<int, string> _commandRegister;
    private readonly Dictionary<string, PetCommand> _petCommands;

    public PetCommandManager(IDatabase database)
    {
        _database = database;
        _petCommands = new();
        _commandRegister = new();
        _commandDatabase = new();
    }

    public void Init()
    {
        _petCommands.Clear();
        _commandRegister.Clear();
        _commandDatabase.Clear();
        DataTable? table = null;
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("SELECT * FROM `bots_pet_commands`");
            table = dbClient.GetTable();
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    var commandKey = row[1].ToString();
                    var commandInput = row[2].ToString();
                    if (string.IsNullOrEmpty(commandKey) || string.IsNullOrEmpty(commandInput))
                        continue;
                    _commandRegister.Add(Convert.ToInt32(row[0]), commandKey);
                    _commandDatabase.Add($"{commandKey}.input", commandInput);
                }
            }
        }
        foreach (var (commandId, commandStringedId) in _commandRegister)
        {
            var commandInput = _commandDatabase[$"{commandStringedId}.input"].Split(',');
            foreach (var command in commandInput) _petCommands.Add(command, new(commandId, command));
        }
    }

    public int TryInvoke(string input)
    {
        if (_petCommands.TryGetValue(input.ToLower(), out var command))
            return command.Id;
        return 0;
    }
}
