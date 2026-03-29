using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Communication.Packets;
using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.Communication.Packets.Outgoing.Notifications;
using Plus.Database;
using Plus.HabboHotel.Users.Messenger;
using Plus.Core;

namespace Plus.HabboHotel.GameClients;

public class GameClientManager : IGameClientManager
{
    private readonly IDatabase _database;
    private readonly ILogger<GameClientManager> _logger;
    private readonly IServerStatusSignal _serverStatusSignal;

    private readonly Stopwatch _clientPingStopwatch;

    private readonly ConcurrentDictionary<int, GameClient> _clients;

    private readonly Queue _timedOutConnections;
    private readonly ConcurrentDictionary<int, GameClient> _userIdRegister;
    private readonly ConcurrentDictionary<string, GameClient> _usernameRegister;

    public GameClientManager(IDatabase database, ILogger<GameClientManager> logger, IServerStatusSignal serverStatusSignal)
    {
        _database = database;
        _logger = logger;
        _serverStatusSignal = serverStatusSignal;
        _clients = new();
        _userIdRegister = new();
        _usernameRegister = new();
        _timedOutConnections = new();
        _clientPingStopwatch = new();
        _clientPingStopwatch.Start();
    }

    public int Count => _clients.Count;

    public ICollection<GameClient> GetClients => _clients.Values;

    public void OnCycle()
    {
        TestClientConnections();
        HandleTimeouts();
    }

    public GameClient? GetClientByUserId(int userId) => _userIdRegister.TryGetValue(userId, out var client) ? client : null;

    public GameClient? GetClientByUsername(string username)
    {
        var normalizedUsername = username.ToLower();
        return _usernameRegister.TryGetValue(normalizedUsername, out var client) ? client : null;
    }

    public bool TryGetClient(int clientId, out GameClient? client) => _clients.TryGetValue(clientId, out client);

    public bool UpdateClientUsername(GameClient client, string oldUsername, string newUsername)
    {
        if (client == null || !_usernameRegister.ContainsKey(oldUsername.ToLower()))
            return false;
        _usernameRegister.TryRemove(oldUsername.ToLower(), out _);
        _usernameRegister.TryAdd(newUsername.ToLower(), client);
        return true;
    }

    public async Task<string> GetNameById(int id)
    {
        var client = GetClientByUserId(id);
        var habbo = client?.GetHabbo();
        if (habbo != null)
            return habbo.Username;
        using var connection = _database.Connection();
        return await connection.QuerySingleOrDefaultAsync<string>("SELECT username FROM users WHERE id = @id LIMIT 1", new { id }) ?? string.Empty;
    }

    public IEnumerable<GameClient> GetClientsById(Dictionary<int, MessengerBuddy>.KeyCollection users)
    {
        foreach (var id in users)
        {
            var client = GetClientByUserId(id);
            if (client != null)
                yield return client;
        }
    }

    public void StaffAlert(IServerPacket message, int exclude = 0)
    {
        foreach (var client in GetClients.ToList())
        {
            if (client == null || client.GetHabbo() == null)
                continue;
            if (client.GetHabbo().Rank < 2 || client.GetHabbo().Id == exclude)
                continue;
            client.Send(message);
        }
    }

    public void ModAlert(string message)
    {
        foreach (var client in GetClients.ToList())
        {
            if (client == null)
                continue;
            var habbo = client?.GetHabbo();
            var permissions = habbo?.Permissions;
            if (habbo == null || permissions == null)
                continue;
            if (permissions.HasRight("mod_tool") && !permissions.HasRight("staff_ignore_mod_alert"))
            {
                try
                {
                    //client.SendWhisper(message, 5);
                }
                catch { }
            }
        }
    }

