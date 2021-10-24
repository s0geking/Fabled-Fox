using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class ReaperController : Entity
{
    [Header("Controls")] 
    public PlayerInput playerInput;
    private Vector2 _move;
    private Vector3 _velocity;
    private Vector3 _position;
    private Vector3 _vxyz;
    private Vector3 _rotationxyz;
    private float heading;
    public Rigidbody rb;
    public int moveSpeed = 10;
    private int _mvSpd = 10;
    public int rotationSpeed = 10;
    public float lerpVal = .3f;
    private Collider movementCollider;
    public List<PhysicMaterial> physicsMaterials;
    public Gamepad playerGamepad = null;
    private string currentControlScheme = "";
    
    [Header("Rumble")]
    public Vector2 rumbleAVals; //x = start value y = end value
    public Vector2 rumbleBVals; //x = start value y = end value
    public Vector2 tapRumbleAVals; //x = start value y = end value
    public Vector2 tapRumbleBVals; //x = start value y = end value
    public float currentRumbleA;
    public float currentRumbleB;
    public float rumbleDuration;
    public float tapRumbleDuration;
    private float rumbleAStep;
    private float rumbleBStep;
    private float rumbleTime; //The time that Time.time must be greater than for the rumble to stop
    private bool tapRumble;

    [Header("Warping")] 
    public float warpDistance = 2f;
    public LayerMask blockingLayers;
    private bool warpAvailable = true;
    public float warpCoolDown = 3f;
    private float warpCoolDownEndTime = 0f;
    public ParticleSystem warpingParticles;

    [Header("Attack")] 
    public AttackingStaff attackingStaff;
    public Transform particlePos;
    public float knockbackStrength = 10f;
    public enum scythComboState {Inactive, Light1, Light2, Light3, HeavyTap, HeavyLaunch, LightHold};
    public scythComboState currentScythState = scythComboState.Inactive;
    private scythComboState _currentlyExecutingScythComboState = scythComboState.Inactive;
    private ComboTree _comboTree = new ComboTree(new ComboNode(scythComboState.Inactive));
    private float _lastComboHitTime = -1f;
    private float _comboEndTime = 0f;
    public float comboLength = 1.5f;
    public bool currentlyAnimatingAttack = false;
    public bool rotationLock = false;
    private Queue<scythComboState> _inputBuffer = new Queue<scythComboState>();
    public LayerMask enemyLayers;
    public HeavyTap baseHeavyAttack;
    public HeavyHold heavyLaunchAttack;
    public ParticleSystem slashParticles;
    public ParticleSystem heavyParticles;
    public float particleDestructionTime = .2f;
    private Queue<ParticleSystem> activeSlashes = new Queue<ParticleSystem>();
    private bool lightHoldStarted = false;
    private bool lightHoldIgnore = false;
    private float lightHoldLength = 0f;
    public float lightHoldTime = 1f;
    public int lightHoldMovementSpeed = 3; //Was the former heavy attack
    private bool lightHoldStepDownDone = false; //Used to determine if the tree has bee stepped down for this charge press
    private bool heavyHoldStarted = false;
    private bool heavyHoldIgnore = false;
    private float heavyHoldStartTime = 0f;
    public float heavyHoldLength = 1f;
    public int heavyHoldMovementSpeed = 3; //The magic attack
    public MagicRing magicRingAttack;

    [Header("Throwing")] 
    public float throwSpeed = .5f;
    public float throwPointsT = .05f;
    public bool holdingStaff = true;
    public GameObject parentHand;
    public GameObject throwingStaff;
    public float throwDistance = 8f;
    public float maxArcHeight = 4f;
    private Vector3 throwDest;
    public bool inAir = false;
    private bool returningToPlayer = false;
    private float recallTimeWindow = 0f;
    public float recallTimeLength = .5f;
    private List<Vector3> throwCurve = new List<Vector3>();
    private int arcIndex = 0;
    public GameObject throwTargetPrefab;
    private GameObject throwTarget;
    private bool throwDown = false;
    private bool throwDownIgnore = false;
    private float _currentThrowDistance = 8f;
    public int throwPlayerMovementSpeed = 3;
    public ParticleSystem ingroundParticlesPrefab;
    private GameObject _activeIngroundParticles;
    public ParticleSystem throwTrailPrefab;
    private GameObject _activeThrowTrail;
    
    [Header("Attack Damage")] 
    public float lightSlash1 = 10f;
    public float lightSlash2 = 10f;
    public float lightSlash3 = 10f;
    public float HeavyAttack1 = 10f;
    public float HeavyAttack2 = 10f;
    public float lightHoldDamage = 10f;
    private float currentLigthHoldDamage = 0f;
    
    [Header("Spirits")]
    public List<SpiritFriend> spiritsFriends = new List<SpiritFriend>();
    public List<GameObject> spiritSpots = new List<GameObject>();

    [Header("Health/hearts")] 
    public int numHearts = 1;
    public int currHeartNum = 1;
    public bool invincible = false;
    public Image[] hearts;
    public Sprite heartFull;
    public Sprite heartEmpty;
    private bool moveDeadBody = false;

    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public GameObject gamepadInteract;
    public GameObject keyboardInteract;
    private DialogueTrigger textStone;

    [Header("Other")] 
    private Animator playerAnimator;
    public GameObject playerModel;
    public SkinnedMeshRenderer meshRend;
    private List<Color> baseColor = new List<Color>();
    public float staffHeight = 6f;
    public LayerMask groundLayers;
    public AudioManager audioMan;
    public List<AudioClip> littleFoxSounds;
    private MenuController _menuController;
    public ArenaManager currentArena;
    private bool ignoreCurrentlyAnimating = false;
    private CinemachineImpulseSource impluseSource;
    public float impulseForce = 1f;
    public NoiseSettings heavyShake;
    public NoiseSettings lightningShake;
    public NoiseSettings lightShake;
    //public EnemyDebug debug_script;
    private MenuController menuCon;


    private void Awake()
    {
        /*playerInput = new PlayerController();
        playerInput.CharacterController.Movement.performed += ctx => _move = ctx.ReadValue<Vector2>();
        playerInput.CharacterController.Movement.canceled += ctx => _move = Vector2.zero;*/
        playerInput = GetComponent<PlayerInput>();
        currentControlScheme = playerInput.currentControlScheme;
        if (currentControlScheme == "Gamepad")
        {
            playerGamepad = Gamepad.current;
        }

        impluseSource = GetComponent<CinemachineImpulseSource>();
        heavyHoldStarted = false;
        throwDown = false;
        lightHoldStarted = false;

        menuCon = GameObject.FindWithTag("Menu Manager").GetComponent<MenuController>();
        menuCon.SwitchScheme(currentControlScheme);
    }

    /*private void OnEnable()
    {
        playerInput.CharacterController.Enable();
    }

    private void OnDisable()
    {
        playerInput.CharacterController.Disable();
    }*/

    /// <summary>
    /// This code is used to interact with the Unity Input System when movement input
    /// is received this function will be called. The Vector2 value given will be used to set the
    /// _move variable.
    /// </summary>
    /// <param name="value">Vector2 that contains movement controls information</param>
    public void OnMovement(InputValue value)
    {
        
        _move = value.Get<Vector2>();
    }

    public void OnPause()
    {
        _menuController.PausePressed();
    }
    // When read button is pressed, this function will check if there are any nearby text stones and open up a dialogue box with them
    public void OnRead()
    {
        // Return if not near any textStones
        if (!textStone)
            return;
        /*
        // Determine which text stone you are facimg
        DialogueTrigger facingStone = textStones[0];
        float largestDot = -100; 
        foreach(DialogueTrigger dTrigger in textStones)
        {
            float dotProduct = Vector3.Dot(transform.forward, dTrigger.transform.position - transform.position); // Dot product compares parallelism of the direction player is facing with the direction to each stone nearby
            if ( dotProduct > largestDot)
            {
                facingStone = dTrigger;
                largestDot = dotProduct;
            }
            
        }*/
        // Trigger the dialogue of the stone you are facing towards
       textStone.TriggerDialogue(playerGamepad != null);
    }
    // Calls dialoguemanager to display ui text
    public void OnNext()
    {
        dialogueManager.DisplayNextSentence();
    }
    // Button to press to know next button will be a debug command
    public void OnDebug()
    {
        Debug.Log("Debug Activated (Currently Inactive)");
        //The switching control schemes for this causes the player to lose control of the character
        // Seems unnecessary Pressing this button could kill the enemies without doing this
        //playerInput.SwitchCurrentActionMap("Debug");
    }
    // Debug command that will kill the next group of enemies
    public void OnKillAll()
    {
        Debug.Log("Kill All presssed");
        //debug_script.KillNextGroup();
        playerInput.SwitchCurrentActionMap("Character Controller");
    }
    /// <summary>
    /// This function is used to interact with the Unity Input System when the slash button is pressed
    /// (west button on controllers and the left mouse button on keyboards). It will activate the slashing animation
    /// and it does work for the combo system as well. If the combo is in the inactive state then it will set the
    /// _lastComboHitTime variable and will set the _comboEndTime which will determine when the combo time frame ends
    /// </summary>
    public void OnSlash()
    {
        /*if (currentScythState == scythComboState.Inactive)
        {
            _lastComboHitTime = Time.time;
            _comboEndTime = _lastComboHitTime + comboLength;
        }*/
        //NextScythState();
        if (holdingStaff)
        {
            Debug.Log("Light Pressed");
            
            StepDownTree(2);
            if (_inputBuffer.Count < 1)
            {
                _inputBuffer.Enqueue(currentScythState);
            }

            if (!currentlyAnimatingAttack && _inputBuffer.Count == 1)
            {
                PopInputBuffer();
            }
            
        }
    }

    public void PopInputBuffer()
    {
        if (_inputBuffer.Count > 0)
        {
            //dequeue the next move
            _currentlyExecutingScythComboState = _inputBuffer.Dequeue();
            //activate all the things for this state
            if (_currentlyExecutingScythComboState == scythComboState.Inactive)
            {
                PopInputBuffer();
                return;
            }
            
            ActivateCurrentScythState();
      
            if (_currentlyExecutingScythComboState == scythComboState.HeavyLaunch ||
                _currentlyExecutingScythComboState == scythComboState.HeavyTap)
            {
                //playerAnimator.SetTrigger("Heavy");
                ActivateMagic();
            }
            else
            {
                if (_currentlyExecutingScythComboState == scythComboState.LightHold && lightHoldStarted)
                {
                    playerAnimator.SetTrigger("ChargeUp");
                }
                else
                {
                    playerAnimator.SetTrigger("Slash");
                }

                SetUpTapRumble();
            }
        }
    }

    public bool IsInputBufferEmpty()
    {
        return _inputBuffer.Count == 0;
    }

    private void StepDownTree(int direction)
    {
        _comboTree.StepDownTree(direction);
        currentScythState = _comboTree.GetCurrent().GetValue();
        if (_comboTree.GetCurrent().GetRightChild() != null || _comboTree.GetCurrent().GetLeftChild() != null || _comboTree.GetCurrent().GetMiddleChild() != null)
        {
            IncreaseComboLength();
        }
    }

    private void IncreaseComboLength()
    {
        _lastComboHitTime = Time.time;
        _comboEndTime = _lastComboHitTime + comboLength;
    }
    
    private void ActivateCurrentScythState()
    {
        switch (_currentlyExecutingScythComboState)
        {
            case scythComboState.Light1:
                playerAnimator.SetInteger("State",1);
                audioMan.FindAndPlay("PlayerLightAttack");
                impluseSource.m_ImpulseDefinition.m_RawSignal = lightShake;
                break;
            case scythComboState.Light2:
                playerAnimator.SetInteger("State",2);
                audioMan.FindAndPlay("PlayerLightAttack");
                impluseSource.m_ImpulseDefinition.m_RawSignal = lightShake;
                //_comboEndTime = _lastComboHitTime + (comboLength * 2);
                break;
            case scythComboState.Light3:
                playerAnimator.SetInteger("State",3);
                audioMan.FindAndPlay("PlayerLightAttack");
                break;
            case scythComboState.HeavyTap:
                playerAnimator.SetInteger("State", 1);
                //audioMan.FindAndPlay("PlayerHeavyAttack");
                //audioMan.PlayOneShot(littleFoxSounds[0]);
                break;
            case scythComboState.HeavyLaunch:
                playerAnimator.SetInteger("State", 2);
                audioMan.FindAndPlay("PlayerHeavyAttack");
                //audioMan.PlayOneShot(littleFoxSounds[0]);
                break;
            case scythComboState.LightHold:
                playerAnimator.SetInteger("State", 1);
                //audioMan.FindAndPlay("PlayerHeavyAttack");
                //audioMan.PlayOneShot(littleFoxSounds[0]);
                break;
        }
    }

    /*public void OnSpiritPower()
    {
        if (spiritsFriends.Count > 0)
        {
            if (spiritsFriends.Count > 1)
            {
                spiritsFriends[1].followTarget = spiritsFriends[0].followTarget;
            }
            spiritsFriends[0].ActivatePower();
            spiritsFriends.RemoveAt(0);
        }
    }*/

    public void OnHeavy()
    {
        /*Debug.Log("Heavy Tap");
        if (holdingStaff && !lightHoldStarted)
        {
            StepDownTree(0);
            if (_inputBuffer.Count < 1)
            {
                _inputBuffer.Enqueue(currentScythState);
            }

            if (!currentlyAnimatingAttack && _inputBuffer.Count == 1)
            {
                PopInputBuffer();
            }
        }*/
        if (!heavyHoldIgnore && !lightHoldStarted)
        {
            if (!heavyHoldStarted)
            {
                heavyHoldStarted = true;
                heavyHoldStartTime = Time.time;
                StepDownTree(0);
                if (_inputBuffer.Count < 1)
                {
                    _inputBuffer.Enqueue(currentScythState);
                }

                if (!currentlyAnimatingAttack && _inputBuffer.Count == 1)
                {
                    PopInputBuffer();
                }

                //lightHoldStepDownDone = false;
                Debug.Log("Started hold");
            }
            else
            {

                heavyHoldStarted = false;
                //ignoreCurrentlyAnimating = false;
                if (Time.time >= heavyHoldStartTime + heavyHoldLength)
                {
                    //playerAnimator.SetBool("Charging", false);
                    //playerAnimator.SetTrigger("ChargeDown");
                    //audioMan.FindAndPlay("PlayerHeavyAttack");
                    impluseSource.m_ImpulseDefinition.m_RawSignal = lightningShake;
                    magicRingAttack.Attack();
                    Debug.Log("Held Attack");
                }

                magicRingAttack.EndAnimation();


                //Reset movement speed back normal
                moveSpeed = _mvSpd;
            }
        }
        else
        {
            heavyHoldIgnore = false;
        }
    }

    public void ActivateMagic()
    {
        /*switch (_currentlyExecutingScythComboState)
        {
            case scythComboState.HeavyTap:
                //play base heavy animation
                baseHeavyAttack.StartAnimation();
                break;
            case scythComboState.HeavyLaunch:
                //play heavy launch animation
                heavyLaunchAttack.StartAnimation();
                break;
        }*/
        magicRingAttack.StartAnimation();
        moveSpeed = heavyHoldMovementSpeed;
    }
    
    public void ActivateLightHold()
    {
        
        baseHeavyAttack.StartAnimation();
        
            
    }

    public void OnLightHold()
    {

        if (!lightHoldIgnore && !heavyHoldStarted)
        {
            if (!lightHoldStarted)
            {
                lightHoldStarted = true;
                lightHoldLength = Time.time;
                lightHoldStepDownDone = false;
                //Debug.Log("Started hold");
            }
            else
            {
                lightHoldStarted = false;
                ignoreCurrentlyAnimating = false;
                lightHoldStepDownDone = false;
                if (Time.time - lightHoldLength >= .3f)
                {
                    impluseSource.m_ImpulseDefinition.m_RawSignal = heavyShake;
                    playerAnimator.SetBool("Charging", false);
                    playerAnimator.SetTrigger("ChargeDown");
                    audioMan.FindAndPlay("PlayerHeavyAttack");
                }

                //Reset movement speed back normal
                moveSpeed = _mvSpd;
            }
        }
        else
        {
            lightHoldIgnore = false;
        }

    }

    public void ShakeCamera()
    {
        //impluseSource.m_ImpulseDefinition.m_AmplitudeGain = 5f;
        impluseSource.GenerateImpulse(impulseForce);
    }

    public void TimeStop(float stopLength)
    {
        //Call coroutine that will stop and start time
        StopCoroutine(TimeStopper(stopLength));
        StartCoroutine(TimeStopper(stopLength));
    }

    /// <summary>
    /// When the warp button is pressed the player will "teleport" out the specified warp distance. It will
    /// send out a raycast and if there is a wall in the way then the player will be teleported up to where
    /// the wall stopped them.
    /// </summary>
    public void OnWarp()
    {
        if (warpCoolDownEndTime < Time.time && warpAvailable)
        {
            //RaycastHit hitObject;

            if (!holdingStaff)
            {
                warpCoolDownEndTime = Time.time + warpCoolDown;
                warpAvailable = false;
                TeleportationDamage();
                var newWarpParticles = Instantiate(warpingParticles, transform.position, transform.rotation);
                transform.position = throwingStaff.transform.position;
                audioMan.FindAndPlay("PlayerWarp");
                StartCoroutine(MoveParticleEffect(newWarpParticles.gameObject));
                //newWarpParticles.gameObject.transform.position = transform.position;
                Destroy(_activeThrowTrail);
                if (_activeIngroundParticles)
                {
                    Destroy(_activeIngroundParticles);
                }

                if (heavyHoldStarted)
                {
                    heavyHoldStarted = false;
                    impluseSource.m_ImpulseDefinition.m_RawSignal = lightningShake;
                    ShakeCamera();
                    magicRingAttack.Attack();
                }
                
            }
            /*else{

                if (Physics.Raycast(transform.position, transform.forward, out hitObject, warpDistance, blockingLayers))
                {
                    if (hitObject.distance > .55f)
                    {
                        Warp(hitObject.distance);
                    }
                    else
                    {
                        //Possibly fake warping here to prevent warping into wall
                        // ie. all of the effects and sounds from warp, but not actually doing anything
                    }
                }
                else
                {
                    Warp(warpDistance);
                }
            }*/
        }
    }

    /// <summary>
    /// Does the actual heavy lifting of "teleporting" the player out the specific warp distance.
    /// </summary>
    /// <param name="warpDist">A float that represents the distance to move the player</param>
    private void Warp(float warpDist)
    {
        //This function is current not in use
        Vector3 forward = transform.forward;
        int x_dir = forward.x < 0 ? -1 : 1;
        int z_dir = forward.z < 0 ? -1 : 1;
        Vector3 warp_pos = transform.position;
        
        if ((Mathf.Abs(forward.x) > .9f && Mathf.Abs(forward.x) <= 1f) ||
            (Mathf.Abs(forward.z) > .9f && Mathf.Abs(forward.z) <= 1f))
        {
            //int temp = Mathf.FloorToInt(forward.x);
            if ((Mathf.Abs(forward.x) > .9f && Mathf.Abs(forward.x) <= 1f))
            {
                warp_pos.x = warp_pos.x + (warpDist * x_dir);
            }
            else
            {
                warp_pos.z = warp_pos.z + (warpDist * z_dir);
            }
        }
        else
        {
            warp_pos.x = warp_pos.x + (warpDistance * x_dir);
            warp_pos.z = warp_pos.z + (warpDistance * z_dir);
        }
        
        transform.position = warp_pos;
    }

    public void OnThrow()
    {
        if (!throwDownIgnore)
        {
            if (!throwDown)
            {
                throwDown = true;
                if (holdingStaff)
                {
                    throwTarget = Instantiate(throwTargetPrefab, transform, true);
                    playerAnimator.SetBool("Charging", true);
                    playerAnimator.SetTrigger("Throw");
                    moveSpeed = throwPlayerMovementSpeed;
                }
            }
            else
            {
                Debug.Log("Throw");
                throwDown = false;
                moveSpeed = _mvSpd;
                if (holdingStaff)
                {
                    Destroy(throwTarget);
                    RaycastHit hitObject;
                    //Instantiate the trail render for the throw
                    _activeThrowTrail = Instantiate(throwTrailPrefab, throwingStaff.transform).gameObject;
                    if (Physics.Raycast(transform.position, transform.forward, out hitObject, throwDistance,
                        blockingLayers))
                    {
                        _currentThrowDistance = hitObject.distance - .2f;
                    }
                    else
                    {
                        _currentThrowDistance = throwDistance;
                    }

                    //StartCoroutine(ThrowStaff(tempPos));
                    playerAnimator.SetBool("Charging", false);
                    playerAnimator.SetTrigger("ThrowEnd");
                }
                else
                {
                    //Delete the in ground particles if it exists
                    if (_activeIngroundParticles)
                    {
                        Destroy(_activeIngroundParticles);
                    }
                    //throwDest = transform.position;
                    returningToPlayer = true;
                    recallTimeWindow = Time.time + recallTimeLength;
                    arcIndex = 3;
                    throwCurve.Clear();
                    for (float i = 0; i <= 1; i += throwPointsT)
                    {
                        throwCurve.Add(QuadraticCurve(throwingStaff.transform.position,
                            throwingStaff.transform.position, transform.position, i));
                    }
                }
            }
        }
        else
        {
            throwDownIgnore = false;
        }
    }

    public void TriggerThrow()
    {
        ThrowStaff(_currentThrowDistance);
    }

    private void ThrowStaff(float throwDist)
    {
        Vector3 tempPos = transform.position;
        Vector3 tempHalfPos = tempPos;
        tempHalfPos.y += maxArcHeight;
        Vector3 forward = transform.forward;
        int x_dir = forward.x < 0 ? -1 : 1;
        int z_dir = forward.z < 0 ? -1 : 1;

        if ((Mathf.Abs(forward.x) > .9f && Mathf.Abs(forward.x) <= 1f) ||
            (Mathf.Abs(forward.z) > .9f && Mathf.Abs(forward.z) <= 1f))
        {
            //int temp = Mathf.FloorToInt(forward.x);
            if ((Mathf.Abs(forward.x) > .9f && Mathf.Abs(forward.x) <= 1f))
            {
                //tempPos.x += (8 * x_dir);
                tempPos.x += (throwDist * forward.x);
                tempHalfPos.x = (transform.position.x + tempPos.x) / 2;
            }
            else
            {
                //tempPos.z += (8 * z_dir);
                tempPos.z += (throwDist * forward.z);
                tempHalfPos.z = (transform.position.z + tempPos.z) / 2;
            }
        }
        else
        {
            tempPos.x += (throwDist * forward.x);
            tempHalfPos.x = (transform.position.x + tempPos.x) / 2;
            
            tempPos.z += (throwDist * forward.z);
            tempHalfPos.z = (transform.position.z + tempPos.z) / 2;
        }
        
        Vector3 tempTempPos = tempPos;
        
        RaycastHit hitObjectDown;
        //Down raycast to find where it hits the ground
        if (Physics.Raycast(tempTempPos, -transform.up, out hitObjectDown, 50, groundLayers))
        {
            tempPos = hitObjectDown.point;
            tempHalfPos.y -= hitObjectDown.distance;
        }

        RaycastHit hitObjectUp;
        //Raycast up to see if it hits anything. Used for going up slopes
        if (Physics.Raycast(tempTempPos, transform.up, out hitObjectUp, 10, groundLayers))
        {
            tempPos = hitObjectUp.point;
            tempHalfPos.y += hitObjectUp.distance;
        }

        holdingStaff = false;
        inAir = true;
        throwDest = tempPos;
        //halfwayDest = tempHalfPos;
        throwingStaff.transform.parent = null;
        throwingStaff.transform.rotation = Quaternion.Euler(-90,0,0);
        arcIndex = 3;
        recallTimeWindow = Mathf.Infinity;
        throwCurve.Clear();
        
        for (float i = 0; i <= 1; i += throwPointsT)
        {
            throwCurve.Add(QuadraticCurve(transform.position, tempHalfPos, tempPos, i));
        }
    }
    
    // Calculates a point in the throw curve
    private Vector3 QuadraticCurve(Vector3 start, Vector3 mid, Vector3 end, float t)
    {
        Vector3 p0 = Vector3.Lerp(start, mid, t);
        Vector3 p1 = Vector3.Lerp(mid, end, t);
        return Vector3.Lerp(p0, p1, t);
    }

    /// <summary>
    /// Sets the throwDest to a new point. This will be used to stop the staff from clipping through stuff.
    /// </summary>
    /// <param name="newDest">New point to set as the destination for the throw.</param>
    public void SetTargetDest(Vector3 newDest)
    {
        throwDest = newDest;
    }

    // Start is called before the first frame update
    void Start()
    {
        BuildComboTree();
        movementCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>(); // Pull the rigidbody component from the player
        playerAnimator = playerModel.GetComponent<Animator>();
        currHeartNum = numHearts;
        //meshRend = GetComponent<MeshRenderer>();
        for (int i = 0; i < meshRend.materials.Length; i++)
        {
            baseColor.Add(new Color(meshRend.materials[i].color.r, meshRend.materials[i].color.g, meshRend.materials[i].color.b, meshRend.materials[i].color.a));
        }

        audioMan = GameObject.FindWithTag("Audio Manager").GetComponent<AudioManager>();
        _menuController = GameObject.FindWithTag("Menu Manager").GetComponent<MenuController>();
        GameObject dManager = GameObject.FindWithTag("Dialogue Manager");
        if (dManager)
            dialogueManager = dManager.GetComponent<DialogueManager>();
        GameObject gm = GameObject.FindWithTag("Game Manager");
        _mvSpd = moveSpeed;
        // if (gm && gm.GetComponent<EnemyDebug>())
        // {
        //     debug_script = gm.GetComponent<EnemyDebug>();
        // }
    }

    // Update is called once per frame
    void Update()
    {

        //Switch the player's physics materials between slick and high friction
        if (_move.x == 0 && _move.y == 0)
        {
            //Switch in high friction material
            movementCollider.material = physicsMaterials[1];
        }
        else
        {
            //Switch in slick material
            movementCollider.material = physicsMaterials[0];
        }

        if (!ignoreCurrentlyAnimating)
        {
            //Stop the player's movement when animating an attack
            if (currentlyAnimatingAttack) rb.velocity = Vector3.zero;
        }

        //Combo
        if (_comboEndTime < Time.time)
        {
            currentScythState = scythComboState.Inactive;
            _comboTree.ResetCurrent();
            warpAvailable = true;
            //_inputBuffer.Clear();
        }
        
        //Hold spear again
        if (((Vector2.Distance(new Vector2(throwingStaff.transform.position.x, throwingStaff.transform.position.z),
            new Vector2(transform.position.x, transform.position.z)) <= .3) || (recallTimeWindow <= Time.time && returningToPlayer)) && !holdingStaff)
        {
            var tempDist = Vector2.Distance(new Vector2(throwingStaff.transform.position.x, throwingStaff.transform.position.z),
                new Vector2(transform.position.x, transform.position.z));
            holdingStaff = true;
            returningToPlayer = false;
            throwingStaff.transform.parent = parentHand.transform;
            throwingStaff.transform.localPosition = new Vector3(0f, 0f, 0f);
            throwingStaff.transform.rotation = new Quaternion(0, 0, 0, 0);
        }
        else if (!holdingStaff && returningToPlayer)
        {
            throwDest = transform.position;
        }
        //Debug.Log(_inputBuffer.Count);
        /*else
        {
            if (!currentlyAnimatingAttack && _inputBuffer.Count > 1)
            {
                PopInputBuffer();
            }
        }*/
        
        // Getting the current control scheme
        if (currentControlScheme != playerInput.currentControlScheme)
        {
            currentControlScheme = playerInput.currentControlScheme;
            playerGamepad = currentControlScheme == "Gamepad" ? Gamepad.current : null;
            menuCon.SwitchScheme(currentControlScheme);
            //Debug.Log(currentScheme);
        }
        // Controller Rumble
        if (playerGamepad != null && Time.time < rumbleTime)
        {
            if (tapRumble)
            {
                playerGamepad.SetMotorSpeeds(currentRumbleA, currentRumbleB);
                
            }
            else
            {
                
                playerGamepad.SetMotorSpeeds(currentRumbleA, currentRumbleB);
                currentRumbleA += (rumbleAStep * Time.deltaTime);
                currentRumbleB += (rumbleBStep * Time.deltaTime);
            }
        }else if (playerGamepad != null)
        {
            
            playerGamepad.SetMotorSpeeds(0, 0);
            
        }
        
        // Have something that will check hold long the button is being held
        // if it's below a threshold don't start the windup animation
        if (lightHoldStarted && Time.time - lightHoldLength >= .3f && !lightHoldStepDownDone && holdingStaff)
        {
            lightHoldStepDownDone = true;
            moveSpeed = lightHoldMovementSpeed;
            ignoreCurrentlyAnimating = true;
            playerAnimator.SetBool("Charging", true);
            if (currentScythState == scythComboState.Light1 || currentScythState == scythComboState.Light2)
            {
                StepDownTree(0);
            }
            else
            {
                StepDownTree(1);
            }

            if (_inputBuffer.Count < 1)
            {
                _inputBuffer.Enqueue(currentScythState);
            }

            if (!currentlyAnimatingAttack && _inputBuffer.Count == 1)
            {
                PopInputBuffer();
            }
            
        }
        
        if (throwDown && holdingStaff)
        {
            GetThrowDest();
        }

        //Camera Shake
        //impluseSource.m_ImpulseDefinition.m_AmplitudeGain =
        //Mathf.Lerp(impluseSource.m_ImpulseDefinition.m_AmplitudeGain, 0, .01f);

    }

    private void FixedUpdate()
    {
        if (!currentlyAnimatingAttack || ignoreCurrentlyAnimating)
        {
            //Movement (Physics based)
            _vxyz.x = _move.x;
            _vxyz.z = _move.y;
            playerAnimator.SetFloat("Movement", Mathf.Abs(_move.magnitude));
            _velocity = rb.velocity;

            _vxyz.x *= moveSpeed;
            _vxyz.z *= moveSpeed;

            _velocity = Vector3.Lerp(_velocity, _vxyz, lerpVal);
            _velocity.y = rb.velocity.y;
            rb.velocity = _velocity;
            
            //Movement (Non-Physics based)
            // _vxyz.x = _move.x + gameObject.transform.position.x;
            // _vxyz.z = _move.y + gameObject.transform.position.z;
            // playerAnimator.SetFloat("Movement", Mathf.Abs(_move.magnitude));
            // _position = gameObject.transform.position;
            //
            // _vxyz.x *= moveSpeed;
            // _vxyz.z *= moveSpeed;
            // //_vxyz *= Time.deltaTime;
            // Vector3 temp_xyz = _vxyz;
            // temp_xyz.Normalize();
            //
            // var inputMag = Mathf.Clamp(new Vector2(temp_xyz.x, temp_xyz.z).sqrMagnitude, 0, 1);
            // if (inputMag >= .27)
            // {
            //     _position = Vector3.Lerp(_position, _vxyz, lerpVal);
            //     gameObject.transform.position = _position;
            // }
            //
            // _position.y = gameObject.transform.position.y;
            
        }

        if (!rotationLock)
        {

            //Rotation
            _rotationxyz.y = _move.y;
            _rotationxyz.x = _move.x;

            _rotationxyz.x *= rotationSpeed * Time.deltaTime;
            _rotationxyz.y *= rotationSpeed * Time.deltaTime;

            _rotationxyz.Normalize();
            heading = Mathf.Atan2(_rotationxyz.x, _rotationxyz.y);

            var inputMag = Mathf.Clamp(new Vector2(_rotationxyz.x, _rotationxyz.y).sqrMagnitude, 0, 1);
            if (inputMag >= .27)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.Euler(0f, heading * Mathf.Rad2Deg, 0f), .4f);
            }
        }
        
        
        // doubles the gravitational force on just the reaper
        rb.AddForce(Physics.gravity * 2f, ForceMode.Acceleration);

        if (!holdingStaff && Vector3.Distance(throwingStaff.transform.position, throwDest) > .1)
        {
            Vector3 currDest = throwCurve[arcIndex];
            throwingStaff.transform.LookAt(currDest);
            float t = Time.deltaTime * throwSpeed;

            if (Vector3.Distance(throwingStaff.transform.position, currDest) <= .1 &&
                arcIndex < throwCurve.Count - 1)
            {
                arcIndex++;
                if (!holdingStaff && !returningToPlayer && !_activeIngroundParticles)
                {
                    //instantiate the staff particles
                    _activeIngroundParticles = Instantiate(ingroundParticlesPrefab, throwingStaff.transform).gameObject;
                    Destroy(_activeThrowTrail, 1f);
                }
            }
            else if (arcIndex >= throwCurve.Count - 1)
            {
                inAir = false;
            }

            throwingStaff.transform.position = Vector3.Lerp(throwingStaff.transform.position, currDest, t);

            //throwingStaff.transform.position = new Vector3(tempVect.x, newStaffHeight, tempVect.z);
        }
        else if (!holdingStaff && !returningToPlayer && !_activeIngroundParticles)
        {
            //instantiate the staff particles
            _activeIngroundParticles = Instantiate(ingroundParticlesPrefab, throwingStaff.transform).gameObject;
            Destroy(_activeThrowTrail);
        }

            FlipSpiritFriends(transform.forward.x <= 0);
    }
    // When entering text stone trigger, add to list of textStones nearby (Might need more optimal implementation later)
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("TextStone"))
        {
            // Check it's not already in list in case another trigger activates function
            DialogueTrigger dt_script = other.GetComponent<DialogueTrigger>();
            if (dt_script)
            {
                textStone = dt_script;
                // Display interact message
                if (playerGamepad != null)
                {
                    if (gamepadInteract)
                        gamepadInteract.SetActive(true);
                }
                else
                {
                    if (keyboardInteract)
                        keyboardInteract.SetActive(true);
                }
            }
        }
    }
    // When leaving text stone trigger, remove from list of textStones nearby
    private void OnTriggerExit(Collider other)
    {
        textStone = null;
        // Turn off interact message
        if (gamepadInteract)
            gamepadInteract.SetActive(false);
        if (keyboardInteract)
            keyboardInteract.SetActive(false);
    }

    private void FlipSpiritFriends(bool faceRight)
    {
        /*switch (spiritsFriends.Count)
        {
            case 1:
                spiritsFriends[0].FlipSprite(faceRight);
                break;
            case 2:
                spiritsFriends[0].FlipSprite(faceRight);
                spiritsFriends[1].FlipSprite(faceRight);
                break;
            case 3:
                spiritsFriends[0].FlipSprite(faceRight);
                spiritsFriends[1].FlipSprite(faceRight);
                spiritsFriends[2].FlipSprite(faceRight);
                break;
        }*/

        foreach (var friend in spiritsFriends)
        {
            friend.FlipSprite(faceRight);
        }
        
    }

    /// <summary>
    /// This function will add a new sprite friend to the Reaper's spirit friend list if there is still room
    /// available otherwise nothing is done.
    /// </summary>
    /// <param name="newFriend">Currently a GameObject just for the sake of prototyping, but will be swapped for
    /// a spirit friend object once they are implemented</param>
    public void AddSpiritFriend(SpiritFriend newFriend)
    {
        if (spiritsFriends.Count < 4)
        {
        GameObject newlyAddedFriend = Instantiate(newFriend.friendPrefab, this.gameObject.transform, true);
        SpiritFriend newFriendScript = newlyAddedFriend.GetComponent<SpiritFriend>();
        newFriendScript.ToggleCollider();
        //newlyAddedFriend.transform.position = spiritSpots[spiritsFriends.Count].transform.position;
        newFriendScript.followTarget = spiritsFriends.Count == 0 ? gameObject : spiritsFriends[spiritsFriends.Count-1].gameObject;

        spiritsFriends.Add(newFriendScript);
        //Unparent the spirit
        newlyAddedFriend.transform.parent = null;
        newFriendScript.withPlayer = true;
        //newFriendScript.parentTarget = gameObject;

        currHeartNum ++;
            
        }
    }

    /// <summary>
    /// Checks to see if there is room to add another spirit friend to the Reaper's list. If there is then return true
    /// otherwise false
    /// </summary>
    /// <returns>Bool representing if there is room to add another spirit</returns>
    public bool RoomForMoreFriends()
    {
        return spiritsFriends.Count < 4;
    }

    public void ActivateAttack()
    {
        attackingStaff.ActivateAxe();
        ParticleSystem newParticle = Instantiate(slashParticles, particlePos.transform, false);
        activeSlashes.Enqueue(newParticle);
        newParticle.gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        
        StartCoroutine(WaitAndDestroySlash(particleDestructionTime));
    }

    public void DeactivateAttack()
    {
        attackingStaff.DeactivateAxe();
    }

    public void TeleportationDamage()
    {
        float halfWidth = Vector3.Distance(transform.position, throwingStaff.transform.position) / 2;
        var playerX = transform.position.x;
        var playerY = transform.position.y;
        var playerZ = transform.position.z;

        var staffX = throwingStaff.transform.position.x;
        var staffY = throwingStaff.transform.position.y;
        var staffZ = throwingStaff.transform.position.z;
        float boxLength = Mathf.Abs(staffZ - playerZ);
        Vector3 forward = transform.forward;
        transform.LookAt(throwingStaff.transform);

        Collider[] hitEnemies = Physics.OverlapBox(
            new Vector3((playerX + staffX) / 2, (playerY + staffY) / 2, (playerZ + staffZ) / 2),
            new Vector3(1, 1, halfWidth), transform.rotation, enemyLayers, QueryTriggerInteraction.Ignore);
        Debug.Log(hitEnemies.Length);

        foreach (Collider enemyhit in hitEnemies)
        {
            DealDamage(enemyhit.GetComponent<Entity>());
        }


    }


    /// <summary>
    /// Overrides the virtual function from the parent Entity class. Changes the received damage amount to an
    /// int by rounding down and will subtract it from the available hearts. If the damage dealt is bigger than the
    /// available hearts then the numHearts will just be set to 0.
    /// </summary>
    /// <param name="damage">Float representing how much damage the Reaper should receive.</param>
    public override void TakeDamage(float damage)
    {
        if (!invincible && currHeartNum > 0)
        {
            int damageAmount = Mathf.FloorToInt(damage);

            /*if (currHeartNum >= damageAmount)
            {
                currHeartNum -= damageAmount;
            }
            else
            {
                currHeartNum = 0;
            }*/
            audioMan.FindAndPlay("PlayerHurt");

            if (spiritsFriends.Count > 0)
            {
                PopSpiritFriendHealth(damageAmount);
            }
            else
            {
                currHeartNum--;
            }
            
            if (currHeartNum > 0)
            {
                playerAnimator.SetTrigger("Hurt");
                if (throwDown)
                {
                    throwDown = false;
                    throwDownIgnore = true;
                    Destroy(throwTarget);
                    playerAnimator.SetBool("Charging", false);
                    playerAnimator.SetTrigger("Abort");
                }
                if (lightHoldStarted)
                {
                    lightHoldStarted = false;
                    lightHoldIgnore = true;
                    currentlyAnimatingAttack = false;
                    ignoreCurrentlyAnimating = false;
                    playerAnimator.SetTrigger("Abort");
                    playerAnimator.SetBool("Charging", false);

                    //Reset movement speed back normal
                    moveSpeed = _mvSpd;
                }

                if (heavyHoldStarted)
                {
                    heavyHoldIgnore = true;
                    heavyHoldStarted = false;
                    magicRingAttack.EndAnimation();
                    moveSpeed = _mvSpd;
                    magicRingAttack.Cancel();
                }
                
                playerAnimator.ResetTrigger("ChargeDown");
                playerAnimator.ResetTrigger("Abort");
                playerAnimator.ResetTrigger("Revive");
            }
            else
            {
                playerAnimator.SetTrigger("Death");
                currentlyAnimatingAttack = true;
            }
            StartCoroutine(PlayerHitFlash());

        }
    }

    private void PopSpiritFriendHealth(int popNum)
    {
        for (int i = 0; i < popNum; i++)
        {
            if (spiritsFriends.Count > 0)
            {
                if (spiritsFriends.Count > 1)
                {
                    spiritsFriends[1].followTarget = spiritsFriends[0].followTarget;
                }

                Destroy(spiritsFriends[0].gameObject);
                spiritsFriends.RemoveAt(0);
                currHeartNum--;
            }
        }
    }

    public void Respawn()
    {
        audioMan.FindAndPlay("PlayerDeath");
        playerAnimator.SetTrigger("Revive");
        currHeartNum = numHearts + spiritsFriends.Count;
        returningToPlayer = true;
        currentlyAnimatingAttack = false;
        recallTimeWindow = Time.time + recallTimeLength;
        if (currentArena)
        {
            currentArena.DeactivateBarriers();
        }
        if (throwDown)
        {
            throwDown = false;
            throwDownIgnore = true;
            Destroy(throwTarget);
            playerAnimator.SetBool("Charging", false);
            playerAnimator.SetTrigger("Abort");
        }
        if (lightHoldStarted)
        {
            lightHoldStarted = false;
            lightHoldIgnore = true;
            currentlyAnimatingAttack = false;
            ignoreCurrentlyAnimating = false;
            playerAnimator.SetTrigger("Abort");
            playerAnimator.SetBool("Charging", false);

            //Reset movement speed back normal
            moveSpeed = _mvSpd;
        }

        if (heavyHoldStarted)
        {
            heavyHoldIgnore = true;
            heavyHoldStarted = false;
            magicRingAttack.EndAnimation();
            moveSpeed = _mvSpd;
            magicRingAttack.Cancel();
        }
        magicRingAttack.Cancel();
        
        playerAnimator.ResetTrigger("ChargeDown");
        playerAnimator.ResetTrigger("Abort");
        //playerAnimator.ResetTrigger("Revive");

    }

    public void ApplyKnockBack(Rigidbody taregtRB)
    {
        Vector3 forward = transform.forward;
        int x_dir = forward.x < 0 ? -1 : 1;
        int z_dir = forward.z < 0 ? -1 : 1;
        float xForce = 0;
        float zForce = 0;
            
        if ((Mathf.Abs(forward.x) > .9f && Mathf.Abs(forward.x) <= 1f) ||
            (Mathf.Abs(forward.z) > .9f && Mathf.Abs(forward.z) <= 1f))
        {
            //int temp = Mathf.FloorToInt(forward.x);
            if ((Mathf.Abs(forward.x) > .9f && Mathf.Abs(forward.x) <= 1f))
            {
                xForce = knockbackStrength * x_dir;
            }
            else
            {
                zForce = knockbackStrength * z_dir;
            }
        }
        else
        {
            xForce = knockbackStrength * x_dir;
            zForce = knockbackStrength * z_dir;
        }
            
        taregtRB.AddForce(xForce, 0, zForce, ForceMode.Impulse);
    }

    /// <summary>
    /// Will deal the appropriate damage amount to an the given enemy.
    /// </summary>
    /// <param name="damagedEntity">The enemy to be damaged</param>
    public override void DealDamage(Entity damagedEntity)
    {
        damagedEntity.TakeDamage(GetDamageAmount());
        //Debug.Log("Damaging " + damagedEntity.name);
    }
    
    private void SetUpLinearRumble(bool asc = true)
    {
        rumbleTime = Time.time + rumbleDuration;
        tapRumble = false;
        if (asc)
        {
            rumbleAStep = (rumbleAVals.y - rumbleAVals.x) / rumbleDuration;
            rumbleBStep = (rumbleBVals.y - rumbleBVals.x) / rumbleDuration;
            
            currentRumbleA = rumbleAVals.x;
            currentRumbleB = rumbleBVals.x;
        }
        else
        {
            rumbleAStep = (rumbleAVals.x - rumbleAVals.y) / rumbleDuration;
            rumbleBStep = (rumbleBVals.x - rumbleBVals.y) / rumbleDuration;
            
            currentRumbleA = rumbleAVals.y;
            currentRumbleB = rumbleBVals.y;
        }
    }

    private void SetUpTapRumble()
    {
        rumbleTime = Time.time + tapRumbleDuration;
        tapRumble = true;
        currentRumbleA = tapRumbleAVals.x;
        currentRumbleB = tapRumbleBVals.x;
    }

    /// <summary>
    /// Will check the current combo state and return the appropriate damage amount.
    /// </summary>
    /// <returns>Amount to damage the enemy.</returns>
    private float GetDamageAmount()
    {
        float damageAmount = 0f;
        if (!holdingStaff)
        {
            damageAmount = 10f;
        }
        else
        {
            damageAmount = _currentlyExecutingScythComboState switch
            {
                scythComboState.Light1 => lightSlash1,
                scythComboState.Light2 => lightSlash2,
                scythComboState.Light3 => lightSlash3,
                scythComboState.HeavyTap => HeavyAttack1,
                scythComboState.HeavyLaunch => HeavyAttack2,
                _ => damageAmount
            };
        }

        return damageAmount;
    }

    private void BuildComboTree()
    {
        //Old combat tree (single tear falls)
        //Light attacks are right children
        //Heavy and everything else should be left
        //ComboNode root = _comboTree.GetRoot();
        //ComboNode current = root;
        
        // Depth 1
        // root.SetRightChild(new ComboNode(scythComboState.Light1));
        // root.SetLeftChild(new ComboNode(scythComboState.HeavyTap));
        //Depth2
        // current = root.GetLeftChild();
        // current.SetRightChild(new ComboNode(scythComboState.Light3));
        // current = root.GetRightChild();
        // current.SetRightChild(new ComboNode(scythComboState.Light2));
        // current.SetLeftChild(new ComboNode(scythComboState.HeavyTap));
        //Depth3
        // current = current.GetRightChild();
        // current.SetRightChild(new ComboNode(scythComboState.Light3));
        // current.SetLeftChild(new ComboNode(scythComboState.HeavyHold));
        
        //New combat tree
        //Construct the tree
        ComboNode root = _comboTree.GetRoot();
        ComboNode current = root;
        
        //Depth 1
        current.SetRightChild(new ComboNode(scythComboState.Light1));
        current.SetMiddleChild(new ComboNode(scythComboState.LightHold));
        current.SetLeftChild(new ComboNode(scythComboState.HeavyTap));
        
        //Depth 2
        current = current.GetRightChild();
        current.SetRightChild(new ComboNode(scythComboState.Light2));
        current.SetLeftChild(new ComboNode(scythComboState.LightHold));

    }

    private void GetThrowDest()
    {
        float throwDist = 0f;
        RaycastHit hitObject;
        if (Physics.Raycast(transform.position, transform.forward, out hitObject, throwDistance,
            blockingLayers))
        {
            throwDist = hitObject.distance - .2f;
        }
        else
        {
            throwDist = throwDistance;
        }
        
        Vector3 tempPos = transform.position;
        Vector3 tempHalfPos = tempPos;
        tempHalfPos.y += maxArcHeight;
        Vector3 forward = transform.forward;
        int x_dir = forward.x < 0 ? -1 : 1;
        int z_dir = forward.z < 0 ? -1 : 1;

        if ((Mathf.Abs(forward.x) > .9f && Mathf.Abs(forward.x) <= 1f) ||
            (Mathf.Abs(forward.z) > .9f && Mathf.Abs(forward.z) <= 1f))
        {
            //int temp = Mathf.FloorToInt(forward.x);
            if ((Mathf.Abs(forward.x) > .9f && Mathf.Abs(forward.x) <= 1f))
            {
                //tempPos.x += (8 * x_dir);
                tempPos.x += (throwDist * forward.x);
                tempHalfPos.x = (transform.position.x + tempPos.x) / 2;
            }
            else
            {
                //tempPos.z += (8 * z_dir);
                tempPos.z += (throwDist * forward.z);
                tempHalfPos.z = (transform.position.z + tempPos.z) / 2;
            }
        }
        else
        {
            tempPos.x += (throwDist * forward.x);
            tempHalfPos.x = (transform.position.x + tempPos.x) / 2;
            
            tempPos.z += (throwDist * forward.z);
            tempHalfPos.z = (transform.position.z + tempPos.z) / 2;
        }

        Vector3 tempTempPos = tempPos;

        RaycastHit hitObjectDown;
        //Down raycast to find where it hits the ground
        if (Physics.Raycast(tempTempPos, -transform.up, out hitObjectDown, 50, groundLayers))
        {
            tempPos = hitObjectDown.point;
            tempHalfPos.y -= hitObjectDown.distance;
        }

        RaycastHit hitObjectUp;
        //Raycast up to see if it hits anything. Used for going up slopes
        if (Physics.Raycast(tempTempPos, transform.up, out hitObjectUp, 10, groundLayers))
        {
            tempPos = hitObjectUp.point;
            tempHalfPos.y += hitObjectUp.distance;
        }

        throwTarget.transform.position = tempPos;
    }

    private IEnumerator ThrowStaff(Vector3 destination)
    {
        throwingStaff.transform.parent = null;
        while (Vector3.Distance(throwingStaff.transform.position, destination) > .1)
        {
            throwingStaff.transform.position = Vector3.Lerp(throwingStaff.transform.position, destination, .001f);
        }

        yield return null;
    }

    private IEnumerator PlayerHitFlash()
    {
        int currentCount = 0;
        invincible = true;
        while (currentCount < 4)
        {
            foreach (Material mat in meshRend.materials)
            {
                mat.color = Color.white;
            }
            yield return new WaitForSeconds(.2f);
            for (int i = 0; i < meshRend.materials.Length; i++)
            {
                meshRend.materials[i].color = baseColor[i];
            }
            yield return new WaitForSeconds(.2f);
            currentCount++;
        }

        invincible = false;
    }

    private IEnumerator WaitAndDestroySlash(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        if (activeSlashes.Count > 0)
        {
            Destroy(activeSlashes.Dequeue().gameObject);
        }
    }

    private IEnumerator TimeStopper(float stopLength)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(stopLength);
        Time.timeScale = 1;
    }

    private IEnumerator MoveParticleEffect(GameObject particles)
    {
        Debug.Log("moving");
        var dist = Vector3.Distance(particles.transform.position, throwDest);
        while (Vector3.Distance(particles.transform.position, throwDest) > .1f)
        {
            particles.transform.position = Vector3.Lerp(particles.transform.position, throwDest, .5f);
            yield return new WaitForSeconds(.1f);
            Debug.Log("moving");
        }
        Destroy(particles, 2f);

    }

    private void OnDrawGizmos()
    {
        Vector3 tempPos = transform.position;
        Vector3 tempHalfPos = tempPos;
        tempHalfPos.y += 8f;
        Vector3 forward = transform.forward;
        int x_dir = forward.x < 0 ? -1 : 1;
        int z_dir = forward.z < 0 ? -1 : 1;

        if ((Mathf.Abs(forward.x) > .9f && Mathf.Abs(forward.x) <= 1f) ||
            (Mathf.Abs(forward.z) > .9f && Mathf.Abs(forward.z) <= 1f))
        {
            //int temp = Mathf.FloorToInt(forward.x);
            if ((Mathf.Abs(forward.x) > .9f && Mathf.Abs(forward.x) <= 1f))
            {
                //tempPos.x += (8 * x_dir);
                tempPos.x += (8 * forward.x);
                tempHalfPos.x = (transform.position.x + tempPos.x) / 2;
            }
            else
            {
                //tempPos.z += (8 * z_dir);
                tempPos.z += (8 * forward.z);
                tempHalfPos.z = (transform.position.z +tempPos.z) / 2;
            }
        }
        else
        {
            tempPos.x += (8 * forward.x);
            tempHalfPos.x = (transform.position.x + tempPos.x) / 2;
            
            tempPos.z += (8 * forward.z);
            tempHalfPos.z = (transform.position.z +tempPos.z) / 2;
        }
        Debug.DrawLine(transform.position, tempPos, Color.green);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(throwingStaff.transform.position.x, throwingStaff.transform.position.y - .25f,throwingStaff.transform.position.z), .25f);
        Debug.DrawLine(tempPos, new Vector3(tempPos.x, tempPos.y - 50f,tempPos.z), Color.yellow);
        Debug.DrawLine(tempPos, new Vector3(tempPos.x, tempPos.y + 10f,tempPos.z), Color.yellow);

        for (float i = 0; i <= 1; i += .09f)
        {
            Vector3 spherePos = QuadraticCurve(transform.position, tempHalfPos, tempPos, i);
            
            Gizmos.DrawSphere(spherePos, .25f);
        }

        if (!holdingStaff)
        {
            Gizmos.color = Color.red;
            var playerX = transform.position.x;
            var playerY = transform.position.y;
            var playerZ = transform.position.z;

            var staffX = throwingStaff.transform.position.x;
            var staffY = throwingStaff.transform.position.y;
            var staffZ = throwingStaff.transform.position.z;
            
            Gizmos.DrawWireCube(new Vector3((playerX + staffX) / 2, (playerY + staffY) / 2, (playerZ + staffZ) / 2), new Vector3(Mathf.Abs(playerX - staffX), 1, Mathf.Abs(playerZ - staffZ)));
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(throwingStaff.transform.position.x, throwingStaff.transform.position.y - .25f,throwingStaff.transform.position.z), .25f);
        Debug.DrawLine(throwingStaff.transform.position, new Vector3(throwingStaff.transform.position.x, throwingStaff.transform.position.y + 6f,throwingStaff.transform.position.z), Color.yellow);
    }
}

