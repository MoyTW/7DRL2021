using System.Collections.Generic;
using SpaceDodgeRL.library.encounter.rulebook;
using SpaceDodgeRL.library.encounter.rulebook.actions;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.scenes.components.AI {

  interface AIComponent: Component {
    List<EncounterAction> DecideNextAction(EncounterState state, Entity parent);
  }

  public abstract class DeciderAIComponent : AIComponent {
    public abstract string EntityGroup { get; }

    public abstract List<EncounterAction> _DecideNextAction(EncounterState state, Entity parent);

    public List<EncounterAction> DecideNextAction(EncounterState state, Entity parent) {
      return _DecideNextAction(state, parent);
    }

    public abstract string Save();
    public abstract void NotifyAttached(Entity parent);
    public abstract void NotifyDetached(Entity parent);
  }
}