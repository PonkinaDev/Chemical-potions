using Fusion;
using UnityEngine;

// SRP: solo maneja movimiento y estado de red del jugador
[RequireComponent(typeof(CharacterController))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;

    // [Networked] = variable sincronizada automáticamente por Fusion
    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public Quaternion NetworkedRotation { get; set; }

    private CharacterController _cc;

    public override void Spawned()
    {
        _cc = GetComponent<CharacterController>();

        // Nombramos el objeto para identificarlo fácilmente
        gameObject.name = HasInputAuthority
            ? "[Local] Player"
            : "[Remote] Player";

        Debug.Log($"[NetworkPlayer] Spawned. HasInputAuthority: {HasInputAuthority}, HasStateAuthority: {HasStateAuthority}");
    }

    // FixedUpdateNetwork: equivalente a FixedUpdate pero sincronizado con Fusion
    // Se ejecuta en todos los clientes pero el movimiento REAL solo ocurre
    // donde hay InputAuthority (el jugador dueño del input)
    public override void FixedUpdateNetwork()
    {
        // GetInput: obtiene el input para ESTE jugador específico
        // Solo retorna true si tenemos InputAuthority sobre este objeto
        if (GetInput(out PlayerInputData input))
        {
            Vector3 move = new Vector3(input.MovementInput.x, 0f, input.MovementInput.y);
            move = move.normalized * _moveSpeed * Runner.DeltaTime;

            _cc.Move(move);

            // Sincronizamos la posición para que Fusion la propague
            NetworkedPosition = transform.position;

            // Rotamos hacia donde nos movemos
            if (move != Vector3.zero)
            {
                NetworkedRotation = Quaternion.LookRotation(move);
            }
        }
    }

    // Interpolación visual: suaviza el movimiento en clientes remotos
    public override void Render()
    {
        // Solo interpolamos en objetos remotos (no el nuestro)
        if (!HasInputAuthority)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                NetworkedPosition,
                Runner.DeltaTime * 15f
            );
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                NetworkedRotation,
                Runner.DeltaTime * 15f
            );
        }
    }
}