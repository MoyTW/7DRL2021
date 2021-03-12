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

    private EncounterAction TryThrowPilaAction(EncounterState state, Entity parent) {
      if (this.PilasRemaining == 0) {
        return null;
      }

      var parentPos = parent.GetComponent<PositionComponent>().EncounterPosition;
      var parentFaction = parent.GetComponent<FactionComponent>().Faction;

      // TODO: build a danger map?
      // TODO: throw OVER the heads of the engaged line
      if (this.PilasRemaining > 0) {
        for (int x = parentPos.X - 3; x <= parentPos.X + 3; x++) {
          for (int y = parentPos.Y - 3; y <= parentPos.Y + 3; y++) {
            var possibleHostiles = AIUtils.HostilesInPosition(state, parentFaction, x, y);
            if (possibleHostiles.Count > 0) {
              this.PilasRemaining -= 1;
              return FireProjectileAction.CreatePilaAction(parent.EntityId, possibleHostiles[0]);
            }
          }
        }
      }
      return null;
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
        var pilaAction = this.TryThrowPilaAction(state, parent);
        if (pilaAction != null) {
          return new List<EncounterAction>() { pilaAction };
        } else {
          return AIUtils.ActionsForUnitAdvanceInLine(state, parent, unit);
        }
      } else if (unit.StandingOrder == UnitOrder.RETREAT) {
        return AIUtils.ActionsForUnitRetreat(state, parent, unit);
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