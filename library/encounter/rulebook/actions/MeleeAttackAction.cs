using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.library.encounter.rulebook.actions {

  public class MeleeAttackAction : EncounterAction {

    public Entity TargetEntity { get; private set; }

    public MeleeAttackAction(string actorId, Entity targetEntity) : base(actorId, ActionType.MELEE_ATTACK) {
      this.TargetEntity = targetEntity;
    }
  }
}