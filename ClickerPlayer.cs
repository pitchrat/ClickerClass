using ClickerClass.Buffs;
using ClickerClass.Items;
using ClickerClass.NPCs;
using ClickerClass.Projectiles;
using ClickerClass.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameInput;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Audio;

namespace ClickerClass
{
	public partial class ClickerPlayer : ModPlayer
	{
		//Key presses
		public double pressedAutoClick;
		public int clickerClassTime = 0;

		//-Clicker-
		//Misc
		public Color clickerRadiusColor = Color.White;
		/// <summary>
		/// Cached clickerRadiusColor for draw
		/// </summary>
		public Color clickerRadiusColorDraw = Color.Transparent;
		public float ClickerRadiusColorMultiplier => clickerRadiusRangeAlpha * clickerRadiusSwitchAlpha;
		/// <summary>
		/// Visual indicator that the cursor is inside clicker radius
		/// </summary>
		public bool clickerInRange = false;
		/// <summary>
		/// Visual indicator that the cursor is inside Motherboard radius
		/// </summary>
		public bool clickerInRangeMotherboard = false;
		public bool GlowVisual => clickerInRange || clickerInRangeMotherboard;
		public bool clickerSelected = false;
		/// <summary>
		/// False if phase reach
		/// </summary>
		public bool clickerDrawRadius = false;
		public const float clickerRadiusSwitchAlphaMin = 0f;
		public const float clickerRadiusSwitchAlphaMax = 1f;
		public const float clickerRadiusSwitchAlphaStep = clickerRadiusSwitchAlphaMax / 40f;
		public float clickerRadiusSwitchAlpha = clickerRadiusSwitchAlphaMin;

		//Gameplay only: not related to player select screen
		public bool CanDrawRadius => !Main.gameMenu && !Player.dead && clickerRadiusSwitchAlpha > clickerRadiusSwitchAlphaMin;

		public const float clickerRadiusRangeAlphaMin = 0.2f;
		public const float clickerRadiusRangeAlphaMax = 0.8f;
		public const float clickerRadiusRangeAlphaStep = clickerRadiusRangeAlphaMax / 20f;
		public float clickerRadiusRangeAlpha = clickerRadiusRangeAlphaMin;

		public bool clickerAutoClick = false;
		/// <summary>
		/// Saved amount of clicks done with any clicker, accumulated, fluff
		/// </summary>
		public int clickerTotal = 0;
		/// <summary>
		/// Amount of clicks done, constantly incremented. Used for click effect proccing
		/// </summary>
		public int clickAmount = 0;
		/// <summary>
		/// cps
		/// </summary>
		public int clickerPerSecond = 0;
		private const int ClickQueueCount = 60;
		/// <summary>
		/// Keeps track of clicks done in the last <see cref="ClickQueueCount"/> ticks. true if a click occured, otherwise false
		/// </summary>
		private Queue<bool> clicks;
		/// <summary>
		/// Amount of money generated by clicker items
		/// </summary>
		public int clickerMoneyGenerated = 0;
		/// <summary>
		/// Offset generated by UI gauges. Each gauge increments the value allowing for multiple gauges to display at once
		/// </summary>
		public int clickerGaugeOffset = 0;

		//Click effects
		/// <summary>
		/// Used to track effect names that are currently active. Resets every tick
		/// </summary>
		private Dictionary<string, bool> ClickEffectActive = new Dictionary<string, bool>();

		//Out of combat
		public const int OutOfCombatTimeMax = 300;
		public bool OutOfCombat => outOfCombatTimer <= 0;
		public int outOfCombatTimer = 0;

		//Armor
		public int setAbilityDelayTimer = 0;
		public float setMotherboardRatio = 0f;
		public float setMotherboardAngle = 0f;
		/// <summary>
		/// Calculated after clickerRadius is calculated, and if the Motherboard set is worn
		/// </summary>
		public Vector2 setMotherboardPosition = Vector2.Zero;
		public float setMotherboardAlpha = 0f;
		public int setMotherboardFrame = 0;
		public bool setMotherboardFrameShift = false;
		public bool setMotherboard = false;
		public bool SetMotherboardDraw => setMotherboard && setMotherboardRatio > 0;

