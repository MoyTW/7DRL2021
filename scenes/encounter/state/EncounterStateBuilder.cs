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
      var unit = new Unit(Guid.NewGuid().ToString(), faction, center, order, type,
        facing, lane.IsOnFlank(faction, Flank.LEFT), lane.IsOnFlank(faction, Flank.RIGHT));
      state.AddUnit(unit);
      commanderAI.RegisterUnit(unit);

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
    }

    private static void AddPlayerToUnit(Entity player, Unit unit, int formationNumber) {
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
        UnitOrder.REFORM, FormationType.LINE_20, FormationFacing.SOUTH, true, true);
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

    class Lane {
      public DeploymentInfo Parent { get; private set; }
      public int LaneIdx { get; private set; }
      public EncounterPosition LaneCenter { get; private set; }

      public Lane(DeploymentInfo parent, int laneIdx, EncounterPosition laneCenter) {
        this.Parent = parent;
        this.LaneIdx = laneIdx;
        this.LaneCenter = laneCenter;
      }

      public bool IsOnFlank(FactionName faction, Flank flank) {
        if (faction == this.Parent.Attacker) {
          if (flank == Flank.LEFT && this.LaneIdx == 0) { return true; } // This works
          else if (flank == Flank.RIGHT && this.LaneIdx == this.Parent.NumLanes - 1) { return true; } // This doesn't!
        }
        else if (faction == this.Parent.Defender) {
          if (flank == Flank.RIGHT && this.LaneIdx == 0) { return true; } // This also works
          else if (flank == Flank.LEFT && this.LaneIdx == this.Parent.NumLanes - 1) { return true; }
        }
        return false;
      }

      // Line indices are "lower towards enemy" - 0th = skirmishers, first = hastatus, third = triarius, highest = reserves
      public EncounterPosition PositionFor(FormationFacing facing, int line, int interval=30) {
        // The facing has to be swapped in order for the position to work - an army FACING north should be DEPLOYED south
        return AIUtils.RotateAndProject(this.LaneCenter, 0, (-30 * line) - line / 2, facing.Opposite());
      }
    }

    class DeploymentInfo {
      public FactionName Attacker { get; private set; }
      public FactionName Defender { get; private set; }
      public FormationFacing AttackerFacing { get; private set; }
      public FormationFacing DefenderFacing { get; private set; }

      public FormationFacing PlayerFacing { get; private set; }
      public FormationFacing EnemyFacing { get; private set; }

      public EncounterPosition CenterPos { get; private set; }
      public int NumLanes { get; private set; }
      public List<Lane> Lanes { get; private set; }

      public DeploymentInfo(int width, int height, Random seededRand, int numLanes) {
        if (seededRand.Next(2) == 0) {
          this.Attacker = FactionName.PLAYER;
          this.Defender = FactionName.ENEMY;
        } else {
          this.Attacker = FactionName.ENEMY;
          this.Defender = FactionName.PLAYER;
        }
        this.AttackerFacing = (FormationFacing)seededRand.Next(4);
        this.DefenderFacing = this.AttackerFacing.Opposite();
        GD.Print("Attacking faction is: ", this.Attacker);

        if (this.Attacker == FactionName.PLAYER) {
          this.PlayerFacing = this.AttackerFacing;
          this.EnemyFacing = this.DefenderFacing;
        } else {
          this.PlayerFacing = this.DefenderFacing;
          this.EnemyFacing = this.AttackerFacing;
        }

        var interval = 20;
        this.CenterPos = new EncounterPosition(width / 2, height / 2);
        this.NumLanes = numLanes;
        this.Lanes = new List<Lane>();
        var leftX = -(Mathf.FloorToInt(this.NumLanes / 2) * interval);
        if (numLanes % 2 == 0) {
          leftX = -(this.NumLanes / 2 * interval) + (interval / 2);
        }
        for (int i = 0; i < this.NumLanes; i++) {
          var laneX = leftX + (i * interval);
          // Lanes are laid out from the perspective of the attacker
          var laneCenterPos = AIUtils.RotateAndProject(this.CenterPos, laneX, 0, this.AttackerFacing);
          this.Lanes.Add(new Lane(this, i, laneCenterPos));
        }
      }
    }

    public static void PopulateStateForLevel(Entity player, int dungeonLevel, EncounterState state, Random seededRand,
        int width = 300, int height = 300, int maxZoneGenAttempts = 100) {
      InitializeMap(state, width, height);

      // Add the player to the map
      var centerPos = new EncounterPosition(width / 2, height / 2);
      // state.PlaceEntity(player, playerPos);

      Func<int, Unit, Entity> hastatusFn = (formationNum, unit) => EntityBuilder.CreateHastatusEntity(state.CurrentTick, formationNum, unit, FactionName.PLAYER);
      Func<int, Unit, Entity> princepsFn = (formationNum, unit) => EntityBuilder.CreatePrincepsEntity(state.CurrentTick, formationNum, unit, FactionName.PLAYER);
      Func<int, Unit, Entity> hastatusWithPlayerFn = WrapWithPlayerFn(state, hastatusFn, 6);
      Func<int, Unit, Entity> secondHastatusFn = (formationNum, unit) => EntityBuilder.CreateHastatusEntity(state.CurrentTick, formationNum, unit, FactionName.PLAYER, startingMorale: 100);
      Func<int, Unit, Entity> iberianLightInfantryFn = (formationNum, unit) => EntityBuilder.CreateIberianLightInfantry(state.CurrentTick, formationNum, unit, FactionName.ENEMY);

      var commanderAI = CreateAndPlaceCommander(state);

      var deploymentInfo = new DeploymentInfo(width, height, seededRand, 2);
      
      // Attacker forces
      int playerLane = deploymentInfo.NumLanes / 2;
      int i = 0;
      foreach (var lane in deploymentInfo.Lanes) {
        // Player units
        if (i == playerLane) {
          var pUnit = CreateAndDeployUnit(seededRand, state, FactionName.PLAYER,
            lane, 1, UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
            deploymentInfo.PlayerFacing, 123, hastatusWithPlayerFn, commanderAI);
          commanderAI.RegisterDeploymentOrder(20, new Order(pUnit.UnitId, OrderType.ADVANCE));
          commanderAI.RegisterDeploymentOrder(30, new Order(pUnit.UnitId, OrderType.OPEN_MANIPULE));
          commanderAI.RegisterDeploymentOrder(50, new Order(pUnit.UnitId, OrderType.ADVANCE));
          RegisterRoutAtPercentage(commanderAI, pUnit, .80f);
        } else {
          var pUnit = CreateAndDeployUnit(seededRand, state, FactionName.PLAYER,
            lane, 1, UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
            deploymentInfo.PlayerFacing, 123, hastatusFn, commanderAI);
          commanderAI.RegisterDeploymentOrder(20, new Order(pUnit.UnitId, OrderType.ADVANCE));
          commanderAI.RegisterDeploymentOrder(30, new Order(pUnit.UnitId, OrderType.OPEN_MANIPULE));
          commanderAI.RegisterDeploymentOrder(50, new Order(pUnit.UnitId, OrderType.ADVANCE));
          RegisterRoutAtPercentage(commanderAI, pUnit, .80f);

          /* var secondLine = CreateAndDeployUnit(seededRand, state, FactionName.PLAYER,
            lane, 2, UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
            deploymentInfo.PlayerFacing, 97, princepsFn, commanderAI);
          commanderAI.RegisterDeploymentOrder(20, new Order(secondLine.UnitId, OrderType.ADVANCE));
          commanderAI.RegisterDeploymentOrder(30, new Order(secondLine.UnitId, OrderType.OPEN_MANIPULE));
          commanderAI.RegisterDeploymentOrder(50, new Order(secondLine.UnitId, OrderType.ADVANCE));
          RegisterRoutAtPercentage(commanderAI, secondLine, .80f); */
        }
        i++;

        var eUnit = CreateAndDeployUnit(seededRand, state, FactionName.ENEMY,
          lane, 1, UnitOrder.REFORM, FormationType.LINE_20,
          deploymentInfo.EnemyFacing, 95, iberianLightInfantryFn, commanderAI);
        commanderAI.RegisterDeploymentOrder(35, new Order(eUnit.UnitId, OrderType.ADVANCE));
        RegisterRoutAtPercentage(commanderAI, eUnit, .80f);
      }
    }
  }
}