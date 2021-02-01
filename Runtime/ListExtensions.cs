using System.Collections;
using System.Collections.Generic;
using System;

public static class ListExtensions
{
    public static List<T> AddUnique<T>(this List<T> list, T item)
    {
        if(!list.Contains(item))
            list.Add(item);

        return list;
    }
    
    public static List<T> RemoveUnique<T>(this List<T> list, T item)
    {
        if(list.Contains(item))
            list.Remove(item);

        return list;
    }
}