﻿using Celeste;
using Celeste.Mod.Entities;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Cil;
using Celeste.Mod;
using Mono.Cecil.Cil;

namespace vitmod {
    [CustomEntity("vitellary/coyotebounce")]
    [Tracked]
    public class CoyoteBounceTrigger : Trigger {
        public static void Load() {
            On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
			IL.Celeste.Player.OnCollideH += Player_OnCollideHV;
            IL.Celeste.Player.OnCollideV += Player_OnCollideHV;
		}

        private static bool Actor_OnGround_int(On.Celeste.Actor.orig_OnGround_int orig, Actor self, int downCheck) {
            var result = orig(self, downCheck);
            if (self is Player && CoyoteBounceTrigger.GroundedOverride > 0f) {
                result = true;
            }
            return result;
        }

		private static void Player_OnCollideHV(ILContext il)
		{
			var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt<DashCollision>("Invoke")))
                return;

            var index = cursor.Index;

            int dataArg = -1;

            if (!cursor.TryGotoPrev(MoveType.After,
                instr => instr.MatchLdarg(out dataArg),
                instr => instr.MatchLdfld<CollisionData>("Direction")))
                return;

            cursor.Index = index;

			Logger.Log("CrystallineHelper/CoyoteBounceTrigger", "Adding Player.OnCollideH/V hook");

            cursor.Emit(OpCodes.Dup);
            cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldarg, dataArg);
			cursor.Emit<CollisionData>(OpCodes.Ldfld, "Direction");
			cursor.EmitDelegate(HookDashCollision);
		}

        private static void HookDashCollision(DashCollisionResults result, Player player, Vector2 direction)
        {
			CoyoteBounceTrigger bouncer = CoyoteBounceTrigger.CoyoteTriggerInside;

            if (bouncer == null)
                return;

            if ((direction.Y < 0f && bouncer.directions != BounceDirections.AllDirections) || (direction.X != 0f && bouncer.directions == BounceDirections.Top))
                return;

			player.jumpGraceTimer = bouncer.time;

			if (bouncer.refill)
			{
				player.RefillDash();
				player.RefillStamina();
			}

			if (bouncer.setGrounded)
			{
				CoyoteBounceTrigger.GroundedOverride = bouncer.time;
			}
		}

        public static void Unload() {
            On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
			IL.Celeste.Player.OnCollideH -= Player_OnCollideHV;
			IL.Celeste.Player.OnCollideV -= Player_OnCollideHV;
		}

		public BounceDirections directions;
		public float time;
        public bool refill;
        public bool setGrounded;

        public static CoyoteBounceTrigger CoyoteTriggerInside;
        public static float GroundedOverride;

        public CoyoteBounceTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            directions = data.Enum("directions", BounceDirections.Top);
            time = data.Float("time", 0.1f);
            refill = data.Bool("refill", true);
            setGrounded = data.Bool("setGrounded", false);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            CoyoteTriggerInside = this;
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (CoyoteTriggerInside == this) {
                CoyoteTriggerInside = null;
            }
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
			if (CoyoteTriggerInside == this)
			{
				CoyoteTriggerInside = null;
			}
		}

        public enum BounceDirections
        {
            Top,
            TopAndSides,
            AllDirections
        }
    }
}
