using MTW7DRL2021.scenes.entities;

namespace MTW7DRL2021.library.encounter.rulebook.actions {

  public class SpawnEntityAction : EncounterAction {

    public Entity EntityToSpawn { get; }
    public EncounterPosition Position { get; }
    public bool IgnoreCollision { get; }

    public SpawnEntityAction(string spawnerId, Entity entityToSpawn, EncounterPosition position, bool ignoreCollision) : base(spawnerId, ActionType.SPAWN_ENTITY) {
      this.EntityToSpawn = entityToSpawn;
      this.Position = position;
      this.IgnoreCollision = ignoreCollision;
    }
  }
}