using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Player : MonoBehaviour, IDamageable
{
    #region Basic

    [SerializeField] Camera cam;
    [SerializeField] Transform model;
    [SerializeField] Animator animator;

    [Header("UI")]
    [SerializeField] Slider healthSlider;
    [SerializeField] GameObject storyPanel;
    [SerializeField] GameObject controlsPanel;
    [SerializeField] TMP_Text goldCount;

    int goldInInventory = 0;
    float health = 1;

    // Start is called before the first frame update
    void Start()
    {
        transform.position = World.GetSpawnPos;

        coolDown = digCooldown;

        //Calculating size of player's screen in game unit size
        screenWorldHeight = cam.orthographicSize * 2f;
        screenWorldWidth = cam.rect.width * screenWorldHeight * (Screen.width / (float)Screen.height);

        walkSound = PlayerManager.Instance.GetAudioSource(0);
        pickaxeSound = PlayerManager.Instance.GetAudioSource(1);
    }

    // Update is called once per frame
    void Update()
    {
        if(startState == StartState.Playing)
        {
            if(!isInside)
            {
                MovementInput();
            }
            else
            {
                HouseInput();
            }
            

            //Generating health
            if (health < 1)
            {
                AddHealth(0.05f * Time.deltaTime);
            }
        }
        else
        {
            CheckStartState();
        }       
    }

    public void AddHealth(float change)
    {
        health += change;
        if(health > 1)
        {
            health = 1;
        }
        else if(health <= 0)
        {
            Destroy(walkSound);
            Destroy(pickaxeSound);
            PlayerManager.Instance.OnDeath(gameObject);
        }
        healthSlider.value = health;
    }

    public void Damage()
    {
        AddHealth(-Time.deltaTime);
    }

    void PickGold()
    {
        goldInInventory++;
        goldCount.text = goldInInventory.ToString();
    }

    #endregion

    #region Controlling

    [Header("Controls")]
    [SerializeField] string horizontalInput;
    [SerializeField] string verticalInput;
    [SerializeField] string digKey;

    [Header("Movement")]
    [SerializeField] float jumpPower = 2f;
    [SerializeField] float digRadius = 0.24f;
    [SerializeField] float digCooldown = 0.5f;
    [SerializeField] float digOffset = 0.04f;

    AudioSource walkSound;
    AudioSource pickaxeSound;

    float horizontal;
    float vertical;

    bool jumpRequest;

    bool isDigging = false;
    float coolDown = 0;

    float screenWorldHeight;
    float screenWorldWidth;

    public void MovementInput()
    {
        CameraPos();
        isDigging = false;

        horizontal = Input.GetAxis(horizontalInput);
        vertical = Input.GetAxis(verticalInput);

        if(vertical>0&&House.Distance(transform.position)<0.4f)
        {
            GoInside();
        }

        //Mirroring sprite based on moving direction
        if (horizontal > 0 && model.localScale.x < 0)
        {
            model.localScale += Vector3.right * 2;
        }
        else if (horizontal < 0 && model.localScale.x > 0)
        {
            model.localScale -= Vector3.right * 2;
        }      

        if(isGrounded)
        {
            //If dig key is held, digging upwards, else trying to jump
            if (Input.GetKey(digKey))
            {
                isDigging = true;
                if (coolDown > 0)
                {
                    coolDown -= Time.deltaTime;
                    horizontal = 0;
                    vertical = 0;
                }
                else
                {
                    Dig(horizontal * digOffset, vertical * digOffset);
                    coolDown = digCooldown;
                }

            }
            else if (Input.GetKeyUp(digKey))
            {
                coolDown = digCooldown;
            }
            else if (vertical > 0)
            {
                jumpRequest = true;
            }

            //Walksound
            CheckSound(walkSound, horizontal != 0);
        }
        else if(walkSound.isPlaying)
        {
            walkSound.Stop();
        }

        //Diggind sound
        CheckSound(pickaxeSound, isDigging);

        //Sending values to animator
        animator.SetFloat("Speed", horizontal);
        animator.SetFloat("Momentum", verticalMomentum);
        animator.SetBool("Grounded", isGrounded);
        animator.SetBool("Digging", isDigging);
    }

    void Dig(float offsetX, float offsetY)
    {
        //Initializing values
        float centerX = transform.position.x + offsetX;
        float centerY = transform.position.y + offsetY + digRadius;
        

        for (float x = -digRadius; x <= digRadius; x+=0.01f)
        {
            for (float y = -digRadius; y <= digRadius; y+=0.01f)
            {
                //We want circle instead of square, so we skip pixels
                // with straight line distance over radius
                float distance = Mathf.Sqrt(x * x + y * y);

                if (distance >= digRadius) continue;

                if(World.Instance.DigPixel(centerX + x, centerY+y))
                {
                    //Digged gold
                    PickGold();
                }
            }
        }
    }

    void CameraPos()
    {
        //Initializing values
        float playerDistanceToRightBorder = World.WORLD_WIDTH - transform.position.x;
        float playerDistanceToLeftBorder = transform.position.x;
        float halfWidth = screenWorldWidth / 2f;
        float x=0;
        float y=0.2f;

        //Preventing camera to go out of the world in horizontal axis
        if (playerDistanceToRightBorder < halfWidth)
        {
            x = playerDistanceToRightBorder-halfWidth;
        }
        else if(playerDistanceToLeftBorder < halfWidth)
        {
            x = halfWidth-playerDistanceToLeftBorder;
        }

        //Preventing camera to go below the world
        if(transform.position.y<screenWorldHeight/2f)
        {
            y=screenWorldHeight/2f-transform.position.y + 0.2f;
        }

        if(x!=0||y!=0.2f)
            cam.transform.localPosition = new(x, y, cam.transform.localPosition.z);
    }

    public void ScaleCameraRect()
    {
        //Full screen
        Debug.Log($"{gameObject} scaled");
        cam.rect = new Rect(0, 0, 1, 1);
        screenWorldHeight = cam.orthographicSize * 2f;
        screenWorldWidth = cam.rect.width * screenWorldHeight * (Screen.width / (float)Screen.height);
    }

    public void CheckSound(AudioSource source, bool condition)
    {
        if (condition && !source.isPlaying)
        {
            source.Play();
        }
        else if (!condition && source.isPlaying)
        {
            source.Stop();
        }
    }

    #endregion

    #region Physics

    [SerializeField] float horizontalOffset = 0.1f;
    [SerializeField] float gravity = -1.6f;

    float lastXVel = 0;
    float lastYVel = 0;
    float xVelocity;
    float yVelocity;

    float verticalMomentum;
    bool isGrounded;

    private void FixedUpdate()
    {
        if (isInside) return;

        if (isDigging)
        {
            horizontal += horizontal * digOffset;
        }

        CalculateVelocity();

        //Jumping
        if (jumpRequest)
        {
            verticalMomentum = jumpPower;
            jumpRequest = false;
        }

        //Smoothing out the movement
        if (isGrounded)
        {
            xVelocity = (xVelocity + xVelocity + lastXVel) / 3f;
        }
        else
        {
            xVelocity = (xVelocity + lastXVel + lastXVel) / 3f;
        }

        yVelocity = (yVelocity + yVelocity + lastYVel) / 3f;


        //Actually moving
        transform.Translate(xVelocity, yVelocity, 0);

        //Saving old velocity
        lastXVel = xVelocity;
        lastYVel = yVelocity;
    }

    public void CalculateVelocity()
    {
        //Applying gravity
        if (verticalMomentum > gravity)
        {
            verticalMomentum += gravity * Time.fixedDeltaTime;
        }

        //Initial velocity
        yVelocity = verticalMomentum * Time.fixedDeltaTime;
        xVelocity = horizontal * Time.fixedDeltaTime;

        if (xVelocity > 0)
        {
            //Checking collision in right
            if (SideCollision(horizontalOffset + xVelocity))
            {
                yVelocity = xVelocity;
                xVelocity = 0;
            }

        }
        else if (xVelocity < 0)
        {
            //Checking collision in left
            if (SideCollision(-horizontalOffset + xVelocity))
            {
                yVelocity = -xVelocity;
                xVelocity = 0;
            }
        }

        if (yVelocity < 0 && VerticalCollision(-0.02f))
        {
            //Checking collison below
            yVelocity = 0;
            isGrounded = true;
        }
        else if (yVelocity > 0 && VerticalCollision(0.4f))
        {
            //Checking collison above
            yVelocity = 0;
            verticalMomentum = 0;
            isGrounded = false;
        }
        else
        {
            isGrounded = false;
        }
    }

    public bool VerticalCollision(float vOffset)
    {
        //Bottom-left check
        if (CheckCollision(-horizontalOffset, vOffset))
        {
            return true;
        }

        //Bottom-center check
        if (CheckCollision(0, vOffset))
        {
            return true;
        }

        //Bottom-right check
        if (CheckCollision(horizontalOffset, vOffset))
        {
            return true;
        }

        return false;
    }

    public bool SideCollision(float x)
    {
        //Checking every second pixel next to character
        for (float y = 0.01f; y < 0.4f; y += 0.02f)
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
        //Checking if pixel in position is solid
        var pixel = World.Instance.CheckForPixel(transform.position.x + xOffset, transform.position.y + yOffset);
        return pixel.solidity > 0 || pixel.IsNull;
    }

    #endregion

    #region House

    [Header("")]
    [SerializeField] HouseMenu houseMenu;

    bool isInside;

    void HouseInput()
    {
        houseMenu.Move(Mathf.RoundToInt(Input.GetAxis(horizontalInput)));
        houseMenu.UpgradePrices();
        if(Input.GetKeyDown(digKey))
        {
            houseMenu.Choose();
            GoOutside();
        }
    }

    void GoInside()
    {
        isInside = true;

        houseMenu.gameObject.SetActive(true);
        houseMenu.UpgradePrices();

        //We move player to distant position but keep camera pointing house
        transform.position = new(1000, 0);
        cam.transform.position = new(House.Instance.transform.position.x, House.Instance.transform.position.y + 0.21f, -5);

        //We drop all gold to house but if storage is full, some is returned.
        goldInInventory = House.Instance.AddGold(goldInInventory);
        goldCount.text = goldInInventory.ToString();
    }

    void GoOutside()
    {
        transform.position = House.Instance.transform.position + Vector3.up * 0.01f;
        cam.transform.localPosition = new(0, 0 + 0.2f, -5);
        houseMenu.gameObject.SetActive(false);
        isInside = false;
    }

    #endregion

    [HideInInspector] public StartState startState = StartState.InStory;
    public void CheckStartState()
    {
        if(startState==StartState.InStory && Input.GetKeyDown(digKey))
        {
            storyPanel.SetActive(false);
            controlsPanel.SetActive(true);
            startState = StartState.InControls;
        }
        else if(startState==StartState.InControls && (Input.GetAxis(horizontalInput)!=0||Input.GetAxis(verticalInput)!=0))
        {
            controlsPanel.SetActive(false);
            startState = StartState.Playing;
            PlayerManager.Instance.StartGame();
        }
    }
}

interface IDamageable
{
    public void Damage();
}

public enum StartState { InStory, InControls, Playing}