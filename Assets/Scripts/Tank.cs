using UnityEngine;
using UnityEngine.Events;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;



public class Tank : Agent {

    [Header("References")]
    [SerializeField] private TankUI _tankUI;
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private ParticleSystem _fireEffect;
    [field: SerializeField] public Material ProjectileMaterial { get; private set; }

    [Header("Settings")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _turnSpeed = 180f;
    [SerializeField] private float _reloadTime = 1f;

    public int Health { get; private set; }
    public event UnityAction OnEpisodeStarted;

    private Rigidbody _rigidbody;
    private InputSystemActions _inputActions;
    private LayerMask _groundCheckLayer;
    private float _timeSinceLastShot;



    protected override void Awake() {
        _groundCheckLayer = LayerMask.NameToLayer("Ground Check");
        _rigidbody = GetComponent<Rigidbody>();

        _inputActions = new ();
        _inputActions.Tank.Enable();

        ParticleSystem.MainModule fireEffectMain = _fireEffect.main;
        fireEffectMain.startColor = ProjectileMaterial.color;

        Health = _maxHealth;
    }



    public override void Initialize() {

    }



    void Update() {
        _timeSinceLastShot += Time.deltaTime;
        _tankUI.UpdateReloadBar(Mathf.Clamp01(_timeSinceLastShot / _reloadTime));
    }



    public override void OnEpisodeBegin() {

        _timeSinceLastShot = _reloadTime;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        Vector2 randomPosition = Random.insideUnitCircle * 10f;
        _rigidbody.position = new Vector3(randomPosition.x, 0f, randomPosition.y) + transform.parent.position - new Vector3(0f, 0.0f, 10f);
        _rigidbody.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Health = _maxHealth;
        _tankUI.UpdateHealthBar(1f);
        _tankUI.UpdateReloadBar(1f);

        OnEpisodeStarted?.Invoke();
    }



    public override void CollectObservations(VectorSensor sensor) {
        sensor.AddObservation(Mathf.Clamp01(_timeSinceLastShot / _reloadTime));
    }



    public override void OnActionReceived(ActionBuffers actionBuffers) {
        Move(actionBuffers.DiscreteActions[0]);
        Rotate(actionBuffers.DiscreteActions[1]);
        Fire(actionBuffers.DiscreteActions[2]);
    }



    private void Move(int action) {
        // 0: stop, 1: forward, -1: backward
        Vector3 moveDirection = _moveSpeed * Time.deltaTime * action * transform.forward;
        _rigidbody.MovePosition(_rigidbody.position + moveDirection);
    }



    private void Rotate(int action) {
        // 0: no turn, 1: right turn, -1: left turn
        float turnAmount = action * _turnSpeed * Time.deltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
        _rigidbody.MoveRotation(_rigidbody.rotation * turnRotation);
    }



    private void Fire(int action) {
        // 0: do not fire, 1: fire
        if (_projectilePrefab == null || _firePoint == null || action == 0 || _timeSinceLastShot < _reloadTime)
            return;
        
        Projectile projectile = Instantiate(_projectilePrefab, _firePoint.position, _firePoint.rotation);
        projectile.Initialize(this);

        _timeSinceLastShot = 0f;
        _fireEffect?.Play();
    }



    public void HitOnTarget(bool isEnemyDestroyed) {
        Debug.Log("Hit target" + (isEnemyDestroyed ? " and destroyed it." : "."));
        if (isEnemyDestroyed) {
            SetReward(1f);
            EndEpisode();
            Debug.Log("Enemy tank destroyed, ending episode.");
        } else {
            AddReward(0.1f);
        }
    }



    public void MissTarget() {
        Debug.Log("Missed target");
        AddReward(-0.05f);
    }



    public void TakeDamage(int damage) {
        Debug.Log($"Tank took {damage} damage");
        Health -= damage;
        _tankUI.UpdateHealthBar(Mathf.Clamp01((float)Health / _maxHealth));
        if (Health <= 0) {
            SetReward(-1f);
            EndEpisode();
            Debug.Log("Tank destroyed, ending episode.");
        }
    }



    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.layer == _groundCheckLayer) {
            SetReward(-1f);
            EndEpisode();
        }
    }



    public override void Heuristic(in ActionBuffers actionsOut) {
        var discreteActions = actionsOut.DiscreteActions;
        Vector2 moveInput = _inputActions.Tank.Move.ReadValue<Vector2>();

        // Move
        if (moveInput.y > 0)
            discreteActions[0] = 1; // Move forward
        else if (moveInput.y < 0)
            discreteActions[0] = -1; // Move backward
        else
            discreteActions[0] = 0; // Stop

        // Turn
        if (moveInput.x > 0)
            discreteActions[1] = 1; // Turn right
        else if (moveInput.x < 0)
            discreteActions[1] = -1; // Turn left
        else
            discreteActions[1] = 0; // No turn

        // Fire
        if (_inputActions.Tank.Fire.IsPressed()) {
            discreteActions[2] = 1; // Fire
        } else
            discreteActions[2] = 0; // Do not fire
    }



    void OnDestroy() {
        _inputActions.Tank.Disable();
        _inputActions.Dispose();
    }



}
