using SpaceDodgeRL.library;
using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.resources.gamedata;
using SpaceDodgeRL.scenes.components;
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

    private static void CreateUnit(Random seededRand, EncounterState state, string unitId, FactionName faction,
        EncounterPosition center, UnitOrder order, FormationType type, FormationFacing facing, int size,
        Func<int, Unit, Entity> entityFn, bool leftFlank, bool rightFlank) {
      var unit = new Unit(unitId, center, order, type, facing, leftFlank, rightFlank);
      state.AddUnit(unit);

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
    }

    public static void PopulateStateForLevel(Entity player, int dungeonLevel, EncounterState state, Random seededRand,
        int width = 300, int height = 300, int maxZoneGenAttempts = 100) {
      InitializeMapAndAddBorderWalls(state, width, height);

      // Add the player to the map
      var playerPos = new EncounterPosition(width / 2, height / 2);
      state.PlaceEntity(player, playerPos);

      Func<int, Unit, Entity> hastatusFn = (formationNum, unit) => EntityBuilder.CreateHastatusEntity(state.CurrentTick, formationNum, unit, FactionName.PLAYER);
      Func<int, Unit, Entity> iberianLightInfantryFn = (formationNum, unit) => EntityBuilder.CreateIberianLightInfantry(state.CurrentTick, formationNum, unit, FactionName.ENEMY);

      CreateUnit(seededRand, state, "test player center", FactionName.PLAYER, new EncounterPosition(playerPos.X, playerPos.Y - 15),
        UnitOrder.REFORM, FormationType.MANIPULE_CLOSED, FormationFacing.SOUTH, 250, hastatusFn, leftFlank: true, rightFlank: true);
      /* CreateUnit(seededRand, state, "test player left", FactionName.PLAYER, new EncounterPosition(playerPos.X + 20, playerPos.Y - 15),
        UnitOrder.REFORM, FormationType.MANIPULE_CLOSED, FormationFacing.SOUTH, 95, hastatusFn);
      CreateUnit(seededRand, state, "test player right", FactionName.PLAYER, new EncounterPosition(playerPos.X - 20, playerPos.Y - 15),
        UnitOrder.REFORM, FormationType.MANIPULE_CLOSED, FormationFacing.SOUTH, 34, hastatusFn); */
      
      CreateUnit(seededRand, state, "test enemy center", FactionName.ENEMY, new EncounterPosition(playerPos.X, playerPos.Y + 40),
        UnitOrder.REFORM, FormationType.LINE_20, FormationFacing.NORTH, 250, iberianLightInfantryFn, leftFlank: false, rightFlank: false);
      /* CreateUnit(seededRand, state, "test enemy left", FactionName.ENEMY, new EncounterPosition(playerPos.X - 20, playerPos.Y + 40),
        UnitOrder.REFORM, FormationType.LINE_20, FormationFacing.NORTH, 70, iberianLightInfantryFn);
      CreateUnit(seededRand, state, "test enemy right", FactionName.ENEMY, new EncounterPosition(playerPos.X + 20, playerPos.Y + 40),
        UnitOrder.REFORM, FormationType.LINE_20, FormationFacing.NORTH, 57, iberianLightInfantryFn); */

      
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