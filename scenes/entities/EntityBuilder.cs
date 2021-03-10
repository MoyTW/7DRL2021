using System;
using System.Collections.Generic;
using Godot;
using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.library.encounter.rulebook.actions;
using SpaceDodgeRL.resources.gamedata;
using SpaceDodgeRL.scenes.components;
using SpaceDodgeRL.scenes.components.AI;
using SpaceDodgeRL.scenes.components.use;

namespace SpaceDodgeRL.scenes.entities {

  public static class EntityBuilder {
    private static int ENTITY_Z_INDEX = 2;
    private static int PROJECTILE_Z_INDEX = 1;
    private static int ITEM_Z_INDEX = 0;

    private static string _texCarrierPath = "res://resources/sprites/carrier.png";
    private static string _texCruiserPath = "res://resources/sprites/cruiser.png";
    private static string _texDestroyerPath = "res://resources/sprites/destroyer.png";
    private static string _texDiplomatPath = "res://resources/sprites/diplomat.png";
    private static string _texEdgeBlockerPath = "res://resources/sprites/edge_blocker.png";
    private static string _texGunshipPath = "res://resources/sprites/gunship.png";
    private static string _texFighterPath = "res://resources/sprites/fighter.png";
    private static string _texFrigatePath = "res://resources/sprites/frigate.png";
    private static string _texIntelPath = "res://resources/sprites/intel.png";
    private static string _texJumpPointPath = "res://resources/sprites/jump_point.png";
    private static string _texPlayerPath = "res://resources/sprites/player.png";
    private static string _texSatellitePath = "res://resources/sprites/asteroid.png";
    private static string _texScoutPath = "res://resources/sprites/scout.png";

    // Items
    private static string _texBatteryPath = "res://resources/sprites/item/battery.png";
    private static string _texDuctTapePath = "res://resources/sprites/item/duct_tape.png";
    private static string _texEMPPath = "res://resources/sprites/item/emp.png";
    private static string _texRedPaintPath = "res://resources/sprites/item/red_paint.png";

    // Projectiles
    private static string _texCuttingLaserPath = "res://resources/sprites/projectile/cutting_laser.png";
    private static string _texSmallCannonPath = "res://resources/sprites/projectile/small_cannon.png";
    private static string _texSmallGatlingPath = "res://resources/sprites/projectile/small_gatling.png";
    private static string _texSmallShotgunPath = "res://resources/sprites/projectile/small_shotgun.png";
    private static string _texRailgunPath = "res://resources/sprites/projectile/railgun.png";
    private static string _texReverserPath = "res://resources/sprites/projectile/reverser.png";

    private class ProjectileDisplayData {
      public ProjectileType Type { get; }
      public string Name { get; }
      public string TexturePath { get; }

      public ProjectileDisplayData(ProjectileType type, string name, string texturePath) {
        this.Type = type;
        this.Name = name;
        this.TexturePath = texturePath;
      }
    }

    private static Dictionary<ProjectileType, ProjectileDisplayData> projectileTypeToProjectileDisplay = new Dictionary<ProjectileType, ProjectileDisplayData>() {
      { ProjectileType.PILA, new ProjectileDisplayData(ProjectileType.CUTTING_LASER, "cutting laser beam", _texCuttingLaserPath) },
      { ProjectileType.CUTTING_LASER, new ProjectileDisplayData(ProjectileType.CUTTING_LASER, "cutting laser beam", _texCuttingLaserPath) },
      { ProjectileType.SMALL_CANNON, new ProjectileDisplayData(ProjectileType.SMALL_CANNON, "small cannon shell", _texSmallCannonPath) },
      { ProjectileType.SMALL_GATLING, new ProjectileDisplayData(ProjectileType.SMALL_GATLING, "small gatling shell", _texSmallGatlingPath) },
      { ProjectileType.SMALL_SHOTGUN, new ProjectileDisplayData(ProjectileType.SMALL_SHOTGUN, "small shotgun pellet", _texSmallShotgunPath) },
      { ProjectileType.RAILGUN, new ProjectileDisplayData(ProjectileType.RAILGUN, "railgun slug", _texRailgunPath) },
      { ProjectileType.REVERSER, new ProjectileDisplayData(ProjectileType.REVERSER, "reverser shot", _texReverserPath) }
    };

    private static Entity CreateEntity(string id, string name) {
      Entity newEntity = Entity.Create(id, name);
      return newEntity;
    }

    public static Entity CreateHeadquartersEntity(int currentTick, FactionName faction) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "Headquarters: " + faction.ToString());
      
      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      e.AddComponent(new CommanderAIComponent());

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(CollisionComponent.CreateDefaultActor());
      e.AddComponent(DefenderComponent.Create(9999, 9999, 9999, 9999, 9999, isInvincible: true));
      e.AddComponent(DisplayComponent.Create(_texCarrierPath, "Headquarters: " + faction.ToString(), false, ENTITY_Z_INDEX));
      e.AddComponent(FactionComponent.Create(faction));
      e.AddComponent(SpeedComponent.Create(baseSpeed: 100));
      e.AddComponent(XPValueComponent.Create(xpValue: 9999));

