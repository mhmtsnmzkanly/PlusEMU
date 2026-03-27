using Plus.Core.Settings;
using System.Threading.Tasks;

namespace Plus.Communication.RCON.Commands.Hotel;

internal class ReloadServerSettingsCommand : IRconCommand
{
    private readonly ISettingsManager _settingsManager;

    public string Description => "This command is used to reload the server settings.";

    public string Key => "reload_server_settings";
    public string Parameters => "";

    public ReloadServerSettingsCommand(ISettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
    }

    public async Task<bool> TryExecute(string[] parameters)
    {
        await _settingsManager.Reload();
        return true;
    }
}