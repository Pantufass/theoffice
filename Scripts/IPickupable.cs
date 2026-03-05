using Godot;
using System;

public interface IPickupable : IInteractable
{
	//public PackedScene Scene { get; }

	public void Use();
	public void OnPickup();
	public void OnDrop();
}
