# MyClicker systems sheet

Live numbers from the current working tree (including uncommitted Star buffs). Use this to balance. Not design intent.

Engine: **tap > Forge**. Field currencies: Gold / Dust / Glory / relics. Stars are loop-milestone spend, not a drop. No player HP, minions, extra Focus skills, or a player combat skill tree. Relics: **same stats per slot, looks only**.

Portrait iOS tap/idle. Bundle `com.solodreams.MyClicker`, display **MyClickerBeta**, Solo Dreams.

---

## 1. Core loop

```
tap horde → gold
  → Forge (run power)
  → wave clear (8 + 6×cycle kills)
    → every 10th wave is a boss
      → dust, potion, relic chance, bank Glory
      → first-clear shard
      → next zone (10 zones → Endless loop)
idle auto-swings while looking
leave → offline gold (3h cap)
Ascend → Glory (Mutations + Glory tree)
clear a loop → Stars (Star Chart)
```

HUD wave resets to 1 after a loop. Combat uses `depthWave = wave + cycle × 100`.

---

## 2. Currencies

| Currency | Earn | Spend | Persist on Ascend |
|---|---|---|---|
| **Gold** (double) | kills, non-boss wave bonus, offline | Forge, Oaths | **No** |
| **Dust** | kill chance, guaranteed on bosses | potion craft, Temper | **Yes** |
| **Glory** | banked on bosses, paid on Ascend | Mutations, Glory tree | **Yes** |
| **Stars** | loop clears only: 1 / 2 / 3… (`Max(1, cycle)` at grant; Second Loop +50%) | Star Chart | **Yes** |
| **Relics** | unique drop IDs | equip (look), Temper uses Dust | **Yes** |
| **Boss shards** | first-clear each zone | none | **Yes** |
| **Renown** | Deeds | none | **Yes** |

Stars are **not** a field currency. No GrantBossStar.

---

## 3. Combat

### 3.1 Field

| Stat | Value |
|---|---|
| Base tap | 10 |
| Tap strike | × **2.25** vs listed TapDamage |
| Auto hit | × **0.40** × Overclock × (StarAuto / StarTap) |
| Spawn interval | 0.85s, late from depth 12: −1.8%/wave, cap −45%, floor **0.16s** |
| Walk | 2.7 × `1.12^ForcedMarch` × `1.06^cycle` |
| Max alive | 6 + 3×cycle + floor((depth−1)/15) [0–8] +2 Horde Banner ± Thick/Thin, **cap 18** |
| Wave need | **8 + 6×cycle** |
| Boss every | 10 waves (10 zones = 100 waves / loop) |
| Focus | max 100, regen 2/s |

Spawn interval also × `0.88^WarTempo` × `0.92^cycle`.

### 3.2 Enemy HP

```
depthWave = wave + cycle × 100
hp = (26 + 7.5 × (depthWave-1))
   × 1.048^(max(0, depthWave-18))
   × zone.hpMul
   × 5.2^cycle                   // ×1.08 if Deep Road
   × [boss: 9 × (1 + 0.08 × (zone + cycle×10))]
```

Gold × `4.2^cycle`. Glory × `2.6^cycle`. Cycle 5.2 vs Harvest Night zone mul 5.1 so Old Road on the next loop is a small step **up**.

### 3.3 Damage (tap)

```
base = 10 + Might×6
     + relic tap (+14 weapon, +5 armor)
     + temper (weapon×3 + armor×1.2)
     + Steel? temperSum×1.5
× Ember (+60%)
× Fury
× Iron Vow (×1.08 per oath rank)
× Renown
× Mutate Might
× Collection + Shards
× Blood Oath (×1.05 per rank)
× Giant's Due ×1.25
× Mythos ×1.15
× Iron Pulse ×1.5
× Titan Heart ×1.5
× leftover War Cry buff if any (UI gone)
× Glory Surge ×2
× StarTapMul
then × 2.25 on the actual strike
× StarTapBossMul if boss
× crit (tap also adds Crit Spark)
```

Auto uses the same TapDamage, then strips StarTap and applies StarAuto, Overclock, and ×0.40.

Crit chance: `min(60%, Crit×2%)`. Extra chance → crit mul ×2.5. Mul: `3 + FuryUpgrade×0.25 + helmet 0.4 + Lucky Strike 0.55 + overflow + 0.12×crit-minors` (crit-minors currently have **no nodes** on the Chart).

Cleave: 0 until rank 1, then `min(100%, 40% + (rank-1)×5%)`. Pulse Star can splash even at Cleave 0.

