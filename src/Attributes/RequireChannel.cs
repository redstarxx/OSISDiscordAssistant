using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace discordbot.Attributes
{
    /// <summary>
    /// Checks whether the command is executed in the channel that is specified.
    /// </summary>
    public class RequireChannel : CheckBaseAttribute
    {
        /// <summary>
        /// Gets the channel that this command is required to be executed inside.
        /// </summary>
        public static ulong Channel { get; private set; }

        /// <summary>
        /// Checks whether the command is executed in the channel that is specified.
        /// </summary>
        /// <param name="channelId">Channel required for this command to be executed inside.</param>
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
