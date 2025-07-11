using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace fortunatesonpeak;

[BepInPlugin("com.aleasdev.fortunatesonpeak", "fortunatesonpeak", "1.0.0")]
public partial class FortunateSonPeakPlugin : BaseUnityPlugin
{
    public static ManualLogSource Log { get; private set; } = null!;
    public static FortunateSonPeakPlugin Instance { get; private set; } = null!; // Necesario para iniciar corrutinas

    private void Awake()
    {
        Instance = this; // Asigna la instancia para poder usar StartCoroutine
        Log = Logger;
        Log.LogInfo("fortunatesonpeak plugin is loaded!");
        Log.LogInfo("El mod está cargando...");

        // Cargar el audio usando el AudioHandler
        AudioHandler.LoadFortunateSonClip(this); // 'this' se refiere a la instancia de FortunateSonPeakPlugin

        var harmony = new Harmony("com.aleasdev.fortunatesonpeak");
        harmony.PatchAll();
        Log.LogInfo("¡El mod se ha cargado correctamente!");
    }
}
