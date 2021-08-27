using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelector : MonoBehaviour
{
    public List<GameObject> levels;
    public GameObject container;
    public GameObject arrows;
    public string selectedLevel;
    private int levelId;
    public int offset;
    public float scrollSpeed;
    public float scaleSpeed;
    public bool shuffeling;
    public float shuffleSpeed;
    public float shuffleDelta;
    public bool isHidden;

    void Awake()
    {
        HideSelector();
    }
    public void MoveLevelSelectObject()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            Vector2 pos = new Vector2(levelId * -offset, 0);
            Vector2 scale = new Vector2(1, 1);
            if (isHidden)
            {
                pos.y = -1000;
            }
            else
            {
                pos.y = 0;
            }
            container.transform.localPosition = Vector3.Lerp(container.transform.localPosition, pos, Time.fixedDeltaTime * scrollSpeed);

            
            if (Mathf.Abs(levelId - i) == 0)
            {
                scale = new Vector3(1f, 1f, 1f);
            } else if (Mathf.Abs(levelId - i) == 1)
            {
                scale = new Vector3(0.5f, 0.5f, 0.5f);
            } else
            {
                scale = new Vector3(0f, 0f, 0f);
            }
            levels[i].transform.localScale = Vector3.Lerp(levels[i].transform.localScale, scale, Time.fixedDeltaTime * scaleSpeed);
        }
        selectedLevel = "Level_" + (levelId + 1);
    }
    public void Shuffle()
    {
        shuffleSpeed = 0;
        selectedLevel = "Level_" + Random.Range(1, levels.Count - 1);
        StartCoroutine(ShuffleDelay());
    }
    public IEnumerator ShuffleDelay()
    {
        shuffeling = true;
        yield return new WaitForSeconds(10f);
        shuffeling = false;
    }
    void FixedUpdate()
    {
        if (shuffeling)
        {
            shuffleSpeed += Time.fixedDeltaTime / 10;
            shuffleDelta += Time.deltaTime;
            if (shuffleDelta >= shuffleSpeed)
            {
                shuffleDelta = 0;
                if (levelId >= levels.Count - 1)
                {
                    levelId = 0;
                    gameObject.SetActive(false);
                    return;
                }
                if (shuffleSpeed >= 0.5f)
                {
                    shuffeling = false;
                }
                ScrollRight();
            }
        }
        MoveLevelSelectObject();
    }
    public void ShowSelector()
    {
        arrows.SetActive(true);
        isHidden = false;
        container.SetActive(true);
    }
    public void HideSelector()
    {
        arrows.SetActive(false);
        isHidden = true;
        StartCoroutine(HideDelay());
    }
    public IEnumerator HideDelay()
    {
        yield return new WaitForSeconds(2);
        if (isHidden)
        {
            container.SetActive(false);
            gameObject.SetActive(false);
        }
    }
    public void ScrollRight()
    {
        if (levelId + 1 <= levels.Count - 1)
        {
            levelId++;
        }
    }
    public void ScrollLeft()
    {
        if (levelId - 1 >= 0)
        {
            levelId--;
        }
    }
}
