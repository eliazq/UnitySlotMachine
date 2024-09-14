using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Symbol
{
    public string name;
    public GameObject prefab;
    public float multiplier;
}

public class SlotMachine : MonoBehaviour
{
    [Header("Slot Machine")]
    [SerializeField] Transform[] slots;  // Should refer to the slot objects in your 3D scene
    [SerializeField] Symbol[] symbols;

    [Header("Settings (change these values to match the slot machine)")]
    [SerializeField] int horizontalSlotCount = 5;
    [SerializeField] int verticalSlotCount = 3;

    [Header("Line Drawing")]
    [SerializeField] GameObject lineRendererPrefab;  // Prefab for a GameObject with LineRenderer attached

    private List<GameObject> _matchLines;
    private bool isSpinning = false;

    bool stopped = false;


    private void Awake()
    {
        _matchLines = new List<GameObject>();
    }

    public void Spin(float bet, out float win)
    {
        if (stopped) { win = 0; return; };

        win = 0;  // Initialize win to 0
        if (isSpinning)
        {
            Debug.Log("Already spinning, wait for the spin to complete.");
        }

        isSpinning = true;

        // Clear any previous symbols and match lines
        ClearSlots();
        ClearMatchLines();

        int currentSlotIndex = 0;
        for (int y = 0; y < verticalSlotCount; y++)
        {
            for (int x = 0; x < horizontalSlotCount; x++)
            {
                Transform slot = slots[currentSlotIndex];
                currentSlotIndex++;

                int randomSymbolIndex = UnityEngine.Random.Range(0, symbols.Length);
                GameObject slotSymbol = Instantiate(symbols[randomSymbolIndex].prefab, slot);
                slotSymbol.transform.localPosition = Vector3.zero;
                slotSymbol.name = symbols[randomSymbolIndex].name;
            }
        }

        // Run match checking and win calculation
        float matchMultiplier = CheckForMatches();

        // Calculate win based on the match multiplier and the bet
        if (matchMultiplier > 0)
        {
            win = bet * matchMultiplier;
        }

        isSpinning = false;
    }

    private void DrawLineWithLineRenderer(Vector3 start, Vector3 end)
    {
        GameObject lineObject = Instantiate(lineRendererPrefab);
        LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();

        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;

        _matchLines.Add(lineObject);
    }

    private float CheckForMatches()
    {
        float totalMultiplier = 0;

        // Dictionary to track how many times each symbol is matched across different rows
        Dictionary<string, int> symbolMatchCounts = new Dictionary<string, int>();

        // Check vertical matches and get match multiplier
        totalMultiplier += CheckVerticalMatches(ref symbolMatchCounts);

        // Check horizontal matches and get match multiplier
        totalMultiplier += CheckHorizontalMatches(ref symbolMatchCounts);

        // Apply additional multiplier based on how many times each symbol was matched
        totalMultiplier += ApplyAdditionalMultiplier(symbolMatchCounts);

        return totalMultiplier;
    }

    private float CheckVerticalMatches(ref Dictionary<string, int> symbolMatchCounts)
    {
        float totalMultiplier = 0;

        for (int x = 0; x < horizontalSlotCount; x++)
        {
            string previousSymbolName = null;
            int matchCount = 0;
            Symbol matchSymbol = null;

            for (int y = 0; y < verticalSlotCount; y++)
            {
                int currentIndex = y * horizontalSlotCount + x;
                Transform currentSlot = slots[currentIndex];
                GameObject currentSymbol = currentSlot.GetChild(0).gameObject;

                Symbol currentSymbolData = GetSymbolDataByName(currentSymbol.name);

                if (previousSymbolName == null)
                {
                    previousSymbolName = currentSymbol.name;
                    matchSymbol = currentSymbolData;
                    matchCount = 1;
                }
                else if (currentSymbol.name == previousSymbolName)
                {
                    matchCount++;

                    if (matchCount == verticalSlotCount)
                    {
                        Vector3 start = slots[x].position;
                        Vector3 end = slots[(verticalSlotCount - 1) * horizontalSlotCount + x].position;
                        DrawLineWithLineRenderer(start, end);

                        totalMultiplier += matchSymbol.multiplier * GetMultiplierForMatches(matchCount);

                        // Track the symbol match count
                        AddToSymbolMatchCount(symbolMatchCounts, previousSymbolName, matchCount);
                    }
                }
                else
                {
                    matchCount = 1;
                    previousSymbolName = currentSymbol.name;
                    matchSymbol = currentSymbolData;
                }
            }
        }

        return totalMultiplier;
    }

