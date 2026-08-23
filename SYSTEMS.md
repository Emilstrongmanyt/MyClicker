# MyClicker systems sheet

Live numbers from current working tree. Use this to balance, not as design intent.

Engine: tap > Forge. Gold / Dust / Glory / relics only. No player HP, minions, or combat skill tree.

---

## 1. Core loop

```
tap horde → gold
  → Forge (run power)
  → wave clear (every 8 + 4×cycle kills)
    → every 10th wave is a boss
      → dust, potion, relic chance, bank Glory
      → first-clear shard
      → next zone (or Endless loop)
idle auto-swings while looking
leave → offline gold (capped)
Ascend → spend Glory on Mutations + talent tree
```

Portrait iOS tap/idle. Bundle `com.solodreams.MyClicker`, display **MyClickerBeta**.

---

## 2. Currencies

| Currency | Earn | Spend | Persist on Ascend |
|---|---|---|---|
| **Gold** (double) | kills, wave bonus (not bosses), offline | Forge, Oaths | **No** (wiped) |
| **Dust** | kill chance, guaranteed on bosses | potions (craft), Temper | **Yes** |
| **Glory** | banked on bosses, paid on Ascend | Mutations, talent tree | **Yes** (pending paid out, then kept) |
| **Relics** | drop unique IDs | equip (cosmetic), Temper uses Dust | **Yes** |
| **Boss shards** | first-clear each zone | none | **Yes** |
| **Renown** | Deeds (lifetime) | none | **Yes** |

Gold display uses compact suffixes (`NumberFmt`).

---

## 3. Combat

### 3.1 Field

| Stat | Value |
|---|---|
| Base tap | 10 |
| Spawn interval | 0.85s, late waves faster, floor **0.16s** |
| Late spawn | from **depth** wave 12: `-1.8%` per wave, cap `-45%` |
| Walk speed | 2.7, × Forced March × `1.06^cycle` |
| Max alive | 6 + 3×cycle + floor((depth-1)/15) clamped 0–8, +2 Horde Banner, **cap 18** |
| Wave need | **8 + 6×cycle** kills |
| Boss every | 10 waves |
| Approach stop | 1.55, ring 2.05 / boss 2.55 |
| Overrun | 5s |

Spawn interval × `0.88^WarTempoRank` × `0.92^cycle`, floor 0.16s.

HUD still shows Wave 1 after a loop. Combat uses `depthWave = wave + cycle × 100` so round 1 of cycle 2 is depth 101.

### 3.2 Enemy HP

```
depthWave = wave + cycle × 100
hp = (26 + 7.5 × (depthWave-1))
   × 1.048^(max(0, depthWave-18))
   × zone.hpMul
   × 5.2^cycle                   // ×1.08 if Deep Road
   × [boss: 9 × (1 + 0.08 × (zone + cycle×10))]
```

Cycle 5.2 offsets Harvest Night's 5.1 zone mul so Old Road on the next loop is a small step **up**, not a dump. Gold uses depthWave too, × `4.2^cycle`. Glory × `2.6^cycle`.

Gold/s HUD uses `CurrentEnemyHp` (includes late HP and depth).

### 3.3 Damage pipeline (tap)

```
base = 10 + Might×4
     + relic tap (+14 weapon, +5 armor)
     + temper (weapon×3 + armor×1.2)
     + Steel? temperSum×1.5
× Ember (+60%)
× Fury (see Focus)
× Iron Vow (×1.08 per oath rank)
× Renown
× Mutate Might
× Collection + Shards
× Blood Oath (×1.05 per rank)
× Giant's Due ×1.25
× Mythos ×1.15
× Iron Pulse ×1.5
× Titan Heart ×1.5
× leftover War Cry buff if any
× Glory Surge ×2
```

Auto = same × Overclock (oath + Swift Echo + cape relic + extra Swift past the 0.28s floor), then crit, then Cleave splash.

Crit chance: `min(60%, Crit×2%)`. Extra chance past 60% becomes crit mul ×2.5. Mul: `3 + FuryUpgrade×0.25 + helmet relic 0.4 + Lucky Strike 0.55 + overflow`.

Cleave: 0 until rank 1, then `min(100%, 40% + (rank-1)×5%)`. Splashes nearest other foe. Sweep does **not** hit bosses.

### 3.4 Focus (Slam / Fury / Sweep)

