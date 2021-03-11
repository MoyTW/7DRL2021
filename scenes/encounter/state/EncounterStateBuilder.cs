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
        Func<int, Unit, Entity> entityFn, bool leftFlank, bool rightFlank, Entity commander) {
      var unit = new Unit(unitId, faction, center, order, type, facing, leftFlank, rightFlank);
      state.AddUnit(unit);
      commander.GetComponent<CommanderAIComponent>().RegisterUnit(unit);

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

    public static void PopulateStateForLevel(Entity player, int dungeonLevel, EncounterState state, Random seededRand,
        int width = 300, int height = 300, int maxZoneGenAttempts = 100) {
      InitializeMap(state, width, height);

      // Add the player to the map
      var centerPos = new EncounterPosition(width / 2, height / 2);
      // state.PlaceEntity(player, playerPos);

      Func<int, Unit, Entity> hastatusWithPlayerFn = delegate(int formationNum, Unit unit) {
        if (formationNum == 6) {
          AddPlayerToUnit(player, unit, 6);
          return player;
        } else {
          return EntityBuilder.CreateHastatusEntity(state.CurrentTick, formationNum, unit, FactionName.PLAYER);
        }
      };
      Func<int, Unit, Entity> hastatusFn = (formationNum, unit) => EntityBuilder.CreateHastatusEntity(state.CurrentTick, formationNum, unit, FactionName.PLAYER);
      Func<int, Unit, Entity> secondHastatusFn = (formationNum, unit) => EntityBuilder.CreateHastatusEntity(state.CurrentTick, formationNum, unit, FactionName.PLAYER, startingMorale: 100);
      Func<int, Unit, Entity> iberianLightInfantryFn = (formationNum, unit) => EntityBuilder.CreateIberianLightInfantry(state.CurrentTick, formationNum, unit, FactionName.ENEMY);

      // HQ
      var hqUnitId = "HQ ID";
      var hq = EntityBuilder.CreateHeadquartersEntity(state.CurrentTick, FactionName.NEUTRAL);
      var commanderAI = hq.GetComponent<CommanderAIComponent>();
      state.PlaceEntity(hq, new EncounterPosition(1, 1)); // Invisible and unreachable but you CAN mouse over it, lol. it's fiiine
      // Commander unit is a placeholder
      var commanderUnit = new Unit(hqUnitId, FactionName.NEUTRAL, hq.GetComponent<PositionComponent>().EncounterPosition,
        UnitOrder.REFORM, FormationType.LINE_20, FormationFacing.SOUTH, true, true);
      state.AddUnit(commanderUnit);
      commanderAI.RegisterUnit(commanderUnit);
      // Boilerplate conditions
      var playerRouted = new OrderTrigger(OrderTriggerType.ALL_UNITS_OF_FACTION_ROUTED, false, triggerFaction: FactionName.PLAYER);
      commanderAI.RegisterTriggeredOrder(playerRouted, new Order(hqUnitId, OrderType.DECLARE_DEFEAT));
      var enemyRouted = new OrderTrigger(OrderTriggerType.ALL_UNITS_OF_FACTION_ROUTED, false, triggerFaction: FactionName.ENEMY);
      commanderAI.RegisterTriggeredOrder(enemyRouted, new Order(hqUnitId, OrderType.DECLARE_VICTORY));


      var pCenter = CreateAndDeployUnit(seededRand, state, "test player center", FactionName.PLAYER,
        new EncounterPosition(centerPos.X, centerPos.Y - 15), UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
        FormationFacing.SOUTH, 123, hastatusWithPlayerFn, leftFlank: true, rightFlank: true, hq);
      commanderAI.RegisterDeploymentOrder(20, new Order(pCenter.UnitId, OrderType.ADVANCE));
      commanderAI.RegisterDeploymentOrder(30, new Order(pCenter.UnitId, OrderType.OPEN_MANIPULE));
      commanderAI.RegisterDeploymentOrder(50, new Order(pCenter.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(commanderAI, pCenter, .80f);

      /* var pLeft = CreateAndDeployUnit(seededRand, state, "test player left", FactionName.PLAYER,
        new EncounterPosition(centerPos.X + 20, centerPos.Y - 15), UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
        FormationFacing.SOUTH, 98, hastatusFn, leftFlank: true, rightFlank: false, friendlyHQ);
      friendlyCommanderAI.RegisterDeploymentOrder(10, new Order(pLeft.UnitId, OrderType.ADVANCE));
      friendlyCommanderAI.RegisterDeploymentOrder(20, new Order(pLeft.UnitId, OrderType.OPEN_MANIPULE));
      friendlyCommanderAI.RegisterDeploymentOrder(50, new Order(pLeft.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(friendlyCommanderAI, pLeft, .80f);

      var pRight = CreateAndDeployUnit(seededRand, state, "test player right", FactionName.PLAYER,
        new EncounterPosition(centerPos.X - 20, centerPos.Y - 15), UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
        FormationFacing.SOUTH, 107, hastatusFn, leftFlank: false, rightFlank: true, friendlyHQ);
      friendlyCommanderAI.RegisterDeploymentOrder(15, new Order(pRight.UnitId, OrderType.ADVANCE));
      friendlyCommanderAI.RegisterDeploymentOrder(25, new Order(pRight.UnitId, OrderType.OPEN_MANIPULE));
      friendlyCommanderAI.RegisterDeploymentOrder(50, new Order(pRight.UnitId, OrderType.ADVANCE));
      var pRightBreakTrigger = new OrderTrigger(OrderTriggerType.UNIT_BELOW_STRENGTH_PERCENT, false,
        watchedUnitIds: new List<string>() { pRight.UnitId }, belowStrengthPercent: 80);
      RegisterRoutAtPercentage(friendlyCommanderAI, pRight, .80f);

      var p2ndCenter = CreateAndDeployUnit(seededRand, state, "test player 2nd center", FactionName.PLAYER,
        new EncounterPosition(centerPos.X, centerPos.Y - 35), UnitOrder.REFORM, FormationType.MANIPULE_OPENED,
        FormationFacing.SOUTH, 58, secondHastatusFn, leftFlank: false, rightFlank: false, friendlyHQ);
      var p2ndCenterAdvTrigger = new OrderTrigger(OrderTriggerType.UNIT_HAS_STANDING_ORDER, false,
        watchedUnitIds: new List<string>() { pCenter.UnitId, pLeft.UnitId, pRight.UnitId },
        awaitedStandingOrders: new List<UnitOrder>() { UnitOrder.RETREAT });
      friendlyCommanderAI.RegisterTriggeredOrder(p2ndCenterAdvTrigger, new Order(p2ndCenter.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(friendlyCommanderAI, p2ndCenter, .50f);

      var p2ndLeft = CreateAndDeployUnit(seededRand, state, "test player 2nd left", FactionName.PLAYER,
        new EncounterPosition(centerPos.X + 20, centerPos.Y - 35), UnitOrder.REFORM, FormationType.MANIPULE_OPENED,
        FormationFacing.SOUTH, 70, secondHastatusFn, leftFlank: true, rightFlank: false, friendlyHQ);
      var p2ndLeftAdvTrigger = new OrderTrigger(OrderTriggerType.UNIT_HAS_STANDING_ORDER, false,
        watchedUnitIds: new List<string>() { pCenter.UnitId, pLeft.UnitId, pRight.UnitId },
        awaitedStandingOrders: new List<UnitOrder>() { UnitOrder.RETREAT });
      friendlyCommanderAI.RegisterTriggeredOrder(p2ndLeftAdvTrigger, new Order(p2ndLeft.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(friendlyCommanderAI, p2ndLeft, .50f);

      var p2ndRight = CreateAndDeployUnit(seededRand, state, "test player 2nd right", FactionName.PLAYER,
        new EncounterPosition(centerPos.X - 20, centerPos.Y - 35), UnitOrder.REFORM, FormationType.MANIPULE_OPENED,
        FormationFacing.SOUTH, 63, secondHastatusFn, leftFlank: false, rightFlank: true, friendlyHQ);
      var p2ndRightAdvTrigger = new OrderTrigger(OrderTriggerType.UNIT_HAS_STANDING_ORDER, false,
        watchedUnitIds: new List<string>() { pCenter.UnitId, pLeft.UnitId, pRight.UnitId },
        awaitedStandingOrders: new List<UnitOrder>() { UnitOrder.RETREAT });
      friendlyCommanderAI.RegisterTriggeredOrder(p2ndRightAdvTrigger, new Order(p2ndRight.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(friendlyCommanderAI, p2ndRight, .50f); */
      
      // Enemy deployment
      var eCenter = CreateAndDeployUnit(seededRand, state, "test enemy center", FactionName.ENEMY,
        new EncounterPosition(centerPos.X, centerPos.Y + 40), UnitOrder.REFORM, FormationType.LINE_20,
        FormationFacing.NORTH, 95, iberianLightInfantryFn, leftFlank: true, rightFlank: true, hq);
      commanderAI.RegisterDeploymentOrder(35, new Order(eCenter.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(commanderAI, eCenter, .80f);

      /* var eLeft = CreateAndDeployUnit(seededRand, state, "test enemy left", FactionName.ENEMY,
        new EncounterPosition(centerPos.X - 20, centerPos.Y + 40), UnitOrder.REFORM, FormationType.LINE_20,
        FormationFacing.NORTH, 99, iberianLightInfantryFn, leftFlank: true, rightFlank: false, enemyHQ);
      friendlyCommanderAI.RegisterDeploymentOrder(35, new Order(eLeft.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(friendlyCommanderAI, eLeft, .80f);
      
      var eRight = CreateAndDeployUnit(seededRand, state, "test enemy right", FactionName.ENEMY,
        new EncounterPosition(centerPos.X + 20, centerPos.Y + 40), UnitOrder.REFORM, FormationType.LINE_20,
        FormationFacing.NORTH, 90, iberianLightInfantryFn, leftFlank: false, rightFlank: true, enemyHQ);
      friendlyCommanderAI.RegisterDeploymentOrder(35, new Order(eRight.UnitId, OrderType.ADVANCE));
      RegisterRoutAtPercentage(friendlyCommanderAI, eRight, .80f); */
      
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