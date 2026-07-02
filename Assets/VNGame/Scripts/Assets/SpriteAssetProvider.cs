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
        public List<SpriteBinding> backgrounds = new List<SpriteBinding>();
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
            if (!string.IsNullOrWhiteSpace(key))
            {
                foreach (var binding in bindings)
                {
                    if (binding != null && binding.key == key)
                    {
                        return binding.sprite != null ? binding.sprite : fallback;
                    }
                }
            }

            return fallback;
        }
    }
}
