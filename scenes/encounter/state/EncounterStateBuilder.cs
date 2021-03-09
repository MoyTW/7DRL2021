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

    private static void InitializeMapAndAddBorderWalls(EncounterState state, int width, int height) {
      // Initialize the map with empty tiles
      state.MapWidth = width;
      state.MapHeight = height;
      state._encounterTiles = new EncounterTile[width, height];
      for (int x = 0; x < width; x++) {
        for (int y = 0; y < height; y++) {
          state._encounterTiles[x, y] = new EncounterTile();
        }
      }

      // Create border walls to prevent objects running off the map
      for (int x = 0; x < width; x++) {
        for (int y = 0; y < height; y++) {
          if (x == 0 || x == width - 1 || y == 0 || y == height - 1) {
            state.PlaceEntity(EntityBuilder.CreateEdgeBlockerEntity(), new EncounterPosition(x, y));
          }
        }
      }
    }

    private static Unit CreateAndDeployUnit(Random seededRand, EncounterState state, string unitId, FactionName faction,
        EncounterPosition center, UnitOrder order, FormationType type, FormationFacing facing, int size,
        Func<int, Unit, Entity> entityFn, bool leftFlank, bool rightFlank, Entity commander) {
      var unit = new Unit(unitId, center, order, type, facing, leftFlank, rightFlank);
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
      }

      return unit;
    }

    public static void PopulateStateForLevel(Entity player, int dungeonLevel, EncounterState state, Random seededRand,
        int width = 300, int height = 300, int maxZoneGenAttempts = 100) {
      InitializeMapAndAddBorderWalls(state, width, height);

      // Add the player to the map
      var playerPos = new EncounterPosition(width / 2, height / 2);
      state.PlaceEntity(player, playerPos);

      Func<int, Unit, Entity> hastatusFn = (formationNum, unit) => EntityBuilder.CreateHastatusEntity(state.CurrentTick, formationNum, unit, FactionName.PLAYER);
      Func<int, Unit, Entity> iberianLightInfantryFn = (formationNum, unit) => EntityBuilder.CreateIberianLightInfantry(state.CurrentTick, formationNum, unit, FactionName.ENEMY);

      // Friendly deployment
      var friendlyHQ = EntityBuilder.CreateHeadquartersEntity(state.CurrentTick, FactionName.PLAYER);
      var friendlyCommanderAI = friendlyHQ.GetComponent<CommanderAIComponent>();
      state.PlaceEntity(friendlyHQ, new EncounterPosition(playerPos.X, playerPos.Y - 50));

      var pCenter = CreateAndDeployUnit(seededRand, state, "test player center", FactionName.PLAYER,
        new EncounterPosition(playerPos.X, playerPos.Y - 15), UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
        FormationFacing.SOUTH, 123, hastatusFn, leftFlank: false, rightFlank: false, friendlyHQ);
      friendlyCommanderAI.RegisterDeploymentOrder(20, new Order(pCenter.UnitId, OrderType.ADVANCE));
      friendlyCommanderAI.RegisterDeploymentOrder(30, new Order(pCenter.UnitId, OrderType.OPEN_MANIPULE));
      friendlyCommanderAI.RegisterDeploymentOrder(50, new Order(pCenter.UnitId, OrderType.ADVANCE));

      var pLeft = CreateAndDeployUnit(seededRand, state, "test player left", FactionName.PLAYER,
        new EncounterPosition(playerPos.X + 20, playerPos.Y - 15), UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
        FormationFacing.SOUTH, 98, hastatusFn, leftFlank: true, rightFlank: false, friendlyHQ);
      friendlyCommanderAI.RegisterDeploymentOrder(10, new Order(pLeft.UnitId, OrderType.ADVANCE));
      friendlyCommanderAI.RegisterDeploymentOrder(20, new Order(pLeft.UnitId, OrderType.OPEN_MANIPULE));
      friendlyCommanderAI.RegisterDeploymentOrder(50, new Order(pLeft.UnitId, OrderType.ADVANCE));

      var pRight = CreateAndDeployUnit(seededRand, state, "test player right", FactionName.PLAYER,
        new EncounterPosition(playerPos.X - 20, playerPos.Y - 15), UnitOrder.REFORM, FormationType.MANIPULE_CLOSED,
        FormationFacing.SOUTH, 107, hastatusFn, leftFlank: false, rightFlank: true, friendlyHQ);
      friendlyCommanderAI.RegisterDeploymentOrder(15, new Order(pRight.UnitId, OrderType.ADVANCE));
      friendlyCommanderAI.RegisterDeploymentOrder(25, new Order(pRight.UnitId, OrderType.OPEN_MANIPULE));
      friendlyCommanderAI.RegisterDeploymentOrder(50, new Order(pRight.UnitId, OrderType.ADVANCE));

      var p2ndCenter = CreateAndDeployUnit(seededRand, state, "test player 2nd center", FactionName.PLAYER,
        new EncounterPosition(playerPos.X, playerPos.Y - 35), UnitOrder.REFORM, FormationType.MANIPULE_OPENED,
        FormationFacing.SOUTH, 58, hastatusFn, leftFlank: false, rightFlank: false, friendlyHQ);
      var p2ndCenterAdvTrigger = new OrderTrigger(OrderTriggerType.UNIT_HAS_STANDING_ORDER,
        watchedUnitIds: new List<string>() { pCenter.UnitId, pLeft.UnitId, pRight.UnitId },
        awaitedStandingOrders: new List<UnitOrder>() { UnitOrder.RETREAT });
      friendlyCommanderAI.RegisterTriggeredOrder(p2ndCenterAdvTrigger, new Order(p2ndCenter.UnitId, OrderType.ADVANCE));

      var p2ndLeft = CreateAndDeployUnit(seededRand, state, "test player 2nd left", FactionName.PLAYER,
        new EncounterPosition(playerPos.X + 20, playerPos.Y - 35), UnitOrder.REFORM, FormationType.MANIPULE_OPENED,
        FormationFacing.SOUTH, 70, hastatusFn, leftFlank: true, rightFlank: false, friendlyHQ);
      var p2ndLeftAdvTrigger = new OrderTrigger(OrderTriggerType.UNIT_HAS_STANDING_ORDER,
        watchedUnitIds: new List<string>() { pCenter.UnitId, pLeft.UnitId, pRight.UnitId },
        awaitedStandingOrders: new List<UnitOrder>() { UnitOrder.RETREAT });
      friendlyCommanderAI.RegisterTriggeredOrder(p2ndLeftAdvTrigger, new Order(p2ndLeft.UnitId, OrderType.ADVANCE));

      var p2ndRight = CreateAndDeployUnit(seededRand, state, "test player 2nd right", FactionName.PLAYER,
        new EncounterPosition(playerPos.X - 20, playerPos.Y - 35), UnitOrder.REFORM, FormationType.MANIPULE_OPENED,
        FormationFacing.SOUTH, 63, hastatusFn, leftFlank: false, rightFlank: true, friendlyHQ);
      var p2ndRightAdvTrigger = new OrderTrigger(OrderTriggerType.UNIT_HAS_STANDING_ORDER,
        watchedUnitIds: new List<string>() { pCenter.UnitId, pLeft.UnitId, pRight.UnitId },
        awaitedStandingOrders: new List<UnitOrder>() { UnitOrder.RETREAT });
      friendlyCommanderAI.RegisterTriggeredOrder(p2ndRightAdvTrigger, new Order(p2ndRight.UnitId, OrderType.ADVANCE));
      
      // Enemy deployment
      var enemyHQ = EntityBuilder.CreateHeadquartersEntity(state.CurrentTick, FactionName.ENEMY);
      state.PlaceEntity(enemyHQ, new EncounterPosition(playerPos.X, playerPos.Y + 90));
      /*this.TestTimer += 1;
      if (this.TestTimer == 35 && unitComponent.FormationNumber == 0) {
        unit.StandingOrder = UnitOrder.ADVANCE;
      }*/

      var eCenter = CreateAndDeployUnit(seededRand, state, "test enemy center", FactionName.ENEMY,
        new EncounterPosition(playerPos.X, playerPos.Y + 40), UnitOrder.REFORM, FormationType.LINE_20,
        FormationFacing.NORTH, 95, iberianLightInfantryFn, leftFlank: false, rightFlank: false, enemyHQ);
      friendlyCommanderAI.RegisterDeploymentOrder(35, new Order(eCenter.UnitId, OrderType.ADVANCE));

      var eLeft = CreateAndDeployUnit(seededRand, state, "test enemy left", FactionName.ENEMY,
        new EncounterPosition(playerPos.X - 20, playerPos.Y + 40), UnitOrder.REFORM, FormationType.LINE_20,
        FormationFacing.NORTH, 99, iberianLightInfantryFn, leftFlank: true, rightFlank: false, enemyHQ);
      friendlyCommanderAI.RegisterDeploymentOrder(35, new Order(eLeft.UnitId, OrderType.ADVANCE));
      
      var eRight = CreateAndDeployUnit(seededRand, state, "test enemy right", FactionName.ENEMY,
        new EncounterPosition(playerPos.X + 20, playerPos.Y + 40), UnitOrder.REFORM, FormationType.LINE_20,
        FormationFacing.NORTH, 90, iberianLightInfantryFn, leftFlank: false, rightFlank: true, enemyHQ);
      friendlyCommanderAI.RegisterDeploymentOrder(35, new Order(eRight.UnitId, OrderType.ADVANCE));

      
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