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

    public static Entity CreateCommanderEntity(int currentTick, FactionName faction) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "Hidden Commander Unit");
      
      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      e.AddComponent(new CommanderAIComponent());

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(CollisionComponent.CreateDefaultActor());
      e.AddComponent(DefenderComponent.Create(9999, 9999, 9999, 9999, 9999, isInvincible: true));
      e.AddComponent(DisplayComponent.Create(_texCarrierPath, "Hidden Commander Unit", false, ENTITY_Z_INDEX, visible: false));
      e.AddComponent(FactionComponent.Create(faction));
      e.AddComponent(SpeedComponent.Create(baseSpeed: 100));
      e.AddComponent(XPValueComponent.Create(xpValue: 9999));

      return e;
    }

    public static Entity CreateHastatusEntity(int currentTick, int formationNumber, Unit unit, FactionName faction,
        int numPilas=1, int startingMorale=45) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "Hastatus");
      
      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      e.AddComponent(new HastatusAIComponent(numPilas));
      e.AddComponent(AIRotationComponent.Create(.60, false));
      e.AddComponent(AIMoraleComponent.Create(100, startingMorale));

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(AttackerComponent.Create(e.EntityId, 5, meleeAttack: 50, rangedAttack: 30));
      e.AddComponent(CollisionComponent.CreateDefaultActor());
      e.AddComponent(DefenderComponent.Create(baseDefense: 2, maxHp: 45, maxFooting: 80, meleeDefense: 15, rangedDefense: 25));
      e.AddComponent(DisplayComponent.Create(_texScoutPath, "A young and eager soldier.", false, ENTITY_Z_INDEX));
      e.AddComponent(FactionComponent.Create(faction));
      e.AddComponent(OnDeathComponent.Create(new List<string>() { OnDeathEffectType.REMOVE_FROM_UNIT }));
      e.AddComponent(SpeedComponent.Create(baseSpeed: 100));
      e.AddComponent(statusEffectTrackerComponent);
      e.AddComponent(UnitComponent.Create(unit.UnitId, formationNumber));
      e.AddComponent(XPValueComponent.Create(xpValue: 30));

      return e;
    }

    public static Entity CreatePrincepsEntity(int currentTick, int formationNumber, Unit unit, FactionName faction,
        int numPilas=1, int startingMorale=65) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "Princeps");
      
      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      // Princeps AI is essentially the same as Hastatus AI
      e.AddComponent(new HastatusAIComponent(numPilas));
      e.AddComponent(AIRotationComponent.Create(.60, false));
      e.AddComponent(AIMoraleComponent.Create(100, startingMorale));

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(AttackerComponent.Create(e.EntityId, 6, meleeAttack: 55, rangedAttack: 30));
      e.AddComponent(CollisionComponent.CreateDefaultActor());
      e.AddComponent(DefenderComponent.Create(baseDefense: 2, maxHp: 65, maxFooting: 100, meleeDefense: 20, rangedDefense: 30));
      e.AddComponent(DisplayComponent.Create(_texDestroyerPath, "An experienced swordsman with good equipment.", false, ENTITY_Z_INDEX));
      e.AddComponent(FactionComponent.Create(faction));
      e.AddComponent(OnDeathComponent.Create(new List<string>() { OnDeathEffectType.REMOVE_FROM_UNIT }));
      e.AddComponent(SpeedComponent.Create(baseSpeed: 100));
      e.AddComponent(statusEffectTrackerComponent);
      e.AddComponent(UnitComponent.Create(unit.UnitId, formationNumber));
      e.AddComponent(XPValueComponent.Create(xpValue: 30));

      return e;
    }

    public static Entity CreateTriariusEntity(int currentTick, int formationNumber, Unit unit, FactionName faction,
        int startingMorale=85) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "Triarius");
      
      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      // TODO: different AI
      e.AddComponent(new HastatusAIComponent(0));
      e.AddComponent(AIRotationComponent.Create(.60, false));
      e.AddComponent(AIMoraleComponent.Create(100, startingMorale));

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(AttackerComponent.Create(e.EntityId, 8, meleeAttack: 65, rangedAttack: 30));
      e.AddComponent(CollisionComponent.CreateDefaultActor());
      e.AddComponent(DefenderComponent.Create(baseDefense: 3, maxHp: 85, maxFooting: 120, meleeDefense: 30, rangedDefense: 45));
      e.AddComponent(DisplayComponent.Create(_texCruiserPath, "An elite spearman of the legion.", false, ENTITY_Z_INDEX));
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
      e.AddComponent(AIRotationComponent.Create(.7, false));
      e.AddComponent(AIMoraleComponent.Create(100, 90));

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(AttackerComponent.Create(e.EntityId, 5, meleeAttack: 55, rangedAttack: 10));
      e.AddComponent(CollisionComponent.CreateDefaultActor());
      e.AddComponent(DefenderComponent.Create(baseDefense: 1, maxHp: 40, maxFooting: 75, meleeDefense: 20, rangedDefense: 5));
      e.AddComponent(DisplayComponent.Create(_texFighterPath, "A fast, deatly, and barely armored Iberian swordsman.", false, ENTITY_Z_INDEX));
      e.AddComponent(FactionComponent.Create(faction));
      e.AddComponent(OnDeathComponent.Create(new List<string>() { OnDeathEffectType.REMOVE_FROM_UNIT }));
      e.AddComponent(SpeedComponent.Create(baseSpeed: 80));
      e.AddComponent(statusEffectTrackerComponent);
      e.AddComponent(UnitComponent.Create(unit.UnitId, formationNumber));
      e.AddComponent(XPValueComponent.Create(xpValue: 30));

      return e;
    }

    public static Entity CreateGallicLightInfantry(int currentTick, int formationNumber, Unit unit, FactionName faction) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "Gallic Light Infantry");
      
      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      // TODO: one day I'll have different AIs
      e.AddComponent(new IberianLightInfantryAIComponent());
      e.AddComponent(AIRotationComponent.Create(.4, false));
      e.AddComponent(AIMoraleComponent.Create(100, 90));

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(AttackerComponent.Create(e.EntityId, 8, meleeAttack: 45, rangedAttack: 10));
      e.AddComponent(CollisionComponent.CreateDefaultActor());
      e.AddComponent(DefenderComponent.Create(baseDefense: 0, maxHp: 35, maxFooting: 60, meleeDefense: 30, rangedDefense: 45));
      e.AddComponent(DisplayComponent.Create(_texBatteryPath, "A fast and strong Gallic soldier. Fights defensively, but hits hard.", false, ENTITY_Z_INDEX));
      e.AddComponent(FactionComponent.Create(faction));
      e.AddComponent(OnDeathComponent.Create(new List<string>() { OnDeathEffectType.REMOVE_FROM_UNIT }));
      e.AddComponent(SpeedComponent.Create(baseSpeed: 90));
      e.AddComponent(statusEffectTrackerComponent);
      e.AddComponent(UnitComponent.Create(unit.UnitId, formationNumber));
      e.AddComponent(XPValueComponent.Create(xpValue: 30));

      return e;
    }

    public static Entity CreateGallicVeteran(int currentTick, int formationNumber, Unit unit, FactionName faction) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "Gallic Veteran Infantry");
      
      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      // TODO: one day I'll have different AIs
      e.AddComponent(new IberianLightInfantryAIComponent());
      e.AddComponent(AIRotationComponent.Create(.4, false));
      e.AddComponent(AIMoraleComponent.Create(100, 90));

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(AttackerComponent.Create(e.EntityId, 9, meleeAttack: 50, rangedAttack: 10));
      e.AddComponent(CollisionComponent.CreateDefaultActor());
      e.AddComponent(DefenderComponent.Create(baseDefense: 2, maxHp: 55, maxFooting: 110, meleeDefense: 35, rangedDefense: 45));
      e.AddComponent(DisplayComponent.Create(_texGunshipPath, "An armored Gallic veteran. Fights defensively, but hits hard.", false, ENTITY_Z_INDEX));
      e.AddComponent(FactionComponent.Create(faction));
      e.AddComponent(OnDeathComponent.Create(new List<string>() { OnDeathEffectType.REMOVE_FROM_UNIT }));
      e.AddComponent(SpeedComponent.Create(baseSpeed: 100));
      e.AddComponent(statusEffectTrackerComponent);
      e.AddComponent(UnitComponent.Create(unit.UnitId, formationNumber));
      e.AddComponent(XPValueComponent.Create(xpValue: 30));

      return e;
    }

    public static Entity CreatePunicVeteranInfantry(int currentTick, int formationNumber, Unit unit, FactionName faction) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "Punic Veteran Infantry");
      
      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      // TODO: one day I'll have different AIs
      e.AddComponent(new IberianLightInfantryAIComponent());
      e.AddComponent(AIRotationComponent.Create(.4, false));
      e.AddComponent(AIMoraleComponent.Create(100, 90));

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(AttackerComponent.Create(e.EntityId, 8, meleeAttack: 55, rangedAttack: 10));
      e.AddComponent(CollisionComponent.CreateDefaultActor());
      e.AddComponent(DefenderComponent.Create(baseDefense: 2, maxHp: 60, maxFooting: 100, meleeDefense: 25, rangedDefense: 35));
      e.AddComponent(DisplayComponent.Create(_texFrigatePath, "Hardened, armored Punic mercenaries.", false, ENTITY_Z_INDEX));
      e.AddComponent(FactionComponent.Create(faction));
      e.AddComponent(OnDeathComponent.Create(new List<string>() { OnDeathEffectType.REMOVE_FROM_UNIT }));
      e.AddComponent(SpeedComponent.Create(baseSpeed: 100));
      e.AddComponent(statusEffectTrackerComponent);
      e.AddComponent(UnitComponent.Create(unit.UnitId, formationNumber));
      e.AddComponent(XPValueComponent.Create(xpValue: 30));

      return e;
    }

    public static Entity CreatePunicHeavyInfantry(int currentTick, int formationNumber, Unit unit, FactionName faction) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "Punic Heavy Infantry");
      
      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      // TODO: one day I'll have different AIs
      e.AddComponent(new IberianLightInfantryAIComponent());
      e.AddComponent(AIRotationComponent.Create(.4, false));
      e.AddComponent(AIMoraleComponent.Create(100, 90));

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(AttackerComponent.Create(e.EntityId, 11, meleeAttack: 55, rangedAttack: 10));
      e.AddComponent(CollisionComponent.CreateDefaultActor());
      e.AddComponent(DefenderComponent.Create(baseDefense: 4, maxHp: 80, maxFooting: 140, meleeDefense: 45, rangedDefense: 45));
      e.AddComponent(DisplayComponent.Create(_texFrigatePath, "Carthage's very best heavy infantry.", false, ENTITY_Z_INDEX));
      e.AddComponent(FactionComponent.Create(faction));
      e.AddComponent(OnDeathComponent.Create(new List<string>() { OnDeathEffectType.REMOVE_FROM_UNIT }));
      e.AddComponent(SpeedComponent.Create(baseSpeed: 100));
      e.AddComponent(statusEffectTrackerComponent);
      e.AddComponent(UnitComponent.Create(unit.UnitId, formationNumber));
      e.AddComponent(XPValueComponent.Create(xpValue: 30));

      return e;
    }

    public static Entity CreatePlayerEntity(int currentTick) {
      var e = CreateEntity(Guid.NewGuid().ToString(), "player");

      // TODO: modify PlayerAIComponent to it doesn't, you know...need these.
      e.AddComponent(new PlayerAIComponent());
      e.AddComponent(AIRotationComponent.Create(.60, true));
      e.AddComponent(AIMoraleComponent.Create(100, 100));

      var statusEffectTrackerComponent = StatusEffectTrackerComponent.Create();

      e.AddComponent(ActionTimeComponent.Create(currentTick));
      e.AddComponent(AttackerComponent.Create(e.EntityId, power: 500, meleeAttack: 9999, rangedAttack: 10)); // TODO: make player not Ares
      e.AddComponent(CollisionComponent.Create(blocksMovement: true, blocksVision: false));
      e.AddComponent(DefenderComponent.Create(baseDefense: 0, maxHp: 100, maxFooting: 100, meleeDefense: 45, rangedDefense: 60, isInvincible: false));
      e.AddComponent(DisplayComponent.Create(_texPlayerPath, "It's you!", false, ENTITY_Z_INDEX));
      e.AddComponent(FactionComponent.Create(FactionName.PLAYER));
      e.AddComponent(InventoryComponent.Create(inventorySize: 26));
      e.AddComponent(OnDeathComponent.Create(new List<string>() { OnDeathEffectType.PLAYER_DEFEAT }));
      e.AddComponent(PlayerComponent.Create(isInFormation: true));
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