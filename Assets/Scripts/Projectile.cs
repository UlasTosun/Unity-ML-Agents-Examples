using UnityEngine;



public class Projectile : MonoBehaviour {

    [Header("Settings")]
    [SerializeField] private float _speed = 60f;
    [SerializeField] private float _range = 60f;
    [SerializeField] private int _damage = 40;

    [Header("References")]
    [SerializeField] private Renderer _renderer;
    [SerializeField] private ParticleSystem _hitEffect;

    private Rigidbody _rigidbody;
    private Tank _tank;
    private LayerMask _tankLayer;



    private void Start() {
        _tankLayer = LayerMask.NameToLayer("Tank");

        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.linearVelocity = transform.forward * _speed;

        Invoke(nameof(SelfDestroy), _range / _speed);
    }



    public void Initialize(Tank tank) {
        _tank = tank;
        _tank.OnEpisodeStarted += OnEpisodeStarted;

        _renderer.material = _tank.ProjectileMaterial;
    }



    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.layer == _tankLayer && collision.gameObject.TryGetComponent<Tank>(out Tank hitTank)) {
            if (hitTank == _tank)
                return;
            else {
                _tank.HitOnTarget(hitTank.Health <= _damage);
                hitTank.TakeDamage(_damage);
                SelfDestroy();
            }
        } else {
            _tank.MissTarget();
            SelfDestroy();
        }

        PlayParticle(collision);
    }



    private void PlayParticle(Collision collision) {
        ParticleSystem particle = Instantiate(_hitEffect, collision.contacts[0].point, Quaternion.identity);
        particle.transform.forward = collision.contacts[0].normal;
        ParticleSystem.MainModule hitEffectMain = particle.main;
        hitEffectMain.startColor = _tank.ProjectileMaterial.color;
    }



    private void OnEpisodeStarted() {
        SelfDestroy();
    }



    private void SelfDestroy() {
        _tank.OnEpisodeStarted -= OnEpisodeStarted;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }



}
