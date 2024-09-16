using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotsHandler : MonoBehaviour
{
    [SerializeField] private Transform[] slotsTransforms;

    [SerializeField] private float slotMovingSpeed = 3f;

    public bool IsMoving { get; private set; } = false;

    private float yNormalPos;

    private void Start()
    {
        yNormalPos = slotsTransforms[0].localPosition.y;
    }

    public void MoveSlotsToZero()
    {
        StartCoroutine(MoveSlotsToZeroCoroutine());
    }

    public void ResetSlots()
    {
        foreach (Transform slot in slotsTransforms)
        {
            slot.localPosition = new Vector3(slot.localPosition.x, yNormalPos, slot.localPosition.z);
        }
    }
    IEnumerator MoveSlotsToZeroCoroutine()
    {
        foreach (Transform slot in slotsTransforms)
        {
            IsMoving = true;
            while (slot.localPosition.y >= 0)
            {
                slot.localPosition += Vector3.down * slotMovingSpeed * Time.deltaTime;
                if (slot.localPosition.y <= 0)
                {
                    slot.localPosition = new Vector3(slot.localPosition.x, 0, slot.localPosition.z);
                    break;
                }
                yield return null;
            }
            yield return null;
        }
        IsMoving = false;
    }
}
