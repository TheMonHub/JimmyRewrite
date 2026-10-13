// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026 TheMonHub

using System.Reflection;
using Microsoft.Data.Sqlite;

namespace JimmyRewrite.Database;

public static class Database
{
    private const string ConnectionString = "Data Source=jimmy.db;";
    private const long DatabaseVersion = 1;
    private static readonly Rule NoRule = new(0, "No Rule", null, "000000", null);
    private static readonly SqliteConnection Connection = GetOpenedSqliteConnection();

    private static SqliteConnection GetOpenedSqliteConnection()
    {
        var newConnection = new SqliteConnection(ConnectionString);
        newConnection.Open();
        
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.CheckVersion.sql", newConnection);
        var version = (long)command.ExecuteScalar()!;
        if (version != DatabaseVersion && version != 0) throw new Exception("Database version mismatch!");
        
        using var command2 = GetSqliteCommand("JimmyRewrite.Database.Statements.Init.sql", newConnection);
        command2.Parameters.AddWithValue("DatabaseVersion", DatabaseVersion);
        command2.ExecuteNonQuery();
        
        return newConnection;
    }

    private static SqliteCommand GetSqliteCommand(string scriptManifestResourceName, SqliteConnection? connection = null)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(scriptManifestResourceName);
        if (stream == null) throw new Exception("Could not find database statements!");
        using var reader = new StreamReader(stream);
        var sql = reader.ReadToEnd();
        
