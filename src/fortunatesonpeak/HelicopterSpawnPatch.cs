using HarmonyLib;
using UnityEngine;

namespace fortunatesonpeak;

[HarmonyPatch(typeof(PeakHandler), "SummonHelicopter")]
public static class HelicopterSpawnPatch
{
    [HarmonyPrefix] // O Postfix, dependiendo de cuándo quieres que se ejecute.
    public static void Postfix()
    {
        FortunateSonPeakPlugin.Log.LogInfo(
            "¡Parche activado! Intentando reproducir 'Fortunate Son'..."
        );
        AudioHandler.PlayFortunateSon(); // Llama al método del manejador de audio
    }
}
