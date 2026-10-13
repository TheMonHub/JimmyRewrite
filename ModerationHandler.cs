// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026 TheMonHub

using System.Net;
using Humanizer;
using JimmyRewrite.Database;
using NetCord;
using NetCord.Rest;

namespace JimmyRewrite;

public static class ModerationHandler
{
    public enum PunishmentType
    {
        Ban = 1,
        Kick = 2,
        Timeout = 3,
        Warn = 4,
        Appeal = 5
    }

    public enum ParamPunishmentType
    {
        Ban = 1,
        Kick = 2,
        Timeout = 3,
        Warn = 4,
    }

    public class InvalidRules : Exception;

    public class NoRuleAllowedIsOff : Exception;

    public static PunishmentType GetPunishmentType(ParamPunishmentType paramPunishmentType)
    {
        return (PunishmentType)paramPunishmentType;
    }

    public static string GetPunishmentTypeEd(PunishmentType type)
    {
        return type switch
        {
            PunishmentType.Ban => "Banned",
            PunishmentType.Kick => "Kicked",
            PunishmentType.Timeout => "Timed Out",
            PunishmentType.Warn => "Warned",
            PunishmentType.Appeal => "Appealed",
            _ => "Unknown"
        };
    }
    
    public static async Task LogConfigChange(ulong guildId, User who, string what, string? from, string to, RestClient restClient)
    {
        var modLogChannel = Database.Database.GetLogModChannel(guildId);
        if (modLogChannel == null)
        {
            return;
        }
        
        var embed = new EmbedProperties()
            .WithTitle("Configuration Changed")
            .WithFields([
                new EmbedFieldProperties()
                    .WithName("User")
                    .WithValue($"<@{who.Id}> ({who.Username} | {who.Id})").WithInline(),
                new EmbedFieldProperties()
                    .WithName("Configuration")
                    .WithValue(what).WithInline(),
                new EmbedFieldProperties()
                    .WithName("From")
                    .WithValue(from ?? "*There was no previous configuration*"),
                new EmbedFieldProperties()
                    .WithName("To")
                    .WithValue(to)
            ]).WithTimestamp(DateTime.UtcNow);
        
        var messageProperties = new MessageProperties().AddEmbeds(embed);

        try
        {
            await restClient.SendMessageAsync((ulong)modLogChannel, messageProperties);
        }
        catch (RestException e)
        {
            if (e.StatusCode != HttpStatusCode.NotFound && e.StatusCode != HttpStatusCode.Forbidden)
            {
                throw;
            }
        }
    }

    public static async Task LogTempUnban(ulong guildId, ulong who, RestClient client)
    {
        var user = await client.GetUserAsync(who);
        var dmChannel = await user.GetDMChannelAsync();
        var guild = await client.GetGuildAsync(guildId);
        
        var dmEmbed = new EmbedProperties()
            .WithTitle($"Your Temporary Ban From {guild.Name} Has Expired")
            .WithTimestamp(DateTime.UtcNow)
            .WithColor(Colors.Blue);
        
        var dmMessageProperties = new MessageProperties().AddEmbeds(dmEmbed);
        
        try
        {
            await dmChannel.SendMessageAsync(dmMessageProperties);
        }
        catch (RestException e)
        {
            if (e.StatusCode != HttpStatusCode.NotFound && e.StatusCode != HttpStatusCode.Forbidden)
            {
                throw;
            }
        }
        
        var logModChannel = Database.Database.GetLogModChannel(guildId);
        if (logModChannel == null)
        {
            return;
        }

        var embed = new EmbedProperties()
            .WithTitle("Temporary Ban Expired")
            .WithFields([
                new EmbedFieldProperties()
                    .WithName("User")
                    .WithValue($"<@{who}> ({who})")
                    .WithInline()
            ])
            .WithTimestamp(DateTime.UtcNow)
            .WithColor(Colors.Blue);
        
        var messageProperties = new MessageProperties().AddEmbeds(embed);
        
        try
        {
            await client.SendMessageAsync((ulong)logModChannel, messageProperties);
        }
        catch (RestException e)
        {
            if (e.StatusCode != HttpStatusCode.NotFound && e.StatusCode != HttpStatusCode.Forbidden)
            {
                throw;
            }
        }
    }