        return new SqliteCommand(sql, connection ?? Connection);
    }

    private static void CreateGuildDatIfNotExists(ulong guildId)
    {
        using var makeGuildTable = GetSqliteCommand("JimmyRewrite.Database.Statements.CreateGuildDatIfNotExists.sql");
        makeGuildTable.Parameters.AddWithValue("GuildId", guildId);
        makeGuildTable.ExecuteNonQuery();
    }

    private static void CreateMemberIfNotExists(ulong guildId, ulong userId)
    {
        using var makeMemberTable = GetSqliteCommand("JimmyRewrite.Database.Statements.CreateMemberIfNotExists.sql");
        makeMemberTable.Parameters.AddWithValue("GuildId", guildId);
        makeMemberTable.Parameters.AddWithValue("UserId", userId);
        makeMemberTable.ExecuteNonQuery();
    }
    
    public static void SetLogMessageChannel(ulong channelId, ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.SetLogMessageChannel.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("ChannelId", channelId);
        command.ExecuteNonQuery();
    }
    
    public static void SetLogWatchChannel(ulong channelId, ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.SetLogWatchChannel.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("ChannelId", channelId);
        command.ExecuteNonQuery();
    }
    
    public static void SetLogModChannel(ulong channelId, ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.SetLogModChannel.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("ChannelId", channelId);
        command.ExecuteNonQuery();   
    }
    
    public static void SetAppealLink(string text, ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.SetAppealLink.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("Text", text);
        command.ExecuteNonQuery();
    }

    public static void SetForceNote(bool value, ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.SetForceNote.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("Value", value ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public static void SetAllowNoRule(bool value, ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.SetAllowNoRule.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("Value", value ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public static void SetModActionConfirm(bool value, ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.SetModActionConfirm.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("Value", value ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public static string? GetAppealLink(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetAppealLink.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        var result = command.ExecuteScalar();
        if (result == DBNull.Value || result == null) return null;
        return (string?)result;
    }

    public static bool GetForceNote(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetForceNote.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        var result = command.ExecuteScalar();
        return (long?)result == 1;
    }

    public static bool GetAllowNoRule(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetAllowNoRule.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        var result = command.ExecuteScalar();
        return (long?)result == 1;
    }

    public static bool GetModActionConfirm(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetModActionConfirm.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        var result = command.ExecuteScalar();
        return (long?)result == 1;
    }
    
    public static ulong? GetLogMessageChannel(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetLogMessageChannel.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        var result = command.ExecuteScalar();
        if (result == DBNull.Value || result == null) return null;
        return (ulong?)(long?)result;
    }
    
    public static ulong? GetLogWatchChannel(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetLogWatchChannel.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        var result = command.ExecuteScalar();
        if (result == DBNull.Value || result == null) return null;
        return (ulong?)(long?)result;
    }
    
    public static ulong? GetLogModChannel(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetLogModChannel.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        var result = command.ExecuteScalar();
        if (result == DBNull.Value || result == null) return null;
        return (ulong?)(long?)result;
    }

    public static void ResetLogMessageChannel(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.ResetLogMessageChannel.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.ExecuteNonQuery();
    }

    public static void ResetLogWatchChannel(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.ResetLogWatchChannel.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.ExecuteNonQuery();
    }

    public static void ResetLogModChannel(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.ResetLogModChannel.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.ExecuteNonQuery();
    }

    public static void ResetAppealLink(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.ResetAppealLink.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.ExecuteNonQuery();
    }

    public static void SetLogWelcomeChannel(ulong channelId, ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.SetLogWelcomeChannel.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("ChannelId", channelId);
        command.ExecuteNonQuery();
    }

    public static void ResetLogWelcomeChannel(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.ResetLogWelcomeChannel.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.ExecuteNonQuery();
    }

    public static ulong? GetLogWelcomeChannel(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetLogWelcomeChannel.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        var result = command.ExecuteScalar();
        if (result == DBNull.Value || result == null) return null;
        return (ulong?)(long?)result;
    }
    
    public static void SetMemberWatch(ulong guildId, ulong userId, bool value)
    {
        CreateGuildDatIfNotExists(guildId);
        CreateMemberIfNotExists(guildId, userId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.SetMemberWatch.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("UserId", userId);
        command.Parameters.AddWithValue("Watch", value ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public static bool GetMemberWatch(ulong guildId, ulong userId)
    {
        CreateGuildDatIfNotExists(guildId);
        CreateMemberIfNotExists(guildId, userId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetMemberWatch.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("UserId", userId);
        var result = command.ExecuteScalar();
        return (long?)result == 1;
    }
    
    public static void AddTBan(ulong guildId, ulong userId, long expire)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.AddTBan.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("UserId", userId);
        command.Parameters.AddWithValue("Expire", expire);
        command.ExecuteNonQuery();
    }

    /// <exception cref="Exception">
    /// The rule does not exist.
    /// </exception>
    public static Case AddCase(ulong guildId, ulong userId, ModerationHandler.PunishmentType type, int[] ruleIds, ulong modUid, string? note, long time, long? tBanExpire, bool? dmSuccess = false, string? image = null)
    {
        CreateGuildDatIfNotExists(guildId);
        
        var rules = new Rule[ruleIds.Length];
        for (var i = 0; i < ruleIds.Length; i++)
        {
            rules[i] = GetRule(guildId, ruleIds[i]) ?? throw new Exception($"Rule {ruleIds[i]} does not exist!");
        }

        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.AddCase.sql");

        var ruleTitle = string.Join(" | ", rules.Select(r => r.Title));
        
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("UserId", userId);
        command.Parameters.AddWithValue("Type", (int)type);
        command.Parameters.AddWithValue("RuleId", string.Join(",", ruleIds));
        command.Parameters.AddWithValue("RuleTitle", ruleTitle);
        command.Parameters.AddWithValue("ModUid", modUid);
        command.Parameters.AddWithValue("Note", (object?)note ?? DBNull.Value);
        command.Parameters.AddWithValue("Time", time);
        command.Parameters.AddWithValue("TBanExpire", (object?)tBanExpire ?? DBNull.Value);
        command.Parameters.AddWithValue("DMSuccess", dmSuccess);
        command.Parameters.AddWithValue("Img", (object?)image ?? DBNull.Value);
        command.ExecuteNonQuery();
        
        return new Case(0, userId, type, ruleIds, ruleTitle, modUid, note, time, tBanExpire, dmSuccess, image);
    }

    public static void ClearCases(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.ClearCases.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("UserId", guildId);
        command.ExecuteNonQuery();
    }

    public static List<(ulong GuildId, ulong UserId)> RemoveExpiredTBans()
    {
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.RemoveExpiredTBans.sql");
        command.Parameters.AddWithValue("Time", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        using var reader = command.ExecuteReader();
        
        var results = new List<(ulong GuildId, ulong UserId)>();
        while (reader.Read())
        {
            var gid = (ulong)reader.GetInt64(0);
            var uid = (ulong)reader.GetInt64(1);
            results.Add((gid, uid));
        }

        return results;
    }
    
    public static long GetTBanStatus(ulong guildId, ulong userId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetTBanStatus.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("UserId", userId);
        var result = command.ExecuteScalar();
        return (long?)result ?? 0;
    }
    
    public static void RemoveTBan(ulong guildId, ulong userId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.RemoveTBan.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("UserId", userId);
        command.ExecuteNonQuery();
    }
    
    public static int GetCurrentRuleId(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetCurrentRuleId.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        var result = command.ExecuteScalar();
        if (result == null || result == DBNull.Value) return 0;
        return (int)(long)result;
    }

    public static List<Rule> GetRules(ulong guildId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetRules.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        using var reader = command.ExecuteReader();
        
        var results = new List<Rule>();
        while (reader.Read())
        {
            var rid = reader.GetInt32(0);
            var title = reader.GetString(1);
            var desc = reader.IsDBNull(2) ? null : reader.GetString(2);
            var color = reader.GetString(3);
            var img = reader.IsDBNull(4) ? null : reader.GetString(4);
            results.Add(new Rule(rid, title, desc, color, img));
        }

        return results;
    }

    public static Rule? GetRule(ulong guildId, int ruleId)
    {
        if (ruleId == 0) return NoRule;
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.GetRule.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("RuleId", ruleId);
        using var reader = command.ExecuteReader();

        if (!reader.Read()) return null;
        var rid = reader.GetInt32(0);
        var title = reader.GetString(1);
        var desc = reader.IsDBNull(2) ? null : reader.GetString(2);
        var color = reader.GetString(3);
        var img = reader.IsDBNull(4) ? null : reader.GetString(4);
        return new Rule(rid, title, desc, color, img);
    }

    public static bool RuleExists(ulong guildId, int ruleId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.RuleExists.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("RuleId", ruleId);
        var result = command.ExecuteScalar();
        return result != null && result != DBNull.Value;
    }

    public static void AddRule(ulong guildId, string title, string color, string? desc = null, string? img = null)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.AddRule.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("Title", title);
        command.Parameters.AddWithValue("Desc", (object?)desc ?? DBNull.Value);
        command.Parameters.AddWithValue("Color", color);
        command.Parameters.AddWithValue("Img", (object?)img ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public static void AddRule(ulong guildId, Rule rule)
    {
        AddRule(guildId, rule.Title, rule.Color, rule.Description, rule.Image);
    }

    public static void ModifyRule(ulong guildId, int ruleId, string title, string color, string? desc = null, string? img = null)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.ModifyRule.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("RuleId", ruleId);
        command.Parameters.AddWithValue("Title", title);
        command.Parameters.AddWithValue("Desc", (object?)desc ?? DBNull.Value);
        command.Parameters.AddWithValue("Color", color);
        command.Parameters.AddWithValue("Img", (object?)img ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public static void ModifyRule(ulong guildId, Rule rule)
    {
        ModifyRule(guildId, rule.Id, rule.Title, rule.Color, rule.Description, rule.Image);
    }

    public static void RemoveRule(ulong guildId, int ruleId)
    {
        CreateGuildDatIfNotExists(guildId);
        using var command = GetSqliteCommand("JimmyRewrite.Database.Statements.RemoveRule.sql");
        command.Parameters.AddWithValue("GuildId", guildId);
        command.Parameters.AddWithValue("RuleId", ruleId);
        command.ExecuteNonQuery();
    }
}