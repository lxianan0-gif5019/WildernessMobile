using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Menu: Wilderness -> 4. Start MCP Server
// Calls the mcp-unity server start without needing a click in its window.
// Uses reflection so this file compiles even when the package assembly changes.
public static class McpControl
{
    [MenuItem("Wilderness/4. Start MCP Server")]
    public static void StartMcp()
    {
        try
        {
            var type = FindType("McpUnity.Unity.McpUnityServer");
            if (type == null) { Debug.Log("[McpControl] McpUnityServer type not found - is the package compiled?"); return; }

            var instProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instProp != null ? instProp.GetValue(null) : null;
            if (instance == null) { Debug.Log("[McpControl] Instance is null"); return; }

            var start = type.GetMethod("StartServer", BindingFlags.Public | BindingFlags.Instance);
            if (start == null) { Debug.Log("[McpControl] StartServer not found"); return; }

            start.Invoke(instance, null);
            Debug.Log("[McpControl] StartServer invoked");

            var listening = type.GetProperty("IsListening", BindingFlags.Public | BindingFlags.Instance);
            if (listening != null)
                Debug.Log("[McpControl] IsListening = " + listening.GetValue(instance));
        }
        catch (Exception e)
        {
            Debug.Log("[McpControl] FAILED: " + e);
        }
    }

    [MenuItem("Wilderness/5. MCP Status")]
    public static void Status()
    {
        var type = FindType("McpUnity.Unity.McpUnityServer");
        if (type == null) { Debug.Log("[McpControl] type not found"); return; }
        var instProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        var instance = instProp != null ? instProp.GetValue(null) : null;
        var listening = type.GetProperty("IsListening", BindingFlags.Public | BindingFlags.Instance);
        Debug.Log("[McpControl] IsListening = " + (instance != null && listening != null ? listening.GetValue(instance) : "?"));
    }

    static Type FindType(string fullName)
    {
        var t = Type.GetType(fullName);
        if (t != null) return t;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                t = asm.GetType(fullName);
                if (t != null) return t;
            }
            catch { }
        }
        return null;
    }
}
