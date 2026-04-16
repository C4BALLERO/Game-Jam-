using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("estadisticas")]
    public float moveSpeed = 4f;

    [Header("Referencias")]
    public Rigidbody2D rb;
    public Animator bodyAnim;

    void Update()
    {
        Movement();
        Mirror();
        Attack();
    }

    void Movement()
    {
        float x = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(x * moveSpeed, rb.linearVelocity.y);

        bodyAnim.SetFloat("Speed", Mathf.Abs(x));
    }

    void Mirror()
    {
        if (rb.linearVelocity.x < 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else if (rb.linearVelocity.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    void Attack()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bodyAnim.SetTrigger("Attack");
        }
    }
}
