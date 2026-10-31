using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WgMod.Common.Players;

namespace WgMod.Content.Prefixes;
public class Abundant : ModPrefix
{
    WgStat _mana = new WgStat(5f, 40f);
    WgStat _attackSpeed = new WgStat(0.01f, 0.04f);

    public override PrefixCategory Category => PrefixCategory.Accessory;

    public override float RollChance(Item item)
    {
        return 1f;
    }

    public override bool CanRoll(Item item)
    {
        return true;
    }

    public override void ModifyValue(ref float valueMult)
    {
        valueMult *= 1.44f;
    }

    public override void ApplyAccessoryEffects(Player player)
    {
        if (!player.TryGetModPlayer(out WgPlayer wg))
            return;
        float immobility = wg.Weight.ClampedImmobility;

        _mana.Lerp(immobility);
        _attackSpeed.Lerp(immobility);

        player.statManaMax2 += _mana;
        player.GetAttackSpeed(DamageClass.Generic) -= _attackSpeed;
    }

    public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
    {
        yield return new TooltipLine(Mod, "Abundant", MaxManaTooltip.Format(_mana, _attackSpeed.Percent()))
        {
            OverrideColor = Colors.RarityPink
        };
    }

    public static LocalizedText MaxManaTooltip {get; private set;}

    public LocalizedText AdditionalTooltip => this.GetLocalization(nameof(AdditionalTooltip));

    public override void SetStaticDefaults()
    {
        MaxManaTooltip = Mod.GetLocalization($"{LocalizationCategory}.{nameof(MaxManaTooltip)}");
        _ = AdditionalTooltip;
    }
}
