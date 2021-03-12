using Godot;
using SpaceDodgeRL.library.encounter.rulebook;
using SpaceDodgeRL.library.encounter.rulebook.actions;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.singletons;

namespace SpaceDodgeRL.scenes.encounter {

  public class MenuButtonBar : TextureRect {
    public SceneManager _sceneManager { get => (SceneManager)GetNode("/root/SceneManager"); }
    private EncounterState _state;
    private InputHandler _inputHandler;


    public void SetState(EncounterState state, InputHandler inputHandler) {
      this._state = state;
      this._inputHandler = inputHandler;
    }

    public override void _Ready() {
      this.GetNode<Button>("VBoxContainer/TopButtonBar/RotateButton").Connect("pressed", this, nameof(OnRotateButtonPressed));
      this.GetNode<Button>("VBoxContainer/TopButtonBar/ExitFormationButton").Connect("pressed", this, nameof(OnLeaveFormationButtonPressed));
      this.GetNode<Button>("VBoxContainer/TopButtonBar/WaitButton").Connect("pressed", this, nameof(OnWaitButtonPressed));
      this.GetNode<Button>("VBoxContainer/TopButtonBar/HelpButton").Connect("pressed", this, nameof(OnHelpButtonPressed));
      this.GetNode<Button>("VBoxContainer/BottomButtonBar/ZoomOutButton").Connect("pressed", this, nameof(OnZoomOutButtonPressed));
      this.GetNode<Button>("VBoxContainer/BottomButtonBar/ZoomInButton").Connect("pressed", this, nameof(OnZoomInButtonPressed));
      this.GetNode<Button>("VBoxContainer/BottomButtonBar/ResetZoomButton").Connect("pressed", this, nameof(ResetZoomButtonPressed));
      this.GetNode<Button>("VBoxContainer/BottomButtonBar/EscapeButton").Connect("pressed", this, nameof(OnEscapeButtonPressed));
    }

    private void OnRotateButtonPressed() {
      this._inputHandler.TryInsertInputAction(new InputHandler.InputAction(InputHandler.ActionMapping.ROTATE));
    }

    private void OnLeaveFormationButtonPressed() {
      this._inputHandler.TryInsertInputAction(new InputHandler.InputAction(InputHandler.ActionMapping.LEAVE_FORMATION));
    }

    private void OnWaitButtonPressed() {
      this._inputHandler.TryInsertInputAction(new InputHandler.InputAction(InputHandler.ActionMapping.WAIT));
    }

    private void OnHelpButtonPressed() {
      this._inputHandler.TryInsertInputAction(new InputHandler.InputAction(InputHandler.ActionMapping.HELP_MENU));
    }

    private void OnZoomOutButtonPressed() {
      this._inputHandler.TryInsertInputAction(new InputHandler.InputAction(InputHandler.ActionMapping.ZOOM_OUT));
    }

    private void OnZoomInButtonPressed() {
      this._inputHandler.TryInsertInputAction(new InputHandler.InputAction(InputHandler.ActionMapping.ZOOM_IN));
    }

    private void ResetZoomButtonPressed() {
      this._inputHandler.TryInsertInputAction(new InputHandler.InputAction(InputHandler.ActionMapping.ZOOM_RESET));
    }

    private void OnEscapeButtonPressed() {
      this._sceneManager.ShowEscapeMenu(this._state);
    }
  }
}