		public bool setMice = false;
		public bool setPrecursor = false;
		public bool setOverclock = false;
		public bool setRGB = false;
		
		public int setPrecursorTimer = 0;

		//Acc
		[Obsolete("Use HasClickEffect(\"ClickerClass:ChocolateChip\") and EnableClickEffect(\"ClickerClass:ChocolateChip\") instead", false)]
		public bool accChocolateChip = false;
		public bool accEnchantedLED = false;
		public bool accEnchantedLED2 = false; //different visuals
		public bool accHandCream = false;
		[Obsolete("Use HasClickEffect(\"ClickerClass:StickyKeychain\") and EnableClickEffect(\"ClickerClass:StickyKeychain\") instead", false)]
		public bool accStickyKeychain = false;
		public bool accGlassOfMilk = false;
		public Item accCookieItem = null;
		public bool accCookie = false;
		public bool accCookie2 = false; //different visuals
		public bool accClickingGlove = false;
		public bool accAncientClickingGlove = false;
		public bool accRegalClickingGlove = false;
		public bool accPortableParticleAccelerator = false; //"is wearing"
		public bool accPortableParticleAccelerator2 = false; //"is active", client only
		public bool accGoldenTicket = false;
		public bool accTriggerFinger = false;
		public bool accIcePack = false;
		public bool accMouseTrap = false;
		public Item accPaperclipsItem = null;
		public bool AccPaperclips => accPaperclipsItem != null && !accPaperclipsItem.IsAir;
		public bool accHotKeychain = false;
		public bool accHotKeychain2 = false;

		public int accClickingGloveTimer = 0;
		public int accCookieTimer = 0;
		public int accPaperclipsAmount = 0;
		public int accHotKeychainTimer = 0;
		public int accHotKeychainAmount = 0;

		//Stats
		/// <summary>
		/// Click damage add flat
		/// </summary>
		public int clickerDamageFlat = 0;

		/// <summary>
		/// How many less clicks are required to trigger an effect
		/// </summary>
		public int clickerBonus = 0;

		/// <summary>
		/// Multiplier to clicks required to trigger an effect
		/// </summary>
		public float clickerBonusPercent = 1f;

		/// <summary>
		/// Effective clicker radius in pixels when multiplied by 100
		/// </summary>
		public float clickerRadius = 1f;

		/// <summary>
		/// Cached clickerRadius for draw
		/// </summary>
		public float clickerRadiusDraw = 1f;

		/// <summary>
		/// Clicker radius in pixels
		/// </summary>
		public float ClickerRadiusReal => clickerRadius * 100;

		/// <summary>
		/// Clicker draw radius in pixels
		/// </summary>
		public float ClickerRadiusRealDraw => clickerRadiusDraw * 100;

		/// <summary>
		/// Motherboard radius in pixels
		/// </summary>
		public float ClickerRadiusMotherboard => ClickerRadiusReal * 0.5f;

		/// <summary>
		/// Motherboard draw radius in pixels
		/// </summary>
		public float ClickerRadiusMotherboardDraw => ClickerRadiusRealDraw * 0.5f;

		//Helper methods
		/// <summary>
		/// Enables the use of a click effect for this player
		/// </summary>
		/// <param name="name">The unique effect name</param>
		public void EnableClickEffect(string name)
		{
			if (ClickEffectActive.TryGetValue(name, out _))
			{
				ClickEffectActive[name] = true;
			}
		}

		/// <summary>
		/// Enables the use of click effects for this player
		/// </summary>
		/// <param name="names">The unique effect names</param>
		public void EnableClickEffect(IEnumerable<string> names)
		{
			foreach (var name in names)
			{
				EnableClickEffect(name);
			}
		}

		/// <summary>
		/// Checks if the player has a click effect enabled
		/// </summary>
		/// <param name="name">The unique effect name</param>
		/// <returns><see langword="true"/> if enabled</returns>
		public bool HasClickEffect(string name)
		{
			if (ClickEffectActive.TryGetValue(name, out _))
			{
				return ClickEffectActive[name];
			}
			return false;
		}

