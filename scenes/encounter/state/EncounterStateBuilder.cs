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

    public static void PopulateStateForLevel(Entity player, int dungeonLevel, EncounterState state, Random seededRand,
        int width = 300, int height = 300, int maxZoneGenAttempts = 100) {
      InitializeMapAndAddBorderWalls(state, width, height);

      // Add the player to the map
      var playerPos = new EncounterPosition(width / 2, height / 2);
      state.PlaceEntity(player, playerPos);
      
      var punit = new Unit("player test unit", playerPos, UnitOrder.REFORM, FormationType.MANIPULE_OPENED, FormationFacing.SOUTH);
      state.AddUnit(punit);

      for (int x = 0; x < 10; x++) {
        for (int y = 0; y < 9; y++) {
          if (!(x == 0 && y == 1)) {
            var marcher = EntityBuilder.CreateManipularEntity(state.CurrentTick, x + 10 * y, punit, Faction.PLAYER);
            var nextToPlayer = new EncounterPosition(playerPos.X + x * 3 - 7, playerPos.Y + y * 2);
            state.PlaceEntity(marcher, nextToPlayer);
          }
        }
      }

      var eunit = new Unit("enemy test unit", new EncounterPosition(playerPos.X, playerPos.Y + 15), UnitOrder.REFORM, FormationType.MANIPULE_OPENED, FormationFacing.NORTH);
      state.AddUnit(eunit);

      for (int x = 0; x < 10; x++) {
        for (int y = 0; y < 9; y++) {
          var marcher = EntityBuilder.CreateManipularEntity(state.CurrentTick, x + 10 * y, eunit, Faction.ENEMY);
            var nextToPlayer = new EncounterPosition(playerPos.X + x * 3 - 7, playerPos.Y + y * 2);
            state.PlaceEntity(marcher, nextToPlayer);
        }
      }
      
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