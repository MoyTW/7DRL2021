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

    private static string[] Quips = new string[] {
      "Hey has anybody seen my posca?",
      "Huphuphuphuphup!",
      "Oy move over, you're poking my thigh.",
      "Damm, I dropped my sword.",
      "We're gonna win, right?",
      "FOR ROME!",
      "(shut up we're supposed to be silent when we form up!)",
      "(...so...we're even right?)",
      "If I die, tell me wife...",
      "Just one more battle 'till retirement.",
      "...always told me, son, you're...",
      "...YOUR MATER! No? No?",
      "Who's that guy?",
      "Man this belt sure chafes.",
      "These boots are stiff.",
      "Ugh it always has to start at lunchtime.",
      "Dammit I stubbed my toe.",
      "This is where the fun begins!",
      "Time to go win some glory, boys!",
      "You hear the one about the Spartan and the slave?",
      "...later. You won't have time...",
      "...never told him...",
      "...then you should've used the bathroom before!",
      "Step back a bit buddy!",
      "It's too hot.",
      "It's too cold.",
      "I wish it were raining.",
      "...and he said to my daughter...",
      "...my brother was, of course, livid, and...",
      "...I'm gonna go buy so much wine!",
      "...gonna do with the loot?",
      "Fast, I'm gonna go there real...",
      "Brother, I am pinned here!",
      "One day I'll go back and show him...",
      "I never realized how terrifying the waiting would be.",
      "Ready to get stuck in!",
      "Boys we at war now!",
      "Shout VICTORY on three! One, two...!",
      "...you're too loud.",
      "Lighten up. Hey, you heard...",
      "Hey, is that my brother over in that other army...?"
    };

    public override List<EncounterAction> _DecideNextAction(EncounterState state, Entity parent) {
      var unit = state.GetUnit(parent.GetComponent<UnitComponent>().UnitId);
      var unitComponent = parent.GetComponent<UnitComponent>();

      if (unit.StandingOrder == UnitOrder.REFORM) {
        if (state.CurrentTurn < EncounterStateBuilder.ADVANCE_AT_TURN) {
            if (state.EncounterRand.Next(750) == 0) {
              parent.GetComponent<PositionComponent>().PlaySpeechBubble(Quips[state.EncounterRand.Next(Quips.Length)]);
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