    public void DoAdvertisingReport(GameClient reporter, GameClient target)
    {
        if (reporter == null || target == null || reporter.GetHabbo() == null || target.GetHabbo() == null)
            return;
        var builder = new StringBuilder();
        builder.Append("New report submitted!\r\r");
        builder.Append($"Reporter: {reporter.GetHabbo().Username}\r");
        builder.Append($"Reported User: {target.GetHabbo().Username}\r\r");
        builder.Append($"{target.GetHabbo().Username}s last 10 messages:\r\r");
        
        using (var connection = _database.Connection())
        {
            var logs = connection.Query<string>("SELECT `message` FROM `chatlogs` WHERE `user_id` = @id ORDER BY `id` DESC LIMIT 10", new { id = target.GetHabbo().Id });
            var number = 11;
            foreach (var message in logs)
            {
                number -= 1;
                builder.Append($"{number}: {message}\r");
            }
        }
        
        foreach (var client in GetClients.ToList())
        {
            var habbo = client.GetHabbo();
            var permissions = habbo?.Permissions;
            if (habbo == null || permissions == null)
                continue;
            if (permissions.HasRight("mod_tool") && !permissions.HasRight("staff_ignore_advertisement_reports"))
                client.Send(new MotdNotificationComposer(builder.ToString()));
        }
    }


    public void SendPacket(IServerPacket packet, string fuse = "")
    {
        foreach (var client in _clients.Values.ToList())
        {
            var habbo = client.GetHabbo();
            if (habbo == null)
                continue;
            if (!string.IsNullOrEmpty(fuse))
            {
                if (!(habbo.Permissions?.HasRight(fuse) ?? false))
                    continue;
            }
            client.Send(packet);
        }
    }

    public void LogClonesOut(int userId)
    {
        var client = GetClientByUserId(userId);
        if (client != null)
            client.Disconnect();
    }

    public void RegisterClient(GameClient client, int userId, string username)
    {
        if (_usernameRegister.ContainsKey(username.ToLower()))
            _usernameRegister[username.ToLower()] = client;
        else
            _usernameRegister.TryAdd(username.ToLower(), client);
        if (_userIdRegister.ContainsKey(userId))
            _userIdRegister[userId] = client;
        else
            _userIdRegister.TryAdd(userId, client);
        _serverStatusSignal.MarkDirty();
    }

    public void UnregisterClient(int userid, string username)
    {
        _userIdRegister.TryRemove(userid, out _);
        _usernameRegister.TryRemove(username.ToLower(), out _);
        _serverStatusSignal.MarkDirty();
    }

    public void CloseAll()
    {
        foreach (var client in GetClients.ToList())
        {
            var habbo = client.GetHabbo();
            if (habbo != null)
            {
                try
                {
                    using (var connection = _database.Connection())
                    {
                        connection.Execute(habbo.GetQueryString);
                    }
                    Console.Clear();
                    _logger.LogInformation("<<- SERVER SHUTDOWN ->> IVNENTORY IS SAVING");
                }
                catch { }
            }
        }
        _logger.LogInformation("Done saving users inventory!");
        _logger.LogInformation("Closing server connections...");
        if (_clients.Count > 0)
            _clients.Clear();
        _logger.LogInformation("Connections closed!");
    }

    private void TestClientConnections()
    {
        if (_clientPingStopwatch.ElapsedMilliseconds >= 30000)
        {
            _clientPingStopwatch.Restart();
            try
            {
                var toPing = new List<GameClient>();
                foreach (var client in _clients.Values.ToList())
                {
                }
                var start = DateTime.Now;
                foreach (var client in toPing.ToList())
                {
                    try
                    {
                        client.Send(new PongComposer());
                    }
                    catch
                    {
                        lock (_timedOutConnections.SyncRoot)
                        {
                            _timedOutConnections.Enqueue(client);
                        }
                    }
                }
            }
            catch (Exception)
            {
                //ignored
            }
        }
    }

    private void HandleTimeouts()
    {
        if (_timedOutConnections.Count > 0)
        {
            lock (_timedOutConnections.SyncRoot)
            {
                while (_timedOutConnections.Count > 0)
                {
                    GameClient? client = null;
                    if (_timedOutConnections.Count > 0 && _timedOutConnections.Dequeue() is GameClient timedOutClient)
                        client = timedOutClient;
                    if (client != null)
                        client.Disconnect();
                }
            }
        }
    }
}
