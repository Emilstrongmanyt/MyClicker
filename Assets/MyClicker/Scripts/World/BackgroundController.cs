using MyClicker.App;
using UnityEngine;

namespace MyClicker.World
{
    public class BackgroundController : MonoBehaviour
    {
        void Start()
        {
            var config = GameServices.Ensure().Config;
            var sprites = config != null ? config.world.backgroundSprites : null;
            if (sprites == null || sprites.Length == 0)
            {
                Camera.main.backgroundColor = new Color(0.36f, 0.48f, 0.28f);
                return;
            }

            int layers = Mathf.Min(3, sprites.Length);
            for (int i = 0; i < layers; i++)
            {
                var go = new GameObject("Bg_" + i);
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(0f, i == 0 ? 0.4f : -0.2f * i, 0f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites[i];
                renderer.sortingOrder = -20 + i;
                FitToCamera(renderer);
            }
        }

        static void FitToCamera(SpriteRenderer renderer)
        {
            if (renderer.sprite == null || Camera.main == null)
                return;
            float worldH = Camera.main.orthographicSize * 2f;
            float worldW = worldH * Camera.main.aspect;
            var size = renderer.sprite.bounds.size;
            if (size.x <= 0f || size.y <= 0f)
                return;
            float scale = Mathf.Max(worldW / size.x, worldH / size.y);
            renderer.transform.localScale = Vector3.one * scale;
        }
    }
}
