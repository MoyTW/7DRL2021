using Godot;
using SpaceDodgeRL.library;
using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.resources.gamedata;
using SpaceDodgeRL.scenes.components;
using SpaceDodgeRL.scenes.components.AI;
using SpaceDodgeRL.scenes.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SpaceDodgeRL.scenes.encounter.state {

  public static class EncounterStateBuilder {

    public static int ZONE_MIN_SIZE = 20;
    public static int ZONE_MAX_SIZE = 40;

    private static void InitializeMap(EncounterState state, int width, int height) {
      // Initialize the map with empty tiles
      state.MapWidth = width;
      state.MapHeight = height;
      state._encounterTiles = new EncounterTile[width, height];
      for (int x = 0; x < width; x++) {
        for (int y = 0; y < height; y++) {
          state._encounterTiles[x, y] = new EncounterTile();
        }
      }
    }

    private static Unit CreateAndDeployUnit(Random seededRand, EncounterState state, FactionName faction,
        Lane lane, int lanePosition, UnitOrder order, FormationType type, FormationFacing facing, int size,
        Func<int, Unit, Entity> entityFn, CommanderAIComponent commanderAI) {
      var center = lane.PositionFor(facing, lanePosition);
      var unit = new Unit(Guid.NewGuid().ToString(), faction, center, order, type, facing);
      state.AddUnit(unit);
      commanderAI.RegisterUnit(unit);
      lane.RegisterUnitAtPosition(unit, lanePosition);

      var width = (int)Math.Ceiling(Math.Sqrt(size));
      
      List<EncounterPosition> positions = new List<EncounterPosition>();
      for (int x = 0 - width; x < width; x++) {
        for (int y = 0 - width; y < width; y++) {
          positions.Add(new EncounterPosition(x, y));
        }
      }
      GameUtils.Shuffle(seededRand, positions);

      for (int i = 0; i < size; i++) {
        var entity = entityFn(i, unit);
        state.PlaceEntity(entity, new EncounterPosition(center.X + positions[i].X, center.Y + positions[i].Y));
        unit.RegisterBattleReadyEntity(entity);
      }

      return unit;
    }

    private static void RegisterRoutAtPercentage(CommanderAIComponent commanderAIComponent, Unit unit, float percentage) {
      var trigger = new OrderTrigger(OrderTriggerType.UNIT_BELOW_STRENGTH_PERCENT, false,
        watchedUnitIds: new List<string>() { unit.UnitId }, belowStrengthPercent: percentage);
      commanderAIComponent.RegisterTriggeredOrder(trigger, new Order(unit.UnitId, OrderType.ROUT));

      var laneClearTrigger = new OrderTrigger(OrderTriggerType.LANE_CLEAR_OF_UNITS_FROM_FACTION, false,
        watchedUnitIds: new List<string>() { unit.UnitId }, triggerFaction: unit.UnitFaction.Opposite());
      commanderAIComponent.RegisterTriggeredOrder(laneClearTrigger, new Order(unit.UnitId, OrderType.PREPARE_SWEEP_NEXT_LANE));
    }

    public static void AddPlayerToUnit(Entity player, Unit unit, int formationNumber) {
      var oldUnitComponent = player.GetComponent<UnitComponent>();
      if (oldUnitComponent != null) {
        player.RemoveComponent(oldUnitComponent);
      }
      player.AddComponent(UnitComponent.Create(unit.UnitId, formationNumber));
    }

    private static CommanderAIComponent CreateAndPlaceCommander(EncounterState state) {
      var commanderUnitId = "Invisible Commander AI Unit";
      var commander = EntityBuilder.CreateCommanderEntity(state.CurrentTick, FactionName.NEUTRAL);
      var commanderAI = commander.GetComponent<CommanderAIComponent>();
      state.PlaceEntity(commander, new EncounterPosition(1, 1)); // Invisible and unreachable but you CAN mouse over it, lol. it's fiiine

      // Commander unit is required for non-targeted commands to resolve properly
      var commanderUnit = new Unit(commanderUnitId, FactionName.NEUTRAL, commander.GetComponent<PositionComponent>().EncounterPosition,
        UnitOrder.REFORM, FormationType.LINE_20, FormationFacing.SOUTH);
      state.AddUnit(commanderUnit);
      commanderAI.RegisterUnit(commanderUnit);
      
      // Boilerplate conditions
      var playerRouted = new OrderTrigger(OrderTriggerType.ALL_UNITS_OF_FACTION_ROUTED, false, triggerFaction: FactionName.PLAYER);
      commanderAI.RegisterTriggeredOrder(playerRouted, new Order(commanderUnitId, OrderType.DECLARE_DEFEAT));
      var enemyRouted = new OrderTrigger(OrderTriggerType.ALL_UNITS_OF_FACTION_ROUTED, false, triggerFaction: FactionName.ENEMY);
      commanderAI.RegisterTriggeredOrder(enemyRouted, new Order(commanderUnitId, OrderType.DECLARE_VICTORY));
      
      return commanderAI;
    }

    private static Func<int, Unit, Entity> WrapWithPlayerFn(EncounterState state, Func<int, Unit, Entity> unitFn, int playerFormationNum) {
      return delegate(int formationNum, Unit unit) {
        if (formationNum == playerFormationNum) {
          AddPlayerToUnit(state.Player, unit, playerFormationNum);
          return state.Player;
        } else {
          return EntityBuilder.CreateHastatusEntity(state.CurrentTick, formationNum, unit, FactionName.PLAYER);
        }
      };
    }

    public static int ADVANCE_AT_TURN = 50;

    private static void PopulatePlayerFactionLane(int dungeonLevel, EncounterState state, Random seededRand,
        DeploymentInfo deploymentInfo, CommanderAIComponent commanderAI, Lane lane) {
      var numLines = seededRand.Next(3) + 1;
      // var numLines = 2;

      Unit hastatusUnit = null;
      if (numLines > 0) {
        var numHastati = seededRand.Next(80, 120);
        Func<int, Unit, Entity> hastatusFn = (formationNum, unit) => EntityBuilder.CreateHastatusEntity(state.CurrentTick, formationNum, unit, FactionName.PLAYER);
        if (lane.LaneIdx == deploymentInfo.NumLanes / 2) {
          hastatusFn = WrapWithPlayerFn(state, hastatusFn, numHastati-3);
          // hastatusFn = WrapWithPlayerFn(state, hastatusFn, 9);
        }
        hastatusUnit = CreateAndDeployUnit(seededRand, state, FactionName.PLAYER,
            lane, 1, UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
            deploymentInfo.PlayerFacing, 
            numHastati,
            hastatusFn, commanderAI);
        commanderAI.RegisterDeploymentOrder(20, new Order(hastatusUnit.UnitId, OrderType.OPEN_MANIPULE));
        commanderAI.RegisterDeploymentOrder(ADVANCE_AT_TURN, new Order(hastatusUnit.UnitId, OrderType.ADVANCE));
        RegisterRoutAtPercentage(commanderAI, hastatusUnit, .80f);
      }
      Unit princepsUnit = null;
      if (numLines > 1) {
        Func<int, Unit, Entity> princepsFn = (formationNum, unit) => EntityBuilder.CreatePrincepsEntity(state.CurrentTick, formationNum, unit, FactionName.PLAYER);
        princepsUnit = CreateAndDeployUnit(seededRand, state, FactionName.PLAYER,
            lane, 2, UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
            deploymentInfo.PlayerFacing,
            seededRand.Next(80, 120),
            // 20,
            princepsFn, commanderAI);
        commanderAI.RegisterDeploymentOrder(30, new Order(princepsUnit.UnitId, OrderType.OPEN_MANIPULE));
        commanderAI.RegisterTriggeredOrder(TriggeredOrder.AdvanceIfUnitRetreatsRoutsOrWithdraws(hastatusUnit, princepsUnit));
        RegisterRoutAtPercentage(commanderAI, princepsUnit, .60f);
      }
      Unit triariusUnit = null;
      if (numLines > 2) {
        Func<int, Unit, Entity> triariusFn = (formationNum, unit) => EntityBuilder.CreateTriariusEntity(state.CurrentTick, formationNum, unit, FactionName.PLAYER);
        triariusUnit = CreateAndDeployUnit(seededRand, state, FactionName.PLAYER,
            lane, 3, UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
            deploymentInfo.PlayerFacing, seededRand.Next(40, 60), triariusFn, commanderAI);
        commanderAI.RegisterDeploymentOrder(40, new Order(triariusUnit.UnitId, OrderType.OPEN_MANIPULE));
        commanderAI.RegisterTriggeredOrder(TriggeredOrder.AdvanceIfUnitRetreatsRoutsOrWithdraws(princepsUnit, triariusUnit));
        RegisterRoutAtPercentage(commanderAI, triariusUnit, .40f);
      }
    }

    private static Func<int, Unit, Entity> FirstLineEnemyFn(EncounterState state, Random seededRand) {
      if (seededRand.Next(2) == 0) {
        return (formationNum, unit) => EntityBuilder.CreateIberianLightInfantry(state.CurrentTick, formationNum, unit, FactionName.ENEMY);
      } else {
        return (formationNum, unit) => EntityBuilder.CreateGallicLightInfantry(state.CurrentTick, formationNum, unit, FactionName.ENEMY);
      }
    }

    private static Func<int, Unit, Entity> SecondLineEnemyFn(EncounterState state, Random seededRand) {
      if (seededRand.Next(2) == 0) {
        return (formationNum, unit) => EntityBuilder.CreateGallicVeteranInfantry(state.CurrentTick, formationNum, unit, FactionName.ENEMY);
      } else {
        return (formationNum, unit) => EntityBuilder.CreatePunicVeteranInfantry(state.CurrentTick, formationNum, unit, FactionName.ENEMY);
      }
    }

    private static Func<int, Unit, Entity> ThirdLineEnemyFn(EncounterState state, Random seededRand) {
      return (formationNum, unit) => EntityBuilder.CreatePunicHeavyInfantry(state.CurrentTick, formationNum, unit, FactionName.ENEMY);
    }

    private static void PopulateEnemyFactionLane(int dungeonLevel, EncounterState state, Random seededRand,
        DeploymentInfo deploymentInfo, CommanderAIComponent commanderAI, Lane lane) {
      var numLines = seededRand.Next(3) + 1;
      // var numLines = 1;

      Unit firstRankUnit = null;
      if (numLines > 0) {
        var enemyFn = FirstLineEnemyFn(state, seededRand);
        firstRankUnit = CreateAndDeployUnit(seededRand, state, FactionName.ENEMY,
            lane, 1, UnitOrder.REFORM, FormationType.LINE_20,
            deploymentInfo.EnemyFacing,
            seededRand.Next(80, 120),
            // 20,
            enemyFn, commanderAI);
        commanderAI.RegisterDeploymentOrder(ADVANCE_AT_TURN - 5, new Order(firstRankUnit.UnitId, OrderType.ADVANCE));
        RegisterRoutAtPercentage(commanderAI, firstRankUnit, .80f);
      }
      Unit secondRankUnit = null;
      if (numLines > 1) {
        var enemyFn = SecondLineEnemyFn(state, seededRand);
        secondRankUnit = CreateAndDeployUnit(seededRand, state, FactionName.ENEMY,
            lane, 2, UnitOrder.REFORM, FormationType.LINE_20,
            deploymentInfo.EnemyFacing,
            seededRand.Next(80, 120),
            // 20,
            enemyFn, commanderAI);
        commanderAI.RegisterTriggeredOrder(TriggeredOrder.AdvanceIfUnitRetreatsRoutsOrWithdraws(firstRankUnit, secondRankUnit));
        RegisterRoutAtPercentage(commanderAI, secondRankUnit, .60f);
      }
      Unit thirdRankUnit = null;
      if (numLines > 2) {
        var enemyFn = ThirdLineEnemyFn(state, seededRand);
        thirdRankUnit = CreateAndDeployUnit(seededRand, state, FactionName.ENEMY,
            lane, 3, UnitOrder.REFORM, FormationType.LINE_20,
            deploymentInfo.EnemyFacing, seededRand.Next(40, 60), enemyFn, commanderAI);
        commanderAI.RegisterTriggeredOrder(TriggeredOrder.AdvanceIfUnitRetreatsRoutsOrWithdraws(secondRankUnit, thirdRankUnit));
        RegisterRoutAtPercentage(commanderAI, thirdRankUnit, .40f);
      }
    }

    public static void PopulateStateForLevel(Entity player, int dungeonLevel, EncounterState state, Random seededRand,
        int width = 300, int height = 300, int maxZoneGenAttempts = 100) {
      InitializeMap(state, width, height);

      // Reset player's start
      // Heal the player
      player.GetComponent<PlayerComponent>().StartOfLevel = true;
      player.GetComponent<DefenderComponent>().RestoreHP(9999);

      var commanderAI = CreateAndPlaceCommander(state);

      var numLanes = seededRand.Next(4) + 1;
      var deploymentInfo = DeploymentInfo.Create(width, height, seededRand, numLanes);
      state.DeploymentInfo = deploymentInfo;

      foreach (var lane in deploymentInfo.Lanes) {
        PopulatePlayerFactionLane(dungeonLevel, state, seededRand, deploymentInfo, commanderAI, lane);
        PopulateEnemyFactionLane(dungeonLevel, state, seededRand, deploymentInfo, commanderAI, lane);
      }
    }
  }
}