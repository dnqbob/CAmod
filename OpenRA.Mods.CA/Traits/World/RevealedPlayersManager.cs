#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum RevealPlayerFactionType
	{
		OnGameStart,
		OnSelection
	}

	[Desc("Attached to the world actor to track which players are revealed (for displaying their real faction in scores panel).")]
	[TraitLocation(SystemActors.Player)]
	public class RevealedPlayersManagerInfo : TraitInfo
	{
		[Desc("When to reveal players.")]
		public RevealPlayerFactionType RevealCondition { get; set; } = RevealPlayerFactionType.OnGameStart;

		public override object Create(ActorInitializer init) { return new RevealedPlayersManager(init.Self, this); }
	}

	public class RevealedPlayersManager : INotifySelection
	{
		readonly Actor self;
		readonly World world;
		readonly RevealedPlayersManagerInfo info;
		public HashSet<Player> Players { get; }

		public RevealedPlayersManager(Actor self, RevealedPlayersManagerInfo info)
		{
			this.self = self;
			world = self.World;
			Players = new HashSet<Player>();
			this.info = info;
		}

		public bool RevealOnGameStart => info.RevealCondition == RevealPlayerFactionType.OnGameStart;

		public void RevealPlayer(Player player)
		{
			Players.Add(player);
		}

		public bool IsRevealed(Player player)
		{
			return Players.Contains(player);
		}

		void INotifySelection.SelectionChanged()
		{
			if (info.RevealCondition != RevealPlayerFactionType.OnSelection)
				return;

			// Disable for spectators
			if (world.LocalPlayer == null || world.LocalPlayer.Spectating)
				return;

			if (self.Owner != world.LocalPlayer)
				return;

			var players = world.Selection.Actors
				.Where(a => a.IsInWorld)
				.Select(a => a.Owner);

			foreach (var player in players)
			{
				foreach (var alliedPlayer in world.Players.Where(p => p == self.Owner || self.Owner.RelationshipWith(p) == PlayerRelationship.Ally))
					alliedPlayer.PlayerActor.TraitOrDefault<RevealedPlayersManager>()?.RevealPlayer(player);
			}
		}
	}
}
