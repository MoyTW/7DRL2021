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

  public class IberianLightInfantryAIComponent : DeciderAIComponent {
    public static readonly string ENTITY_GROUP = "IBERIAN_LIGHT_INFANTRY_AI_COMPONENT_GROUP";
    public override string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public int FormationNumber { get; private set; }

    public int TestTimer { get; set; }

    public IberianLightInfantryAIComponent(int formationNumber) {
      this.FormationNumber = formationNumber;

      this.TestTimer = 0;
    }

    public static IberianLightInfantryAIComponent Create(string saveData) {
      return JsonSerializer.Deserialize<IberianLightInfantryAIComponent>(saveData);
    }

    private List<EncounterAction> _ActionsForUnitAdvance(EncounterState state, Entity parent, Unit unit) {
      var actions = new List<EncounterAction>();

      var parentPos = parent.GetComponent<PositionComponent>().EncounterPosition;
      var parentFaction = parent.GetComponent<FactionComponent>().Faction;

      var targetPos = AIUtils.RotateAndProject(parentPos, 0, -1, unit.UnitFacing);
      if (state.EntitiesAtPosition(targetPos.X, targetPos.Y).Count == 0) {
        // Move
        actions.Add(new MoveAction(parent.EntityId, targetPos));
        // Attack TODO: replace this with charge
        var adjacentHostiles = AIUtils.AdjacentHostiles(state, parentFaction, targetPos);
        // TODO: don't attack randomly
        if (adjacentHostiles.Count > 0) {
          var target = adjacentHostiles[state.EncounterRand.Next(adjacentHostiles.Count)];
          actions.Add(new MeleeAttackAction(parent.EntityId, target));
        }
      } else {
        var adjacentHostiles = AIUtils.AdjacentHostiles(state, parentFaction, parentPos);
        // TODO: don't attack randomly
        if (adjacentHostiles.Count > 0) {
          var target = adjacentHostiles[state.EncounterRand.Next(adjacentHostiles.Count)];
          actions.Add(new MeleeAttackAction(parent.EntityId, target));
        } else {
          actions.Add(new WaitAction(parent.EntityId));
        }
      }
      
      return actions;
    }

    public override List<EncounterAction> _DecideNextAction(EncounterState state, Entity parent) {
      var unit = state.GetUnit(parent.GetComponent<UnitComponent>().UnitId);

      this.TestTimer += 1;
      if (this.TestTimer == 35 && this.FormationNumber == 0) {
        unit.StandingOrder = UnitOrder.ADVANCE;
      }

      if (unit.StandingOrder == UnitOrder.REFORM) {
        return AIUtils.ActionsForUnitReform(state, parent, this.FormationNumber, unit);
      } else if (unit.StandingOrder == UnitOrder.ADVANCE) {
        return _ActionsForUnitAdvance(state, parent, unit);
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