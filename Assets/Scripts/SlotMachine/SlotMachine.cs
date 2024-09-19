using System;
using System.Collections;
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
    [SerializeField] Transform[] slots;
    [SerializeField] Symbol[] symbols;
    [SerializeField] SlotsHandler slotsHandler;
    [SerializeField] float matchesShowingDelay = 0.3f;
    [SerializeField] float matchesScale = 1.1f;

    [Header("Settings (change these values to match the slot machine)")]
    [SerializeField] int horizontalSlotCount = 5;
    [SerializeField] int verticalSlotCount = 3;

    [Header("Line Drawing")]
    [SerializeField] GameObject lineRendererPrefab;  // Prefab for a GameObject with LineRenderer attached

    private List<GameObject> _matchLines;
    public bool isSpinning = false;

    bool stopped = false;

    public bool hasSpinned = false;

    public float won = 0;

    private void Awake()
    {
        _matchLines = new List<GameObject>();
    }

    public void Spin(float bet)
    {
        if (stopped) { return; };
        slotsHandler.ResetSlots();

        slotsHandler.MoveSlotsToZero();

        isSpinning = true;
        hasSpinned = false;
        // Clear any previous symbols and match lines
        won = 0;
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

        StartCoroutine(StartMatchChecking(bet));
    }

    IEnumerator StartMatchChecking(float bet)
    {
        while (true)
        {
            if (slotsHandler.IsMoving) yield return null;
            else
            {
                break;
            }
        }
        yield return new WaitForSeconds(matchesShowingDelay);
        // Run match checking and win calculation
        float matchMultiplier = CheckForMatches();

        // Calculate win based on the match multiplier and the bet
        if (matchMultiplier > 0)
        {
            won = bet * matchMultiplier;
        }
        hasSpinned = true;

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
        StartCoroutine(ToggleGameObjectActive(lineObject, 0.5f));
    }

    IEnumerator ToggleGameObjectActive(GameObject target, float interval)
    {
        // Check if the GameObject exists and is not destroyed
        while (target != null)
        {
            // Wait for the specified interval before toggling the state
            yield return new WaitForSeconds(interval);

            // Toggle active state
            if (target != null)
                target.SetActive(!target.activeSelf);
        }

        // If GameObject is destroyed, exit the coroutine
        yield break;
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

                        // Scale matching symbols
                        for (int scaleIndex = 0; scaleIndex < verticalSlotCount; scaleIndex++)
                        {
                            int symbolIndex = scaleIndex * horizontalSlotCount + x;
                            StartCoroutine(ScaleSymbolOverTime(slots[symbolIndex].GetChild(0).gameObject));
                        }

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

                        // Scale matching symbols
                        for (int scaleIndex = matchStartIndex; scaleIndex <= x; scaleIndex++)
                        {
                            int symbolIndex = y * horizontalSlotCount + scaleIndex;
                            StartCoroutine(ScaleSymbolOverTime(slots[symbolIndex].GetChild(0).gameObject));
                        }

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
                additionalMultiplier += entry.Value * 10f;    // Normal low value
            }
            else if (entry.Value > 11)
            {
                additionalMultiplier += entry.Value * 8f;    // Low average
            }
            else if (entry.Value > 10)
            {
                additionalMultiplier += entry.Value * 6.5f;     // Slightly below low average
            }
            else if (entry.Value > 9)
            {
                additionalMultiplier += entry.Value * 5.5f;     // Small value
            }
            else if (entry.Value > 8)
            {
                additionalMultiplier += entry.Value * 4f;     // Very small value
            }
            else if (entry.Value > 7)
            {
                additionalMultiplier += entry.Value * 1f;     // Minimum multiplier range starts here
            }
            else if (entry.Value > 6)
            {
                additionalMultiplier += entry.Value * 0.8f;     // Minimum meaningful value
            }
            else if (entry.Value > 5)
            {
                additionalMultiplier += entry.Value * 0.4f;      // Negligible but exists
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

    private IEnumerator ScaleSymbolOverTime(GameObject symbol)
    {
        float scaleFactor = matchesScale;
        float duration = 0.35f;
        Vector3 originalScale = symbol.transform.localScale;
        Vector3 targetScale = originalScale * scaleFactor;

        // Scale up
        float time = 0;
        while (time < duration && symbol != null)
        {
            symbol.transform.localScale = Vector3.Lerp(originalScale, targetScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        // Hold at max scale for a brief moment
        yield return new WaitForSeconds(.2f);

        // Scale back down
        time = 0;
        while (time < duration && symbol != null)
        {
            symbol.transform.localScale = Vector3.Lerp(targetScale, originalScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        if (symbol != null)
            symbol.transform.localScale = originalScale;  // Ensure it's reset to the original scale
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
