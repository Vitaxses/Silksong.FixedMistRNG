using System.Reflection;

namespace FixedMistRNG.SSMPAddon;

internal static class AddonIdentifiers
{
    public static string VERSION
    {
        get
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();       
        }
    }
    
    public const string NAME = "Fixed Mist RNG";
}
