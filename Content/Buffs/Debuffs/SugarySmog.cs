using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using WgMod.Common.Players;

namespace WgMod.Content.Buffs.Debuffs;

[Credit(ProjectRole.Programmer, Contributor.Chegg)]
[Credit(ProjectRole.Artist, Contributor.PLACEHOLDER)]
public class SugarySmog : ModBuff
{
    public override string Texture => "WgMod/Assets/Placeholder/DebuffTemplate";
    readonly float _amt = Weight.FromStage(WeightStage.SoftImmobile).Mass / (20 * 60 * 60);

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (!player.TryGetModPlayer(out WgPlayer wg))
        {
            return;
        }

        wg.AddWeight(_amt);
    }
}

public class SugarySmogBuffPlayer : ModPlayer
{
    public override void PreUpdateBuffs()
    {
        if (Player.ZoneHallow && Player.ZoneRockLayerHeight)
        {
            Player.AddBuff(ModContent.BuffType<SugarySmog>(), 2);
        }
    }
}
