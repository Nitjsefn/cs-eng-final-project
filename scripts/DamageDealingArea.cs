using Godot;
using System;

public partial class DamageDealingArea : Area3D
{
	[Export]
	public int Damage = 1;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void OnBodyOrAreaEntered(Node3D node)
	{
		if(node is IDamageable damageable)
		{
			damageable.DealDamage(Damage);
		}
	}
}
