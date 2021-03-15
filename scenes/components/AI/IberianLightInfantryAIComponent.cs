using MTW7DRL2021.library.encounter;
using MTW7DRL2021.library.encounter.rulebook;
using MTW7DRL2021.library.encounter.rulebook.actions;
using MTW7DRL2021.scenes.encounter.state;
using MTW7DRL2021.scenes.entities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MTW7DRL2021.scenes.components.AI {

  public class IberianLightInfantryAIComponent : DeciderAIComponent {
    public static readonly string ENTITY_GROUP = "IBERIAN_LIGHT_INFANTRY_AI_COMPONENT_GROUP";
    public override string EntityGroup => ENTITY_GROUP;

    public IberianLightInfantryAIComponent() { }

    public static IberianLightInfantryAIComponent Create(string saveData) {
      return JsonSerializer.Deserialize<IberianLightInfantryAIComponent>(saveData);
    }

    public override List<EncounterAction> _DecideNextAction(EncounterState state, Entity parent) {
      var unit = state.GetUnit(parent.GetComponent<UnitComponent>().UnitId);
      var unitComponent = parent.GetComponent<UnitComponent>();

      if (unit.StandingOrder == UnitOrder.REFORM) {
        return AIUtils.ActionsForUnitReform(state, parent, unitComponent.FormationNumber, unit);
      } else if (unit.StandingOrder == UnitOrder.ADVANCE) {
        return AIUtils.ActionsForUnitAdvanceInLine(state, parent, unit);
      } else if (unit.StandingOrder == UnitOrder.ROUT) {
        return AIUtils.ActionsForUnitRetreat(state, parent, unit);
      } else {
        throw new NotImplementedException();
      }
    }

    public override string Save() {
      return JsonSerializer.Serialize(this);
    }

    public override void NotifyAttached(Entity parent) { }

    public override void NotifyDetached(Entity parent) { }
  }
}