		/// <summary>
		/// Checks if the player has a click effect enabled
		/// </summary>
		/// <param name="name">The unique effect name</param>
		/// <param name="effect">The effect associated with the name</param>
		/// <returns><see langword="true"/> if enabled</returns>
		public bool HasClickEffect(string name, out ClickEffect effect)
		{
			effect = null;
			if (HasClickEffect(name))
			{
				return ClickerSystem.IsClickEffect(name, out effect);
			}
			return false;
		}

		//Unused yet
		public bool HasAnyClickEffect()
		{
			foreach (var value in ClickEffectActive.Values)
			{
				if (value) return true;
			}
			return false;
		}

		internal void ResetAllClickEffects()
		{
			//Stupid trick to be able to write to a value in a dictionary
			foreach (var key in ClickEffectActive.Keys.ToList())
			{
				ClickEffectActive[key] = false;
			}
		}

		/// <summary>
		/// Call to register a click towards the "clicks per second" and total calculations
		/// </summary>
		internal void AddClick()
		{
			clicks.Enqueue(true);
			clickerTotal++;
		}

		/// <summary>
		/// Call to increment the click amount counter used for proccing click effects
		/// </summary>
		internal void AddClickAmount()
		{
			clickAmount++;
		}

		private void FillClickQueue()
		{
			int missing = ClickQueueCount - clicks.Count;
			for (int i = 0; i < missing; i++)
			{
				clicks.Enqueue(false);
			}
		}

		/// <summary>
		/// Manages the click queue and calculates <see cref="clickerPerSecond"/>
		/// </summary>
		private void HandleCPS()
		{
			if (clicks.Count < ClickQueueCount - 1)
			{
				FillClickQueue();
			}

			//Queue can get more than ClickQueueCount: when a click happens
			clicks.Dequeue();

			clickerPerSecond = clicks.Count(val => val);
		}

		private void HandleRadiusAlphas()
		{
			if (clickerDrawRadius)
			{
				if (clickerRadiusSwitchAlpha < clickerRadiusSwitchAlphaMax)
				{
					clickerRadiusSwitchAlpha += clickerRadiusSwitchAlphaStep;
				}
				else
				{
					clickerRadiusSwitchAlpha = clickerRadiusSwitchAlphaMax;
				}
			}
			else
			{
				if (clickerRadiusSwitchAlpha > clickerRadiusSwitchAlphaMin)
				{
					clickerRadiusSwitchAlpha -= clickerRadiusSwitchAlphaStep;
				}
				else
				{
					clickerRadiusSwitchAlpha = clickerRadiusSwitchAlphaMin;
				}
			}

			if (GlowVisual)
			{
				if (clickerRadiusRangeAlpha < clickerRadiusRangeAlphaMax)
				{
					clickerRadiusRangeAlpha += clickerRadiusRangeAlphaStep;
				}
				else
				{
					clickerRadiusRangeAlpha = clickerRadiusRangeAlphaMax;
				}
			}
			else
			{
				if (clickerRadiusRangeAlpha > clickerRadiusRangeAlphaMin)
				{
					clickerRadiusRangeAlpha -= clickerRadiusRangeAlphaStep;
				}
				else
				{
					clickerRadiusRangeAlpha = clickerRadiusRangeAlphaMin;
				}
			}
		}

		/// <summary>
		/// Returns the position from the ratio and angle, given the radius in pixels
		/// </summary>
		/// <param name="realRadius">The reference radius</param>
		public Vector2 CalculateMotherboardPosition(float realRadius)
		{
			float length = setMotherboardRatio * realRadius;
			Vector2 direction = setMotherboardAngle.ToRotationVector2();
			return direction * length;
		}

		/// <summary>
		/// Construct ratio and angle from position
		/// </summary>
		public void SetMotherboardRelativePosition(Vector2 position)
		{
			Vector2 toPosition = position - Player.Center;
			float length = toPosition.Length();
			float radius = ClickerRadiusReal;
			float ratio = length / radius;
			if (ratio < 0.6f)
			{
				//Enforce minimal range
				ratio = 0.6f;
			}
			setMotherboardRatio = ratio;
			setMotherboardAngle = toPosition.ToRotation();
		}

		/// <summary>
		/// Dispels the motherboard position
		/// </summary>
		public void ResetMotherboardPosition()
		{
			setMotherboardRatio = 0f;
			setMotherboardAngle = 0f;
		}