    public static MessageProperties GetCaseEmbed(Case caseData, ulong guildId, Color color)
    {
        List<EmbedFieldProperties> embedFields =
        [
            new EmbedFieldProperties()
                .WithName("Moderator")
                .WithValue($"<@{caseData.ModUid}> ({caseData.ModUid})")
                .WithInline(),
            new EmbedFieldProperties()
                .WithName("User")
                .WithValue($"<@{caseData.UserId}> ({caseData.UserId})")
                .WithInline()
        ];

        if (caseData.RuleIds.Length > 0 && caseData.RuleTitle.Length > 0)
        {
            embedFields.Add(new EmbedFieldProperties()
                .WithName("Rule IDs")
                .WithValue(string.Join(", ", caseData.RuleIds))
                .WithInline());
            embedFields.Add(new EmbedFieldProperties()
                .WithName("Rule Contents")
                .WithValue(caseData.RuleTitle)
                .WithInline());
        }

        if (caseData.Note != null)
        {
            embedFields.Add(new EmbedFieldProperties()
                .WithName("Note")
                .WithValue(caseData.Note));
        }

        if (caseData.TempBanExpire != null)
        {
            embedFields.Add(new EmbedFieldProperties()
                .WithName("Duration")
                .WithValue($"{TimeSpan.FromSeconds((double)(caseData.TempBanExpire - caseData.Timestamp)).Humanize()}"));
            embedFields.Add(new EmbedFieldProperties()
                .WithName("Expire")
                .WithValue($"<t:{caseData.TempBanExpire}:F")
                .WithInline()
            );
        }
        
        var embed = new EmbedProperties()
            .WithTitle($"{caseData.Id} | Moderation Action: {caseData.PunishmentType}")
            .WithFields(embedFields)
            .WithColor(color)
            .WithTimestamp(DateTimeOffset.FromUnixTimeSeconds(caseData.Timestamp));
        
        if (caseData.Image != null)
        {
            embed.WithImage(new EmbedImageProperties(caseData.Image));
        }
        
        return new MessageProperties().AddEmbeds(embed);
    }
    
        public static async Task<MessageProperties> GetCaseDmEmbed(Case caseData, ulong guildId, Color color, RestClient client)
    {
        var appealLink = Database.Database.GetAppealLink(guildId);
        ActionRowProperties? appealButton = null;
        
        if (appealLink != null)
        {
            appealButton =
            [
                new LinkButtonProperties(appealLink, "Appeal")
            ];
        }
        
        var guild = await client.GetGuildAsync(guildId);
        List<EmbedFieldProperties> embedFields = [];

        if (caseData.RuleIds.Length > 0 && caseData.RuleTitle.Length > 0)
        {
            embedFields.Add(new EmbedFieldProperties()
                .WithName("Rule IDs")
                .WithValue(string.Join(", ", caseData.RuleIds))
                .WithInline());
            embedFields.Add(new EmbedFieldProperties()
                .WithName("Rule Contents")
                .WithValue(caseData.RuleTitle)
                .WithInline());
        }

        if (caseData.Note != null)
        {
            embedFields.Add(new EmbedFieldProperties()
                .WithName("Note")
                .WithValue(caseData.Note));
        }

        if (caseData.TempBanExpire != null)
        {
            embedFields.Add(new EmbedFieldProperties()
                .WithName("Duration")
                .WithValue($"{TimeSpan.FromSeconds((double)(caseData.TempBanExpire - caseData.Timestamp)).Humanize()}"));
            embedFields.Add(new EmbedFieldProperties()
                .WithName("Expire")
                .WithValue($"<t:{caseData.TempBanExpire}:F")
                .WithInline()
            );
        }

        embedFields.Add(new EmbedFieldProperties()
                .WithName("User DM'd")
                .WithValue(caseData.dmSuccess)
                .WithInline()
            );
        
        var embed = new EmbedProperties()
            .WithTitle($"You have been {GetPunishmentTypeEd(caseData.PunishmentType)} In {guild.Name}")
            .WithFields(embedFields)
            .WithColor(color)
            .WithTimestamp(DateTimeOffset.FromUnixTimeSeconds(caseData.Timestamp));
        
        if (caseData.Image != null)
        {
            embed.WithImage(new EmbedImageProperties(caseData.Image));
        }

        var messageProperties = new MessageProperties().AddEmbeds(embed);
        if (appealButton != null)
        {
            messageProperties.WithComponents([appealButton]);
        }
        
        return messageProperties;
    }

    private static async Task LogCase(Case caseData, ulong guildId, Color color, RestClient client)
    {
        var logModChannel = Database.Database.GetLogModChannel(guildId);
        
        var user = await client.GetUserAsync(caseData.UserId);
        var dmChannel = await user.GetDMChannelAsync();
        try {
            await dmChannel.SendMessageAsync(await GetCaseDmEmbed(caseData, guildId, color, client));
            caseData.dmSuccess = true;
        }
        catch (RestException e)
        {
            caseData.dmSuccess = false;
            if (e.StatusCode != HttpStatusCode.NotFound && e.StatusCode != HttpStatusCode.Forbidden)
            {
                throw;
            }
        }
        
        if (logModChannel == null)
        {
            return;
        }
        
        await client.SendMessageAsync((ulong)logModChannel, GetCaseEmbed(caseData, guildId, color));
    }