| | Cost | Effect |
|---|---|---|
| Slam | 35 | ×5.5 × `1.12^CrushingSlam` vs nearest |
| Fury | 50 | +125% tap, 8s × `1.1^EternalFury`; bonus also ×`(1+0.08×rank)`. Durations stack up to 3×. |
| Sweep | 70 | ×3 × `1.12^WideSweep` vs all **non-boss** alive |

Regen **2/s**, max **100**. Focus Well: +8% max and +12% regen per rank (8). Helmet relic: +15% regen. Second Wind: costs ×0.8. Starts empty on Ascend.

Default time to one skill: Slam ~17.5s, Fury 25s, Sweep 35s.

FX: Fury = CFXR Electrified 3. No per-tap/crit particles. No boss BGM swap. No ascend SFX.

---

## 4. Zones (one loop)

Boss every 10 waves. After boss, zone++. After Harvest Night + Endless unlocked, `cycle++`, zone 0, wave 1.

| # | Zone | HP | Gold | First-loop Glory (rounded) |
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

**~86 Glory** for a clean first loop (`5 + 0.8×zone`). Zone gold/HP already diverge (Harvest Night is +410% HP vs +300% gold).

Enemies recycle three 6-packs of sprites. Harvest Night uses the full 24-pack. Bosses are unique per zone (10).

---

## 5. Gold

```
kill  = 5 + 1.15×(wave-1)
boss  = 70 + 6×wave
late  = ×1.03^(wave-18) from wave 18
wave clear = +18 gold (not on boss waves)
then × GoldMultiplier
```

GoldMultiplier:

```
1 + Fortune×0.12
+ armor relic 8% + temperArmor×2%
× Gilded (×2)
× Blood Tithe (×1.08 per rank)
× Renown
× Mutate Fortune
× Unspent Tithe   // see Glory
× Hoard (×1 + 1.5% per owned relic)
× Gold Vein (×1.05 per rank)
× Collection + Shards
× 1.50^cycle      // ×1.08 if Deep Road
× zone.goldMul
```

---

## 6. Forge (run gold)

Cost `base × growth^level`. Buy 1 / N / max.

| Upgrade | Base | Growth | Per rank | Cap | Unlock |
|---|---|---|---|---|---|
| Might | 15 | 1.18 | +4 tap | 200 | — |
| Fortune | 25 | 1.20 | +12% gold | 200 | — |
| Swift | 40 | 1.22 | interval ×0.93; extra past 0.28s → auto damage | 40 | — |
| Crit | 50 | 1.25 | +2% chance; extra past 60% → crit mul | 30 | — |
| Cleave | 80 | 1.23 | splash; 40% then +5% | 13 (100%) | Might 6 |
| Fury (upgrade) | 90 | 1.26 | crit mul +0.25 | 200 | Crit 5 |
| Harvest | 70 | 1.22 | +4% dust chance, +1.5% potion, +0.25% relic | 200 | Fortune 6 |
| Blood Tithe | 180 | 1.22 | +8% gold | 40 | 500 lifetime kills |
| Iron Vow | 220 | 1.22 | +8% tap | 40 | Might 20 **or** a boss this run |
| Overclock | 280 | 1.24 | auto damage +12% (not speed) | 40 | Swift MAX |

Swift interval: `2.35 × 0.93^level × Gale × (1 - mutSwift)`, floor **0.28s**. Hits floor around rank **30**. Ranks 31–40 and Gale/Mutate Swift past the floor convert extra speed into Overclock (auto damage). Cleave still stops at 100% so those ranks cannot be bought.

---

## 7. Dust sinks

### Potions (20s, **durations stack** up to 60s, potency does not)

| Potion | Drop weight | Craft | Effect |
|---|---|---|---|
| Ember | 40% | 12 | +60% tap |
| Gale | 35% | 12 | auto 35% faster (extra speed → auto damage at the cap) |
| Gilded | 25% | 18 | ×2 gold |

Kill drop 8% + Harvest + luck + Relic Sense. Bosses always drop a potion.

### Temper (per slot)

Cost `8 × 1.35^rank` Dust.

| Slot | Temper |
|---|---|
| Weapon | +3 tap / rank |
| Armor | +1.2 tap and +2% gold / rank |
| Helmet | +0.06 crit mul and +3% Focus regen / rank |
| Cape | +3% auto damage / rank |

Steel (Glory): all temper ranks also +1.5 tap each.

---

## 8. Relics / Armory

Unique unlock, no auto-equip. Starter leaves the cycle pool once any relic exists in that slot.

**Equipped stats are per slot, not per look.**

