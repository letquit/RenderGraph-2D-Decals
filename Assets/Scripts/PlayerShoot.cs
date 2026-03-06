using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class PlayerShoot : MonoBehaviour
{
    [SerializeField] private Transform _gun;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Transform _muzzlePoint;
    [SerializeField] private LineRenderer _lineRendPrefab;
    
    [SerializeField] private float _timeBetweenShots = 0.1f;
    [SerializeField] private float _timeForLineToVanish = 0.05f;
    [SerializeField] private LayerMask _layerMask;
    
    private float _bulletTimer;
    private RaycastHit2D _hit;

    private void Update()
    {
        _bulletTimer += Time.deltaTime;

        RotateGunToMouse();
        
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            if (_bulletTimer >= _timeBetweenShots)
            {
                _bulletTimer = 0f;
                ShootBullet();
            }
        }
    }
    
    private void RotateGunToMouse()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldPosition.z = _gun.position.z;

        Vector3 direction = mouseWorldPosition - _gun.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        _gun.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    private void ShootBullet()
    {
        float randomX = Random.Range(-0.025f, 0.025f);
        float randomY = Random.Range(-0.025f, 0.025f);
        Vector2 direction = (_gun.transform.right + new Vector3(randomX, randomY, 0)).normalized;

        _hit = Physics2D.Raycast(_firePoint.position, direction, 100f, _layerMask);
        
        Vector2 hitPoint;
        
        if (_hit.collider != null)
        {
            hitPoint = _hit.point;
            
            Vector3 decalPos = hitPoint - (_hit.normal * Random.Range(0.45f, 0.55f));
            decalPos.z = -0.3f;

            if (DecalManager.Instance != null)
            {
                DecalManager.Instance.AddDecal(decalPos, Vector2.one * 0.65f, _layerMask);
            }
        }
        else
        {
            hitPoint = (Vector2)_firePoint.position + direction * 100f;
        }
        
        StartCoroutine(SpawnAndManageLineRend(hitPoint));
    }
    
    private IEnumerator SpawnAndManageLineRend(Vector2 hitPoint)
    {
        LineRenderer rend = Instantiate(
            _lineRendPrefab, 
            _muzzlePoint.position, 
            Quaternion.identity);

        rend.enabled = false;

        float randomY = Random.Range(-0.25f, 0.25f);
        float randomX = Random.Range(-0.25f, 0.25f);
        Vector2 shootPoint = _muzzlePoint.position + new Vector3(randomX, randomY);

        rend.SetPosition(0, shootPoint);
        rend.SetPosition(1, hitPoint);
        rend.enabled = true;

        yield return new WaitForSeconds(_timeForLineToVanish);

        Destroy(rend.gameObject);
    }
    
    private void Reset()
    {
        if (_firePoint == null)
            _firePoint = transform.Find("FirePoint");
        if (_muzzlePoint == null)
            _muzzlePoint = transform.Find("MuzzlePoint");
        if (_gun == null)
            _gun = transform;
    }
}