using Godot;
using System;

public partial class GameRoot : Node
{
    public bool GamePaused = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        _pauseGame();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _Input(InputEvent @event)
    {
        if(@event.IsActionPressed("ui_cancel"))
        {
            _toggleGameState();
        }
    }

    private void _toggleGameState()
    {
        if(this.GamePaused)
            _resumeGame();
        else
            _pauseGame();
    }

    private void _pauseGame()
    {
        this.GamePaused = true;
        this.GetTree().Paused = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private void _resumeGame()
    {
        this.GamePaused = false;
        this.GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }
}
