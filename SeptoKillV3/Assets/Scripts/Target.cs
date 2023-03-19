using UnityEngine;

public class Target : MonoBehaviour
{
    public float health = 100;
    public void Update()
    {
        if (health <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Damaged!");

    }
    public void Die()
    {
        Debug.Log("You killed an enemy!");
    }
}
