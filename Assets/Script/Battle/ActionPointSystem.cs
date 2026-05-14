using UnityEngine;

public class ActionPointSystem
{
    public int Current => current;
    public bool IsEmpty => current <= 0;

    private int current;

    public bool TryConsume(int cost)
    {
        if (cost < 0 || current < cost)
        {
            return false;
        }

        current -= cost;
        return true;
    }

    public bool CanConsume(int cost)
    {
        return current - cost >= 0;
    }

    public void PlusPoint(int number)
    {
        current += number;
    }

}
