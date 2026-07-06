using System;
using System.Collections.Generic;
using UnityEngine;

namespace VNGame
{
    [Serializable]
    public sealed class SpriteBinding
    {
        public string key;
        public Sprite sprite;
    }

    public sealed class SpriteAssetProvider : MonoBehaviour
    {
        public Sprite fallbackBackground;
        public Sprite fallbackCharacter;
        [Tooltip("Editor tools populate this from Assets/VNGame/Art/Backgrounds. Keys use folder paths without extensions, for example School/classroom_day.")]
        public List<SpriteBinding> backgrounds = new List<SpriteBinding>();
        [Tooltip("Editor tools populate this from Assets/VNGame/Art/Characters. Keys use folder paths without extensions, for example Rei/happy.")]
        public List<SpriteBinding> characters = new List<SpriteBinding>();

        public Sprite GetBackground(string key)
        {
            return FindSprite(backgrounds, key, fallbackBackground);
        }

        public Sprite GetCharacter(string key)
        {
            return FindSprite(characters, key, fallbackCharacter);
        }

        private static Sprite FindSprite(List<SpriteBinding> bindings, string key, Sprite fallback)
        {
            key = NormalizeKey(key);
            if (!string.IsNullOrWhiteSpace(key))
            {
                foreach (var binding in bindings)
                {
                    if (binding != null && NormalizeKey(binding.key) == key)
                    {
                        return binding.sprite != null ? binding.sprite : fallback;
                    }
                }
            }

            return fallback;
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().Replace('\\', '/');
        }
    }
}
