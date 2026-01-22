using Godot;
using System;

public partial class Bullet : Area3D
{
    private Vector3 _velocity;
    private const double _speed = 40;
    private const int _damage = 1;
    private const float _maxTravelDistance = 10;
    private const float _maxTravelDistanceSquared = _maxTravelDistance * _maxTravelDistance;
    private Vector3 _startPosition;

    public static Bullet Instantiate(Vector3 position, Vector3 relTargetPos, double HAngle, double VAngle)
    {
        Bullet bullet = (Bullet)PreloadedScenes.BulletScene.Instantiate();
        bullet._velocity = relTargetPos.Normalized() * (float)_speed;
        Transform3D transform = bullet.Transform;
        transform.Origin = position;
        bullet.Transform = transform;
        bullet.Rotation = new Vector3(0, (float)HAngle, (float)-VAngle);
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
        if(body is IDamageable damageable)
        {
            damageable.DealDamage(_damage);
        }
        _onImpact();
    }

    private void _onImpact()
    {
        this.QueueFree();
    }
}
