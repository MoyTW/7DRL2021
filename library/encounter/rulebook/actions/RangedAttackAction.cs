using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.library.encounter.rulebook.actions {

  public class RangedAttackAction : EncounterAction {

    public Entity TargetEntity { get; private set; }

    public RangedAttackAction(string actorId, Entity targetEntity) : base(actorId, ActionType.RANGED_ATTACK) {
      this.TargetEntity = targetEntity;
    }
  }
}