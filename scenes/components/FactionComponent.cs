using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.scenes.components {

  public enum FactionName {
    PLAYER,
    ENEMY
  }

  public class FactionComponent : Component {
    public static readonly string ENTITY_GROUP = "FACTION_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public FactionName Faction { get; private set; }
    
    public static FactionComponent Create(FactionName faction) {
      var component = new FactionComponent();

      component.Faction = faction;
      
      return component;
    }

    public static FactionComponent Create(string saveData) {
      return JsonSerializer.Deserialize<FactionComponent>(saveData);
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}