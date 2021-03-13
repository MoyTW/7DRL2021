using Godot;
using SpaceDodgeRL.scenes.singletons;
using System;

namespace SpaceDodgeRL.scenes.encounter {
  public class IntroFormUpMenu : VBoxContainer {
    
    private Button _closeButton;

    public override void _Ready() {
      var button = GetNode<Button>("Button");
      button.Connect("pressed", this, nameof(OnCloseButtonPressed));
      button.Connect("tree_entered", this, nameof(OnTreeEntered));
    }

    private void OnTreeEntered() {
      _closeButton.GrabFocus();
    }

    private void OnCloseButtonPressed() {
      var sceneManager = (SceneManager)GetNode("/root/SceneManager");
      sceneManager.ReturnToPreviousScene();
    }
  }
}