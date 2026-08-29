using System.Collections.Generic;
using UnityEngine;

namespace MyClicker.Economy
{
    public static class StarLayout
    {
        const float Spoke = 148f;
        const float Wheel = 118f;
        const int SpokeCount = 7;
        const int OuterTravel = 5;

        struct Region
        {
            public string id;
            public float ang;
            public string tag;
            public string minorIcon;
            public string innerId;
            public string innerTitle;
            public string innerBlurb;
            public string innerIcon;
            public string outerId;
            public string outerTitle;
            public string outerBlurb;
            public string outerIcon;
            public string keyId;
            public string keyTitle;
            public string keyBlurb;
            public string keyIcon;
            public string forkA;
            public string forkATitle;
            public string forkABlurb;
            public string forkAIcon;
            public string forkB;
            public string forkBTitle;
            public string forkBBlurb;
            public string forkBIcon;
            public string extraId;
            public string extraTitle;
            public string extraBlurb;
            public string extraIcon;
            public string extra2Id;
            public string extra2Title;
            public string extra2Blurb;
            public string extra2Icon;
        }

        public static StarNode[] Build()
        {
            var nodes = new List<StarNode>(220);
            var links = new List<string>(400);

            var origin = Place(StarIds.FirstLight, "First Light", "The center of the Star Chart. Clear waves to earn Stars.",
                "laurel", null, StarKind.Keystone, 0, 0f, 0f, 0);
            nodes.Add(origin);

            var regions = Regions();
            string[] innerHub = new string[regions.Length];
            for (int r = 0; r < regions.Length; r++)
                innerHub[r] = BuildRegion(nodes, links, origin.id, regions[r]);

            for (int r = 0; r < regions.Length; r++)
            {
                int n = (r + 1) % regions.Length;
                string a = regions[r].id + "_iw2";
                string b = regions[n].id + "_iw6";
                if (Find(nodes, a) != null && Find(nodes, b) != null)
                    Join(links, a, b);
            }

            BakeNeighbors(nodes, links);
            return nodes.ToArray();
        }

        static string BuildRegion(List<StarNode> nodes, List<string> links, string originId, Region region)
        {
            string prev = originId;
            for (int i = 0; i < SpokeCount; i++)
            {
                float dist = Spoke * (i + 1);
                var p = Polar(region.ang, dist);
                string id = region.id + "_s" + i;
                nodes.Add(Place(id, "Star", "+2.5% " + Label(region.tag) + ".", region.minorIcon, region.tag,
                    StarKind.Minor, 1, p.x, p.y));
                Join(links, prev, id);
                prev = id;
            }

            var innerAt = Polar(region.ang, Spoke * (SpokeCount + 1));
            nodes.Add(Place(region.innerId, region.innerTitle, region.innerBlurb, region.innerIcon, null,
                StarKind.Notable, 1, innerAt.x, innerAt.y, region.innerId));
            Join(links, prev, region.innerId);

            for (int i = 0; i < 8; i++)
            {
                float a = region.ang + i * 45f;
                var p = innerAt + Polar(a, Wheel);
                bool forkA = i == 0 && !string.IsNullOrEmpty(region.forkA);
                bool forkB = i == 4 && !string.IsNullOrEmpty(region.forkB);
                string id;
                if (forkA)
                {
                    id = region.forkA;
                    nodes.Add(Place(id, region.forkATitle, region.forkABlurb, region.forkAIcon, null,
                        StarKind.Notable, 1, p.x, p.y, id, region.forkB));
                }
                else if (forkB)
                {
                    id = region.forkB;
                    nodes.Add(Place(id, region.forkBTitle, region.forkBBlurb, region.forkBIcon, null,
                        StarKind.Notable, 1, p.x, p.y, id, region.forkA));
                }
                else
                {
                    id = region.id + "_iw" + i;
                    nodes.Add(Place(id, "Star", "+2.5% " + Label(region.tag) + ".", region.minorIcon, region.tag,
                        StarKind.Minor, 1, p.x, p.y));
                }

                Join(links, region.innerId, id);
                if (i > 0)
                    Join(links, WheelId(region, i - 1), id);
                if (i == 7)
                    Join(links, id, WheelId(region, 0));
            }

            prev = region.innerId;
            for (int i = 0; i < OuterTravel; i++)
            {
                float dist = Spoke * (SpokeCount + 2 + i);
                var p = Polar(region.ang, dist);
                string id = region.id + "_o" + i;
                nodes.Add(Place(id, "Star", "+2.5% " + Label(region.tag) + ".", region.minorIcon, region.tag,
                    StarKind.Minor, 1, p.x, p.y));
                Join(links, prev, id);
                prev = id;
            }

            var outerAt = Polar(region.ang, Spoke * (SpokeCount + 2 + OuterTravel));
            nodes.Add(Place(region.outerId, region.outerTitle, region.outerBlurb, region.outerIcon, null,
                StarKind.Notable, 1, outerAt.x, outerAt.y, region.outerId));
            Join(links, prev, region.outerId);

            for (int i = 0; i < 8; i++)
            {
                float a = region.ang + i * 45f;
                var p = outerAt + Polar(a, Wheel);
                string id;
                if (i == 2 && !string.IsNullOrEmpty(region.extraId))
                {
                    id = region.extraId;
                    nodes.Add(Place(id, region.extraTitle, region.extraBlurb, region.extraIcon, null,
                        StarKind.Notable, 1, p.x, p.y, id));
                }
                else if (i == 6 && !string.IsNullOrEmpty(region.extra2Id))
                {
                    id = region.extra2Id;
                    nodes.Add(Place(id, region.extra2Title, region.extra2Blurb, region.extra2Icon, null,
                        StarKind.Notable, 1, p.x, p.y, id));
                }
                else
                {
                    id = region.id + "_ow" + i;
                    nodes.Add(Place(id, "Star", "+2.5% " + Label(region.tag) + ".", region.minorIcon, region.tag,
                        StarKind.Minor, 1, p.x, p.y));
                }

                Join(links, region.outerId, id);
                if (i > 0)
                    Join(links, OuterWheelId(region, i - 1), id);
                if (i == 7)
                    Join(links, id, OuterWheelId(region, 0));
            }

            var keyAt = Polar(region.ang, Spoke * (SpokeCount + 3 + OuterTravel) + 40f);
            nodes.Add(Place(region.keyId, region.keyTitle, region.keyBlurb, region.keyIcon, null,
                StarKind.Keystone, 2, keyAt.x, keyAt.y, region.keyId));
            Join(links, region.outerId, region.keyId);
            return region.innerId;
        }

