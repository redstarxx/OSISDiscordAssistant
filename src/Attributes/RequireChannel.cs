using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace discordbot.Attributes
{
    /// <summary>
    /// Checks whether the command is executed in the channel that is specified in the attribute.
    /// </summary>
    public class RequireChannel : CheckBaseAttribute
    {
        public static ulong Channel { get; private set; }

        public RequireChannel(ulong channelId)
        {
            Channel = channelId;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return Task.FromResult(Channel == ctx.Channel.Id);
        }
    }
}
