using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OSISDiscordAssistant.Attributes;
using OSISDiscordAssistant.Constants;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace OSISDiscordAssistant.Commands
{
    public sealed class BotAdministrationCommands : BaseCommandModule
    {
        [RequireServiceAdminRole]
        [Command("kill")]
        public async Task KillAsync(CommandContext ctx)
        {
            Bot.Client.Logger.LogWarning(EventIds.CommandHandler, $"{ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.User.Id}) initiated a kill command.", DateTime.UtcNow.AddHours(7));
            await ctx.Channel.SendMessageAsync($"Disconnecting all shards from the gateway...");
            Thread.Sleep(TimeSpan.FromSeconds(1));

            await Bot.Client.StopAsync();

            Environment.Exit(0);
        }
        
        // Credits to Emzi0767#1837 for the code.
        [RequireServiceAdminRole]
        [Command("eval")]
        public async Task EvalAsync(CommandContext ctx, [RemainingText] string code)
        {
            var cs1 = code.IndexOf("```") + 3;
            cs1 = code.IndexOf('\n', cs1) + 1;
            var cs2 = code.LastIndexOf("```");

            if (cs1 == -1 || cs2 == -1)
                throw new ArgumentException("You need to wrap the code into a code block.", nameof(code));

            code = code[cs1..cs2];

            var embed = new DiscordEmbedBuilder
            {
                Title = "Evaluating...",
                Color = new DiscordColor(0xD091B2)
            };
            var msg = await ctx.RespondAsync("", embed: embed.Build());

            var globals = new EvaluationEnvironment(ctx);
            var sopts = ScriptOptions.Default
                .WithImports("System", "System.Collections.Generic", "System.Diagnostics", "System.Linq", "System.Net.Http", "System.Net.Http.Headers", "System.Reflection", "System.Text",
                             "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.CommandsNext", "DSharpPlus.Entities", "DSharpPlus.EventArgs", "DSharpPlus.Exceptions", "OSISDiscordAssistant")
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

            var sw1 = Stopwatch.StartNew();
            var cs = CSharpScript.Create(code, sopts, typeof(EvaluationEnvironment));
            var csc = cs.Compile();
            sw1.Stop();

            if (csc.Any(xd => xd.Severity == DiagnosticSeverity.Error))
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Compilation failed",
                    Description = string.Concat("Compilation failed after ", sw1.ElapsedMilliseconds.ToString("#,##0"), "ms with ", csc.Length.ToString("#,##0"), " errors."),
                    Color = new DiscordColor(0xD091B2)
                };
                foreach (var xd in csc.Take(3))
                {
                    var ls = xd.Location.GetLineSpan();
                    embed.AddField(string.Concat("Error at ", ls.StartLinePosition.Line.ToString("#,##0"), ", ", ls.StartLinePosition.Character.ToString("#,##0")), Formatter.InlineCode(xd.GetMessage()), false);
                }
                if (csc.Length > 3)
                {
                    embed.AddField("Some errors ommited", string.Concat((csc.Length - 3).ToString("#,##0"), " more errors not displayed"), false);
                }
                await msg.ModifyAsync(embed: embed.Build());
                return;
            }

            Exception rex = null;
            ScriptState<object> css = null;
            var sw2 = Stopwatch.StartNew();
            try
            {
                css = await cs.RunAsync(globals);
                rex = css.Exception;
            }
            catch (Exception ex)
            {
                rex = ex;
            }
            sw2.Stop();

            if (rex != null)
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Execution failed",
                    Description = string.Concat("Execution failed after ", sw2.ElapsedMilliseconds.ToString("#,##0"), "ms with `", rex.GetType(), ": ", rex.Message, "`."),
                    Color = new DiscordColor(0xD091B2),
                };
                await msg.ModifyAsync(embed: embed.Build());
                return;
            }

            // execution succeeded
            embed = new DiscordEmbedBuilder
            {
                Title = "Evaluation successful",
                Color = new DiscordColor(0xD091B2),
            };

            embed.AddField("Result", css.ReturnValue != null ? css.ReturnValue.ToString() : "No value returned", false)
                .AddField("Compilation time", string.Concat(sw1.ElapsedMilliseconds.ToString("#,##0"), "ms"), true)
                .AddField("Execution time", string.Concat(sw2.ElapsedMilliseconds.ToString("#,##0"), "ms"), true);

            if (css.ReturnValue != null)
                embed.AddField("Return type", css.ReturnValue.GetType().ToString(), true);

            await msg.ModifyAsync(embed: embed.Build());
        }
    }

    public sealed class EvaluationEnvironment
    {
        public CommandContext Context { get; }

        public DiscordMessage Message => this.Context.Message;
        public DiscordChannel Channel => this.Context.Channel;
        public DiscordGuild Guild => this.Context.Guild;
        public DiscordUser User => this.Context.User;
        public DiscordMember Member => this.Context.Member;
        public DiscordClient Client => this.Context.Client;

        public EvaluationEnvironment(CommandContext ctx)
        {
            this.Context = ctx;
        }
    }
}