        static string WheelId(Region region, int i)
        {
            if (i == 0 && !string.IsNullOrEmpty(region.forkA))
                return region.forkA;
            if (i == 4 && !string.IsNullOrEmpty(region.forkB))
                return region.forkB;
            return region.id + "_iw" + i;
        }

        static string OuterWheelId(Region region, int i)
        {
            if (i == 2 && !string.IsNullOrEmpty(region.extraId))
                return region.extraId;
            if (i == 6 && !string.IsNullOrEmpty(region.extra2Id))
                return region.extra2Id;
            return region.id + "_ow" + i;
        }

        static Region[] Regions()
        {
            return new[]
            {
                new Region
                {
                    id = "tap", ang = 0f, tag = StarTags.Tap, minorIcon = "picto_sword",
                    innerId = StarIds.HeavyTap, innerTitle = "Heavy Tap", innerBlurb = "+8% tap vs bosses.", innerIcon = "sword_a",
                    outerId = StarIds.CritSpark, outerTitle = "Crit Spark", outerBlurb = "+0.15 crit mul on taps.", outerIcon = "target",
                    keyId = StarIds.Godhand, keyTitle = "Godhand", keyBlurb = "+15% tap, −10% auto.", keyIcon = "energy",
                    forkA = StarIds.Pulse, forkATitle = "Pulse", forkABlurb = "Taps splash 15% to a nearby foe.", forkAIcon = "bomb",
                    forkB = StarIds.Nail, forkBTitle = "Nail", forkBBlurb = "+10% tap.", forkBIcon = "sword_b",
                    extraId = StarIds.TapEdge, extraTitle = "Edge", extraBlurb = "+6% tap.", extraIcon = "sword_b",
                    extra2Id = StarIds.IronFinger, extra2Title = "Iron Finger", extra2Blurb = "+6% tap.", extra2Icon = "sword_a"
                },
                new Region
                {
                    id = "auto", ang = 60f, tag = StarTags.Auto, minorIcon = "picto_time",
                    innerId = StarIds.Tick, innerTitle = "Tick", innerBlurb = "+6% auto.", innerIcon = "talaria",
                    outerId = StarIds.Overspin, outerTitle = "Overspin", outerBlurb = "+8% Overclock.", outerIcon = "timer",
                    keyId = StarIds.Sleepless, keyTitle = "Sleepless", keyBlurb = "+10% auto. Away potion from 10m.", keyIcon = "sandglass",
                    forkA = StarIds.Metronome, forkATitle = "Metronome", forkABlurb = "Auto floor 0.26s.", forkAIcon = "horner",
                    forkB = StarIds.Anvil, forkBTitle = "Anvil", forkBBlurb = "+12% auto. Keeps the 0.28s floor.", forkBIcon = "anvil",
                    extraId = StarIds.AutoEdge, extraTitle = "Idle Edge", extraBlurb = "+6% auto.", extraIcon = "timer"
                },
                new Region
                {
                    id = "endless", ang = 120f, tag = StarTags.BossGold, minorIcon = "picto_star",
                    innerId = StarIds.RoadMark, innerTitle = "Road Mark", innerBlurb = "After a loop, ascend starts in Endless 1.", innerIcon = "laurel",
                    outerId = StarIds.DepthSense, outerTitle = "Depth Sense", outerBlurb = "+4% gold on Endless.", outerIcon = "mission",
                    keyId = StarIds.SecondLoop, keyTitle = "Second Loop", keyBlurb = "Every other loop grants an extra Star.", keyIcon = "crown",
                    forkA = StarIds.Thick, forkATitle = "Thick", forkABlurb = "+1 spawn cap, −4% tap.", forkAIcon = "skull",
                    forkB = StarIds.Thin, forkBTitle = "Thin", forkBBlurb = "−1 spawn cap, +8% tap.", forkBIcon = "trophy",
                    extraId = StarIds.CycleGold, extraTitle = "Cycle Gold", extraBlurb = "+5% gold on Endless.", extraIcon = "gold"
                },
                new Region
                {
                    id = "focus", ang = 180f, tag = StarTags.Regen, minorIcon = "picto_flask",
                    innerId = StarIds.Wellspring, innerTitle = "Wellspring", innerBlurb = "+8% Focus regen.", innerIcon = "potion_red",
                    outerId = StarIds.CheapSlam, outerTitle = "Cheap Slam", outerBlurb = "Slam costs 4 less Focus.", outerIcon = "potion_purple",
                    keyId = StarIds.Flow, keyTitle = "Flow", keyBlurb = "Ascend with 30 Focus.", keyIcon = "candle",
                    forkA = StarIds.Wide, forkATitle = "Wide", forkABlurb = "+10% Sweep.", forkAIcon = "skull",
                    forkB = StarIds.Point, forkBTitle = "Point", forkBBlurb = "Reaper Sweep ×2.0, or +12% Slam.", forkBIcon = "energy",
                    extraId = StarIds.LongFury, extraTitle = "Long Fury", extraBlurb = "+12% Fury duration.", extraIcon = "candle",
                    extra2Id = StarIds.FocusEdge, extra2Title = "Focus Edge", extra2Blurb = "+6% Focus regen.", extra2Icon = "potion_red"
                },
                new Region
                {
                    id = "gold", ang = 240f, tag = StarTags.Gold, minorIcon = "picto_gold",
                    innerId = StarIds.Tithe, innerTitle = "Tithe", innerBlurb = "+5% gold.", innerIcon = "gold",
                    outerId = StarIds.BossPurse, outerTitle = "Boss Purse", outerBlurb = "+12% boss gold.", outerIcon = "treasure",
                    keyId = StarIds.HoardStar, keyTitle = "Hoard Star", keyBlurb = "Collection 16+ and 24 get +2%.", keyIcon = "gem",
                    forkA = StarIds.Merchant, forkATitle = "Merchant", forkABlurb = "+20% potion drops.", forkAIcon = "clover",
                    forkB = StarIds.Smith, forkBTitle = "Smith", forkBBlurb = "Temper costs 15% less Dust.", forkBIcon = "hammer"
                },
                new Region
                {
                    id = "craft", ang = 300f, tag = StarTags.Relic, minorIcon = "picto_hammer",
                    innerId = StarIds.RelicMagnet, innerTitle = "Relic Magnet", innerBlurb = "+10% relic drops.", innerIcon = "key",
                    outerId = StarIds.GoldEdge, outerTitle = "Gold Edge", outerBlurb = "+5% gold.", outerIcon = "purplegem",
                    keyId = StarIds.NightShift, keyTitle = "Night Shift", keyBlurb = "+12% away gold.", keyIcon = "shield_a"
                }
            };
        }

