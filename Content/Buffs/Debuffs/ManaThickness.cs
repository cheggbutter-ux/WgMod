using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using WgMod;
using WgMod.Common.Players;

namespace WgMod.Content.Buffs.Debuffs;

[Credit(ProjectRole.Programmer, Contributor.Chegg)]
[Credit( ProjectRole.Artist, Contributor.evanescentsummer)]
class ManaThickness : ModBuff
{

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.buffNoSave[Type] = false;
    }

    float calcEffect(Player player, int buffIndex)
    {
        return (float)(Math.Round(player.buffTime[buffIndex] / 60f / 100f * 20f) / 20f);
    }

    public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
    {
        Player player = Main.LocalPlayer;
        tip = string.Format(tip, calcEffect(player, player.FindBuffIndex(ModContent.BuffType<ManaThickness>())).Percent());
    }

    public override bool ReApply(Player player, int time, int buffIndex)
    {
        player.buffTime[buffIndex] = Math.Min(player.buffTime[buffIndex] + time, Utility.TimeToTicks(minutes: 5));
        return true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (!player.TryGetModPlayer(out WgPlayer wg))
            return;
        wg.WeightGainRate += calcEffect(player, buffIndex);
    }

}
