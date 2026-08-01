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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	class GameInfoStatsLogicCA : ChromeLogic
	{
		
		const string Unmute = "Game-GameInfoStatsLogic-Unmute";

		
		const string Mute = "Game-GameInfoStatsLogic-Mute";

		
		const string Accomplished = "Game-GameInfoStatsLogic-Accomplished";

		
		const string Failed = "Game-GameInfoStatsLogic-Failed";

		
		const string InProgress = "Game-GameInfoStatsLogic-InProgress";

		const string TeamNumber = "Game-ObserverStatsLogic-TeamName";

		
		const string NoTeam = "Game-ObserverStatsLogic-NoTeam";

		
		const string Spectators = "Game-GameInfoStatsLogic-Spectators";

		
		const string Gone = "Game-GameInfoStatsLogic-Gone";

		
		const string KickTooltip = "Game-GameInfoStatsLogic-KickPlayerTooltip";

		const string KickTitle = "Game-GameInfoStatsLogic-KickDialog-Title";

		
		const string KickPrompt = "Game-GameInfoStatsLogic-KickDialog-Text";

		
		const string KickAccept = "Game-GameInfoLogic-Kick-OkButton";

		
		const string KickVoteTooltip = "Game-GameInfoStatsLogic-VoteKickPlayerTooltip";

		const string VoteKickTitle = "Game-GameInfoStatsLogic-VoteKickDialog-Title";

		
		const string VoteKickPrompt = "Game-GameInfoStatsLogic-VoteKickDialog-Text";

		const string VoteKickPromptBreakBots = "Game-GameInfoStatsLogic-VoteKickDialog-BreakBotsText";
		const string VoteKickPromptBreakBot = "Game-GameInfoStatsLogic-VoteKickDialog-BreakBotText";

		const string RevealFactionHint = "Game-CA-GameInfoStatsLogic-RevealFactionHint";

		
		const string VoteKickVoteStart = "Game-GameInfoStatsLogic-VoteKick-StartVote";

		
		const string VoteKickVoteFor = "Game-GameInfoStatsLogic-VoteKick-VoteFor";

		
		const string VoteKickVoteAgainst = "Game-GameInfoStatsLogic-VoteKick-VoteAgainst";

		
		const string VoteKickVoteCancel = "Game-GameInfoStatsLogic-VoteKick-Abstain";

		[ObjectCreator.UseCtor]
		public GameInfoStatsLogicCA(Widget widget, ModData modData, World world,
			OrderManager orderManager, WorldRenderer worldRenderer, Action<bool> hideMenu, Action closeMenu)
		{
			var player = world.LocalPlayer;
			var playerPanel = widget.Get<ScrollPanelWidget>("PLAYER_LIST");
			var statsHeader = widget.Get("STATS_HEADERS");

			if (player != null && !player.NonCombatant)
			{
				var checkbox = widget.Get<CheckboxWidget>("STATS_CHECKBOX");
				var statusLabel = widget.Get<LabelWidget>("STATS_STATUS");

				checkbox.IsChecked = () => player.WinState != WinState.Undefined;
				checkbox.GetCheckmark = () => player.WinState == WinState.Won ? "tick" : "cross";

				if (player.HasObjectives)
				{
					var mo = player.PlayerActor.Trait<MissionObjectives>();
					checkbox.GetText = () => Game.Translate(mo.Objectives[0].Description);
				}

				var failed = Game.Translate(Failed);
				var inProgress = Game.Translate(InProgress);
				var accomplished = Game.Translate(Accomplished);
				statusLabel.GetText = () => player.WinState == WinState.Won ? accomplished :
					player.WinState == WinState.Lost ? failed : inProgress;
				statusLabel.GetColor = () => player.WinState == WinState.Won ? Color.LimeGreen :
					player.WinState == WinState.Lost ? Color.Red : Color.White;
			}
			else
			{
				// Expand the stats window to cover the hidden objectives
				var objectiveGroup = widget.Get("OBJECTIVE");

				objectiveGroup.Visible = false;
				statsHeader.Bounds.Y -= objectiveGroup.Bounds.Height;
				playerPanel.Bounds.Y -= objectiveGroup.Bounds.Height;
				playerPanel.Bounds.Height += objectiveGroup.Bounds.Height;
			}

			if (!orderManager.LobbyInfo.Clients.Any(c => !c.IsBot && c.Index != orderManager.LocalClient?.Index && c.State != Session.ClientState.Disconnected))
				statsHeader.Get<LabelWidget>("ACTIONS").Visible = false;

			var teamTemplate = playerPanel.Get<ScrollItemWidget>("TEAM_TEMPLATE");
			var playerTemplate = playerPanel.Get("PLAYER_TEMPLATE");
			var spectatorTemplate = playerPanel.Get("SPECTATOR_TEMPLATE");
			var unmuteTooltip = Game.Translate(Unmute);
			var muteTooltip = Game.Translate(Mute);
			var kickTooltip = Game.Translate(KickTooltip);
			var voteKickTooltip = Game.Translate(KickVoteTooltip);
			playerPanel.RemoveChildren();

			var teams = world.Players.Where(p => !p.NonCombatant && p.Playable)
				.Select(p => (Player: p, PlayerStatistics: p.PlayerActor.TraitOrDefault<PlayerStatistics>()))
				.OrderByDescending(p => p.PlayerStatistics?.Experience ?? 0)
				.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.Player.ClientIndex) ?? new Session.Client()).Team)
				.OrderByDescending(g => g.Sum(gg => gg.PlayerStatistics?.Experience ?? 0));

			void KickAction(Session.Client client, Func<bool> isVoteKick)
			{
				hideMenu(true);
				if (isVoteKick())
				{
					var botsCount = 0;
					if (client.IsAdmin)
						botsCount = world.Players.Count(p => p.IsBot && p.WinState == WinState.Undefined);
					var voteKickPrompt = botsCount == 1 ? VoteKickPromptBreakBot : VoteKickPromptBreakBots;

					if (UnitOrders.KickVoteTarget == null)
					{
						ConfirmationDialogs.ButtonPrompt(modData,
							title: VoteKickTitle,
							text: botsCount > 0 ? voteKickPrompt : VoteKickPrompt,
							titleArguments: new object[] { "player", client.Name },
							textArguments: new object[] { "bots", botsCount },
							onConfirm: () =>
							{
								orderManager.IssueOrder(Order.Command($"vote_kick {client.Index} {true}"));
								hideMenu(false);
								closeMenu();
							},
							confirmText: VoteKickVoteStart,
							onCancel: () => hideMenu(false));
						return;
					}

					ConfirmationDialogs.ButtonPrompt(modData,
						title: VoteKickTitle,
						text: botsCount > 0 ? voteKickPrompt : VoteKickPrompt,
						titleArguments: new object[] { "player", client.Name },
						textArguments: new object[] { "bots", botsCount },
						onConfirm: () =>
						{
							orderManager.IssueOrder(Order.Command($"vote_kick {client.Index} {true}"));
							hideMenu(false);
							closeMenu();
						},
						confirmText: VoteKickVoteFor,
						onCancel: () => hideMenu(false),
						cancelText: VoteKickVoteCancel,
						onOther: () =>
						{
							Ui.CloseWindow();
							orderManager.IssueOrder(Order.Command($"vote_kick {client.Index} {false}"));
							hideMenu(false);
							closeMenu();
						},
						otherText: VoteKickVoteAgainst);
				}
				else
				{
					ConfirmationDialogs.ButtonPrompt(modData,
						title: KickTitle,
						text: KickPrompt,
						titleArguments: new object[] { "player", client.Name },
						onConfirm: () =>
						{
							orderManager.IssueOrder(Order.Command($"kick {client.Index} {false}"));
							hideMenu(false);
						},
						confirmText: KickAccept,
						onCancel: () => hideMenu(false));
				}
			}

			var localClient = orderManager.LocalClient;
			var localPlayer = localClient == null ? null : world.Players.FirstOrDefault(player => player.ClientIndex == localClient.Index);
			bool LocalPlayerCanKick() => localClient != null
				&& (Game.IsHost || ((!orderManager.LocalClient.IsObserver) && localPlayer.WinState == WinState.Undefined));
			bool CanClientBeKicked(Session.Client client, Func<bool> isVoteKick) =>
				client.Index != localClient.Index && client.State != Session.ClientState.Disconnected
				&& (!client.IsAdmin || orderManager.LobbyInfo.GlobalSettings.Dedicated)
				&& (!isVoteKick() || UnitOrders.KickVoteTarget == null || UnitOrders.KickVoteTarget == client.Index);

			var revealedPlayersManager = player != null ? player.World.WorldActor.TraitOrDefault<RevealedPlayersManager>() : null;

			foreach (var t in teams)
			{
				if (teams.Count() > 1)
				{
					var teamHeader = ScrollItemWidget.Setup(teamTemplate, () => false, () => { });
					var team = t.Key > 0
						? Game.Translate(TeamNumber, "team", t.Key.ToString())
						: Game.Translate(NoTeam);
					teamHeader.Get<LabelWidget>("TEAM").GetText = () => team;
					var teamRating = teamHeader.Get<LabelWidget>("TEAM_SCORE");
					var scoreCache = new CachedTransform<int, string>(s => s.ToString());
					var teamMemberScores = t.Select(tt => tt.PlayerStatistics).Where(s => s != null).ToArray().Select(s => s.Experience);
					teamRating.GetText = () => scoreCache.Update(teamMemberScores.Sum());

					playerPanel.AddChild(teamHeader);
				}

				foreach (var p in t.ToList())
				{
					var pp = p.Player;
					var client = world.LobbyInfo.ClientWithIndex(pp.ClientIndex);
					var item = playerTemplate.Clone();
					LobbyUtils.SetupProfileWidget(item, client, orderManager, worldRenderer);

					var nameLabel = item.Get<LabelWidget>("NAME");
					WidgetUtils.BindPlayerNameAndStatus(nameLabel, pp);
					nameLabel.GetColor = () => pp.Color;

					// Begin custom CA section for revealing random factions
					var isRevealed = revealedPlayersManager != null && revealedPlayersManager.IsRevealed(pp);
					var factionAndLabel = item.Get<ContainerWithTooltipWidget>("FACTIONFLAGANDLABEL");
					var realFactionVisible = player == null || player.RelationshipWith(pp) == PlayerRelationship.Ally || player.WinState != WinState.Undefined || isRevealed;

					var flag = factionAndLabel.Get<ImageWidget>("FACTIONFLAG");
					flag.GetImageCollection = () => "flags";

					var tooltipTextSplit = SplitOnFirstToken(Game.Translate(pp.Faction.Description), "\n");
					var factionName = Game.Translate(pp.DisplayFaction.Name);

					if (realFactionVisible)
					{
						var resolvedFactionName = Game.Translate(pp.Faction.Name);
						flag.GetImageName = () => pp.Faction.InternalName;
						factionName = resolvedFactionName != factionName ? $"{factionName} ({resolvedFactionName})" : resolvedFactionName;
						factionAndLabel.GetTooltipText = () => tooltipTextSplit.First;
						factionAndLabel.GetTooltipDesc = () => tooltipTextSplit.Second;
					}
					else
					{
						flag.GetImageName = () => pp.DisplayFaction.InternalName;
						factionAndLabel.GetTooltipText = () => factionName;
						factionAndLabel.GetTooltipDesc = () => Game.Translate(RevealFactionHint);
					}

					var factionLabel = item.Get<LabelWidget>("FACTION");
					factionLabel.GetText = () => factionName;
					// End custom CA section for revealing random factions

					var scoreCache = new CachedTransform<int, string>(s => s.ToString());
					item.Get<LabelWidget>("SCORE").GetText = () => scoreCache.Update(p.PlayerStatistics?.Experience ?? 0);

					var muteCheckbox = item.Get<CheckboxWidget>("MUTE");
					muteCheckbox.IsChecked = () => TextNotificationsManager.MutedPlayers[pp.ClientIndex];
					muteCheckbox.OnClick = () => TextNotificationsManager.MutedPlayers[pp.ClientIndex] ^= true;
					muteCheckbox.IsVisible = () => !pp.IsBot && client.State != Session.ClientState.Disconnected && pp.ClientIndex != orderManager.LocalClient?.Index;
					muteCheckbox.GetTooltipText = () => muteCheckbox.IsChecked() ? unmuteTooltip : muteTooltip;

					var kickButton = item.Get<ButtonWidget>("KICK");
					bool IsVoteKick() => !Game.IsHost || pp.WinState == WinState.Undefined;
					kickButton.IsVisible = () => !pp.IsBot && LocalPlayerCanKick() && CanClientBeKicked(client, IsVoteKick);
					kickButton.OnClick = () => KickAction(client, IsVoteKick);
					kickButton.GetTooltipText = () => IsVoteKick() ? voteKickTooltip : kickTooltip;

					playerPanel.AddChild(item);
				}
			}

			var spectators = orderManager.LobbyInfo.Clients.Where(c => c.IsObserver).ToList();
			if (spectators.Count > 0)
			{
				var spectatorHeader = ScrollItemWidget.Setup(teamTemplate, () => false, () => { });
				var spectatorTeam = Game.Translate(Spectators);
				spectatorHeader.Get<LabelWidget>("TEAM").GetText = () => spectatorTeam;

				playerPanel.AddChild(spectatorHeader);

				foreach (var client in spectators)
				{
					var item = spectatorTemplate.Clone();
					LobbyUtils.SetupProfileWidget(item, client, orderManager, worldRenderer);

					var nameLabel = item.Get<LabelWidget>("NAME");
					var nameFont = Game.Renderer.Fonts[nameLabel.Font];

					var suffixLength = new CachedTransform<string, int>(s => nameFont.Measure(s).X);
					var name = new CachedTransform<(string Name, string Suffix), string>(c =>
						WidgetUtils.TruncateText(c.Name, nameLabel.Bounds.Width - suffixLength.Update(c.Suffix), nameFont) + c.Suffix);

					nameLabel.GetText = () =>
					{
						var suffix = client.State == Session.ClientState.Disconnected ? $" ({Game.Translate(Gone)})" : "";
						return name.Update((client.Name, suffix));
					};

					var kickButton = item.Get<ButtonWidget>("KICK");
					bool IsVoteKick() => !Game.IsHost;
					kickButton.IsVisible = () => Game.IsHost && client.Index != orderManager.LocalClient?.Index && client.State != Session.ClientState.Disconnected;
					kickButton.OnClick = () => KickAction(client, IsVoteKick);
					kickButton.GetTooltipText = () => IsVoteKick() ? voteKickTooltip : kickTooltip;

					var muteCheckbox = item.Get<CheckboxWidget>("MUTE");
					muteCheckbox.IsChecked = () => TextNotificationsManager.MutedPlayers[client.Index];
					muteCheckbox.OnClick = () => TextNotificationsManager.MutedPlayers[client.Index] ^= true;
					muteCheckbox.IsVisible = () => !client.IsBot && client.State != Session.ClientState.Disconnected && client.Index != orderManager.LocalClient?.Index;
					muteCheckbox.GetTooltipText = () => muteCheckbox.IsChecked() ? unmuteTooltip : muteTooltip;

					playerPanel.AddChild(item);
				}
			}
		}

		/// <summary>Splits a string into two parts on the first instance of a given token.</summary>
		static (string First, string Second) SplitOnFirstToken(string input, string token = "\\n")
		{
			if (string.IsNullOrEmpty(input))
				return (null, null);

			var split = input.IndexOf(token, StringComparison.Ordinal);
			var first = split > 0 ? input.Substring(0, split) : input;
			var second = split > 0 ? input.Substring(split + token.Length) : null;
			return (first, second);
		}
	}
}
