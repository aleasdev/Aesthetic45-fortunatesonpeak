using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using System.IO;
using System.Reflection;
using fortunatesonpeak;
using System;


namespace fortunatesonpeak;

// Here are some basic resources on code style and naming conventions to help
// you in your first CSharp plugin!
// https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
// https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names
// https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces

// This BepInAutoPlugin attribute comes from the Hamunii.BepInEx.AutoPlugin
// NuGet package, and it will generate the BepInPlugin attribute for you!
// For more info, see https://github.com/Hamunii/BepInEx.AutoPlugin

[BepInPlugin("com.aleasdev.fortunatesonpeak", "fortunatesonpeak", "1.0.0")]

public partial class FortunateSonPeakPlugin : BaseUnityPlugin
{

    public static ManualLogSource Log { get; private set; } = null!;

    private static AudioClip fortunateSonClip = null!; 

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo("fortunatesonpeak plugin is loaded!");
        Log.LogInfo("el mod ql esta cargando...");

        LoadAudioClip();

        var harmony = new Harmony("com.aleasdev.fortunatesonpeak");
        harmony.PatchAll();
        Log.LogInfo("el mod ql forme cargado correctamente!");
    }

    private void LoadAudioClip()
    {
        try
        {
            string pluginLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string audioFilePath = Path.Combine(pluginLocation, "fortunateson.wav");

            if (File.Exists(audioFilePath))
            {
                // Unity puede cargar archivos WAV directamente en runtime
                // Necesitamos un GameObject temporal para el AudioSource si lo queremos cargar con UnityWebRequest
                // Ojo: Esto es una Coroutine, así que la carga es asíncrona.
                // Podríamos hacerlo sincrónico si no nos importa bloquear un poco.
                // Para simplificar, y dado que estamos en Awake, lo haremos sincrónico si el tamaño no es un problema.
                // Sin embargo, UnityWebRequest.GetAudioClip es el camino a seguir para archivos más grandes.

                // Para un archivo pequeño y simplicidad, podrías intentar esto (menos robusto para errores de carga):
                // byte[] audioBytes = File.ReadAllBytes(audioFilePath);
                // fortunateSonClip = WavUtility.ToAudioClip(audioBytes, "fortunateson"); // Necesitarías una clase WavUtility

                // La forma asíncrona es la más segura y robusta:
                 GameObject tempGameObject = new GameObject("AudioLoader");
                // Asegúrate de que este objeto no se destruya al cargar escenas, o hazlo persistente.
                DontDestroyOnLoad(tempGameObject); 
                StartCoroutine(LoadWavFile(audioFilePath, tempGameObject)); // Pasamos el GameObject para que la Coroutine lo destruya
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

    // Coroutine para cargar el archivo WAV
    private System.Collections.IEnumerator LoadWavFile(string path, GameObject owner)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError || www.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
            {
                Log.LogError(www.error);
            }
            else
            {
                fortunateSonClip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                if (fortunateSonClip != null)
                {
                    Log.LogInfo("Audio 'fortunateson.wav' cargado correctamente.");
                }
                else
                {
                    Log.LogError("No se pudo obtener el contenido del AudioClip desde el archivo WAV.");
                }
            }
        }
        // Destruir el GameObject temporal después de cargar
        Destroy(owner);
    }

    // --- PARCHE DE HARMONY ---
    [HarmonyPatch(typeof(PeakHandler), "SummonHelicopter")]
    public static class HelicopterSpawnPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            Log.LogInfo("¡Parche activado! Intentando reproducir 'Fortunate Son'...");

            if (FortunateSonPeakPlugin.fortunateSonClip != null)
            {
                // Crear un GameObject temporal para reproducir el sonido.
                // Es importante que no se destruya inmediatamente si el método original
                // causa un cambio de escena o destrucción de objetos.
                GameObject audioPlayer = new GameObject("FortunateSonAudioPlayer");
                // Para que el objeto persista a través de posibles cambios de escena,
                // aunque en un final de nivel, podría no ser estrictamente necesario
                // si la escena no se recarga inmediatamente.
                GameObject.DontDestroyOnLoad(audioPlayer); 

                AudioSource source = audioPlayer.AddComponent<AudioSource>();
                source.clip = FortunateSonPeakPlugin.fortunateSonClip;
                source.Play();

                // Opcional: para que el GameObject se destruya solo después de que termine la canción.
                // Considera la duración de la canción.
                // GameObject.Destroy(audioPlayer, fortunatesonClip.length + 1f); // +1f para dar un pequeño margen

                FortunateSonPeakPlugin.Log.LogInfo("¡'Fortunate Son' debería estar sonando ahora!");
            }
            else
            {
                FortunateSonPeakPlugin.Log.LogWarning("El AudioClip de 'Fortunate Son' no se ha cargado. No se puede reproducir la canción.");
            }
        }
    }


    [HarmonyPatch(typeof(PeakHandler), "EndScreenComplete")]
    public static class EndScreenPatch
    {
        [HarmonyPostfix] // Ejecutar DESPUÉS de EndScreenComplete
        public static void Postfix()
        {
            FortunateSonPeakPlugin.Log.LogInfo("EndScreenComplete detectado. Deteniendo música del mod...");
            // Busca el GameObject que creamos para reproducir la música
            GameObject audioPlayer = GameObject.Find("FortunateSonAudioPlayer");
            if (audioPlayer != null)
            {
                // Destruye el GameObject, lo que detendrá la reproducción y lo eliminará
                GameObject.Destroy(audioPlayer);
                FortunateSonPeakPlugin.Log.LogInfo("Música del mod detenida y objeto destruido.");
            }
        }
    }
    
}
