using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using WgMod.Common.Configs;
using WgMod.Common.Systems;
using WgMod.Content.Achievements;
using WgMod.Content.Buffs;
using WgMod.Content.Buffs.Debuffs;
using WgMod.Content.Items.Accessories.Fat;

namespace WgMod.Common.Players;

public partial class WgPlayer : ModPlayer
{
    /// <summary>
    /// The time it takes between digestion ticks ranged between 1x and 2x this value.
    /// </summary>
    public const int DigestTime = 60;
    /// <summary>
    /// The amount of mass to be digested per digestion tick
    /// </summary>
    public const float DigestAmount = 0.25f;
    /// <summary>
    /// The maximum mass that can be stored in the player's stomach
    /// </summary>
    public const float StomachCapacity = 20f;

    /// <summary> The player's weight </summary>
    public Weight Weight { get; private set; } = Weight.Base;

    // <summary> The to be digested mass inside the player's stomach. Only relevant for the local client. </summary>
    public Mass Stomach { get; private set; }

    /// <summary> How much movement will be reduced because of the player's weight. Multiply this. </summary>
    public StatModifier MovementPenalty;

    /// <summary> How fast the player will lose weight from movement. Add or subtract to this. </summary>
    public StatModifier MovementWeightLossRate;

    /// <summary> How fast the player will gain weight from most sources. Add or subtract to this. </summary>
    public StatModifier WeightGainRate;

    /// <summary> How much weight the player will gain due to food. Multiply this. </summary>
    public StatModifier FoodAbsorption;

    /// <summary> The maximum weight stage that the player can reach </summary>
    public int MaxStage;

    /// <summary> Whether the weight is currently fixed/pinned. No gain or loss. </summary>
    public bool WeightFixed;

    /// <summary> Whether a softening curve gets applied to the movement penalty allowing the player to move no matter how high it gets. </summary>
    public bool PreventImmobility;

    /// <summary> Whether the player has jumped this tick. </summary>
    public bool JustJumped { get; private set; }

    /// <summary> Whether the player can still move. Read-only. </summary>
    public bool IsMobile => _finalMovementFactor > 0.01f;

    public readonly int[] BuffDuration = new int[Player.MaxBuffs];
    internal int _ignoreWgBuffTimer = 2;

    internal float _finalKnockbackResistance;
    internal float _finalMovementFactor = 1f;
    internal int _finalMaxStage = WeightStage.Max;
    internal bool _finalWeightFixed;

    internal float _buffTotalGain;
    internal int _iceBreakTimer = 0;
    internal bool _displayWeight;

    internal float _softSquishLeft;
    internal float _softSquishRight;

    readonly int _crackLifeTime = 10;
    internal Dictionary<int, int> _crackTimers = [];

    internal int _groundedState = 2;
    Vector2 _prevVel;
    float _digestTimer;
    bool _hasJumped;
    int _lastLegFrame;

    static MethodInfo _getPickaxeDamage;

