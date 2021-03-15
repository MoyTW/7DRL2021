using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using MTW7DRL2021.library.encounter;
using MTW7DRL2021.scenes.components.AI;
using MTW7DRL2021.scenes.encounter.state;
using MTW7DRL2021.scenes.entities;

namespace MTW7DRL2021.scenes.components {

  public class AIRotationComponent : Component {
    public static readonly string ENTITY_GROUP = "AI_ROTATION_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public bool IsRotating { get; private set; }
    [JsonInclude] public int TimesRotated { get; private set; }
    [JsonInclude] public int RotateAtHpThreshold { get; private set; }
    [JsonInclude] public double RotateAtFootingPercentThreshold { get; private set; }
    [JsonInclude] public bool IsPlayer { get; private set; }
    
    public static AIRotationComponent Create(double rotateAtFootingPercentThreshold, bool isPlayer) {
      var component = new AIRotationComponent();

      component.IsRotating = false;
      component.TimesRotated = 0;
      component.RotateAtHpThreshold = -1;
      component.RotateAtFootingPercentThreshold = rotateAtFootingPercentThreshold;
      component.IsPlayer = isPlayer;
      
      return component;
    }

    public static AIRotationComponent Create(string saveData) {
      return JsonSerializer.Deserialize<AIRotationComponent>(saveData);
    }

    public bool BackSecure(EncounterState state, Entity parent, Unit unit) {
      var parentPos = parent.GetComponent<PositionComponent>().EncounterPosition;
      var positionBack = AIUtils.RotateAndProject(parentPos, 0, 1, unit.UnitFacing);
      var friendliesBack = AIUtils.FriendliesInPosition(state, parent, unit.UnitFaction, positionBack.X, positionBack.Y);
      foreach (var friendly in friendliesBack) {
        if (!friendly.IsRotating()) {
          return true;
        }
      }
      return false;
    }

    // Returns true if rotation CHANGED
    public bool DecideIfShouldRotate(Entity parent, bool backSecure) {
      if (this.IsPlayer) { return false; }
      if (!backSecure) { return false; }

      var defender = parent.GetComponent<DefenderComponent>();
      if (this.RotateAtHpThreshold == -1) {
        this.RotateAtHpThreshold = defender.MaxHp * 2 / 3;
      }

      bool underHPThreshold = defender.CurrentHp < this.RotateAtHpThreshold;
      if (underHPThreshold) {
        this.RotateAtHpThreshold = this.RotateAtHpThreshold * 2 / 3;
      }

      bool underFootingThreshold = defender.PercentageFooting < this.RotateAtFootingPercentThreshold;

      if ((underHPThreshold || underFootingThreshold) && !this.IsRotating) {
        this.IsRotating = true;
        return true;
      } else {
        return false;
      }
    }

    public void PlayerSetRotation(bool isRotating) {
      this.IsRotating = isRotating;
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