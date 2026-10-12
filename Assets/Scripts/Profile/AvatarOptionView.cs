using System;
using UnityEngine;
using UnityEngine.UI;

public class AvatarOptionView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image avatarImage;
    [SerializeField] private GameObject selectionGlow;
    [SerializeField, Min(0)] private int avatarIndex;

    public int AvatarIndex => avatarIndex;

    public Sprite AvatarSprite =>
        avatarImage != null
            ? avatarImage.sprite
            : null;

    public event Action<AvatarOptionView> Selected;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(HandleClicked);
    }

    public void SetSelected(bool selected)
    {
        if (selectionGlow != null)
            selectionGlow.SetActive(selected);
    }

    private void HandleClicked()
    {
        Selected?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }
}