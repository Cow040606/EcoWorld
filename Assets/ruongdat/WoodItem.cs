using Fusion;
using UnityEngine;

public class WoodItem : NetworkBehaviour
{
    // Bạn có thể thêm các Networked variable vào đây nếu gỗ có số lượng, độ bền,...
    // Ví dụ:
    [Networked] public int woodAmount { get; set; } = 1;

    public override void Spawned()
    {
        // Logic khi cục gỗ vừa được spawn ra
    }
}