		internal int originalSelectedItem;
		internal bool autoRevertSelectedItem = false;

		/// <summary>
		/// Uses the item in the specified index from the players inventory
		/// </summary>
		public void QuickUseItemInSlot(int index)
		{
			if (index > -1 && index < Main.InventorySlotsTotal && Player.inventory[index].type != ItemID.None)
			{
				if (Player.CheckMana(Player.inventory[index], -1, false, false))
				{
					originalSelectedItem = Player.selectedItem;
					autoRevertSelectedItem = true;
					Player.selectedItem = index;
					Player.controlUseItem = true;
					Player.ItemCheck(Player.whoAmI);
				}
				else
				{
					SoundEngine.PlaySound(SoundID.Drip, (int)Player.Center.X, (int)Player.Center.Y, Main.rand.Next(3));
				}
			}
		}

		/// <summary>
		/// Returns the amount of clicks required for an effect of the given name to trigger (defaults to the item's assigned effect). Includes various bonuses
		/// </summary>
		public int GetClickAmountTotal(ClickerItemCore clickerItem, string name)
		{
			//Doesn't go below 1
			int amount = 1;
			if (ClickerSystem.IsClickEffect(name, out ClickEffect effect))
			{
				amount = effect.Amount;
			}
			float percent = Math.Max(0f, clickerBonusPercent);
			int prePercentAmount = Math.Max(1, amount + clickerItem.clickBoostPrefix - clickerBonus);
			return Math.Max(1, (int)(prePercentAmount * percent));
		}

		/// <summary>
		/// Returns the amount of clicks required for the effect of this item to trigger. Includes various bonuses
		/// </summary>
		public int GetClickAmountTotal(Item item, string name)
		{
			return GetClickAmountTotal(item.GetGlobalItem<ClickerItemCore>(), name);
		}

		public override void ResetEffects()
		{
			//-Clicker-
			//Misc
			clickerRadiusColor = Color.White;
			clickerInRange = false;
			clickerInRangeMotherboard = false;
			clickerSelected = false;
			clickerDrawRadius = false;
			clickerGaugeOffset = 0;

			//Click Effects
			ResetAllClickEffects();

			//Armor
			setMotherboard = false;
			setMice = false;
			setPrecursor = false;
			setOverclock = false;
			setRGB = false;

			//Acc
			accEnchantedLED = false;
			accEnchantedLED2 = false;
			accHandCream = false;
			accGlassOfMilk = false;
			accCookieItem = null;
			accCookie = false;
			accCookie2 = false;
			accClickingGlove = false;
			accAncientClickingGlove = false;
			accRegalClickingGlove = false;
			accPortableParticleAccelerator = false;
			accPortableParticleAccelerator2 = false;
			accGoldenTicket = false;
			accTriggerFinger = false;
			accIcePack = false;
			accMouseTrap = false;
			accPaperclipsItem = null;
			accHotKeychain = false;
			accHotKeychain2 = false;

			//Stats
			clickerDamageFlat = 0;
			clickerBonus = 0;
			clickerBonusPercent = 1f;
			clickerRadius = 1f;
		}

		public override void UpdateAutopause()
		{
			clickerGaugeOffset = 0; //Otherwise it starts moving downwards when in the menu/paused
			clickerRadius = 1f;
		}

		public override void Initialize()
		{
			clickerTotal = 0;
			clickerMoneyGenerated = 0;
			
			ClickEffectActive = new Dictionary<string, bool>();
			foreach (var name in ClickerSystem.GetAllEffectNames())
			{
				ClickEffectActive.Add(name, false);
			}

			clicks = new Queue<bool>();
			FillClickQueue();
		}

		public override void SaveData(TagCompound tag)
		{
			tag.Add("clickerTotal", clickerTotal);
			tag.Add("clickerMoneyGenerated", clickerMoneyGenerated);
		}

		public override void LoadData(TagCompound tag)
		{
			clickerTotal = tag.GetInt("clickerTotal");
			clickerMoneyGenerated = tag.GetInt("clickerMoneyGenerated");
		}

