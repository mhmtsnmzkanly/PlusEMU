using Dapper;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.UserData;
using System.Diagnostics;

namespace Plus.HabboHotel.Users.Authentication;

internal class Authenticator : IAuthenticator
{
    private readonly IEnumerable<IAuthenticationTask> _authenticationTasks;
    private readonly IGameClientManager _gameClientManager;
    private readonly IUserDataFactory _userDataFactory;
    private readonly IDatabase _database;

    public Authenticator(IEnumerable<IAuthenticationTask> authenticationTasks, IGameClientManager gameClientManager, IUserDataFactory userDataFactory, IDatabase database)
    {
        _authenticationTasks = authenticationTasks;
        _gameClientManager = gameClientManager;
        _userDataFactory = userDataFactory;
        _database = database;
    }

    public async Task<AuthenticationError?> AuthenticateUsingSSO(GameClient session, string sso)
    {
        sso = sso.Trim();
        if (string.IsNullOrEmpty(sso))
            return AuthenticationError.EmptySSO;

        if (!Debugger.IsAttached && sso.Length < 15)
            return AuthenticationError.InvalidSSO;

        var userId = await GetUserIdFromSso(sso);
        if (userId == default)
            return AuthenticationError.NoAccountFound;

        if (!Debugger.IsAttached)
            await ResetSso(userId);

        var canLogin = await CanLogin(userId);
        if (!canLogin)
            return AuthenticationError.LoginProhibited;

        var habbo = await TryCreateHabbo(userId);
        if (habbo == null)
            return AuthenticationError.NoAccountFound;

        AttachDisconnectLifecycle(habbo);
        BindAuthenticatedSession(session, habbo);
        await CompleteLogin(habbo);
        return null;
    }

    private Task<Habbo?> TryCreateHabbo(int userId) => _userDataFactory.Create(userId);

    private void AttachDisconnectLifecycle(Habbo habbo)
    {
        habbo.Disconnected += async (_, _) => await OnHabboDisconnected(habbo);
    }

    private void BindAuthenticatedSession(GameClient session, Habbo habbo)
    {
        session.SetHabbo(habbo);

        habbo.AttachClient(session);
        _gameClientManager.RegisterClient(session, habbo.Id, habbo.Username);
    }

    private Task CompleteLogin(Habbo habbo) => RaiseHabboLoggedIn(habbo);

    private async Task<int> GetUserIdFromSso(string sso)
    {
        using var connection = _database.Connection();
        return await connection.ExecuteScalarAsync<int>("SELECT id FROM users WHERE auth_ticket = @sso", new { sso });
    }

    private async Task ResetSso(int userId)
    {
        using var connection = _database.Connection();
        await connection.ExecuteAsync("UPDATE users SET auth_ticket = NULL WHERE id = @userId", new { userId });
    }

    private async Task<bool> CanLogin(int userId)
    {
        foreach (var task in _authenticationTasks)
        {
            if (!(await task.CanLogin(userId)))
                return false;
        }
        return true;
    }

    private async Task RaiseHabboLoggedIn(Habbo habbo)
    {
        foreach (var task in _authenticationTasks)
            await task.UserLoggedIn(habbo);
    }

    private async Task OnHabboDisconnected(Habbo habbo)
    {
        foreach (var task in _authenticationTasks)
            await task.UserLoggedOut(habbo);
    }
}
