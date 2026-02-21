using Godot;
using System;

public partial class Door : StaticBody3D
{
	[Export]
	public bool OpenDefault = false;
	[Export]
	public Vector3 OpenVelocity = new(0, 3, 0);
	[Export]
	public float OpenDistance = 6;
	
	private bool _open = false;
	private DoorAction _action = DoorAction.IDLE;
	private Vector3 _actionStartPos;
	private float _openDistanceSquared;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if(OpenDefault)
		{
			_open = true;
		}
		_openDistanceSquared = OpenDistance * OpenDistance;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if(_action == DoorAction.OPENING)
		{
			Vector3 fromStartPos = this.Position - _actionStartPos;
			float distanceSquared = fromStartPos.LengthSquared();
			if(distanceSquared >= _openDistanceSquared)
			{
				this.Position = _actionStartPos + OpenVelocity.Normalized() * OpenDistance;
				_action = DoorAction.IDLE;
			}
			else
			{
				this.Position += OpenVelocity * (float)delta;
			}
		}
		else if(_action == DoorAction.CLOSING)
		{
			Vector3 fromStartPos = this.Position - _actionStartPos;
			float distanceSquared = fromStartPos.LengthSquared();
			if(distanceSquared >= _openDistanceSquared)
			{
				this.Position = _actionStartPos - OpenVelocity.Normalized() * OpenDistance;
				_action = DoorAction.IDLE;
			}
			else
			{
				this.Position -= OpenVelocity * (float)delta;
			}
		}
	}
	
	public void Open(StringName areaKey)
	{
		Open();
	}
	
	public void Close(StringName areaKey)
	{
		Close();
	}
	
	public void Open()
	{
		if(_open == true)
		{
			return;
		}
		if(_action != DoorAction.IDLE)
		{
			return;
		}
		_open = true;
		_action = DoorAction.OPENING;
		_actionStartPos = this.Position;
	}
	
	public void Close()
	{
		if(_open == false)
		{
			return;
		}
		if(_action != DoorAction.IDLE)
		{
			return;
		}
		_open = false;
		_action = DoorAction.CLOSING;
		_actionStartPos = this.Position;
	}
}

enum DoorAction
{
	IDLE,
	OPENING,
	CLOSING
}