| Slot | Any relic | Starter |
|---|---|---|
| Weapon | +14 tap | 0 |
| Armor | +5 tap, +8% gold | 0 |
| Helmet | +0.4 crit mul, +15% Focus regen | 0 |
| Cape | +15% auto damage | 0 |

Looks are cosmetic. Collection still counts unique IDs.

Drop: 1% trash / 18% boss, + Harvest×0.25%, × luck, ×1.25 Relic Sense. Rolls a random slot that still has a fresh ID.

Seasonal HeroEditor sets (Christmas / Halloween) are excluded.

---

## 9. Collection and shards (permanent tap+gold)

```
MilestoneMul = 1 + CollectionBonus + ShardBonus
```

| Relics owned | Collection |
|---|---|
| 4 | +2% |
| 8 | +5% |
| 12 | +10% (cap) |

Shards: +2% each, one per zone first-clear, cap 10 = **+20%**. Retroactively granted from `bestZone` on load.

Hoard (Glory, separate): +1.5% **gold only** per owned relic. Not in MilestoneMul.

---

## 10. Idle / offline

Auto DPS = `TapDamage × Overclock / AutoInterval`.

Offline:

```
seconds capped at 3 hours
kills ≈ AutoDps × seconds / max(8, currentWaveHp)   // no zone mul, no late HP
gold  = kills × GoldForKill(wave) × factor
factor = 0.12 + glory×0.008 + NightMarket×0.08
```

Unspent Glory in the bank **directly** raises offline. Night Market 6 = +48% extra factor.

---

## 11. Ascend

Allowed if zone > 0 **or** wave ≥ 10 **or** any boss slain.

Pays `pendingGlory` into `glory`, then resets the run.

| Persists | Wipes |
|---|---|
| Relics, Temper, Dust | Gold |
| Glory, Mutations, talent ranks | Forge except Keep Might / Keep Fortune |
| Deeds / Renown | Oaths, potions, buffs, Focus |
| Shards, bestZone, bestCycle | Wave, zone, **cycle** |
| Lifetime kills / bosses / forgeBought | Run bosses, pending Glory (paid) |
| Hero look JSON | Glory Surge / Fury |

Cycle 0 after every Ascend — Endless progress is a **run** stat, bestCycle is lifetime.

---

## 12. Glory: Mutations

Cost `25 × 1.4^rank` Glory (25, 35, 49…). Bonus `0.55 × log10(1+spent)` (Swift uses 0.35). Extra Swift past the 0.28s floor becomes auto damage.

| Mutation | Effect |
|---|---|
| Mutate Might | tap/auto |
| Mutate Fortune | gold |
| Mutate Swift | auto speed, then auto damage at the cap |
| Mutate Harvest | dust / potion / relic chance |

Diminishing: 10 spent ≈ +57%, 100 ≈ +110%, 1000 ≈ +165% Might/Fortune/Luck.

Late dump after the talent tree, not a first-Ascend replacement for it.

---

## 13. Glory talent tree

Opens after first Ascend (`Legacy`, free). 2-column list, rows 0–14. One-time or ranked. First purchase also writes `gloryNodes`.

### 13.1 One-time

| Node | Cost | Needs | Effect |
|---|---|---|---|
| Legacy | 0 | 1 Ascend | Opens tree |
| Keep Might | 40 | Legacy | Might ranks survive |
| Keep Fortune | 120 | Keep Might, 2 Ascend | Fortune survives |
| Steel | 90 | Keep Might | Temper adds tap |
| Hoard | 90 | Legacy | +1.5% gold / relic |
| Deep Road | 220 | Legacy, Harvest Night | Endless + 8% cycle scales |
| Lucky Strike | 800 | Blood Oath, 2 Ascend | +0.55 crit mul |
| Second Wind | 700 | Focus Well, 2 Ascend | Focus costs −20% |
| Relic Sense | 600 | Hoard | drops ×1.25 |
| Giant's Due | 600 | Blood Oath | ×1.25 tap |
| Swift Echo | 900 | Keep Might, 3 Ascend | Overclock +0.25 |
| Horde Banner | 500 | War Tempo | +2 spawn cap |
| Mythos | 4000 | Iron Pulse, 4 Ascend | ×1.15 tap |
| Iron Pulse | 2500 | Keep Might, 3 Ascend | **permanent ×1.5 tap** |
| Titan Heart | 8000 | Iron Pulse, 5 Ascend | **another ×1.5 tap** |

One-time total: **19,160**.

