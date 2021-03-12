using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using SpaceDodgeRL.library.encounter;
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

    private string _UnitOrderToActionText(UnitOrder order, bool rotating) {
      if (order == UnitOrder.ADVANCE && !rotating) {
        return "Advancing in formation.";
      } else if (order == UnitOrder.ADVANCE && rotating) {
        return "Rotating to the rear.";
      } else if (order == UnitOrder.REFORM) {
        return "Reforming.";
      } else if (order == UnitOrder.RETREAT) {
        return "Disengaging in formation.";
      } else if (order == UnitOrder.ROUT) {
        return "You should never see this, since you should get booted out of the unit as soon as the rout starts!";
      } else if (order == UnitOrder.WITHDRAW) {
        return "Withdrawing in good order.";
      } else {
        throw new NotImplementedException();
      }
    }

    private static Dictionary<string, string> _actionToReadableString = new Dictionary<string, string>() {
      { InputHandler.ActionMapping.WAIT, "Wait (Space, N5)" },
      { InputHandler.ActionMapping.ROTATE, "Rotate (r)" },
      { InputHandler.ActionMapping.LEAVE_FORMATION, "Flee (x)" },
      { InputHandler.ActionMapping.MOVE_N, "N (h, N8)" },
      { InputHandler.ActionMapping.MOVE_NE, "NE (u, N9)" },
      { InputHandler.ActionMapping.MOVE_E, "E (l, N6)" },
      { InputHandler.ActionMapping.MOVE_SE, "SE (n, N3)" },
      { InputHandler.ActionMapping.MOVE_S, "S (j, N2)" },
      { InputHandler.ActionMapping.MOVE_SW, "SW (b, N1)" },
      { InputHandler.ActionMapping.MOVE_W, "W (k, N4)" },
      { InputHandler.ActionMapping.MOVE_NW, "NW (y, N7)" },
    };

    private static HashSet<string> _moveActions = new HashSet<string>() {
      InputHandler.ActionMapping.MOVE_N,
      InputHandler.ActionMapping.MOVE_NE,
      InputHandler.ActionMapping.MOVE_E,
      InputHandler.ActionMapping.MOVE_SE,
      InputHandler.ActionMapping.MOVE_S,
      InputHandler.ActionMapping.MOVE_SW,
      InputHandler.ActionMapping.MOVE_W,
      InputHandler.ActionMapping.MOVE_NW
    };

    private void OnPlayerTurnStarted() {
      var player = this.EncounterState.Player;
      var playerComponent = player.GetComponent<PlayerComponent>();

      if (this.EncounterState.RunStatus == EncounterState.RUN_STATUS_ARMY_DEFEAT) {
        this.GetNode<Label>("CanvasLayer/DefeatText").Show();
      } else if (this.EncounterState.RunStatus == EncounterState.RUN_STATUS_ARMY_VICTORY) {
        this.GetNode<Label>("CanvasLayer/VictoryText").Show();
      } else {
        this.GetNode<Label>("CanvasLayer/VictoryText").Hide();
        this.GetNode<Label>("CanvasLayer/DefeatText").Hide();
      }

      if (playerComponent.IsInFormation) {
        var unit = this.EncounterState.GetUnit(player.GetComponent<UnitComponent>().UnitId);
        if (unit.StandingOrder == library.encounter.UnitOrder.ROUT) {
          playerComponent.LeaveFormation(this.EncounterState, player);
        } else {
          var formationText = this.GetNode<Label>("CanvasLayer/FormationText");
          formationText.Show();
          var order = this.EncounterState.GetUnit(player.GetComponent<UnitComponent>().UnitId).StandingOrder;
          var actions = player.GetComponent<PlayerAIComponent>().AllowedActions(this.EncounterState, player, order);
          var actionText = _UnitOrderToActionText(unit.StandingOrder, player.GetComponent<AIRotationComponent>().IsRotating);

          var moveStrings = actions.Where((s) => _moveActions.Contains(s)).Select((s) => _actionToReadableString[s]);
          var nonMoveStrings = actions.Where((s) => !_moveActions.Contains(s)).Select((s) => _actionToReadableString[s]);

          formationText.Text = String.Format("{0}\nApproved Moves: {1}\n{2}", actionText, String.Join(", ", moveStrings), String.Join(" ", nonMoveStrings));
        }
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