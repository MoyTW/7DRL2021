using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.library.encounter.rulebook;
using SpaceDodgeRL.library.encounter.rulebook.actions;
using SpaceDodgeRL.scenes.components;
using SpaceDodgeRL.scenes.components.AI;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;
using SpaceDodgeRL.scenes.singletons;

namespace SpaceDodgeRL.scenes.encounter {
  public class EncounterRunner : Node {

    // We use this to determine whether the UI should refresh; this will cause a comical number of UI updates since we'll be
    // sending it every end turn, but I don't want to have to go in and effectively instrument all my state changes to set up
    // "change on data update" changes.
    [Signal] public delegate void PlayerTurnStarted();
    [Signal] public delegate void TurnEnded();
    [Signal] public delegate void PositionScanned(int x, int y, Entity scannedEntity);

    public InputHandler inputHandlerRef = null;
    public SceneManager _sceneManager { get => (SceneManager)GetNode("/root/SceneManager"); }

    private EncounterState _encounterState;
    private GameSettings _gameSettings;
    public void SetEncounterState(EncounterState encounterState) {
      this._encounterState = encounterState;
      this._gameSettings = (GameSettings)GetNode("/root/GameSettings");
    }

    private float msUntilTurn = 0;

    public override void _Process(float delta) {
      // If it's the player's turn, and there are no animating sprites, we don't need to wait on input for the whole timer
      var isReadyForPlayerInput = IsPlayerTurn(this._encounterState) && !this._encounterState.HasAnimatingSprites;

      if (msUntilTurn <= 0 || isReadyForPlayerInput) {
        RunTurn(this._encounterState, inputHandlerRef);
        msUntilTurn = this._gameSettings.TurnTimeMs / 1000f;
      } else {
        msUntilTurn -= delta;
      }
    }

    // TODO: Write a system that has a component which does this instead of hard-coding it into the player's turn end
    private void PlayerExecuteTurnEndingAction(EncounterAction action, EncounterState state) {
      var player = state.Player;

      bool actionResolvedSuccessfully = Rulebook.ResolveAction(action, state);
      if (!actionResolvedSuccessfully) {
        return;
      }

      Rulebook.ResolveEndTurn(player.EntityId, state);

      // After the player executes their turn we need to update the UI
      EmitSignal(nameof(EncounterRunner.TurnEnded));
      state.UpdatePlayerOverlays();
    }

    private void PlayerMove(EncounterState state, int dx, int dy) {
      var positionComponent = state.Player.GetComponent<PositionComponent>();
      var oldPos = positionComponent.EncounterPosition;
      var moveAction = new MoveAction(state.Player.EntityId, new EncounterPosition(oldPos.X + dx, oldPos.Y + dy));
      PlayerMove(state, moveAction);
    }

    private void PlayerMove(EncounterState state, MoveAction moveAction) {
      PlayerExecuteTurnEndingAction(moveAction, state);
    }

    private void PlayerWait(EncounterState state) {
      var waitAction = new WaitAction(state.Player.EntityId);
      PlayerExecuteTurnEndingAction(waitAction, state);
    }

    private bool IsPlayerTurn(EncounterState state) {
      return state.NextEntity.IsInGroup(PlayerComponent.ENTITY_GROUP);
    }

    private static void HandleScanAction(EncounterState state, EncounterRunner runner, InputHandler.InputAction action) {
      var scanAction = action as InputHandler.ScanInputAction;
      if (!state.IsInBounds(scanAction.X, scanAction.Y)) {
        return;
      }

      var blockingEntity = state.BlockingEntityAtPosition(scanAction.X, scanAction.Y);
      var allEntities = state.EntitiesAtPosition(scanAction.X, scanAction.Y);
      if (blockingEntity != null) {
        runner.EmitSignal(nameof(PositionScanned), scanAction.X, scanAction.Y, blockingEntity);
      } else if (allEntities.Count > 0) {
        runner.EmitSignal(nameof(PositionScanned), scanAction.X, scanAction.Y, allEntities[0]);
      } else {
        runner.EmitSignal(nameof(PositionScanned), scanAction.X, scanAction.Y, null);
      }
    }

    private static void HandleClaimVictoryAction(EncounterState state, EncounterRunner runner, InputHandler.InputAction action) {
      if (state.RunStatus == EncounterState.RUN_STATUS_ARMY_VICTORY) {
        state.ResetStateForNewLevel(state.Player, state.DungeonLevel + 1);
        state.WriteToFile();
      }
    }