    public static async Task DoModAction(ulong guildId, ModAction action, RestClient client)
    {
        var punishmentType = action.Type;
        switch (punishmentType)
        {
            case PunishmentType.Ban:
                await BanUser(guildId, action, client);
                break;
            case PunishmentType.Kick:
                await KickUser(guildId, action, client);
                break;
            case PunishmentType.Timeout:
                await TimeoutUser(guildId, action, client);
                break;
            case PunishmentType.Warn:
                await WarnUser(guildId, action, client);
                break;
            case PunishmentType.Appeal:
                await AppealUser(guildId, action, client);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    public static async Task BanUser(ulong guildId, ModAction action,
        RestClient client)
    {
        if (action.Type != PunishmentType.Ban)
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }
        await BanUser(guildId, action.ModId, action.UserId, action.Rules, action.DurationMinute, action.Note, action.Image, client);
    }

    public static async Task BanUser(ulong guildId, ulong modId, ulong userId, int[] rules, long durationMinute, string? note, string? image, RestClient client)
    {
        var isAllowedNoRule = Database.Database.GetAllowNoRule(guildId);
        if (!isAllowedNoRule && rules.Length == 0)
        {
            throw new NoRuleAllowedIsOff();
        }
        
        var serverRulesCount = Database.Database.GetCurrentRuleId(guildId);
        if (rules.Any(rule => rule <= 0 || rule > serverRulesCount))
        {
            throw new InvalidRules();
        }
        var startDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long banDuration = 0;
        if (durationMinute != 0)
        {
            banDuration = DateTimeOffset.UtcNow.AddMinutes(durationMinute).ToUnixTimeSeconds();
        }
        
        var serverRules = Database.Database.GetRules(guildId);

        var ruleStrings = new string[rules.Length];
        for (var i = 0; i < rules.Length; i++)
        {
            ruleStrings[i] = serverRules[rules[i]].Title;
        }

        var auditLogReason = string.Join(" | ", ruleStrings);
        auditLogReason += $" | {note}";
        
        string? uploadedImage = null;
        if (image != null)
        {
            uploadedImage = await ImageUploader.UploadFromUrl(image);
        }

        if (durationMinute == 0)
        {
            Database.Database.RemoveTBan(guildId, userId);
            
            await client.BanGuildUserAsync(
                guildId,
                userId,
                0,
                new RestRequestProperties()
                    .WithAuditLogReason(auditLogReason)
            );
            
            await LogCase(Database.Database.AddCase(guildId,
                    userId,
                    PunishmentType.Ban,
                    rules,
                    modId,
                    note,
                    startDate,
                    null,
                    false,
                    uploadedImage),
                guildId,
                Colors.Red,
                client);
            return;
        }
        
        Database.Database.AddTBan(guildId, userId, banDuration);
        await client.BanGuildUserAsync(
            guildId,
            userId,
            0,
            new RestRequestProperties()
                .WithAuditLogReason(auditLogReason)
        );
        await LogCase(Database.Database.AddCase(guildId,
                userId,
                PunishmentType.Ban,
                rules,
                modId,
                note,
                startDate,
                banDuration,
                false,
                uploadedImage),
            guildId,
            Colors.Red,
            client);
    }

    public static async Task KickUser(ulong guildId, ModAction action, RestClient client)
    {
        if (action.Type != PunishmentType.Kick)
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }
        await KickUser(guildId, action.ModId, action.UserId, action.Rules, action.Note, action.Image, client);
    }

    public static async Task KickUser(ulong guildId, ulong modId, ulong userId, int[] rules, string? note, string? image, RestClient client)
    {
        var isAllowedNoRule = Database.Database.GetAllowNoRule(guildId);
        if (!isAllowedNoRule && rules.Length == 0)
        {
            throw new NoRuleAllowedIsOff();
        }
        
        var serverRulesCount = Database.Database.GetCurrentRuleId(guildId);
        if (rules.Any(rule => rule <= 0 || rule > serverRulesCount))
        {
            throw new InvalidRules();
        }
        var startDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var serverRules = Database.Database.GetRules(guildId);

        var ruleStrings = new string[rules.Length];
        for (var i = 0; i < rules.Length; i++)
        {
            ruleStrings[i] = serverRules[rules[i]].Title;
        }

        var auditLogReason = string.Join(" | ", ruleStrings);
        auditLogReason += $" | {note}";
        
        string? uploadedImage = null;
        if (image != null)
        {
            uploadedImage = await ImageUploader.UploadFromUrl(image);
        }
        
        await client.KickGuildUserAsync(
            guildId,
            userId,
            new RestRequestProperties()
                .WithAuditLogReason(auditLogReason)
        );
        await LogCase(Database.Database.AddCase(guildId,
                userId,
                PunishmentType.Kick,
                rules,
                modId,
                note,
                startDate,
                null,
                uploadedImage),
            guildId,
            Colors.Red,
            client);
    }
    