### 13.2 Ranked (cost = `base × growth^rank`)

| Node | Ranks | Base | Growth | All ranks | Effect |
|---|---|---|---|---|---|
| Focus Well | 8 | 50 | 1.50 | 2,463 | +8% max Focus, +12% regen |
| Unspent Tithe | 8 | 35 | 1.50 | 1,724 | gold from banked Glory |
| Night Market | 6 | 45 | 1.50 | 936 | +8% offline factor / rank |
| Deed Angel | 8 | 90 | 1.50 | 4,433 | Renown ×(1+15%/rank) |
| War Tempo | 10 | 70 | 1.50 | 7,933 | spawn ×0.88 / rank (×0.28 at 10) |
| Forced March | 8 | 110 | 1.55 | 6,462 | walk ×1.12 / rank (×2.48 at 8) |
| Crushing Slam | 8 | 80 | 1.50 | 3,941 | Slam ×1.12 / rank (×13.6 at 8) |
| Eternal Fury | 8 | 90 | 1.50 | 4,433 | Fury time ×1.1, bonus ×(1+8%) |
| Wide Sweep | 8 | 85 | 1.50 | 4,186 | Sweep ×1.12 / rank (×7.4 at 8) |
| Blood Oath | 12 | 80 | 1.45 | 15,180 | +5% tap / rank (**+60% at 12**) |
| Gold Vein | 10 | 100 | 1.50 | 11,333 | +5% gold / rank (+50% at 10) |
| Boss Tithe | 8 | 150 | 1.55 | 8,813 | +15% Glory / rank (×2.2 at 8) |

Ranked total **~71,837**. Tree max **~91,000 Glory**.

Unspent Tithe formula:

```
× (1 + min(1.00, 0.03 × rank × log10(1 + glory)))
```

Rank 8 + 100 banked ≈ **+48% gold**. Rank 8 + 1000 ≈ +72%. Caps at +100%.

Tap Glory stack at max (Pulse × Heart × Giant × Mythos × Blood Oath 12) = **×5.2 tap**.

Glory Surge (not a node): after 1 Ascend, **0.4% per kill**, ×2 tap for 8s, 40s cooldown. War Cry / Godstrike still exist in code, **UI removed**.

---

## 14. Deeds / Renown

35 deeds. Each: **+2% tap and gold**. Deed Angel multiplies that.

Max: 35 × 2% × (1 + 0.15×8) = **+154%**.

| Track | Thresholds |
|---|---|
| Kills | 100 / 500 / 1k / 5k / 10k |
| Bosses | 1 / 10 / 50 / 200 |
| Zone | 1 / 3 / 6 / 9 |
| Endless cycle | 1 / 3 / 5 |
| Ascend | 1 / 3 / 10 |
| Relics | 1 / 4 / 8 / 12 |
| Forge ranks bought | 10 / 50 / 100 |
| Once | Slam, Fury, Sweep, potion, Temper, Might 20, Swift max, Bare Fist |

Bare Fist: beat a boss this run with **zero** Forge ranks (oaths count). Relics/temper do not block it.

---

## 15. Endless

Unlock: beat Harvest Night (or buy Deep Road). Looping resets the HUD to Wave 1 / Old Road but **does not reset combat depth**.

| Scale | Growth | Cycle 1 | Cycle 3 | Cycle 5 |
|---|---|---|---|---|
| Depth wave at loop start | +100 | 101 | 301 | 501 |
| HP (zone offset) | 5.2^n | 5.20 | 141 | 3803 |
| Gold (zone offset) | 4.2^n | 4.20 | 74 | 1307 |
| Glory (zone offset) | 2.6^n | 2.60 | 17.6 | 119 |
| Kills / wave | 8+6n | 14 | 26 | 38 |
| Spawn cap | 6+3n | 9+ | 15+ | 18 |
| Spawn interval | 0.92^n | 0.92 | 0.78 | 0.66 |
| Walk | 1.06^n | 1.06 | 1.19 | 1.34 |

Seam (Harvest Night cycle 0 trash → Old Road cycle 1 trash): about **1.1×** HP, not a collapse. Deep Road ×1.08 on HP/gold/glory. HP still outruns gold so Forge matters.

---

## 16. Screens

