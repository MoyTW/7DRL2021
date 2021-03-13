using SpaceDodgeRL.library;
using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.library.encounter.rulebook;
using SpaceDodgeRL.library.encounter.rulebook.actions;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceDodgeRL.scenes.components.AI {

  public class HastatusAIComponent : DeciderAIComponent {
    public static readonly string ENTITY_GROUP = "HASTATUS_AI_COMPONENT_GROUP";
    public override string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public int PilasRemaining { get; private set; }

    public HastatusAIComponent(int pilasRemaining) {
      this.PilasRemaining = pilasRemaining;
    }

    public static HastatusAIComponent Create(string saveData) {
      return JsonSerializer.Deserialize<HastatusAIComponent>(saveData);
    }

    public override List<EncounterAction> _DecideNextAction(EncounterState state, Entity parent) {
      var unit = state.GetUnit(parent.GetComponent<UnitComponent>().UnitId);
      var unitComponent = parent.GetComponent<UnitComponent>();

      if (unit.StandingOrder == UnitOrder.REFORM) {
        if (state.CurrentTurn < 30) {
            if (state.EncounterRand.Next(50) == 0) {
              parent.GetComponent<PositionComponent>().PlaySpeechBubble("Huphuphup!");
            }
          }
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