### 3.4 Focus (Slam / Fury / Sweep only)

| | Cost | Effect |
|---|---|---|
| Slam | 35 (−12 Cheap Slam, floor 8) | ×5.5 × `1.12^CrushingSlam` × Point 1.30 if not Reaper |
| Fury | 50 | +125% tap, 8s × `1.1^EternalFury`; bonus also ×`(1+0.08×rank)`. Duration stacks to 3×. Long Fury ×1.40 time. |
| Sweep | 70 | ×3 × `1.12^WideSweep` × Wide 1.30 vs all **non-boss** |
| Reaper Sweep | Glory spec | Sweep becomes single-target, hits bosses, ×1.75 (Point → ×2.50) |

Second Wind: all three costs ×0.8. Flow Star: Ascend with 50 Focus, else 0.

---

## 4. Zones (one loop)

| # | Zone | HP | Gold | First-loop Glory |
|---|---|---|---|---|
| 0 | The Old Road | 1.00 | 1.00 | 5 |
| 1 | Moon Fen | 1.25 | 1.20 | 6 |
| 2 | Chitter Deep | 1.50 | 1.40 | 7 |
| 3 | Labyrinth Gate | 1.80 | 1.65 | 7 |
| 4 | Blight Ducts | 2.15 | 1.90 | 8 |
| 5 | Howling Wood | 2.55 | 2.20 | 9 |
| 6 | Bone Yard | 3.05 | 2.55 | 10 |
| 7 | Black Pool | 3.60 | 2.95 | 11 |
| 8 | Titan Stair | 4.30 | 3.40 | 11 |
| 9 | Harvest Night | 5.10 | 4.00 | 12 |

~86 Glory for a clean first loop. Enemies recycle three 6-packs. Harvest Night uses the full 24. Bosses unique per zone (10).

Visuals: cycle 0 zone 4+ tint B, zone 7+ tint C. Cycles 1–3 use tints B/C/D. Cycle **4+** uses `r8_` / `sx_` unique sprites. Tints stay at catalog scale (2.25); unique sprites fit to 2.25/2.7 cell.

---

## 5. Gold

```
kill  = 5 + 1.15×(depthWave-1)
boss  = 70 + 6×depthWave
late  = ×1.03^(depthWave-18) from depth 18
wave clear = +18 × CycleGoldMul only (no GoldMultiplier, skipped on bosses)
then × GoldMultiplier
boss also × (1 + 0.08×boss-gold minors) × Boss Purse 1.30
```

GoldMultiplier: Fortune, armor relic, Gilded, Blood Tithe, Renown, Mutate Fortune, Unspent Tithe, Hoard, Gold Vein, Collection+Shards, `4.2^cycle`, StarGoldMul, zone.goldMul.

---

## 6. Forge (run gold)

Cost `base × growth^level`. Buy 1 / 10 / max. Live Might uses **GameConfig.mightPerLevel = 6**, not catalog `perLevel: 4`.

| Upgrade | Base | Growth | Per rank | Cap | Unlock |
|---|---|---|---|---|---|
| Might | 15 | 1.18 | +6 tap | 200 | — |
| Fortune | 25 | 1.20 | +12% gold | 200 | — |
| Swift | 40 | 1.22 | interval ×0.93; extra past 0.28s → auto dmg | 40 | — |
| Crit | 50 | 1.25 | +2% chance; extra past 60% → crit mul | 30 | — |
| Cleave | 80 | 1.23 | splash; 40% then +5% | 13 (100%) | Might 6 |
| Fury | 90 | 1.26 | crit mul +0.25 | 200 | Crit 5 |
| Harvest | 70 | 1.22 | +4% dust, +1.5% potion, +0.25% relic | 200 | Fortune 6 |
| Blood Tithe | 180 | 1.22 | +8% gold | 40 | 500 lifetime kills |
| Iron Vow | 220 | 1.22 | +8% tap | 40 | Might 20 **or** a boss this run |
| Overclock | 280 | 1.24 | auto dmg +12% | 40 | Swift MAX |

Swift floor **0.28s** (~rank 30). Metronome Star lowers floor to **0.22s**. Cleave cannot be bought past 100%. **Harvest still can** after drop chances hit 100%.

**Crit overflow never fires.** Cap 30 × 2% = exactly 60%. Shop still says “next +crit mul” at the cap. Extra Crit ranks do nothing.

HUD tap/DPS is **pre-`TapStrikeMul`**. A tap hit is 2.25× the number on the HUD. Gold/s is **auto-only**.

---

## 7. Dust sinks

