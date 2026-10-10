// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026 TheMonHub

using JimmyRewrite.Commands;
using MessagePack;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace JimmyRewrite.Buttons;

public class ModActionConfirmModule : ComponentInteractionModule<ComponentInteractionContext>
{
    private static Task<InteractionCallbackResponse?> SendConfirmOnlyAuthorError(ComponentInteractionContext context)
    {
        return context.Interaction.SendResponseAsync(
            InteractionCallback.Message(
                new InteractionMessageProperties()
                    .WithContent(":x: You cannot confirm this as you are not the one who used the command!")
                    
                )
        );       
    }

    private static Task<InteractionCallbackResponse?> SendAttachmentExpired(ComponentInteractionContext context)
    {
        return context.Interaction.SendResponseAsync(
            InteractionCallback.Message(
                new InteractionMessageProperties()
                    .WithContent(":x: Attachment has expired! Please send the command again!")
            )
        );
    }

    private static Task<InteractionCallbackResponse?> SendPermLessThanError(ComponentInteractionContext context)
    {
        return context.Interaction.SendResponseAsync(
            InteractionCallback.Message(
                new InteractionMessageProperties()
                    .WithContent(":x: You or I (the bot) have less permission than the target user or doesn't have the permission required!\n" +
                                 "Check your and my role if it's higher than the target user's role or not and if you have the permission to actually run the command!")
            )
        );
    }

    
    [ComponentInteraction("mac")] 
    public async Task OnModActionConfirmButtonClick(string modActionString, string? attachmentIdBase64) 
    {
        var modAction = MessagePackSerializer.Deserialize<ModAction>(Convert.FromBase64String(modActionString));
        long? attachmentId = null;
        if (attachmentIdBase64 is { Length: > 0 })
        {
            attachmentId = BitConverter.ToInt64(Convert.FromBase64String(attachmentIdBase64), 0);
        }
        if (modAction.ModId != Context.User.Id)
        {
            await SendConfirmOnlyAuthorError(Context);
            return;
        }
        if (attachmentId != null)
        {
            var attachment = AttachmentUrlCache.Get((long)attachmentId);
            if (attachment == null)
            {
                await SendAttachmentExpired(Context);
                return;
            }
            
            modAction = new ModAction(modAction.Type,
                modAction.ModId,
                modAction.UserId,
                modAction.Rules,
                modAction.DurationMinute,
                modAction.Note,
                CommandsModule.ModModule.AttachmentLeadingUrl + attachment);
        }
        
        var guildId = (Context.Interaction.GuildId ?? Context.Guild?.Id)!.Value;

        var requiredPerms = UserHelper.GetRequiredPermissions(modAction.Type);
        var hasModPerm = await UserHelper.HasPermissionsAsync(modAction.ModId, guildId, requiredPerms, Context.Client.Rest, Context.Guild);
        var hasBotPerm = await UserHelper.HasPermissionsAsync(Context.Client.Id, guildId, requiredPerms, Context.Client.Rest, Context.Guild);

        if (!hasModPerm || !hasBotPerm)
        {
            await SendPermLessThanError(Context);
            return;
        }

        var canModModerate = await UserHelper.CanModerateAsync(modAction.ModId, modAction.UserId, guildId, Context.Client.Rest, Context.Guild);
        var canBotModerate = await UserHelper.CanModerateAsync(Context.Client.Id, modAction.UserId, guildId, Context.Client.Rest, Context.Guild);

        if (!canModModerate || !canBotModerate)
        {
            await SendPermLessThanError(Context);
            return;
        }

        try
        {
            await ModerationHandler.DoModAction(guildId, modAction, Context.Client.Rest);
        }
        catch (ModerationHandler.InvalidRules)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties()
                        .WithContent(":x: Invalid rule!")
                )
            );
        }
        catch (ModerationHandler.NoRuleAllowedIsOff)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties()
                        .WithContent(":x: Allow no rule is off!")
                )
            );
            return;
        }
        
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.ModifyMessage(options => 
                options.WithComponents([
                    new ActionRowProperties
                    {
                        new ButtonProperties("mac", "Confirmed", ButtonStyle.Primary)
                            .WithDisabled()
                    }
                ])
            )
        );
        
        await Context.Interaction.SendFollowupMessageAsync(
            new InteractionMessageProperties()
                .WithContent(":white_check_mark: Moderation action confirmed!")
        );
    }
}