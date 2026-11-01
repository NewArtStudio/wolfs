using System;
using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// The 3D model and textures of one character. Filled automatically by the VampWolfs menu
    /// from Assets/VampWolfs/Art/Characters/&lt;Name&gt;/. Height and facing can be tuned in the Inspector.
    /// </summary>
    [Serializable]
    public class CharacterArt
    {
        public GameObject Model;
        public Texture2D BaseColor;
        public Texture2D Normal;

        [Tooltip("Model height in meters after auto-scaling.")]
        public float Height = 2f;

        [Tooltip("Extra rotation if the model faces the wrong way (try 180).")]
        public float YawOffset;

        public bool IsValid
        {
            get { return Model != null; }
        }

        public CharacterArt(float height)
        {
            Height = height;
        }
    }

    /// <summary>Art used by CharacterFactory. Set once by GameBootstrap at startup.</summary>
    public static class ArtLibrary
    {
        public static CharacterArt Vampire;
        public static CharacterArt Werewolf;
        public static CharacterArt MonsterHunter;
    }
}
