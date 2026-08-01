#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Globalization;
using OpenRA.Mods.CA.Widgets;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SpritePowerMeterLogic : ChromeLogic
	{
		const string PowerUsage = "Game-IngamePowerCounterLogic-Usage";
		const string Infinite = "Game-IngamePowerCounterLogic-Infinite";

		readonly PowerManager powerManager;
		readonly SpritePowerMeterWidget powerMeter;

		[ObjectCreator.UseCtor]
		public SpritePowerMeterLogic(World world, Widget widget)
		{
			powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			powerMeter = widget.Get<SpritePowerMeterWidget>("POWER_BAR");

			var tooltipTextCached = new CachedTransform<(int Usage, int? Capacity), string>(args =>
			{
				var capacity = args.Capacity == null
					? Game.Translate(Infinite)
					: args.Capacity.Value.ToString(NumberFormatInfo.CurrentInfo);

				return Game.Translate(PowerUsage,
					"usage", args.Usage.ToString(NumberFormatInfo.CurrentInfo),
					"capacity", capacity);
			});

			powerMeter.GetTooltipText = () =>
			{
				var capacity = powerManager.PowerProvided == 1000000 ? (int?)null : powerManager.PowerProvided;
				return tooltipTextCached.Update((powerManager.PowerDrained, capacity));
			};
		}

		void CheckFlash()
		{
			var startWarningFlash = powerManager.PowerState != PowerState.Normal;

			if (powerMeter.LastTotalPowerDisplay != powerMeter.TotalPowerDisplay)
			{
				startWarningFlash = true;
				powerMeter.LastTotalPowerDisplay = powerMeter.TotalPowerDisplay;
			}

			if (startWarningFlash && powerMeter.WarningFlash <= 0)
				powerMeter.WarningFlash = powerMeter.WarningFlashDuration;
		}

		public override void Tick()
		{
			powerMeter.LowPower = powerManager.PowerState == PowerState.Low;

			powerMeter.TotalPowerDisplay = Math.Max(powerManager.PowerProvided, powerManager.PowerDrained);

			powerMeter.TotalPowerStep = powerMeter.TotalPowerDisplay / powerMeter.PowerUnitsPerBar;
			powerMeter.PowerUsedStep = powerManager.PowerDrained / powerMeter.PowerUnitsPerBar;
			powerMeter.PowerAvailableStep = powerManager.PowerProvided / powerMeter.PowerUnitsPerBar;

			// Display a percentage if the bar is maxed out
			if (powerMeter.TotalPowerStep > powerMeter.NumberOfBars)
			{
				var powerFraction = powerMeter.NumberOfBars / powerMeter.TotalPowerStep;
				powerMeter.TotalPowerDisplay = (int)(powerMeter.TotalPowerDisplay * powerFraction);
				powerMeter.TotalPowerStep *= powerFraction;
				powerMeter.PowerUsedStep *= powerFraction;
				powerMeter.PowerAvailableStep *= powerFraction;
			}

			CheckFlash();
		}
	}
}
