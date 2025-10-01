using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Enemy : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float attackSpeed = 0.9f; // attackSpeed = tốc độ animation của nhân vật
    [SerializeField] private float attackRange;
    [SerializeField] private float attackDamage = 40f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool isAttacking;
    [SerializeField] private bool isPlayerInAttackRange;
    [SerializeField] private LayerMask playerLayer;

    public int damageTakenTimes = 0;
    
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform playerTransform;

    void Start()
    {
       currentHealth = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        FlipSprite();
        if (!isAttacking && isPlayerInAttackRange) PlayAttackAnimation();
    }


    public void TakeDamage(float damage)
    {
        damageTakenTimes++;
        currentHealth -= damage;
        Debug.Log("Ouch!" + currentHealth);
        animator.SetTrigger("hurt");
        if (currentHealth < 0) Die();
    }
    
    private void FlipSprite()
    {
        if (playerTransform != null)
        {
            if (playerTransform.position.x < transform.position.x) // Nếu người chơi ở bên trái kẻ địch
            {
                transform.localScale = new Vector3(3, 3, 1);
            }

            else // Nếu ở bên phải
            {
                transform.localScale = new Vector3(-3, 3, 1);
            }
        }

        else return;
    }

    private void Die()
    {
        animator.SetBool("isDead", true);
        GetComponent<Collider2D>().enabled = false;
    }

    public void DestroyObject() // Hàm dùng xóa nhân vật sau khi chết
    {
        Debug.Log("Bro is really dead!");
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInAttackRange = true;
            playerTransform = collision.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        isPlayerInAttackRange = false;
    }

    private void PlayAttackAnimation()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("attack");
    }

    public void CauseDamge() // được gọi từ hàm animation event chỉ để thật sự gây dame ở giữa Animation
    {
        StartCoroutine(PerformAttack());
    }

    private IEnumerator PerformAttack()
    {
        
        
        Debug.Log("Death performed attack");

        Collider2D[] enemiesHit = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);

        foreach (Collider2D enemy in enemiesHit)
        {
            if (enemy != null)
            {
                Debug.Log("Player got hit!");
               //enemy.GetComponent<PlayerController>().TakeDamage(attackDamage);
            }

            else break;
        }

        yield return new WaitForSeconds(attackSpeed);
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
