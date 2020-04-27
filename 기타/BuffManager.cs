using System;
using System.Collections.Generic;
using UnityEngine;

public enum BuffType
{
    None,
    GoldFountain,
    VideoFree,
    BannerFree,
}

public class BuffManager : MonoBehaviour
{
    static BuffManager _instance;
    public static BuffManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BuffManager>();
            }
            return _instance;
        }
    }

    public Dictionary<BuffType, Buff> Buffs = new Dictionary<BuffType, Buff>();

    public Buff FindBuff<T>(BuffType type) where T : class, new()
    {
        if (Buffs.ContainsKey(type) == false)
        {
            T tClass = new T();
            Buff buff = tClass as Buff;

            if (GamePlayManager.Instance.BuffTimes.ContainsKey(type))
            {
                buff.EndTime = GamePlayManager.Instance.BuffTimes[type];
            }
            Buffs.Add(type, buff);
        }

        return Buffs[type];
    }

    public void AddBuff<T>(BuffType type, int hour) where T : class, new()
    {
        var find = FindBuff<T>(type);
        var buff = find as T;

        find.StartBuff(hour);
        (buff as Buff).StartBuff(hour);
        GamePlayManager.Instance.SaveData();
    }

    public Dictionary<BuffType, DateTime> SaveBuffs()
    {
        Dictionary<BuffType, DateTime> buffTimes = new Dictionary<BuffType, DateTime>();

        foreach (BuffType type in Enum.GetValues(typeof(BuffType)))
        {
            if (Buffs.ContainsKey(type))
                buffTimes.Add(type, Buffs[type].EndTime);    
        }

        return buffTimes;
    }
}

[Serializable]
public class Buff
{
    public DateTime EndTime;

    /// <summary>
    /// hour 값이 음수면 영구 지속
    /// </summary>
    public void StartBuff(int hour)
    {
        DateTime startTime = TimeManager.Instance.Now;
        EndTime = hour > 0 ? startTime.AddHours(hour) : DateTime.MaxValue;
    }

    /// <summary>
    /// 버프가 수행해야 할 동작
    /// </summary>
    public virtual T Operate<T>(T value) { return default(T); }

    public TimeSpan GetTime()
    {
        TimeSpan remainTime = EndTime - TimeManager.Instance.Now;
        return remainTime;
    }
}

[Serializable]
public class GoldFountainBuff : Buff
{
    public BuffType Type = BuffType.GoldFountain;
    readonly int GoldBuff = 10;

    public override T Operate<T>(T coin)
    {
        if (GetTime().TotalSeconds > 0 && coin is int)
        {
            int value = Convert.ToInt32(coin) + GoldBuff;
            return (T)Convert.ChangeType(value, typeof(T));
        }
        else
            return coin;
    }
}

[Serializable]
public class VideoFreeBuff : Buff
{
    public override T Operate<T>(T value)
    {
        return value;
        //AdvertisingMgr.Instance.VideoFree = ture;
    }
}

[Serializable]
public class BannerFreeBuff : Buff
{
    public override T Operate<T>(T value)
    {
        return value;
        //AdvertisingMgr.Instance.BannerFree = ture;
    }
}