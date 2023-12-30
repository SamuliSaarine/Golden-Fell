using UnityEngine;

public class Orc : MonoBehaviour
{
    #region Variables

    const float DETECT_DISTANCE = 3;

    [SerializeField] float horizontalOffset = 0.1f;
    [SerializeField] float gravity = -1.6f;
    [SerializeField] Transform model;
    [SerializeField] Animator animator;
    [SerializeField] float destroyTimeFromStart = 240f;
    [SerializeField] float destroyTimeFromNoTarget = 120f;

    float horizontal;
    float verticalMomentum;
    float lastXVel = 0;
    float lastYVel = 0;
    float xVelocity;
    float yVelocity;
    float startDirection;
    bool punching;

    Transform jaska;
    Transform antti;
    Transform house;
    Transform target;

    float secondsFromStart;
    float secondsFromNoTarget;

    AudioSource punchAudio;
    AudioSource walkAudio;

    #endregion

    void Start()
    {
        transform.position = World.GetOrcSpawnPos(out startDirection);
        transform.localScale = new(-1*startDirection, 1, 1);
        startDirection *= 0.6f;
        horizontal = startDirection;

        GameObject jaskaObject = GameObject.Find("Jaska");
        jaska = jaskaObject == null ? null : jaskaObject.transform;

        GameObject anttiObject = GameObject.Find("Antti");
        antti = anttiObject == null ? null : anttiObject.transform;

        walkAudio = PlayerManager.Instance.GetAudioSource(2);
        punchAudio = PlayerManager.Instance.GetAudioSource(3,false);

        house = House.Instance.transform;
    }

    void Update()
    {
        secondsFromStart += Time.deltaTime;

        //If transform is null, we return infinite distance
        float jaskaDistance = jaska==null ? float.PositiveInfinity : Vector3.Distance(jaska.position, transform.position);
        float anttiDistance = antti==null ? float.PositiveInfinity : Vector3.Distance(antti.position, transform.position);

        if(jaskaDistance<=DETECT_DISTANCE)
        {
            target = jaska;
        }
        
        if(anttiDistance<=DETECT_DISTANCE && anttiDistance<jaskaDistance)
        {
            target = antti;
        }

        punching = false;

        if (target!=null)
        {
            float targetDistance = Vector3.Distance(transform.position,target.position);
            if(targetDistance>0.1f)
            {
                if (targetDistance <= DETECT_DISTANCE * 1.2f)
                {
                    if(Mathf.Abs(target.position.x-transform.position.x)>0.1f)
                    {
                        //Chase target
                        horizontal = target.position.x < transform.position.x ? -0.9f : 0.9f;
                    }                 
                }
                else
                {
                    //Stop chasing
                    target = null;
                    horizontal = startDirection;
                }
            }
            else
            {
                //Punch             
                horizontal = 0;
                punching = true;
                target.GetComponent<IDamageable>().Damage();
                punchAudio.Play();
            }
            secondsFromNoTarget = 0;
        }
        else
        {
            horizontal = startDirection;

            if(Vector3.Distance(transform.position, house.position)<=DETECT_DISTANCE*2)
            {
                target = house;
            }
            else
            {
                secondsFromNoTarget += Time.deltaTime;

                //If orc has been alive for a long time without having target for a long time
                if ((secondsFromNoTarget > destroyTimeFromNoTarget && secondsFromStart > destroyTimeFromStart)
                    //Or if orc has walked to another end of map and has no target
                    || (secondsFromNoTarget > destroyTimeFromNoTarget / 2 && (startDirection == -0.6f ? transform.position.x < 1 : transform.position.x > World.WORLD_WIDTH - 2)))
                {
                    //We despawn it
                    Destroy(gameObject);
                }
            }            
        }

        //Face to direction
        if(horizontal!=0 && transform.localScale.x*0.6f!=-horizontal)
        {
            transform.localScale = new(Mathf.Round(-horizontal), 1, 1);
        }

        if(horizontal != 0 && !walkAudio.isPlaying)
        {
            float audioDistance = jaskaDistance < anttiDistance ? jaskaDistance : anttiDistance;
            float maxDistance = DETECT_DISTANCE*2;
            walkAudio.volume = Mathf.Clamp((maxDistance - audioDistance)/maxDistance, 0, 1);
            walkAudio.Play();
        }

        if(horizontal == 0 && walkAudio.isPlaying)
        {
            walkAudio.Stop();
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontal));
            animator.SetBool("IsHitting", punching);
        }
    }

    #region Movement And Physics

    private void FixedUpdate()
    {
        CalculateVelocity();

        xVelocity = (xVelocity + xVelocity + lastXVel) / 3f;
        yVelocity = (yVelocity + yVelocity + lastYVel) / 3f;

        transform.Translate(xVelocity, yVelocity, 0);
        lastXVel = xVelocity;
        lastYVel = yVelocity;
    }

    public void CalculateVelocity()
    {
        if (verticalMomentum > gravity)
        {
            verticalMomentum += gravity * Time.fixedDeltaTime;
        }

        yVelocity = verticalMomentum * Time.fixedDeltaTime*0.6f;
        xVelocity = horizontal * Time.fixedDeltaTime*0.8f;

        if (xVelocity > 0)
        {
            if (SideCollision(horizontalOffset + xVelocity))
            {
                yVelocity = xVelocity;
                xVelocity = 0;
            }

        }
        else if (xVelocity < 0)
        {
            if (SideCollision(-horizontalOffset + xVelocity))
            {
                yVelocity = -xVelocity;
                xVelocity = 0;
            }
        }

        if (yVelocity < 0 && VerticalCollision(-0.02f))
        {
            yVelocity = 0;
        }
        else if (yVelocity > 0 && VerticalCollision(0.8f))
        {
            yVelocity = 0;
            verticalMomentum = 0;
        }
    }

    public bool VerticalCollision(float vOffset)
    {

        if (CheckCollision(-horizontalOffset, vOffset))
        {
            return true;
        }

        if (CheckCollision(0, vOffset))
        {
            return true;
        }

        if (CheckCollision(horizontalOffset, vOffset))
        {
            return true;
        }

        return false;
    }

    public bool SideCollision(float x)
    {
        for (float y = 0.01f; y < 0.8f; y += 0.04f)
        {
            if (CheckCollision(x, y))
            {
                return true;
            }
        }

        return false;
    }

    bool CheckCollision(float xOffset, float yOffset)
    {
        var pixel = World.Instance.CheckForPixel(transform.position.x + xOffset, transform.position.y + yOffset);
        return pixel.solidity > 0 || pixel.IsNull;
    }

    #endregion
}
