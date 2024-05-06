using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using DG.Tweening;

public class ScrollController : MonoBehaviour
{
    [SerializeField] Text _nameCountry1st, _nameCountry2nd, _nameCountry3th;
    [SerializeField] List<Sprite> _flags = new List<Sprite>();
    [Header("===============Rank Anim==================")]
    [SerializeField] ScrollRect _scrollRect;
    [SerializeField]
    [Range(0, 1)]
    float scrollValue = 0;

    List<RankItem> _listElements = new List<RankItem>();

    public RankItem playerItem => _listElements.Last();

    TextAsset dataNames, dataCountries;

    private void Awake()
    {
        _listElements = _scrollRect.content.GetComponentsInChildren<RankItem>(true).ToList();
        Debug.Log(_listElements.Count);
        dataNames = Resources.Load<TextAsset>("names");
        dataCountries = Resources.Load<TextAsset>("countries");
    }

    private void OnEnable()
    {
        StartCoroutine(waitCanvasUpdate(null));
    }

    public IEnumerator waitCanvasUpdate(Action callback)
    {
        yield return new WaitForSeconds(3);
        yield return new WaitForEndOfFrame();
        InitAnim(callback);
    }
    // Update is called once per frame
    void Update()
    {
        _scrollRect.verticalNormalizedPosition = scrollValue;
    }

    public void InitAnim(Action callback = null)
    {

        _nameCountry1st.text = this.randomCountry();
        _nameCountry2nd.text = this.randomCountry();
        _nameCountry3th.text = this.randomCountry();

        //int maxChangeRank = Random.Range(5, 10);
         int maxChangeRank =1;// Random.Range(2, 4);
         int rankNumber = 1;//PlayerPrefs.GetInt("Rank", Random.Range(5, 8)) - maxChangeRank;

       // int rankNumber = PlayerPrefs.GetInt("Rank", Random.Range(800, 900)) - maxChangeRank;



        for (int i = 1; i < _listElements.Count; i++)
        {
            if (i <= maxChangeRank || i == _listElements.Count - 1)
            {
              //  _listElements[i].Init((rankNumber + i).ToString(), randomName(), _flags[Random.Range(0, _flags.Count)]);
                _listElements[i].Init(1.ToString(), randomName(), _flags[0]);
                _listElements[i].gameObject.SetActive(true);
            }
            else
                _listElements[i].gameObject.SetActive(false);
        }


        this.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
        this.scrollValue = 0;
        _scrollRect.verticalNormalizedPosition = scrollValue;
        RankItem lastItem = _listElements.Last();
        lastItem.transform.SetParent(_scrollRect.transform);
        lastItem.Init((rankNumber).ToString(), "", _flags[0]);
        //lastItem.Init((rankNumber).ToString(), "user", _flags[UserDatas.Instance.GetUserData().IDAvatar]);
        PlayerPrefs.SetInt("Rank", rankNumber);

        var changeScroll = DOTween.To(() => scrollValue, x => scrollValue = x, 1, 0.8f);

        changeScroll.OnComplete(() =>
        {
            lastItem.transform.DOMove(_listElements[0].transform.position, 0.5f).OnComplete(() =>
            {
                callback?.Invoke();
            }).Play();
        });
        changeScroll.Play();
    }


    string randomName()
    {
      
      //  string[] data = JsonSerializer.Deserialize<string[]>(dataNames.text);
       // string boyOrGirl = Random.Range(0, 1) == 0 ? "girls" : "boys";
        return "Noah";// data[boyOrGirl][Random.Range(0, data[boyOrGirl].Count)];
    }
    string randomCountry()
    {
        //var data = JsonConvert.DeserializeObject<RandomCountry>(dataCountries.text);
        // var data = JSON.Parse(dataCountries.text).AsArray;
        //return data[Random.Range(0, data.Count)];
        return "Canada";
    }
}


