using Godot;
using System;

public partial class GunEnemy : CharacterBody3D, IDamageable
{
	private Player _player = null;
	private Node3D _firingPointNode3D;
	private const double _fireCooldown = 1;
	private double _fireCooldownStatus = 0;
	private const double _visionRange = 100;
	private const double _visionRangeSquared = _visionRange * _visionRange;
	private Node _parentNode;
	private MeshInstance3D _gunMeshInstance;
    private int _health = 1;

	public override void _Ready()
	{
		FetchPlayer();
		_firingPointNode3D = this.GetNode<Node3D>("GunMeshInstance3D/FiringPointNode3D");
		_parentNode = this.GetParent();
		_gunMeshInstance = this.GetNode<MeshInstance3D>("GunMeshInstance3D");
	}

	public override void _PhysicsProcess(double delta)
	{
		if(_fireCooldownStatus > 0)
		{
			_fireCooldownStatus -= delta;
		}
		TrackAndAttackPlayer();
	}

	private void FetchPlayer()
	{
		_player = this.GetNodeOrNull<Player>("/root/GameRoot/World/Player");
	}

	private bool CheckForPlayer()
	{
		if(_player == null)
		{
			FetchPlayer();
		}
		if(_player == null)
		{
			return false;
		}
		return true;
	}

	private void TrackAndAttackPlayer()
	{
		if(!CheckForPlayer())
		{
			return;
		}
		Vector3 playerPos = _player.GlobalPosition;
		double playerDistance = this.GlobalPosition.DistanceSquaredTo(playerPos);
		if(playerDistance > _visionRangeSquared)
		{
			return;
		}
		var rayQuery = PhysicsRayQueryParameters3D.Create(_firingPointNode3D.GlobalPosition, playerPos);
		rayQuery.Exclude = [this.GetRid()];
		var raycastResult = GetWorld3D().DirectSpaceState.IntersectRay(rayQuery);
		if(raycastResult.Count == 0)
		{
			return;
		}
		var colliderGObj = raycastResult["collider"].AsGodotObject();
		if(colliderGObj is not Player)
		{
			return;
		}
		Vector3 toPlayerPos = playerPos - this.GlobalPosition;
		Vector2 horizontalToPlayerPos = new(toPlayerPos.X, toPlayerPos.Z);
		Vector2 verticalToPlayerPos = new(horizontalToPlayerPos.Length(), toPlayerPos.Y);
		float hAngle = -horizontalToPlayerPos.Angle() - (float)Math.PI/2;
		float vAngle = verticalToPlayerPos.Angle();
		this.Rotation = new Vector3(this.Rotation.X, hAngle, this.Rotation.Z);
		_gunMeshInstance.Rotation = new Vector3(vAngle, _gunMeshInstance.Rotation.Y, _gunMeshInstance.Rotation.Z);

		if(_fireCooldownStatus > 0)
		{
			return;
		}
		_fireCooldownStatus = _fireCooldown;
		Vector3 bulletStartPos = _firingPointNode3D.GlobalPosition;
		Vector3 bulletToPlayer = playerPos - bulletStartPos;
		Vector2 bulletHToPlayer = new(-bulletToPlayer.Z, -bulletToPlayer.X);
		Vector2 bulletVToPlayer = new(bulletHToPlayer.Length(), bulletToPlayer.Y);
		float bulletHAngle = bulletHToPlayer.Angle();
		float bulletVAngle = bulletVToPlayer.Angle();
		var bullet = Bullet.Instantiate(
			bulletStartPos,
			bulletToPlayer,
			bulletHAngle,
			bulletVAngle
		);
		this.AddSibling(bullet);
	}

    private void Die()
    {
        this.QueueFree();
    }

    public void DealDamage(int dmg)
    {
        _health -= dmg;
        if(_health <= 0)
        {
            Die();
        }
    }
}
