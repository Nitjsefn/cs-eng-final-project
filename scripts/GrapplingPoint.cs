using Godot;
using System;

public partial class GrapplingPoint : Area3D
{
    private bool _playerLooking = false;
    private MeshInstance3D _mesh;
    private StandardMaterial3D _material;
    private Color _defaultColor;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        _mesh = (MeshInstance3D)this.GetNode("MeshInstance3D");
        _material = (StandardMaterial3D)((SphereMesh)_mesh.Mesh).Material;
        _defaultColor = _material.AlbedoColor;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public void NotifyPlayerIsLooking()
    {
        if(!_playerLooking)
        {
            _playerLooking = true;
            _material.AlbedoColor = Color.Color8(255, 0, 0, 0);
        }
    }

    public void NotifyPlayerStoppedLooking()
    {
        if(_playerLooking)
        {
            _playerLooking = false;
            _material.AlbedoColor = _defaultColor;
        }
    }
}
