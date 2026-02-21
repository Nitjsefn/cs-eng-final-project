using Godot;
using System;

public partial class TeleportingBoss : CharacterBody3D, IDamageable
{
    private Player _player = null;
    private bool _fightStarted = false;
    private const float _maxTeleportDistance = 20;
    private const float _maxTeleportDistanceSquared = _maxTeleportDistance * _maxTeleportDistance;
    private const float _walkingSpeed = 20;
    private const float _attackRange = 1;
    private AttackAnimationState _attackState = AttackAnimationState.READY;
    private int _attackComboCount = 0;
    private AnimationPlayer _swordAnimationPlayer;
    private Action _currentAction = Action.NONE;
    private bool _actionCalculated;
    private const float _distancingSpeed = 40;
    private const float _distancingDistance = 60;
    private Vector3 _distancingStartPoint;
    private AnimationPlayer _animationPlayer;
    private const int _bulletsCount = 3;
    private static readonly float[] _bulletRotation = [float.DegreesToRadians(45), 0, float.DegreesToRadians(-45)];
    private Bullet[] _bullets = new Bullet[_bulletsCount];
    private Node3D _projectilesSpawnNode3D;
    private const float _bulletDeflectStartDistance = 1;
    private const float _bulletDeflectStartDistanceSquared = _bulletDeflectStartDistance * _bulletDeflectStartDistance;
    private const int _maxStartingHealth = 1;
    private int _health = _maxStartingHealth;
    private const int _slashDamage = 1;
    private Vector3 _gravity = new(0, -10, 0);

    [Signal]
    public delegate void DefeatedSignalEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        this.UpDirection = Vector3.Up;
        SetupPlayerRef();
        _swordAnimationPlayer = this.GetNode<AnimationPlayer>("SwordArea3D/AnimationPlayer");
        _animationPlayer = this.GetNode<AnimationPlayer>("AnimationPlayer");
        _projectilesSpawnNode3D = this.GetNode<Node3D>("ProjectilesSpawnNode3D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
        if(_player == null)
        {
            if(!SetupPlayerRef())
            {
                return;
            }
        }
        if(!_fightStarted || _currentAction == Action.PARRIED)
        {
            return;
        }

        this.Velocity += _gravity;
        
        Vector3 toPlayerPos = _player.GlobalPosition - this.GlobalPosition;
        float toPlayerPosLenSquared = toPlayerPos.LengthSquared();
        Vector3 hDirectionNormalized = new Vector3(toPlayerPos.X, 0f, toPlayerPos.Z).Normalized();
        Transform3D transform = this.GlobalTransform;
        transform.Basis = Godot.Basis.LookingAt(hDirectionNormalized);
        this.GlobalTransform = transform;

        if(_currentAction == Action.NONE)
        {
            if(toPlayerPosLenSquared > _maxTeleportDistanceSquared)
            {
                Vector3 walkVelocity = hDirectionNormalized * _walkingSpeed;
                this.Velocity = walkVelocity;
                this.MoveAndSlide();
            }
            else if(toPlayerPosLenSquared > _attackRange && _attackState == AttackAnimationState.READY)
            {
                this.GlobalPosition = _player.GlobalPosition - hDirectionNormalized * (_attackRange / 2);
                _currentAction = Action.TELEPORT_ATTACK;
                _swordAnimationPlayer.Play("sword_slash");
                _attackState = AttackAnimationState.ACTIVE;
                _attackComboCount++;
            }
        }
        else if(_currentAction == Action.DISTANCING)
        {
            if(!_actionCalculated)
            {
                this.Velocity = -hDirectionNormalized * _distancingSpeed + new Vector3(0, this.Velocity.Y, 0);
                _animationPlayer.Play("distancing");
                _actionCalculated = true;
            }
            this.MoveAndSlide();
        }
        else if(_currentAction == Action.WAITING_FOR_PROJECTILES)
        {
            if(!_actionCalculated)
            {
                _actionCalculated = true;
                ShootProjectiles();
            }
            else
            {
                foreach(Bullet bullet in _bullets)
                {
                    Vector3 bulletDest = -bullet.GlobalTransform.Basis.Z;
                    Vector3 futurePos = bullet.GlobalPosition + bulletDest;
                    Vector3 toFuturePos = futurePos - this.GlobalPosition;
                    Vector3 toPos = bullet.GlobalPosition - this.GlobalPosition;
                    float distanceToFuturePosSquared = toFuturePos.LengthSquared();
                    float distanceToPosSquared = toPos.LengthSquared();
                    if(distanceToFuturePosSquared < distanceToPosSquared)
                    {
                        if(distanceToPosSquared <= _bulletDeflectStartDistanceSquared)
                        {
                            _swordAnimationPlayer.Play("sword_slash");
                        }
                    }
                }
            }
            this.MoveAndSlide();
        }
	}

