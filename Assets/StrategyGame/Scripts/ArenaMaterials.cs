using UnityEngine;

namespace StrategyGame
{
    public static class ArenaMaterials
    {
        public static Material CreateOpaque(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Unlit/Color");

            var material = new Material(shader);
            ApplyColor(material, color);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            return material;
        }

        public static Material CreateTransparent(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Sprites/Default");

            var material = new Material(shader);
            ApplyColor(material, color);

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }

        static void ApplyColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            material.color = color;
        }
    }
}
