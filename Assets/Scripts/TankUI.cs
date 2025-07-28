using UnityEngine;
using UnityEngine.UI;



public class TankUI : MonoBehaviour {

    [Header("References")]
    [SerializeField] private Image _reloadBar;
    [SerializeField] private Image _healthBar;



    void Update() {
        transform.forward = Camera.main.transform.forward;
    }



    public void UpdateReloadBar(float fillAmount) {
        _reloadBar.fillAmount = fillAmount;
    }



    public void UpdateHealthBar(float fillAmount) {
        _healthBar.fillAmount = fillAmount;
    }



}