      return e;
    }

    public static Entity CreateHastatusEntity(int currentTick, int formationNumber, Unit unit, FactionName faction, int numPilas=1) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "Hastatus");
      
      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      e.AddComponent(new HastatusAIComponent(numPilas));
      e.AddComponent(AIRotationComponent.Create(.60));

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(AttackerComponent.Create(e.EntityId, 3, meleeAttack: 50, rangedAttack: 30));
      e.AddComponent(CollisionComponent.CreateDefaultActor());
      e.AddComponent(DefenderComponent.Create(baseDefense: 0, maxHp: 45, maxFooting: 80, meleeDefense: 20, rangedDefense: 25));
      e.AddComponent(DisplayComponent.Create(_texScoutPath, "A small scout craft, armed with a shotgun.", false, ENTITY_Z_INDEX));
      e.AddComponent(FactionComponent.Create(faction));
      e.AddComponent(OnDeathComponent.Create(new List<string>() { OnDeathEffectType.REMOVE_FROM_UNIT }));
      e.AddComponent(SpeedComponent.Create(baseSpeed: 100));
      e.AddComponent(statusEffectTrackerComponent);
      e.AddComponent(UnitComponent.Create(unit.UnitId, formationNumber));
      e.AddComponent(XPValueComponent.Create(xpValue: 30));

      return e;
    }

    public static Entity CreateIberianLightInfantry(int currentTick, int formationNumber, Unit unit, FactionName faction) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "Iberian Light Infantry");
      
      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      e.AddComponent(new IberianLightInfantryAIComponent());
      e.AddComponent(AIRotationComponent.Create(.7));

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(AttackerComponent.Create(e.EntityId, 4, meleeAttack: 60, rangedAttack: 10));
      e.AddComponent(CollisionComponent.CreateDefaultActor());
      e.AddComponent(DefenderComponent.Create(baseDefense: 0, maxHp: 30, maxFooting: 60, meleeDefense: 25, rangedDefense: 5));
      e.AddComponent(DisplayComponent.Create(_texFighterPath, "A small scout craft, armed with a shotgun.", false, ENTITY_Z_INDEX));
      e.AddComponent(FactionComponent.Create(faction));
      e.AddComponent(OnDeathComponent.Create(new List<string>() { OnDeathEffectType.REMOVE_FROM_UNIT }));
      e.AddComponent(SpeedComponent.Create(baseSpeed: 80));
      e.AddComponent(statusEffectTrackerComponent);
      e.AddComponent(UnitComponent.Create(unit.UnitId, formationNumber));
      e.AddComponent(XPValueComponent.Create(xpValue: 30));

      return e;
    }

    private static Entity CreateExtraBatteryEntity() {
      var e = CreateEntity(Guid.NewGuid().ToString(), "extra battery");

      e.AddComponent(DisplayComponent.Create(_texBatteryPath, "An extra battery for your weapons. Gives 20 power for 450 ticks.", true, ITEM_Z_INDEX));
      e.AddComponent(StorableComponent.Create());
      e.AddComponent(UsableComponent.Create(useOnGet: false));
      e.AddComponent(UseEffectBoostPowerComponent.Create(boostPower: 20, duration: 450));

      return e;
    }

    private static Entity CreateDuctTapeEntity() {
      var e = CreateEntity(Guid.NewGuid().ToString(), "duct tape");

      e.AddComponent(DisplayComponent.Create(_texDuctTapePath, "Some duct tape. Heals 10 HP.", true, ITEM_Z_INDEX));
      e.AddComponent(StorableComponent.Create());
      e.AddComponent(UsableComponent.Create(useOnGet: false));
      e.AddComponent(UseEffectHealComponent.Create(healpower: 10));

      return e;
    }

    private static Entity CreateRedPaintEntity() {
      var e = CreateEntity(Guid.NewGuid().ToString(), "red paint");

      e.AddComponent(DisplayComponent.Create(_texRedPaintPath, "Reduces turn time by 75 for 300 ticks (minimum time is 1).", true, ITEM_Z_INDEX));
      e.AddComponent(StorableComponent.Create());
      e.AddComponent(UsableComponent.Create(useOnGet: false));
      e.AddComponent(UseEffectBoostSpeedComponent.Create(boostPower: 75, duration: 300));

      return e;
    }

    private static Entity CreateEMPEntity() {
      var e = CreateEntity(Guid.NewGuid().ToString(), "EMP");

      e.AddComponent(DisplayComponent.Create(_texEMPPath, "An EMP burst. Disables enemies for 10 turns in radius 20.", true, ITEM_Z_INDEX));
      e.AddComponent(StorableComponent.Create());
      e.AddComponent(UsableComponent.Create(useOnGet: false));
      // I seriously put 20 radius 10 turns? That's enough time to mop up an entire encounter!
      e.AddComponent(UseEffectEMPComponent.Create(radius: 20, disableTurns: 10));

      return e;
    }

    public static Entity CreateEnemyByEntityDefId(string enemyDefId, string activationGroupId, int currentTick) {
      throw new NotImplementedException("No mapping defined for " + enemyDefId);
    }

    public static Entity CreateItemByEntityDefId(string itemDefId) {
      if (itemDefId == EntityDefId.ITEM_DUCT_TAPE) {
        return EntityBuilder.CreateDuctTapeEntity();
      } else if (itemDefId == EntityDefId.ITEM_EXTRA_BATTERY) {
        return EntityBuilder.CreateExtraBatteryEntity();
      } else if (itemDefId == EntityDefId.ITEM_RED_PAINT) {
        return EntityBuilder.CreateRedPaintEntity();
      } else if (itemDefId == EntityDefId.ITEM_EMP) {
        return EntityBuilder.CreateEMPEntity();
      } else {
        throw new NotImplementedException("No mapping defined for " + itemDefId);
      }
    }

    public static Entity CreatePlayerEntity(int currentTick) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "player");

      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(CollisionComponent.Create(blocksMovement: true, blocksVision: false));
      e.AddComponent(DefenderComponent.Create(baseDefense: 0, maxHp: 100, maxFooting: 100, meleeDefense: 45, rangedDefense: 60, isInvincible: false));
      e.AddComponent(DisplayComponent.Create(_texPlayerPath, "It's you!", false, ENTITY_Z_INDEX));
      e.AddComponent(FactionComponent.Create(FactionName.PLAYER));
      e.AddComponent(InventoryComponent.Create(inventorySize: 26));
      e.AddComponent(OnDeathComponent.Create(new List<string>() { OnDeathEffectType.PLAYER_DEFEAT }));
      e.AddComponent(PlayerComponent.Create());
      e.AddComponent(SpeedComponent.Create(baseSpeed: 100));
      e.AddComponent(statusEffectTrackerComponent);
      e.AddComponent(XPTrackerComponent.Create(levelUpBase: 200, levelUpFactor: 150));

      return e;
    }

    public static Entity CreateProjectileEntity(Entity source, ProjectileType type, int power, EncounterPath path, Entity target, int speed, int currentTick) {
      var displayData = projectileTypeToProjectileDisplay[type];

      var e = CreateEntity(Guid.NewGuid().ToString(), displayData.Name);

      e.AddComponent(ProjectileAIComponent.Create(path, target.EntityId));

      e.AddComponent(ActionTimeComponent.Create(currentTick)); // Should it go instantly or should it wait for its turn...?
      e.AddComponent(AttackerComponent.Create(source.EntityId, power, meleeAttack: 9999, rangedAttack: 9999)); // TODO: ranged attack
      e.AddComponent(CollisionComponent.Create(false, false, true, true));
      e.AddComponent(DisplayComponent.Create(displayData.TexturePath, "A projectile.", false, PROJECTILE_Z_INDEX));
      e.AddComponent(SpeedComponent.Create(speed));

      return e;
    }

    public static Entity CreateEdgeBlockerEntity() {
      var e = CreateEntity(Guid.NewGuid().ToString(), "boundary sign");

      e.AddComponent(CollisionComponent.Create(true, false));
      e.AddComponent(DefenderComponent.Create(9999, 9999, 9999, 9999, 9999, logDamage: false, isInvincible: true));
      e.AddComponent(DisplayComponent.Create(_texEdgeBlockerPath, "Trying to run away, eh? Get back to your mission!", true, ENTITY_Z_INDEX));

      return e;
    }

    // public static Entity CreateSatelliteEntity() {
    //   var e = CreateEntity(Guid.NewGuid().ToString(), "satellite");

    //   e.AddComponent(CollisionComponent.Create(blocksMovement: true, blocksVision: true));
    //   e.AddComponent(DefenderComponent.Create(baseDefense: int.MaxValue, maxHp: int.MaxValue, isInvincible: true, logDamage: false));
    //   e.AddComponent(DisplayComponent.Create(_texSatellitePath, "Space junk. Blocks movement and projectiles. Cannot be destroyed.", true, ENTITY_Z_INDEX));

    //   return e;
    // }

    public static Entity CreateStairsEntity() {
      var e = CreateEntity(Guid.NewGuid().ToString(), "jump point");

      e.AddComponent(DisplayComponent.Create(_texJumpPointPath, "The jump point to the next sector.", true, ITEM_Z_INDEX));
      e.AddComponent(StairsComponent.Create());

      return e;
    }

    public static Entity CreateIntelEntity(int targetDungeonLevel) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "intel for sector " + targetDungeonLevel);

      e.AddComponent(DisplayComponent.Create(_texIntelPath, "Intel! Gives you zone information for the next sector. You want this.", true, ITEM_Z_INDEX));
      e.AddComponent(StorableComponent.Create());
      e.AddComponent(UsableComponent.Create(useOnGet: true));
      e.AddComponent(UseEffectAddIntelComponent.Create(targetDungeonLevel));

      return e;
    }
  }
}