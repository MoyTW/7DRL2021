using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using MTW7DRL2021.scenes.entities;

namespace MTW7DRL2021.scenes.components {

  public static class FactionNameExtensions {
    public static FactionName Opposite(this FactionName faction) {
      if (faction == FactionName.PLAYER) {
        return FactionName.ENEMY;
      } else if (faction == FactionName.ENEMY) {
        return FactionName.PLAYER;
      } else {
        throw new NotImplementedException();
      }
    }
  }
  public enum FactionName {
    PLAYER,
    ENEMY,
    NEUTRAL
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