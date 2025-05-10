using UnityEngine;

public class MainProjectile : MonoBehaviour {
    float lifetime = 2f;
    float timer = 0f;
    void Update()
    {
        timer += Time.deltaTime;
        if(timer >= lifetime){
            Destroy(gameObject);
            timer = 0f;

        }
        
    }
    public int damage = 5;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // push back enemy 
        if(collision.gameObject.tag == "Enemy"){
            // find angle that the projectile was fired
            Vector2 direction = (collision.transform.position - transform.position).normalized;

            // call enemy damamge function
            collision.gameObject.GetComponent<BasicEnemyAI>().TakeDamage(damage);
            // push back enemy
            collision.gameObject.GetComponent<BasicEnemyAI>().ApplyKnockback(direction, 5f, .3f);
            Destroy(gameObject);
        }

    }
}