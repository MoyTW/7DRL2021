using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.scenes.components {

  public enum MoraleState {
    CONFIDENT = 4, // pushes forwards
    STEADY = 3,    // acts normally
    WAVERING = 2,  // never pushes forwards
    BREAKING = 1,  // 50% chance to withdraw
    FLEEING = 0    // flees
  }

  public class AIMoraleComponent : Component {
    public static readonly string ENTITY_GROUP = "AI_Morale_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public int MaxMorale { get; private set; }
    [JsonInclude] public int CurrentMorale { get; private set; }
    [JsonIgnore] public double PercentageMorale { get => (double)this.CurrentMorale / (double)this.MaxMorale; }
    [JsonIgnore] public MoraleState CurrentMoraleState { get {
      if (this.PercentageMorale >= .7) {
        return MoraleState.CONFIDENT;
      } else if (this.PercentageMorale >= .5) {
        return MoraleState.STEADY;
      } else if (this.PercentageMorale >= .3) {
        return MoraleState.WAVERING;
      } else if (this.PercentageMorale >= .2) {
        return MoraleState.BREAKING;
      } else {
        return MoraleState.FLEEING;
      }
    } }
    
    public static AIMoraleComponent Create(int maxMorale, int startingMorale) {
      var component = new AIMoraleComponent();

      component.MaxMorale = maxMorale;
      component.CurrentMorale = startingMorale;
      
      return component;
    }

    public static AIMoraleComponent Create(string saveData) {
      return JsonSerializer.Deserialize<AIMoraleComponent>(saveData);
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}