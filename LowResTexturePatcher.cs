using System.Reflection;

using UnityEngine;

namespace ChangeOptimizer;
class LowResTexturePatcher
{
    const string ImageName = "assets.images.screen_lowres.png";
    const string ResourceName = "ChangeOptimizer." + ImageName;

    public static void Apply(Canvas canvas)
    {
        Texture2D tex = LoadEmbeddedTexture();
        if (tex == null) return;

        Transform lowResTransform = canvas.transform.parent?.Find("LowRes");
        if (lowResTransform == null)
        {
            Plugin.Log.LogWarning("[ChangeOptimizer] 'LowRes' child not found next to canvas — texture not injected");
            return;
        }

        Renderer r = lowResTransform.GetComponent<Renderer>();
        if (r == null)
        {
            Plugin.Log.LogWarning("[ChangeOptimizer] 'LowRes' object has no Renderer — texture not injected");
            return;
        }

        var shared = r.sharedMaterials;
        for (int i = 0; i < shared.Length; i++)
        {
            if (shared[i] == null) continue;
            
            Material instance = new(shared[i]) { name = shared[i].name + " (CO Replacement)" };
            if (instance.HasProperty("_MainTex")) instance.SetTexture("_MainTex", tex);
            if (instance.HasProperty("_BaseMap"))  instance.SetTexture("_BaseMap",  tex);

            shared[i] = instance;
        }
        r.sharedMaterials = shared;
    }

    static Texture2D LoadEmbeddedTexture()
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);

        if (stream == null)
        {
            Plugin.Log.LogWarning($"[ChangeOptimizer] Embedded resource '{ResourceName}' not found; LOD patch skipped");
            return null;
        }

        using (stream)
        {
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);

            Texture2D tex = new(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(tex, bytes);
            tex.name = "ChangeOptimizer_ScreenLowRes";
            return tex;
        }
    }
}
