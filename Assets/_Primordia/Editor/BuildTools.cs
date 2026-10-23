using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Primordia.Editor
{
    /// <summary>
    /// Build web (WebGL) lista para subir a itch.io. La build se guarda en Builds/WebGL, que Git ignora, y al acabar
    /// se comprime en Builds/Primordia-WebGL.zip con index.html en la raíz, que es lo que se sube.
    /// </summary>
    public static class BuildTools
    {
        const string WebGLFolder = "Builds/WebGL";
        const string ZipPath = "Builds/Primordia-WebGL.zip";

        // 2× la resolución del juego (480×270): los píxeles se ven nítidos sin pantalla completa.
        const int WebWidth = 960;
        const int WebHeight = 540;

        [MenuItem("Primordia/Build/Build WebGL (itch.io)")]
        static void BuildWebGL()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("No hay escenas en el build. Usa Primordia > UI > Crear menú principal y escena de juego.");
                return;
            }

            ConfigureWebGL();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = WebGLFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"La build WebGL falló ({summary.result}) con {summary.totalErrors} errores. Revisa la Console.");
                return;
            }

            bool zipped = CreateZip();
            Debug.Log($"Build WebGL lista en {Path.GetFullPath(WebGLFolder)} " +
                      $"({summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalSeconds:0} s). " +
                      (zipped
                          ? $"Para itch.io, sube {Path.GetFullPath(ZipPath)}."
                          : "No se pudo crear el .zip: comprime el contenido de esa carpeta (index.html en la raíz del zip)."));
            EditorUtility.RevealInFinder(zipped ? ZipPath : Path.Combine(WebGLFolder, "index.html"));
        }

        /// <summary>
        /// Comprime la build con el tar.exe de Windows: Compress-Archive de PowerShell 5.1 guarda las rutas con barras
        /// invertidas y itch.io no encuentra las carpetas. Se nombran las entradas de la raíz una a una, así no llevan
        /// el prefijo "./".
        /// </summary>
        static bool CreateZip()
        {
            try
            {
                if (File.Exists(ZipPath))
                    File.Delete(ZipPath);

                var entries = Directory.GetFileSystemEntries(WebGLFolder).Select(e => $"\"{Path.GetFileName(e)}\"");
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "tar.exe",
                    Arguments = $"-a -c -f \"{Path.GetFullPath(ZipPath)}\" {string.Join(" ", entries)}",
                    WorkingDirectory = Path.GetFullPath(WebGLFolder),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                });
                string errors = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    Debug.LogWarning($"tar.exe terminó con el código {process.ExitCode}: {errors}");
                    return false;
                }
                return File.Exists(ZipPath);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"No se pudo crear {ZipPath}: {exception.Message}");
                return false;
            }
        }

        /// <summary>Ajustes de WebGL que funcionan en itch.io.</summary>
        [MenuItem("Primordia/Build/Configurar ajustes WebGL")]
        internal static void ConfigureWebGL()
        {
            PlayerSettings.productName = "Primordia";
            PlayerSettings.defaultWebScreenWidth = WebWidth;
            PlayerSettings.defaultWebScreenHeight = WebHeight;

            // itch.io no envía la cabecera que necesita Brotli; Gzip con descompresión de respaldo funciona siempre.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            // Guarda los datos en la caché del navegador: la segunda visita carga mucho más rápido.
            PlayerSettings.WebGL.dataCaching = true;

            AssetDatabase.SaveAssets();
            Debug.Log($"Ajustes WebGL configurados: {WebWidth}×{WebHeight}, Gzip con descompresión de respaldo.");
        }
    }
}