        static StarNode Place(string id, string title, string blurb, string iconId, string tag, StarKind kind, int cost, float x, float y, string effectId = null, string exclusiveWith = null)
        {
            return new StarNode
            {
                id = id,
                title = title,
                blurb = blurb,
                iconId = iconId,
                statTag = tag,
                kind = kind,
                cost = cost,
                x = x,
                y = y,
                effectId = effectId,
                exclusiveWith = exclusiveWith
            };
        }

        static void Join(List<string> links, string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b)
                return;
            links.Add(a);
            links.Add(b);
        }

        static void BakeNeighbors(List<StarNode> nodes, List<string> links)
        {
            var map = new Dictionary<string, List<string>>(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
                map[nodes[i].id] = new List<string>(4);
            for (int i = 0; i + 1 < links.Count; i += 2)
            {
                string a = links[i];
                string b = links[i + 1];
                if (!map.ContainsKey(a) || !map.ContainsKey(b))
                    continue;
                if (!map[a].Contains(b))
                    map[a].Add(b);
                if (!map[b].Contains(a))
                    map[b].Add(a);
            }

            for (int i = 0; i < nodes.Count; i++)
                nodes[i].neighbors = map[nodes[i].id].ToArray();
        }

        static StarNode Find(List<StarNode> nodes, string id)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].id == id)
                    return nodes[i];
            }

            return null;
        }

        static Vector2 Polar(float degrees, float dist)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * dist;
        }

        static string Label(string tag)
        {
            switch (tag)
            {
                case StarTags.Tap: return "tap";
                case StarTags.Auto: return "auto";
                case StarTags.Gold: return "gold";
                case StarTags.Regen: return "Focus regen";
                case StarTags.Relic: return "relic chance";
                case StarTags.BossGold: return "boss gold";
                case StarTags.Offline: return "away gold";
                case StarTags.Crit: return "crit";
                default: return "power";
            }
        }
    }
}
