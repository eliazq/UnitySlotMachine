using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotMachineUI : MonoBehaviour
{
    [SerializeField] Button spinButton;
    [SerializeField] SlotMachine slotMachine;

    [SerializeField] TextMeshProUGUI winningText;
    [SerializeField] TextMeshProUGUI balanceText;

    [SerializeField] private double money = 100.00f;

    [SerializeField] private double bet = 0.20f;

    public void Spin()
    {
        slotMachine.Spin((float)bet);
        money -= bet;
        balanceText.text = "Balance " + RoundFloatToTwoDecimals((float)money) + "€";
        StartCoroutine(GetWinnings());
    }

    IEnumerator GetWinnings()
    {
        while (true)
        {
            if (slotMachine.isSpinning) yield return null;
            else break;
        }
        money += slotMachine.won;
        balanceText.text = "Balance " + RoundFloatToTwoDecimals((float)money) + "€";
        // Round the winnings to two decimal places before formatting
        winningText.text = FormatMoneyWithSpaces(RoundFloatToTwoDecimals(slotMachine.won)) + "€";
    }

    public float RoundFloatToTwoDecimals(float value)
    {
        return Mathf.Round(value * 100f) / 100f;
    }

    public string FormatMoneyWithSpaces(float amount)
    {
        return amount.ToString("#,0.00", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
    }
}
