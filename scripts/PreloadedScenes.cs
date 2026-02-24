using Godot;

public static class PreloadedScenes
{
    public static PackedScene BulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
    public static PackedScene SphereProjectileScene = GD.Load<PackedScene>("res://scenes/sphere_projectile.tscn");
}
