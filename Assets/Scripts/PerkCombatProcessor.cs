using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PerkCombatProcessor : MonoBehaviour
{
    private DiceUIController diceUI;

    public void Initialize(DiceUIController diceUIController)
    {
        diceUI = diceUIController;
    }

    public IEnumerator ProcessPerks(CombatPayload payload, List<int> rolls)
    {
        if (RunManager.instance == null || RunManager.instance.activePerks.Count == 0)
            yield break;

        List<BasePerk> perksToProcess = RunManager.instance.activePerks.FindAll(p => p != null);
        perksToProcess.Sort((a, b) =>
        {
            int r = b.isRerollPerk.CompareTo(a.isRerollPerk);
            return r != 0 ? r : a.priority.CompareTo(b.priority);
        });

        yield return StartCoroutine(ProcessPerksFromList(payload, rolls, perksToProcess));
    }

    public IEnumerator ProcessLetsGoAgainPass(CombatPayload payload, List<int> rolls)
    {
        if (RunManager.instance == null) yield break;
        if (!RunManager.instance.activePerks.Exists(p => p is LetsGoAgainPerk)) yield break;

        var lgaPerk = RunManager.instance.activePerks.Find(p => p is LetsGoAgainPerk);
        if (lgaPerk != null && !diceUI.skipDiceVisuals)
        {
            lgaPerk.TriggerVisualPop();
            if (PerkListUI.instance != null)
                PerkListUI.instance.TriggerShakeForPerk(lgaPerk);
            yield return StartCoroutine(diceUI.SkippableWait(0.4f));
        }

        List<BasePerk> secondPass = RunManager.instance.activePerks
            .FindAll(p => p != null && !(p is LetsGoAgainPerk));
        secondPass.Sort((a, b) =>
        {
            int r = b.isRerollPerk.CompareTo(a.isRerollPerk);
            return r != 0 ? r : a.priority.CompareTo(b.priority);
        });

        payload.flatBonus = 0;
        payload.multiplier = 1.0f;

        yield return StartCoroutine(ProcessPerksFromList(payload, rolls, secondPass));

        var sfPerk = RunManager.instance.activePerks.Find(p => p is SymbioticFuryPerk);
        if (sfPerk != null && !diceUI.skipDiceVisuals)
        {
            sfPerk.TriggerVisualPop();
            if (PerkListUI.instance != null)
                PerkListUI.instance.TriggerShakeForPerk(sfPerk);
            diceUI.UpdateTotalDamageDisplay(payload.GetFinalDamage());
            yield return StartCoroutine(diceUI.SkippableWait(0.3f));
        }
    }

    private IEnumerator ProcessPerksFromList(CombatPayload payload, List<int> rolls, List<BasePerk> perks)
    {
        foreach (BasePerk perk in perks)
        {
            int beforeTotal = payload.GetFinalDamage();
            perk.ModifyCombat(payload);

            bool anyDieChanged = false;
            List<int> changedIndices = new List<int>();
            for (int i = 0; i < rolls.Count; i++)
                if (rolls[i] != payload.diceRolls[i])
                    changedIndices.Add(i);

            if (changedIndices.Count > 0)
            {
                if (!diceUI.skipDiceVisuals)
                {
                    if (perk.isRerollPerk)
                    {
                        foreach (int idx in changedIndices)
                        {
                            if (idx < diceUI.SpawnedDiceUI.Count)
                            {
                                Animator dieAnim = diceUI.SpawnedDiceUI[idx].GetComponent<Animator>();
                                TMP_Text dieText = diceUI.SpawnedDiceUI[idx].GetComponentInChildren<TMP_Text>();
                                if (dieAnim != null) dieAnim.enabled = true;
                                if (dieText != null) dieText.text = "!";
                            }
                        }
                        yield return StartCoroutine(diceUI.SkippableWait(0.5f));
                        foreach (int idx in changedIndices)
                        {
                            rolls[idx] = payload.diceRolls[idx];
                            if (idx < diceUI.SpawnedDiceUI.Count)
                            {
                                Animator dieAnim = diceUI.SpawnedDiceUI[idx].GetComponent<Animator>();
                                if (dieAnim != null) dieAnim.enabled = false;
                            }
                            diceUI.AnimateSpecificDie(idx, rolls[idx]);
                        }
                    }
                    else
                    {
                        foreach (int idx in changedIndices)
                        {
                            rolls[idx] = payload.diceRolls[idx];
                            diceUI.AnimateSpecificDie(idx, rolls[idx]);
                        }
                    }
                    anyDieChanged = true;
                    yield return StartCoroutine(diceUI.SkippableWait(0.3f));
                }
                else
                {
                    foreach (int idx in changedIndices)
                        rolls[idx] = payload.diceRolls[idx];
                    anyDieChanged = true;
                }
            }

            int afterTotal = payload.GetFinalDamage();
            if (beforeTotal != afterTotal || anyDieChanged)
            {
                if (!diceUI.skipDiceVisuals)
                {
                    perk.TriggerVisualPop();
                    if (PerkListUI.instance != null)
                        PerkListUI.instance.TriggerShakeForPerk(perk);
                    diceUI.UpdateTotalDamageDisplay(afterTotal);
                    yield return StartCoroutine(diceUI.SkippableWait(0.3f));
                }
            }
        }
    }
}
