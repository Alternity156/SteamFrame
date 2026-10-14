using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using SteamFrameMod;

[assembly: AssemblyTitle(SteamFrame.BuildInfo.Name)]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(SteamFrame.BuildInfo.Company)]
[assembly: AssemblyProduct(SteamFrame.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + SteamFrame.BuildInfo.Author)]
[assembly: AssemblyTrademark(SteamFrame.BuildInfo.Company)]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
//[assembly: Guid("")]
[assembly: AssemblyVersion(SteamFrame.BuildInfo.Version)]
[assembly: AssemblyFileVersion(SteamFrame.BuildInfo.Version)]
[assembly: NeutralResourcesLanguage("en")]
[assembly: MelonInfo(typeof(SteamFrame), SteamFrame.BuildInfo.Name, SteamFrame.BuildInfo.Version, SteamFrame.BuildInfo.Author, SteamFrame.BuildInfo.DownloadLink)]


// Create and Setup a MelonModGame to mark a Mod as Universal or Compatible with specific Games.
// If no MelonModGameAttribute is found or any of the Values for any MelonModGame on the Mod is null or empty it will be assumed the Mod is Universal.
// Values for MelonModGame can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonGame(null, null)]