using System.Collections.Generic;
using UnityEngine;

public static class UpgradeCardSelector
{
    private const int CardCount = 3;

    public static UpgradeData[] Shuffle3(PlayerController player)
    {
        List<UpgradeData> eligible = new List<UpgradeData>();
        foreach (UpgradeData data in Resources.LoadAll<UpgradeData>("Upgrades"))
        {
            if (IsEligible(data, player))
            {
                eligible.Add(data);
            }
        }

        Shuffle(eligible);

        UpgradeData[] result = new UpgradeData[CardCount];
        int resultCount = 0;

        // 1차: 카테고리/대상이 겹치지 않는 카드부터 채움(§7 "3장 모두 다른 카테고리/대상 선호")
        HashSet<string> usedGroups = new HashSet<string>();
        for (int i = 0; i < eligible.Count && resultCount < CardCount; i++)
        {
            string group = GroupKey(eligible[i]);
            if (usedGroups.Contains(group))
            {
                continue;
            }

            usedGroups.Add(group);
            result[resultCount++] = eligible[i];
        }

        // 2차: 그래도 3장이 안 차면 남은 카드로 중복 허용해 채움
        for (int i = 0; i < eligible.Count && resultCount < CardCount; i++)
        {
            if (System.Array.IndexOf(result, eligible[i]) >= 0)
            {
                continue;
            }

            result[resultCount++] = eligible[i];
        }

        return result;
    }

    private static bool IsEligible(UpgradeData data, PlayerController player)
    {
        switch (data.Category)
        {
            case UpgradeCategory.NewWeapon:
                return !player.Weapons.TryGet(data.TargetWeapon, out _);
            case UpgradeCategory.WeaponBuff:
                return player.Weapons.TryGet(data.TargetWeapon, out _);
            case UpgradeCategory.StatBuff:
                return true;
            default:
                return false;
        }
    }

    private static string GroupKey(UpgradeData data)
    {
        return data.Category == UpgradeCategory.StatBuff
            ? $"Stat:{data.StatType}"
            : $"{data.Category}:{data.TargetWeapon}";
    }

    private static void Shuffle(List<UpgradeData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
