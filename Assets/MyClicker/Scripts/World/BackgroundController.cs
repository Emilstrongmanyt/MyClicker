using MyClicker.App;
using UnityEngine;

namespace MyClicker.World
{
    public class BackgroundController : MonoBehaviour
    {
        const int Sky = 41;
        const int Sun = 40;
        const int TreeA = 32;
        const int TreeB = 33;
        const int RockA = 28;
        const int RockB = 29;
        const int BushA = 14;
        const int BushB = 15;
        const int GrassTuft = 3;

        static readonly int[] GroundByZone = { 48, 44, 45, 46, 44, 48, 45, 43, 46, 48 };
        static readonly Color[] SkyTint =
        {
            new Color(0.75f, 0.90f, 1f),
            new Color(0.28f, 0.32f, 0.48f),
            new Color(0.22f, 0.20f, 0.22f),
            new Color(0.62f, 0.66f, 0.70f),
            new Color(0.45f, 0.55f, 0.32f),
            new Color(0.70f, 0.86f, 0.95f),
            new Color(0.38f, 0.34f, 0.32f),
            new Color(0.18f, 0.24f, 0.38f),
            new Color(0.70f, 0.72f, 0.78f),
            new Color(0.42f, 0.28f, 0.38f),
        };

        void Start()
        {
            var services = GameServices.Ensure();
            var sprites = services.Config != null ? services.Config.world.backgroundSprites : null;
            int zone = services.Save != null ? services.Save.Profile.zone : 0;
            zone = Mathf.Clamp(zone, 0, GroundByZone.Length - 1);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.backgroundColor = SkyTint[zone];
            }

            if (sprites == null || sprites.Length < 49)
            {
                Debug.LogWarning("[MyClicker] Background slice set is incomplete.");
                return;
            }

            float worldH = cam != null ? cam.orthographicSize * 2f : 16f;
            float worldW = cam != null ? worldH * cam.aspect : 9f;
            bool night = zone == 1 || zone == 7 || zone == 9;

            Place(sprites[Sky], 0f, 0f, -30, Fill(sprites[Sky], worldW * 1.08f, worldH * 1.08f), SkyTint[zone], false);
            if (!night)
                Place(sprites[Sun], worldW * 0.28f, worldH * 0.28f, -28, 0.9f, Color.white, false);
            else
                Place(sprites[Sun], worldW * 0.26f, worldH * 0.28f, -28, 0.7f, new Color(0.75f, 0.80f, 1f, 0.55f), false);

            Sprite ground = sprites[GroundByZone[zone]];
            float groundScale = 1.05f;
            float tileW = ground != null ? ground.bounds.size.x * groundScale : 2.2f;
            float groundY = -worldH * 0.5f + 0.02f;
            float groundTop = groundY + (ground != null ? ground.bounds.size.y * groundScale * 0.78f : 4.8f);
            int tiles = Mathf.CeilToInt(worldW / Mathf.Max(0.5f, tileW)) + 2;
            float startX = -((tiles - 1) * tileW) * 0.5f;
            for (int i = 0; i < tiles; i++)
                Place(ground, startX + i * tileW, groundY, -18, groundScale, Color.white);

            Place(sprites[TreeA], -worldW * 0.32f, groundTop, -12, 1.15f, Color.white);
            Place(sprites[TreeB], worldW * 0.30f, groundTop, -12, 1.0f, Color.white);
            Place(sprites[RockA], -worldW * 0.12f, groundTop, -11, 0.95f, Color.white);
            Place(sprites[RockB], worldW * 0.08f, groundTop, -11, 0.9f, Color.white);
            Place(sprites[BushA], -worldW * 0.22f, groundTop, -10, 1.2f, Color.white);
            Place(sprites[BushB], worldW * 0.20f, groundTop, -10, 1.15f, Color.white);
            Place(sprites[GrassTuft], -worldW * 0.05f, groundTop, -9, 1.3f, Color.white);
            Place(sprites[GrassTuft], worldW * 0.36f, groundTop, -9, 1.15f, Color.white);
        }

        void Place(Sprite sprite, float x, float y, int order, float scale, Color color, bool sitOnY = true)
        {
            if (sprite == null)
                return;
            var go = new GameObject(sprite.name);
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * scale;
            float py = sitOnY ? y - sprite.bounds.min.y * scale : y;
            go.transform.position = new Vector3(x, py, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.color = color;
        }

        static float Fill(Sprite sprite, float worldW, float worldH)
        {
            if (sprite == null)
                return 1f;
            var size = sprite.bounds.size;
            if (size.x <= 0f || size.y <= 0f)
                return 1f;
            return Mathf.Max(worldW / size.x, worldH / size.y);
        }
    }
}
