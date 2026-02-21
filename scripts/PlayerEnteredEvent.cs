using Godot;
using System;

public partial class PlayerEnteredEvent : Area3D
{
	private GameRoot _gameRoot;
	[Export]
	public StringName AreaKey;
	
	[Signal]
	public delegate void PlayerEnteredSignalEventHandler(StringName areaKey);
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		FetchGameRoot();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	private bool FetchGameRoot()
	{
		if(_gameRoot == null)
		{
			_gameRoot = this.GetNodeOrNull<GameRoot>("/root/GameRoot");
			if(_gameRoot == null)
			{
				return false;
			}
		}
		return true;
	}
	
	public void OnBodyEntered(Node3D body)
	{
		if(_gameRoot == null)
		{
			if(FetchGameRoot() == false)
			{
				return;
			}
		}
		if(body is Player)
		{
			_gameRoot.OnPlayerEnteredArea(AreaKey);
			this.EmitSignalPlayerEnteredSignal(AreaKey);
		}
	}
}
