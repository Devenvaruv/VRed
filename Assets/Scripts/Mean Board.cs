using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName="DataManager", menuName="StatsVR/Data Manager")]
public class DataManager : ScriptableObject
{
    [SerializeField] private List<int> values = new();
    public IReadOnlyList<int> Values => values;

    // C# events broadcast to listeners
    public event Action OnDataChanged;

    public void Add(int v)  { values.Add(v);  OnDataChanged?.Invoke(); }
    public void Remove(int v){ values.Remove(v); OnDataChanged?.Invoke(); }

    // Helpers -------------------------------------------------------------
    public float Mean()   => values.Count==0?0 : (float)values.Average();
    public float Median() {
        if(values.Count==0) return 0;
        var ordered = values.OrderBy(x=>x).ToArray();
        int n = ordered.Length;
        return n%2==1 ? ordered[n/2] : (ordered[n/2-1]+ordered[n/2])/2f;
    }
    public IEnumerable<(int value,int freq)> Mode(){ return values
         .GroupBy(x=>x)
         .OrderByDescending(g=>g.Count())
         .TakeWhile(g=>g.Count()==values.GroupBy(x=>x).Max(h=>h.Count()))
         .Select(g=>(g.Key,g.Count()));}
}
