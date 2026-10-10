// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026 TheMonHub

using JimmyRewrite.Buttons;
using JimmyRewrite.events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using CommandsModule = JimmyRewrite.Commands.CommandsModule;

namespace JimmyRewrite;

internal static class Program
{
    public static ConfigurationManager ConfigManager { get; private set; } = null!;
    public static readonly HttpClient Client = new();
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        ConfigManager = builder.Configuration;
        builder.Services.AddDiscordGateway(options =>
        {
            options.Intents = GatewayIntents.Guilds | GatewayIntents.MessageContent | GatewayIntents.GuildMessages | GatewayIntents.GuildUsers;
        });

        builder.Services.AddApplicationCommands();
        builder.Services.AddComponentInteractions();
            
        builder.Services.AddGatewayHandler<MessageDeleteHandler>();
        builder.Services.AddGatewayHandler<MessageDeleteBulkHandler>();
        builder.Services.AddGatewayHandler<MessageUpdateHandler>();
        builder.Services.AddGatewayHandler<MessageCreateHandler>();
        builder.Services.AddGatewayHandler<GuildUserAddHandler>();
        builder.Services.AddGatewayHandler<GuildUserRemoveHandler>();

        builder.Services.AddHostedService<TempBanManager>();

        var host = builder.Build();

        host.AddApplicationCommandModule<CommandsModule>();
        host.AddApplicationCommandModule<CommandsModule.ConfigModule>();
        host.AddApplicationCommandModule<CommandsModule.ModModule>();
        
        host.AddComponentInteractionModule<ModActionConfirmModule>();

        await host.RunAsync();
    }
}
