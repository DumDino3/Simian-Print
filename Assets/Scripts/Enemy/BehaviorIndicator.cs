using UnityEngine;

public class BehaviorIndicator : MonoBehaviour
{
    [SerializeField] private GameObject iconCanvas;
    [SerializeField] private GameObject questionMark;
    [SerializeField] private GameObject exclamationMark;

    private void Start()
    {
        questionMark.SetActive(false);
        exclamationMark.SetActive(false);
    }

    public void ShowQuestion(bool on)
    {
        if (iconCanvas) iconCanvas.SetActive(on || (exclamationMark && exclamationMark.activeSelf));
        if (questionMark) questionMark.SetActive(on);
        //AutoHideCanvasIfNone();
    }
    public void ShowExclamation(bool on)
    {
        if (iconCanvas) iconCanvas.SetActive(on || (questionMark && questionMark.activeSelf));
        if (exclamationMark) exclamationMark.SetActive(on);
        //AutoHideCanvasIfNone();
    }
    public void HideAll()
    {
        questionMark.SetActive(false);
        exclamationMark.SetActive(false);
        iconCanvas.SetActive(false);
    }
    //void AutoHideCanvasIfNone()
    //{
    //    if (!iconCanvas) return;
    //    bool any = (questionMark && questionMark.activeSelf) || (exclamationMark && exclamationMark.activeSelf);
    //    iconCanvas.SetActive(any);
    //}
}