using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RankItem : MonoBehaviour
{
    [SerializeField] Text _rankNumber, _namePlayer;
    [SerializeField] Image _countryFlag;
    [SerializeField] Transform _star;
    public Transform Star => _star;


    public void Init(string rankNumber, string namePlayer, Sprite countryFlag)
    {
        if (!string.IsNullOrEmpty(rankNumber))
            _rankNumber.text = rankNumber;
        if (!string.IsNullOrEmpty(namePlayer))
            _namePlayer.text = namePlayer;
        if (countryFlag != null)
            _countryFlag.sprite = countryFlag;
    }
}
