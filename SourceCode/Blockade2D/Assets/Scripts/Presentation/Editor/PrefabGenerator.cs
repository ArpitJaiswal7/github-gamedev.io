using System.IO;
using UnityEditor;
using UnityEngine;
using Blockade2D.Presentation.Views;

namespace Blockade2D.Presentation.Editor
{
    public static class PrefabGenerator
    {
        private const string PrefabDir = "Assets/Resources/Prefabs";

        public static void GeneratePrefabsIfMissing()
        {
            if (!Directory.Exists(PrefabDir))
            {
                Directory.CreateDirectory(PrefabDir);
            }

            Sprite tileSprite = Resources.Load<Sprite>("Sprites/TileRoundedSquare");
            Sprite pawnSprite = Resources.Load<Sprite>("Sprites/PawnCircle");
            Sprite wallSprite = Resources.Load<Sprite>("Sprites/WallRoundedSquare");

            // 1. Tile Prefab
            string tilePrefabPath = $"{PrefabDir}/Tile.prefab";
            if (!File.Exists(tilePrefabPath))
            {
                var go = new GameObject("Tile");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = tileSprite;
                sr.sortingOrder = 0;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
                go.AddComponent<TileView>();

                PrefabUtility.SaveAsPrefabAsset(go, tilePrefabPath);
                Object.DestroyImmediate(go);
            }

            // 2. Pawn Prefab
            string pawnPrefabPath = $"{PrefabDir}/Pawn.prefab";
            if (!File.Exists(pawnPrefabPath))
            {
                var go = new GameObject("Pawn");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = pawnSprite;
                sr.sortingOrder = 10;
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.5f;
                go.AddComponent<PawnView>();

                PrefabUtility.SaveAsPrefabAsset(go, pawnPrefabPath);
                Object.DestroyImmediate(go);
            }

            // 3. Intersection Slot Prefab
            string slotPrefabPath = $"{PrefabDir}/IntersectionSlot.prefab";
            if (!File.Exists(slotPrefabPath))
            {
                var go = new GameObject("IntersectionSlot");
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.85f, 0.85f);
                go.AddComponent<IntersectionSlotView>();

                PrefabUtility.SaveAsPrefabAsset(go, slotPrefabPath);
                Object.DestroyImmediate(go);
            }

            // 4. Wall Prefab
            string wallPrefabPath = $"{PrefabDir}/Wall.prefab";
            if (!File.Exists(wallPrefabPath))
            {
                var go = new GameObject("Wall");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = wallSprite;
                sr.sortingOrder = 5;
                go.AddComponent<WallView>();

                PrefabUtility.SaveAsPrefabAsset(go, wallPrefabPath);
                Object.DestroyImmediate(go);
            }

            // 5. Ghost Wall Prefab
            string ghostPrefabPath = $"{PrefabDir}/GhostWall.prefab";
            if (!File.Exists(ghostPrefabPath))
            {
                var go = new GameObject("GhostWall");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = wallSprite;
                sr.sortingOrder = 6;
                go.AddComponent<GhostWallView>();

                PrefabUtility.SaveAsPrefabAsset(go, ghostPrefabPath);
                Object.DestroyImmediate(go);
            }

            AssetDatabase.Refresh();
            Debug.Log("<color=#10B981>[Blockade2D]</color> Core prefabs verified/generated in Resources/Prefabs!");
        }
    }
}