Potions 20s, duration stacks to 60s, potency does not. Ember +60% tap / Gale +35% auto speed / Gilded ×2 gold. Craft 12 / 12 / 18 Dust. Kill drop 8% + Harvest; bosses always drop a potion.

Temper: `8 × 1.35^rank` Dust (Smith Star ×0.70).

| Slot | Any relic | Temper / rank |
|---|---|---|
| Weapon | +14 tap | +3 tap |
| Armor | +5 tap, +8% gold | +1.2 tap, +2% gold |
| Helmet | +0.4 crit mul, +15% Focus regen | +0.06 mul, +3% regen |
| Cape | +15% auto | +3% auto |

Looks are cosmetic. Collection counts unique IDs. Drop 1% / 18% boss × Harvest × luck × Relic Sense 1.25 × Relic Magnet 1.30 × relic-minors 8%.

---

## 8. Collection and shards

```
MilestoneMul = 1 + CollectionBonus + ShardBonus
```

| Relics | Collection |
|---|---|
| 4 | +2% |
| 8 | +5% |
| 12 | +10% |
| 16 | +15% (+10% more with Hoard Star) |
| 24 | +20% (+10% more with Hoard Star) |

Shards: +2% each, one per zone first-clear, cap 10 = **+20%**.

Hoard (Glory): +1.5% **gold only** per owned relic. Not in MilestoneMul.

---

## 9. Idle / offline

```
AutoDps = TapDamage × (StarAuto/StarTap) × Overclock × 0.40 / interval
factor  = 0.12 + glory×0.008 + NightMarket×0.08 + NightShift 0.35 + 0.08×offline-minors
cap 3 hours
```

Offline-minors currently have **no nodes** on the Chart. Sleepless: away potion from **6m** (else 15m). 30m+ away can force a relic roll.

---

## 10. Ascend

Allowed if zone > 0 **or** wave ≥ 10 **or** any boss slain.

| Persists | Wipes |
|---|---|
| Relics, Temper, Dust | Gold |
| Glory, Mutations, Glory ranks, Stars | Forge except Keep Might / Fortune / Swift |
| Deeds / Renown / Shards | Oaths, potions, buffs |
| bestZone, bestCycle, loopClears, starEarned | Wave, zone; **cycle unless Keep Road or Road Mark** |
| heroJson | Focus (unless Flow → 50) |

---

## 11. Glory

Opens after first Ascend (`Legacy`). Paths: Keep / Focus / Horde / Gold / Power. Respec refunds tree spend, **keeps Mutations**. Reaper Sweep is the resettable Sweep spec.

Mutations (Glory, persist respec): cost `25 × 1.4^rank`. Bonus `0.55 × log10(1+spent)` (Swift 0.35). Might / Fortune / Swift / Luck.

Notable one-time: Keep Might 40, Keep Fortune 120, Keep Swift 150, Keep Road 280 (needs Deep Road + bestCycle 1), Steel 90, Reaper Sweep 220, Deep Road 220, Giant's Due 600, Iron Pulse 2500 ×1.5, Mythos 4000 ×1.15, Titan Heart 8000 ×1.5, …

Unspent Tithe: `× (1 + min(1.00, 0.03 × rank × log10(1+glory)))`. Rank 8 + 100 Glory ≈ +48% gold. Caps +100%.

Tap Glory stack at max (Pulse × Heart × Giant × Mythos × Blood Oath 12) ≈ **×5.2**.

Glory Surge: after 1 Ascend, **0.4% per kill**, ×2 tap for 8s, 40s cooldown. War Cry / Godstrike still exist in code, **no UI**.

---

## 12. Star Chart

PoE-style adjacency web. ~187 nodes, 6 regions (Tap / Auto / Endless / Focus / Gold / Craft), origin First Light (free, no stat). Minors / Notables (1★) / Keystones (2★). Allocate-on-tap. Respec keeps First Light.

Grant: on loop clear, `gained = Max(1, cycle)` so Endless 0/1/2 → 1/2/3. Second Loop adds `(gained+1)/2`. Migrate old wallets up to triangular `n(n+1)/2` from bestCycle; never lower.

First inner notable sits behind **7 spoke minors** → ~8★ → about **4 loops**. Filling the web ≈ 190★ ≈ 19 loops (faster with Second Loop).

Minors: **+8%** of the region tag (tap / auto / gold / Focus regen / relic chance / boss gold).

