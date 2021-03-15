using MTW7DRL2021.scenes.entities;

namespace MTW7DRL2021.library.encounter.rulebook.actions {

  public class MeleeAttackAction : EncounterAction {

    public Entity TargetEntity { get; private set; }

    public MeleeAttackAction(string actorId, Entity targetEntity) : base(actorId, ActionType.MELEE_ATTACK) {
      this.TargetEntity = targetEntity;
    }
  }
}