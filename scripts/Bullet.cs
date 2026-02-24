using Godot;
using System;

public partial class Bullet : Area3D, IDeflectable
{
    private Vector3 _velocity;
    private const double _speed = 40;
    private const int _damage = 1;
    private const float _maxTravelDistance = 1000;
    private const float _maxTravelDistanceSquared = _maxTravelDistance * _maxTravelDistance;
    private Vector3 _startPosition;
    private bool _harmfulToEnemies = false;
    private Node3D _lastDeflectingCollider = null;

    public bool HarmfulToEnemies
    {
        get { return _harmfulToEnemies; }
        set { _harmfulToEnemies = value; }
    }

    [Signal]
    public delegate void DestroyedSignalEventHandler(Bullet instance);

    public static Bullet Instantiate(Vector3 position, Vector3 relTargetPos, double HAngle, double VAngle)
    {
        Bullet bullet = (Bullet)PreloadedScenes.BulletScene.Instantiate();
        bullet._velocity = CalcVelocity(relTargetPos);
        Transform3D transform = bullet.Transform;
        transform.Origin = position;
        bullet.Transform = transform;
        bullet.Rotation = new Vector3(0, (float)HAngle, (float)-VAngle);
        bullet._startPosition = position;
        return bullet;
    }

    public static Bullet Instantiate(Vector3 position, Vector3 relTargetPos)
    {
        Bullet bullet = (Bullet)PreloadedScenes.BulletScene.Instantiate();
        bullet._velocity = CalcVelocity(relTargetPos);
        Transform3D transform = bullet.Transform;
        transform.Origin = position;
        transform.Basis = Godot.Basis.LookingAt(relTargetPos);
        bullet.Transform = transform;
        bullet._startPosition = position;
        return bullet;
    }

    public static Bullet InstantiateSphereProjectile(Vector3 position, Vector3 velocity)
    {
        Bullet bullet = (Bullet)PreloadedScenes.SphereProjectileScene.Instantiate();
        bullet._velocity = velocity;
        Transform3D transform = bullet.Transform;
        transform.Origin = position;
        bullet.Transform = transform;
        bullet._startPosition = position;
        return bullet;
    }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
        this.Position += _velocity * (float)delta;
        if((this.Position - _startPosition).LengthSquared() > _maxTravelDistanceSquared)
        {
            this.QueueFree();
        }
	}

    public void OnBodyEntered(Node3D body)
    {
        if(_lastDeflectingCollider != null)
        {
            _lastDeflectingCollider = null;
            return;
        }
        if(!_harmfulToEnemies && body.IsInGroup("Enemies"))
        {
            return;
        }
        if(body is IDamageable damageable)
        {
            damageable.DealDamage(_damage);
        }
        _onImpact();
    }

    private void _onImpact()
    {
        this.EmitSignal(SignalName.DestroyedSignal, this);
        this.QueueFree();
    }

    public void Deflect(Node3D collider, Vector3 destination)
    {
        _lastDeflectingCollider = collider;
        _velocity = CalcVelocity(destination);
        Transform3D transform = new(Godot.Basis.LookingAt(destination), this.GlobalTransform.Origin);
        this.GlobalTransform = transform;
    }

    private static Vector3 CalcVelocity(Vector3 destination)
    {
        if(!destination.IsNormalized())
        {
            destination = destination.Normalized();
        }
        Vector3 velocity = destination  * (float)_speed;

        return velocity;
    }
}
