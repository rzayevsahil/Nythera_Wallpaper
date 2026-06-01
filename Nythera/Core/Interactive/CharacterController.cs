using System;
using Nythera.Core.Interactive.Models;

namespace Nythera.Core.Interactive;

public class CharacterController
{
    private readonly MouseTrackerService _mouseTracker;
    public CharacterAction CurrentAction { get; private set; } = CharacterAction.Idle;
    public event EventHandler<CharacterAction>? ActionChanged;

    public CharacterController(MouseTrackerService mouseTracker)
    {
        _mouseTracker = mouseTracker;
        _mouseTracker.MouseMoved += OnMouseMoved;
    }

    private void OnMouseMoved(object? sender, MousePosition position)
    {
        // MVP: Simple left/right looking logic based on screen center
        // Assume 1920x1080 as a rough center for V1
        double centerX = 1920 / 2.0;

        CharacterAction newAction = position.X < centerX ? CharacterAction.LookLeft : CharacterAction.LookRight;

        if (CurrentAction != newAction)
        {
            CurrentAction = newAction;
            ActionChanged?.Invoke(this, CurrentAction);
        }
    }
}
