using BepInEx;
using BepInEx.Configuration;
using FixedMistRNG.SSMPAddon;
using HarmonyLib;
using SSMP;
using SSMP.Api.Client;
using SSMP.Api.Server;

namespace FixedMistRNG;

[BepInDependency(SSMPPlugin.Id)]
[BepInDependency(Silksong.FsmUtil.Plugin.Id)]
[BepInAutoPlugin(id: "io.github.vitaxses.fixedmistrng")]
public partial class FixedMistPlugin : BaseUnityPlugin
{
    public static ConfigEntry<bool> AdjustSeed = null!;

    private void Awake()
    {
        AdjustSeed = Config.Bind("General", "Adjust Seed", false, "Adjust the seed if a player leaves The Mist or respawns at the entrance while no one else is inside");
        AdjustSeed.SettingChanged += (sender, e) =>
        {
            if (!FixedMistClientAddon.IsConnected())
                return;

            if (FixedMistClientAddon.Instance!.ReceivedSettingUpdate)
            {
                FixedMistClientAddon.Instance.ReceivedSettingUpdate = false;
                return;
            }

            FixedMistClientAddon.Instance.SendOptionUpdate();
        };

        ClientAddon.RegisterAddon(new FixedMistClientAddon());
        ServerAddon.RegisterAddon(new FixedMistServerAddon());

        new Harmony(Id).PatchAll();

        Logger.LogInfo($"Plugin {Name} ({Id}) v{Version} has loaded!");
    }
}
