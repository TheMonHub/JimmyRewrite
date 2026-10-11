// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026 TheMonHub

using System.Net;
using Humanizer;
using JimmyRewrite.Database;
using NetCord;
using NetCord.Rest;

namespace JimmyRewrite;

// TODO: Send DM to user after a punishment in LogCase (LogTempUnban completed)

public static class ModerationHandler
{
    public enum PunishmentType
    {
        Unknown = 0,
        Ban = 1,
        Kick = 2,
        Timeout = 3,
        Warn = 4,
        Appeal = 5
    }

    public class InvalidRules : Exception;

    public class NoRuleAllowedIsOff : Exception;
    
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
        var user = await client.RestClient.Users.GetAsync(who);
        var dmChannel = await user.GetDMChannelAsync();
        
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

        var dmEmbed = new EmbedProperties()
            .WithTitle("Your Temporary Ban Has Expired")
            .WithTimestamp(DateTime.UtcNow)
            .WithColor(Colors.Blue);
        
        var messageProperties = new MessageProperties().AddEmbeds(embed);
        var dmMessageProperties = new MessageProperties().AddEmbeds(dmEmbed);
        
        try
        {
            await client.SendMessageAsync((ulong)logModChannel, messageProperties)
        }
        catch (RestException e)
        {
            if (e.StatusCode != HttpStatusCode.NotFound && e.StatusCode != HttpStatusCode.Forbidden)
            {
                throw;
            }
        }
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

    private static async Task LogCase(Case caseData, ulong guildId, Color color, RestClient client)
    {
        var logModChannel = Database.Database.GetLogModChannel(guildId);
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
            case PunishmentType.Unknown:
                throw new ArgumentOutOfRangeException(nameof(action));
            case PunishmentType.Ban:
                await BanUser(guildId, action, client);
                break;
            case PunishmentType.Kick:
                break;
            case PunishmentType.Timeout:
                break;
            case PunishmentType.Warn:
                break;
            case PunishmentType.Appeal:
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
            return;
        }
        await BanUser(guildId, action.ModId, action.UserId, action.Rules, action.DurationMinute, action.Note, action.Image, client);
    }

    public static async Task BanUser(ulong guildId, User mod, User user, int[] rules, long durationMinute, string? note, RestClient client)
    {
        await BanUser(guildId, mod.Id, user.Id, rules, durationMinute, note, null, client);
    }

    public static async Task BanUser(ulong guildId, User mod, User user, int[] rules, long durationMinute, string? note, string? image, RestClient client)
    {
        await BanUser(guildId, mod.Id, user.Id, rules, durationMinute, note, image, client);
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
            
            // await client.BanGuildUserAsync(
            //     guildId,
            //     userId,
            //     0,
            //     new RestRequestProperties()
            //         .WithAuditLogReason(auditLogReason)
            // );
            
            await LogCase(Database.Database.AddCase(guildId,
                    userId,
                    PunishmentType.Ban,
                    rules,
                    modId,
                    note,
                    startDate,
                    null,
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
                uploadedImage),
            guildId,
            Colors.Red,
            client);
    }

    public static async Task KickUser(ulong guildId, User mod, User user, int[] rules, string? note, RestClient client, string? image = null)
    {
        
    }

    public static async Task TimeoutUser(ulong guildId, User mod, User user, int[] rules, long durationMinute, string? note, RestClient client, string? image = null)
    {
        
    }
    
    public static async Task WarnUser(ulong guildId, User mod, User user, int[] rules, string? note, RestClient client, string? image = null)
    {
        
    }

    public static async Task AppealUser(ulong guildId, User mod, User user, string? note, RestClient client, string? image = null)
    {
        
    }
}
