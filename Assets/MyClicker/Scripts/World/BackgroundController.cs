using MyClicker.App;
using MyClicker.Data;
using UnityEngine;

namespace MyClicker.World
{
    public class BackgroundController : MonoBehaviour
    {
        int _builtZone = -1;
        Transform _root;

        static readonly Color[] GroundTint =
        {
            new Color(0.36f, 0.52f, 0.24f),
            new Color(0.18f, 0.24f, 0.30f),
            new Color(0.22f, 0.26f, 0.18f),
            new Color(0.38f, 0.38f, 0.40f),
            new Color(0.24f, 0.30f, 0.18f),
            new Color(0.28f, 0.42f, 0.20f),
            new Color(0.34f, 0.32f, 0.26f),
            new Color(0.12f, 0.14f, 0.18f),
            new Color(0.40f, 0.38f, 0.34f),
            new Color(0.28f, 0.16f, 0.16f),
        };

        void Start() => Rebuild(true);

        void LateUpdate()
        {
            var services = GameServices.Instance;
            int zone = services != null && services.Save != null ? services.Save.Profile.zone : 0;
            if (zone != _builtZone)
                Rebuild(false);
        }

        void Rebuild(bool force)
        {
            var services = GameServices.Ensure();
            int zone = services.Save != null ? services.Save.Profile.zone : 0;
            var catalog = services.Catalog;
            if (catalog != null && catalog.zones != null && catalog.zones.Length > 0)
                zone = Mathf.Clamp(zone, 0, catalog.zones.Length - 1);
            else
                zone = Mathf.Clamp(zone, 0, GroundTint.Length - 1);
            if (!force && zone == _builtZone)
                return;
            _builtZone = zone;

            if (_root != null)
                Destroy(_root.gameObject);
            var go = new GameObject("ZoneArt");
            go.transform.SetParent(transform, false);
            _root = go.transform;

            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.backgroundColor = GroundTint[Mathf.Clamp(zone, 0, GroundTint.Length - 1)];
            }

            var world = services.Config != null ? services.Config.world : new GameConfig.WorldSettings();
            BuildTopDown(zone, world, cam);
        }

