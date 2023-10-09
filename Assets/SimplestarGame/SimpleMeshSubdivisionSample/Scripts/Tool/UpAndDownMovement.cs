using UnityEngine;

public class UpAndDownMovement : MonoBehaviour
{
    public float speed = 1.0f; // �㉺�ړ��̑��x
    public float maxY = 1.0f;  // ������̍ő�ʒu
    public float minY = -1.0f; // �������̍ő�ʒu

    private bool movingUp = true; // ��Ɉړ������ǂ����̃t���O

    private void Update()
    {
        // �㉺�ړ��̌v�Z
        float newYPosition = transform.position.y + (speed * Time.deltaTime * (movingUp ? 1 : -1));

        // �ړ��������`�F�b�N���A�K�v�Ȃ������؂�ւ���
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

        // �V�����ʒu�ɃI�u�W�F�N�g���ړ�������
        transform.position = new Vector3(transform.position.x, newYPosition, transform.position.z);
    }
}
