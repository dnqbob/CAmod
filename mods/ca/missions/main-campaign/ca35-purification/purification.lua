MissionDir = "ca|missions/main-campaign/ca35-purification"

UnitBuildTimeMultipliers = {
	easy = 0.8,
	normal = 0.5,
	hard = 0.33,
	vhard = 0.25,
	brutal = 0.2
}

LiquidTibCooldown = DateTime.Minutes(5)

RiftEnabledTime = {
	easy = DateTime.Seconds((60 * 45) + 17),
	normal = DateTime.Seconds((60 * 30) + 17),
	hard = DateTime.Seconds((60 * 15) + 17),
	vhard = DateTime.Seconds((60 * 15) + 17),
	brutal = DateTime.Seconds((60 * 15) + 17)
}

ScrinReinforcementSpawns = {
	ScrinReinforcementsSpawn1, ScrinReinforcementsSpawn2, ScrinReinforcementsSpawn3, ScrinReinforcementsSpawn4, ScrinReinforcementsSpawn5, ScrinReinforcementsSpawn6, ScrinReinforcementsSpawn7, ScrinReinforcementsSpawn8
}

AdjustedScrinCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin)

Squads = {
	ScrinMain = {
		InitTimeAdjustment = -DateTime.Minutes(10),
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 25, Max = 55 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { Portal1, Portal2 }, Vehicles = { WarpSphere1, WarpSphere2 }, Aircraft = { GravityStabilizer1, GravityStabilizer2 } },
		ProducerTypes = { Infantry = { "port", "wormhole" }, Vehicles = { "wsph", "wormhole" }, Aircraft = { "grav", "hiddenspawner" } },
		Compositions = AdjustedScrinCompositions,
		AttackPaths = {
			{ ScrinAttack1a.Location, ScrinAttack1b.Location, ScrinAttack1c.Location, ScrinAttack1d.Location },
			{ ScrinAttack1a.Location, ScrinAttack2.Location },
			{ ScrinAttack3a.Location, ScrinAttack3b.Location, ScrinAttack3c.Location },
			{ ScrinAttack3a.Location, ScrinAttack4a.Location, ScrinAttack4b.Location, ScrinAttack3c.Location },
		},
	},
	ScrinSecondary = {
		InitTimeAdjustment = -DateTime.Minutes(10),
		Delay = AdjustDelayForDifficulty(DateTime.Seconds(170)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 15, Max = 25 }),
		FollowLeader = true,
		ProducerActors = { Infantry = { Portal1, Portal2 }, Vehicles = { WarpSphere1, WarpSphere2 }, Aircraft = { GravityStabilizer1, GravityStabilizer2 } },
		ProducerTypes = { Infantry = { "port", "wormhole" }, Vehicles = { "wsph", "wormhole" }, Aircraft = { "grav", "hiddenspawner" } },
		Compositions = AdjustedScrinCompositions,
		AttackPaths = {
			{ ScrinAttack1a.Location, ScrinAttack1b.Location, ScrinAttack1c.Location, ScrinAttack1d.Location },
			{ ScrinAttack1a.Location, ScrinAttack2.Location },
			{ ScrinAttack3a.Location, ScrinAttack3b.Location, ScrinAttack3c.Location },
			{ ScrinAttack3a.Location, ScrinAttack4a.Location, ScrinAttack4b.Location, ScrinAttack3c.Location },
		},
	},
	ScrinAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(5)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Scrin
	},
	ScrinRebelsMain = {
		AttackValuePerSecond = { Min = 70, Max = 70 },
		FollowLeader = true,
		ProducerTypes = { Infantry = { "wormhole" }, Vehicles = { "wormhole" }, Aircraft = { "grav" } },
		Compositions = AdjustedScrinCompositions,
		AttackPaths = {
			{ ScrinBaseCenter.Location },
		},
	},
	TibTruckKillers = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(5)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = {
			{ Aircraft = { "stmr", "stmr", "stmr" } },
			{ Aircraft = { "torm", "torm", "torm" } },
			{ Aircraft = { "enrv", "enrv" } },
		}
	}
}

