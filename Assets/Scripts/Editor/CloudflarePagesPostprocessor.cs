#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Match3.EditorTools
{
    public static class CloudflarePagesPostprocessor
    {
        private const string HeaderTemplateRelativePath = "Tools/cloudflare-pages/_headers";
        private const string HeaderFileName = "_headers";

        [PostProcessBuild(1000)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.WebGL)
            {
                return;
            }

            string outputRoot = Path.GetFullPath(pathToBuiltProject);
            string indexPath = Path.Combine(outputRoot, "index.html");
            string buildDirectory = Path.Combine(outputRoot, "Build");
            string destinationPath = Path.Combine(outputRoot, HeaderFileName);

            RequireFile(indexPath,
                "WebGL output has no index.html. Check the selected build destination.");

            if (!Directory.Exists(buildDirectory))
            {
                throw new BuildFailedException(
                    $"[WebGLHosting] WebGL Build directory was not found: {buildDirectory}");
            }

            if (PlayerSettings.WebGL.decompressionFallback)
            {
                RequireBuildAsset(buildDirectory, ".data.unityweb", "Decompression Fallback");
                RequireBuildAsset(buildDirectory, ".wasm.unityweb", "Decompression Fallback");
                RequireBuildAsset(buildDirectory, ".js.unityweb", "Decompression Fallback");

                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }

                Debug.Log(
                    "[WebGLHosting] Decompression Fallback build validated. " +
                    "Deploy index.html, Build and TemplateData together; no _headers file is required.");
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new BuildFailedException(
                    "[CloudflarePages] Could not resolve the Unity project root.");
            }

            string templatePath = Path.Combine(projectRoot, HeaderTemplateRelativePath);
            RequireFile(templatePath,
                "Cloudflare header template is missing. Restore Tools/cloudflare-pages/_headers.");
            RequireBuildAsset(buildDirectory, ".data.gz", "Gzip without Decompression Fallback");
            RequireBuildAsset(buildDirectory, ".wasm.gz", "Gzip without Decompression Fallback");
            RequireBuildAsset(buildDirectory, ".js.gz", "Gzip without Decompression Fallback");

            File.Copy(templatePath, destinationPath, overwrite: true);
            Debug.Log(
                $"[CloudflarePages] Added gzip response headers to WebGL output: {destinationPath}");
        }

        private static void RequireFile(string path, string message)
        {
            if (!File.Exists(path))
            {
                throw new BuildFailedException($"[WebGLHosting] {message} Path: {path}");
            }
        }

        private static void RequireBuildAsset(string buildDirectory, string suffix, string mode)
        {
            foreach (string path in Directory.EnumerateFiles(buildDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                if (path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            throw new BuildFailedException(
                $"[WebGLHosting] Expected a '{suffix}' asset in '{buildDirectory}' for mode '{mode}'. " +
                "Delete the old output and make a complete WebGL build so files from different " +
                "compression modes cannot be mixed.");
        }
    }
}
#endif
