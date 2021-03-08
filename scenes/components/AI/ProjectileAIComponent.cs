using Godot;
using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.library.encounter.rulebook;
using SpaceDodgeRL.library.encounter.rulebook.actions;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceDodgeRL.scenes.components.AI {

  public class ProjectileAIComponent : AIComponent {
    public static readonly string ENTITY_GROUP = "PATH_AI_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public EncounterPath Path { get; private set; }
    [JsonInclude] public string TargetId { get; private set; }

    public static ProjectileAIComponent Create(EncounterPath path, string targetId) {
      var component = new ProjectileAIComponent();

      component.Path = path;
      component.TargetId = targetId;

      return component;
    }

    public static ProjectileAIComponent Create(string saveData) {
      return JsonSerializer.Deserialize<ProjectileAIComponent>(saveData);
    }

    public List<EncounterAction> DecideNextAction(EncounterState state, Entity parent) {
      if (Path.AtEnd) {
        var actions = new List<EncounterAction>();

        var target = state.GetEntityById(this.TargetId);
        if (target != null) {
          actions.Add(new RangedAttackAction(parent.EntityId, state.GetEntityById(this.TargetId)));
        }
        
        actions.Add(new DestroyAction(parent.EntityId));
        return actions; ;
      } else {
        var nextPosition = Path.Step();
        return new List<EncounterAction>() { new MoveAction(parent.EntityId, nextPosition) };
      }
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}