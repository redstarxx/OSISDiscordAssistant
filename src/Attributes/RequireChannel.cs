using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace discordbot.Attributes
{
    public class RequireChannel : CheckBaseAttribute
    {
        public ulong channel { get; private set; }

        public RequireChannel(ulong channelId)
        {
            channel = channelId;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return Task.FromResult(channel == ctx.Channel.Id);
        }
    }
}
