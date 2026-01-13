using Godot;
using System;

public partial class Deathbox : Area3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        this.BodyEntered += _onBodyEntered;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    private void _onBodyEntered(Node3D body)
    {
        if(body is Player player)
        {
            player.OnDeathboxEntered(body);
        }
    }
}
