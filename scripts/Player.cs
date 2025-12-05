using Godot;
using System;

public partial class Player : CharacterBody3D
{
    private Vector3 _gravityAcceleration = new Vector3(0, -10, 0);
    private float _acceleration = 10;
    private float _maxAcceleratedSpeed = 10;
    private float _maxAcceleratedSpeedSquared;
    private float _deacceleration = 15;
    private Camera3D _camera;


	public override void _Ready()
	{
        _maxAcceleratedSpeedSquared = (float)Math.Pow(_maxAcceleratedSpeed, 2);
        this.Velocity = new Vector3(0, 0, 0);
        this.UpDirection = Vector3.Up;
        _camera = (Camera3D)this.GetNode("Camera3D");
	}

	public override void _Process(double delta)
	{
        if(this.IsOnFloor() || this.IsOnWall())
        {
            Vector2 horizontalVelocity = new Vector2(this.Velocity.X, this.Velocity.Z);
            bool motionPressed = false;
            if(Input.IsActionPressed("forward"))
            {
                motionPressed = true;
                horizontalVelocity += Vector2.Up.Rotated(-_camera.Rotation.Y) * _acceleration * (float)delta;
            }
            if(Input.IsActionPressed("back"))
            {
                motionPressed = true;
                horizontalVelocity += Vector2.Down.Rotated(-_camera.Rotation.Y) * _acceleration * (float)delta;
            }
            if(Input.IsActionPressed("left"))
            {
                motionPressed = true;
                horizontalVelocity += Vector2.Left.Rotated(-_camera.Rotation.Y) * _acceleration * (float)delta;
            }
            if(Input.IsActionPressed("right"))
            {
                motionPressed = true;
                horizontalVelocity += Vector2.Right.Rotated(-_camera.Rotation.Y) * _acceleration * (float)delta;
            }
            if(horizontalVelocity.LengthSquared() >= _maxAcceleratedSpeedSquared)
                horizontalVelocity = horizontalVelocity.Normalized() * _maxAcceleratedSpeed * (float)delta;

            if(!motionPressed)
            {
                Vector2 slowingVelocity = horizontalVelocity.Normalized() * _deacceleration * (float)delta;
                if(slowingVelocity.LengthSquared() > horizontalVelocity.LengthSquared())
                    horizontalVelocity = Vector2.Zero;
                else
                    horizontalVelocity -= slowingVelocity;
            }
            this.Velocity = new Vector3(horizontalVelocity.X, this.Velocity.Y, horizontalVelocity.Y);
        }

        this.Velocity += _gravityAcceleration * (float)delta;
        MoveAndSlide();
        GD.Print($"Delta: {delta} Velocity: {this.Velocity} Postition: {this.Position}");
	}

    public override void _Input(InputEvent @event)
    {
        if(@event is InputEventMouseMotion mouseMotionEvent)
        {
            _camera.Rotation += new Vector3(-mouseMotionEvent.Relative.Y, -mouseMotionEvent.Relative.X, 0) * (float)0.001;
        }
    }
}
