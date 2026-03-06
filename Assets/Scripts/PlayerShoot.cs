using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

/// <summary>
/// 玩家射击系统组件，处理枪械旋转、射击逻辑和弹道效果
/// </summary>
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

    /// <summary>
    /// 每帧更新函数，处理射击计时、鼠标瞄准和射击输入检测
    /// </summary>
    private void Update()
    {
        _bulletTimer += Time.deltaTime;

        RotateGunToMouse();
        
        // 检测鼠标左键按下并执行射击
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            if (_bulletTimer >= _timeBetweenShots)
            {
                _bulletTimer = 0f;
                ShootBullet();
            }
        }
    }
    
    /// <summary>
    /// 根据鼠标位置旋转枪械朝向
    /// </summary>
    private void RotateGunToMouse()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorldPosition.z = _gun.position.z;

        Vector3 direction = mouseWorldPosition - _gun.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        _gun.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    /// <summary>
    /// 执行射击逻辑，包括射线检测、撞击点计算和弹孔贴花生成
    /// </summary>
    private void ShootBullet()
    {
        // 添加随机偏移以模拟射击散布
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
    
    /// <summary>
    /// 创建并管理射击轨迹线渲染器的生命周期
    /// </summary>
    /// <param name="hitPoint">射线撞击点位置</param>
    /// <returns>协程迭代器</returns>
    private IEnumerator SpawnAndManageLineRend(Vector2 hitPoint)
    {
        LineRenderer rend = Instantiate(
            _lineRendPrefab, 
            _muzzlePoint.position, 
            Quaternion.identity);

        rend.enabled = false;

        // 添加随机偏移以模拟枪口抖动效果
        float randomY = Random.Range(-0.25f, 0.25f);
        float randomX = Random.Range(-0.25f, 0.25f);
        Vector2 shootPoint = _muzzlePoint.position + new Vector3(randomX, randomY);

        rend.SetPosition(0, shootPoint);
        rend.SetPosition(1, hitPoint);
        rend.enabled = true;

        yield return new WaitForSeconds(_timeForLineToVanish);

        Destroy(rend.gameObject);
    }
    
    /// <summary>
    /// 重置函数，在编辑器中设置默认引用
    /// </summary>
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
