using System; // Make sure System is included for Exception
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using fortunatesonpeak;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio; // Needed for AudioMixerGroup
using UnityEngine.Networking; // Needed for UnityWebRequestMultimedia

// Make sure you have added references to Zorro.Core.Runtime.dll and Sirenix.Serialization.dll
// in your .csproj file or via your IDE's reference manager.
// These DLLs are usually found in your game's PEAK_Data\Managed folder.

namespace fortunatesonpeak;

[BepInPlugin("com.aleasdev.fortunatesonpeak", "fortunatesonpeak", "1.0.0")]
public partial class FortunateSonPeakPlugin : BaseUnityPlugin
{
    public static ManualLogSource Log { get; private set; } = null!;
    private static AudioClip fortunateSonClip = null!;
    private static GameObject? currentAudioPlayer;

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo("fortunatesonpeak plugin is loaded!");
        Log.LogInfo("El mod está cargando...");

        LoadAudioClip();

        var harmony = new Harmony("com.aleasdev.fortunatesonpeak");
        harmony.PatchAll();
        Log.LogInfo("¡El mod se ha cargado correctamente!");
    }

    private void LoadAudioClip()
    {
        try
        {
            string pluginLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string audioFilePath = Path.Combine(pluginLocation, "fortunateson.wav");

            if (File.Exists(audioFilePath))
            {
                StartCoroutine(LoadWavFile(audioFilePath));
            }
            else
            {
                Log.LogError($"¡Error! No se encontró el archivo de audio: {audioFilePath}");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"Error al cargar el archivo de audio: {ex}");
        }
    }

    private System.Collections.IEnumerator LoadWavFile(string path)
    {
        using (
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(
                "file://" + path,
                AudioType.WAV
            )
        )
        {
            yield return www.SendWebRequest();

            if (
                www.result == UnityWebRequest.Result.ConnectionError
                || www.result == UnityWebRequest.Result.ProtocolError
            )
            {
                Log.LogError($"Error al cargar audio: {www.error}"); // Using string interpolation
            }
            else
            {
                fortunateSonClip = DownloadHandlerAudioClip.GetContent(www);
                if (fortunateSonClip != null)
                {
                    Log.LogInfo("Audio 'fortunateson.wav' cargado correctamente.");
                }
                else
                {
                    Log.LogError(
                        "No se pudo obtener el contenido del AudioClip desde el archivo WAV."
                    );
                }
            }
        }
    }

    // [HarmonyPatch(typeof(PeakHandler), "OpenDiscord")]
    [HarmonyPatch(typeof(PeakHandler), "SummonHelicopter")]
    public static class HelicopterSpawnPatch
    {
        [HarmonyPrefix]
        public static void Postfix()
        {
            Log.LogInfo("¡Parche activado! Intentando reproducir 'Fortunate Son'...");

            if (fortunateSonClip != null)
            {
                // Destruir el objeto de audio anterior si existe
                if (currentAudioPlayer != null)
                {
                    UnityEngine.Object.Destroy(currentAudioPlayer);
                }

                // Crear nuevo objeto de audio
                currentAudioPlayer = new GameObject("FortunateSonAudioPlayer");
                UnityEngine.Object.DontDestroyOnLoad(currentAudioPlayer);

                // Agregar el componente AudioSource
                AudioSource source = currentAudioPlayer.AddComponent<AudioSource>();
                source.clip = fortunateSonClip;
                source.loop = true;

                // Buscar el AudioMixerGroup de música
                var musicGroup = Resources
                    .FindObjectsOfTypeAll<AudioMixerGroup>()
                    .FirstOrDefault(g => g.name == "Music_Setting");

                if (musicGroup != null)
                {
                    source.outputAudioMixerGroup = musicGroup;
                    source.volume = 0.7f;
                    Log.LogInfo("[✔] Asignado a MusicVolumeSetting.");
                }
                else if (StaticReferences.Instance?.masterMixerGroup != null)
                {
                    source.outputAudioMixerGroup = StaticReferences.Instance.masterMixerGroup;
                    source.volume = 0.7f;
                    Log.LogWarning("[⚠] Usando masterMixerGroup como fallback.");
                }
                else
                {
                    source.volume = 0.7f;
                    Log.LogError("[✖] No se encontró ningún AudioMixerGroup.");
                }

                source.Play();
                Log.LogInfo("¡'Fortunate Son' debería estar sonando ahora!");
            }
            else
            {
                Log.LogWarning(
                    "El AudioClip de 'Fortunate Son' no se ha cargado. No se puede reproducir la canción."
                );
            }
        }
    }

    [HarmonyPatch(typeof(GameOverHandler), "LoadAirport")]
    public static class EndScreenPatch
    {
        [HarmonyPostfix] // Execute AFTER EndScreenComplete
        public static void Prefix()
        {
            if (currentAudioPlayer != null)
            {
                Log.LogInfo("Deteniendo la música del mod...");
                Destroy(currentAudioPlayer);
                currentAudioPlayer = null; // Set to null to avoid stale references
                Log.LogInfo("Música del mod detenida y objeto destruido.");
            }
            else
            {
                Log.LogWarning("No hay música del mod para detener.");
            }
            Log.LogInfo("Música del mod detenida y objeto destruido.");
        }
    }

    private static AudioMixerGroup? GetMusicMixerGroup()
    {
        foreach (var obj in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
        {
            var type = obj.GetType();

            // Ignorar si el tipo no se llama MusicVolumeSetting
            if (type.Name != "MusicVolumeSetting")
                continue;

            // Obtener campo protegido o privado "mixerGroup" desde la clase base VolumeSetting
            var mixerGroupField = type.BaseType?.GetField(
                "mixerGroup",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

            if (mixerGroupField == null)
                continue;

            var mixerGroup = mixerGroupField.GetValue(obj) as AudioMixerGroup;

            if (mixerGroup != null)
            {
                Log.LogInfo($"[✔] AudioMixerGroup de música encontrado: {mixerGroup.name}");
                return mixerGroup;
            }
        }

        Log.LogWarning(
            "[⚠] No se encontró ninguna instancia válida de MusicVolumeSetting con un AudioMixerGroup."
        );
        return null;
    }
}