Key notables (live buffs): Nail +25% tap, Godhand +35% tap −15% auto, Heavy Tap +25% vs bosses, Pulse 35% splash, Crit Spark +0.40 tap crit mul, Anvil +28% auto, Sleepless +25% auto + 6m potion, Metronome 0.22s floor, Tithe +15% gold, Boss Purse +30%, Relic Magnet +30%, Night Shift +0.35 away factor, Wellspring +25% regen, Cheap Slam −12, Wide +30% Sweep, Point Reaper ×2.50 or +30% Slam, Long Fury +40% time, Road Mark keeps Endless 1 on ascend, Second Loop +50% loop Stars.

Dead tags: `StarTags.Crit` and `StarTags.Offline` are applied in EconomyService but **no region uses them**.

Access: Forge → Stars (also Glory header). Not on the HUD chip.

---

## 13. Deeds / Renown

34 deeds. Each **+2% tap and gold**. Deed Angel ×(1+15%/rank). Max ≈ 34 × 2% × 2.2 ≈ **+150%**.

Tracks: kills, bosses, zones, endless 1/3/5, ascend 1/3/10, relics 1/4/8/12 (no 16/24), forge 10/50/100, Slam/Fury/Sweep/potion/Temper/Might 20/Swift max/Bare Fist.

Bare Fist: boss this run with **zero** Forge ranks (oaths count). Relics/temper do not block it.

---

## 14. Screens

| UI | Holds |
|---|---|
| HUD | name, zone+Endless, wave, gold, dust, taps/s, DPS, g/s, boss bar, Focus, Slam/Fury/Sweep, potions, Forge, Armory |
| Forge | upgrades + Oaths; **Stars** and **Glory** buttons |
| Glory | Ascend, Mutations, path list, Respec; Deeds + Stars |
| Stars | pan/zoom web, allocate, Respec |
| Armory | slot cycle, Temper, craft; Stats |
| Stats | live multipliers |
| Deeds | trophy list |
| Creator | first-run look (Hair/Eyes/Armor/Helmet/Weapon/Cape) |

No settings panel. War Cry / Godstrike have no button.

---

## 15. Soft caps

| System | Soft cap |
|---|---|
| Crit chance | 60% at rank 30 — overflow code is dead |
| Auto interval | 0.28s (0.22s Metronome) → overflow Overclock |
| Cleave | 100% — **cannot buy more** |
| Harvest dust | 100% around rank 23 — **can still buy to 200** |
| Harvest potions | 100% around rank 61 — **can still buy** |
| Spawn interval | 0.16s |
| Spawn cap | 18 |
| Collection | 24 relics / +20% (+10% Hoard Star) |
| Shards | 10 / +20% |
| Offline | 3 hours |
| Unspent Tithe | +100% gold |
| Potion / Fury duration | 3× |
| Focus | 100 (Focus Well raises) |

---

## 16. Dead / leftover

- War Cry (12 Glory, ×2 / 15s) and Godstrike (40, ×3.5 / 12s): code live, no UI.
- Star Crit / Offline minor tags: code live, no nodes.
- Crit overflow: code live, **cannot trigger** (30 ranks = 60% exactly).
- `UpgradeDef.perLevel` for Might (4) and `PotionDef.potency` unused; combat uses GameConfig.
- `ZoneDef.bossCue` / `AudioDirector.PlayBoss()` unused — bosses keep zone BGM.
- `overrunSeconds` unused — enemies never threaten the player.
- Sweep splash can still hit a boss from trash if cleave mul > 1.3.
- `PlayerProfile.tapDamage` is a cache (default 12 vs live base 10).
- Stats copy still says relics “look cosmetic” in a way that undersells slot stats + collection.

---

## 17. Stacks to check when changing a number

1. **Tap** — Might, relics, Steel, Ember, Fury, Iron Vow, Renown, Blood Oath, Giant/Mythos/Pulse/Heart, Surge, Stars, ×2.25 strike.
2. **Auto** — same TapDamage, then AutoHit 0.40, Overclock, StarAuto, interval floor.
3. **Gold** — Fortune, armor, Gilded, Tithe, Renown, Unspent Tithe, Hoard, Gold Vein, collection, cycle 4.2, Stars, zone.
4. **Pace** — Swift vs HP 5.2 vs War Tempo / Forced March vs spawn cap vs wave need 8+6n.
5. **Prestige** — Glory/boss vs tree vs Mutations vs Unspent Tithe hoarding vs Stars/loop.
6. **Endless** — HP 5.2 vs gold 4.2 vs glory 2.6 vs Keep Road / Road Mark.

Do not add currencies, HP, minions, extra Focus skills, or a player combat tree.
