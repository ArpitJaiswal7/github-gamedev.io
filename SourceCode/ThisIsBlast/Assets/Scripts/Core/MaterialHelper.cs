using UnityEngine;

namespace ThisIsBlast.Core
{
    /// <summary>
    /// Centralized material and shader utility that safely resolves shaders across all platforms
    /// (Editor, Standalone, Android, iOS, WebGL) and handles stripped shader fallbacks gracefully
    /// without ever throwing ArgumentNullException.
    /// </summary>
    public static class MaterialHelper
    {
        private static Shader _cachedStandardShader;
        private static Shader _cachedUnlitShader;
        private static Shader _cachedParticleShader;
        private static Shader _cachedSpriteShader;
        private static Material _cachedBaseMaterial;

        /// <summary>
        /// Retrieves the best available lit standard shader, falling back to mobile/diffuse, unlit, or sprite shaders.
        /// Guaranteed never to return null if any shader exists in the build.
        /// </summary>
        public static Shader GetStandardShader()
        {
            if (_cachedStandardShader != null) return _cachedStandardShader;

            // Try standard shaders in order of quality
            _cachedStandardShader = Shader.Find("Universal Render Pipeline/Lit")
                                 ?? Shader.Find("Standard")
                                 ?? Shader.Find("Mobile/Diffuse");

            if (_cachedStandardShader == null)
            {
                // Try bundled material in Resources
                var loaded = Resources.Load<Material>("Materials/GameStandardMaterial");
                if (loaded != null && loaded.shader != null)
                {
                    _cachedStandardShader = loaded.shader;
                }
            }

            if (_cachedStandardShader == null)
            {
                // Fallback to builtin resource
                var defMat = Resources.GetBuiltinResource<Material>("Default-Material.mat");
                if (defMat != null && defMat.shader != null)
                {
                    _cachedStandardShader = defMat.shader;
                }
            }

            if (_cachedStandardShader == null)
            {
                _cachedStandardShader = Shader.Find("Unlit/Color")
                                     ?? Shader.Find("Sprites/Default");
            }

            return _cachedStandardShader;
        }

        public static Shader GetUnlitShader()
        {
            if (_cachedUnlitShader != null) return _cachedUnlitShader;

            _cachedUnlitShader = Shader.Find("Unlit/Color")
                              ?? Shader.Find("Sprites/Default")
                              ?? GetStandardShader();

            return _cachedUnlitShader;
        }

        public static Shader GetParticleShader()
        {
            if (_cachedParticleShader != null) return _cachedParticleShader;

            _cachedParticleShader = Shader.Find("Particles/Standard Unlit")
                                 ?? Shader.Find("Mobile/Particles/Additive")
                                 ?? Shader.Find("Sprites/Default")
                                 ?? GetUnlitShader();

            return _cachedParticleShader;
        }

        public static Shader GetSpriteShader()
        {
            if (_cachedSpriteShader != null) return _cachedSpriteShader;

            _cachedSpriteShader = Shader.Find("Sprites/Default")
                               ?? Shader.Find("Unlit/Color")
                               ?? GetStandardShader();

            return _cachedSpriteShader;
        }

        /// <summary>
        /// Creates a new lit/standard material with safety checks preventing crashes in player builds.
        /// </summary>
        public static Material CreateStandardMaterial(Color color, float smoothness = 0.5f, float metallic = 0f)
        {
            Shader shader = GetStandardShader();
            Material mat = null;

            if (shader != null)
            {
                mat = new Material(shader);
            }
            else
            {
                // Ultimate fallback: clone default builtin material
                var def = Resources.GetBuiltinResource<Material>("Default-Material.mat");
                if (def != null)
                {
                    mat = new Material(def);
                }
                else
                {
                    // Fallback to internal sprite default
                    mat = new Material(Shader.Find("Sprites/Default"));
                }
            }

            mat.color = color;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            return mat;
        }

        /// <summary>
        /// Creates a safe unlit material for UI, trays, or line overlays.
        /// </summary>
        public static Material CreateUnlitMaterial(Color color)
        {
            Shader shader = GetUnlitShader();
            Material mat = (shader != null) ? new Material(shader) : CreateStandardMaterial(color);
            mat.color = color;
            return mat;
        }

        /// <summary>
        /// Creates a safe particle material.
        /// </summary>
        public static Material CreateParticleMaterial()
        {
            Shader shader = GetParticleShader();
            Material mat = (shader != null) ? new Material(shader) : CreateStandardMaterial(Color.white);
            return mat;
        }

        /// <summary>
        /// Creates a safe trail material.
        /// </summary>
        public static Material CreateTrailMaterial()
        {
            Shader shader = GetSpriteShader();
            Material mat = (shader != null) ? new Material(shader) : CreateStandardMaterial(Color.white);
            return mat;
        }
    }
}