SetupPlayers = function()
	Nod = Player.GetPlayer("Nod")
	Scrin = Player.GetPlayer("Scrin")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { Nod }
	MissionEnemies = { Scrin }
end

WorldLoaded = function()
	SetupPlayers()

	ShipmentsComplete = 0
	TimerTicks = DateTime.Seconds(60)
	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	AdjustPlayerStartingCashForDifficulty()
	InitScrin()

	ObjectiveChargeDevice = Nod.AddObjective("Lua-purification-bring-the-device")
	ObjectiveProtectLiquidTib = Nod.AddObjective("Lua-purification-protect-liquid-tiberium")

	UpdateMissionText()

	if Difficulty == "easy" then
		NormalHardOnlyCarrier1.Destroy()
		NormalHardOnlyCarrier2.Destroy()
	end

	if IsNormalOrBelow() then
		HardOnlyCarrier1.Destroy()
	end

	Trigger.OnKilled(LiquidTibFacility, function(self, killer)
		if not Nod.IsObjectiveCompleted(ObjectiveProtectLiquidTib) then
			Nod.MarkFailedObjective(ObjectiveProtectLiquidTib)
		end
	end)

	Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
		Media.DisplayMessage("Lua-purification-commander-we-must", "Kane", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound(MissionDir .. "/kane_liquidt.aud", 2)
		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(18)), function()
			Tip("Lua-purification-move-a-tanker")
			Utils.Do({ InitAttacker1, InitAttacker2, InitAttacker3, InitAttacker4 }, function(a)
				if not a.IsDead then
					a.AttackMove(PlayerStart.Location)
				end
			end)
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				Utils.Do({ InitAttacker5, InitAttacker6, InitAttacker7 }, function(a)
					if not a.IsDead then
						a.AttackMove(PlayerStart.Location)
					end
				end)
			end)
		end)
	end)

	Trigger.OnEnteredFootprint({ LiquidTibPickup1.Location, LiquidTibPickup2.Location }, function(a)
		if IsMissionPlayer(a.Owner) and not a.IsDead and a.Type == "ttrk" then
			if not LiquidTibFacility.IsDead and LiquidTibFacility.AmmoCount("primary") == 0 then
				Notification("Lua-purification-no-liquid-tiberium")
			end
		end
	end)

	Trigger.OnEnteredFootprint({ CaveEntrance.Location, LiquidTibDropOff1.Location, LiquidTibDropOff2.Location, LiquidTibDropOff3.Location }, function(a)
		if IsMissionPlayer(a.Owner) and not a.IsDead and a.Type == "ttrk" then
			if a.AmmoCount("primary") == 1 then
				a.Reload("primary", -1)
				ShipmentsComplete = ShipmentsComplete + 1
				Notification("Lua-purification-liquid-tiberium-shipment")
				MediaCA.PlaySound(MissionDir .. "/n_liquidtibdelivered.aud", 2)
				UpdateMissionText()
				if ShipmentsComplete == 5 then
					PurificationWave()
					Nod.MarkCompletedObjective(ObjectiveChargeDevice)
					Nod.MarkCompletedObjective(ObjectiveProtectLiquidTib)
				end
			else
				Notification("Lua-purification-no-liquid-tiberium2")
			end
		end
	end)

	AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
	AfterTick()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
				LiquidTibProduced()
			end
			UpdateMissionText()
		end

		if not LiquidTibFacility.IsDead and LiquidTibFacility.AmmoCount("primary") > 0 then
			local nearbyTrucks = Map.ActorsInBox(LiquidTibPickup1.CenterPosition, LiquidTibPickup2.CenterPosition, function(a)
				return IsMissionPlayer(a.Owner) and not a.IsDead and a.Type == "ttrk"
			end)

			Utils.Do(nearbyTrucks, function(t)
				if t.AmmoCount("primary") == 0 and not TibLoaded then
					TibLoaded = true
					t.Reload("primary", 1)
					LiquidTibFacility.Reload("primary", -1)
					Notification("Lua-purification-liquid-tiberium-transfer")
					Beacon.New(Nod, t.CenterPosition)
				end
			end)

			TibLoaded = false
		end

		if ObjectiveDestroyRemainingLoyalists ~= nil then
			if not PlayerHasBuildings(Scrin) and #Scrin.GetActorsByType("wormhole") == 0 then
				Nod.MarkCompletedObjective(ObjectiveDestroyRemainingLoyalists)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()

		if not ScrinReinforcementsEnabled then
			local scrinProducerActors = Scrin.GetActorsByTypes({ "sfac", "wsph", "port" })

			if #scrinProducerActors == 0 then
				InitScrinReinforcements()
			end
		end

		if MissionPlayersHaveNoRequiredUnits() then
			if not Nod.IsObjectiveCompleted(ObjectiveChargeDevice) then
				Nod.MarkFailedObjective(ObjectiveChargeDevice)
			end
			if ObjectiveDestroyRemainingLoyalists ~= nil and not Nod.IsObjectiveCompleted(ObjectiveDestroyRemainingLoyalists) then
				Nod.MarkFailedObjective(ObjectiveDestroyRemainingLoyalists)
			end
		end
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % DateTime.Seconds(30) == 0 then
		CalculatePlayerCharacteristics()
	end
