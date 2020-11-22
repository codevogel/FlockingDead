using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Agent : MonoBehaviour {
    private float sight = 100f;
    private static float zombieSight = 150f;
    private static float space = 50f;
    private static float movementSpeed = 75f;
    private static float rotateSpeed = 3f;
    private static float distToBoundary = 100f;

    public float seperationWeight = 1f;
    public float cohesionWeight = 1f;

    private BoxCollider2D boundary;

    public float dX;
    public float dY;

    public bool isZombie;
    public Vector2 position;
    public SpriteRenderer sprRenderer;

    private Sprite zombieSprite;

    public void Initialize(bool zombie, Sprite zombieSprite, Sprite regularSprite, BoxCollider2D boundary)
    {
        position = new Vector2(Random.Range(boundary.bounds.min.x + distToBoundary, boundary.bounds.max.x - distToBoundary), Random.Range(boundary.bounds.min.y + distToBoundary, boundary.bounds.max.y - distToBoundary));
        transform.position = position;

        this.boundary = boundary;

        isZombie = zombie;
        if (isZombie)
        {
            sight = zombieSight;
        }
        
        sprRenderer = GetComponent<SpriteRenderer>();

        this.zombieSprite = zombieSprite;

        if (isZombie)
            sprRenderer.sprite = zombieSprite;
        else
            sprRenderer.sprite = regularSprite;
    }

    public void Move(List<Agent> agents)
    {
        //Agents flock, zombie's hunt 
        if (!isZombie) Flock(agents, seperationWeight, cohesionWeight);
        else Hunt(agents);
        CheckBounds();
        CheckSpeed();

        position.x += dX;
        position.y += dY;

        Vector2 direction = (Vector3)position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotateSpeed * Time.deltaTime);

        transform.position = position;
    }

    private void Flock(List<Agent> agents, float seperationWeight, float cohesionWeight)
    {
        int numConhesionNeighbours = 0;
        Vector3 cohesionDirections = Vector2.zero;
        foreach (Agent a in agents)
        {
            float distance = Distance(position, a.position);
            if (a != this && !a.isZombie)
            {
                if (distance < space)
                {
                    // Separation
                    dX += seperationWeight * (position.x - a.position.x);
                    dY += seperationWeight * (position.y - a.position.y);
                }
                else if (distance < sight)
                {
                    numConhesionNeighbours++;
                    Debug.DrawLine(a.transform.position, this.transform.position, Color.yellow);
                    cohesionDirections += a.transform.position - this.transform.position;
                }
                if (distance < sight)
                {
                    // Alignment
                    //dX += TODO
                    //dY += TODO
                }
            }
            if (a.isZombie && distance < sight)
            {
                // Move away from spotted zombie.
                Vector2 direction = (a.transform.position - transform.position).normalized;
                Vector2 deltaPos = movementSpeed * - direction;
                dX += deltaPos.x;
                dY += deltaPos.y;
            }
        }

        if (numConhesionNeighbours != 0)
        {
            Vector3 averageDirection = (cohesionDirections / numConhesionNeighbours).normalized;

            Vector2 deltaPos = averageDirection * cohesionWeight;
            dX += deltaPos.x;
            dY += deltaPos.y;
        }
    }

    private void Hunt(List<Agent> agents)
    {
        float range = float.MaxValue;
        Agent prey = null;
        foreach (Agent a in agents)
        {
            if (!a.isZombie)
            {
                float distance = Distance(position, a.position);
                if (distance < sight && distance < range)
                {
                    range = distance;
                    prey = a;
                }
            }
        }
        if (prey != null)
        {
            // Move towards prey.

            float distanceToPrey = Vector2.Distance(transform.position, prey.position);
            float timeToPrey = distanceToPrey / (movementSpeed / 2f);
            float preyCurrentSpeed = new Vector2(prey.dY, prey.dY).magnitude;
            Vector3 predictedPreyPosition = prey.transform.position + preyCurrentSpeed * movementSpeed * prey.transform.right * timeToPrey;
            Debug.DrawLine(this.transform.position, predictedPreyPosition, Color.red);
            Vector2 direction = (predictedPreyPosition - transform.position).normalized;
            Vector2 deltaPos = movementSpeed * direction;
            dX += deltaPos.x;
            dY += deltaPos.y;
        }
    }

    private static float Distance(Vector2 p1, Vector2 p2)
    {
        float val = Mathf.Pow(p1.x - p2.x, 2) + Mathf.Pow(p1.y - p2.y, 2);
        return Mathf.Sqrt(val);
    }

    private void CheckBounds()
    {
        if (position.x < boundary.bounds.min.x + distToBoundary)
            dX += boundary.bounds.min.x + distToBoundary - position.x;
        if (position.y < boundary.bounds.min.y + distToBoundary)
            dY += boundary.bounds.min.y + distToBoundary - position.y;

        if (position.x > boundary.bounds.max.x - distToBoundary)
            dX += boundary.bounds.max.x - distToBoundary - position.x;
        if (position.y > boundary.bounds.max.y - distToBoundary)
            dY += boundary.bounds.max.y - distToBoundary - position.y;
    }

    private void CheckSpeed()
    {
        float s;
        if (!isZombie) s = movementSpeed * Time.deltaTime;
        else s = movementSpeed / 2f * Time.deltaTime; //Zombies are slower

        float val = Distance(Vector2.zero, new Vector2(dX, dY));
        if (val > s)
        {
            dX = dX * s / val;
            dY = dY * s / val;
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        Agent otherAgent = other.gameObject.GetComponent<Agent>();

        if(otherAgent != null)
        {
            // if im not a zombie and the other is, become a zombie
            if (otherAgent.isZombie && !this.isZombie)
                BecomeZombie();
        }
    }

    private void BecomeZombie()
    {
        isZombie = true;
        sprRenderer.sprite = zombieSprite;
        sight = zombieSight;
    }
}
