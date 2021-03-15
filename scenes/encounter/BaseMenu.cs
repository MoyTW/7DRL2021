using Godot;

namespace MTW7DRL2021.scenes.encounter {
  abstract public class BaseMenu : Control {
    private bool _currentlyHovered;
    private Button _lastHovered;

    protected void RegisterHoverable(Node hoverable) {
      hoverable.Connect("mouse_entered", this, nameof(OnMouseEntered), new Godot.Collections.Array() { hoverable });
      hoverable.Connect("mouse_exited", this, nameof(OnMouseExited));
    }

    public override void _Input(InputEvent @event) {
      if (@event is InputEventMouseMotion && Input.GetMouseMode() != Input.MouseMode.Visible) {
        Input.SetMouseMode(Input.MouseMode.Visible);
        this.GetFocusOwner().ReleaseFocus();
        if (this._currentlyHovered) {
          this._lastHovered.GrabFocus();
        }
      } else if (@event is InputEventKey && Input.GetMouseMode() == Input.MouseMode.Visible) {
        if (this.GetFocusOwner() == null && this._lastHovered != null) {
          this._lastHovered.GrabFocus();
        } else if (this.GetFocusOwner() == null && this._lastHovered == null) {
          HandleNeedsFocusButNoFocusSet();
        }
      }
    }

    public abstract void HandleNeedsFocusButNoFocusSet();

    private void OnMouseEntered(Button entered) {
      this._currentlyHovered = true;
      this._lastHovered = entered;
    }

    private void OnMouseExited() {
      this._currentlyHovered = false;
    }
  }
}