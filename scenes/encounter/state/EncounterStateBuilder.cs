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

    private static Unit CreateAndDeployUnit(Random seededRand, EncounterState state, string unitId, FactionName faction,
        EncounterPosition center, UnitOrder order, FormationType type, FormationFacing facing, int size,
        Func<int, Unit, Entity> entityFn, bool leftFlank, bool rightFlank, CommanderAIComponent commanderAI) {
      var unit = new Unit(unitId, faction, center, order, type, facing, leftFlank, rightFlank);
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
      public EncounterPosition LaneCenter { get; private set; }

      public Lane(EncounterPosition laneCenter) {
        this.LaneCenter = laneCenter;
      }

      // Line indices are "lower towards enemy" - 0th = skirmishers, first = hastatus, third = triarius, highest = reserves
      public EncounterPosition PositionFor(FormationFacing facing, int line, int interval=30) {
        // The facing has to be swapped in order for the position to work - an army FACING north should be DEPLOYED south
        return AIUtils.RotateAndProject(this.LaneCenter, 0, -30 * line, facing.Opposite());
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
          var laneCenterPos = AIUtils.RotateAndProject(this.CenterPos, laneX, 0, this.AttackerFacing);
          GD.Print(String.Format("Lane i={0}, laneX={1}, laneCenter={2} & leftX={3}, facing={4}", i, laneX, laneCenterPos, leftX, this.AttackerFacing));
          this.Lanes.Add(new Lane(laneCenterPos));
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
          var pUnit = CreateAndDeployUnit(seededRand, state, Guid.NewGuid().ToString(), FactionName.PLAYER,
            lane.PositionFor(deploymentInfo.PlayerFacing, 1), UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
            deploymentInfo.PlayerFacing, 123, hastatusWithPlayerFn, leftFlank: false, rightFlank: false, commanderAI);
          commanderAI.RegisterDeploymentOrder(20, new Order(pUnit.UnitId, OrderType.ADVANCE));
          commanderAI.RegisterDeploymentOrder(30, new Order(pUnit.UnitId, OrderType.OPEN_MANIPULE));
          commanderAI.RegisterDeploymentOrder(50, new Order(pUnit.UnitId, OrderType.ADVANCE));
          RegisterRoutAtPercentage(commanderAI, pUnit, .80f);
        } else {
          var pUnit = CreateAndDeployUnit(seededRand, state, Guid.NewGuid().ToString(), FactionName.PLAYER,
          lane.PositionFor(deploymentInfo.PlayerFacing, 1), UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
          deploymentInfo.PlayerFacing, 123, hastatusFn, leftFlank: false, rightFlank: false, commanderAI);
          commanderAI.RegisterDeploymentOrder(20, new Order(pUnit.UnitId, OrderType.ADVANCE));
          commanderAI.RegisterDeploymentOrder(30, new Order(pUnit.UnitId, OrderType.OPEN_MANIPULE));
          commanderAI.RegisterDeploymentOrder(50, new Order(pUnit.UnitId, OrderType.ADVANCE));
          RegisterRoutAtPercentage(commanderAI, pUnit, .80f);
        }
        i++;

        var eUnit = CreateAndDeployUnit(seededRand, state, Guid.NewGuid().ToString(), FactionName.ENEMY,
          lane.PositionFor(deploymentInfo.EnemyFacing, 1), UnitOrder.REFORM, FormationType.LINE_20,
          deploymentInfo.EnemyFacing, 95, iberianLightInfantryFn, leftFlank: false, rightFlank: false, commanderAI);
        commanderAI.RegisterDeploymentOrder(35, new Order(eUnit.UnitId, OrderType.ADVANCE));
        RegisterRoutAtPercentage(commanderAI, eUnit, .80f);
      }



      /* var pCenter = CreateAndDeployUnit(seededRand, state, "test player center", FactionName.PLAYER,
        new EncounterPosition(centerPos.X, centerPos.Y - 15), UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
        FormationFacing.SOUTH, 123, hastatusWithPlayerFn, leftFlank: false, rightFlank: false, commanderAI);
      commanderAI.RegisterDeploymentOrder(20, new Order(pCenter.UnitId, OrderType.ADVANCE));
      commanderAI.RegisterDeploymentOrder(30, new Order(pCenter.UnitId, OrderType.OPEN_MANIPULE));
      commanderAI.RegisterDeploymentOrder(50, new Order(pCenter.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(commanderAI, pCenter, .80f);

       var pLeft = CreateAndDeployUnit(seededRand, state, "test player left", FactionName.PLAYER,
        new EncounterPosition(centerPos.X + 20, centerPos.Y - 15), UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
        FormationFacing.SOUTH, 98, hastatusFn, leftFlank: true, rightFlank: false, commanderAI);
      commanderAI.RegisterDeploymentOrder(10, new Order(pLeft.UnitId, OrderType.ADVANCE));
      commanderAI.RegisterDeploymentOrder(20, new Order(pLeft.UnitId, OrderType.OPEN_MANIPULE));
      commanderAI.RegisterDeploymentOrder(50, new Order(pLeft.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(commanderAI, pLeft, .80f);

      var pRight = CreateAndDeployUnit(seededRand, state, "test player right", FactionName.PLAYER,
        new EncounterPosition(centerPos.X - 20, centerPos.Y - 15), UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
        FormationFacing.SOUTH, 107, hastatusFn, leftFlank: false, rightFlank: true, commanderAI);
      commanderAI.RegisterDeploymentOrder(15, new Order(pRight.UnitId, OrderType.ADVANCE));
      commanderAI.RegisterDeploymentOrder(25, new Order(pRight.UnitId, OrderType.OPEN_MANIPULE));
      commanderAI.RegisterDeploymentOrder(50, new Order(pRight.UnitId, OrderType.ADVANCE));
      var pRightBreakTrigger = new OrderTrigger(OrderTriggerType.UNIT_BELOW_STRENGTH_PERCENT, false,
        watchedUnitIds: new List<string>() { pRight.UnitId }, belowStrengthPercent: 80);
      RegisterRoutAtPercentage(commanderAI, pRight, .80f); */

      /* var p2ndCenter = CreateAndDeployUnit(seededRand, state, "test player 2nd center", FactionName.PLAYER,
        new EncounterPosition(centerPos.X, centerPos.Y - 35), UnitOrder.REFORM, FormationType.MANIPULE_OPENED,
        FormationFacing.SOUTH, 58, secondHastatusFn, leftFlank: false, rightFlank: false, commanderAI);
      var p2ndCenterAdvTrigger = new OrderTrigger(OrderTriggerType.UNIT_HAS_STANDING_ORDER, false,
        watchedUnitIds: new List<string>() { pCenter.UnitId, pLeft.UnitId, pRight.UnitId },
        awaitedStandingOrders: new List<UnitOrder>() { UnitOrder.RETREAT });
      commanderAI.RegisterTriggeredOrder(p2ndCenterAdvTrigger, new Order(p2ndCenter.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(commanderAI, p2ndCenter, .50f);

      var p2ndLeft = CreateAndDeployUnit(seededRand, state, "test player 2nd left", FactionName.PLAYER,
        new EncounterPosition(centerPos.X + 20, centerPos.Y - 35), UnitOrder.REFORM, FormationType.MANIPULE_OPENED,
        FormationFacing.SOUTH, 70, secondHastatusFn, leftFlank: true, rightFlank: false, commanderAI);
      var p2ndLeftAdvTrigger = new OrderTrigger(OrderTriggerType.UNIT_HAS_STANDING_ORDER, false,
        watchedUnitIds: new List<string>() { pCenter.UnitId, pLeft.UnitId, pRight.UnitId },
        awaitedStandingOrders: new List<UnitOrder>() { UnitOrder.RETREAT });
      commanderAI.RegisterTriggeredOrder(p2ndLeftAdvTrigger, new Order(p2ndLeft.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(commanderAI, p2ndLeft, .50f);

      var p2ndRight = CreateAndDeployUnit(seededRand, state, "test player 2nd right", FactionName.PLAYER,
        new EncounterPosition(centerPos.X - 20, centerPos.Y - 35), UnitOrder.REFORM, FormationType.MANIPULE_OPENED,
        FormationFacing.SOUTH, 63, secondHastatusFn, leftFlank: false, rightFlank: true, commanderAI);
      var p2ndRightAdvTrigger = new OrderTrigger(OrderTriggerType.UNIT_HAS_STANDING_ORDER, false,
        watchedUnitIds: new List<string>() { pCenter.UnitId, pLeft.UnitId, pRight.UnitId },
        awaitedStandingOrders: new List<UnitOrder>() { UnitOrder.RETREAT });
      commanderAI.RegisterTriggeredOrder(p2ndRightAdvTrigger, new Order(p2ndRight.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(commanderAI, p2ndRight, .50f); */
      
      // Enemy deployment
      /* var eCenter = CreateAndDeployUnit(seededRand, state, "test enemy center", FactionName.ENEMY,
        new EncounterPosition(centerPos.X, centerPos.Y + 40), UnitOrder.REFORM, FormationType.LINE_20,
        FormationFacing.NORTH, 95, iberianLightInfantryFn, leftFlank: false, rightFlank: false, commanderAI);
      commanderAI.RegisterDeploymentOrder(35, new Order(eCenter.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(commanderAI, eCenter, .80f);

       var eLeft = CreateAndDeployUnit(seededRand, state, "test enemy left", FactionName.ENEMY,
        new EncounterPosition(centerPos.X - 20, centerPos.Y + 40), UnitOrder.REFORM, FormationType.LINE_20,
        FormationFacing.NORTH, 99, iberianLightInfantryFn, leftFlank: true, rightFlank: false, commanderAI);
      commanderAI.RegisterDeploymentOrder(35, new Order(eLeft.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(commanderAI, eLeft, .80f);
      
      var eRight = CreateAndDeployUnit(seededRand, state, "test enemy right", FactionName.ENEMY,
        new EncounterPosition(centerPos.X + 20, centerPos.Y + 40), UnitOrder.REFORM, FormationType.LINE_20,
        FormationFacing.NORTH, 90, iberianLightInfantryFn, leftFlank: false, rightFlank: true, commanderAI);
      commanderAI.RegisterDeploymentOrder(35, new Order(eRight.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(commanderAI, eRight, .80f);  */
      
      /*
      var nextToPlayer = new EncounterPosition(zones[playerZoneIdx].Center.X + 2, zones[playerZoneIdx].Center.Y + 1);
      state.PlaceEntity(EntityBuilder.CreateItemByEntityDefId(EntityDefId.ITEM_RED_PAINT), nextToPlayer);
      nextToPlayer = new EncounterPosition(zones[playerZoneIdx].Center.X + 1, zones[playerZoneIdx].Center.Y + 1);
      state.PlaceEntity(EntityBuilder.CreateItemByEntityDefId(EntityDefId.ITEM_EMP), nextToPlayer);
      for (int i = 0; i < 26; i++) {
        nextToPlayer = new EncounterPosition(zones[playerZoneIdx].Center.X + i, zones[playerZoneIdx].Center.Y + 3);
        state.PlaceEntity(EntityBuilder.CreateItemByEntityDefId(EntityDefId.ITEM_EXTRA_BATTERY), nextToPlayer);
      }
      */
    }
  }
}