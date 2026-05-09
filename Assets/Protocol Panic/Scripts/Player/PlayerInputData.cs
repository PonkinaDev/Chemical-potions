using Fusion;
using UnityEngine;

// Struct que viaja por la red cada tick con el input del jugador
// DEBE ser struct, no class
public struct PlayerInputData : INetworkInput
{
    public Vector2 MovementInput;
    public NetworkBool PickupPressed;
}