using Godot;
using System;

public partial class MomentumBoss : CharacterBody3D, IDamageable
{
    private const int _maxHealth = 3;
    private int _health = _maxHealth;
    private const float _maxAngularSpeed = 2 * (float)Math.PI;
    private static readonly Vector3 _maxAngularVelocity = new(0, _maxAngularSpeed, 0);
    private static readonly Vector3 _angularAcceleration = new(0, (float)(0.5 * Math.PI), 0);
    private bool _fightStarted = false;
    private Player _player;
    private Vector3 _angularVelocity = new(0, 0, 0);
    private static readonly Vector3 _armHidingVelocity = new(.5f, 0, 0);
    private Vector3 _maxArmPosition = new(-4.189f, -1.399f, 0);
    private Vector3 _minArmPosition = new(-.613f, -1.399f, 0);
    private ArmAction _action = ArmAction.IDLE;
    private bool _extended = true;
    private MeshInstance3D _arm;
    private MeshInstance3D _projectile;
    private double _momentum;
    private const double _projectileWeight = 1;
    private const float _targetingAngleMargin = (float)(.01 * Math.PI);
    private const double _cooldown = 1;
    private double _currentCooldown = 0;
    private Node _world;

    [Signal]
    public delegate void DestroyedSignalEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        FetchPlayer();
        FetchWorld();
        _arm = this.GetNode<MeshInstance3D>("CollisionShape3D/MeshInstance3D/ArmMeshInstance3D");
        _projectile = this.GetNode<MeshInstance3D>("CollisionShape3D/MeshInstance3D/ArmMeshInstance3D/ProjectileMeshInstance3D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
        if(_fightStarted == false)
        {
            return;
        }
        if(!FetchPlayer() || !FetchWorld())
        {
            return;
        }
        if(_currentCooldown > 0)
        {
            _currentCooldown -= delta;
            return;
        }
        if(_action == ArmAction.IDLE)
        {
            if(_projectile.Visible == false)
            {
                _projectile.Visible = true;
            }
            if(_angularVelocity.Y > _maxAngularSpeed)
            {
                _angularVelocity = _maxAngularVelocity;
            }
            if(_angularVelocity == _maxAngularVelocity)
            {
                _momentum = CalcMomentum();
                if(_extended)
                {
                    _action = ArmAction.RETRACTING;
                }
                else
                {
                    _action = ArmAction.EXTENDING;
                }
            }
            else
            {
                _angularVelocity += _angularAcceleration * (float)delta;
            }
        }
        else if(_action == ArmAction.RETRACTING)
        {
            _arm.Position += _armHidingVelocity * (float)delta;
            _angularVelocity = CalcAngularVelocity();
            Vector3 startToEnd = _minArmPosition - _maxArmPosition;
            Vector3 fromEnd = _arm.Position - _minArmPosition;
            Vector3 fromStart = _arm.Position - _maxArmPosition;
            if(startToEnd.LengthSquared() + fromEnd.LengthSquared() < fromStart.LengthSquared())
            {
                _arm.Position = _minArmPosition;
                _extended = false;
                _action = ArmAction.TARGETING;
            }
        }
        else if(_action == ArmAction.EXTENDING)
        {
            _arm.Position -= _armHidingVelocity * (float)delta;
            _angularVelocity = CalcAngularVelocity();
            Vector3 startToEnd = _maxArmPosition - _minArmPosition;
            Vector3 fromEnd = _arm.Position - _maxArmPosition;
            Vector3 fromStart = _arm.Position - _minArmPosition;
            if(startToEnd.LengthSquared() + fromEnd.LengthSquared() < fromStart.LengthSquared())
            {
                _arm.Position = _maxArmPosition;
                _extended = true;
                _action = ArmAction.TARGETING;
            }
        }
        else if(_action == ArmAction.TARGETING)
        {
            Vector3 toPlayer = _player.GlobalPosition - this.GlobalPosition;
            float angleToPlayer = new Vector2(-toPlayer.Z, -toPlayer.X).Angle();
            if(Math.Abs(this.GlobalRotation.Y - angleToPlayer) <= _targetingAngleMargin)
            {
                _action = ArmAction.IDLE;
                _angularVelocity = Vector3.Zero;
                _currentCooldown = _cooldown;
                ShootProjectile();
            }
        }

        this.Rotation -= _angularVelocity * (float)delta;
	}

    private bool FetchPlayer()
    {
        if(_player == null)
        {
            _player = this.GetNodeOrNull<Player>("/root/GameRoot/World/Player");
            if(_player == null)
            {
                return false;
            }
        }
        return true;
    }

    private bool FetchWorld()
    {
        if(_world == null)
        {
            _world = this.GetNodeOrNull<Node>("/root/GameRoot/World");
            if(_world == null)
            {
                return false;
            }
        }
        return true;
    }

    public void DealDamage(int dmg)
    {
        _health -= dmg;
        if(_health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        this.EmitSignalDestroyedSignal();
        this.QueueFree();
    }

    private void ShootProjectile()
    {
        _projectile.Visible = false;
        Vector3 direction = -this.Transform.Basis.Z;
        float linearSpeed = CalcProjectileLinearSpeed();
        Vector3 linearVelocity = linearSpeed * direction;
        Bullet projectile = Bullet.InstantiateSphereProjectile(_projectile.GlobalPosition, linearVelocity);
        _world.AddChild(projectile);
    }

    private float CalcProjectileLinearSpeed()
    {
        double r = (_projectile.Position + _arm.Position).Length();
        double speed = _momentum / (r * _projectileWeight);
        return (float)speed;
    }

    private double CalcMomentum()
    {
        double rSquared = (_projectile.Position + _arm.Position).LengthSquared();
        double momentum = _projectileWeight * rSquared * _angularVelocity.Y;
        return momentum;
    }

    private Vector3 CalcAngularVelocity()
    {
        double rSquared = (_projectile.Position + _arm.Position).LengthSquared();
        double speed = _momentum / (_projectileWeight * rSquared);
        return new(0, (float)speed, 0);
    }

    public void OnPlayerEnteredArena(StringName areaKey)
    {
        OnPlayerEnteredArena();
    }

    public void OnPlayerEnteredArena()
    {
        _fightStarted = true;
    }
}

enum ArmAction
{
    IDLE,
    EXTENDING,
    RETRACTING,
    TARGETING
}