    public static async Task TimeoutUser(ulong guildId, ModAction action,
        RestClient client)
    {
        if (action.Type != PunishmentType.Ban)
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }
        await TimeoutUser(guildId, action.ModId, action.UserId, action.Rules, action.DurationMinute, action.Note, action.Image, client);
    }

    public static async Task TimeoutUser(ulong guildId, ulong modId, ulong userId, int[] rules, long durationMinute, string? note, string? image, RestClient client)
    {
        ArgumentOutOfRangeException.ThrowIfZero(durationMinute);
        var isAllowedNoRule = Database.Database.GetAllowNoRule(guildId);
        if (!isAllowedNoRule && rules.Length == 0)
        {
            throw new NoRuleAllowedIsOff();
        }
        
        var serverRulesCount = Database.Database.GetCurrentRuleId(guildId);
        if (rules.Any(rule => rule <= 0 || rule > serverRulesCount))
        {
            throw new InvalidRules();
        }
        var startDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timeoutDuration = DateTimeOffset.UtcNow.AddMinutes(durationMinute);
        
        var serverRules = Database.Database.GetRules(guildId);

        var ruleStrings = new string[rules.Length];
        for (var i = 0; i < rules.Length; i++)
        {
            ruleStrings[i] = serverRules[rules[i]].Title;
        }

        var auditLogReason = string.Join(" | ", ruleStrings);
        auditLogReason += $" | {note}";
        
        string? uploadedImage = null;
        if (image != null)
        {
            uploadedImage = await ImageUploader.UploadFromUrl(image);
        }
        
        var guildUser = await client.GetGuildUserAsync(
            guildId,
            userId
        );
        
        await guildUser.TimeOutAsync(timeoutDuration, new RestRequestProperties().WithAuditLogReason(auditLogReason));
        
        await LogCase(Database.Database.AddCase(guildId,
                userId,
                PunishmentType.Timeout,
                rules,
                modId,
                note,
                startDate,
                timeoutDuration.ToUnixTimeSeconds(),
                uploadedImage),
            guildId,
            Colors.Red,
            client);
    }


    public static async Task WarnUser(ulong guildId, ModAction action, RestClient client)
    {
        if (action.Type != PunishmentType.Warn)
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }
        await WarnUser(guildId, action.ModId, action.UserId, action.Rules, action.Note, action.Image, client);
    }
    
    public static async Task WarnUser(ulong guildId, ulong modId, ulong userId, int[] rules, string? note, string? image, RestClient client)
    {
        var isAllowedNoRule = Database.Database.GetAllowNoRule(guildId);
        if (!isAllowedNoRule && rules.Length == 0)
        {
            throw new NoRuleAllowedIsOff();
        }
        
        var serverRulesCount = Database.Database.GetCurrentRuleId(guildId);
        if (rules.Any(rule => rule <= 0 || rule > serverRulesCount))
        {
            throw new InvalidRules();
        }
        var startDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        string? uploadedImage = null;
        if (image != null)
        {
            uploadedImage = await ImageUploader.UploadFromUrl(image);
        }
        
        await LogCase(Database.Database.AddCase(guildId,
                userId,
                PunishmentType.Warn,
                rules,
                modId,
                note,
                startDate,
                null,
                uploadedImage),
            guildId,
            Colors.Yellow,
            client);
    }
    
    public static async Task AppealUser(ulong guildId, ModAction action, RestClient client)
    {
        if (action.Type != PunishmentType.Appeal)
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }
        await AppealUser(guildId, action.ModId, action.UserId, action.Note, client);
    }

    public static async Task AppealUser(ulong guildId, ulong modId, ulong userId, string? note, RestClient client)
    {
        var startDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await client.UnbanGuildUserAsync(guildId, userId);
        
        await LogCase(Database.Database.AddCase(guildId,
                userId,
                PunishmentType.Warn,
                [],
                modId,
                note,
                startDate,
                null),
            guildId,
            Colors.Green,
            client);
    }
}
