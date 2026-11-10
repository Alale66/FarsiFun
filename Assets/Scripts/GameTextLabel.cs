using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class GameTextLabel : MonoBehaviour
{
    [SerializeField] private string key;

    private TMP_Text label;
    private string fallback;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        fallback = label.text;
    }

    private void OnEnable()
    {
        label.text = GameTexts.Get(key, fallback);
    }
}