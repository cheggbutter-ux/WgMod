using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WgMod.Common.Players;

namespace WgMod.Content.Prefixes;
public class Overflowing : CursedPrefix
{
    public override PrefixCategory Category => PrefixCategory.Magic;

    public override float RollChance(Item item)
    {
        return 1f;
    }

    public override bool CanRoll(Item item)
    {
        return true;
    }

    public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
    {
        damageMult = 1.2f;
        knockbackMult = 1.2f;
        useTimeMult = 0.9f;
        manaMult = 0.9f;
        critBonus = 5;
    }
}
