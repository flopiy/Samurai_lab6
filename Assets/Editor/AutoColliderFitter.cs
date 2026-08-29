using UnityEngine;
using UnityEditor;
using System.Linq;

public class AutoColliderFitter : EditorWindow
{
    [MenuItem("Tools/Fit Obstacle Colliders")]
    public static void FitColliders()
    {
        GameObject obstaclesParent = GameObject.Find("Obstacles");
        if (obstaclesParent == null)
        {
            Debug.LogError("Obstacles parent GameObject not found! Please make sure you are in scene 0.unity and have the Obstacles group.");
            return;
        }

        int configuredCount = 0;
        for (int i = 0; i < obstaclesParent.transform.childCount; i++)
        {
            GameObject child = obstaclesParent.transform.GetChild(i).gameObject;
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();

            if (sr == null || sr.sprite == null) continue;

            Undo.RecordObject(child, "Fit Collider and YSort");

            // Ensure YSort exists and is configured
            var ysort = child.GetComponent<YSort>();
            if (ysort == null) ysort = child.AddComponent<YSort>();
            ysort.baseOrder = 100;

            // Configure BoxCollider2D based on sprite type
            var col = child.GetComponent<BoxCollider2D>();
            if (col == null) col = child.AddComponent<BoxCollider2D>();

            // Get sprite dimensions in local space
            Bounds bounds = sr.sprite.bounds;
            Vector2 spriteSize = bounds.size;

            float colWidth = 0.5f;
            float colHeight = 0.3f;
            float colOffsetY = 0f;

            string spriteName = sr.sprite.name.ToLower();

            if (spriteName.Contains("preview_0") || spriteName.Contains("preview_1") || spriteName.Contains("preview_5"))
            {
                // It is a Sakura Tree (trunk collider at bottom)
                colWidth = spriteSize.x * 0.25f;
                colHeight = spriteSize.y * 0.12f;
                colOffsetY = bounds.min.y + (colHeight / 2f);
                ysort.offset = colOffsetY;
            }
            else if (spriteName.Contains("preview_14") || spriteName.Contains("preview_21") || spriteName.Contains("preview_26") || spriteName.Contains("preview_27"))
            {
                // It is a Runic Pillar
                colWidth = spriteSize.x * 0.4f;
                colHeight = spriteSize.y * 0.2f;
                colOffsetY = bounds.min.y + (colHeight / 2f);
                ysort.offset = colOffsetY;
            }
            else if (spriteName.Contains("preview_11"))
            {
                // Barrel
                colWidth = spriteSize.x * 0.65f;
                colHeight = spriteSize.y * 0.35f;
                colOffsetY = bounds.min.y + (colHeight / 2f);
                ysort.offset = colOffsetY;
            }
            else if (spriteName.Contains("preview_13"))
            {
                // Large Pot & Sacks
                colWidth = spriteSize.x * 0.8f;
                colHeight = spriteSize.y * 0.4f;
                colOffsetY = bounds.min.y + (colHeight / 2f);
                ysort.offset = colOffsetY;
            }
            else if (spriteName.Contains("preview_22"))
            {
                // Bonsai Table
                colWidth = spriteSize.x * 0.75f;
                colHeight = spriteSize.y * 0.3f;
                colOffsetY = bounds.min.y + (colHeight / 2f);
                ysort.offset = colOffsetY;
            }
            else if (spriteName.Contains("preview_33"))
            {
                // Stone Lantern
                colWidth = spriteSize.x * 0.5f;
                colHeight = spriteSize.y * 0.25f;
                colOffsetY = bounds.min.y + (colHeight / 2f);
                ysort.offset = colOffsetY;
            }
            else
            {
                // Generic prop fitting
                colWidth = spriteSize.x * 0.6f;
                colHeight = spriteSize.y * 0.3f;
                colOffsetY = bounds.min.y + (colHeight / 2f);
                ysort.offset = colOffsetY;
            }

            col.size = new Vector2(colWidth, colHeight);
            col.offset = new Vector2(0f, colOffsetY);

            EditorUtility.SetDirty(child);
            configuredCount++;
        }

        // Add or update screen Boundaries & fences
        CreateScreenBoundaries();

        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(obstaclesParent);
        Debug.Log("Successfully fitted " + configuredCount + " obstacles with custom colliders and Y-Sorting!");
    }