    private static Dictionary<string, Action<EncounterState, EncounterRunner, InputHandler.InputAction>> AlwaysAvaiableActionMappingToActionDict =
      new Dictionary<string, Action<EncounterState, EncounterRunner, InputHandler.InputAction>>()
    {
      { InputHandler.ActionMapping.CHARACTER, (s, r, a) => r._sceneManager.ShowCharacterMenu(s) },
      { InputHandler.ActionMapping.ESCAPE_MENU, (s, r, a) => r._sceneManager.ShowEscapeMenu(s) },
      { InputHandler.ActionMapping.HELP_MENU, (s, r, a) => r._sceneManager.ShowHelpMenu() },
      { InputHandler.ActionMapping.INVENTORY, (s, r, a) => r._sceneManager.ShowInventoryMenu(s) },
      { InputHandler.ActionMapping.ZOOM_IN, (s, r, a) => s.ZoomIn() },
      { InputHandler.ActionMapping.ZOOM_OUT, (s, r, a) => s.ZoomOut() },
      { InputHandler.ActionMapping.ZOOM_RESET, (s, r, a) => s.ZoomReset() },
      { InputHandler.ActionMapping.SCAN_POSITION, HandleScanAction },
      { InputHandler.ActionMapping.CLAIM_VICTORY, HandleClaimVictoryAction },
    };

    private static Dictionary<string, Action<EncounterState, EncounterRunner, InputHandler.InputAction>> FreeMovementActionMappingToActionDict =
      new Dictionary<string, Action<EncounterState, EncounterRunner, InputHandler.InputAction>>()
    {
      { InputHandler.ActionMapping.MOVE_N, (s, r, a) => r.PlayerMove(s, 0, -1) },
      { InputHandler.ActionMapping.MOVE_NE, (s, r, a) => r.PlayerMove(s, 1, -1) },
      { InputHandler.ActionMapping.MOVE_E, (s, r, a) => r.PlayerMove(s, 1, 0) },
      { InputHandler.ActionMapping.MOVE_SE, (s, r, a) => r.PlayerMove(s, 1, 1) },
      { InputHandler.ActionMapping.MOVE_S, (s, r, a) => r.PlayerMove(s, 0, 1) },
      { InputHandler.ActionMapping.MOVE_SW, (s, r, a) => r.PlayerMove(s, -1, 1) },
      { InputHandler.ActionMapping.MOVE_W, (s, r, a) => r.PlayerMove(s, -1, 0) },
      { InputHandler.ActionMapping.MOVE_NW, (s, r, a) => r.PlayerMove(s, -1, -1) },
      { InputHandler.ActionMapping.WAIT, (s, r, a) => r.PlayerWait(s) },
    };