    private float CheckHorizontalMatches(ref Dictionary<string, int> symbolMatchCounts)
    {
        float totalMultiplier = 0;

        for (int y = 0; y < verticalSlotCount; y++)
        {
            string previousSymbolName = null;
            int matchCount = 0;
            int matchStartIndex = 0;
            Symbol matchSymbol = null;

            for (int x = 0; x < horizontalSlotCount; x++)
            {
                int currentIndex = y * horizontalSlotCount + x;
                Transform currentSlot = slots[currentIndex];
                GameObject currentSymbol = currentSlot.GetChild(0).gameObject;

                Symbol currentSymbolData = GetSymbolDataByName(currentSymbol.name);

                if (previousSymbolName == null)
                {
                    previousSymbolName = currentSymbol.name;
                    matchSymbol = currentSymbolData;
                    matchCount = 1;
                    matchStartIndex = x;
                }
                else if (currentSymbol.name == previousSymbolName)
                {
                    matchCount++;

                    if (matchCount >= 3 && (x == horizontalSlotCount - 1 || slots[(y * horizontalSlotCount) + x + 1].GetChild(0).gameObject.name != previousSymbolName))
                    {
                        Vector3 start = slots[y * horizontalSlotCount + matchStartIndex].position;
                        Vector3 end = slots[y * horizontalSlotCount + x].position;
                        DrawLineWithLineRenderer(start, end);

                        totalMultiplier += matchSymbol.multiplier * GetMultiplierForMatches(matchCount);

                        // Track the symbol match count
                        AddToSymbolMatchCount(symbolMatchCounts, previousSymbolName, matchCount);
                    }
                }
                else
                {
                    previousSymbolName = currentSymbol.name;
                    matchCount = 1;
                    matchStartIndex = x;
                    matchSymbol = currentSymbolData;
                }
            }
        }

        return totalMultiplier;
    }

    // Helper function to track how many times a symbol is matched
    private void AddToSymbolMatchCount(Dictionary<string, int> symbolMatchCounts, string symbolName, int matchCount)
    {
        if (symbolMatchCounts.ContainsKey(symbolName))
        {
            symbolMatchCounts[symbolName] += matchCount;
        }
        else
        {
            symbolMatchCounts[symbolName] = matchCount;
        }
    }

    private float ApplyAdditionalMultiplier(Dictionary<string, int> symbolMatchCounts)
    {
        float additionalMultiplier = 0;

        foreach (var entry in symbolMatchCounts)
        {
            // Apply different multipliers based on the number of matches for each symbol
            if (entry.Value > 22)
            {
                additionalMultiplier += entry.Value * 1000f;  // Extreme high value for rare occurrences
            }
            else if (entry.Value > 21)
            {
                additionalMultiplier += entry.Value * 750f;   // High value for a strong occurrence
            }
            else if (entry.Value > 20)
            {
                additionalMultiplier += entry.Value * 150f;   // Significant, but less than 22+
            }
            else if (entry.Value > 19)
            {
                additionalMultiplier += entry.Value * 70f;   // Mid-high multiplier
            }
            else if (entry.Value > 18)
            {
                additionalMultiplier += entry.Value * 50f;   // Slightly higher than moderate
            }
            else if (entry.Value > 17)
            {
                additionalMultiplier += entry.Value * 40f;   // Moderately valuable occurrence
            }
            else if (entry.Value > 16)
            {
                additionalMultiplier += entry.Value * 30f;    // Slightly above average
            }
            else if (entry.Value > 15)
            {
                additionalMultiplier += entry.Value * 25f;    // Average high
            }
            else if (entry.Value > 14)
            {
                additionalMultiplier += entry.Value * 15f;    // Slightly below average
            }
            else if (entry.Value > 13)
            {
                additionalMultiplier += entry.Value * 13f;    // Commonly valuable
            }
            else if (entry.Value > 12)
            {
                additionalMultiplier += entry.Value * 7.5f;    // Normal low value
            }
            else if (entry.Value > 11)
            {
                additionalMultiplier += entry.Value * 6f;    // Low average
            }
            else if (entry.Value > 10)
            {
                additionalMultiplier += entry.Value * 4.5f;     // Slightly below low average
            }
            else if (entry.Value > 9)
            {
                additionalMultiplier += entry.Value * 3f;     // Small value
            }
            else if (entry.Value > 8)
            {
                additionalMultiplier += entry.Value * 2f;     // Very small value
            }
            else if (entry.Value > 7)
            {
                additionalMultiplier += entry.Value * 0.6f;     // Minimum multiplier range starts here
            }
            else if (entry.Value > 6)
            {
                additionalMultiplier += entry.Value * 0.35f;     // Minimum meaningful value
            }
            else if (entry.Value > 5)
            {
                additionalMultiplier += entry.Value * 0.25f;      // Negligible but exists
            }
            else if (entry.Value > 4)
            {
                additionalMultiplier += entry.Value * 0.2f;      // Very low multiplier
            }
            else
            {
                additionalMultiplier += entry.Value * 0f;      // No multiplier for low values
            }
        }

        return additionalMultiplier;
    }
    // Helper function to get the Symbol data by name
    private Symbol GetSymbolDataByName(string symbolName)
    {
        foreach (var symbol in symbols)
        {
            if (symbol.name == symbolName)
            {
                return symbol;
            }
        }

        return null;  // Return null if no symbol is found, though this case shouldn't occur
    }

    // Define multipliers for different match counts
    private float GetMultiplierForMatches(int matchCount)
    {
        switch (matchCount)
        {
            case 3: return 0.2f;
            case 4: return 0.4f;
            case 5: return 0.8f;
            default: return 0.0f; // No win for less than 3 matches
        }
    }

    private void ClearSlots()
    {
        foreach (var slot in slots)
        {
            while (slot.childCount > 0)
            {
                DestroyImmediate(slot.GetChild(0).gameObject);  // Use DestroyImmediate to ensure slots are cleared immediately
            }
        }
    }

    private void ClearMatchLines()
    {
        foreach (var line in _matchLines)
        {
            Destroy(line);  // Destroy all match lines
        }
        _matchLines.Clear();  // Clear the list
    }
}