    private bool SetupPlayerRef()
    {
        _player = this.GetNodeOrNull<Player>("/root/GameRoot/World/Player");
        if(_player == null)
        {
            return false;
        }
        _player.AttackSignal += OnPlayerStartedAttack;
        return true;
    }

    private void OnPlayerStartedAttack()
    {
        Action[] exceptions = [Action.TELEPORT_ATTACK, Action.PARRY, Action.PARRIED];
        foreach(Action ex in exceptions)
        {
            if(_currentAction == ex)
            {
                return;
            }
        }

        _currentAction = Action.PARRY;
        _swordAnimationPlayer.Play("sword_slash");
    }

    public void StartFight()
    {
        _fightStarted = true;
    }

    public void StopFight()
    {
        _fightStarted = false;
    }

    private Vector2 CreateHVec2(Vector3 vec3)
    {
        return new(-vec3.Z, -vec3.X);
    }

    public void OnSwordAnimationFinished(StringName animationName)
    {
        _attackState = AttackAnimationState.READY;
        if(_currentAction == Action.TELEPORT_ATTACK)
        {
            if(_attackComboCount == 0)
            {
                _currentAction = Action.DISTANCING;
                _actionCalculated = false;
            }
            else if(_attackComboCount > 0)
            {
                _currentAction = Action.NONE;
            }
        }
        else if(_currentAction == Action.PARRY)
        {
            _currentAction = Action.NONE;
        }
    }

    public void OnAnimationFinished(StringName animationName)
    {
        if(_currentAction == Action.DISTANCING)
        {
            _currentAction = Action.WAITING_FOR_PROJECTILES;
            _actionCalculated = false;
        }
        else if(_currentAction == Action.PARRIED)
        {
            _currentAction = Action.NONE;
        }
    }

    private void ShootProjectiles()
    {
        Vector3 direction = _player.GlobalPosition - _projectilesSpawnNode3D.GlobalPosition;
        for(int i = 0; i < _bulletsCount; i++)
        {
            Vector3 currentDirection = direction.Rotated(Vector3.Up, _bulletRotation[i]);
            var bullet = Bullet.Instantiate(_projectilesSpawnNode3D.GlobalPosition, currentDirection);
            bullet.DestroyedSignal += OnBulletDestroyed;
            _bullets[i] = bullet;
        }
    }

    public void OnBulletDestroyed(Bullet bullet)
    {
        int destroyedBulletsCount = 0;
        for(int i = 0; i < _bulletsCount; i++)
        {
            if(_bullets[i] == bullet)
            {
                _bullets[i] = null;
                destroyedBulletsCount++;
            }
            else if(_bullets[i] == null)
            {
                destroyedBulletsCount++;
            }
        }

        if(destroyedBulletsCount == _bulletsCount)
        {
            _currentAction = Action.NONE;
        }
    }

    public void OnAttackParried()
    {
        if(_attackState != AttackAnimationState.ACTIVE)
        {
            return;
        }
        else if(_currentAction == Action.PARRY)
        {
            _swordAnimationPlayer.Stop();
            OnSwordAnimationFinished(_swordAnimationPlayer.CurrentAnimation);
        }
        else if(_attackComboCount == 0)
        {
            _swordAnimationPlayer.Stop();
            OnSwordAnimationFinished(_swordAnimationPlayer.CurrentAnimation);
        }
        else if(_attackComboCount > 0)
        {
            _currentAction = Action.PARRIED;
            _attackState = AttackAnimationState.READY;
            _animationPlayer.Play("parried");
        }
    }

    public void OnSwordBodyOrAreaEntered(Node3D node)
    {
        if(_attackState != AttackAnimationState.ACTIVE)
        {
            return;
        }
        if(node is TeleportingBoss)
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
        Vector3 toPlayer = _player.GlobalPosition - this.GlobalPosition;
        Vector3 deflectDest = toPlayer.Normalized();
        deflectable.Deflect(null, deflectDest);
    }

    private void PerformDealingDamage(IDamageable damageable)
    {
        damageable.DealDamage(_slashDamage);
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
        this.EmitSignalDefeatedSignal();
        this.QueueFree();
    }

    private void PerformParry(IParryable parryable)
    {
        parryable.Parry();
    }

    public void OnPlayerEnteredArena(StringName areaKey)
    {
        _fightStarted = true;
    }
}

enum Action
{
    NONE,
    TELEPORT_ATTACK,
    PARRY,
    PARRIED,
    DISTANCING,
    WAITING_FOR_PROJECTILES
}
