
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WgMod.Content.Projectiles.Minions;

public class IndulgentImpBuff : ModBuff
{
	public override void SetStaticDefaults()
	{
		Main.buffNoSave[Type] = true;
		Main.buffNoTimeDisplay[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		if (player.ownedProjectileCounts[ModContent.ProjectileType<IndulgentImp>()] > 0)
			player.buffTime[buffIndex] = 18000;
		else
		{
			player.DelBuff(buffIndex);
			buffIndex--;
		}
	}
}

public class IndulgentImp : ModProjectile
{
	static readonly int _projectileCooldown = 120;

	static readonly int _projectileRandom = 1 + _projectileCooldown / 10;

	int _shootingAndKillingPeopleForFun = Main.rand.Next(0, _projectileRandom);

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = 11;
		ProjectileID.Sets.MinionTargettingFeature[Type] = true;

		Main.projPet[Type] = true;

		ProjectileID.Sets.MinionSacrificable[Type] = true;
		ProjectileID.Sets.CultistIsResistantTo[Type] = true;
	}

	public sealed override void SetDefaults()
	{
		Projectile.aiStyle = ProjAIStyleID.Hornet;

		DrawOffsetX = -20;
		DrawOriginOffsetY = -6;

		Projectile.width = 48;
		Projectile.height = 52;
		Projectile.tileCollide = false;

		Projectile.friendly = true;
		Projectile.minion = true;
		Projectile.DamageType = DamageClass.Summon;
		Projectile.minionSlots = 1f;
		Projectile.penetrate = -1;
	}

	public override bool? CanCutTiles()
	{
		return false;
	}

	public override bool MinionContactDamage()
	{
		return true;
	}

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];

		if (!owner.TryGetModPlayer(out IndulgentImpPlayer ii) || !CheckActive(owner))
			return;

		NPC target = Projectile.FindTargetWithinRange(800);

		if (target != null)
		{
			if (_shootingAndKillingPeopleForFun <= _projectileCooldown)
				_shootingAndKillingPeopleForFun++;
			else
			{
				_shootingAndKillingPeopleForFun = Main.rand.Next(0, _projectileRandom);

				Vector2 direction = Projectile.Center.DirectionTo(target.Center);

				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, direction * 12f, ProjectileID.ImpFireball, Projectile.damage, Projectile.knockBack);
			}
		}

		Projectile.rotation = Projectile.velocity.X * 0.05f;

		int frameSpeed = 15;

		Projectile.frameCounter++;

		if (Projectile.frameCounter >= frameSpeed)
		{
			Projectile.frameCounter = 0;
			Projectile.frame++;

			if (Projectile.frame >= 3)
				Projectile.frame = 0;
		}

		if (Main.rand.NextBool(6))
		{
			Dust dust = Dust.NewDustDirect(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Torch, Alpha: 100, newColor: new Color(), Scale: 2f);

			dust.velocity *= 0.3f;
			dust.noGravity = true;
			dust.noLight = true;
		}
	}

	bool CheckActive(Player owner)
	{
		if (owner.dead || !owner.active)
		{
			owner.ClearBuff(ModContent.BuffType<IndulgentImpBuff>());

			return false;
		}

		if (owner.HasBuff(ModContent.BuffType<IndulgentImpBuff>()))
			Projectile.timeLeft = 2;

		return true;
	}
}

public class IndulgentImpPlayer : ModPlayer
{
	public int Stage;

	public override void PreUpdateBuffs()
	{
		Player player = Main.LocalPlayer;

		if (Player != player)
			return;

		if (!player.HasBuff(ModContent.BuffType<IndulgentImpBuff>()))
			Stage = 0;
	}
}