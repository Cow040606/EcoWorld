using Fusion;
using UnityEngine;
using System;

public class AnimalHealth : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnHPChanged))]
    public float HP { get; set; }
    
    [Header("Settings")]
    public float maxHP = 100f;

    public event Action OnDeath;
    public event Action<NetworkObject> OnDamaged;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            HP = maxHP;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_TakeDamage(float damage, NetworkObject attacker)
    {
        if (HP <= 0) return;

        HP -= damage;
        OnDamaged?.Invoke(attacker);

        if (HP <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    void OnHPChanged()
    {
        // Bạn có thể update thanh máu UI ở đây
    }
}