        void BuildTopDown(int zone, GameConfig.WorldSettings world, Camera cam)
        {
            float worldH = cam != null ? cam.orthographicSize * 2f : 16f;
            float worldW = cam != null ? worldH * cam.aspect : 9f;
            var theme = ThemeFor(zone);
            var grassFill = FillTiles(world.grassTiles, "Flower");
            var stoneFill = FillTiles(world.stoneTiles, null);
            var floor = theme.stoneFloor ? FirstLive(stoneFill, grassFill) : FirstLive(grassFill, stoneFill);
            var path = theme.stonePath ? FirstLive(stoneFill, grassFill) : FirstLive(grassFill, stoneFill);
            if (floor == null || floor.Length == 0)
                return;

            Sprite sample = floor[0];
            float native = Mathf.Max(0.25f, sample.bounds.size.x);
            float scale = 1.35f;
            float tile = native * scale * 0.98f;
            int cols = Mathf.CeilToInt(worldW / tile) + 3;
            int rows = Mathf.CeilToInt(worldH / tile) + 3;
            float originX = -cols * tile * 0.5f + tile * 0.5f;
            float originY = -rows * tile * 0.5f + tile * 0.5f;
            float pathHalf = 1.7f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float x = originX + c * tile;
                    float y = originY + r * tile;
                    bool onPath = Mathf.Abs(x) <= pathHalf;
                    var set = onPath ? path : floor;
                    var sprite = Pick(set, zone, r * 37 + c * 17 + zone * 91);
                    Place(sprite, x, y, -40, scale, theme.tint, false);
                }
            }

            if (theme.walls)
                BuildBorderWalls(world.wallTiles, worldW, worldH, theme.tint, zone);
            if (theme.stairs)
                BuildStairs(world.propSprites, theme.tint);

            Scatter(world, theme, zone, worldW, worldH, pathHalf);
        }

        void BuildBorderWalls(Sprite[] walls, float worldW, float worldH, Color tint, int zone)
        {
            if (walls == null || walls.Length == 0)
                return;
            float tile = 1.25f;
            int count = Mathf.CeilToInt(worldW / tile) + 1;
            float start = -count * tile * 0.5f + tile * 0.5f;
            float north = worldH * 0.46f;
            float south = -worldH * 0.46f;
            for (int i = 0; i < count; i++)
            {
                float x = start + i * tile;
                Place(Pick(walls, zone, i * 13 + 4), x, north, -18, 1.2f, tint, false);
                Place(Pick(walls, zone, i * 17 + 9), x, south, -18, 1.2f, tint, false);
            }
        }

        void BuildStairs(Sprite[] props, Color tint)
        {
            Place(Find(props, "TX Stairs M L", "TX Props Stair S L"), -1.2f, 4.6f, -15, 1.2f, tint, true);
            Place(Find(props, "TX Stairs M M", "TX Props Stair NM"), 0f, 4.6f, -15, 1.2f, tint, true);
            Place(Find(props, "TX Stairs M R", "TX Props Stair S R"), 1.2f, 4.6f, -15, 1.2f, tint, true);
        }

        void Scatter(GameConfig.WorldSettings world, Theme theme, int zone, float worldW, float worldH, float pathHalf)
        {
            var plants = world.plantSprites;
            var props = world.propSprites;
            float left = -worldW * 0.38f;
            float right = worldW * 0.38f;
            float midY = -0.4f;
            float highY = worldH * 0.22f;
            float lowY = -worldH * 0.28f;

            PlaceTree(plants, props, "T1", left, highY, theme.tint);
            PlaceTree(plants, props, "T2", right, highY * 0.72f, theme.tint);
            if (theme.trees >= 3)
                PlaceTree(plants, props, "T3", left * 0.55f, highY * 0.55f, theme.tint);
            if (theme.trees >= 4)
                PlaceTree(plants, props, "T1", right * 0.62f, lowY + 1.6f, theme.tint);

            Place(Find(plants, "TX Bush T1", "TX Props Bush A"), left * 0.72f, midY, -11, 1.35f, theme.tint, true);
            Place(Find(plants, "TX Bush T3", "TX Props Bush C"), right * 0.70f, midY - 0.4f, -11, 1.3f, theme.tint, true);
            Place(Find(plants, "TX Grass A", "TX Props Grass A"), left * 0.20f, lowY, -9, 1.25f, theme.tint, true);
            Place(Find(plants, "TX Grass F", "TX Props Grass F"), right * 0.22f, lowY + 0.35f, -9, 1.2f, theme.tint, true);
            Place(Find(plants, "TX Flower", "TX Props Grass Flower"), left * 0.48f, lowY + 0.8f, -9, 1.2f, theme.tint, true);

            for (int i = 0; i < theme.propNames.Length; i++)
            {
                float side = (i % 2 == 0) ? left : right;
                float y = highY - 1.1f - (i / 2) * 1.55f;
                if (Mathf.Abs(side) < pathHalf)
                    side = side < 0f ? -pathHalf - 0.6f : pathHalf + 0.6f;
                Place(Find(props, theme.propNames[i]), side * (0.78f + 0.08f * (i % 3)), y, -10, 1.25f, theme.tint, true);
            }
        }

        void PlaceTree(Sprite[] plants, Sprite[] props, string key, float x, float y, Color tint)
        {
            var lower = Find(plants, "TX Tree " + key + " Lower", "TX Props Tree " + key + " Lower");
            var upper = Find(plants, "TX Tree " + key + " Upper", "TX Props Tree " + key + " Upper");
            if (lower == null && upper == null)
            {
                Place(Find(props, "TX Props Tree A", "TX Props Tree T3 Lower"), x, y, -12, 1.55f, tint, true);
                return;
            }

            Place(lower, x, y, -12, 1.55f, tint, true);
            if (upper != null)
            {
                float lift = lower != null ? lower.bounds.size.y * 1.55f * 0.72f : 1.2f;
                Place(upper, x, y + lift, -8, 1.55f, tint, true);
            }
        }

        void Place(Sprite sprite, float x, float y, int order, float scale, Color color, bool sitOnY = true)
        {
            if (sprite == null || _root == null)
                return;
            var go = new GameObject(sprite.name);
            go.transform.SetParent(_root, false);
            go.transform.localScale = Vector3.one * scale;
            float py = sitOnY ? y - sprite.bounds.min.y * scale : y;
            go.transform.position = new Vector3(x, py, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.color = color;
        }

        static Theme ThemeFor(int zone)
        {
            switch (zone)
            {
                case 1:
                    return new Theme(false, true, false, false, 3, new Color(0.62f, 0.70f, 0.92f),
                        "TX Props Stone Lantern", "TX Props Pot A", "TX Props Well");
                case 2:
                    return new Theme(false, true, false, false, 2, new Color(0.70f, 0.78f, 0.62f),
                        "TX Props Barrel", "TX Props Crate", "TX Props Brick T1");
                case 3:
                    return new Theme(true, false, true, false, 1, new Color(0.86f, 0.86f, 0.88f),
                        "TX Struct Gate T1 T", "TX Props Pillar", "TX Props Wooden Gate");
                case 4:
                    return new Theme(true, false, true, false, 1, new Color(0.72f, 0.82f, 0.58f),
                        "TX Props Barrel", "TX Props Brick T2", "TX Props Pot C", "TX Props Crate Small");
                case 5:
                    return new Theme(false, true, false, false, 4, new Color(0.78f, 0.90f, 0.70f),
                        "TX Props Road Sign E", "TX Props Bush D", "TX Props Stone T2");
                case 6:
                    return new Theme(true, false, false, false, 1, new Color(0.88f, 0.84f, 0.76f),
                        "TX Props Gravestone A", "TX Props Gravestone B", "TX Props Stone Coffin V", "TX Props Statue");
                case 7:
                    return new Theme(true, false, true, false, 1, new Color(0.55f, 0.62f, 0.82f),
                        "TX Props Altar", "TX Props Rune Pillar X2", "TX Props Rune Pillar Broken");
                case 8:
                    return new Theme(true, false, true, true, 1, new Color(0.90f, 0.88f, 0.82f),
                        "TX Props Pillar", "TX Props Statue", "TX Props Stone Cube");
                case 9:
                    return new Theme(false, true, false, false, 3, new Color(0.92f, 0.62f, 0.48f),
                        "TX Props Altar", "TX Props Stone Lantern", "TX Props Chest");
                default:
                    return new Theme(false, true, false, false, 2, Color.white,
                        "TX Props Road Sign W", "TX Props Barrel", "TX Props Well", "TX Props Crate");
            }
        }

        static Sprite[] FillTiles(Sprite[] source, string exclude)
        {
            if (source == null || source.Length == 0)
                return source;
            int count = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (UsableFill(source[i], exclude))
                    count++;
            }

            if (count == 0)
                return source;
            var list = new Sprite[count];
            int n = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (UsableFill(source[i], exclude))
                    list[n++] = source[i];
            }

            return list;
        }

        static bool UsableFill(Sprite sprite, string exclude)
        {
            if (sprite == null)
                return false;
            if (string.IsNullOrEmpty(exclude))
                return true;
            return sprite.name.IndexOf(exclude, System.StringComparison.OrdinalIgnoreCase) < 0;
        }

        static Sprite[] FirstLive(params Sprite[][] options)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] != null && options[i].Length > 0)
                    return options[i];
            }

            return null;
        }

        static Sprite Pick(Sprite[] list, int zone, int salt)
        {
            if (list == null || list.Length == 0)
                return null;
            int i = Mathf.Abs(zone * 13 + salt) % list.Length;
            return list[i];
        }

        static Sprite Find(Sprite[] list, params string[] names)
        {
            if (list == null || names == null)
                return null;
            for (int n = 0; n < names.Length; n++)
            {
                for (int i = 0; i < list.Length; i++)
                {
                    if (list[i] != null && list[i].name == names[n])
                        return list[i];
                }
            }

            for (int n = 0; n < names.Length; n++)
            {
                for (int i = 0; i < list.Length; i++)
                {
                    if (list[i] != null && list[i].name.IndexOf(names[n], System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return list[i];
                }
            }

            return null;
        }

        readonly struct Theme
        {
            public readonly bool stoneFloor;
            public readonly bool stonePath;
            public readonly bool walls;
            public readonly bool stairs;
            public readonly int trees;
            public readonly Color tint;
            public readonly string[] propNames;

            public Theme(bool stoneFloor, bool stonePath, bool walls, bool stairs, int trees, Color tint, params string[] propNames)
            {
                this.stoneFloor = stoneFloor;
                this.stonePath = stonePath;
                this.walls = walls;
                this.stairs = stairs;
                this.trees = trees;
                this.tint = tint;
                this.propNames = propNames ?? System.Array.Empty<string>();
            }
        }
    }
}
