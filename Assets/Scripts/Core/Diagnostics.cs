using System;
using System.IO;
using System.Text;
using UnityEngine;

// Writes errors/exception to a file on the device so a crash is not invisible.
// Also records the device profile once per launch.
public class Diagnostics : MonoBehaviour
{
    public static Diagnostics Instance;

    public string errorFileName = "errors.log";
    public long maxBytes = 512 * 1024;

    public string ErrorPath { get { return Path.Combine(Application.persistentDataPath, errorFileName); } }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Application.logMessageReceived += OnLog;
        LogEnvironment();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Application.logMessageReceived -= OnLog;
    }

    void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;

        var sb = new StringBuilder();
        sb.Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("] ").Append(type).Append(": ").Append(condition).Append('\n');
        if (!string.IsNullOrEmpty(stackTrace)) sb.Append(stackTrace).Append('\n');
        Append(sb.ToString());
    }

    void LogEnvironment()
    {
        var sb = new StringBuilder();
        sb.Append("=== launch ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append(" ===\n");
        sb.Append("device: ").Append(SystemInfo.deviceModel).Append('\n');
        sb.Append("os: ").Append(SystemInfo.operatingSystem).Append('\n');
        sb.Append("gpu: ").Append(SystemInfo.graphicsDeviceName).Append('\n');
        sb.Append("memory: ").Append(SystemInfo.systemMemorySize).Append(" MB\n");
        sb.Append("screen: ").Append(Screen.width).Append('x').Append(Screen.height).Append('\n');
        sb.Append("version: ").Append(Application.version).Append('\n');
        Append(sb.ToString());
    }

    void Append(string text)
    {
        try
        {
            var path = ErrorPath;
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                if (info.Length > maxBytes) File.WriteAllText(path, "=== log rotated ===\n");
            }
            File.AppendAllText(path, text);
        }
        catch (Exception)
        {
            // never let logging break the game
        }
    }

    /// <summary>Used by the pause menu to show where the log lives.</summary>
    public string LogPathForDisplay() { return ErrorPath; }
}
