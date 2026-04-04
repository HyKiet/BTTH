using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script gắn trên chùm sấm sét đính kèm nòng súng.
/// Quái đi vào vùng sẽ bị giật điện (nhận Damage theo chu kỳ Tick Rate).
/// </summary>
public class LaserBlast : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damagePerTick = 20; 
    [Tooltip("Khoảng cách giữa 2 lần giật điện (ví dụ 0.2 giây một lần nảy số)")]
    public float tickRate = 0.2f;

    // Quản lý riêng cooldown của từng con quái
    private Dictionary<int, float> nextDamageTime = new Dictionary<int, float>();

    void OnEnable()
    {
        nextDamageTime.Clear();
        // Áp dụng âm thanh giật điện (nếu có Audio Manager)
    }

    void OnTriggerStay2D(Collider2D collider)
    {
        int colId = collider.gameObject.GetInstanceID();

        // Kiểm tra xem đã đến điểm để bị chích điện tiếp trên con quái này chưa
        if (nextDamageTime.ContainsKey(colId) && Time.time < nextDamageTime[colId])
        {
            return; 
        }

        if (collider.CompareTag("Enemy") || collider.name.Contains("Enemy"))
        {
            bool hitLanded = false;

            EnemyController ec = collider.GetComponent<EnemyController>();
            if (ec == null) ec = collider.GetComponentInParent<EnemyController>();

            if (ec != null)
            {
                ec.TakeElectricDamage(damagePerTick);
                hitLanded = true;
            }
            else
            {
                EnemyRangedController erc = collider.GetComponent<EnemyRangedController>();
                if (erc == null) erc = collider.GetComponentInParent<EnemyRangedController>();

                if (erc != null)
                {
                    erc.TakeElectricDamage(damagePerTick);
                    hitLanded = true;
                }
                else
                {
                    // Fallback
                    collider.SendMessage("TakeElectricDamage", damagePerTick, SendMessageOptions.DontRequireReceiver);
                    hitLanded = true;
                }
            }

            if (hitLanded)
            {
                nextDamageTime[colId] = Time.time + tickRate;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        int colId = collider.gameObject.GetInstanceID();
        if (nextDamageTime.ContainsKey(colId))
        {
            nextDamageTime.Remove(colId);
        }
    }
}
