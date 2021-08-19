using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;
    public Rigidbody2D rb2D;
    public Animator animator;
    private SpriteRenderer sr;
    public AudioSource audioSource;

    public bool isLeft = true;
    private bool _isLeft
    {
        get
        {
            if (Horizontal == 0 || isFall)
                return isLeft;

            sr.flipX = isLeft = Horizontal < 0 ? true : false;
            return isLeft;
        }
    }

    private int lookDir { get { return _isLeft ? -1 : 1; } }

    public float Gravity = -15.0f;
    public Vector2 ReboundForce;
    private float Horizontal;
    public float MoveSpeed = 6;

    [Header("Jump")]
    public bool isJump;
    private Parabola2D jumpMove = new Parabola2D();
    public float JumpDistance = 5f;
    public float JumpMoveDistanceMax = 8f;
    public float JumpTimeout = 0.50f;
    public float JumpPower = 5f;
    public float JumpPowerMax = 10f;

    private Vector2 _jumpTarget;
    private float _jumpTimeoutDelta;
    private Vector2 _jumpVelocity; //跳躍的方向速度
    [SerializeField]
    private float _jumpPowerVelocity;
    [SerializeField]
    private float _laterJumpPowerVelocity;

    // private float _verticalVelocity;
    // private float _terminalVelocity = 53.0f;
    [Header("Fall And Wall")]
    public bool isFall;
    public float FallTimeout = 0.15f;
    private float _fallTimeoutDelta;

    public bool isWall;
    private RaycastHit2D hitWallPosition;
    public Vector3 WallPhysicsOffset;
    public float WallDirection = 0.33f;
    public LayerMask WallLayers;

    public bool isFalltoHitWall = false;
    public float FalltoHiitWallTimeOut = 0.5f;
    private float _falltoHiitWallTime;

    [Header("Player Grounded")]
    public bool isGrounded = true;
    public float GroundDirection = 0.5f;
    public LayerMask GroundLayers;

    [Header("Physics")]
    public float BoxSizePercentage = 0.95f;
    public Vector2 CubeSize = Vector2.one;

    [Header("DeBug")]
    public bool DrawJumpPath = false;

    //animation ID
    private int _animIDSpeed;
    private int _animIDJump;
    private int _animIDHitWall;

    private void Awake()
    {
        Instance = this;
        Physics2D.gravity = new Vector2(0, Gravity);
        sr = animator.GetComponent<SpriteRenderer>();
    }
    void Start()
    {
        AssignAnimationIDs();

        _jumpPowerVelocity = JumpPower;
        _jumpTimeoutDelta = JumpTimeout;
        _falltoHiitWallTime = FalltoHiitWallTimeOut;
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDHitWall = Animator.StringToHash("HitWall");
    }

    private void FixedUpdate()
    {
        if (GameManger.Instance.EndGame)
            return;

        WallCheck();
    }

    void Update()
    {
        if (GameManger.Instance.EndGame)
            return;

        GroundedCheck();
        JumpAndGravity();

        if (isJump)
            rb2D.velocity = _jumpVelocity;
        else
        {
            if (isFall && isWall && !isFalltoHitWall)
            {
                isFalltoHitWall = true;
                EffectSet(hitWallPosition.point, hitWallPosition.normal);
                rb2D.velocity = new Vector2(rb2D.velocity.x, 0);
                rb2D.AddForce(new Vector2(-lookDir, -1f) * ReboundForce * _laterJumpPowerVelocity * 0.75f, ForceMode2D.Impulse);

                animator.SetBool(_animIDHitWall, isFalltoHitWall);
                playAudio("hitTheWall");
                _falltoHiitWallTime = FalltoHiitWallTimeOut;
            }
            else if (!isFall && _falltoHiitWallTime <= 0)
                PlayerMove();
        }

        if (DrawJumpPath)
            jumpMove.DrawPath(rb2D, _jumpTarget, _jumpPowerVelocity * 0.65f);

    }

    private void PlayerMove()
    {
        Horizontal = Input.GetAxis("Horizontal");

        float MoveDir = isWall ? 0 : Horizontal;
        Vector2 pos = new Vector2(MoveDir * MoveSpeed, 0);

        transform.Translate(pos * Time.deltaTime);

        animator.SetFloat(_animIDSpeed, Mathf.Abs(MoveDir));
    }

    public void JumpAndGravity()
    {
        if (isGrounded)
        {
            _fallTimeoutDelta = 0;

            if (_jumpTimeoutDelta <= 0 && _falltoHiitWallTime <= 0)
                JumpAccumulating();

            if (_jumpTimeoutDelta >= 0)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            _jumpTimeoutDelta = JumpTimeout;

            _fallTimeoutDelta += Time.deltaTime;
            if (_fallTimeoutDelta > FallTimeout) animator.SetBool(_animIDJump, true);

            isJump = false;
        }
    }

    public void JumpAccumulating()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            _jumpPowerVelocity += 10 * Time.deltaTime;
            _jumpPowerVelocity = Mathf.Clamp(_jumpPowerVelocity, JumpPower, JumpPowerMax);
        }

        float _JumpMoveMultiple = _jumpPowerVelocity / JumpPowerMax;
        _jumpTarget = new Vector2(transform.position.x + lookDir * (JumpMoveDistanceMax * _JumpMoveMultiple), transform.position.y);

        if (Input.GetKeyUp(KeyCode.Space) && !isJump)
        {
            isJump = true;
            isFall = true;
            isGrounded = true;

            jumpMove = new Parabola2D();
            _jumpVelocity = jumpMove.VelocityData(rb2D, _jumpTarget, _jumpPowerVelocity * 0.8f);
            //_verticalVelocity = Mathf.Sqrt(_jumpPowerVelocity * -2f * Gravity);
            _laterJumpPowerVelocity = _jumpPowerVelocity;
            _jumpPowerVelocity = JumpPower;

            animator.SetBool(_animIDJump, true);
            playAudio("jump");
        }
    }

    #region Check
    private void GroundedCheck()
    {
        RaycastHit2D[] hits = Physics2D.BoxCastAll(transform.position, CubeSize * BoxSizePercentage, 0, Vector2.down, GroundDirection, GroundLayers);

        isGrounded = false;
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.distance != 0)
            {
                isGrounded = true;
            }
        }

        if (isGrounded && !isJump)
        {
            isFall = false;
            if (isFalltoHitWall)
            {
                isFalltoHitWall = false;
                animator.SetBool(_animIDHitWall, isFalltoHitWall);
            }
            else
                animator.SetBool(_animIDJump, false);

            if (_falltoHiitWallTime > 0f)
                _falltoHiitWallTime -= Time.deltaTime;
        }
    }

    private void WallCheck()
    {
        isWall = hitWallPosition = Physics2D.BoxCast(transform.position + WallPhysicsOffset, CubeSize * BoxSizePercentage, 0, Vector2.left * -lookDir, WallDirection, WallLayers);
    }
    #endregion

    private void EffectSet(Vector2 position, Vector2 rotation)
    {
        Sprite sprite = GameManger.Instance.SpritePool.GetObject("Mocus_" + Random.Range(0, 3)); //0~2

        GameObject effect = new GameObject();

        SpriteRenderer sr = effect.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Effect";
        sr.color = new Color(1, 1, 1, .85f);

        effect.transform.position = position;

        int rot_z = rotation.y == 0 ? lookDir * 90 : 180;
        effect.transform.rotation = Quaternion.Euler(new Vector3(0, 0, rot_z));

        Destroy(effect, 3f);
    }

    private void playAudio(string name)
    {
        AudioClip clip = GameManger.Instance.AudioClipPool.GetObject(name);
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position + Vector3.down * GroundDirection, CubeSize * BoxSizePercentage);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + WallPhysicsOffset + Vector3.left * -lookDir * WallDirection, CubeSize * BoxSizePercentage);
    }
}
