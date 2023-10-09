using UnityEngine;

public class UpAndDownMovement : MonoBehaviour
{
    public float speed = 1.0f; // 上下移動の速度
    public float maxY = 1.0f;  // 上方向の最大位置
    public float minY = -1.0f; // 下方向の最大位置

    private bool movingUp = true; // 上に移動中かどうかのフラグ

    private void Update()
    {
        // 上下移動の計算
        float newYPosition = transform.position.y + (speed * Time.deltaTime * (movingUp ? 1 : -1));

        // 移動制限をチェックし、必要なら方向を切り替える
        if (newYPosition >= maxY)
        {
            newYPosition = maxY;
            movingUp = false;
        }
        else if (newYPosition <= minY)
        {
            newYPosition = minY;
            movingUp = true;
        }

        // 新しい位置にオブジェクトを移動させる
        transform.position = new Vector3(transform.position.x, newYPosition, transform.position.z);
    }
}
