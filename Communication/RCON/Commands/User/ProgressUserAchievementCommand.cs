using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.RCON.Commands.User;

internal class ProgressUserAchievementCommand : IRconCommand
{
    private readonly IGameClientManager _gameClientManager;
    private readonly IAchievementService _achievementService;
    public string Description => "This command is used to progress a users achievement.";

    public string Key => "progress_user_achievement";
    public string Parameters => "%userId% %achievement% %progess%";

    public ProgressUserAchievementCommand(IGameClientManager gameClientManager, IAchievementService achievementService)
    {
        _gameClientManager = gameClientManager;
        _achievementService = achievementService;
    }

    public async Task<bool> TryExecute(string[] parameters)
    {
        if (!int.TryParse(parameters[0], out var userId))
            return false;
        var client = _gameClientManager.GetClientByUserId(userId);
        if (client == null || client.GetHabbo() == null)
            return false;

        // Validate the achievement
        if (string.IsNullOrEmpty(Convert.ToString(parameters[1])))
            return false;
        var achievement = Convert.ToString(parameters[1]);

        // Validate the progress
        if (!int.TryParse(parameters[2], out var progress))
            return false;
        
        await _achievementService.ProgressAchievement(client, achievement, progress);
        return true;
    }
}