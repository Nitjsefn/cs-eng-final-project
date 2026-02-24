using Godot;
using System;

public partial class MomentumBossArena : Node3D
{
	private Node3D laserWall;
	private Node3D laserWall2;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		laserWall = this.GetNode<Node3D>("laserWallNode3D");
		laserWall2 = this.GetNode<Node3D>("laserWallNode3D2");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void OnBossDestroyed()
	{
		laserWall.Visible = false;
		laserWall.GetTree().Paused = true;
		laserWall2.Visible = false;
		laserWall2.GetTree().Paused = true;
	}
}
