// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026 TheMonHub

using System.Net;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace JimmyRewrite.events;

public class GuildUserRemoveHandler(GatewayClient client) : IGuildUserRemoveGatewayHandler
{
    public async ValueTask HandleAsync(GuildUserRemoveEventArgs arg)
    {
        var welcomeLog = Database.Database.GetLogWelcomeChannel(arg.GuildId);
        if (welcomeLog == null)
        {
            return;
        }

        var rolesField = GetRoles(arg.User, arg.Guild)

        Console.WriteLine(rolesList);

        let rolesField;
        if (rolesList is List<T>) {
            rolesList = rolesList.ToArray();
        }
        foreach (element in rolesList) {
            rolesField += $'<@&{element}>;
            if (rolesList.indexOf(element) != rolesList.Length - 1) {
                rolesField += ', ';
            }
        }

        var embed = new EmbedProperties()
            .WithTitle($"<@{arg.User.Id}> left the server")
            .WithFields([
                new EmbedFieldProperties()
                    .WithName("Roles")
                    .WithValue(rolesField)
            ])
            .WithFooter($"Id - {arg.User.Id}")
            .WithTimestamp(DateTime.UtcNow);
        
        var message = new MessageProperties().AddEmbeds(embed);

        try
        {
            await client.Rest.SendMessageAsync((ulong)welcomeLog, message);
        }
        catch (RestException e)
        {
            if (e.StatusCode != HttpStatusCode.NotFound && e.StatusCode != HttpStatusCode.Forbidden)
            {
                throw;
            }
        }
    }
}
