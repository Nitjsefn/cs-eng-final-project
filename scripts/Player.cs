using Godot;
using System;

public partial class Player : CharacterBody3D
{
    private Vector3 _gravityAcceleration = new Vector3(0, -10, 0);
    private float _acceleration = 10;
    private float _maxAcceleratedSpeed = 10;
    private float _maxAcceleratedSpeedSquared;
    private float _deacceleration = 15;
    private float _maxUpCameraAngle = float.DegreesToRadians(50);
    private float _maxDownCameraAngle = float.DegreesToRadians(-50);
    private float _mouseCameraRotationFactor = (float)0.001;
    private float _wallJumpAcceleration = 5;
    private float _crouchingDeacceleration = 2;
    private Vector3 _floorJumpVelocity = new Vector3(0, 15, 0);
    private Camera3D _camera;
    private bool _jumpAwaits = false;
    private bool _crouching = false;
    private Vector3 _crouchingScale = new Vector3(1, (float)0.5, 1);
    private Vector3 _normalScale = new Vector3(1, 1, 1);
    private Vector3 _crouchingYPosDelta;


    public override void _Ready()
    {
        _maxAcceleratedSpeedSquared = (float)Math.Pow(_maxAcceleratedSpeed, 2);
        this.Velocity = new Vector3(0, 0, 0);
        this.UpDirection = Vector3.Up;
        _camera = (Camera3D)this.GetNode("Camera3D");
        CollisionShape3D collisionShape3D = (CollisionShape3D)this.GetNode("CollisionShape3D");
        CapsuleShape3D collisionCapsule = (CapsuleShape3D)collisionShape3D.Shape;
        _crouchingYPosDelta = new Vector3(0, collisionCapsule.Height / 4, 0);
    }

    public override void _Process(double delta)
    {
        if(this.IsOnFloor())
        {
            bool crouchPressed = Input.IsActionPressed("crouch");
            if(!_crouching && crouchPressed)
            {
                _crouch();
            }
            else if(_crouching && !crouchPressed)
            {
                _uncrouch();
            }

            Vector2 horizontalVelocity = new Vector2(this.Velocity.X, this.Velocity.Z);
            Vector2 desiredMotionDirection = _createDesiredMotionDirection();
            if(!_crouching && !desiredMotionDirection.IsZeroApprox())
            {
                horizontalVelocity += desiredMotionDirection * _acceleration * (float)delta;

                float desiredMotionAngle = desiredMotionDirection.Angle();
                Vector2 derotatedHorizontalVelocity = horizontalVelocity.Rotated(-desiredMotionAngle);
                float sideSlowingSpeed = Math.Sign(derotatedHorizontalVelocity.Y)  * _deacceleration * (float)delta;
                if(Math.Pow(sideSlowingSpeed, 2) > Math.Pow(derotatedHorizontalVelocity.Y, 2))
                {
                    sideSlowingSpeed = derotatedHorizontalVelocity.Y;
                }
                derotatedHorizontalVelocity = new Vector2(derotatedHorizontalVelocity.X, derotatedHorizontalVelocity.Y - sideSlowingSpeed);
                horizontalVelocity = derotatedHorizontalVelocity.Rotated(desiredMotionAngle);

                if(horizontalVelocity.LengthSquared() >= _maxAcceleratedSpeedSquared)
                {
                    horizontalVelocity = horizontalVelocity.Normalized() * _maxAcceleratedSpeed;
                }
            }
            else
            {
                float deaccelerationToApply = _deacceleration;
                if(_crouching)
                {
                    deaccelerationToApply = _crouchingDeacceleration;
                }
                Vector2 slowingVelocity = horizontalVelocity.Normalized() * deaccelerationToApply * (float)delta;
                if(slowingVelocity.LengthSquared() > horizontalVelocity.LengthSquared())
                    horizontalVelocity = Vector2.Zero;
                else
                    horizontalVelocity -= slowingVelocity;
            }
            this.Velocity = new Vector3(horizontalVelocity.X, this.Velocity.Y, horizontalVelocity.Y);

            if(_jumpAwaits)
            {
                _jumpAwaits = false;
                this.Velocity += _floorJumpVelocity;
            }
        }
        else if(this.IsOnWall())
        {
            Vector2 horizontalVelocity = new Vector2(this.Velocity.X, this.Velocity.Z);
            Vector2 desiredMotionDirection = _createDesiredMotionDirection();

            Vector3 wallNormal = this.GetWallNormal();
            Vector2 horizontalWallNormal = new Vector2(wallNormal.X, wallNormal.Z);
            float wallHorizontalAngle = horizontalWallNormal.Angle() + float.DegreesToRadians(90);

            Vector2 derotatedHorizontalVelocity = horizontalVelocity.Rotated(-wallHorizontalAngle);
            derotatedHorizontalVelocity = new Vector2(derotatedHorizontalVelocity.X, 0);
            horizontalVelocity = derotatedHorizontalVelocity.Rotated(wallHorizontalAngle);

            horizontalVelocity += horizontalVelocity.Normalized() * _acceleration * (float)delta;

            if(horizontalVelocity.LengthSquared() > _maxAcceleratedSpeedSquared)
            {
                horizontalVelocity = horizontalVelocity.Normalized() * _maxAcceleratedSpeed;
            }

            this.Velocity = new Vector3(horizontalVelocity.X, 0, horizontalVelocity.Y);

            if(_jumpAwaits)
            {
                _jumpAwaits = false;
                Vector3 desiredJumpDirection = new Vector3(desiredMotionDirection.X, _camera.Rotation.Normalized().X, desiredMotionDirection.Y);
                this.Velocity = desiredJumpDirection * (_wallJumpAcceleration + this.Velocity.Length());
            }
        }

        if(!this.IsOnWall())
        {
            this.Velocity += _gravityAcceleration * (float)delta;
        }
        MoveAndSlide();
        GD.Print($"Delta: {delta} Velocity: {this.Velocity} Postition: {this.Position}");
    }

