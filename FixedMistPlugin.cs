using BepInEx;

using Silksong.ModMenu;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;

using SSMP;
using SSMP.Api.Client;
using SSMP.Api.Server;

namespace FixedMistRNG;

[BepInDependency(SSMPPlugin.Id)]
[BepInDependency(Silksong.FsmUtil.Plugin.Id)]
[BepInDependency(ModMenuPlugin.Id)]
[BepInAutoPlugin(id: "io.github.vitaxses.fixedmistrng")]
public partial class FixedMistPlugin : BaseUnityPlugin, IModMenuCustomMenu
{

    private void Awake()
    {
        ClientAddon.RegisterAddon(new FixedMistClientAddon());
        ServerAddon.RegisterAddon(new FixedMistServerAddon());

        new Harmony(Id).PatchAll();

        Logger.LogInfo($"Plugin {Name} ({Id}) v{Version} has loaded!");
    }

    public LocalizedText ModMenuName() => AddonIdentifiers.NAME;

    public AbstractMenuScreen BuildCustomMenu()
    {
        PaginatedMenuScreenBuilder builder = new(Name);

        var adjustSeedDescription = "Randomize the seed if a player leaves The Mist or respawns at the entrance while no one else is inside";

        ChoiceElement<bool> adjustSeedElement = new(LocalizedText.Raw("Randomize Seed"), ChoiceModels.ForBool("Disabled", "Enabled"));
        SliderElement<int> seedElement = new(LocalizedText.Raw("Mist Seed"), SliderModels.ForInts(0, 1000));

        builder.Add(adjustSeedElement);
        builder.Add(seedElement);

        PaginatedMenuScreen menu = builder.Build();
        menu.OnShow += (navType) =>
        {
            adjustSeedElement.DescriptionText.text = (seedElement.Interactable = adjustSeedElement.Interactable = FixedMistClientAddon.IsConnected()) ? adjustSeedDescription : "";
            adjustSeedElement.Value = FixedMistClientAddon.Instance != null && FixedMistClientAddon.Instance.AdjustSeed;

            seedElement.Value = FixedMistClientAddon.Instance == null ? -1 : FixedMistClientAddon.Instance.CurrentSeed;
        };

        menu.OnHide += (navType) =>
        {
            if (!FixedMistClientAddon.IsConnected())
                return;

            FixedMistClientAddon.Instance!.SendOptionUpdate
            (
                seedElement.Value, 
                adjustSeedElement.Value
            );
        };
        
        return menu;
    }
}
