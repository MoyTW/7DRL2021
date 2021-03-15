using System.Collections.Generic;
using MTW7DRL2021.library.encounter.rulebook;
using MTW7DRL2021.library.encounter.rulebook.actions;
using MTW7DRL2021.scenes.encounter.state;
using MTW7DRL2021.scenes.entities;

namespace MTW7DRL2021.scenes.components.AI {

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