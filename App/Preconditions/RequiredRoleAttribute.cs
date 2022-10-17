using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Reservator.Models;

namespace Reservator.Preconditions;

public class RequireManagerAttribute : PreconditionAttribute
{
    // Override the CheckPermissions method
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
        IServiceProvider services)
    {
        var database = services.GetRequiredService<GameContext>();
        if (context.User is not SocketGuildUser gUser)
            return Task.FromResult(PreconditionResult.FromError("You must be in a guild to run this command."));
            
        return Task.FromResult(
            database.GuildRoles.ToList().Exists(_ =>
                context.Guild.Id == _.GuildId && gUser.Roles.Any(r => r.Id == _.RoleId) && _.Permission == "admin")
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError($"You must have a needed role to run this command."));
    }
}