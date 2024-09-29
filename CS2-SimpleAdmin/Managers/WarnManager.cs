﻿using CS2_SimpleAdminApi;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CS2_SimpleAdmin.Managers;

internal class WarnManager(Database.Database database)
{
    public async Task WarnPlayer(PlayerInfo player, PlayerInfo? issuer, string reason, int time = 0)
    {
        var now = Time.ActualDateTime();
        var futureTime = now.AddMinutes(time);

        try
        {
            await using var connection = await database.GetConnectionAsync();
            const string sql =
                "INSERT INTO `sa_warns` (`player_steamid`, `player_name`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `server_id`) " +
                               "VALUES (@playerSteamid, @playerName, @adminSteamid, @adminName, @muteReason, @duration, @ends, @created, @serverid)";

            await connection.ExecuteAsync(sql, new
            {
                playerSteamid = player.SteamId.SteamId64.ToString(),
                playerName = player.Name,
                adminSteamid = issuer?.SteamId.SteamId64.ToString() ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                adminName = issuer?.Name ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                muteReason = reason,
                duration = time,
                ends = futureTime,
                created = now,
                serverid = CS2_SimpleAdmin.ServerId
            });
        }
        catch { };
    }

    public async Task AddWarnBySteamid(string playerSteamId, PlayerInfo? issuer, string reason, int time = 0)
    {
        if (string.IsNullOrEmpty(playerSteamId)) return;


        var now = Time.ActualDateTime();
        var futureTime = now.AddMinutes(time);

        try
        {
            await using var connection = await database.GetConnectionAsync();
            const string sql = "INSERT INTO `sa_warns` (`player_steamid`, `admin_steamid`, `admin_name`, `reason`, `duration`, `ends`, `created`, `server_id`) " +
                               "VALUES (@playerSteamid, @adminSteamid, @adminName, @muteReason, @duration, @ends, @created, @serverid)";

            await connection.ExecuteAsync(sql, new
            {
                playerSteamid = playerSteamId,
                adminSteamid = issuer?.SteamId.ToString() ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                adminName = issuer?.Name ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console",
                muteReason = reason,
                duration = time,
                ends = futureTime,
                created = now,
                serverid = CS2_SimpleAdmin.ServerId
            });
        }
        catch { };
    }

    public async Task<List<dynamic>> GetPlayerWarns(PlayerInfo player, bool active = true)
    {
        try
        {
            await using var connection = await database.GetConnectionAsync();

            string sql;

            if (CS2_SimpleAdmin.Instance.Config.MultiServerMode)
            {
                sql = active
                    ? "SELECT * FROM sa_warns WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE' ORDER BY id DESC"
                    : "SELECT * FROM sa_warns WHERE player_steamid = @PlayerSteamID ORDER BY id DESC";
            }
            else
            {
                sql = active
                    ? "SELECT * FROM sa_warns WHERE player_steamid = @PlayerSteamID AND server_id = @serverid AND status = 'ACTIVE' ORDER BY id DESC"
                    : "SELECT * FROM sa_warns WHERE player_steamid = @PlayerSteamID AND server_id = @serverid ORDER BY id DESC";
            }

            var parameters = new { PlayerSteamID = player.SteamId.SteamId64.ToString(), serverid = CS2_SimpleAdmin.ServerId };
            var warns = await connection.QueryAsync<dynamic>(sql, parameters);

            return warns.ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<int> GetPlayerWarnsCount(string steamId, bool active = true)
    {
        try
        {
            await using var connection = await database.GetConnectionAsync();

            var sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode
                ? active
                    ? "SELECT COUNT(*) FROM sa_warns WHERE player_steamid = @PlayerSteamID AND status = 'ACTIVE'"
                    : "SELECT COUNT(*) FROM sa_warns WHERE player_steamid = @PlayerSteamID"
                : active
                    ? "SELECT COUNT(*) FROM sa_warns WHERE player_steamid = @PlayerSteamID AND server_id = @serverid  AND status = 'ACTIVE'"
                    : "SELECT COUNT(*) FROM sa_warns WHERE player_steamid = @PlayerSteamID'";

            var muteCount = await connection.ExecuteScalarAsync<int>(sql, new { PlayerSteamID = steamId, serverid = CS2_SimpleAdmin.ServerId });
            return muteCount;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public async Task UnwarnPlayer(PlayerInfo player, int warnId)
    {
        try
        {
            await using var connection = await database.GetConnectionAsync();

            var sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode
                ? "UPDATE sa_warns SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND player_steamid = @steamid AND id = @warnId"
                : "UPDATE sa_warns SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND player_steamid = @steamid AND id = @warnId AND server_id = @serverid";

            await connection.ExecuteAsync(sql, new { steamid = player.SteamId.SteamId64.ToString(), warnId, serverid = CS2_SimpleAdmin.ServerId });
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogCritical($"Unable to remove warn + {ex}");
        }
    }
    
    public async Task UnwarnPlayer(string playerPattern)
    {
        try
        {
            await using var connection = await database.GetConnectionAsync();

            var sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode
                ? "UPDATE sa_warns SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND player_steamid = @steamid AND id = (SELECT MAX(id) FROM sa_warns WHERE player_steamid = @steamid AND status = 'ACTIVE')"
                : "UPDATE sa_warns SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND player_steamid = @steamid AND id = (SELECT MAX(id) FROM sa_warns WHERE player_steamid = @steamid AND status = 'ACTIVE' AND server_id = @serverid)";

            await connection.ExecuteAsync(sql, new { steamid = playerPattern, serverid = CS2_SimpleAdmin.ServerId });
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogCritical($"Unable to remove last warn + {ex}");
        }
    }

    public async Task ExpireOldWarns()
    {
        try
        {
            await using var connection = await database.GetConnectionAsync();

            var sql = CS2_SimpleAdmin.Instance.Config.MultiServerMode
                ? "UPDATE sa_warns SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime"
                : "UPDATE sa_warns SET status = 'EXPIRED' WHERE status = 'ACTIVE' AND `duration` > 0 AND ends <= @CurrentTime AND server_id = @serverid";

            await connection.ExecuteAsync(sql, new { CurrentTime = Time.ActualDateTime(), serverid = CS2_SimpleAdmin.ServerId });
        }
        catch (Exception ex)
        {
            CS2_SimpleAdmin._logger?.LogCritical($"Unable to remove expired warns + {ex}");
        }
    }
}