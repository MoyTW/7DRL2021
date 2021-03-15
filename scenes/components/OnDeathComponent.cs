using Godot;
using MTW7DRL2021.scenes.entities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MTW7DRL2021.scenes.components {

  public static class OnDeathEffectType {
    public static string PLAYER_VICTORY = "ON_DEATH_EFFECT_TYPE_PLAYER_VICTORY";
    public static string PLAYER_DEFEAT = "ON_DEATH_EFFECT_TYPE_PLAYER_DEFEAT";
    public static string REMOVE_FROM_UNIT = "ON_DEATH_EFFECT_REMOVE_FROM_UNIT";
  }

  public class OnDeathComponent : Component {
    public static readonly string ENTITY_GROUP = "ON_DEATH_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public List<string> ActiveEffectTypes { get; private set; } = new List<string>();

    public static OnDeathComponent Create(List<string> activeEffectTypes) {
      var component = new OnDeathComponent();

      component.ActiveEffectTypes = activeEffectTypes;

      return component;
    }

    public static OnDeathComponent Create(string saveData) {
      return JsonSerializer.Deserialize<OnDeathComponent>(saveData);
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}