using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using WgMod.Common.Players;

namespace WgMod.Content.Prefixes;
public abstract class CursedPrefix : ModPrefix {
    public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
    {
        yield return new TooltipLine(Mod, "Cursed", CursedTooltip.Value)
        {
            IsModifier = true,
            IsModifierBad = true,
        };
    }

    public override void ModifyValue(ref float valueMult)
    {
        valueMult *= 0.9f;
    }

    public static LocalizedText CursedTooltip { get; private set; }

    public override void SetStaticDefaults()
    {
        CursedTooltip = Mod.GetLocalization($"{LocalizationCategory}.{nameof(CursedTooltip)}");
    }

}

public class CursedGlobalItem : GlobalItem
{
    public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!player.TryGetModPlayer(out WgPlayer wg))
        {
            return;
        }

        if (PrefixLoader.GetPrefix(item.prefix) is CursedPrefix && damageDone > 0)
        {
            wg.AddWeight(new Mass(damageDone));
        }

    }
}

public class CursedGlobalProjectile : GlobalProjectile
{
    bool _isCursed = false;

    public override bool InstancePerEntity => true;

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (source is EntitySource_ItemUse itemUseSource)
        {
            if (PrefixLoader.GetPrefix(itemUseSource.Item.prefix) is CursedPrefix)
            {
                _isCursed = true;
            }
        }
        else if (source is EntitySource_Parent parentSource && parentSource.Entity is Projectile parent)
        {
            _isCursed = parent.GetGlobalProjectile<CursedGlobalProjectile>()._isCursed;
        }
    }

    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        Player owner = Main.player[projectile.owner];

        if (!owner.TryGetModPlayer(out WgPlayer wg))
        {
            return;
        }

        if (_isCursed)
        {
            float amt = damageDone / 500f;

            wg.AddWeight(new Mass(Math.Max(amt, 0.01f)));
        }
    }
}