    private void RunTurn(EncounterState state, InputHandler inputHandler) {
      if (state.RunStatus == EncounterState.RUN_STATUS_PLAYER_DEFEAT) {
        this._sceneManager.ShowDefeatMenu(state);
        return;
      } else if (state.RunStatus == EncounterState.RUN_STATUS_PLAYER_VICTORY) {
        this._sceneManager.ShowVictoryMenu(state);
        return;
      }

      Action<EncounterState, EncounterRunner> lol = (s, runner) => runner._sceneManager.ShowCharacterMenu(s);

      var entity = state.NextEntity;
      var actionTimeComponent = entity.GetComponent<ActionTimeComponent>();

      if (entity.IsInGroup(PlayerComponent.ENTITY_GROUP)) {
        // We force the player to pick a level-up if they have any available.
        if (entity.GetComponent<XPTrackerComponent>().UnusedLevelUps.Count > 0) {
          this._sceneManager.ShowCharacterMenu(state);
        }

        // Update the player options text
        EmitSignal(nameof(EncounterRunner.PlayerTurnStarted));
        
        var action = inputHandler.PopQueue();

        // Autopilot section
        var playerComponent = entity.GetComponent<PlayerComponent>();
        var playerUnit = state.GetUnit(entity.GetComponent<UnitComponent>().UnitId);
        var playerAI = entity.GetComponent<PlayerAIComponent>();
        // Autopilot if beginning
        if (playerComponent.StartOfLevel) {
          var commands = playerAI.DecideNextActionForInput(state, entity, InputHandler.ActionMapping.WAIT);
          if (commands != null) { 
            Rulebook.ResolveActionsAndEndTurn(commands, state);
            EmitSignal(nameof(EncounterRunner.TurnEnded));
            state.UpdatePlayerOverlays();
          } else {
            return;
          }
        }
        // Autopilot if allowed actions are LEAVE & WAIT
        var playerAllowedActions = playerAI.AllowedActions(state, entity, playerUnit.StandingOrder);
        GD.Print("NUM ALLOWED ACTIONS ", playerAllowedActions.Count);
        if (playerAllowedActions.Count == 2) { // You can always wait or leave the formation
          var commands = playerAI.DecideNextActionForInput(state, entity, InputHandler.ActionMapping.WAIT);
          if (commands != null) { 
            Rulebook.ResolveActionsAndEndTurn(commands, state);
            EmitSignal(nameof(EncounterRunner.TurnEnded));
            state.UpdatePlayerOverlays();
          } else {
            return;
          }
        }

        // Super not a fan of the awkwardness of checking this twice! Switch string -> enum, maybe?
        if (action != null && AlwaysAvaiableActionMappingToActionDict.ContainsKey(action.Mapping)) {
          AlwaysAvaiableActionMappingToActionDict[action.Mapping].Invoke(state, this, action);
        } else if (action != null && !playerComponent.IsInFormation && FreeMovementActionMappingToActionDict.ContainsKey(action.Mapping) ) {
          FreeMovementActionMappingToActionDict[action.Mapping].Invoke(state, this, action);
        } else if (action != null && playerComponent.IsInFormation) {
          var commands = playerAI.DecideNextActionForInput(state, entity, action.Mapping);

          if (commands != null) { 
            Rulebook.ResolveActionsAndEndTurn(commands, state);
            EmitSignal(nameof(EncounterRunner.TurnEnded));
            state.UpdatePlayerOverlays();
          } else {
            return;
          }
        } else if (action != null) {
          GD.Print("No handler yet for ", action);
        }
      } else {
        var playerPos = state.Player.GetComponent<PositionComponent>().EncounterPosition;

        int maxTurnsToRun = 1000;
        int numTurnsRan = 0;

        var firstEntity = entity;
        while (!entity.IsInGroup(PlayerComponent.ENTITY_GROUP) && numTurnsRan < maxTurnsToRun) {
          AIComponent aIComponent = entity.GetComponent<AIComponent>();
          StatusEffectTrackerComponent statusTracker = entity.GetComponent<StatusEffectTrackerComponent>();

          List<EncounterAction> aIActions;
          if (statusTracker != null && statusTracker.HasDisabledEffect()) {
            aIActions = new List<EncounterAction>() { new WaitAction(entity.EntityId) };
          } else {
            aIActions = aIComponent.DecideNextAction(state, entity);
          }
          Rulebook.ResolveActionsAndEndTurn(aIActions, state);
          EmitSignal(nameof(EncounterRunner.TurnEnded));

          entity = state.NextEntity;
          numTurnsRan += 1;

          // Note that the special cases below break the "every turn takes the same amount of real time" rule - some turns will
          // be shorter (travel & no projectiles at all) and some will be longer (player engaged in combat & taking a bunch of
          // hits). I think this is fine because otherwise visual feedback becomes difficult (for example, player takes 3
          // different hits - if they resolved at exact same time it'd be hard to tell) but it's also kinda bad for "flow".

          // Special-case for 0-speed entities; if the next entity is 0-speed, start a new frame. If the entity which started
          // the frame is 0-speed, continue resolving it until it has fully resolved, then start a new frame. This ensures that
          // you can see the start and end of the 0-frame entity's Tween. It also causes a discontinuity in the danger map, since
          // the danger map instantly resolves while Tweens don't, (TODO that) and means that each 0-speed action takes TWO
          // entire turns.
          if (entity.GetComponent<SpeedComponent>().Speed == 0 && entity != firstEntity) {
            break;
          } else if (firstEntity.GetComponent<SpeedComponent>().Speed == 0 && entity != firstEntity) {
            break;
          }
          // Special case for projectiles which are about to hit the player - always start a new turn for these so the player can
          // see what's hitting them.
          var pathAIComponent = entity.GetComponent<ProjectileAIComponent>();
          if (pathAIComponent != null && pathAIComponent.Path.Project(1).Any(p => p == playerPos)) {
            break;
          }
        }
      }
    }

    public void HandleUseItemSelection(string itemIdToUse) {
      var playerId = this._encounterState.Player.EntityId;
      PlayerExecuteTurnEndingAction(new UseAction(playerId, itemIdToUse, true), this._encounterState);
    }
  }
}