		public override void ProcessTriggers(TriggersSet triggersSet)
		{
			// checks for frozen, webbed and stoned
			if (Player.CCed)
			{
				return;
			}

			if (ClickerClass.AutoClickKey.JustPressed)
			{
				if (Math.Abs(clickerClassTime - pressedAutoClick) > 60)
				{
					pressedAutoClick = clickerClassTime;

					SoundEngine.PlaySound(SoundID.MenuTick, Player.position);
					clickerAutoClick = clickerAutoClick ? false : true;
				}
			}
		}

		public override void PreUpdate()
		{
			if (Player.whoAmI == Main.myPlayer)
			{
				if (autoRevertSelectedItem)
				{
					if (Player.itemTime == 0 && Player.itemAnimation == 0)
					{
						Player.selectedItem = originalSelectedItem;
						autoRevertSelectedItem = false;
					}
				}
			}

			if (Player.whoAmI == Main.myPlayer)
			{
				if (Player.itemTime == 0 && Player.itemAnimation == 0)
				{
					if (accRegalClickingGlove && accClickingGloveTimer > 30)
					{
						QuickUseItemInSlot(Player.selectedItem);
						accClickingGloveTimer = 0;
					}
					else if (accAncientClickingGlove && accClickingGloveTimer > 60)
					{
						QuickUseItemInSlot(Player.selectedItem);
						accClickingGloveTimer = 0;
					}
					else if (accClickingGlove && accClickingGloveTimer > 180)
					{
						QuickUseItemInSlot(Player.selectedItem);
						accClickingGloveTimer = 0;
					}
				}
			}
		}

