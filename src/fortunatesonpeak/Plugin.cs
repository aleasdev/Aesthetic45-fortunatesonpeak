using System; // Make sure System is included for Exception
using System.ComponentModel;
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
    private static AudioClip? fortunateSonClip;
    private static GameObject? currentAudioPlayer;
    public static FortunateSonPeakPlugin Instance { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;

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
            string audioFilePath = Path.Combine(pluginLocation, "FortunateSon.wav");

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

    // [HarmonyPatch(typeof(PeakHandler), "OpenDiscord")]AudioMixerGroup
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

    // Parche para detener la música cuando se carga la pantalla de fin del juego
    // Este parche se activa cuando se llama al método LoadAirport de GameOverHandler.
    [HarmonyPatch(typeof(GameOverHandler), "LoadAirport")]
    public static class EndScreenPatch
    {
        [HarmonyPostfix]
        public static void Prefix()
        {
            Log.LogInfo("¡Parche activado! Deteniendo la música de 'Fortunate Son'...");

            // Verificar si el objeto de audio actual existe y tiene un AudioSource
            if (currentAudioPlayer != null)
            {
                var audioSource = currentAudioPlayer.GetComponent<AudioSource>();

                if (audioSource != null)
                {
                    // Iniciar la corrutina de fade out usando la instancia del plugin
                    // Asegúrate de que FortunateSonPeakPlugin.Instance no sea null
                    if (FortunateSonPeakPlugin.Instance != null)
                    {
                        FortunateSonPeakPlugin.Instance.StartCoroutine(
                            StopAudioAfterFade(audioSource, 2.0f)
                        ); // Duración del fade out
                        Log.LogInfo("¡Iniciando fade out de la música de 'Fortunate Son'!");
                    }
                    else
                    {
                        Log.LogError(
                            "La instancia de FortunateSonPeakPlugin no está disponible para iniciar el fade out. Deteniendo el audio inmediatamente."
                        );
                        audioSource.Stop(); // Detener inmediatamente como fallback
                        UnityEngine.Object.Destroy(currentAudioPlayer);
                        currentAudioPlayer = null;
                    }
                }
                else
                {
                    Log.LogWarning(
                        "No se encontró AudioSource en currentAudioPlayer. Destruyendo inmediatamente."
                    );
                    UnityEngine.Object.Destroy(currentAudioPlayer);
                    currentAudioPlayer = null;
                }
            }
            else
            {
                Log.LogWarning("No hay un reproductor de audio activo para detener la música.");
            }
        }

        private static System.Collections.IEnumerator StopAudioAfterFade(
            AudioSource audioSource,
            float fadeDuration
        )
        {
            float startVolume = audioSource.volume;
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                // Usar Time.unscaledDeltaTime para que el fade no se vea afectado por la escala de tiempo del juego
                elapsedTime += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
                yield return null;
            }

            audioSource.Stop();
            // Opcional: Resetear el volumen a su valor original después de detenerlo,
            // por si el AudioSource se reutiliza más tarde (aunque aquí lo destruimos).
            audioSource.volume = startVolume;

            // Destruir el GameObject que contiene el AudioSource después del fade out
            if (currentAudioPlayer != null)
            {
                Destroy(currentAudioPlayer);
                currentAudioPlayer = null;
                Log.LogInfo("Música del mod desvanecida y objeto destruido.");
            }
        }

        // Método para obtener el AudioMixerGroup de música
        // Este método busca en los recursos del juego para encontrar una instancia de MusicVolumeSetting
        // y extrae su AudioMixerGroup.
        // Si no se encuentra, devuelve null y registra una advertencia.

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
}