| UI | Holds |
|---|---|
| HUD left | zone, wave sting, stacked buff bars (Ember/Gale/Gilded/Fury/Surge) |
| HUD right | taps/s, DPS, gold/s (outlined, no plaques) |
| HUD top | gold + dust icons tight to numbers |
| HUD bottom-left | potion USE, count only |
| HUD bottom | Focus (green) + boss HP (orange/red), solid fills |
| Forge | upgrades + Oaths; Glory button |
| Glory | Ascend, Mutations, 2-col talent list; Deeds button |
| Armory | slot cycle + Temper + craft; Stats button |
| Stats | live multipliers |
| Deeds | trophy list |

---

## 17. Persist map (quick)

```
PERMANENT
  relics, temper, dust
  glory, mutations, talent ranks
  deeds, shards, collection
  bestZone, bestCycle, endlessUnlocked
  lifetime kills / bosses / forgeBought
  heroJson

RUN
  gold, forge (unless Keep Might/Fortune)
  oaths, potions, buffs, focus
  wave, zone, cycle
  pending Glory (banks, pays on Ascend)
```

---

## 18. Dead / leftover

- War Cry (12 Glory, ×2 / 15s) and Godstrike (40, ×3.5 / 12s): code live, no UI.
- `goldPerWave` **is** used (18 gold on non-boss wave clear).
- `endlessCycleGrowth` / gold / glory on GameConfig now drive cycle scaling (5.2 / 4.2 / 2.6).
- `PlayerProfile.tapDamage` is a cache, recomputed.

---

## 19. Balance flags (where to look first)

### Retuned
1. **Unspent Tithe** — log10 of banked Glory, +48% at rank 8 / 100 Glory (was +320%).
2. **Glory Surge** — 0.4%/kill, ×2 / 8s, 40s cooldown (was 1.2%, ×4 / 10s, 20s).
3. **Mutations** — first rank 25 Glory, 1.4× cost growth; weaker log (0.55).
4. **Caps** — helmet is crit mul + Focus regen; cape is auto damage; extra Swift/Gale becomes Overclock; extra Crit chance becomes crit mul; Cleave still cannot be bought past 100%.
5. **Tap Glory stack** — Pulse × Heart × Giant × Mythos × Blood Oath 12 = **×5.2** (was ×14.7).
6. **First loop Glory ~86** — Keep Might plus two early ranks.
8. **Potions and Fury** — durations stack (potency does not), cap 3×. Cannot drink / Fury when already at cap.

### Still watching
7. **Cycle HP vs gold** — 5.2 vs 4.2 per loop, plus late HP 1.048 vs late gold 1.03. Forge still needed; Keep Might matters.
9. **Sweep ignores bosses** — late bosses are the DPS check; Sweep is horde-only.
10. **Offline 12% × 3h** — plus unspent Glory can dwarf it (`glory×0.008`: 200 Glory → +160% factor). Night Market stacks on top.

### Structural gaps
11. Relics are cosmetics + collection milestones. No loadout puzzle.
12. Tree is a 2-column ranked shop, not a drawn Cookie graph (prereqs exist, layout is a list).
13. Two Glory sinks (Mutations + tree) with no shared budget or diminishing across both.
14. Ascend wipes **cycle**, so Endless is a single-run climb unless you never Ascend.
15. Collection cap is 12 relics / +10%. Loot pool is the whole HeroEditor non-starter set — extra relics after 12 are Hoard-only (and only if Hoard is bought).

### Soft caps to remember when retuning

| System | Soft cap |
|---|---|
| Crit chance | 60% |
| Auto interval | 0.28s |
| Cleave | 100% |
| Spawn interval | 0.16s |
| Spawn cap | 14 |
| Collection | 12 relics / +10% |
| Shards | 10 / +20% |
| Offline | 3 hours |
| Unspent Tithe | +100% gold (log10, rank 8 needs huge bank) |
| Potion / Fury duration | 3× one drink / one Fury |

---

## 20. Suggested analysis order

When changing one number, check the stack it sits in:

1. **Tap stack** — Might, relics, Steel, Ember, Fury, Iron Vow, Renown, Blood Oath, Giant/Mythos/Pulse/Heart, Surge.
2. **Gold stack** — Fortune, armor, Gilded, Tithe, Renown, Unspent Tithe, Hoard, Gold Vein, collection, cycle, zone.
3. **Pace** — Swift/Overclock vs HP curve vs War Tempo/Forced March vs spawn cap.
4. **Prestige** — Glory/boss vs tree costs vs Mutations vs Unspent Tithe hoarding.
5. **Endless** — HP 1.65 vs gold 1.50 vs glory 1.55 vs wave need/spawn cap.

Do not add currencies, factories, Golden Cookies, or a player combat tree.
