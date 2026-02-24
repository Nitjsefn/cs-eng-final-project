using Godot;
using System;

public partial class Player : CharacterBody3D, IDamageable
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
    private Area3D _grapplingToNode = null;
    private bool _grappling = false;
    private float _grapplingAcceleration = 20;
    private RayCast3D _grapplingRaycast;
    private Vector3 _grappleStartPos;
    private GrapplingPoint _currentGrapplingPoint;
    private Vector3 _locationStartPoint = new Vector3(-33.014f, 4.47f, 16.433f);
    private Vector3 _locationStartRotation = Vector3.Zero;
    private const int _startingHealth = 1;
    private int _health = _startingHealth;
    private const int _slashDamage = 1;
    private AttackAnimationState _slashAttackState = AttackAnimationState.READY;
    private Area3D _swordArea3D;
    private AnimationPlayer _swordAnimationPlayer;
    private AnimationPlayer _animationPlayer;

    [Signal]
    public delegate void AttackSignalEventHandler();


    public override void _Ready()
    {
        _maxAcceleratedSpeedSquared = (float)Math.Pow(_maxAcceleratedSpeed, 2);
        this.Velocity = new Vector3(0, 0, 0);
        this.UpDirection = Vector3.Up;
        _camera = (Camera3D)this.GetNode("Camera3D");
        CollisionShape3D collisionShape3D = (CollisionShape3D)this.GetNode("CollisionShape3D");
        CapsuleShape3D collisionCapsule = (CapsuleShape3D)collisionShape3D.Shape;
        _crouchingYPosDelta = new Vector3(0, collisionCapsule.Height / 4, 0);
        _grapplingRaycast = (RayCast3D)_camera.GetNode("RayCast3D");
        _swordArea3D = this.GetNode<Area3D>("SwordArea3D");
        _swordAnimationPlayer = _swordArea3D.GetNode<AnimationPlayer>("AnimationPlayer");
        _animationPlayer = this.GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public override void _Process(double delta)
    {
        if(_animationPlayer.CurrentAnimation == "parried")
        {
            return;
        }
        _markGrapplePoint();
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

        if(_grappling)
        {
            Vector3 startPointToGrapplePoint = _grapplingToNode.Position - _grappleStartPos;
            Vector3 fromGrapplePoint = this.Position - _grapplingToNode.Position;
            Vector3 fromStartPoint = startPointToGrapplePoint + fromGrapplePoint;
            float grappleReleaseLengthSquared = startPointToGrapplePoint.LengthSquared() + fromGrapplePoint.LengthSquared();
            if(fromStartPoint.LengthSquared() > grappleReleaseLengthSquared)
            {
                _grapplingToNode = null;
                _grappling = false;
            }
            else
            {
                Vector3 direction = _grapplingToNode.Position - this.Position;
                this.Velocity += direction.Normalized() * _grapplingAcceleration * (float)delta;
            }
        }

        if(!this.IsOnWall())
        {
            this.Velocity += _gravityAcceleration * (float)delta;
        }
        MoveAndSlide();
        //GD.Print($"Delta: {delta} Velocity: {this.Velocity} Postition: {this.Position}");
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
            else if(keyEvent.IsActionPressed("grapple"))
            {
                _grapplingRaycast.ForceRaycastUpdate();
                if(_grapplingRaycast.IsColliding())
                {
                    _grappling = true;
                    _grappleStartPos = this.Position;
                    _grapplingToNode = (Area3D)_grapplingRaycast.GetCollider();
                }
            }
            else if(keyEvent.IsActionPressed("attack"))
            {
                if(_swordAnimationPlayer.IsPlaying())
                {
                    return;
                }
                _swordAnimationPlayer.Play("sword_slash");
                _slashAttackState = AttackAnimationState.ACTIVE;
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

    private void _markGrapplePoint()
    {
        if(!_grapplingRaycast.IsColliding())
        {
            _currentGrapplingPoint?.NotifyPlayerStoppedLooking();
            _currentGrapplingPoint = null;
            return;
        }
        //GrapplingPoint collider = (GrapplingPoint);
        if(_grapplingRaycast.GetCollider() is GrapplingPoint collider && collider != _currentGrapplingPoint)
        {
            collider.NotifyPlayerIsLooking();
            _currentGrapplingPoint = collider;
        }
    }
    
    private void _die() {
        this.Velocity = Vector3.Zero;
        this.Position = _locationStartPoint;
        this.Rotation = _locationStartRotation;
        _jumpAwaits = false;
        _grappling = false;
        _grapplingToNode = null;
        _health = _startingHealth;
        _currentGrapplingPoint?.NotifyPlayerStoppedLooking();
    }

    public void OnDeathboxEntered(Node3D body)
    {
        _die();
    }

    public void DealDamage(int dmg)
    {
        _health -= dmg;
        if(_health > 0)
        {
            return;
        }
        _die();
    }

    public void OnSwordAreaBodyOrAreaEntered(Node3D node)
    {
        if(_slashAttackState != AttackAnimationState.ACTIVE)
        {
            return;
        }
        if(node is Player)
        {
            return;
        }

        if(node is IDeflectable deflectable)
        {
            PerformDeflect(deflectable);
        }
        else if(node is IParryable parryable)
        {
            PerformParry(parryable);
        }
        else if(node is IDamageable damageable)
        {
            PerformDealingDamage(damageable);
        }
    }

    private void PerformDeflect(IDeflectable deflectable)
    {
        //Vector3 deflectRotation = new(_camera.Rotation.X, this.Rotation.Y, 0);
        //Vector3 deflectDest = Vector3.Forward.Rotated(deflectRotation.Normalized(), );
        Vector3 deflectDest = -_camera.GlobalBasis.Z.Normalized();
        if(deflectable is Bullet bullet)
        {
            bullet.HarmfulToEnemies = true;
        }
        deflectable.Deflect(null, deflectDest);
    }

    private void PerformDealingDamage(IDamageable damageable)
    {
        damageable.DealDamage(_slashDamage);
    }

    private void PerformParry(IParryable parryable)
    {
        parryable.Parry();
    }

    public void OnSwordAnimationFinished(StringName animationName)
    {
        if(animationName == "sword_slash")
        {
            _slashAttackState = AttackAnimationState.READY;
        }
    }

    public void OnSwordParried()
    {
        if(_slashAttackState != AttackAnimationState.ACTIVE)
        {
            return;
        }
        _slashAttackState = AttackAnimationState.READY;
        _swordAnimationPlayer.Stop();
        _animationPlayer.Play("parried");
    }
}
