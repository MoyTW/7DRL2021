using Godot;
using MTW7DRL2021.scenes.entities;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MTW7DRL2021.scenes.components {

  public class StorableComponent : Component {
    public static readonly string ENTITY_GROUP = "STORABLE_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public int Size { get; private set; }

    public static StorableComponent Create(int size = 1) {
      var component = new StorableComponent();

      component.Size = size;

      return component;
    }

    public static StorableComponent Create (string saveData) {
      return JsonSerializer.Deserialize<StorableComponent>(saveData);
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}