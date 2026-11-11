using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Content.Buffs;
using WgMod.Content.Buffs.Consumables;
using WgMod.Content.Buffs.Debuffs;

namespace WgMod;

partial class WgMod
{
    static int Buff<T>() where T : ModBuff => ModContent.BuffType<T>();
    static GainOptions GainOverTime(Mass totalGain, float totalTime) => new(totalGain, totalTime);

    void RegisterBuffs()
    {
        // Vanilla
        AddBuffs([
            (BuffID.AmmoReservation, 1f),
            (BuffID.Archery, 1f),
            (BuffID.Battle, 1f),
            (BuffID.BiomeSight, 1f),
            (BuffID.Builder, 1f),
            (BuffID.Calm, 5f),
            (BuffID.Crate, 1f),
            (BuffID.Dangersense, 1f),
            (BuffID.Endurance, 3f),
            (BuffID.Featherfall, 1f),
            (BuffID.Fishing, 1f),
            (BuffID.Flipper, 1f),
            (BuffID.Gills, 1f),
            (BuffID.Gravitation, 1f),
            (BuffID.Heartreach, 2f),
            (BuffID.Hunter, 2f),
            (BuffID.Inferno, 2f),
            (BuffID.Invisibility, 1f),
            (BuffID.Ironskin, 3f),
            (BuffID.Lifeforce, 5f),
            (BuffID.Lucky, 1f),
            (BuffID.MagicPower, 2f),
            (BuffID.ManaRegeneration, 3f),
            (BuffID.Mining, 1f),
            (BuffID.NightOwl, 1f),
            (BuffID.ObsidianSkin, 2f),
            (BuffID.Rage, 1f),
            (BuffID.Regeneration, 6f),
            (BuffID.Shine, 1f),
            (BuffID.Sonar, 1f),
            (BuffID.Spelunker, 5f),
            (BuffID.Summoning, 2f),
            (BuffID.Swiftness, 1f),
            (BuffID.Thorns, 1f),
            (BuffID.Titan, 6f),
            (BuffID.Warmth, 10f),
            (BuffID.WaterWalking, 1f),
            (BuffID.Wrath, 3f),

            (BuffID.SoulDrain, GainOverTime(2f, 1f)),
            (BuffID.RapidHealing, 0.5f),

            (BuffID.SugarRush, 6f),

            (BuffID.Honey, GainOverTime(2f, 1f)),

            (BuffID.Ichor, 1f),
            (BuffID.Slimed, 1f),
            (BuffID.GelBalloonBuff, 5f),
            (BuffID.Tipsy, 10f),
            (BuffID.PotionSickness, 10f),
            (BuffID.ManaSickness, 5f),

            (BuffID.WellFed, 3f),
            (BuffID.WellFed2, 5f),
            (BuffID.WellFed3, 7f),

            (BuffID.HeartyMeal, GainOverTime(1f, 1f)),
        ]);

        // WgMod
        AddBuffs([
            (Buff<AmbrosiaGorged>(), 6f),
            (Buff<WobWobWobWob>(), 6f),
            (Buff<GnomeLuck>(), 6f),
            (Buff<SpikedSkin>(), 6f),
            (Buff<FullOfSpider>(), 6f),
            (Buff<Caramel>(), 12f),
            (Buff<Caramelized>(), 4f),
            (Buff<CrystalSugarFrenzy>(), 12f)
        ]);

        // Calamity Mod
        AddBuffs(
            "CalamityMod",
            [
                ("BloodyMaryBuff", 6f),
                ("CaribbeanRumBuff", 6f),
                ("CinnamonRollBuff", 8f),
                ("EverclearBuff", 6f),
                ("EvergreenGinBuff", 6f),
                ("FireballBuff", 6f),
                ("GrapeBeerBuff", 6f),
                ("MargaritaBuff", 6f),
                ("MoonshineBuff", 6f),
                ("MoscowMuleBuff", 6f),
                ("OldFashionedBuff", 6f),
                ("PurpleHazeBuff", 6f),
                ("RedWineBuff", 6f),
                ("RumBuff", 6f),
                ("ScrewdriverBuff", 6f),
                ("StarBeamRyeBuff", 6f),
                ("TequilaBuff", 6f),
                ("VodkaBuff", 6f),
                ("WhiskeyBuff", 6f),
                ("WhiteWineBuff", 2f),
                ("AnechoicCoatingBuff", 2f),
                ("BaguetteBuff", 6f),
                ("BloodfinBoost", 3f),
                ("BoundingBuff", 2f),
                ("CalciumBuff", 1f),
                ("CeaselessHunger", 12f),
                ("GravityNormalizerBuff", 6f),
                ("Omniscience", 6f),
                ("PhotosynthesisBuff", 4f),
                ("ShadowBuff", 1f),
                ("Soaring", 2f),
                ("SulphurskinBuff", 3f),
                ("Zen", 6f),
                ("Zerg", 6f),
                ("AbsorberRegen", 2f),
                ("GreenJellyRegen", GainOverTime(3f, 1f)),
                ("PinkJellyRegen", GainOverTime(3f, 1f))
            ]
        );

        // Better Potions
        AddBuffs(
            "BetterPotions",
            [
                ("BerserkerBuff", 3f),
                ("DeterringBuff", 3f),
                ("DiscoInfernoBuff", 6f),
                ("FlightBuff", 3f),
                ("HeightenedSensesBuff", 5f),
                ("ImmovableBuff", 3f),
                ("InstigatingBuff", 3f),
                ("LeapingBuff", 1f),
                ("OrichalcumskinBuff", 5f),
                ("PiercingBuff", 3f),
                ("PredatorBuff", 1f),
                ("SteelfallBuff", 8f),
                ("WarBuff", 3f)
            ]
        );
    }
}