		public override void PostUpdateEquips()
		{
			clickerClassTime++;
			if (clickerClassTime > 36000)
			{
				clickerClassTime = 0;
			}

			if (!accHandCream && !accIcePack)
			{
				clickerAutoClick = false;
			}

			if (setAbilityDelayTimer > 0)
			{
				setAbilityDelayTimer--;
			}

			if (!setMotherboard)
			{
				setMotherboardPosition = Vector2.Zero;
				setMotherboardRatio = 0f;
				setMotherboardAngle = 0f;
			}
			else
			{
				setMotherboardAlpha += !setMotherboardFrameShift ? 0.025f : -0.025f;
				if (setMotherboardAlpha >= 1f)
				{
					setMotherboardFrameShift = true;
				}

				if (setMotherboardFrameShift && setMotherboardAlpha <= 0.25f)
				{
					setMotherboardFrame++;
					if (setMotherboardFrame >= 4)
					{
						setMotherboardFrame = 0;
					}
					setMotherboardFrameShift = false;
				}
			}

			Item heldItem = Player.HeldItem;
			if (ClickerSystem.IsClickerWeapon(heldItem, out ClickerItemCore clickerItem))
			{
				EnableClickEffect(clickerItem.itemClickEffects);
				clickerSelected = true;
				clickerDrawRadius = true;
				if (HasClickEffect(ClickEffect.PhaseReach))
				{
					clickerDrawRadius = false;
				}

				if (clickerItem.radiusBoost > 0f)
				{
					clickerRadius += clickerItem.radiusBoost;
				}

				if (clickerItem.radiusBoostPrefix > 0f)
				{
					clickerRadius += clickerItem.radiusBoostPrefix;
				}

				//Cache for draw
				clickerRadiusDraw = clickerRadius;

				//collision
				float radiusSQ = ClickerRadiusReal * ClickerRadiusReal;
				if (Vector2.DistanceSquared(Main.MouseWorld, Player.Center) < radiusSQ && Collision.CanHit(new Vector2(Player.Center.X, Player.Center.Y - 12), 1, 1, Main.MouseWorld, 1, 1))
				{
					clickerInRange = true;
				}
				if (setMotherboard)
				{
					//Important: has to be after final clickerRadius calculation because it depends on it
					setMotherboardPosition = Player.Center + CalculateMotherboardPosition(ClickerRadiusReal);
				}

				//collision
				radiusSQ = ClickerRadiusMotherboard * ClickerRadiusMotherboard;
				if (Vector2.DistanceSquared(Main.MouseWorld, setMotherboardPosition) < radiusSQ && Collision.CanHit(setMotherboardPosition, 1, 1, Main.MouseWorld, 1, 1))
				{
					clickerInRangeMotherboard = true;
				}
				clickerRadiusColor = clickerItem.clickerRadiusColor;

				//Cache for draw
				clickerRadiusColorDraw = Color.Lerp(clickerRadiusColorDraw, clickerRadiusColor, clickerRadiusSwitchAlpha);

				//Glove acc
				if (!OutOfCombat && (accClickingGlove || accAncientClickingGlove || accRegalClickingGlove))
				{
					accClickingGloveTimer++;
				}
				else
				{
					accClickingGloveTimer = 0;
				}

				if (setPrecursor && !OutOfCombat && clickerInRange)
				{
					setPrecursorTimer++;
					if (setPrecursorTimer > 10)
					{
						if (Main.myPlayer == Player.whoAmI)
						{
							Projectile.NewProjectile(Player.GetProjectileSource_SetBonus(0), Main.MouseWorld.X + 8, Main.MouseWorld.Y + 11, 0f, 0f, ModContent.ProjectileType<PrecursorPro>(), (int)(heldItem.damage * 0.2f), 0f, Player.whoAmI);
						}
						setPrecursorTimer = 0;
					}
				}
				else
				{
					setPrecursorTimer = 0;
				}
			}
			else
			{
				clickerRadiusColorDraw = Color.Lerp(Color.Transparent, clickerRadiusColorDraw, clickerRadiusSwitchAlpha);
			}

			if (Player.HasBuff(ModContent.BuffType<Haste>()))
			{
				Player.armorEffectDrawShadow = true;
			}

			//Acc
			//Cookie acc
			if (accCookieItem != null && !accCookieItem.IsAir && (accCookie || accCookie2) && clickerSelected)
			{
				accCookieTimer++;
				if (Player.whoAmI == Main.myPlayer && accCookieTimer > 600)
				{
					int radius = (int)(95 * clickerRadius);
					if (radius > 350)
					{
						radius = 350;
					}

					//Circles give me a damn headache...
					double r = radius * Math.Sqrt(Main.rand.NextFloat(0f, 1f));
					double theta = Main.rand.NextFloat(0f, 1f) * MathHelper.TwoPi;
					double xOffset = Player.Center.X + r * Math.Cos(theta);
					double yOffset = Player.Center.Y + r * Math.Sin(theta);

					int frame = 0;
					if (accCookie2 && Main.rand.NextFloat() <= 0.1f)
					{
						frame= 1;
					}
					Projectile.NewProjectile(Player.GetProjectileSource_Accessory(accCookieItem), (float)xOffset, (float)yOffset, 0f, 0f, ModContent.ProjectileType<CookiePro>(), 0, 0f, Player.whoAmI, frame);

					accCookieTimer = 0;
				}

				//Cookie Click
				if (Player.whoAmI == Main.myPlayer)
				{
					for (int i = 0; i < 1000; i++)
					{
						Projectile cookieProjectile = Main.projectile[i];

						if (cookieProjectile.active && cookieProjectile.type == ModContent.ProjectileType<CookiePro>() && cookieProjectile.owner == Player.whoAmI)
						{
							if (Main.mouseLeft && Main.mouseLeftRelease && cookieProjectile.DistanceSQ(Main.MouseWorld) < 30 * 30)
							{
								if (cookieProjectile.ai[0] == 1f)
								{
									SoundEngine.PlaySound(2, (int)Player.position.X, (int)Player.position.Y, 4);
									Player.AddBuff(ModContent.BuffType<CookieBuff>(), 600);
									Player.HealLife(10);
									for (int k = 0; k < 10; k++)
									{
										Dust dust = Dust.NewDustDirect(cookieProjectile.Center, 20, 20, 87, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 0, default, 1.15f);
										dust.noGravity = true;
									}
								}
								else
								{
									SoundEngine.PlaySound(2, (int)Player.position.X, (int)Player.position.Y, 2);
									Player.AddBuff(ModContent.BuffType<CookieBuff>(), 300);
									for (int k = 0; k < 10; k++)
									{
										Dust dust = Dust.NewDustDirect(cookieProjectile.Center, 20, 20, 0, Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f), 75, default, 1.5f);
										dust.noGravity = true;
									}
								}
								cookieProjectile.Kill();
							}
						}
					}
				}
			}

