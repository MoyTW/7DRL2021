using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.resources.gamedata;
using SpaceDodgeRL.scenes.entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceDodgeRL.scenes.encounter.state {

  public static class EncounterStateBuilder {

    public static int ZONE_MIN_SIZE = 20;
    public static int ZONE_MAX_SIZE = 40;

    private static void PopulateZone(EncounterZone zone, int dungeonLevel, Random seededRand, EncounterState state, bool safe=false) {
      // Add satellites
      int numSatellites = LevelData.GetNumberOfSatellites(dungeonLevel);
      for (int i = 0; i < numSatellites; i++) {
        var unblockedPosition = zone.RandomEmptyPosition(seededRand, state);
        var satellite = EntityBuilder.CreateSatelliteEntity();
        state.PlaceEntity(satellite, unblockedPosition);
      }

      EncounterDef encounterDef;
      if (safe) {
        encounterDef = LevelData.GetEncounterDefById(EncounterDefId.EMPTY_ENCOUNTER);
      } else {
        encounterDef = LevelData.ChooseEncounter(dungeonLevel, seededRand);
      }
      zone.ReadoutEncounterName = encounterDef.Name;

      if (encounterDef.EntityDefIds.Count > 0) {
        string activationGroupId = Guid.NewGuid().ToString();
        foreach (string entityDefId in encounterDef.EntityDefIds) {
          var unblockedPosition = zone.RandomEmptyPosition(seededRand, state);
          var newEntity = EntityBuilder.CreateEnemyByEntityDefId(entityDefId, activationGroupId, state.CurrentTick);
          state.PlaceEntity(newEntity, unblockedPosition);
        }
      }

      var chosenItemDefs = LevelData.ChooseItemDefs(dungeonLevel, seededRand);
      foreach(string chosenItemDefId in chosenItemDefs) {
        var unblockedPosition = zone.RandomEmptyPosition(seededRand, state);
        var newEntity = EntityBuilder.CreateItemByEntityDefId(chosenItemDefId);
        state.PlaceEntity(newEntity, unblockedPosition);
        zone.AddItemToReadout(newEntity);
      }
    }

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

      state._zones = new List<EncounterZone>();

      // Add the player to the map
      state.PlaceEntity(player, new EncounterPosition(width / 2, height / 2));
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