    public override void _Input(InputEvent @event)
    {
        if(@event is InputEventMouseMotion mouseMotionEvent)
        {
            _camera.Rotation += new Vector3(-mouseMotionEvent.Relative.Y, 0, 0) * _mouseCameraRotationFactor;
            if(_camera.Rotation.X > _maxUpCameraAngle)
                _camera.Rotation = new Vector3(_maxUpCameraAngle, 0, 0);
            else if(_camera.Rotation.X < _maxDownCameraAngle)
                _camera.Rotation = new Vector3(_maxDownCameraAngle, 0, 0);

            this.Rotation += new Vector3(0, -mouseMotionEvent.Relative.X, 0) * _mouseCameraRotationFactor;
        }
        else if(@event is InputEventKey keyEvent)
        {
            if(keyEvent.IsActionPressed("jump") && (this.IsOnFloor() || this.IsOnWall()))
            {
                _jumpAwaits = true;
            }
        }
    }

    private Vector2 _createDesiredMotionDirection()
    {
        Vector2 desiredDirection = Vector2.Zero;
        if(Input.IsActionPressed("forward"))
        {
            desiredDirection += Vector2.Up;
        }
        if(Input.IsActionPressed("back"))
        {
            desiredDirection += Vector2.Down;
        }
        if(Input.IsActionPressed("left"))
        {
            desiredDirection += Vector2.Left;
        }
        if(Input.IsActionPressed("right"))
        {
            desiredDirection += Vector2.Right;
        }

        desiredDirection = desiredDirection.Rotated(-this.Rotation.Y);

        return desiredDirection;
    }

    private void _crouch()
    {
        _crouching = true;
        this.Scale = _crouchingScale;
        this.Position -= _crouchingYPosDelta;
    }

    private void _uncrouch()
    {
        _crouching = false;
        this.Scale = _normalScale;
        this.Position += _crouchingYPosDelta;
    }
}
