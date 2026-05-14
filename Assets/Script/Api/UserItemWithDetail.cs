using System;

[Serializable]
public class UserItemWithDetail
{
    public int userId;
    public int itemId;
    public int quantity;
    public int shopOrderId;
    public string itemName;
    public string itemDescription;
    public string itemImageUrl;
    public string itemStatus;
}