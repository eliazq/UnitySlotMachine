using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlotMachineUI : MonoBehaviour
{
    [SerializeField] Button spinButton;
    [SerializeField] SlotMachine slotMachine;

    [SerializeField] private double money = 100f;

    [SerializeField] private double bet = 0.20f;

    [SerializeField] private bool fastTestMode = false;

    private float highestWin = 0;

    private void Start()
    {
        slotMachine = GetComponent<SlotMachine>();
        spinButton.onClick.AddListener(() =>
        {
            slotMachine.Spin((float)bet, out float win);
            money -= bet;
            money += win;
            Debug.Log("Bet " + bet);
            Debug.Log($"you won {win}€");

        });
    }

    private void Update()
    {
        if (!fastTestMode) return;
        slotMachine.Spin((float)bet, out float win);
        money -= bet;
        money += win;
        if (win > highestWin) highestWin = win;
        Debug.Log(highestWin.ToString() + "€");
    }
}