    public override void Initialize()
    {
        SetWeightForced(Weight.Base, false);
        _getPickaxeDamage = Player.GetType().GetMethod
        (
            "GetPickaxeDamage",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(int), typeof(int), typeof(int), typeof(int), typeof(Tile)],
            null
        );
    }

    public override void OnEnterWorld()
    {
        _ignoreWgBuffTimer = 2;
    }

    public bool OwnsPlayer()
    {
        return !Main.dedServ && Player.whoAmI == Main.myPlayer;
    }

    /// <summary>
    /// Sets a player's weight.
    /// Will respect player configuration.
    /// </summary>
    /// <param name="weight">The weight to set</param>
    /// <param name="effects">If visual and audio effects should play</param>
    public void SetWeight(Weight weight, bool effects = true)
    {
        if (!OwnsPlayer() || _finalWeightFixed)
            return;
        if (WgClientConfig.Instance.DisableWeightGain)
            weight = new Weight(MathF.Min(weight.Mass, Weight.Mass));
        weight = Weight.Clamp(weight, _finalMaxStage);
        SetWeightForced(weight, effects);
    }

    /// <summary>
    /// Adds to a player's weight.
    /// Will respect player configuration.
    /// </summary>
    /// <param name="mass">The mass to add</param>
    /// <param name="effects">If visual and audio effects should play</param>
    /// <returns>The different between the new and old mass</returns>
    public Mass AddWeight(Mass mass, bool effects = true)
    {
        Weight start = Weight;
        if (mass > 0f)
            mass = WeightGainRate.ApplyTo(mass);
        SetWeight(Weight + mass, effects);
        return Weight.Mass - start.Mass;
    }

    /// <summary>
    /// Sets a player's weight.
    /// Will bypass player configuration.
    /// </summary>
    /// <param name="weight">The weight to set</param>
    /// <param name="effects">If visual and audio effects should play</param>
    internal void SetWeightForced(Weight weight, bool effects = true)
    {
        int prevStage = Weight.GetStage();
        Weight = Weight.Clamp(weight);
        if (Weight.GetStage() != prevStage && effects)
        {
            SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
            Jiggle(3.6f);
            _requestPhysicsSetup = true;
        }
    }

    /// <summary>
    /// Sets a player's stomach contents.
    /// Will respect player configuration.
    /// </summary>
    /// <param name="mass">The mass to set</param>
    /// <param name="effects">If visual and audio effects should play</param>
    public void SetStomach(Mass mass, bool effects = true)
    {
        if (!OwnsPlayer())
            return;
        if (WgClientConfig.Instance.DisableWeightGain)
            mass = MathF.Min(mass, Stomach);
        SetStomachForced(mass, effects);
    }

    /// <summary>
    /// Adds to a player's stomach contents.
    /// Will respect player configuration.
    /// </summary>
    /// <param name="mass">The mass to add</param>
    /// <param name="effects">If visual and audio effects should play</param>
    /// <returns>The different between the new and old mass</returns>
    public Mass AddStomach(Mass mass, bool effects = true)
    {
        Mass start = Stomach;
        SetStomach(Stomach + mass, effects);
        return Stomach - start;
    }

    /// <summary>
    /// Sets a player's somtach contents.
    /// Will bypass player configuration.
    /// </summary>
    /// <param name="mass">The mass to set</param>
    /// <param name="effects">If visual and audio effects should play</param>
    internal void SetStomachForced(Mass mass, bool effects = true)
    {
        Stomach = Math.Clamp(mass, 0f, StomachCapacity);
        if (mass > StomachCapacity)
            AddWeight(mass - StomachCapacity, effects);
    }

    /// <summary>
    /// Resets all WgPlayer state values to their defaults.
    /// </summary>
    public override void ResetEffects()
    {
        // Custom stats
        MovementPenalty = StatModifier.Default;
        MovementWeightLossRate = StatModifier.Default;
        WeightGainRate = StatModifier.Default;
        FoodAbsorption = StatModifier.Default;
        MaxStage = WgClientConfig.Instance.StageCap;

        _finalWeightFixed = WeightFixed;
        WeightFixed = false;
        PreventImmobility = false;

        // Jump detection
        if (Player.jump <= 0)
            _hasJumped = false;
        JustJumped = false;
        if (!_hasJumped && Player.jump > 0)
        {
            JustJumped = true;
            _hasJumped = true;
        }
    }

    public override void PreUpdateBuffs()
    {
        EnsureBuff<FatBuff>();
        EnsureBuff<StomachBuff>();
        if (Weight.GetStage() >= Tired.StartStage)
            Player.AddBuff(ModContent.BuffType<Tired>(), 2);
        if (Stomach > 0f && (Player.HasBuff(BuffID.NeutralHunger) || Player.HasBuff(BuffID.Hunger) || Player.HasBuff(BuffID.Starving)))
        {
            if (Main.remixWorld && Main.dontStarveWorld)
                Player.AddBuff(BuffID.NeutralHunger, 28800);
            else
                Player.AddBuff(BuffID.NeutralHunger, 18000);
        }
    }

    /// <summary>
    /// Applies a buff, if it is not currently applied.
    /// </summary>
    /// <typeparam name="T">The buff to be ensured</typeparam>
    /// <param name="time">If the buff is not currently applied, how long should it be applied for</param>
    public void EnsureBuff<T>(int time = 60) where T : ModBuff
    {
        int type = ModContent.BuffType<T>();
        if (!Player.HasBuff(type))
            Player.AddBuff(type, time);
    }

    public override void PostUpdateRunSpeeds()
    {
        if (WgServerConfig.Instance.DisableFatBuffs)
        {
            _finalMovementFactor = 1f;
            return;
        }

        const float mountReduction = 0.8f;
        if (Player.mount.Active)
        {
            MovementPenalty *= mountReduction;
        }

        int stage = Weight.GetStage();
        if (stage >= WeightStage.DamageReduction)
        {
            if (stage < WeightStage.SoftImmobile)
            {
                _finalKnockbackResistance = float.Lerp(0f, 0.6f, Weight.GetClampedFactor(WeightStage.DamageReduction, WeightStage.SoftImmobile));
            }
            else
            {
                _finalKnockbackResistance = 1f;
            }
        }
        else
        {
            _finalKnockbackResistance = 0f;
        }

        float basePenalty;
        if (stage < WeightStage.SoftImmobile)
        {
            float immobility = Weight.ClampedImmobility;
            basePenalty = float.Lerp(0f, 0.7f, immobility * immobility);
        }
        else
        {
            float factor = Weight.GetFactor(WeightStage.SoftImmobile, WeightStage.HardImmobile);
            basePenalty = float.Lerp(1f, 2f, factor);
        }
        basePenalty = MovementPenalty.ApplyTo(basePenalty);
        if (PreventImmobility)
        {
            basePenalty = -1f / (2f * basePenalty + 1f) + 1f;
        }
        _finalMovementFactor = Math.Clamp(1f - basePenalty, 0f, 1f);

        Player.runAcceleration *= _finalMovementFactor;
        Player.maxRunSpeed *= _finalMovementFactor;
        Player.accRunSpeed *= _finalMovementFactor;
        Player.jumpSpeed *= float.Lerp(0.2f, 1f, _finalMovementFactor);
    }

    public override void PostUpdateMiscEffects()
    {
        Vector2 acc = Player.velocity - _prevVel;
        _squishRest = 1f;

        int stage = Weight.GetStage();
        ResizeHitbox(stage);
        SoftHitbox(stage);

        // Weight loss
        if (!Player.mount.Active)
        {
            float factor = MathF.Abs(Player.velocity.X) * 0.5f;
            factor += MathF.Abs(acc.X) * 5f;
            if (JustJumped)
            {
                factor += 5f;
            }
            factor /= 60f * 60f; // ticks to minutes
            AddWeight(-MovementWeightLossRate.ApplyTo(factor));
        }

        // Ice break
        if (stage >= WeightStage.Heavy)
        {
            int iceBreakTime = (int)(60f * (1 - Weight.GetClampedFactor(WeightStage.Heavy, WeightStage.SoftImmobile)));
            if (Math.Abs(Player.velocity.Y) < 0.01f && HasIceBelow())
            {
                if (_iceBreakTimer == iceBreakTime / 2)
                {
                    SoundEngine.PlaySound(SoundID.Item127);
                }
                _iceBreakTimer++;
                if (_iceBreakTimer > iceBreakTime)
                {
                    ThinIceBreak();
                }
            }
            else
            {
                _iceBreakTimer = 0;
            }
        }

        if (stage >= WeightStage.MorbidlyObese && !Player.mount.Active && Player.TryGetModPlayer(out MeteorCrushPlayer crush) && !crush._crushEffect)
        {
            if (Player.CheckForSolidGround())
            {
                _groundedState = _groundedState == 0 ? 1 : 2;
            }
            else
            {
                _groundedState = 0;
            }

            if (_groundedState == 1)
            {
                float strength = Math.Min(_prevVel.Y, 10f) / 10f + (float)Math.Pow(Weight.GetClampedFactor(WeightStage.MorbidlyObese, WeightStage.HardImmobile), 1.5f);
                if (strength >= 1 && _prevVel.Y >= 1)
                {
                    Main.NewText("impact");
                    SoundEngine.PlaySound(WgSounds.Thump.Build((strength - 1f) * 0.75f + 0.25f), Player.Center);
                    if (strength >= 1.5)
                    {
                        SoundEngine.PlaySound(WgSounds.Stomp.Build((strength - 1.5f)), Player.Center);
                    }

                    for (int i = 0; i < 40; i++)
                    {
                        Dust dust = Dust.NewDustDirect(new Vector2(Player.position.X, Player.position.Y + Player.height), Player.width, 0, DustID.Smoke, 0f, 0f, 0, default, 1f);
                        dust.velocity.Y = -Math.Abs(dust.velocity.Y);
                        dust.velocity.Y *= (strength-1f)*0.5f+0.1f;
                        dust.velocity.X *= strength-0.5f;
                    }

                    CrackUnderBody(strength * 0.5f);
                }
            }
        }

        foreach ((int id, int value) in _crackTimers)
        {
            if (value == 0)
            {
                Player.hitTile.AddDamage(id, -1);
                if (Player.hitTile.data[id].damage == 0)
                {
                    _crackTimers.Remove(id);

                }
            }
            else
            {
                _crackTimers[id]--;
            }
        }

        _prevVel = Player.velocity;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (target.type == NPCID.KingSlime && target.life - damageDone <= 0 && Weight.GetStage() == WeightStage.Blob)
        {
            ImBigger.Condition.Complete();
        }
    }

    /// <summary>
    /// Applies the relevant hitboxes for a given stage.
    /// </summary>
    /// <param name="stage">The stage id</param>
    void ResizeHitbox(int stage)
    {
        // if the player's hitbox has been externally modified we skip modifying the hitbox
        if ((Player.width + 12) % 16 != 0 || Player.height != Player.defaultHeight)
        {
            if (Player.mount.Active && Player.width != Player.defaultWidth) // Vanilla mounts expect default play width.
            {
                float targetX = Player.position.X + Player.width * 0.5f - Player.defaultWidth * 0.5f;
                Player.width = Player.defaultWidth;
                Player.position.X = targetX;
            }
            return;
        }

        int targetWidth = Player.defaultWidth;
        if (!WgServerConfig.Instance.DisableFatHitbox && !Player.mount.Active && !Player.isLockedToATile)
        {
            targetWidth = WeightValues.GetHitboxWidthInTiles(stage) * 16 - 12;
        }

        if (Player.width != targetWidth)
        {
            float targetX = Player.position.X + Player.width * 0.5f - targetWidth * 0.5f;
            // Make sure we have enough space... otherwise we'd be able to walk through walls
            if (!Collision.SolidCollision(new Vector2(targetX, Player.position.Y), targetWidth, Player.height))
            {
                Player.width = targetWidth;
                Player.position.X = targetX;
            }
            else
            {
                _squishRest = 1.2f;
            }
        }
    }

    /// <summary>
    /// Perform calculations for how the play's softbody should push against blocks and affect their position.
    /// </summary>
    /// <param name="stage">The stage id</param>
    void SoftHitbox(int stage)
    {
        if (WgServerConfig.Instance.DisableFatHitbox || !WeightValues.EnableSoftHitbox(stage) || Player.mount.Active)
        {
            _softSquishLeft = 0f;
            _softSquishRight = 0f;
            return;
        }

        const float pushForce = 0.5f;
        const float width = 16f;

        float Push(int dir)
        {
            Vector2 origin = Player.Center + new Vector2(dir * (Player.width * 0.5f - 1f), 0f);
            if (PhysicsUtility.RayIntersectSolid(origin, dir * Vector2.UnitX, width, out Vector2 point, out _))
            {
                float distance = MathF.Abs(point.X - origin.X);
                float factor = 1f - distance / width;
                float force = factor * factor * pushForce;
                Player.velocity.X -= dir * force;
                return factor;
            }
            return 0f;
        }

        _softSquishLeft = Push(-Player.direction);
        _softSquishRight = Push(Player.direction);
    }

    public override void PreUpdate()
    {
        PreUpdateVisuals();
    }

    public override void PostUpdate()
    {
        if (OwnsPlayer())
        {
            if (Stomach > 0f)
            {
                float rate = (float)Math.Max(Main.dayRate, 1.0);
                if (_digestTimer < 0)
                {
                    float delta = Stomach - MathF.Max(Stomach - Main.rand.NextFloat(DigestAmount * 0.5f, DigestAmount), 0f);
                    SetStomach(Stomach - delta);
                    AddWeight(delta);
                    if (Main.rand.NextBool(75))
                    {
                        PlaySound(WgSounds.Gurgle);
                    }
                    _digestTimer = Main.rand.Next(DigestTime, DigestTime * 2);
                }
                else
                {
                    _digestTimer -= rate;
                }
            }
            else
            {
                _digestTimer = DigestTime * 2; // first digestion tick after being on an empty stomach is always max length
            }
        }

        int frame = Player.legFrame.Y / Player.legFrame.Height;
        if (frame != _lastLegFrame && (frame == 9 || frame == 16) && MathF.Abs(Player.velocity.X) < 10f)
        {
            float volume = Weight.GetClampedFactor(WeightStage.MorbidlyObese, WeightStage.SoftImmobile) * 0.25f;
            if (volume > 0.01f)
            {
                SoundEngine.PlaySound(WgSounds.Thump.Build(volume), Player.Center);
                CrackUnderBody(volume * 2, -1);
            }
        }
        _lastLegFrame = frame;

        UpdateAnimation();
        UpdateJiggle();
        PostUpdateVisuals();

        _finalMaxStage = Math.Clamp(MaxStage, 0, WeightStage.Max);
        if (_ignoreWgBuffTimer > 0)
        {
            _ignoreWgBuffTimer--;
        }

        int stage = Weight.GetStage();
        if (Player.sleeping.isSleeping && stage >= WeightStage.Obese)
        {
            Player.fullRotation = 0;
            Player.gfxOffY -= 16;
        }

        if (stage >= WeightStage.Fat)
        {
            TownNPCRespawnSystem.unlockMilkmaid = true;
        }
    }

    public override void UpdateDead()
    {
        _ignoreWgBuffTimer = 2;
    }

    void CrackUnderBody(float chance, int size = 0)
    {
        if (_getPickaxeDamage == null)
        {
            return;
        }

        Vector2 pos = Player.position + Player.velocity;
        int x1 = ((int)(pos.X) / 16) - size;
        int x2 = ((int)(pos.X + Player.width) / 16) + size;
        int y = (int)(pos.Y + Player.height + 1f) / 16;

        for (int x = x1; x <= x2; x++)
        {
            Tile tile = Framing.GetTileSafely(x, y);
            if (tile != null && Main.rand.NextFloat() < chance && tile.HasUnactuatedTile)
            {
                int tileid = Player.hitTile.HitObject(x, y, 1);
                int damage = (int)_getPickaxeDamage.Invoke(Main.LocalPlayer, [x, y, 35, tileid, tile]);
                Player.hitTile.AddDamage(tileid, damage);
                _crackTimers[tileid] = _crackLifeTime * 60;
            }
        }
    }

    // Taken from CheckIceBreak() in Player.cs
    /// <summary>
    /// checks for breakable ice tiles below the player and breaks them.
    /// </summary>
    void ThinIceBreak()
    {
        Vector2 pos = Player.position + Player.velocity;
        int xStart = (int)(pos.X / 16.0);
        int xEnd = (int)(((double)pos.X + Player.width) / 16.0);
        int yStart = (int)(((double)Player.position.Y + Player.height + 1.0) / 16.0);
        for (int x = xStart; x <= xEnd; x++)
        {
            for (int y = yStart; y <= yStart + 1 && Main.tile[x, y] != null; y++)
            {
                Tile tile = Main.tile[x, y];

                if (tile.HasUnactuatedTile && !WorldGen.SolidTile(x, y - 1) && tile.TileType == TileID.BreakableIce)
                {
                    WorldGen.KillTile(x, y);
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        NetMessage.SendData(MessageID.TileManipulation, number2: x, number3: y);
                }
            }
        }
    }

    // Not exactly proud of this...
    /// <summary>
    /// Check if there is breakable ice below the current player.
    /// </summary>
    bool HasIceBelow()
    {
        Vector2 pos = Player.position + Player.velocity;
        int xStart = (int)(pos.X / 16.0);
        int xEnd = (int)(((double)pos.X + Player.width) / 16.0);
        int yStart = (int)(((double)Player.position.Y + Player.height + 1.0) / 16.0);
        for (int x = xStart; x <= xEnd; x++)
        {
            for (int y = yStart; y <= yStart + 2; y++)
            {
                Tile tile = Main.tile[x, y];
                if (tile.HasUnactuatedTile && tile.TileType == TileID.BreakableIce && !WorldGen.SolidTile(x, y - 1))
                    return true;
            }
        }
        return false;
    }

    //public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    //{
    //    SetWeight(new Weight(Weight.Mass));
    //}

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        modifiers.Knockback *= 1f - _finalKnockbackResistance;
    }

    public override void ResetInfoAccessories()
    {
        _displayWeight = false;
    }

    public override void RefreshInfoAccessoriesFromTeamPlayers(Player otherPlayer)
    {
        if (otherPlayer.Wg()._displayWeight)
        {
            _displayWeight = true;
        }
    }
}