			//Portable Particle Accelerator acc
			if (accPortableParticleAccelerator && Main.myPlayer == Player.whoAmI)
			{
				float radius = ClickerRadiusReal * 0.5f;
				if (Player.DistanceSQ(Main.MouseWorld) < radius * radius)
				{
					accPortableParticleAccelerator2 = true;
				}
			}
			
			//Balloon Defense effect
			if (Player.whoAmI == Main.myPlayer)
			{
				for (int i = 0; i < 1000; i++)
				{
					Projectile balloonProjectile = Main.projectile[i];

					if (balloonProjectile.active && balloonProjectile.ai[0] == 0f && balloonProjectile.type == ModContent.ProjectileType<BalloonClickerPro>() && balloonProjectile.owner == Player.whoAmI)
					{
						if (Main.mouseLeft && Main.mouseLeftRelease && balloonProjectile.DistanceSQ(new Vector2(Main.MouseWorld.X, Main.MouseWorld.Y + 40)) < 20 * 20)
						{
							for (int k = 0; k < 8; k++)
							{
								Dust dust = Dust.NewDustDirect(balloonProjectile.Center - new Vector2(4), 8, 8, 115, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 100, default, 1.25f);
								dust.noGravity = true;
							}
							SoundEngine.PlaySound(4, (int)Player.position.X, (int)Player.position.Y, 63);
							balloonProjectile.ai[0] = 1f;
							balloonProjectile.timeLeft = 300;
						}
					}
				}
			}
			
			HandleCPS();

			HandleRadiusAlphas();

			//Milk acc
			if (accGlassOfMilk)
			{
				float bonusDamage = (float)(clickerPerSecond * 0.015f);
				if (bonusDamage >= 0.15f)
				{
					bonusDamage = 0.15f;
				}
				Player.GetDamage<ClickerDamage>() += bonusDamage;
			}
			
			//Hot Keychain
			if (accHotKeychain && !OutOfCombat)
			{
				if (clickerSelected)
				{
					if (accHotKeychainAmount < 0)
					{
						accHotKeychainAmount = 0;
					}
					accHotKeychain2 = true;

					accHotKeychainTimer++;
					if (accHotKeychainTimer > 60)
					{
						int accHotKeychainSpice = (int)(8 - clickerPerSecond);
						Color color = new Color(150, 150, 150);
						if (accHotKeychainSpice > 0)
						{
							color = new Color(255, 150, 75);

							for (int k = 0; k < 2 * accHotKeychainSpice; k++)
							{
								Vector2 offset = new Vector2(Main.rand.Next(-25, 26), Main.rand.Next(-25, 26));
								Dust dust = Dust.NewDustDirect(Player.position + offset, Player.width, Player.height, 174, Scale: 1f);
								dust.noGravity = true;
								dust.velocity = -offset * 0.05f;
							}
						}

						CombatText.NewText(Player.Hitbox, color, accHotKeychainSpice, true, true);

						accHotKeychainAmount += accHotKeychainSpice;
						accHotKeychainTimer = 0;

						if (accHotKeychainAmount > 50)
						{
							Player.AddBuff(BuffID.OnFire3, 300);
							SoundEngine.PlaySound(2, (int)Player.position.X, (int)Player.position.Y, 74);
							for (int k = 0; k < 10; k++)
							{
								Vector2 offset = new Vector2(Main.rand.Next(-25, 26), Main.rand.Next(-25, 26));
								Dust dust = Dust.NewDustDirect(Player.position + offset, Player.width, Player.height, 174, Scale: 1.5f);
								dust.noGravity = true;
								dust.velocity = -offset * 0.05f;
							}
							accHotKeychainAmount = 0;
						}
					}
				}
			}
			else
			{
				accHotKeychainTimer = 0;
				if (OutOfCombat)
				{
					accHotKeychainAmount = 0;
				}
			}

			// Out of Combat timer
			if (outOfCombatTimer > 0)
			{
				outOfCombatTimer--;
			}
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			if (ClickerSystem.IsClickerProj(proj))
			{
				if (target.GetGlobalNPC<ClickerGlobalNPC>().embrittle)
				{
					damage += 8;
				}
			}
		}

