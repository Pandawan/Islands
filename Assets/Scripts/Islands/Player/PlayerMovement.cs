using Pandawan.Islands.Other;
using UnityEngine;

namespace Pandawan.Islands.Player
{
    /// <summary>
    ///     Manages player movement and its animations
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerMovement : MonoBehaviour
    {
        /// <summary>
        ///     The speed at which the player moves
        /// </summary>
        [SerializeField] private float speed = 5f;

        /// <summary>
        ///     Whether or not the player should be allowed to move
        /// </summary>
        [SerializeField] private bool allowMovement = true;

        private Animator anim;
        private Vector2 lastFacingDirection;

        private Vector2 movement;

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();

            lastFacingDirection = new Vector2(0, -1);
        }

        private void Update()
        {
            // Do Movement/Input in Update for more accurate results
            if (allowMovement)
                Move();
            else
                movement = Vector2.zero;


            // Animations
            if (anim != null)
            {
                anim.SetBool("Moving", !Utilities.IsEmpty(movement));

                anim.SetFloat("MoveX", movement.x);
                anim.SetFloat("MoveY", movement.y);

                anim.SetFloat("LastMoveX", lastFacingDirection.x);
                anim.SetFloat("LastMoveY", lastFacingDirection.y);
            }
        }

        private void FixedUpdate()
        {
            rb.velocity = movement * speed;
        }

        /// <summary>
        ///     Get Input Axis and assign them to vector to be used for movement
        /// </summary>
        private void Move()
        {
            // Create Vector2 from Input
            movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            // Clamp it so you don't go faster when pressing two keys at once
            movement = Vector2.ClampMagnitude(movement, 1);

            if (!Utilities.IsEmpty(movement)) lastFacingDirection = movement;
        }

        public Vector2 GetFacingDirection()
        {
            return lastFacingDirection;
        }
    }
}