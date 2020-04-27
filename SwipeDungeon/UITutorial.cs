using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UITutorial : UIPopup
{
    [SerializeField]
    GameObject Bg;

    [SerializeField]
    Text tutorialText;

    [SerializeField]
    Animation anim;

    bool itemEventFlag;

    public void Show()
    {
        gameObject.SetActive(true);
        StartCoroutine(IE_Tutorial());
    }

    public IEnumerator IE_Tutorial()
    {
        SetText(Defines.ToturialText1);
        yield return IE_WaitSwipe();
        yield return IE_WaitSwipe();

        SetText(Defines.ToturialText2);
        yield return IE_WaitKillMonster();

        Bg.SetActive(true);
        SetText(Defines.ToturialText3);
        yield return IE_WaitTouch();

        Bg.SetActive(false);
        SetText("");
        yield return IE_WaitGetItem();

        SetText(Defines.ToturialText4); SetAnim(Defines.ToturialAnim1);
        yield return IE_WaitSwipe();

        StopAnim();
        SetText(Defines.ToturialText5);
        yield return IE_WaitTouch();

        OnClickClose();
    }

    public void GetItemEventTrigger()
    {
        itemEventFlag = true;
    }

    IEnumerator IE_WaitGetItem()
    {
        itemEventFlag = false;

        while (!itemEventFlag)
            yield return null;
    }

    IEnumerator IE_WaitSwipe()
    {
        int swipeCount = GameManager.Instance.SwipeCount;
        int checkValue = swipeCount;

        while (checkValue == swipeCount)
        {
            if (Input.GetMouseButtonUp(0))
            {
                swipeCount = GameManager.Instance.SwipeCount;
            }
            yield return null;
        }
    }

    IEnumerator IE_WaitKillMonster()
    {
        int monsterKillCount = GameManager.Instance.MonsterKillCount;
        int checkValue = monsterKillCount;

        while (checkValue == monsterKillCount)
        {
            monsterKillCount = GameManager.Instance.MonsterKillCount;
            yield return null;
        }
    }

    IEnumerator IE_WaitTouch()
    {
        yield return null;

        while (!Input.GetMouseButtonUp(0))
            yield return null;
    }

    void SetText(string str)
    {
        tutorialText.text = str;
    }

    void SetAnim(string name)
    {
        anim.gameObject.SetActive(true);

        var clip = anim.GetClip(name);
        if (clip)
        {
            anim.clip = clip;
            anim.Play(name);
        }
    }

    void StopAnim()
    {
        anim.Stop();
        anim.gameObject.SetActive(false);
    }
}
