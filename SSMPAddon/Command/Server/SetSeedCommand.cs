using SSMP.Api.Command.Server;

namespace FixedMistRNG.SSMPAddon.Command.Server;

public class SetSeedCommand(FixedMistServerAddon addon) : IServerCommand
{
    public bool AuthorizedOnly => false;

    public string Trigger => "/setMistSeed";

    public string[] Aliases => ["/setmistseed", "/SetMistSeed", "/SETMISTSEED", "/mistseed", "/mistSeed"];

    public void Execute(ICommandSender commandSender, string[] arguments)
    {
        if (!commandSender.IsAuthorized || arguments.Length < 2)
        {
            commandSender.SendMessage($"Current seed: {addon.CurrentSeed}!");
            return;
        }

        if (int.TryParse(arguments[1], out int result))
        {
            FixedMistServerAddon.SetSeed(result);
            commandSender.SendMessage($"Successfully changed the seed to {addon.CurrentSeed}!");
        } else
        {
            commandSender.SendMessage("Please provide an integer value.");
        }
    }
}