end

InitScrin = function()
	RebuildExcludes.Scrin = { Types = { "rfgn" } }

	AutoRepairAndRebuildBuildings(Scrin, 15)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)
	AutoRebuildConyards(Scrin)
	InitAiUpgrades(Scrin)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()

	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)

	BeginScrinAttacks()

	Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Scrin })

	Trigger.AfterDelay(RiftEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = Scrin })
	end)
end

BeginScrinAttacks = function()
	InitAttackSquad(Squads.ScrinMain, Scrin)
	InitAttackSquad(Squads.ScrinSecondary, Scrin)
	InitAirAttackSquad(Squads.ScrinAir, Scrin)
	if Difficulty == "brutal" then
		InitAirAttackSquad(Squads.TibTruckKillers, Scrin, MissionPlayers, { "ttrk" })
	end
end

UpdateMissionText = function()
	if Nod.IsObjectiveCompleted(ObjectiveChargeDevice) then
		UserInterface.SetMissionText("")
		return
	end

		UserInterface.SetMissionTextWithArgs("Lua-purification-shipments-progress", { tostring(ShipmentsComplete), UtilsCA.FormatTimeForGameSpeed(TimerTicks) }, HSLColor.Yellow)

LiquidTibProduced = function()
	if Nod.IsObjectiveCompleted(ObjectiveChargeDevice) then
		return
	end

	TimerTicks = LiquidTibCooldown
	Notification("Lua-purification-liquid-tiberium-shipment2")
	MediaCA.PlaySound(MissionDir .. "/n_liquidtibready.aud", 2)

	if not LiquidTibFacility.IsDead then
		LiquidTibFacility.Reload("primary", 1)
		Beacon.New(Nod, LiquidTibFacility.CenterPosition)
	end
end

