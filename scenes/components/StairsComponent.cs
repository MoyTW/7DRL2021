using Godot;
using MTW7DRL2021.scenes.entities;
using System;
using System.Text.Json;

namespace MTW7DRL2021.scenes.components {

  public class StairsComponent : Component {
    public static readonly string ENTITY_GROUP = "STAIRS_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    public static StairsComponent Create() {
      var component = new StairsComponent();
      return component;
    }

    public static StairsComponent Create(string saveData) {
      return Create();
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}