		public override void OnHitNPCWithProj(Projectile projectile, NPC target, int damage, float knockback, bool crit)
		{
			if (ClickerSystem.IsClickerProj(projectile))
			{
				if (target.value > 0f)
				{
					if (accGoldenTicket)
					{
						for (int k = 0; k < 15; k++)
						{
							int dust = Dust.NewDust(target.position, 20, 20, 11, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 75, default(Color), 1.25f);
							Main.dust[dust].noGravity = true;
						}
						
						int amount = 1 + Main.rand.Next(5);
						int coin = Item.NewItem(target.Hitbox, ItemID.CopperCoin, amount, false, 0, false, false);
						if (amount > 0)
						{
							clickerMoneyGenerated += amount;
						}
						if (Main.netMode == NetmodeID.MultiplayerClient)
						{
							NetMessage.SendData(MessageID.SyncItem, -1, -1, null, coin, 1f);
						}
					}

					if (AccPaperclips && projectile.type != ModContent.ProjectileType<BottomlessBoxofPaperclipsPro>())
					{
						int matterAmount = (int)((target.height * target.width) / 200);
						if (matterAmount > 10)
						{
							matterAmount = 10;
						}
						accPaperclipsAmount += matterAmount;
						
						if (accPaperclipsAmount >= 100)
						{
							SoundEngine.PlaySound(2, (int)Player.position.X, (int)Player.position.Y, 108);
							for (int k = 0; k < 15; k++)
							{
								int dust = Dust.NewDust(target.position, 20, 20, 1, Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f), 150, default(Color), 1.35f);
								Main.dust[dust].noGravity = true;
							}

							if (Main.myPlayer == Player.whoAmI)
							{
								for (int k = 0; k < 4; k++)
								{
									Projectile.NewProjectile(Player.GetProjectileSource_Accessory(accPaperclipsItem), Main.MouseWorld.X, Main.MouseWorld.Y, Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-6f, -2f), ModContent.ProjectileType<BottomlessBoxofPaperclipsPro>(), damage, 2f, Player.whoAmI);
								}
							}

							accPaperclipsAmount = 0;
						}
					}
					
					if (target.GetGlobalNPC<ClickerGlobalNPC>().crystalSlime && projectile.type != ModContent.ProjectileType<ClearKeychainPro2>())
					{
						target.GetGlobalNPC<ClickerGlobalNPC>().crystalSlimeEnd = true;
						int crystal = ModContent.ProjectileType<ClearKeychainPro2>();
						bool spawnEffects = true;
						
						float num102 = 10f;
						int num103 = 0;
						while ((float)num103 < num102)
						{
							float hasSpawnEffects = spawnEffects ? 1f : 0f;
							Vector2 vector12 = Vector2.UnitX * 0f;
							vector12 += -Vector2.UnitY.RotatedBy((double)((float)num103 * (6.28318548f / num102)), default(Vector2)) * new Vector2(10f, 10f);
							vector12 = vector12.RotatedBy((double)target.velocity.ToRotation(), default(Vector2));
							int damageAmount = (int)(damage * 0.25f);
							damageAmount = damageAmount < 1 ? 1 : damageAmount;
							Projectile.NewProjectile(Player.GetProjectileSource_Accessory(accPaperclipsItem), target.Center + vector12, target.velocity * 0f + vector12.SafeNormalize(Vector2.UnitY) * 10f, crystal, damageAmount, 1f, Main.myPlayer, target.whoAmI, hasSpawnEffects);
							int num = num103;
							num103 = num + 1;
						}
						spawnEffects = false;
					}
				}
			}

			if (projectile.type != ModContent.ProjectileType<PrecursorPro>())
			{
				outOfCombatTimer = OutOfCombatTimeMax;
			}
		}

		public override void OnHitNPC(Item item, NPC target, int damage, float knockback, bool crit)
		{
			outOfCombatTimer = OutOfCombatTimeMax;
		}

		public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit)
		{
			outOfCombatTimer = OutOfCombatTimeMax;
		}

		public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
		{
			// Don't count as in combat after death, in case respawn timer is less than OutOfCombatTimeMax
			outOfCombatTimer = 0;
		}
	}
}
