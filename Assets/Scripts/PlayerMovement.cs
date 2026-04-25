using UnityEngine;
using Unity.Cinemachine;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    [SerializeField] float speed = 0.5f;
    [SerializeField] private CinemachineCamera graveyardCam;
    public bool canMove = false;
    private Rigidbody2D rb;
    private Vector2 input;

    private Animator animator;
    private enum Direction { Down, Up, Left, Right }
    private Direction lastYDirection = Direction.Down;
    private Direction lastXDirection = Direction.Right;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (canMove && graveyardCam.enabled == false)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
        }
        input.Normalize();

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = input * speed;
    }

    
    private void UpdateAnimation()
    {
        if (input != Vector2.zero)
        {
            // Downwards facing animations
            if (input.y <= 0)
            {
                if (input.x > 0)
                {
                    animator.Play("WalkRight");
                }

                else if (input.x < 0)
                {
                    animator.Play("WalkLeft");
                }

                else if (input.x == 0)
                {
                    switch (lastXDirection)
                    {
                        case Direction.Right:
                            animator.Play("WalkRight");
                            break;
                        case Direction.Left:
                            animator.Play("WalkLeft");
                            break;
                    }
                }
            }

            // Up-animations
            else if (input.y > 0)
            {
                if (input.x > 0)
                {
                    animator.Play("WaulkUpR");
                }

                else if (input.x < 0)
                {
                    animator.Play("WalkUpL");
                }

                else if (input.x == 0)
                {
                    switch (lastXDirection)
                    {
                        case Direction.Right:
                            animator.Play("WaulkUpR");
                            break;
                        case Direction.Left:
                            animator.Play("WalkUpL");
                            break;
                    }
                }
            }

            if (input.y < 0) lastYDirection = Direction.Down;
            if (input.y > 0) lastYDirection = Direction.Up;
            if (input.x < 0) lastXDirection = Direction.Left;
            if (input.x > 0) lastXDirection = Direction.Right;
        }

        else
        {
            // Idle animation
            switch (lastYDirection)
            { 
                case Direction.Up:
                    animator.Play("UpIdle");
                    break;
                case Direction.Down:
                    animator.Play("DownIdle");
                    break;
            }
        }
    }
}
