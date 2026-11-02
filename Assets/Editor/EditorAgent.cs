// 职责：文件命令通道，cmd.json → 编辑器执行 → result.json
// 依赖：无
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// File-based control channel for the Unity Editor.
//
// The agent writes  <project>/.agent/cmd.json
// The editor executes it within ~0.3s and writes <project>/.agent/result.json
//
// Supported commands:
//   {"cmd":"ping"}
//   {"cmd":"menu","path":"Wilderness/2. Build Forest"}
//   {"cmd":"screenshot","view":"game","width":1280,"height":720,"out":"D:/.../shot.png"}
//   {"cmd":"save"}
//   {"cmd":"play"} / {"cmd":"stop"}
//   {"cmd":"info"}
[InitializeOnLoad]
public static class EditorAgent
{
    static readonly string AgentDir = Path.Combine(Application.dataPath, "../.agent");
    static string CmdPath => Path.Combine(AgentDir, "cmd.json");
    static string ResultPath => Path.Combine(AgentDir, "result.json");
    static DateTime lastStamp = DateTime.MinValue;
    static bool busy;

    [Serializable]
    public class Command
    {
        public string cmd;
        public string path;
        public string view;
        public string @out;
        public int width;
        public int height;
    }

    [Serializable]
    public class Result
    {
        public bool ok;
        public string cmd;
        public string message;
        public string data;
        public string time;
        public string scene;
        public int objects;
    }

    static EditorAgent()
    {
        EditorApplication.update += Tick;
        try { Directory.CreateDirectory(AgentDir); } catch { }
    }

    static void Tick()
    {
        if (busy) return;
        if (!File.Exists(CmdPath)) return;

        DateTime stamp;
        try { stamp = File.GetLastWriteTimeUtc(CmdPath); } catch { return; }
        if (stamp == lastStamp) return;

        string json;
        try { json = File.ReadAllText(CmdPath); }
        catch { return; }                 // still being written, try again next tick

        lastStamp = stamp;
        busy = true;
        var result = new Result { ok = false, time = DateTime.Now.ToString("HH:mm:ss") };
        Command cmd = null;
        try
        {
            cmd = JsonUtility.FromJson<Command>(json);
            result.cmd = cmd != null ? cmd.cmd : "(null)";
            Execute(cmd, result);
        }
        catch (Exception e)
        {
            result.ok = false;
            result.message = "EXCEPTION: " + e.GetType().Name + ": " + e.Message;
        }
        finally
        {
            try
            {
                var scene = EditorSceneManager.GetActiveScene();
                result.scene = scene.name;
                result.objects = scene.rootCount;
            }
            catch { }
            try { File.WriteAllText(ResultPath, JsonUtility.ToJson(result, true)); } catch { }

            // Consume the command: a domain reload (which happens on entering Play
            // mode) resets the static state, and a leftover cmd.json would be
            // replayed - which used to stop Play mode right after starting it.
            try { File.Delete(CmdPath); } catch { }

            busy = false;
        }
    }

    static void Execute(Command c, Result r)
    {
        if (c == null) { r.message = "empty command"; return; }

        switch ((c.cmd ?? "").Trim().ToLowerInvariant())
        {
            case "ping":
                r.ok = true;
                r.message = "pong";
                r.data = "Unity " + Application.unityVersion;
                return;

            case "info":
                r.ok = true;
                r.message = "editor info";
                r.data = "version=" + Application.unityVersion
                       + "; platform=" + EditorUserBuildSettings.activeBuildTarget
                       + "; playmode=" + EditorApplication.isPlaying
                       + "; datapath=" + Application.dataPath;
                return;

            case "menu":
                if (string.IsNullOrEmpty(c.path)) { r.message = "menu path required"; return; }
                bool ok = EditorApplication.ExecuteMenuItem(c.path);
                r.ok = ok;
                r.message = ok ? "menu executed: " + c.path : "menu NOT found: " + c.path;
                return;

            case "save":
                bool saved = EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                r.ok = saved;
                r.message = saved ? "scenes saved" : "nothing to save";
                return;

            case "play":
                EditorApplication.isPlaying = true;
                r.ok = true; r.message = "play mode on";
                return;

            case "stop":
                EditorApplication.isPlaying = false;
                r.ok = true; r.message = "play mode off";
                return;

            case "screenshot":
                DoScreenshot(c, r);
                return;

            default:
                r.message = "unknown command: " + c.cmd;
                return;
        }
    }

    static void DoScreenshot(Command c, Result r)
    {
        int w = c.width > 0 ? c.width : 1280;
        int h = c.height > 0 ? c.height : 720;
        string outPath = string.IsNullOrEmpty(c.@out)
            ? Path.Combine(AgentDir, "shot.png")
            : c.@out.Replace('/', Path.DirectorySeparatorChar);

        Camera cam = null;
        if ((c.view ?? "game").ToLowerInvariant() == "scene")
        {
            var sv = SceneView.lastActiveSceneView;
            cam = sv != null ? sv.camera : null;
        }
        else
        {
            cam = Camera.main;
            if (cam == null)
            {
                foreach (var c2 in UnityEngine.Object.FindObjectsOfType<Camera>())
                { if (c2.enabled) { cam = c2; break; } }
            }
        }
        if (cam == null) { r.message = "no camera for view " + c.view; return; }

        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        var prevTarget = cam.targetTexture;
        var prevActive = RenderTexture.active;
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            r.ok = true;
            r.message = "screenshot written";
            r.data = outPath;
        }
        finally
        {
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
