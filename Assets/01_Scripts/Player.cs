using UnityEngine;

public class Player : MonoBehaviour
{

    [Header("estadisticas")]
    public float moveSpeed = 4f;
    public float jumpForce = 7F;
    public bool canJump = true;

    [Header("Referencias")]
    public Rigidbody2D rb;
    public Animator bodyAnim;

    //Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bodyAnim.SetBool("Grounded", false);
    }

    // Update is called once per frame
    void Update()
    {
        Movement();
        Mirror();
        Jump();
        MelleAttack();
        bodyAnim.SetFloat("FallingSpeed", rb.linearVelocity.y);
    }

    void Movement()
    {
        float x = Input.GetAxis("Horizontal");//-1 a 1
        bodyAnim.SetFloat("Speed", Mathf.Abs(x));
        rb.linearVelocity = new Vector2(x * moveSpeed, rb.linearVelocity.y);
    }

    void Mirror()
    {
        if(rb.linearVelocity.x < 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else if(rb.linearVelocity.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    void Jump()
    {
        if (canJump)
        {
            if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                canJump = false;
                bodyAnim.SetBool("Grounded", false);
            }
        }
    }

    void MelleAttack()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            bodyAnim.SetTrigger("Attack");
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            canJump = true;
            bodyAnim.SetBool("Grounded", true);
        }
    }
}