    private static void CreateScreenBoundaries()
    {
        var boundariesParent = GameObject.Find("Boundaries");
        if (boundariesParent != null)
        {
            Undo.DestroyObjectImmediate(boundariesParent);
        }
        boundariesParent = new GameObject("Boundaries");
        Undo.RegisterCreatedObjectUndo(boundariesParent, "Create Boundaries");

        System.Action<string, Vector2, Vector2> spawnStaticWall = (name, position, size) =>
        {
            GameObject go = new GameObject(name, typeof(BoxCollider2D));
            go.transform.SetParent(boundariesParent.transform);
            go.transform.position = position;
            var col = go.GetComponent<BoxCollider2D>();
            col.size = size;
            Undo.RegisterCreatedObjectUndo(go, "Create Boundaries");
        };

        // Outermost screen walls matching scene dimensions
        spawnStaticWall("Border_Left", new Vector2(-12.8f, 0f), new Vector2(1f, 14f));
        spawnStaticWall("Border_Right", new Vector2(12.7f, 0f), new Vector2(1f, 14f));
        spawnStaticWall("Border_Bottom_Left", new Vector2(-7.4f, -6.1f), new Vector2(10.8f, 1f));
        spawnStaticWall("Border_Bottom_Right", new Vector2(7.4f, -6.1f), new Vector2(10.8f, 1f));
        spawnStaticWall("Border_Bottom_Center", new Vector2(0f, -6.1f), new Vector2(4f, 1f));

        // Upper retaining garden wall ledges (matching image steps)
        spawnStaticWall("Wall_UL_Top", new Vector2(-9.2f, 5.6f), new Vector2(7.5f, 1f));
        spawnStaticWall("Wall_UL_Retaining_Horiz", new Vector2(-9.6f, 1.3f), new Vector2(6.4f, 0.8f));
        spawnStaticWall("Wall_UL_Retaining_Vert_Right", new Vector2(-6.5f, 3.3f), new Vector2(0.8f, 4.2f));
        spawnStaticWall("Wall_UL_Retaining_Vert_Left", new Vector2(-12.5f, 3.3f), new Vector2(0.8f, 4.2f));

        // Dojo Stairs and Porch Walls
        spawnStaticWall("Dojo_Porch_Fence_Left", new Vector2(-3.8f, 1.6f), new Vector2(5.4f, 0.6f));
        spawnStaticWall("Dojo_Porch_Fence_Right", new Vector2(3.8f, 1.6f), new Vector2(5.4f, 0.6f));
        spawnStaticWall("Dojo_Stairs_Wall_Left", new Vector2(-1.4f, 3.5f), new Vector2(0.6f, 3.8f));
        spawnStaticWall("Dojo_Stairs_Wall_Right", new Vector2(1.4f, 3.5f), new Vector2(0.6f, 3.8f));
        spawnStaticWall("Dojo_Back_Wall", new Vector2(0f, 5.4f), new Vector2(3.4f, 1f));

        // Upper-Right Garden / House
        spawnStaticWall("Wall_UR_Retaining_Horiz", new Vector2(9.6f, 1.3f), new Vector2(6.4f, 0.8f));
        spawnStaticWall("Wall_UR_Retaining_Vert_Left", new Vector2(6.4f, 3.3f), new Vector2(0.8f, 4.2f));
        spawnStaticWall("Wall_UR_House_Top", new Vector2(9.6f, 5.6f), new Vector2(6.4f, 1f));
        spawnStaticWall("Wall_UR_Retaining_Vert_Right", new Vector2(12.5f, 3.3f), new Vector2(0.8f, 4.2f));

        // Bottom Left Stone Pond
        spawnStaticWall("Stone_Pond_Left_Bottom", new Vector2(-10.1f, -5.0f), new Vector2(4.8f, 1.8f));

        // Bottom Right Zen Sand Garden
        spawnStaticWall("Zen_Garden_Right_Bottom", new Vector2(9.0f, -2.5f), new Vector2(6.5f, 3.5f));
    }
}