using System;
using Godot;
using SpaceDodgeRL.scenes.components;
using SpaceDodgeRL.scenes.components.AI;
using SpaceDodgeRL.scenes.encounter;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.scenes {

  public class EncounterScene : Container {
    public EncounterState EncounterState { get; private set; }
    private Viewport encounterViewport;
    public InputHandler inputHandler;
    private EncounterRunner encounterRunner;

    public override void _Ready() {
      this.encounterViewport = GetNode<Viewport>("SceneFrame/SceneVBox/EncounterViewportContainer/EncounterViewport");

      this.inputHandler = GetNode<InputHandler>("InputHandler");

      this.encounterRunner = GetNode<EncounterRunner>("EncounterRunner");
      this.encounterRunner.inputHandlerRef = this.inputHandler;

      if (this.EncounterState == null) {
        throw new NotImplementedException("must call SetEncounterState before adding to tree");
      }
      this.encounterViewport.AddChild(this.EncounterState);
      this.encounterRunner.SetEncounterState(this.EncounterState);
      this.GetNode<MenuButtonBar>("SceneFrame/SceneVBox/MenuButtonBar").SetState(this.EncounterState, inputHandler);

      // Hook up the UI
      this.EncounterState.Connect(nameof(EncounterState.EncounterLogMessageAdded), this, nameof(OnEncounterLogMessageAdded));
      this.encounterRunner.Connect(nameof(EncounterRunner.TurnEnded), this, nameof(OnTurnEnded));
      this.encounterRunner.Connect(nameof(EncounterRunner.PlayerTurnStarted), this, nameof(OnPlayerTurnStarted));
      // TODO: Add keyboard look via "s"
      this.encounterRunner.Connect(nameof(EncounterRunner.PositionScanned), this, nameof(OnPositionScanned));
      var viewportContainer = GetNode<ViewportContainer>("SceneFrame/SceneVBox/EncounterViewportContainer");
      viewportContainer.Connect(nameof(EncounterViewportContainer.MousedOverPosition), this, nameof(OnMousedOverPosition));
      viewportContainer.Connect(nameof(EncounterViewportContainer.ActionSelected), this, nameof(OnActionSelected));
      // Since we can't have the state broadcast its events before we connect, we instead pull log messages; this will be empty
      // on new game and populated on load.
      foreach (var logMessage in this.EncounterState.EncounterLog) {
        this.OnEncounterLogMessageAdded(logMessage, int.MaxValue);
      }

      OnTurnEnded();
    }

    /**
     * Must be called once and only once, before being added to the scene tree.
     */
    public void SetEncounterState(EncounterState state) {
      if (this.EncounterState != null) {
        throw new NotImplementedException("can't call SetEncounterState twice");
      }

      this.EncounterState = state;
    }

    private void OnEncounterLogMessageAdded(string bbCodeMessage, int encounterLogSize) {
      this.GetNode<SidebarDisplay>("SceneFrame/SidebarDisplay").AddEncounterLogMessage(bbCodeMessage, encounterLogSize);
    }

    private void OnPlayerTurnStarted() {
      var player = this.EncounterState.Player;
      var playerComponent = player.GetComponent<PlayerComponent>();

      if (playerComponent.IsInFormation) {
        var formationText = this.GetNode<Label>("CanvasLayer/FormationText");
        formationText.Show();
        var order = this.EncounterState.GetUnit(player.GetComponent<UnitComponent>().UnitId).StandingOrder;
        var actions = player.GetComponent<PlayerAIComponent>().AllowedActions(this.EncounterState, player, order);
        formationText.Text = "In formation. Approved actions: " + String.Join(",", actions);
      }
    }

    private void OnTurnEnded() {
      this.GetNode<SidebarDisplay>("SceneFrame/SidebarDisplay").RefreshStats(this.EncounterState);
      this.GetNode<Label>("CanvasLayer/FormationText").Hide();
    }

    private void OnPositionScanned(int x, int y, Entity entity) {
      this.GetNode<SidebarDisplay>("SceneFrame/SidebarDisplay").DisplayScannedEntity(x, y, entity);
    }

    private void OnMousedOverPosition(int x, int y) {
      this.inputHandler.TryInsertInputAction(new InputHandler.ScanInputAction(x, y));
    }

    private void OnActionSelected(string actionMapping) {
      this.inputHandler.TryInsertInputAction(new InputHandler.InputAction(actionMapping));
    }

    // TODO: The many layers of indirection for these menus are vexing but feature-complete first
    public void HandleItemToUseSelected(string itemIdToUse) {
      encounterRunner.HandleUseItemSelection(itemIdToUse);
    }

    // This could probably be a signal.
    public void HandleLevelUpSelected(Entity entity, string levelUpSelection) {
      EncounterState.Player.GetComponent<XPTrackerComponent>().RegisterLevelUpChoice(entity, levelUpSelection);
    }
  }
}