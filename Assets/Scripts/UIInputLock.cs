using UnityEngine;

public class UIInputLock : MonoBehaviour
{
    [SerializeField] private GameObject inputBlocker;

    private int lockCount;

    public void Lock()
    {
        lockCount++;

        if (inputBlocker != null)
            inputBlocker.SetActive(true);
    }

    public void Unlock()
    {
        lockCount = Mathf.Max(0, lockCount - 1);

        if (lockCount == 0 && inputBlocker != null)
            inputBlocker.SetActive(false);
    }

    public void ForceUnlock()
    {
        lockCount = 0;

        if (inputBlocker != null)
            inputBlocker.SetActive(false);
    }
}