PurificationWave = function()
	ObjectivePurify = Nod.AddObjective("Lua-purification-await-the-purification")

	Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(2)), function()
		Media.DisplayMessage("Lua-purification-well-done-commander", "Kane", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound(MissionDir .. "/kane_purification.aud", 2)

		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(14)), function()
			MediaCA.PlaySound("purification.aud", 2)
			Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(9)), function()
				PurificationComplete = true
				Lighting.Flash("Purification", AdjustTimeForGameSpeed(10))
				ObjectiveDestroyRemainingLoyalists = Nod.AddObjective("Lua-purification-eliminate-any-hostile")
				Nod.MarkCompletedObjective(ObjectivePurify)
				PurifyScrin()
				InitScrinReinforcements()
				InitRebelReinforcements()
				if not IslandGrav1.IsDead then
					IslandGrav1.Kill()
				end
				if not IslandGrav2.IsDead then
					IslandGrav2.Kill()
				end
				Trigger.AfterDelay(1, function()
					local wormholes = Scrin.GetActorsByType("wormhole")
					Utils.Do(wormholes, function(w)
						if not w.IsDead then
							w.GrantCondition("regen-disabled")
						end
					end)
				end)
				Trigger.AfterDelay(AdjustTimeForGameSpeed(4), function()
					Lighting.Flash("Purification", AdjustTimeForGameSpeed(10))
					Trigger.AfterDelay(AdjustTimeForGameSpeed(4), function()
						Lighting.Flash("Purification", AdjustTimeForGameSpeed(10))
						Trigger.AfterDelay(AdjustTimeForGameSpeed(4), function()
							Lighting.Flash("Purification", AdjustTimeForGameSpeed(10))
						end)
					end)
				end)
			end)
		end)
	end)
end

PurifyScrin = function()
	local scrinGroundUnits = Utils.Shuffle(Utils.Where(Scrin.GetGroundAttackers(), IsScrinGroundHunterUnit))
	local count = 1

	Utils.Do(scrinGroundUnits, function(a)
		Purify(a, count)
		count = count + 1
		if count > 10 then
			count = 1
		end
	end)

	local scrinAirUnits = Scrin.GetActorsByTypes({ "pac", "deva" })
	count = 1

	Utils.Do(scrinAirUnits, function(a)
		Purify(a, count)
		count = count + 1
		if count > 10 then
			count = 1
		end
	end)
end

Purify = function(a, count)
	Trigger.ClearAll(a)
	local shouldConvert = false

	if Difficulty == "easy" and count % 3 > 0 then
		shouldConvert = true
	elseif count % 2 == 0 then
		shouldConvert = true
	end

	if shouldConvert then
		a.Owner = ScrinRebels
	end
	Trigger.AfterDelay(1, function()
		IdleHunt(a)
	end)
end

InitScrinReinforcements = function()
	if not ScrinReinforcementsEnabled then
		ScrinReinforcementsEnabled = true
		Utils.Do(ScrinReinforcementSpawns, function(s)
			SpawnWormhole(s.Location)
		end)
		Utils.Do({ ScrinHiddenSpawn1.Location, ScrinHiddenSpawn2.Location, ScrinHiddenSpawn3.Location, ScrinHiddenSpawn4.Location, ScrinHiddenSpawn5.Location, ScrinHiddenSpawn6.Location }, function(loc)
			Actor.Create("hiddenspawner", true, { Owner = Scrin, Location = loc })
		end)
	end
end

SpawnWormhole = function(loc)
	local wormhole = Actor.Create("wormhole", true, { Owner = Scrin, Location = loc })
	Trigger.OnKilled(wormhole, function(self, killer)
		Trigger.AfterDelay(DateTime.Minutes(1), function()
			if not Nod.IsObjectiveCompleted(ObjectiveChargeDevice) then
				SpawnWormhole(loc)
			end
		end)
	end)
end

InitRebelReinforcements = function()
	local rebelSpawns = { RebelSpawnPoint1, RebelSpawnPoint2 }

	Utils.Do(rebelSpawns, function(s)
		local wormhole = Actor.Create("wormhole", true, { Owner = ScrinRebels, Location = s.Location })
		local units = Reinforcements.Reinforce(ScrinRebels, { "s1", "s3", "intl.ai2", "devo", "s4", "s1", "tpod", "gscr", "intl", "s1", "s1" }, { wormhole.Location }, 10)

		Utils.Do(units, function(a)
			a.AttackMove(ScrinBaseCenter.Location)
			IdleHunt(a)
		end)
	end)

	InitAttackSquad(Squads.ScrinRebelsMain, ScrinRebels, Scrin)
end
