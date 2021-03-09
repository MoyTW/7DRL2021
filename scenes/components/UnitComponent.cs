using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.scenes.components {

  public class UnitComponent : Component {
    public static readonly string ENTITY_GROUP = "UNIT_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public string UnitId { get; private set; }
    [JsonInclude] public int FormationNumber { get; private set; }
    
    public static UnitComponent Create(string unitId, int formationNumber) {
      var component = new UnitComponent();

      component.UnitId = unitId;
      component.FormationNumber = formationNumber;
      
      return component;
    }

    public static UnitComponent Create(string saveData) {
      return JsonSerializer.Deserialize<UnitComponent>(saveData);
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}