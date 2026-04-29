using System;

[Serializable]
public class OrderRequest
{
    public int userId;
    public float totalAmount;
    public string returnUrl;
    public string cancelUrl;
    public string playerEmail;
    public string playerUserName;
    public string expiredAt;
    public OrderItem[] items;
    public bool isSuccess;
    public string accountName;
}

[Serializable]
public class OrderItem
{
    public int bundleId;
    public int bundleBuyQuantity;
}