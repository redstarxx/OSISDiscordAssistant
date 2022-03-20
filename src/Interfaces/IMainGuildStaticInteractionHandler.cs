using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace OSISDiscordAssistant.Services
{
    public interface IMainGuildStaticInteractionHandler
    {
        Task<Task> HandleRolesInteraction(DiscordClient client, ComponentInteractionCreateEventArgs e);
        Task<Task> HandleVerificationRequests(DiscordClient client, ComponentInteractionCreateEventArgs e);
    }
}