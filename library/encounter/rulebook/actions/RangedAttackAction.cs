using MTW7DRL2021.scenes.entities;

namespace MTW7DRL2021.library.encounter.rulebook.actions {

  public class RangedAttackAction : EncounterAction {

    public Entity TargetEntity { get; private set; }

    public RangedAttackAction(string actorId, Entity targetEntity) : base(actorId, ActionType.RANGED_ATTACK) {
      this.TargetEntity = targetEntity;
    }
  }
}