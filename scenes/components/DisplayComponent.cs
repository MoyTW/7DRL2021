using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using MTW7DRL2021.scenes.entities;

namespace MTW7DRL2021.scenes.components {

  public class DisplayComponent : Component {
    public static readonly string ENTITY_GROUP = "SPRITE_DATA_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public string TexturePath { get; private set; }
    [JsonInclude] public string Description { get; private set; }
    [JsonInclude] public bool VisibleInFoW { get; private set; }
    [JsonInclude] public int ZIndex { get; private set; }
    [JsonInclude] public bool Visible { get; private set; }

    public static DisplayComponent Create(string texturePath, string description, bool visibleInFoW, int zIndex, bool visible=true) {
      var component = new DisplayComponent();

      component.TexturePath = texturePath;
      component.Description = description;
      component.VisibleInFoW = visibleInFoW;
      component.ZIndex = zIndex;
      component.Visible = visible;

      return component;
    }

    public static DisplayComponent Create(string saveData) {
      return JsonSerializer.Deserialize<DisplayComponent>(saveData);
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}