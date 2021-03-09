using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.scenes.components {

  public class AIRotationComponent : Component {
    public static readonly string ENTITY_GROUP = "AI_ROTATION_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public bool IsRotating { get; private set; }
    [JsonInclude] public int TimesRotated { get; private set; }
    [JsonInclude] public int RotateAtHpThreshold { get; private set; }
    [JsonInclude] public double RotateAtFootingPercentThreshold { get; private set; }
    
    public static AIRotationComponent Create(double rotateAtFootingPercentThreshold) {
      var component = new AIRotationComponent();

      component.IsRotating = false;
      component.TimesRotated = 0;
      component.RotateAtHpThreshold = -1;
      component.RotateAtFootingPercentThreshold = rotateAtFootingPercentThreshold;
      
      return component;
    }

    public static AIRotationComponent Create(string saveData) {
      return JsonSerializer.Deserialize<AIRotationComponent>(saveData);
    }

    public void DecideIfShouldRotate(Entity parent) {
      var defender = parent.GetComponent<DefenderComponent>();
      if (this.RotateAtHpThreshold == -1) {
        this.RotateAtHpThreshold = defender.MaxHp * 2 / 3;
      }

      bool underHPThreshold = defender.CurrentHp < this.RotateAtHpThreshold;
      if (underHPThreshold) {
        this.RotateAtHpThreshold = this.RotateAtHpThreshold * 2 / 3;
      }

      bool underFootingThreshold = defender.PercentageFooting < this.RotateAtFootingPercentThreshold;

      if (underHPThreshold || underFootingThreshold) {
        this.IsRotating = true;
      }
    }

    public void NotifyRotationCompleted() {
      this.IsRotating = false;
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}