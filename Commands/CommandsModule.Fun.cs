// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026 TheMonHub

using System.Text.Json;
using NetCord.Services.ApplicationCommands;

namespace JimmyRewrite.Commands;

public partial class CommandsModule
{
    private enum CoinSides
    {
        Heads = 1,
        Tails = 2
    }
    
    [SlashCommand("rand", "Roll a dice!... or coin")]
    public static string Rand(
        [SlashCommandParameter(Name = "sides", Description = "How many sides would you like on your dice?... Two sides for a coin flip", MinValue = 2, MaxValue = 100)]
        int sides = 6,
        [SlashCommandParameter(Name = "rolls", Description = "How many times do you wanna roll/flip it?", MinValue = 1, MaxValue = 100)]
        int rolls = 1
        )
    {
        var results = new int[rolls];
        for (var i = 0; i < rolls; i++)
        {
            results[i] = Random.Shared.Next(1, sides + 1);
        }
        
        var min = results.Min();
        var max = results.Max();
        var avg = (int)results.Average();
        var sum = results.Sum();

        if (sides == 2)
        {
            if (rolls <= 1) return $":coin: You got ${IsHeadOrTails(results[0])}!";
            
            var heads = results.Count(x => x == (int)CoinSides.Heads);
            var tails = results.Count(x => x == (int)CoinSides.Tails);
            return $"> **{string.Join(", ", IsHeadOrTailsShort(results))}**\n" +
                   $":coin: total: **{sum}** | heads: **{heads}** | tails: **{tails}**";
        }

        if (rolls <= 1) return $":game_die: You rolled a **{results[0]}**!";
        
        return $"> **{string.Join(", ", results)}**\n" +
               $":game_die: total: **{sum}** | min: **{min}** | max: **{max}** | avg: **{avg}** | **{rolls}d{sides}**";

        string IsHeadOrTails(int value) => value == (int)CoinSides.Heads ? "Head" : "Tails";

        string[] IsHeadOrTailsShort(int[] values)
        {
            var returnValue = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                returnValue[i] = values[i] == (int)CoinSides.Heads ? "H" : "T";
            }

            return returnValue;
        }
    }

    [SlashCommand("cat", "Get a random cat image")]
    public static async Task<string> Cat() {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://beta-api.thecatapi.com/v1/images/search");

        var catKey = Program.ConfigManager["TheCatApi:Key"];
        request.Headers.Add("x-api-key", catKey);

        var response = await Program.Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);
        var url = jsonDoc.RootElement[0].GetProperty("url").GetString();

        return url ?? throw new Exception();
    }
        
    [SlashCommand("dog", "Get a random dog image")]
    public static async Task<string> Dog() {
            
        var request = new HttpRequestMessage(HttpMethod.Get, "https://dog.ceo/api/breeds/image/random");

        var response = await Program.Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);
        var url = jsonDoc.RootElement.GetProperty("message").GetString();

        return url ?? throw new Exception();
    }
}
